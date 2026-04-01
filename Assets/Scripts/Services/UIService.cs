using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    public class UIService : IInitializable, IDisposable
    {
        private const int DefaultBarrackBuildCostSilver = 1;

        private IUIModel _ui;
        private IArmyUpgradePModel _armyUpgrade;
        private IResourcePModel _resources;
        private IFieldPModel _field;
        private LevelConfig _levelConfig;
        private ISaveStorage _saveStorage;
        private LevelData _currentLevelData;

        private readonly CompositeDisposable _disposables = new();
        private IBarrackSlotPModel _activeSlot;
        private readonly Dictionary<IBarrackSlotPModel, TroopType> _slotTroopTypes = new();
        private int _currentSilver;
        private int _initialEnemyCount;
        private int _currentEnemyCount;

        [Inject]
        public void SetDependency(IUIModel ui, IArmyUpgradePModel armyUpgrade, IResourcePModel resources, IFieldPModel field, LevelConfig levelConfig, ISaveStorage saveStorage)
        {
            _ui = ui;
            _armyUpgrade = armyUpgrade;
            _resources = resources;
            _field = field;
            _levelConfig = levelConfig;
            _saveStorage = saveStorage;
        }

        public void Initialize()
        {
            _currentLevelData = GetCurrentLevelData();
            RebuildEnemyProgressBaseline();

            _resources.Gold
                .Subscribe(gold => _ui.SetGold(gold))
                .AddTo(_disposables);

            _resources.Silver
                .Subscribe(silver =>
                {
                    _currentSilver = silver;
                    _ui.SetSilver(silver);
                })
                .AddTo(_disposables);

            _ui.OnBuildSelected
                .Subscribe(OnBuildSelected)
                .AddTo(_disposables);

            _field.OnTroopAdded
                .Subscribe(OnTroopAdded)
                .AddTo(_disposables);

            _field.OnTroopRemoved
                .Subscribe(OnTroopRemoved)
                .AddTo(_disposables);
        }

        public void RegisterSlot(IBarrackSlotPModel slot)
        {
            _slotTroopTypes[slot] = TroopType.Empty;

            slot.TroopType
                .Subscribe(type => _slotTroopTypes[slot] = type)
                .AddTo(_disposables);

            slot.IsPlayerInZone
                .Subscribe(inZone => OnSlotZoneChanged(slot, inZone))
                .AddTo(_disposables);
        }

        private void OnSlotZoneChanged(IBarrackSlotPModel slot, bool inZone)
        {
            if (inZone)
            {
                _activeSlot = slot;
                if (!IsSlotEmpty(slot))
                {
                    _ui.HideBuildPopup();
                    return;
                }

                var availableTypes = GetAvailableTypes();
                if (availableTypes.Count == 0)
                {
                    _ui.HideBuildPopup();
                    return;
                }

                var affordableTypes = GetAffordableTypes(availableTypes);
                _ui.ShowBuildPopup(availableTypes, affordableTypes);
            }
            else if (_activeSlot == slot)
            {
                _activeSlot = null;
                _ui.HideBuildPopup();
            }
        }

        private void OnBuildSelected(TroopType type)
        {
            if (_activeSlot == null) return;

            var costSilver = GetBarrackBuildCostSilver(type);
            if (!_resources.TrySpendSilver(costSilver)) return;

            _activeSlot.SetTroopType(type);
            _ui.HideBuildPopup();
            _activeSlot = null;
        }

        private List<TroopType> GetAvailableTypes()
        {
            var level = (int)_armyUpgrade.CurrentLevel;
            var available = new List<TroopType>();
            if (level >= 1) available.Add(TroopType.Soldier);
            if (level >= 2) available.Add(TroopType.Veteran);
            if (level >= 3) available.Add(TroopType.Master);
            return available;
        }

        private bool IsSlotEmpty(IBarrackSlotPModel slot)
        {
            if (_slotTroopTypes.TryGetValue(slot, out var troopType))
                return troopType == TroopType.Empty;

            return false;
        }

        private List<TroopType> GetAffordableTypes(List<TroopType> availableTypes)
        {
            var affordableTypes = new List<TroopType>();
            foreach (var type in availableTypes)
            {
                if (_currentSilver >= GetBarrackBuildCostSilver(type))
                    affordableTypes.Add(type);
            }

            return affordableTypes;
        }

        private int GetBarrackBuildCostSilver(TroopType type)
        {
            if (_currentLevelData == null)
                return DefaultBarrackBuildCostSilver;

            return type switch
            {
                TroopType.Soldier => ResolveBarrackCost(_currentLevelData.BarrackSoldierCostSilver),
                TroopType.Veteran => ResolveBarrackCost(_currentLevelData.BarrackVeteranCostSilver),
                TroopType.Master => ResolveBarrackCost(_currentLevelData.BarrackMasterCostSilver),
                _ => DefaultBarrackBuildCostSilver
            };
        }

        private static int ResolveBarrackCost(int configuredCost)
        {
            return configuredCost > 0 ? configuredCost : DefaultBarrackBuildCostSilver;
        }

        private LevelData GetCurrentLevelData()
        {
            if (_levelConfig == null || _levelConfig.Levels == null || _levelConfig.Levels.Count == 0)
                return null;

            var currentLevelIndex = _saveStorage.GetInt(ProgressionKeys.CurrentLevel, 0);
            currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, _levelConfig.Levels.Count - 1);
            return _levelConfig.GetData(currentLevelIndex);
        }

        private void RebuildEnemyProgressBaseline()
        {
            _initialEnemyCount = _field.GetEnemyCount();
            _currentEnemyCount = _initialEnemyCount;

            _ui.InitializeEnemyProgress(_initialEnemyCount);
            if (_currentEnemyCount > 0)
                _ui.UpdateEnemyProgress(_currentEnemyCount);
        }

        private void OnTroopAdded(ITroopPModel troop)
        {
            if (troop.Team != Team.Enemy) return;

            _currentEnemyCount++;
            var initialChanged = false;
            if (_currentEnemyCount > _initialEnemyCount)
            {
                _initialEnemyCount = _currentEnemyCount;
                initialChanged = true;
            }

            PushEnemyProgress(initialChanged);
        }

        private void OnTroopRemoved(ITroopPModel troop)
        {
            if (troop.Team != Team.Enemy) return;

            _currentEnemyCount = Math.Max(0, _currentEnemyCount - 1);
            PushEnemyProgress(false);
        }

        private void PushEnemyProgress(bool initialChanged)
        {
            if (initialChanged)
                _ui.InitializeEnemyProgress(_initialEnemyCount);

            _ui.UpdateEnemyProgress(_currentEnemyCount);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
