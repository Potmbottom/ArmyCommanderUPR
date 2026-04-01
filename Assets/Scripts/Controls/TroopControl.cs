using R3;
using UnityEngine;

namespace ArmyCommander
{
    public class TroopControl : BaseControl<ITroopPModel>
    {
        private const float RotationEpsilonSqr = 0.0001f;
        private const int AnimatorStateIdle = 0;
        private const int AnimatorStateRun = 1;
        private const int AnimatorStateAttack = 2;
        private const int AnimatorStateDeath = 3;

        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private ParticleSystem _vfx;
        [SerializeField] private ParticleSystem _appearVfx;
        
        private int _currentAnimatorState = -1;
        private Collider[] _colliders;
        private bool _deadStateApplied;

        protected override void OnModelBind(ITroopPModel model)
        {
            _deadStateApplied = false;
            _rigidbody.constraints |= RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
            _rigidbody.detectCollisions = true;
            _rigidbody.position = model.Position;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            SetCollidersEnabled(true);
            _currentAnimatorState = -1;
            SetAnimatorState(MapState(model.State));

            if (_vfx != null)
                _vfx.gameObject.SetActive(false);
            if (_appearVfx != null)
                _appearVfx.gameObject.SetActive(true);
            model.OnStateChanged
                .Subscribe(OnStateChanged)
                .AddTo(Disposables);
            model.OnHealthChanged.Skip(1).Subscribe(_ =>
            {
                if (_vfx == null) return;
                _vfx.gameObject.SetActive(true);
                _vfx.Play(true);
            }).AddTo(Disposables);
        }

        public void FixedTick()
        {
            if (Model == null) return;
            SetAnimatorState(MapState(Model.State));
            if (Model.State == TroopState.Dead) return;
            Model.SetPosition(transform.position);
            var velocity = Model.Velocity;
            _rigidbody.velocity = velocity;
            ApplyInstantYawRotation(ResolveFacingDirection(velocity));
        }

        private void OnStateChanged(TroopState state)
        {
            SetAnimatorState(MapState(state));
            if (state == TroopState.Dead)
            {
                ApplyDeadState();
            }
        }

        private void ApplyDeadState()
        {
            if (_deadStateApplied) return;
            _deadStateApplied = true;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.Sleep();
            _rigidbody.detectCollisions = false;
            SetCollidersEnabled(false);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            _colliders ??= GetComponentsInChildren<Collider>(true);
            foreach (var c in _colliders)
                c.enabled = enabled;
        }

        private void ApplyInstantYawRotation(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < RotationEpsilonSqr) return;

            var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            _rigidbody.MoveRotation(targetRotation);
        }

        private Vector3 ResolveFacingDirection(Vector3 velocity)
        {
            if (Model.State != TroopState.Attack)
                return velocity;

            var toTarget = Model.TargetPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude >= RotationEpsilonSqr)
                return toTarget;

            return velocity;
        }

        private static int MapState(TroopState state)
        {
            return state switch
            {
                TroopState.Idle => AnimatorStateIdle,
                TroopState.Move => AnimatorStateRun,
                TroopState.Attack => AnimatorStateAttack,
                TroopState.Dead => AnimatorStateDeath,
                _ => AnimatorStateIdle
            };
        }

        private void SetAnimatorState(int state)
        {
            if (_animator == null) return;
            if (_currentAnimatorState == state) return;
            _currentAnimatorState = state;
            _animator.SetInteger("State", state);
        }
    }
}
