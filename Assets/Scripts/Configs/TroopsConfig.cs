using System.Collections.Generic;
using UnityEngine;

namespace ArmyCommander
{
    [CreateAssetMenu(fileName = "TroopsConfig", menuName = "ArmyCommander/TroopsConfig")]
    public class TroopsConfig : ScriptableObject
    {
        public List<TroopDataModel> Troops;

        public TroopDataModel GetData(int index) => Troops[index];
    }
}
