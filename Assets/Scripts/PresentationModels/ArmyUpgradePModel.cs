using R3;
using UnityEngine;

namespace ArmyCommander
{
    public class ArmyUpgradePModel : IArmyUpgradePModel
    {
        private ArmyLevel _currentLevel = ArmyLevel.Level1;
        public ArmyLevel CurrentLevel => _currentLevel;

        private readonly Subject<Unit> _onUpgradeRequested = new();
        public Observable<Unit> OnUpgradeRequested => _onUpgradeRequested;

        private readonly Subject<ArmyLevel> _onUpgraded = new();
        public Observable<ArmyLevel> OnUpgraded => _onUpgraded;

        public void RequestUpgrade() => _onUpgradeRequested.OnNext(Unit.Default);

        public void Upgrade()
        {
            if (_currentLevel == ArmyLevel.Level3) return;
            _currentLevel = (ArmyLevel)((int)_currentLevel + 1);
            _onUpgraded.OnNext(_currentLevel);
        }

        public void SetLevel(ArmyLevel level)
        {
            var clampedLevel = (ArmyLevel)Mathf.Clamp((int)level, (int)ArmyLevel.Level1, (int)ArmyLevel.Level3);
            _currentLevel = clampedLevel;
        }
    }
}
