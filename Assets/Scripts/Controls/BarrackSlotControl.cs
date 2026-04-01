using R3;
using UnityEngine;

namespace ArmyCommander
{
    public class BarrackSlotControl : BaseControl<IBarrackSlotPModel>
    {
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private GameObject _soldierVisual;
        [SerializeField] private GameObject _veteranVisual;
        [SerializeField] private GameObject _masterVisual;

        protected override void OnModelBind(IBarrackSlotPModel model)
        {
            var spawnPosition = _spawnPoint != null ? _spawnPoint.position : transform.position;
            model.SetBuildPoint(spawnPosition);

            model.TroopType
                .Subscribe(SetVisualByType)
                .AddTo(Disposables);
        }

        private void SetVisualByType(TroopType type)
        {
            SetVisualActive(_soldierVisual, type == TroopType.Soldier);
            SetVisualActive(_veteranVisual, type == TroopType.Veteran);
            SetVisualActive(_masterVisual, type == TroopType.Master);
        }

        private static void SetVisualActive(GameObject target, bool isActive)
        {
            if (target != null)
                target.SetActive(isActive);
        }

        private void OnTriggerEnter(Collider other) => Model?.SetPlayerInZone(true);
        private void OnTriggerExit(Collider other) => Model?.SetPlayerInZone(false);
    }
}
