using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Trecho escavado de um ramo da tela Evolução (KI-046): salões (câmaras de formigueiro, mais largas que altas e de piso
    /// plano) ligados por túneis finos e sinuosos, numa só malha de cor opaca, o que dá o corte de um formigueiro sem sprite.
    /// O centro do RectTransform é a origem das coordenadas do conteúdo da árvore. <see cref="_grow"/> engorda tudo por igual:
    /// uma cópia maior e mais clara por baixo vira a parede.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CaveGraphic : MaskableGraphic
    {
        [SerializeField] Vector4[] _chambers = System.Array.Empty<Vector4>(); // x, y do centro; meia largura, meia altura
        [SerializeField] Vector4[] _tunnels = System.Array.Empty<Vector4>();  // x1, y1, x2, y2
        [SerializeField, Min(1f)] float _tunnelWidth = 34f;
        [SerializeField, Min(0f)] float _grow;
        [SerializeField, Range(16, 96)] int _segments = 64;

        public void Set(Vector4[] chambers, Vector4[] tunnels, float tunnelWidth, float grow)
        {
            _chambers = chambers;
            _tunnels = tunnels;
            _tunnelWidth = tunnelWidth;
            _grow = grow;
            SetVerticesDirty();
        }

        // Irregularidade: a borda do salão ondula de leve e o túnel serpenteia e alarga/estreita. O ruído sai só da posição,
        // então a parede (cópia engordada, noutro objeto) e o vão de cada ramo coincidem.
        const float TunnelPiece = 24f, TunnelSway = 30f;
        // Salão: superelipse (expoente 4, quase um retângulo de cantos bem redondos) com o piso cortado reto nesta fração da
        // meia altura, como as câmaras de um formigueiro em corte.
        const float ChamberPower = 4f, FloorFraction = 0.82f;

        static float Hash(float x, float y) => Mathf.Repeat(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f, 1f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 origin = new Vector2((0.5f - rectTransform.pivot.x) * rectTransform.rect.width,
                                         (0.5f - rectTransform.pivot.y) * rectTransform.rect.height);
            Color32 c = color;
            AddTunnels(vh, origin, c);
            AddChambers(vh, origin, c);
        }

        void AddChambers(VertexHelper vh, Vector2 origin, Color32 c)
        {
            foreach (Vector4 ch in _chambers)
            {
                float a = ch.z + _grow, b = ch.w + _grow, floor = ch.w * FloorFraction + _grow;
                float p1 = Hash(ch.x, ch.y) * 6.283f, p2 = Hash(ch.y, ch.x + 3f) * 6.283f;
                Vector2 center = origin + new Vector2(ch.x, ch.y);
                int first = vh.currentVertCount;
                vh.AddVert(center, c, Vector4.zero);
                for (int i = 0; i < _segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / _segments;
                    float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                    // Superelipse: |x/a|^n + |y/b|^n = 1 na direção do ângulo.
                    float r = Mathf.Pow(Mathf.Pow(Mathf.Abs(cos) / a, ChamberPower) + Mathf.Pow(Mathf.Abs(sin) / b, ChamberPower), -1f / ChamberPower);
                    r *= 1f + 0.05f * Mathf.Sin(3f * angle + p1) + 0.03f * Mathf.Sin(7f * angle + p2);
                    Vector2 p = new Vector2(cos * r, sin * r);
                    if (p.y < -floor) // piso plano, com calombos de terra pequenos
                        p.y = -floor + 3f * Mathf.Sin(p.x * 0.09f + p1);
                    vh.AddVert(center + p, c, Vector4.zero);
                }
                for (int i = 0; i < _segments; i++)
                    vh.AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % _segments);
            }
        }

        /// <summary>Quantos trechos retos formam um túnel deste comprimento (a malha e as linhas usam os mesmos).</summary>
        public static int TunnelPieces(float length) => Mathf.Max(2, Mathf.CeilToInt(length / TunnelPiece));

        /// <summary>
        /// Ponto do eixo do túnel <paramref name="t"/> (x1, y1, x2, y2) na fração <paramref name="u"/>, nas coordenadas do
        /// conteúdo: a mesma curva da malha, para as linhas de dependência andarem dentro do túnel.
        /// </summary>
        public static Vector2 TunnelPoint(Vector4 t, float u)
        {
            Vector2 a = new Vector2(t.x, t.y), b = new Vector2(t.z, t.w);
            Vector2 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.1f)
                return a;
            Vector2 normal = new Vector2(-delta.y, delta.x) / length;
            float phase = TunnelPhase(t), waves = TunnelWaves(length);
            float ends = Mathf.Sin(u * Mathf.PI); // 0 nas pontas: o túnel encosta nos salões sem desvio
            float sway = (Mathf.Sin(u * waves * 6.283f + phase) * 0.7f + Mathf.Sin(u * waves * 15f + phase * 2f) * 0.3f) * TunnelSway * ends;
            return a + delta * u + normal * sway;
        }

        static float TunnelPhase(Vector4 t) => Hash(t.x, t.y) * 6.283f + Hash(t.z, t.w) * 3f;

        // Ondas proporcionais ao comprimento: um túnel longo faz mais curvas, não curvas mais largas.
        static float TunnelWaves(float length) => Mathf.Max(1f, length / 260f);

        void AddTunnels(VertexHelper vh, Vector2 origin, Color32 c)
        {
            foreach (Vector4 t in _tunnels)
            {
                Vector2 delta = new Vector2(t.z - t.x, t.w - t.y);
                float length = delta.magnitude;
                if (length < 0.1f)
                    continue;
                Vector2 normal = new Vector2(-delta.y, delta.x) / length;
                float phase = TunnelPhase(t), waves = TunnelWaves(length);
                int pieces = TunnelPieces(length);
                int previous = -1;
                for (int k = 0; k <= pieces; k++)
                {
                    float u = k / (float)pieces;
                    float ends = Mathf.Sin(u * Mathf.PI);
                    float half = _tunnelWidth * 0.5f * (1f + 0.3f * Mathf.Sin(u * waves * 9f + phase * 1.7f) * ends) + _grow;
                    Vector2 mid = origin + TunnelPoint(t, u);
                    int v = vh.currentVertCount;
                    vh.AddVert(mid - normal * half, c, Vector4.zero);
                    vh.AddVert(mid + normal * half, c, Vector4.zero);
                    if (previous >= 0)
                    {
                        vh.AddTriangle(previous, previous + 1, v + 1);
                        vh.AddTriangle(previous, v + 1, v);
                    }
                    previous = v;
                }
            }
        }
    }
}
