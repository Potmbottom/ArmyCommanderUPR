using System;
using UnityEngine;

namespace ArmyCommander
{
    [Serializable]
    public class EnemySpawnPoint
    {
        public Transform SpawnTransform;
        public int TroopDataIndex;
    }
}
