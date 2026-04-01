using System;
using R3;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    public class ArmyUpgradeService : IInitializable, IDisposable
    {
        private IArmyUpgradePModel _armyUpgrade;
        private IResourcePModel _resources;
        private IUIModel _ui;
        private LevelConfig _levelConfig;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void SetDependency(IArmyUpgradePModel armyUpgrade, IResourcePModel resources, IUIModel ui, LevelConfig levelConfig)
        {
            _armyUpgrade = armyUpgrade;
            _resources = resources;
            _ui = ui;
            _levelConfig = levelConfig;
        }

        public void Initialize()
        {
            _armyUpgrade.OnUpgradeRequested
                .Subscribe(_ =>
                {
                    var currentLevelIndex = Math.Max(0, (int)_armyUpgrade.CurrentLevel - 1);
                    TryUpgrade(currentLevelIndex);
                })
                .AddTo(_disposables);

            _armyUpgrade.OnUpgraded
                .Subscribe(OnUpgraded)
                .AddTo(_disposables);
        }

        private void TryUpgrade(int currentLevelIndex)
        {
            if (_armyUpgrade.CurrentLevel == ArmyLevel.Level3) return;
            
            var levelData = _levelConfig.GetData(currentLevelIndex);
            if (!_resources.TrySpendGold(levelData.UpgradeCostGold)) return;
            
            _armyUpgrade.Upgrade();
        }

        private void OnUpgraded(ArmyLevel newLevel)
        {
            _ui.ShowNextLevelPopup();
        }

        public void Dispose() => _disposables.Dispose();
    }
}
