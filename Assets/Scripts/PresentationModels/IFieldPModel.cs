using System.Collections.Generic;
using R3;
using UnityEngine;

namespace ArmyCommander
{
    public interface IFieldPModel
    {
        IReadOnlyList<ITroopPModel> Troops { get; }
        IReadOnlyList<IProjectilePModel> Projectiles { get; }
        IReadOnlyList<IResourceDropPModel> ResourceDrops { get; }
        Observable<ITroopPModel> OnTroopAdded { get; }
        Observable<ITroopPModel> OnTroopRemoved { get; }
        Observable<IProjectilePModel> OnProjectileAdded { get; }
        Observable<IProjectilePModel> OnProjectileRemoved { get; }
        Observable<IResourceDropPModel> OnResourceDropAdded { get; }
        Observable<IResourceDropPModel> OnResourceDropRemoved { get; }

        ITroopPModel CreateTroop(int dataIndex, Team team, Vector3 position, Vector3 homePosition, float health, AIBehaviour aiBehaviour = AIBehaviour.Home);
        void RemoveTroop(ITroopPModel model);
        IProjectilePModel CreateProjectile(int dataIndex, Team ownerTeam, Vector3 position, Vector3 targetPosition);
        void RemoveProjectile(IProjectilePModel model);
        IResourceDropPModel CreateResourceDrop(int dataIndex, ResourceType resourceType, int amount, Vector3 position);
        void RemoveResourceDrop(IResourceDropPModel model);
        int GetAlliedCount();
        int GetEnemyCount();
    }
}
