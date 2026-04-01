using System.Collections.Generic;
using R3;
using UnityEngine;

namespace ArmyCommander
{
    public class TrainingFieldPModel : ITrainingFieldPModel
    {
        private readonly List<Vector3> _slotPositions = new();
        public IReadOnlyList<Vector3> SlotPositions => _slotPositions;

        private readonly Subject<Unit> _onOrderGiven = new();
        public Observable<Unit> OnOrderGiven => _onOrderGiven;

        private bool _isOrderActive;
        public bool IsOrderActive => _isOrderActive;
        private bool _canGiveOrder;
        public bool CanGiveOrder => _canGiveOrder;

        public void SetPoints(IEnumerable<Transform> points)
        {
            _slotPositions.Clear();
            foreach (var t in points)
                _slotPositions.Add(t.position);
        }

        public void SetOrderAvailable(bool value) => _canGiveOrder = value;

        public void GiveAttackOrder()
        {
            if (_isOrderActive || !_canGiveOrder) return;
            _isOrderActive = true;
            _onOrderGiven.OnNext(Unit.Default);
        }

        public void ResetOrder() => _isOrderActive = false;
    }
}
