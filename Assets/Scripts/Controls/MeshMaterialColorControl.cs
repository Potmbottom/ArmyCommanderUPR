using UnityEngine;

namespace ArmyCommander
{
    [RequireComponent(typeof(Renderer))]
    public class MeshMaterialColorControl : MonoBehaviour
    {
        private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        [SerializeField] private Color _color = Color.white;

        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            CacheRendererIfNeeded();
            ApplyColor();
        }

        private void OnValidate()
        {
            CacheRendererIfNeeded();
            ApplyColor();
        }

        private void CacheRendererIfNeeded()
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();

            if (_propertyBlock == null)
                _propertyBlock = new MaterialPropertyBlock();
        }

        private void ApplyColor()
        {
            if (_renderer == null || _propertyBlock == null)
                return;

            var material = _renderer.sharedMaterial;
            if (material == null)
                return;

            _renderer.GetPropertyBlock(_propertyBlock);

            if (material.HasProperty(BaseColorPropertyId))
                _propertyBlock.SetColor(BaseColorPropertyId, _color);
            else if (material.HasProperty(ColorPropertyId))
                _propertyBlock.SetColor(ColorPropertyId, _color);
            else
                return;

            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
