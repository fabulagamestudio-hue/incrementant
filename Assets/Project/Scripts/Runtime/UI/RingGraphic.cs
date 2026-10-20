using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Coroa circular desenhada por malha (sem sprite): a "terra escura" de um anel fechado da tela Evolução. O centro do
    /// círculo é o pivô do RectTransform; os raios estão em pixels de UI.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RingGraphic : MaskableGraphic
    {
        [SerializeField, Min(0f)] float _innerRadius = 100f;
        [SerializeField, Min(1f)] float _outerRadius = 200f;
        [SerializeField, Range(16, 256)] int _segments = 128;

        public void SetRadii(float inner, float outer)
        {
            _innerRadius = Mathf.Max(0f, inner);
            _outerRadius = Mathf.Max(_innerRadius + 1f, outer);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 pivotOffset = new Vector2((0.5f - rectTransform.pivot.x) * rectTransform.rect.width,
                                              (0.5f - rectTransform.pivot.y) * rectTransform.rect.height);
            Color32 c = color;
            for (int i = 0; i <= _segments; i++)
            {
                float angle = i * Mathf.PI * 2f / _segments;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(pivotOffset + dir * _innerRadius, c, Vector4.zero);
                vh.AddVert(pivotOffset + dir * _outerRadius, c, Vector4.zero);
            }
            for (int i = 0; i < _segments; i++)
            {
                int a = i * 2;
                vh.AddTriangle(a, a + 1, a + 3);
                vh.AddTriangle(a, a + 3, a + 2);
            }
        }
    }
}
