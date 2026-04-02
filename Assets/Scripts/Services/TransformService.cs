using System;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    public class TransformService : ITickable, IDisposable
    {
        private const float TargetReachedEpsilon = 0.01f;
        private const float TargetReachedEpsilonSqr = TargetReachedEpsilon * TargetReachedEpsilon;

        private IFieldPModel _field;
        private TroopsConfig _config;
        private ProjectileConfig _projectileConfig;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void SetDependency(IFieldPModel field, TroopsConfig config, ProjectileConfig projectileConfig)
        {
            _field = field;
            _config = config;
            _projectileConfig = projectileConfig;
        }

        public void Tick()
        {
            TickTroops();
            TickProjectiles();
        }

        private void TickTroops()
        {
            foreach (var troop in _field.Troops)
            {
                if (troop.State == TroopState.Dead
                    || troop.State == TroopState.Idle
                    || troop.State == TroopState.Attack)
                {
                    troop.SetVelocity(Vector3.zero);
                    continue;
                }

                var data = _config.GetData(troop.DataIndex);
                var dir = (troop.TargetPosition - troop.Position);
                dir.y = 0f;

                if (dir.sqrMagnitude < TargetReachedEpsilonSqr)
                {
                    troop.SetVelocity(Vector3.zero);
                    continue;
                }

                troop.SetVelocity(dir.normalized * data.MoveSpeed);
            }
        }

        private void TickProjectiles()
        {
            foreach (var projectile in _field.Projectiles)
            {
                if (projectile.State != ProjectileState.Active)
                    continue;

                var data = _projectileConfig.GetData(projectile.DataIndex);
                MoveProjectile(projectile, data);
            }
        }

        private static void MoveProjectile(IProjectilePModel projectile, ProjectileDataModel data)
        {
            var step = data.MoveSpeed * Time.deltaTime;
            projectile.SetPosition(projectile.Position + projectile.Direction * step);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
