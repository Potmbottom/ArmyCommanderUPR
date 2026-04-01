using System;
using UnityEngine;

namespace ArmyCommander
{
    [Serializable]
    public class TroopDataModel
    {
        public int Index;
        public TroopType TroopType;
        public GameObject Prefab;
        public float MoveSpeed;
        public float Health;
        public float AggressiveRange;
        public float AttackRange;
        public float AttackSpeed;
        public float SpawnSpeed;
        public int ProjectileIndex;
    }
}
