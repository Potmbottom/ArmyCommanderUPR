using UnityEngine;

namespace ArmyCommander
{
    public class ArmyUpgradeControl : BaseControl<IArmyUpgradePModel>
    {
        protected override void OnModelBind(IArmyUpgradePModel model) { }

        private void OnTriggerEnter(Collider other) => Model?.RequestUpgrade();
    }
}
