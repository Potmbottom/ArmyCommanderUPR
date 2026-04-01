using System.Collections.Generic;
using R3;
using UnityEngine;

namespace ArmyCommander
{
    public interface ITrainingFieldPModel
    {
        IReadOnlyList<Vector3> SlotPositions { get; }
        Observable<Unit> OnOrderGiven { get; }
        bool IsOrderActive { get; }
        bool CanGiveOrder { get; }

        void SetPoints(IEnumerable<Transform> points);
        void SetOrderAvailable(bool value);
        void GiveAttackOrder();
        void ResetOrder();
    }
}
