using R3;

namespace ArmyCommander
{
    public class ResourcePModel : IResourcePModel
    {
        private readonly ReactiveProperty<int> _gold = new(0);
        public ReadOnlyReactiveProperty<int> Gold => _gold;

        private readonly ReactiveProperty<int> _silver = new(0);
        public ReadOnlyReactiveProperty<int> Silver => _silver;

        public void AddGold(int amount) => _gold.Value += amount;
        public void AddSilver(int amount) => _silver.Value += amount;

        public bool TrySpendGold(int amount)
        {
            if (_gold.Value < amount) return false;
            _gold.Value -= amount;
            return true;
        }

        public bool TrySpendSilver(int amount)
        {
            if (_silver.Value < amount) return false;
            _silver.Value -= amount;
            return true;
        }
    }
}
