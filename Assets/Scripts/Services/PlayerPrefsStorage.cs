using System;
using UnityEngine;

namespace ArmyCommander
{
    public class PlayerPrefsStorage : ISaveStorage
    {
        public bool HasKey(string key)
        {
            return ExecuteRead(key, false, PlayerPrefs.HasKey);
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return ExecuteRead(key, defaultValue, validKey => PlayerPrefs.GetInt(validKey, defaultValue));
        }

        public void SetInt(string key, int value)
        {
            ExecuteWrite(key, validKey => PlayerPrefs.SetInt(validKey, value));
        }

        public void DeleteKey(string key)
        {
            ExecuteWrite(key, PlayerPrefs.DeleteKey);
        }

        public void Save()
        {
            try
            {
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{nameof(PlayerPrefsStorage)}: Failed to save PlayerPrefs. {ex.Message}");
            }
        }

        private static T ExecuteRead<T>(string key, T fallbackValue, Func<string, T> readOperation)
        {
            if (!TryValidateKey(key, out var validKey))
                return fallbackValue;

            try
            {
                return readOperation(validKey);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{nameof(PlayerPrefsStorage)}: Read failed for key '{validKey}'. {ex.Message}");
                return fallbackValue;
            }
        }

        private static void ExecuteWrite(string key, Action<string> writeOperation)
        {
            if (!TryValidateKey(key, out var validKey))
                return;

            try
            {
                writeOperation(validKey);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{nameof(PlayerPrefsStorage)}: Write failed for key '{validKey}'. {ex.Message}");
            }
        }

        private static bool TryValidateKey(string key, out string validKey)
        {
            validKey = key;
            if (!string.IsNullOrWhiteSpace(key))
                return true;

            Debug.LogWarning($"{nameof(PlayerPrefsStorage)}: Save key is null or whitespace.");
            return false;
        }
    }
}
