using UnityEngine;

namespace ArmyCommander
{
    public class PlayerControl : BaseControl<IPlayerPModel>
    {
        private const float RotationEpsilonSqr = 0.0001f;
        private const int AnimatorStateIdle = 0;
        private const int AnimatorStateRun = 1;
        private const int AnimatorStateDeath = 2;

        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 10f, -8f);
        [SerializeField] private float _cameraFollowSmooth = 8f;

        private Vector3 _cameraFollowVelocity;
        private int _currentAnimatorState = -1;

        protected override void OnModelBind(IPlayerPModel model)
        {
            _rigidbody.constraints |= RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _currentAnimatorState = -1;
            TryResolveCameraTransform();
            model.SetPosition(_rigidbody.position);
        }

        internal void FixedTick()
        {
            if (Model == null) return;

            if (Model.IsDead)
            {
                _rigidbody.velocity = Vector3.zero;
                SetAnimatorState(AnimatorStateDeath);
                Model.SetPosition(_rigidbody.position);
                return;
            }

            var direction = Model.MoveDirection;
            _rigidbody.velocity = new Vector3(direction.x * _moveSpeed, _rigidbody.velocity.y, direction.y * _moveSpeed);
            ApplyInstantYawRotation(direction);
            SetAnimatorState(direction.sqrMagnitude < RotationEpsilonSqr ? AnimatorStateIdle : AnimatorStateRun);
            Model.SetPosition(_rigidbody.position);
        }

        internal void LateTick()
        {
            if (Model == null) return;
            TryResolveCameraTransform();
            if (_cameraTransform == null) return;

            var targetPosition = _rigidbody.position + _cameraOffset;
            _cameraTransform.position = Vector3.SmoothDamp(
                _cameraTransform.position,
                targetPosition,
                ref _cameraFollowVelocity,
                1f / Mathf.Max(0.01f, _cameraFollowSmooth));
        }

        private void TryResolveCameraTransform()
        {
            if (_cameraTransform != null) return;
            if (Camera.main == null) return;
            _cameraTransform = Camera.main.transform;
        }

        private void ApplyInstantYawRotation(Vector2 direction)
        {
            var direction3D = new Vector3(direction.x, 0f, direction.y);
            if (direction3D.sqrMagnitude < RotationEpsilonSqr) return;

            var targetRotation = Quaternion.LookRotation(direction3D.normalized, Vector3.up);
            _rigidbody.MoveRotation(targetRotation);
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
