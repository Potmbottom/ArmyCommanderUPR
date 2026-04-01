using System.Collections.Generic;
using UnityEngine;

namespace ArmyCommander
{
    [CreateAssetMenu(fileName = "ProjectileConfig", menuName = "ArmyCommander/ProjectileConfig")]
    public class ProjectileConfig : ScriptableObject
    {
        public List<ProjectileDataModel> Projectiles;

        public ProjectileDataModel GetData(int index) => Projectiles[index];
    }
}
