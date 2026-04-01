using UnityEngine;

namespace ArmyCommander
{
    public class AndroidFrameRateControl : MonoBehaviour
    {
        [SerializeField] private int _targetFrameRate = 60;

        private void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _targetFrameRate;
#endif
        }
    }
}
