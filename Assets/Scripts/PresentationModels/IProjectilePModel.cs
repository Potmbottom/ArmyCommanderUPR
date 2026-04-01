using System;
using R3;
using UnityEngine;

namespace ArmyCommander
{
    public interface IProjectilePModel : IDisposable
    {
        int DataIndex { get; }
        Team OwnerTeam { get; }
        Vector3 TargetPosition { get; }
        Vector3 Direction { get; }
        Vector3 Position { get; }
        ProjectileState State { get; }
        Observable<ProjectileState> OnStateChanged { get; }

        void SetPosition(Vector3 position);
        void SetState(ProjectileState state);
    }
}
