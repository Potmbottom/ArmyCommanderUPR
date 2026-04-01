using R3;
using UnityEngine;

namespace ArmyCommander
{
    public class ProjectileControl : BaseControl<IProjectilePModel>
    {
        private const float DirectionEpsilonSqr = 0.000001f;

        [SerializeField] private Animator _animator;

        protected override void OnModelBind(IProjectilePModel model)
        {
            model.OnStateChanged
                .Subscribe(state => _animator.SetInteger("State", (int)state))
                .AddTo(Disposables);
        }

        public void Tick()
        {
            if (Model == null) return;
            transform.position = Model.Position;

            var planarDirection = Model.Direction;
            planarDirection.y = 0f;
            if (planarDirection.sqrMagnitude <= DirectionEpsilonSqr) return;

            transform.rotation = Quaternion.LookRotation(planarDirection, Vector3.up);
        }
    }
}
