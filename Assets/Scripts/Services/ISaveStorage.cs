namespace ArmyCommander
{
    public interface ISaveStorage
    {
        bool HasKey(string key);
        int GetInt(string key, int defaultValue = 0);
        void SetInt(string key, int value);
        void DeleteKey(string key);
        void Save();
    }
}
