using System.Collections.Generic;
using UnityEngine;

namespace ArmyCommander
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "ArmyCommander/LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        public List<LevelData> Levels;

        public LevelData GetData(int levelIndex) => Levels[levelIndex];
    }
}
