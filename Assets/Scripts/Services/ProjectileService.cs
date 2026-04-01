using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    // TODO: Replace team-list broad phase with a reusable uniform-grid spatial index when unit/projectile counts grow further.
    public class ProjectileService : ITickable, IInitializable, IDisposable
    {
        private IFieldPModel _field;
        private IPlayerPModel _player;
        private TroopsConfig _troopsConfig;
        private ProjectileConfig _projectileConfig;

        private readonly CompositeDisposable _disposables = new();
        private readonly Dictionary<ITroopPModel, CompositeDisposable> _troopSubs = new();
        private readonly Dictionary<ITroopPModel, float> _attackTimers = new();
        private readonly List<ITroopPModel> _attackTimerKeysCache = new();
        private readonly Dictionary<IProjectilePModel, float> _projectileLifeTimers = new();
        private readonly List<IProjectilePModel> _toRemove = new();
        private readonly List<ITroopPModel> _aliveAlliedTroops = new();
        private readonly List<ITroopPModel> _aliveEnemyTroops = new();

        [Inject]
        public void SetDependency(IFieldPModel field, IPlayerPModel player, TroopsConfig troopsConfig, ProjectileConfig projectileConfig)
        {
            _field = field;
            _player = player;
            _troopsConfig = troopsConfig;
            _projectileConfig = projectileConfig;
        }

        public void Initialize()
        {
            _field.OnTroopAdded.Subscribe(OnTroopAdded).AddTo(_disposables);
            _field.OnTroopRemoved.Subscribe(OnTroopRemoved).AddTo(_disposables);
        }

        private void OnTroopAdded(ITroopPModel troop)
        {
            var subs = new CompositeDisposable();
            troop.OnStateChanged
                .Subscribe(state => OnTroopStateChanged(troop, state))
                .AddTo(subs);
            _troopSubs[troop] = subs;

            if (troop.Team == Team.Allied)
                _aliveAlliedTroops.Add(troop);
            else
                _aliveEnemyTroops.Add(troop);
        }

        private void OnTroopRemoved(ITroopPModel troop)
        {
            if (_troopSubs.TryGetValue(troop, out var subs))
            {
                subs.Dispose();
                _troopSubs.Remove(troop);
            }
            _attackTimers.Remove(troop);

            if (troop.Team == Team.Allied)
                _aliveAlliedTroops.Remove(troop);
            else
                _aliveEnemyTroops.Remove(troop);
        }

        private void OnTroopStateChanged(ITroopPModel troop, TroopState state)
        {
            if (state != TroopState.Attack)
                _attackTimers.Remove(troop);
            else if (!_attackTimers.ContainsKey(troop))
                _attackTimers[troop] = 0f;
        }

        public void Tick()
        {
            TickAttackTimers();
            TickProjectiles();
        }

        private void TickAttackTimers()
        {
            _attackTimerKeysCache.Clear();
            foreach (var troop in _attackTimers.Keys)
                _attackTimerKeysCache.Add(troop);

            foreach (var troop in _attackTimerKeysCache)
            {
                if (!_attackTimers.TryGetValue(troop, out var elapsed))
                    continue;

                if (troop.State != TroopState.Attack)
                {
                    _attackTimers.Remove(troop);
                    continue;
                }

                elapsed += Time.deltaTime;
                var data = _troopsConfig.GetData(troop.DataIndex);
                if (elapsed < 1f / data.AttackSpeed)
                {
                    _attackTimers[troop] = elapsed;
                    continue;
                }

                _attackTimers[troop] = 0f;
                FireProjectile(troop, data);
            }
        }

        private void FireProjectile(ITroopPModel troop, TroopDataModel troopData)
        {
            if (!TryFindTargetPosition(troop, out var targetPosition)) return;

            _field.CreateProjectile(
                troopData.ProjectileIndex,
                troop.Team,
                troop.Position,
                targetPosition
            );
        }

        private void TickProjectiles()
        {
            _toRemove.Clear();

            foreach (var projectile in _field.Projectiles)
            {
                if (projectile.State == ProjectileState.Destroyed) continue;

                _projectileLifeTimers.TryAdd(projectile, 0f);
                var projData = _projectileConfig.GetData(projectile.DataIndex);
                MoveProjectile(projectile, projData);
                var troopHit = CheckTroopCollision(projectile, projData.ColliderRadius);
                if (troopHit != null)
                {
                    troopHit.MakeDamage(projData.Damage);
                    projectile.SetState(ProjectileState.Destroyed);
                    _toRemove.Add(projectile);
                    continue;
                }

                if (CheckPlayerCollision(projectile, projData.ColliderRadius))
                {
                    _player.MakeDamage(projData.Damage);
                    projectile.SetState(ProjectileState.Destroyed);
                    _toRemove.Add(projectile);
                    continue;
                }

                _projectileLifeTimers[projectile] += Time.deltaTime;
                if (_projectileLifeTimers[projectile] >= projData.LifeTime)
                {
                    projectile.SetState(ProjectileState.Destroyed);
                    _toRemove.Add(projectile);
                }
            }

            foreach (var p in _toRemove)
            {
                _field.RemoveProjectile(p);
                _projectileLifeTimers.Remove(p);
            }
        }

        private static void MoveProjectile(IProjectilePModel projectile, ProjectileDataModel data)
        {
            var step = data.MoveSpeed * Time.deltaTime;
            projectile.SetPosition(projectile.Position + projectile.Direction * step);
        }

        private ITroopPModel CheckTroopCollision(IProjectilePModel projectile, float colliderRadius)
        {
            var radiusSqr = colliderRadius * colliderRadius;
            var candidates = GetOppositeTeamTroops(projectile.OwnerTeam);
            foreach (var troop in candidates)
            {
                if (troop.State == TroopState.Dead) continue;

                var delta = projectile.Position - troop.Position;
                if (Mathf.Abs(delta.x) > colliderRadius || Mathf.Abs(delta.y) > colliderRadius || Mathf.Abs(delta.z) > colliderRadius)
                    continue;

                if (delta.sqrMagnitude <= radiusSqr)
                    return troop;
            }
            return null;
        }

        private bool CheckPlayerCollision(IProjectilePModel projectile, float colliderRadius)
        {
            if (projectile.OwnerTeam != Team.Enemy) return false;
            if (_player.IsDead) return false;

            var delta = projectile.Position - _player.Position;
            if (Mathf.Abs(delta.x) > colliderRadius || Mathf.Abs(delta.y) > colliderRadius || Mathf.Abs(delta.z) > colliderRadius)
                return false;

            return delta.sqrMagnitude <= colliderRadius * colliderRadius;
        }

        private bool TryFindTargetPosition(ITroopPModel troop, out Vector3 targetPosition)
        {
            targetPosition = default;
            float nearestDistSqr = float.MaxValue;

            var candidates = GetOppositeTeamTroops(troop.Team);
            foreach (var other in candidates)
            {
                if (other.State == TroopState.Dead) continue;

                var delta = troop.Position - other.Position;
                var distSqr = delta.sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    targetPosition = other.Position;
                }
            }

            if (troop.Team == Team.Enemy && !_player.IsDead)
            {
                var playerDelta = troop.Position - _player.Position;
                var playerDistSqr = playerDelta.sqrMagnitude;
                if (playerDistSqr < nearestDistSqr)
                {
                    nearestDistSqr = playerDistSqr;
                    targetPosition = _player.Position;
                }
            }

            return nearestDistSqr < float.MaxValue;
        }

        private IReadOnlyList<ITroopPModel> GetOppositeTeamTroops(Team ownerTeam)
        {
            return ownerTeam == Team.Allied ? _aliveEnemyTroops : _aliveAlliedTroops;
        }

        public void Dispose()
        {
            _disposables.Dispose();
            foreach (var subs in _troopSubs.Values) subs.Dispose();
            _projectileLifeTimers.Clear();
            _aliveAlliedTroops.Clear();
            _aliveEnemyTroops.Clear();
        }
    }
}
