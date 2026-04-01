using System;
using System.Collections.Generic;
using R3;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    public class ResourceService : IInitializable, IDisposable
    {
        private IFieldPModel _field;
        private IResourcePModel _resources;
        private ResourceDropConfig _resourceDropConfig;

        private readonly CompositeDisposable _disposables = new();
        private readonly Dictionary<IResourceDropPModel, IDisposable> _dropSubscriptions = new();

        [Inject]
        public void SetDependency(IFieldPModel field, IResourcePModel resources, ResourceDropConfig resourceDropConfig)
        {
            _field = field;
            _resources = resources;
            _resourceDropConfig = resourceDropConfig;
        }

        public void Initialize()
        {
            _field.OnTroopRemoved
                .Subscribe(OnTroopRemoved)
                .AddTo(_disposables);

            _field.OnResourceDropAdded
                .Subscribe(OnResourceDropAdded)
                .AddTo(_disposables);

            _field.OnResourceDropRemoved
                .Subscribe(OnResourceDropRemoved)
                .AddTo(_disposables);
        }

        private void OnTroopRemoved(ITroopPModel troop)
        {
            var resourceType = troop.Team == Team.Enemy ? ResourceType.Gold : ResourceType.Silver;
            var dropData = _resourceDropConfig.GetDataByResourceType(resourceType);
            if (dropData == null) return;

            _field.CreateResourceDrop(dropData.Index, dropData.ResourceType, dropData.Amount, troop.Position);
        }

        private void OnResourceDropAdded(IResourceDropPModel drop)
        {
            if (drop == null) return;
            if (_dropSubscriptions.ContainsKey(drop)) return;

            var subscription = drop.OnCollected.Subscribe(OnDropCollected);
            _dropSubscriptions[drop] = subscription;
        }

        private void OnResourceDropRemoved(IResourceDropPModel drop)
        {
            if (drop == null) return;
            if (!_dropSubscriptions.TryGetValue(drop, out var subscription)) return;

            subscription.Dispose();
            _dropSubscriptions.Remove(drop);
        }

        private void OnDropCollected(IResourceDropPModel drop)
        {
            if (drop.ResourceType == ResourceType.Gold)
                _resources.AddGold(drop.Amount);
            else
                _resources.AddSilver(drop.Amount);

            _field.RemoveResourceDrop(drop);
        }

        public void Dispose()
        {
            _disposables.Dispose();

            foreach (var subscription in _dropSubscriptions.Values)
                subscription.Dispose();
            _dropSubscriptions.Clear();
        }
    }
}
