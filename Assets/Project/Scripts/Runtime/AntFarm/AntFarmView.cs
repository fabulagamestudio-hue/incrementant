using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Desenha grade, formigas e pincel numa textura (1 célula = 1 texel, filtro Point) que cobre só uma janela da
    /// grade em volta da vista da câmera, para o custo não crescer com o mundo. A janela é exibida por um
    /// SpriteRenderer filho (criado no Play) com 1 unidade de mundo por célula; o SpriteRenderer deste objeto
    /// fica só para o preview do mapa no Editor. A posição deste objeto é a origem do mapa (célula 0,0).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class AntFarmView : MonoBehaviour
    {
        [SerializeField] Color32 _skyTop = new Color32(120, 170, 215, 255);
        [SerializeField] Color32 _skyBottom = new Color32(190, 220, 235, 255);
        [SerializeField] Color32 _tunnelColor = new Color32(54, 37, 27, 255);
        [SerializeField] Color32 _markColor = new Color32(255, 196, 40, 255);
        [SerializeField, Range(0f, 1f)] float _markStrength = 0.45f;
        [SerializeField] Color32 _antBody = new Color32(20, 12, 10, 255);
        [SerializeField] Color32 _antHead = new Color32(48, 26, 18, 255);
        [SerializeField] Color32 _brushColor = new Color32(255, 255, 255, 255);
        [SerializeField] Color32 _dumpMarkerColor = new Color32(255, 255, 255, 255);
        [Header("Chuva")]
        [SerializeField] Color32 _lightDropColor = new Color32(225, 238, 252, 255);
        [SerializeField] Color32 _heavyDropColor = new Color32(205, 225, 250, 255);
        [SerializeField, Range(0f, 1f)] float _dropOpacity = 0.85f;
        [Tooltip("Comprimento do risco da gota, em múltiplos da queda por passo.")]
        [SerializeField, Range(1f, 4f)] float _dropStreak = 2f;
        [Tooltip("Quanto cada nível de umidade escurece a célula (terra molhada pela chuva leve).")]
        [SerializeField, Range(0f, 0.3f)] float _wetDarkening = 0.1f;

        /// <summary>A janela cresce em passos deste tamanho, para não recriar a textura a cada mudança de zoom.</summary>
        const int WindowStep = 16;

        SpriteRenderer _windowRenderer;
        Texture2D _texture;
        Sprite _sprite;
        Color32[] _pixels;
        Color32[] _skyRows;
        int _width;
        int _height;
        int _originX;
        int _originY;

        /// <param name="transparentSky">Deixa o céu transparente, para um cenário desenhado atrás (SurfaceSceneryView) aparecer.</param>
        /// <param name="skyBaseY">Linha onde o gradiente do céu começa (cor de baixo); o topo da grade tem a cor de cima.</param>
        public void Initialize(PixelGrid grid, bool transparentSky = false, int skyBaseY = 0)
        {
            if (_windowRenderer == null)
            {
                var own = GetComponent<SpriteRenderer>();
                var go = new GameObject("TerrainWindow") { hideFlags = HideFlags.DontSave };
                go.transform.SetParent(transform, false);
                _windowRenderer = go.AddComponent<SpriteRenderer>();
                _windowRenderer.sortingLayerID = own.sortingLayerID;
                _windowRenderer.sortingOrder = own.sortingOrder;
                _windowRenderer.sharedMaterial = own.sharedMaterial;
                // O sprite próprio é só o preview do Editor (PNG do modo Image); no Play quem desenha é a janela.
                own.enabled = false;
            }

            int top = grid.Height - 1;
            _skyRows = new Color32[grid.Height];
            for (int y = 0; y < grid.Height; y++)
                _skyRows[y] = transparentSky
                    ? new Color32(0, 0, 0, 0)
                    : Color32.Lerp(_skyBottom, _skyTop, Mathf.Clamp01((y - skyBaseY) / (float)Mathf.Max(1, top - skyBaseY)));
        }

        /// <summary>Recorte da grade (em células) que cobre <paramref name="view"/> (retângulo local ao mapa) com folga.</summary>
        public static RectInt WindowFor(Rect view, PixelGrid grid)
        {
            int width = Mathf.Min(grid.Width, RoundUp(Mathf.CeilToInt(view.width) + 2, WindowStep));
            int height = Mathf.Min(grid.Height, RoundUp(Mathf.CeilToInt(view.height) + 2, WindowStep));
            int x = Mathf.Clamp(Mathf.FloorToInt(view.center.x - width * 0.5f), 0, grid.Width - width);
            int y = Mathf.Clamp(Mathf.FloorToInt(view.center.y - height * 0.5f), 0, grid.Height - height);
            return new RectInt(x, y, width, height);
        }

        static int RoundUp(int value, int step) => (value + step - 1) / step * step;

        /// <param name="window">Recorte da grade a desenhar (ver <see cref="WindowFor"/>).</param>
        /// <param name="drawAnts">Desenha as formigas na textura (fallback de 3 pixels quando não há AntSwarmView).</param>
        /// <param name="drops">Gotas de chuva em queda (opcional).</param>
        public void Render(PixelGrid grid, IReadOnlyList<Ant> ants, TerrainLayout layout, Vector2Int brushCell, int brushRadius,
                           bool showBrush, bool drawAnts, RectInt window, IReadOnlyList<RainDrop> drops = null)
        {
            if (_windowRenderer == null || _skyRows.Length != grid.Height)
                Initialize(grid);
            if (_texture == null || window.width != _width || window.height != _height)
                CreateTexture(window.width, window.height);
            _originX = window.x;
            _originY = window.y;
            _windowRenderer.transform.localPosition = new Vector3(_originX, _originY, 0f);

            DrawCells(grid);
            DrawDumpMarkers(grid, layout);
            if (drops != null)
                DrawDrops(drops);
            if (drawAnts)
                DrawAnts(ants);
            if (showBrush)
                DrawBrush(brushCell, brushRadius);

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        void CreateTexture(int width, int height)
        {
            ReleaseTexture();
            _width = width;
            _height = height;
            _pixels = new Color32[width * height];
            _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "AntFarmWorld",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            _sprite = Sprite.Create(_texture, new Rect(0, 0, width, height), Vector2.zero, 1f, 0, SpriteMeshType.FullRect);
            _sprite.name = "AntFarmWorld";
            _windowRenderer.sprite = _sprite;
        }

        void DrawCells(PixelGrid grid)
        {
            for (int wy = 0; wy < _height; wy++)
            {
                int row = wy * _width;
                int y = _originY + wy;
                for (int wx = 0; wx < _width; wx++)
                {
                    int x = _originX + wx;
                    CellMaterial material = grid.Get(x, y);
                    Color32 color;
                    if (material == CellMaterial.Empty)
                    {
                        if (y > grid.GetSurfaceY(x))
                        {
                            color = _skyRows[y];
                        }
                        else
                        {
                            // Fundo de túnel com leve granulação fixa por posição.
                            int n = ((x * 73856093) ^ (y * 19349663)) & 15;
                            color = new Color32((byte)(_tunnelColor.r + n), (byte)(_tunnelColor.g + n / 2), (byte)(_tunnelColor.b + n / 3), 255);
                        }
                    }
                    else
                    {
                        color = CellMaterials.ShadedColor(material, grid.GetShade(x, y));
                        byte wetness = grid.GetWetness(x, y);
                        if (wetness != 0)
                            color = Darken(color, Mathf.Max(0.4f, 1f - wetness * _wetDarkening));
                        if (grid.IsMarked(x, y))
                            color = Color32.Lerp(color, _markColor, _markStrength);
                    }
                    _pixels[row + wx] = color;
                }
            }
        }

        void DrawDumpMarkers(PixelGrid grid, TerrainLayout layout)
        {
            // Duas estacas discretas indicando a faixa onde as formigas despejam terra.
            DrawStake(grid, layout.DumpMinX);
            DrawStake(grid, layout.DumpMaxX);
        }

        void DrawStake(PixelGrid grid, int x)
        {
            int baseY = grid.GetSurfaceY(x) + 1;
            for (int i = 0; i < 4; i++)
                Blend(x, baseY + i + 6, _dumpMarkerColor, 0.5f);
        }

        void DrawAnts(IReadOnlyList<Ant> ants)
        {
            for (int i = 0; i < ants.Count; i++)
            {
                Ant ant = ants[i];
                Put(ant.X - ant.FacingX, ant.Y, _antBody);
                Put(ant.X, ant.Y, _antBody);
                Put(ant.X + ant.FacingX, ant.Y, _antHead);
                if (ant.State == AntState.Carrying)
                    Put(ant.X, ant.Y + 1, CellMaterials.ShadedColor(ant.Carried, ant.CarriedShade));
            }
        }

        void DrawDrops(IReadOnlyList<RainDrop> drops)
        {
            // Cada gota é um risco vertical proporcional à queda por passo, terminando na posição atual.
            for (int i = 0; i < drops.Count; i++)
            {
                RainDrop drop = drops[i];
                Color32 color = drop.Heavy ? _heavyDropColor : _lightDropColor;
                int length = Mathf.Max(1, Mathf.RoundToInt(drop.Speed * _dropStreak));
                for (int k = 0; k < length; k++)
                    BlendOver(drop.X, drop.Y + k, color, _dropOpacity * (1f - k / (float)(length + 1)));
            }
        }

        static Color32 Darken(Color32 c, float f) =>
            new Color32((byte)(c.r * f), (byte)(c.g * f), (byte)(c.b * f), c.a);

        void DrawBrush(Vector2Int center, int radius)
        {
            int r2 = radius * radius;
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (!Inside(dx, dy, r2))
                    continue;
                bool edge = !Inside(dx + 1, dy, r2) || !Inside(dx - 1, dy, r2) ||
                            !Inside(dx, dy + 1, r2) || !Inside(dx, dy - 1, r2);
                if (edge)
                    Blend(center.x + dx, center.y + dy, _brushColor, 0.6f);
            }
        }

        static bool Inside(int dx, int dy, int r2) => dx * dx + dy * dy <= r2;

        // Put/Blend recebem coordenadas de célula da grade e descartam o que cai fora da janela.
        void Put(int x, int y, Color32 color)
        {
            x -= _originX;
            y -= _originY;
            if ((uint)x < (uint)_width && (uint)y < (uint)_height)
                _pixels[y * _width + x] = color;
        }

        void Blend(int x, int y, Color32 color, float t)
        {
            x -= _originX;
            y -= _originY;
            if ((uint)x < (uint)_width && (uint)y < (uint)_height)
                _pixels[y * _width + x] = Color32.Lerp(_pixels[y * _width + x], color, t);
        }

        /// <summary>Como <see cref="Blend"/>, mas sobre céu transparente pinta a cor com alfa (o cenário aparece atrás).</summary>
        void BlendOver(int x, int y, Color32 color, float t)
        {
            x -= _originX;
            y -= _originY;
            if ((uint)x >= (uint)_width || (uint)y >= (uint)_height)
                return;
            int i = y * _width + x;
            Color32 under = _pixels[i];
            if (under.a == 0)
                _pixels[i] = new Color32(color.r, color.g, color.b, (byte)(255 * t));
            else
                _pixels[i] = Color32.Lerp(under, new Color32(color.r, color.g, color.b, under.a), t);
        }

        void OnDestroy() => ReleaseTexture();

        void ReleaseTexture()
        {
            if (_sprite != null)
                Destroy(_sprite);
            if (_texture != null)
                Destroy(_texture);
            _sprite = null;
            _texture = null;
        }
    }
}
