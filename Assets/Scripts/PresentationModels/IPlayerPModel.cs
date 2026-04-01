using R3;
using UnityEngine;

namespace ArmyCommander
{
    public interface IPlayerPModel
    {
        Vector2 MoveDirection { get; }
        Vector3 Position { get; }
        bool IsDead { get; }
        Observable<Unit> OnDead { get; }

        void SetMoveDirection(Vector2 direction);
        void SetPosition(Vector3 position);
        void MakeDamage(float damage);
    }
}
