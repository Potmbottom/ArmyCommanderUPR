using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    public class GameRoot : MonoBehaviour, IStartable, IFixedTickable, ILateTickable, IDisposable
    {
        [SerializeField] private string _gameSceneName = "Game";
        [SerializeField] private Transform _levelContainer;
        [SerializeField] private UIControl _uiControl;

        private IFieldPModel _field;
        private ITrainingFieldPModel _trainingField;
        private IPlayerPModel _playerModel;
        private IArmyUpgradePModel _armyUpgradeModel;
        private IResourcePModel _resourceModel;
        private IUIModel _uiModel;
        private readonly List<IBarrackSlotPModel> _slotModels = new();

        private BarrackService _barrackService;
        private UIService _uiService;
        private TroopsConfig _troopsConfig;
        private LevelConfig _levelConfig;
        private ISaveStorage _saveStorage;
        private LevelRuntimeRoot _activeLevelRoot;
        private GameObject _activeLevelInstance;
        private PlayerControl _activePlayerControl;
        private bool _isStarted;
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void SetDependency(
            IFieldPModel field,
            ITrainingFieldPModel trainingField,
            IPlayerPModel playerModel,
            IArmyUpgradePModel armyUpgradeModel,
            IResourcePModel resourceModel,
            IUIModel uiModel,
            BarrackService barrackService,
            UIService uiService,
            TroopsConfig troopsConfig,
            LevelConfig levelConfig,
            ISaveStorage saveStorage)
        {
            _field = field;
            _trainingField = trainingField;
            _playerModel = playerModel;
            _armyUpgradeModel = armyUpgradeModel;
            _resourceModel = resourceModel;
            _uiModel = uiModel;
            _barrackService = barrackService;
            _uiService = uiService;
            _troopsConfig = troopsConfig;
            _levelConfig = levelConfig;
            _saveStorage = saveStorage;
        }

        void IStartable.Start()
        {
            if (_isStarted) return;
            _isStarted = true;

            var currentLevelIndex = NormalizeLevelIndex(_saveStorage.GetInt(ProgressionKeys.CurrentLevel, 0));
            _saveStorage.SetInt(ProgressionKeys.CurrentLevel, currentLevelIndex);
            _armyUpgradeModel.SetLevel(GetArmyLevelForProgressionIndex(currentLevelIndex));

            if (!TryGetLevelData(currentLevelIndex, out var levelData))
                return;

            _activeLevelRoot = LoadLevel(levelData);
            if (_activeLevelRoot == null)
            {
                Debug.LogError($"{nameof(GameRoot)}: Failed to load level prefab for index {currentLevelIndex}.");
                return;
            }

            if (!TryGetRequiredControls(_activeLevelRoot, out _activePlayerControl, out var trainingFieldControl, out var armyUpgradeControl))
                return;

            _activePlayerControl.Bind(_playerModel);
            trainingFieldControl.Bind(_trainingField);
            armyUpgradeControl.Bind(_armyUpgradeModel);
            _uiControl.Bind(_uiModel);

            _uiModel.OnNextLevelRequested
                .Subscribe(_ => LoadNextLevel())
                .AddTo(_disposables);

            _uiModel.OnReloadRequested
                .Subscribe(_ => ReloadCurrentLevel())
                .AddTo(_disposables);
            
            _playerModel.OnDead
                .Subscribe(_ => _uiModel.ShowEndGamePopup())
                .AddTo(_disposables);

            foreach (var slotControl in _activeLevelRoot.BarrackSlotControls ?? Array.Empty<BarrackSlotControl>())
            {
                if (slotControl == null) continue;
                var slotModel = new BarrackSlotPModel();
                _slotModels.Add(slotModel);
                slotControl.Bind(slotModel);
                _barrackService.RegisterSlot(slotModel);
                _uiService.RegisterSlot(slotModel);
            }

            SpawnEnemies(_activeLevelRoot.EnemySpawnPoints ?? Array.Empty<EnemySpawnPoint>(), _activePlayerControl.transform.position.y);
            _resourceModel.AddSilver(levelData.InitialSilver);
        }

        public void FixedTick()
        {
            if (_activePlayerControl == null) return;
            _activePlayerControl.FixedTick();
        }

        public void LateTick()
        {
            if (_activePlayerControl == null) return;
            _activePlayerControl.LateTick();
        }

        private void SpawnEnemies(IReadOnlyList<EnemySpawnPoint> spawnPoints, float planeY)
        {
            foreach (var spawnPoint in spawnPoints)
            {
                if (spawnPoint?.SpawnTransform == null) continue;
                var spawnPosition = spawnPoint.SpawnTransform.position;
                spawnPosition.y = planeY;

                var data = _troopsConfig.GetData(spawnPoint.TroopDataIndex);
                _field.CreateTroop(
                    spawnPoint.TroopDataIndex,
                    Team.Enemy,
                    spawnPosition,
                    spawnPosition,
                    data.Health,
                    AIBehaviour.Aggressive
                );
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();

            foreach (var slotModel in _slotModels)
                slotModel.Dispose();
            _slotModels.Clear();

            _activePlayerControl = null;

            if (_activeLevelInstance != null)
                Destroy(_activeLevelInstance);
        }

        private void LoadNextLevel()
        {
            if (!HasValidLevelConfig()) return;

            var currentLevel = NormalizeLevelIndex(_saveStorage.GetInt(ProgressionKeys.CurrentLevel, 0));
            var nextLevel = currentLevel + 1;
            if (nextLevel >= _levelConfig.Levels.Count)
                nextLevel = 0;

            _saveStorage.SetInt(ProgressionKeys.CurrentLevel, nextLevel);
            SceneManager.LoadScene(_gameSceneName);
        }
        
        private void ReloadCurrentLevel()
        {
            SceneManager.LoadScene(_gameSceneName);
        }

        private LevelRuntimeRoot LoadLevel(LevelData levelData)
        {
            if (levelData == null || levelData.LevelPrefab == null)
            {
                Debug.LogError($"{nameof(GameRoot)}: Level prefab is not assigned.");
                return null;
            }

            var parent = _levelContainer != null ? _levelContainer : transform;
            _activeLevelInstance = Instantiate(levelData.LevelPrefab, parent);
            if (!_activeLevelInstance.TryGetComponent(out LevelRuntimeRoot levelRoot))
            {
                Debug.LogError($"{nameof(GameRoot)}: Loaded level prefab does not contain {nameof(LevelRuntimeRoot)}.");
                Destroy(_activeLevelInstance);
                _activeLevelInstance = null;
                return null;
            }

            return levelRoot;
        }

        private bool TryGetRequiredControls(
            LevelRuntimeRoot levelRoot,
            out PlayerControl playerControl,
            out TrainingFieldControl trainingFieldControl,
            out ArmyUpgradeControl armyUpgradeControl)
        {
            playerControl = null;
            trainingFieldControl = null;
            armyUpgradeControl = null;

            if (levelRoot == null) return false;

            playerControl = levelRoot.PlayerControl;
            trainingFieldControl = levelRoot.TrainingFieldControl;
            armyUpgradeControl = levelRoot.ArmyUpgradeControl;

            if (playerControl != null && trainingFieldControl != null && armyUpgradeControl != null)
                return true;

            Debug.LogError($"{nameof(GameRoot)}: LevelRuntimeRoot missing required controls (Player/TrainingField/ArmyUpgrade).");
            return false;
        }

        private bool TryGetLevelData(int levelIndex, out LevelData levelData)
        {
            levelData = null;
            if (!HasValidLevelConfig()) return false;

            var safeIndex = NormalizeLevelIndex(levelIndex);
            levelData = _levelConfig.GetData(safeIndex);
            if (levelData == null)
            {
                Debug.LogError($"{nameof(GameRoot)}: Level data at index {safeIndex} is null.");
                return false;
            }

            return true;
        }

        private bool HasValidLevelConfig()
        {
            if (_levelConfig == null || _levelConfig.Levels == null || _levelConfig.Levels.Count == 0)
            {
                Debug.LogError($"{nameof(GameRoot)}: LevelConfig has no levels.");
                return false;
            }

            return true;
        }

        private int NormalizeLevelIndex(int index)
        {
            if (!HasValidLevelConfig()) return 0;

            if (index < 0) return 0;
            if (index >= _levelConfig.Levels.Count) return _levelConfig.Levels.Count - 1;
            return index;
        }

        private static ArmyLevel GetArmyLevelForProgressionIndex(int levelIndex)
        {
            var levelValue = Mathf.Clamp(levelIndex + 1, (int)ArmyLevel.Level1, (int)ArmyLevel.Level3);
            return (ArmyLevel)levelValue;
        }
    }
}
