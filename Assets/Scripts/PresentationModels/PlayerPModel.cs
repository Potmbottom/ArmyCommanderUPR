using R3;
using UnityEngine;

namespace ArmyCommander
{
    public class PlayerPModel : IPlayerPModel
    {
        private const float InitialHealth = 100f;

        private Vector2 _moveDirection;
        public Vector2 MoveDirection => _moveDirection;
        private Vector3 _position;
        public Vector3 Position => _position;
        private float _currentHealth = InitialHealth;
        public bool IsDead => _currentHealth <= 0f;
        private readonly Subject<Unit> _onDead = new();
        public Observable<Unit> OnDead => _onDead;

        public void SetMoveDirection(Vector2 direction) => _moveDirection = direction;
        public void SetPosition(Vector3 position) => _position = position;

        public void MakeDamage(float damage)
        {
            if (IsDead) return;
            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            if (IsDead)
                _onDead.OnNext(Unit.Default);
        }
    }
}
