using DG.Tweening;
using UnityEngine;
#if DOTWEEN_ENABLED
using DG.Tweening;
#endif

namespace ArmyCommander
{
    public class ResourceDropRotateControl : MonoBehaviour
    {
        [SerializeField] private float _loopDuration = 3f;
        [SerializeField] private float _rotationDegreesPerLoop = 360f;

        private Quaternion _initialLocalRotation;
        
        private Tween _rotationTween;

        private void Awake()
        {
            _initialLocalRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            transform.localRotation = _initialLocalRotation;
            
            _rotationTween?.Kill();
            _rotationTween = transform
                .DOLocalRotate(new Vector3(0f, _rotationDegreesPerLoop, 0f), _loopDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }

        private void OnDisable()
        {
            _rotationTween?.Kill();
            _rotationTween = null;
        }
    }
}
