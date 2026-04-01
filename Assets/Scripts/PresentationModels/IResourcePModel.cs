using R3;

namespace ArmyCommander
{
    public interface IResourcePModel
    {
        ReadOnlyReactiveProperty<int> Gold { get; }
        ReadOnlyReactiveProperty<int> Silver { get; }

        void AddGold(int amount);
        void AddSilver(int amount);
        bool TrySpendGold(int amount);
        bool TrySpendSilver(int amount);
    }
}
