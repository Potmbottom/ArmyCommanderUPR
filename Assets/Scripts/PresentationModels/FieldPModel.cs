using System.Collections.Generic;
using R3;
using UnityEngine;

namespace ArmyCommander
{
    public class FieldPModel : IFieldPModel
    {
        private readonly List<ITroopPModel> _troops = new();
        private readonly List<IProjectilePModel> _projectiles = new();
        private readonly List<IResourceDropPModel> _resourceDrops = new();

        public IReadOnlyList<ITroopPModel> Troops => _troops;
        public IReadOnlyList<IProjectilePModel> Projectiles => _projectiles;
        public IReadOnlyList<IResourceDropPModel> ResourceDrops => _resourceDrops;

        private readonly Subject<ITroopPModel> _onTroopAdded = new();
        private readonly Subject<ITroopPModel> _onTroopRemoved = new();
        private readonly Subject<IProjectilePModel> _onProjectileAdded = new();
        private readonly Subject<IProjectilePModel> _onProjectileRemoved = new();
        private readonly Subject<IResourceDropPModel> _onResourceDropAdded = new();
        private readonly Subject<IResourceDropPModel> _onResourceDropRemoved = new();

        public Observable<ITroopPModel> OnTroopAdded => _onTroopAdded;
        public Observable<ITroopPModel> OnTroopRemoved => _onTroopRemoved;
        public Observable<IProjectilePModel> OnProjectileAdded => _onProjectileAdded;
        public Observable<IProjectilePModel> OnProjectileRemoved => _onProjectileRemoved;
        public Observable<IResourceDropPModel> OnResourceDropAdded => _onResourceDropAdded;
        public Observable<IResourceDropPModel> OnResourceDropRemoved => _onResourceDropRemoved;

        public ITroopPModel CreateTroop(int dataIndex, Team team, Vector3 position, Vector3 homePosition, float health, AIBehaviour aiBehaviour = AIBehaviour.Home)
        {
            var model = new TroopPModel(dataIndex, team, homePosition, health, aiBehaviour);
            model.SetPosition(position);
            model.SetTargetPosition(homePosition);

            _troops.Add(model);
            _onTroopAdded.OnNext(model);
            return model;
        }

        public void RemoveTroop(ITroopPModel model)
        {
            if (!_troops.Remove(model)) return;
            _onTroopRemoved.OnNext(model);
            model.Dispose();
        }

        public IProjectilePModel CreateProjectile(int dataIndex, Team ownerTeam, Vector3 position, Vector3 targetPosition)
        {
            var model = new ProjectilePModel(dataIndex, ownerTeam, position, targetPosition);
            _projectiles.Add(model);
            _onProjectileAdded.OnNext(model);
            return model;
        }

        public void RemoveProjectile(IProjectilePModel model)
        {
            if (!_projectiles.Remove(model)) return;
            _onProjectileRemoved.OnNext(model);
            model.Dispose();
        }

        public IResourceDropPModel CreateResourceDrop(int dataIndex, ResourceType resourceType, int amount, Vector3 position)
        {
            var model = new ResourceDropPModel(dataIndex, resourceType, amount, position);
            _resourceDrops.Add(model);
            _onResourceDropAdded.OnNext(model);
            return model;
        }

        public void RemoveResourceDrop(IResourceDropPModel model)
        {
            if (!_resourceDrops.Remove(model)) return;
            _onResourceDropRemoved.OnNext(model);
            model.Dispose();
        }

        public int GetAlliedCount()
        {
            int count = 0;
            foreach (var t in _troops)
                if (t.Team == Team.Allied) count++;
            return count;
        }

        public int GetEnemyCount()
        {
            int count = 0;
            foreach (var t in _troops)
                if (t.Team == Team.Enemy) count++;
            return count;
        }
    }
}
