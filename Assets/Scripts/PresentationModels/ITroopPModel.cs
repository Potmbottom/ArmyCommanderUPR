using System;
using R3;
using UnityEngine;

namespace ArmyCommander
{
    public interface ITroopPModel : IDisposable
    {
        int DataIndex { get; }
        Team Team { get; }
        Vector3 HomePosition { get; }
        AIBehaviour AIBehaviour { get; }
        Vector3 Position { get; }
        Vector3 Velocity { get; }
        Vector3 TargetPosition { get; }
        TroopState State { get; }
        Observable<TroopState> OnStateChanged { get; }
        Observable<float> OnHealthChanged { get; }

        void SetPosition(Vector3 position);
        void SetVelocity(Vector3 velocity);
        void SetTargetPosition(Vector3 position);
        void SetState(TroopState state);
        void SetAIBehaviour(AIBehaviour behaviour);
        void MakeDamage(float damage);
    }
}
