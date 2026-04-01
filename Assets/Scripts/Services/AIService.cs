using System;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    public class AIService : ITickable, IInitializable, IDisposable
    {
        private const float HomeArrivalDistance = 0.15f;
        private const float HomeArrivalDistanceSqr = HomeArrivalDistance * HomeArrivalDistance;
        private const float HomeKeepIdleDistance = 0.2f;
        private const float HomeKeepIdleDistanceSqr = HomeKeepIdleDistance * HomeKeepIdleDistance;

        private IFieldPModel _field;
        private ITrainingFieldPModel _trainingField;
        private IPlayerPModel _player;
        private TroopsConfig _config;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void SetDependency(IFieldPModel field, ITrainingFieldPModel trainingField, IPlayerPModel player, TroopsConfig config)
        {
            _field = field;
            _trainingField = trainingField;
            _player = player;
            _config = config;
        }

        public void Initialize()
        {
            _trainingField.OnOrderGiven
                .Subscribe(_ => OnOrderGiven())
                .AddTo(_disposables);
        }

        private void OnOrderGiven()
        {
            foreach (var troop in _field.Troops)
            {
                if (troop.Team == Team.Allied)
                    troop.SetAIBehaviour(AIBehaviour.Aggressive);
            }
        }

        public void Tick()
        {
            foreach (var troop in _field.Troops)
            {
                if (troop.State == TroopState.Dead) continue;

                if (troop.AIBehaviour == AIBehaviour.Home)
                    TickHome(troop);
                else
                    TickAggressive(troop);
            }
        }

        private void TickHome(ITroopPModel troop)
        {
            var toHome = troop.HomePosition - troop.Position;
            toHome.y = 0f;
            var toHomeSqr = toHome.sqrMagnitude;

            if (troop.State == TroopState.Idle && toHomeSqr <= HomeKeepIdleDistanceSqr)
            {
                troop.SetTargetPosition(troop.HomePosition);
                return;
            }

            if (toHomeSqr < HomeArrivalDistanceSqr)
            {
                troop.SetState(TroopState.Idle);
                troop.SetTargetPosition(troop.HomePosition);
                return;
            }

            troop.SetState(TroopState.Move);
            troop.SetTargetPosition(troop.HomePosition);
        }

        private void TickAggressive(ITroopPModel troop)
        {
            var data = _config.GetData(troop.DataIndex);
            if (!TryFindNearestTarget(troop, out var targetPosition, out var targetDistSqr))
            {
                TickHome(troop);
                return;
            }

            var attackRangeSqr = data.AttackRange * data.AttackRange;
            var aggressiveRangeSqr = data.AggressiveRange * data.AggressiveRange;

            if (targetDistSqr <= attackRangeSqr)
            {
                troop.SetState(TroopState.Attack);
                troop.SetTargetPosition(targetPosition);
            }
            else if (targetDistSqr <= aggressiveRangeSqr)
            {
                troop.SetState(TroopState.Move);
                troop.SetTargetPosition(targetPosition);
            }
            else
            {
                TickHome(troop);
            }
        }

        private bool TryFindNearestTarget(ITroopPModel troop, out Vector3 targetPosition, out float targetDistSqr)
        {
            targetPosition = default;
            targetDistSqr = float.MaxValue;

            foreach (var other in _field.Troops)
            {
                if (other.Team == troop.Team) continue;
                if (other.State == TroopState.Dead) continue;

                var distSqr = DistanceXZSqr(troop.Position, other.Position);
                if (distSqr < targetDistSqr)
                {
                    targetDistSqr = distSqr;
                    targetPosition = other.Position;
                }
            }

            if (troop.Team == Team.Enemy && !_player.IsDead)
            {
                var playerDistSqr = DistanceXZSqr(troop.Position, _player.Position);
                if (playerDistSqr < targetDistSqr)
                {
                    targetDistSqr = playerDistSqr;
                    targetPosition = _player.Position;
                }
            }

            return targetDistSqr < float.MaxValue;
        }

        private static float DistanceXZSqr(Vector3 a, Vector3 b)
        {
            var delta = a - b;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }

        public void Dispose() => _disposables.Dispose();
    }
}
