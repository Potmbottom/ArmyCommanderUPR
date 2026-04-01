using System.Text;
using UnityEngine;

namespace ArmyCommander
{
    public class FpsDisplayControl : MonoBehaviour
    {
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private int _fontSize = 18;
        [SerializeField] private Vector2 _margin = new Vector2(10f, 10f);

        private float _smoothedDeltaTime;
        private GUIStyle _style;
        private readonly StringBuilder _textBuilder = new StringBuilder(16);

        private void Update()
        {
            _smoothedDeltaTime += (Time.unscaledDeltaTime - _smoothedDeltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            if (_style == null)
                _style = new GUIStyle(GUI.skin.label);

            _style.fontSize = _fontSize;
            _style.normal.textColor = _textColor;

            var fps = _smoothedDeltaTime > 0f ? 1f / _smoothedDeltaTime : 0f;
            _textBuilder.Clear();
            _textBuilder.Append("FPS: ");
            _textBuilder.Append(Mathf.RoundToInt(fps));

            GUI.Label(new Rect(_margin.x, _margin.y, 340f, 230f), _textBuilder.ToString(), _style);
        }
    }
}
