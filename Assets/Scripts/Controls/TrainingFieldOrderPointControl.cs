using UnityEngine;

namespace ArmyCommander
{
    public class TrainingFieldOrderPointControl : MonoBehaviour
    {
        [SerializeField] private TrainingFieldControl _trainingFieldControl;
        private void OnTriggerEnter(Collider other) => _trainingFieldControl?.OnOrderTriggerEnter(other);
    }
}
