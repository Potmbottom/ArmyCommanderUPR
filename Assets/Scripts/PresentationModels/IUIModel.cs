using System.Collections.Generic;
using R3;

namespace ArmyCommander
{
    public interface IUIModel
    {
        ReadOnlyReactiveProperty<int> Gold { get; }
        ReadOnlyReactiveProperty<int> Silver { get; }
        ReadOnlyReactiveProperty<float> EnemyProgress { get; }
        Observable<(List<TroopType> AvailableTypes, List<TroopType> AffordableTypes)> OnShowBuildPopup { get; }
        Observable<Unit> OnHideBuildPopup { get; }
        Observable<TroopType> OnBuildSelected { get; }
        Observable<Unit> OnShowNextLevelPopup { get; }
        Observable<Unit> OnShowEndGamePopup { get; }
        Observable<Unit> OnNextLevelRequested { get; }
        Observable<Unit> OnReloadRequested { get; }

        void SetGold(int amount);
        void SetSilver(int amount);
        void InitializeEnemyProgress(int initialEnemyCount);
        void UpdateEnemyProgress(int remainingEnemyCount);
        void ShowBuildPopup(List<TroopType> availableTypes, List<TroopType> affordableTypes);
        void HideBuildPopup();
        void SelectBuild(TroopType type);
        void ShowNextLevelPopup();
        void ShowEndGamePopup();
        void RequestNextLevel();
        void RequestReload();
    }
}
