using UnityEngine;
using VContainer;

namespace ArmyCommander
{
    public class AndroidInputProvider : IInputProvider
    {
        private VirtualJoystick _joystick;

        [Inject]
        public void SetDependency(VirtualJoystick joystick) => _joystick = joystick;

        public Vector2 GetMoveDirection() => _joystick.Direction;
    }
}
