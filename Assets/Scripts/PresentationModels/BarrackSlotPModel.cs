using R3;
using UnityEngine;

namespace ArmyCommander
{
    public class BarrackSlotPModel : IBarrackSlotPModel
    {
        private Vector3 _buildPoint;
        public Vector3 BuildPoint => _buildPoint;

        private readonly ReactiveProperty<TroopType> _troopType = new(ArmyCommander.TroopType.Empty);
        public ReadOnlyReactiveProperty<TroopType> TroopType => _troopType;

        private readonly ReactiveProperty<bool> _isPlayerInZone = new(false);
        public ReadOnlyReactiveProperty<bool> IsPlayerInZone => _isPlayerInZone;

        public void SetBuildPoint(Vector3 point) => _buildPoint = point;
        public void SetTroopType(TroopType type) => _troopType.Value = type;
        public void SetPlayerInZone(bool value) => _isPlayerInZone.Value = value;

        public void Dispose()
        {
            _troopType.Dispose();
            _isPlayerInZone.Dispose();
        }
    }
}
