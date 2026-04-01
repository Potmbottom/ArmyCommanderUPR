using R3;
using UnityEngine;

namespace ArmyCommander
{
    public class TroopPModel : ITroopPModel
    {
        public int DataIndex { get; }
        public Team Team { get; }
        public Vector3 HomePosition { get; }

        private AIBehaviour _aiBehaviour;
        public AIBehaviour AIBehaviour => _aiBehaviour;

        private Vector3 _position;
        public Vector3 Position => _position;

        private Vector3 _velocity;
        public Vector3 Velocity => _velocity;

        private Vector3 _targetPosition;
        public Vector3 TargetPosition => _targetPosition;

        private readonly ReactiveProperty<TroopState> _state = new(TroopState.Idle);
        public TroopState State => _state.Value;
        public Observable<TroopState> OnStateChanged => _state;

        private readonly ReactiveProperty<float> _health;
        public Observable<float> OnHealthChanged => _health;

        public TroopPModel(int dataIndex, Team team, Vector3 homePosition, float health, AIBehaviour aiBehaviour = AIBehaviour.Home)
        {
            DataIndex = dataIndex;
            Team = team;
            HomePosition = homePosition;
            _health = new ReactiveProperty<float>(health);
            _aiBehaviour = aiBehaviour;
        }

        public void SetPosition(Vector3 position) => _position = position;
        public void SetVelocity(Vector3 velocity) => _velocity = velocity;
        public void SetTargetPosition(Vector3 position) => _targetPosition = position;

        public void SetState(TroopState state)
        {
            if (_state.Value == TroopState.Dead) return;
            _state.Value = state;
        }

        public void SetAIBehaviour(AIBehaviour behaviour)
        {
            if (_state.Value == TroopState.Dead) return;
            _aiBehaviour = behaviour;
        }

        public void MakeDamage(float damage)
        {
            if (_state.Value == TroopState.Dead) return;
            _health.Value = Mathf.Max(0f, _health.Value - damage);
            if (_health.Value <= 0f)
                _state.Value = TroopState.Dead;
        }

        public void Dispose()
        {
            _health.Dispose();
            _state.Dispose();
        }
    }
}
