using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace ArmyCommander
{
    public class BarrackService : ITickable, IInitializable, IDisposable
    {
        private const float DeadTroopLifetimeSeconds = 2f;

        private IFieldPModel _field;
        private ITrainingFieldPModel _trainingField;
        private TroopsConfig _config;
        private LevelConfig _levelConfig;
        private ISaveStorage _saveStorage;

        private readonly CompositeDisposable _disposables = new();
        private readonly Dictionary<ITroopPModel, IDisposable> _deathSubs = new();
        private readonly Dictionary<ITroopPModel, float> _deadTroopTimers = new();
        private readonly List<ITroopPModel> _deadTroopKeysCache = new();
        private readonly Dictionary<IBarrackSlotPModel, BarrackProduction> _productions = new();

        [Inject]
        public void SetDependency(IFieldPModel field, ITrainingFieldPModel trainingField, TroopsConfig config, LevelConfig levelConfig, ISaveStorage saveStorage)
        {
            _field = field;
            _trainingField = trainingField;
            _config = config;
            _levelConfig = levelConfig;
            _saveStorage = saveStorage;
        }

        public void Initialize()
        {
            _field.OnTroopAdded.Subscribe(OnTroopAdded).AddTo(_disposables);
            _field.OnTroopRemoved.Subscribe(OnTroopRemoved).AddTo(_disposables);
        }

        public void RegisterSlot(IBarrackSlotPModel slot)
        {
            slot.TroopType
                .Subscribe(type => OnSlotTypeChanged(slot, type))
                .AddTo(_disposables);
        }

        private void OnSlotTypeChanged(IBarrackSlotPModel slot, TroopType type)
        {
            if (_productions.TryGetValue(slot, out var existing))
            {
                existing.Dispose();
                _productions.Remove(slot);
            }

            if (type != TroopType.Empty)
            {
                var data = GetDataForType(type);
                if (data != null)
                {
                    var production = new BarrackProduction(data, slot);
                    _productions[slot] = production;
                }
            }

            _trainingField.SetOrderAvailable(_productions.Count > 0);
        }

        private void OnTroopAdded(ITroopPModel troop)
        {
            var sub = troop.OnStateChanged
                .Where(s => s == TroopState.Dead)
                .Take(1)
                .Subscribe(_ => OnTroopDead(troop));

            _deathSubs[troop] = sub;
        }

        private void OnTroopRemoved(ITroopPModel troop)
        {
            if (_deathSubs.TryGetValue(troop, out var sub))
            {
                sub.Dispose();
                _deathSubs.Remove(troop);
            }
            _deadTroopTimers.Remove(troop);

            if (_trainingField.IsOrderActive && _field.GetAlliedCount() == 0)
                _trainingField.ResetOrder();
        }

        private void OnTroopDead(ITroopPModel troop)
        {
            if (_deadTroopTimers.ContainsKey(troop)) return;
            _deadTroopTimers[troop] = DeadTroopLifetimeSeconds;
        }

        public void Tick()
        {
            TickDeadTroops();
            if (_trainingField.IsOrderActive) return;

            var slotCount = _trainingField.SlotPositions.Count;
            var alliedCount = _field.GetAlliedCount();
            if (alliedCount >= slotCount) return;

            foreach (var kv in _productions)
            {
                var slot = kv.Key;
                var production = kv.Value;

                production.Elapsed += Time.deltaTime;
                if (production.Elapsed < production.Data.SpawnSpeed) continue;

                production.Elapsed = 0f;
                TrySpawnTroop(slot, production.Data);
            }
        }

        private void TickDeadTroops()
        {
            if (_deadTroopTimers.Count == 0) return;

            _deadTroopKeysCache.Clear();
            foreach (var troop in _deadTroopTimers.Keys)
                _deadTroopKeysCache.Add(troop);

            foreach (var troop in _deadTroopKeysCache)
            {
                if (!_deadTroopTimers.TryGetValue(troop, out var timer))
                    continue;

                var next = timer - Time.deltaTime;
                if (next > 0f)
                {
                    _deadTroopTimers[troop] = next;
                    continue;
                }

                _deadTroopTimers.Remove(troop);
                _field.RemoveTroop(troop);
            }
        }

        private void TrySpawnTroop(IBarrackSlotPModel slot, TroopDataModel data)
        {
            var slotPositions = _trainingField.SlotPositions;
            var homePosition = FindFreeSlot(slotPositions);
            if (homePosition == null) return;

            _field.CreateTroop(
                data.Index,
                Team.Allied,
                slot.BuildPoint,
                homePosition.Value,
                data.Health
            );
        }

        private Vector3? FindFreeSlot(IReadOnlyList<Vector3> slotPositions)
        {
            foreach (var slotPos in slotPositions)
            {
                bool occupied = false;
                foreach (var troop in _field.Troops)
                {
                    if (troop.Team == Team.Allied && Vector3.Distance(troop.HomePosition, slotPos) < 0.1f)
                    {
                        occupied = true;
                        break;
                    }
                }
                if (!occupied) return slotPos;
            }
            return null;
        }

        private TroopDataModel GetDataForType(TroopType type)
        {
            if (TryGetLevelOverrideDataIndex(type, out var overrideDataIndex) &&
                TryGetTroopDataByIndex(overrideDataIndex, out var overrideData))
            {
                return overrideData;
            }

            foreach (var data in _config.Troops)
                if (data.TroopType == type) return data;
            return null;
        }

        private bool TryGetLevelOverrideDataIndex(TroopType type, out int dataIndex)
        {
            dataIndex = -1;
            var levelData = GetCurrentLevelData();
            if (levelData == null) return false;

            var overrides = levelData.BarrackTroopIdOverrides;
            if (overrides == null) return false;

            foreach (var item in overrides)
            {
                if (item == null || item.TroopType != type) continue;
                dataIndex = item.IdOverride;
                return dataIndex >= 0;
            }

            return false;
        }

        private LevelData GetCurrentLevelData()
        {
            if (_levelConfig == null || _levelConfig.Levels == null || _levelConfig.Levels.Count == 0)
                return null;

            var currentLevelIndex = _saveStorage.GetInt(ProgressionKeys.CurrentLevel, 0);
            if (currentLevelIndex < 0) currentLevelIndex = 0;
            if (currentLevelIndex >= _levelConfig.Levels.Count) currentLevelIndex = _levelConfig.Levels.Count - 1;
            return _levelConfig.GetData(currentLevelIndex);
        }

        private bool TryGetTroopDataByIndex(int dataIndex, out TroopDataModel data)
        {
            data = null;
            if (_config == null || _config.Troops == null) return false;
            if (dataIndex < 0 || dataIndex >= _config.Troops.Count) return false;

            data = _config.GetData(dataIndex);
            return data != null;
        }

        public void Dispose()
        {
            _disposables.Dispose();
            foreach (var sub in _deathSubs.Values) sub.Dispose();
            foreach (var prod in _productions.Values) prod.Dispose();
            _deathSubs.Clear();
            _deadTroopTimers.Clear();
            _productions.Clear();
        }

        private class BarrackProduction : IDisposable
        {
            public readonly TroopDataModel Data;
            public float Elapsed;
            private readonly IBarrackSlotPModel _slot;

            public BarrackProduction(TroopDataModel data, IBarrackSlotPModel slot)
            {
                Data = data;
                _slot = slot;
            }

            public void Dispose() { }
        }
    }
}
