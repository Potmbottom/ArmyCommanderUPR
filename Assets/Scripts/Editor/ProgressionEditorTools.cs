using UnityEditor;
using UnityEngine;

namespace ArmyCommander.EditorTools
{
    public static class ProgressionEditorTools
    {
        private const int ResetLevelIndex = 0;
        private static readonly ISaveStorage SaveStorage = new PlayerPrefsStorage();

        [MenuItem("Tools/Reset Level Progression")]
        private static void ResetLevelProgression()
        {
            var currentLevel = SaveStorage.GetInt(ProgressionKeys.CurrentLevel, ResetLevelIndex);
            var shouldReset = EditorUtility.DisplayDialog(
                "Reset Level Progression",
                $"Current saved level is {currentLevel}. Reset progression to level {ResetLevelIndex}?",
                "Reset",
                "Cancel");

            if (!shouldReset)
                return;

            SaveStorage.SetInt(ProgressionKeys.CurrentLevel, ResetLevelIndex);
            SaveStorage.Save();
            Debug.Log($"Level progression reset to {ResetLevelIndex}.");
        }
    }
}
