using System.Collections.Generic;
using R3;
using UnityEngine;

namespace ArmyCommander
{
    //TODO: Split on parts if grow more
    public class UIModel : IUIModel
    {
        private readonly ReactiveProperty<int> _gold = new(0);
        public ReadOnlyReactiveProperty<int> Gold => _gold;

        private readonly ReactiveProperty<int> _silver = new(0);
        public ReadOnlyReactiveProperty<int> Silver => _silver;

        private readonly ReactiveProperty<float> _enemyProgress = new(0f);
        public ReadOnlyReactiveProperty<float> EnemyProgress => _enemyProgress;

        private readonly Subject<(List<TroopType> AvailableTypes, List<TroopType> AffordableTypes)> _onShowBuildPopup = new();
        public Observable<(List<TroopType> AvailableTypes, List<TroopType> AffordableTypes)> OnShowBuildPopup => _onShowBuildPopup;

        private readonly Subject<Unit> _onHideBuildPopup = new();
        public Observable<Unit> OnHideBuildPopup => _onHideBuildPopup;

        private readonly Subject<TroopType> _onBuildSelected = new();
        public Observable<TroopType> OnBuildSelected => _onBuildSelected;

        private readonly Subject<Unit> _onShowNextLevelPopup = new();
        public Observable<Unit> OnShowNextLevelPopup => _onShowNextLevelPopup;
        
        private readonly Subject<Unit> _onShowEndGamePopup = new();
        public Observable<Unit> OnShowEndGamePopup => _onShowEndGamePopup;

        private readonly Subject<Unit> _onNextLevelRequested = new();
        public Observable<Unit> OnNextLevelRequested => _onNextLevelRequested;

        private readonly Subject<Unit> _onReloadRequested = new();
        public Observable<Unit> OnReloadRequested => _onReloadRequested;

        private int _initialEnemyCount;

        public void SetGold(int amount) => _gold.Value = amount;
        public void SetSilver(int amount) => _silver.Value = amount;

        public void InitializeEnemyProgress(int initialEnemyCount)
        {
            _initialEnemyCount = Mathf.Max(0, initialEnemyCount);
            _enemyProgress.Value = 0f;
        }

        public void UpdateEnemyProgress(int remainingEnemyCount)
        {
            _enemyProgress.Value = _initialEnemyCount > 0
                ? 1f - (float)Mathf.Max(0, remainingEnemyCount) / _initialEnemyCount
                : 1f;
        }

        public void ShowBuildPopup(List<TroopType> availableTypes, List<TroopType> affordableTypes) => _onShowBuildPopup.OnNext((availableTypes, affordableTypes));
        public void HideBuildPopup() => _onHideBuildPopup.OnNext(Unit.Default);
        public void SelectBuild(TroopType type) => _onBuildSelected.OnNext(type);
        public void ShowNextLevelPopup() => _onShowNextLevelPopup.OnNext(Unit.Default);
        public void ShowEndGamePopup() => _onShowEndGamePopup.OnNext(Unit.Default);
        public void RequestNextLevel() => _onNextLevelRequested.OnNext(Unit.Default);
        public void RequestReload() => _onReloadRequested.OnNext(Unit.Default);
    }
}
