using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace ArmyCommander
{
    public class SpawnService : ITickable, IFixedTickable, IInitializable, IDisposable
    {
        private IFieldPModel _field;
        private TroopsConfig _troopsConfig;
        private ProjectileConfig _projectileConfig;
        private ResourceDropConfig _resourceDropConfig;

        private readonly Dictionary<int, ObjectPool<TroopControl>> _troopPools = new();
        private readonly Dictionary<int, ObjectPool<ProjectileControl>> _projectilePools = new();
        private readonly Dictionary<int, ObjectPool<ResourceDropControl>> _resourceDropPools = new();
        private readonly Dictionary<ITroopPModel, TroopControl> _troopControls = new();
        private readonly Dictionary<IProjectilePModel, ProjectileControl> _projectileControls = new();
        private readonly Dictionary<IResourceDropPModel, ResourceDropControl> _resourceDropControls = new();

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void SetDependency(IFieldPModel field, TroopsConfig troopsConfig, ProjectileConfig projectileConfig, ResourceDropConfig resourceDropConfig)
        {
            _field = field;
            _troopsConfig = troopsConfig;
            _projectileConfig = projectileConfig;
            _resourceDropConfig = resourceDropConfig;
        }

        public void Initialize()
        {
            _field.OnTroopAdded.Subscribe(OnTroopAdded).AddTo(_disposables);
            _field.OnTroopRemoved.Subscribe(OnTroopRemoved).AddTo(_disposables);
            _field.OnProjectileAdded.Subscribe(OnProjectileAdded).AddTo(_disposables);
            _field.OnProjectileRemoved.Subscribe(OnProjectileRemoved).AddTo(_disposables);
            _field.OnResourceDropAdded.Subscribe(OnResourceDropAdded).AddTo(_disposables);
            _field.OnResourceDropRemoved.Subscribe(OnResourceDropRemoved).AddTo(_disposables);
        }

        public void Tick()
        {
            foreach (var kv in _projectileControls)
                kv.Value.Tick();
        }

        public void FixedTick()
        {
            foreach (var kv in _troopControls)
                kv.Value.FixedTick();
        }

        private void OnTroopAdded(ITroopPModel model)
        {
            var control = GetOrCreateTroopPool(model.DataIndex).Get();
            control.Bind(model);
            _troopControls[model] = control;
        }

        private void OnTroopRemoved(ITroopPModel model)
        {
            if (!_troopControls.TryGetValue(model, out var control)) return;
            control.Release();
            _troopControls.Remove(model);
        }

        private void OnProjectileAdded(IProjectilePModel model)
        {
            var control = GetOrCreateProjectilePool(model.DataIndex).Get();
            control.Bind(model);
            _projectileControls[model] = control;
        }

        private void OnProjectileRemoved(IProjectilePModel model)
        {
            if (!_projectileControls.TryGetValue(model, out var control)) return;
            control.Release();
            _projectileControls.Remove(model);
        }

        private void OnResourceDropAdded(IResourceDropPModel model)
        {
            var control = GetOrCreateResourceDropPool(model.DataIndex).Get();
            control.Bind(model);
            _resourceDropControls[model] = control;
        }

        private void OnResourceDropRemoved(IResourceDropPModel model)
        {
            if (!_resourceDropControls.TryGetValue(model, out var control)) return;
            control.Release();
            _resourceDropControls.Remove(model);
        }

        private ObjectPool<TroopControl> GetOrCreateTroopPool(int dataIndex)
        {
            if (_troopPools.TryGetValue(dataIndex, out var pool)) return pool;

            var prefab = _troopsConfig.GetData(dataIndex).Prefab;
            ObjectPool<TroopControl> newPool = null;
            newPool = new ObjectPool<TroopControl>(
                createFunc: () => Object.Instantiate(prefab).GetComponent<TroopControl>(),
                actionOnGet: c =>
                {
                    c.gameObject.SetActive(true);
                    c.ReleaseToPool = () => newPool.Release(c);
                },
                actionOnRelease: c =>
                {
                    var stashPosition = c.transform.position;
                    stashPosition.x = 1000f;
                    stashPosition.z = 1000f;
                    c.transform.position = stashPosition;
                    c.gameObject.SetActive(false);
                },
                actionOnDestroy: c => Object.Destroy(c.gameObject)
            );

            _troopPools[dataIndex] = newPool;
            return newPool;
        }

        private ObjectPool<ProjectileControl> GetOrCreateProjectilePool(int dataIndex)
        {
            if (_projectilePools.TryGetValue(dataIndex, out var pool)) return pool;

            var prefab = _projectileConfig.GetData(dataIndex).Prefab;
            ObjectPool<ProjectileControl> newPool = null;
            newPool = new ObjectPool<ProjectileControl>(
                createFunc: () => Object.Instantiate(prefab).GetComponent<ProjectileControl>(),
                actionOnGet: c =>
                {
                    c.gameObject.SetActive(true);
                    c.ReleaseToPool = () => newPool.Release(c);
                },
                actionOnRelease: c => c.gameObject.SetActive(false),
                actionOnDestroy: c => Object.Destroy(c.gameObject)
            );

            _projectilePools[dataIndex] = newPool;
            return newPool;
        }

        private ObjectPool<ResourceDropControl> GetOrCreateResourceDropPool(int dataIndex)
        {
            if (_resourceDropPools.TryGetValue(dataIndex, out var pool)) return pool;

            var prefab = _resourceDropConfig.GetData(dataIndex).Prefab;
            ObjectPool<ResourceDropControl> newPool = null;
            newPool = new ObjectPool<ResourceDropControl>(
                createFunc: () => Object.Instantiate(prefab).GetComponent<ResourceDropControl>(),
                actionOnGet: c =>
                {
                    c.gameObject.SetActive(true);
                    c.ReleaseToPool = () => newPool.Release(c);
                },
                actionOnRelease: c => c.gameObject.SetActive(false),
                actionOnDestroy: c => Object.Destroy(c.gameObject)
            );

            _resourceDropPools[dataIndex] = newPool;
            return newPool;
        }

        public void Dispose() => _disposables.Dispose();
    }
}
