using R3;

namespace ArmyCommander
{
    public interface IArmyUpgradePModel
    {
        ArmyLevel CurrentLevel { get; }
        Observable<Unit> OnUpgradeRequested { get; }
        Observable<ArmyLevel> OnUpgraded { get; }

        void RequestUpgrade();
        void Upgrade();
        void SetLevel(ArmyLevel level);
    }
}
