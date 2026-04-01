using System.Collections.Generic;
using UnityEngine;

namespace ArmyCommander
{
    public class LevelRuntimeRoot : MonoBehaviour
    {
        [SerializeField] private PlayerControl _playerControl;
        [SerializeField] private TrainingFieldControl _trainingFieldControl;
        [SerializeField] private ArmyUpgradeControl _armyUpgradeControl;
        [SerializeField] private List<BarrackSlotControl> _barrackSlotControls;
        [SerializeField] private List<EnemySpawnPoint> _enemySpawnPoints;

        public PlayerControl PlayerControl => _playerControl;
        public TrainingFieldControl TrainingFieldControl => _trainingFieldControl;
        public ArmyUpgradeControl ArmyUpgradeControl => _armyUpgradeControl;
        public IReadOnlyList<BarrackSlotControl> BarrackSlotControls => _barrackSlotControls;
        public IReadOnlyList<EnemySpawnPoint> EnemySpawnPoints => _enemySpawnPoints;
    }
}
