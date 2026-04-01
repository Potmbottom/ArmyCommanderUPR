using UnityEngine;

namespace ArmyCommander
{
    public class PCInputProvider : IInputProvider
    {
        public Vector2 GetMoveDirection()
        {
            return new Vector2(
                Input.GetAxis("Horizontal"),
                Input.GetAxis("Vertical")
            );
        }
    }
}
