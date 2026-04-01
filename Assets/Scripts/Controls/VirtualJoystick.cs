using UnityEngine;
using UnityEngine.EventSystems;

namespace ArmyCommander
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private float _handleRange = 50f;

        public Vector2 Direction { get; private set; }

        private RectTransform _rectTransform;
        private Canvas _canvas;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
#if !UNITY_ANDROID || UNITY_EDITOR
            gameObject.SetActive(false);
            return;
#endif
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Direction = Vector2.zero;
            _handle.anchoredPosition = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background,
                eventData.position,
                _canvas.worldCamera,
                out var localPoint
            );

            var clamped = Vector2.ClampMagnitude(localPoint, _handleRange);
            _handle.anchoredPosition = clamped;
            Direction = clamped / _handleRange;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Direction = Vector2.zero;
            _handle.anchoredPosition = Vector2.zero;
        }
    }
}
