using System;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    public class InputService : ITickable, IDisposable
    {
        private IPlayerPModel _player;
        private IInputProvider _inputProvider;

        [Inject]
        public void SetDependency(IPlayerPModel player, IInputProvider inputProvider)
        {
            _player = player;
            _inputProvider = inputProvider;
        }

        public void Tick() => _player.SetMoveDirection(_inputProvider.GetMoveDirection());

        public void Dispose() { }
    }
}
