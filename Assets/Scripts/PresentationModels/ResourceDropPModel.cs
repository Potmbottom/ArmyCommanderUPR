using R3;
using UnityEngine;

namespace ArmyCommander
{
    public class ResourceDropPModel : IResourceDropPModel
    {
        public int DataIndex { get; }
        public ResourceType ResourceType { get; }
        public int Amount { get; }
        public Vector3 Position { get; }

        private bool _isCollected;
        public bool IsCollected => _isCollected;

        private readonly Subject<IResourceDropPModel> _onCollected = new();
        public Observable<IResourceDropPModel> OnCollected => _onCollected;

        public ResourceDropPModel(int dataIndex, ResourceType resourceType, int amount, Vector3 position)
        {
            DataIndex = dataIndex;
            ResourceType = resourceType;
            Amount = amount;
            Position = position;
        }

        public void Collect()
        {
            if (_isCollected) return;
            _isCollected = true;
            _onCollected.OnNext(this);
        }

        public void Dispose() => _onCollected.Dispose();
    }
}
