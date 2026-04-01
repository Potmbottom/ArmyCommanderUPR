using System.Collections.Generic;
using UnityEngine;

namespace ArmyCommander
{
    [CreateAssetMenu(fileName = "ResourceDropConfig", menuName = "ArmyCommander/ResourceDropConfig")]
    public class ResourceDropConfig : ScriptableObject
    {
        public List<ResourceDropDataModel> ResourceDrops;

        public ResourceDropDataModel GetData(int index) => ResourceDrops[index];

        public ResourceDropDataModel GetDataByResourceType(ResourceType resourceType)
        {
            foreach (var data in ResourceDrops)
            {
                if (data != null && data.ResourceType == resourceType)
                    return data;
            }

            return null;
        }
    }
}
