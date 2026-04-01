using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArmyCommander
{
    [Serializable]
    public class LevelData
    {
        public GameObject LevelPrefab;
        public int UpgradeCostGold;
        public int BarrackSoldierCostSilver;
        public int BarrackVeteranCostSilver;
        public int BarrackMasterCostSilver;
        public int InitialSilver;
        public List<BarrackTroopIdOverrideData> BarrackTroopIdOverrides;
    }
}
