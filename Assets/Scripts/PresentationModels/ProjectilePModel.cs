using R3;
using UnityEngine;

namespace ArmyCommander
{
    public class ProjectilePModel : IProjectilePModel
    {
        private const float DirectionEpsilonSqr = 0.000001f;

        public int DataIndex { get; }
        public Team OwnerTeam { get; }
        public Vector3 TargetPosition { get; }
        public Vector3 Direction { get; }

        private Vector3 _position;
        public Vector3 Position => _position;

        private readonly ReactiveProperty<ProjectileState> _state = new(ProjectileState.Active);
        public ProjectileState State => _state.Value;
        public Observable<ProjectileState> OnStateChanged => _state;

        public ProjectilePModel(int dataIndex, Team ownerTeam, Vector3 position, Vector3 targetPosition)
        {
            DataIndex = dataIndex;
            OwnerTeam = ownerTeam;
            _position = position;
            TargetPosition = targetPosition;

            var toTarget = targetPosition - position;
            Direction = toTarget.sqrMagnitude <= DirectionEpsilonSqr
                ? Vector3.zero
                : toTarget.normalized;
        }

        public void SetPosition(Vector3 position) => _position = position;
        public void SetState(ProjectileState state) => _state.Value = state;

        public void Dispose() => _state.Dispose();
    }
}
