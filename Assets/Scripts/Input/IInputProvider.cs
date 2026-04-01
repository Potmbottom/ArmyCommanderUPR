using UnityEngine;

namespace ArmyCommander
{
    public interface IInputProvider
    {
        Vector2 GetMoveDirection();
    }
}
