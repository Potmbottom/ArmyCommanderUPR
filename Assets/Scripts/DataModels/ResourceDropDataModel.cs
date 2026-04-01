using System;
using UnityEngine;

namespace ArmyCommander
{
    [Serializable]
    public class ResourceDropDataModel
    {
        public int Index;
        public ResourceType ResourceType;
        public GameObject Prefab;
        public int Amount = 1;
    }
}
