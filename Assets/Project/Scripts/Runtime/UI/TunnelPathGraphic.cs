using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Linha de dependência da tela Evolução que segue os túneis do formigueiro: uma polilinha tracejada (traço com contorno
    /// escuro e miolo na cor do <see cref="Graphic.color"/>), no mesmo padrão do sprite antigo das ligações (16 px de traço,
    /// 4 de vão, 3 de altura, em escala 2). Os pontos ficam nas coordenadas do conteúdo da árvore, com o centro do
    /// RectTransform na origem; o setup calcula o caminho pelos túneis.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TunnelPathGraphic : MaskableGraphic
    {
        [SerializeField] Vector2[] _points = System.Array.Empty<Vector2>();
        [SerializeField, Min(0f)] float _core = 2f;
        [SerializeField, Min(0f)] float _outline = 2f;
        [SerializeField, Min(1f)] float _dash = 32f;
        [SerializeField, Min(0f)] float _gap = 8f;
        [SerializeField] Color _outlineColor = new Color32(0x17, 0x13, 0x0f, 0xff);

        public void SetPoints(Vector2[] points)
        {
            _points = points;
            SetVerticesDirty();
        }

        /// <summary>Espessura do miolo (o contorno fica por fora); muda para destacar a linha.</summary>
        public float Core
        {
            get => _core;
            set
            {
                if (Mathf.Approximately(_core, value))
                    return;
                _core = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points == null || _points.Length < 2)
                return;
            Vector2 origin = new Vector2((0.5f - rectTransform.pivot.x) * rectTransform.rect.width,
                                         (0.5f - rectTransform.pivot.y) * rectTransform.rect.height);
            Color32 outline = _outlineColor;
            outline.a = (byte)(outline.a * color.a);
            // Contorno de todos os traços antes dos miolos, para o miolo de um trecho não ficar sob o contorno do seguinte.
            AddDashes(vh, origin, _core * 0.5f + _outline, outline, true);
            AddDashes(vh, origin, _core * 0.5f, color, false);
        }

        void AddDashes(VertexHelper vh, Vector2 origin, float half, Color32 c, bool extendEnds)
        {
            float period = _dash + _gap;
            float distance = 0f;
            for (int i = 0; i + 1 < _points.Length; i++)
            {
                Vector2 a = _points[i], b = _points[i + 1];
                float length = (b - a).magnitude;
                if (length < 0.01f)
                    continue;
                Vector2 dir = (b - a) / length;
                Vector2 normal = new Vector2(-dir.y, dir.x) * half;
                // Traços que cruzam este trecho: [k·período, k·período + traço] ∩ [distância, distância + comprimento].
                for (float start = Mathf.Floor(distance / period) * period; start < distance + length; start += period)
                {
                    float from = Mathf.Max(start, distance), to = Mathf.Min(start + _dash, distance + length);
                    if (to <= from)
                        continue;
                    // O contorno passa um pouco das pontas, como a borda do sprite; o miolo para no traço.
                    float pad = extendEnds ? _outline : 0f;
                    Vector2 p = origin + a + dir * (from - distance - (from == start ? pad : 0f));
                    Vector2 q = origin + a + dir * (to - distance + (to == start + _dash ? pad : 0f));
                    int v = vh.currentVertCount;
                    vh.AddVert(p - normal, c, Vector4.zero);
                    vh.AddVert(p + normal, c, Vector4.zero);
                    vh.AddVert(q + normal, c, Vector4.zero);
                    vh.AddVert(q - normal, c, Vector4.zero);
                    vh.AddTriangle(v, v + 1, v + 2);
                    vh.AddTriangle(v, v + 2, v + 3);
                }
                distance += length;
            }
        }
    }
}
