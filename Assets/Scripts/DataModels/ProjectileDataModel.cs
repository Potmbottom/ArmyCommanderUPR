using System;
using UnityEngine;

namespace ArmyCommander
{
    [Serializable]
    public class ProjectileDataModel
    {
        public int Index;
        public GameObject Prefab;
        public float MoveSpeed;
        public float LifeTime;
        public float ColliderRadius;
        public float Damage;
    }
}
