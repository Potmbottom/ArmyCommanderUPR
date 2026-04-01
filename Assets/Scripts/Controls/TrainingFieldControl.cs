using System.Collections.Generic;
using UnityEngine;

namespace ArmyCommander
{
    public class TrainingFieldControl : BaseControl<ITrainingFieldPModel>
    {
        [SerializeField] private List<Transform> _slotPoints;

        protected override void OnModelBind(ITrainingFieldPModel model)
        {
            model.SetPoints(_slotPoints);
        }

        internal void OnOrderTriggerEnter(Collider other) => Model?.GiveAttackOrder();
    }
}
