using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Desenha as plantas colhíveis (<see cref="VegetationSimulation"/>) com a arte do cenário: grama com os carimbos de
    /// <see cref="SceneryElement.Grass"/> e de <see cref="SceneryElement.Tuft"/> (tufo baixo) e muda folhosa com os de
    /// <see cref="SceneryElement.Clover"/> e de <see cref="SceneryElement.Flower"/>. A forma da planta na
    /// grade é a silhueta do carimbo reduzida a células (<see cref="PlantShape.FromPixels"/>), então cada célula da planta
    /// é um bloco de <see cref="SurfaceSceneryPainter.PixelsPerCell"/>² pixels do desenho. Cada planta tem a própria
    /// textura: célula colhida apaga o bloco dela, célula que voltou a crescer o redesenha e célula marcada para colheita
    /// ganha a tinta de marca, como a terra. Um SpriteRenderer por planta, atrás do terreno e na frente do cenário perto.
    /// </summary>
    public sealed class PlantSprites
    {
        readonly Transform _parent;
        readonly int _sortingOrder;
        readonly SceneryStamp[][] _stamps = new SceneryStamp[2][];
        readonly PlantShape[][] _shapes = new PlantShape[2][];
        readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();
        int _structureVersion = -1;
        int _markVersion = -1;
        int _plantCount = -1;

        sealed class Entry
        {
            public SpriteRenderer Renderer;
            public Texture2D Texture;
            public Sprite Sprite;
            public SceneryStamp Stamp;
            public PlantShape Shape;
            public Color32[] Pixels;
            /// <summary>Por célula da forma: 0 = colhida, 1 = presente, 2 = presente e marcada.</summary>
            public byte[] State;
            /// <summary>Linhas enterradas desenhadas por último (VegetationSimulation.GetSunkRows).</summary>
            public int Sunk;
            public float BaseY;
        }

        public PlantSprites(Transform parent, SceneryStampLibrary library, int sortingOrder)
        {
            _parent = parent;
            _sortingOrder = sortingOrder;
            // Toda planta da superfície é colhível (o cenário não pinta mais grama, trevo, flor nem tufo).
            Load(PlantKind.Grass, library, SceneryElement.Grass, SceneryElement.Tuft);
            Load(PlantKind.Leafy, library, SceneryElement.Clover, SceneryElement.Flower);
        }

        /// <summary>Forma de cada variante de arte do tipo, na ordem das variantes (passe para <see cref="VegetationSimulation"/>).</summary>
        public PlantShape[] Shapes(PlantKind kind) => _shapes[(int)kind];

        void Load(PlantKind kind, SceneryStampLibrary library, params SceneryElement[] elements)
        {
            var all = new List<SceneryStamp>();
            foreach (SceneryElement element in elements)
            {
                SceneryStamp[] art = library?.Get(element);
                all.AddRange(art != null && art.Length > 0 ? art : SceneryStampLibrary.Default.Get(element));
            }
            SceneryStamp[] stamps = all.ToArray();
            var shapes = new PlantShape[stamps.Length];
            for (int i = 0; i < stamps.Length; i++)
                shapes[i] = PlantShape.FromPixels(stamps[i].Pixels, stamps[i].Width, stamps[i].Height, stamps[i].PivotX,
                                                  stamps[i].PivotY, SurfaceSceneryPainter.PixelsPerCell);
            _stamps[(int)kind] = stamps;
            _shapes[(int)kind] = shapes;
        }

        /// <summary>Some com os sprites do mapa anterior (as plantas do mapa novo nascem em outras colunas).</summary>
        public void Clear()
        {
            foreach (Entry entry in _entries.Values)
                Release(entry);
            _entries.Clear();
            _structureVersion = _markVersion = _plantCount = -1;
        }

        /// <summary>Acompanha as células e as marcas de cada planta. Só confere quando a grade ou a lista de plantas mudou.</summary>
        public void Update(PixelGrid grid, VegetationSimulation vegetation, Vector3 mapOrigin, Color32 markTint, float markStrength)
        {
            IReadOnlyList<int> plants = vegetation.Plants;
            if (grid.StructureVersion == _structureVersion && grid.MarkVersion == _markVersion && plants.Count == _plantCount)
                return;
            _structureVersion = grid.StructureVersion;
            _markVersion = grid.MarkVersion;
            _plantCount = plants.Count;

            foreach (int x in plants)
            {
                if (!_entries.TryGetValue(x, out Entry entry))
                {
                    if (!vegetation.TryGetPlant(x, out PlantKind kind, out PlantShape shape, out int variant))
                        continue;
                    SceneryStamp[] stamps = _stamps[(int)kind];
                    entry = Create(x, stamps[Mathf.Min(variant, stamps.Length - 1)], shape, grid, mapOrigin);
                    _entries.Add(x, entry);
                }
                int sunk = vegetation.GetSunkRows(x);
                if (sunk != entry.Sunk)
                {
                    // A planta subiu (ou rebrotou): o sprite acompanha, a parte enterrada fica atrás do terreno.
                    entry.Sunk = sunk;
                    Vector3 position = entry.Renderer.transform.position;
                    entry.Renderer.transform.position = new Vector3(position.x, entry.BaseY - sunk, position.z);
                }
                Refresh(entry, grid, x, markTint, markStrength);
            }
        }

        Entry Create(int x, SceneryStamp stamp, PlantShape shape, PixelGrid grid, Vector3 mapOrigin)
        {
            int ppc = SurfaceSceneryPainter.PixelsPerCell;
            var entry = new Entry
            {
                Stamp = stamp,
                Shape = shape,
                Pixels = new Color32[stamp.Pixels.Length],
                State = new byte[shape.Cells.Length],
            };
            // Começa vazia: Refresh desenha as células presentes. O que fica abaixo do chão (enterrado, atrás do terreno)
            // é copiado uma vez, para a base não aparecer cortada onde o chão desce.
            for (int py = 0; py < Mathf.Min(stamp.PivotY, stamp.Height); py++)
                System.Array.Copy(stamp.Pixels, py * stamp.Width, entry.Pixels, py * stamp.Width, stamp.Width);

            entry.Texture = new Texture2D(stamp.Width, stamp.Height, TextureFormat.RGBA32, false)
            {
                name = $"Plant_{x}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pivot = new Vector2(stamp.PivotX / (float)stamp.Width, stamp.PivotY / (float)stamp.Height);
            entry.Sprite = Sprite.Create(entry.Texture, new Rect(0, 0, stamp.Width, stamp.Height), pivot, ppc, 0, SpriteMeshType.FullRect);
            entry.Sprite.name = entry.Texture.name;

            var go = new GameObject($"Plant_{x}") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(_parent, false);
            entry.Renderer = go.AddComponent<SpriteRenderer>();
            entry.Renderer.sortingOrder = _sortingOrder;
            entry.Renderer.sprite = entry.Sprite;
            // Pivô no centro da coluna da raiz, no topo do chão: as células da forma caem sobre as da grade.
            entry.BaseY = mapOrigin.y + grid.GetSurfaceY(x) + 1;
            go.transform.position = new Vector3(mapOrigin.x + x + 0.5f, entry.BaseY, mapOrigin.z);
            // Força o primeiro desenho de todas as células.
            for (int i = 0; i < entry.State.Length; i++)
                entry.State[i] = byte.MaxValue;
            return entry;
        }

        static void Refresh(Entry entry, PixelGrid grid, int x, Color32 markTint, float markStrength)
        {
            int rootY = grid.GetSurfaceY(x) + 1;
            bool changed = false;
            Vector2Int[] cells = entry.Shape.Cells;
            for (int i = 0; i < cells.Length; i++)
            {
                int cx = x + cells[i].x, cy = rootY + cells[i].y - entry.Sunk;
                byte state = cy < rootY ? (byte)0 : CellMaterials.IsPlant(grid.Get(cx, cy)) ? grid.IsMarked(cx, cy) ? (byte)2 : (byte)1 : (byte)0;
                if (state == entry.State[i])
                    continue;
                entry.State[i] = state;
                PaintCell(entry, cells[i], state, markTint, markStrength);
                changed = true;
            }
            if (!changed)
                return;
            entry.Texture.SetPixels32(entry.Pixels);
            entry.Texture.Apply(false);
        }

        /// <summary>Copia (ou apaga) o bloco de pixels da célula; marcada, puxa a cor para a tinta de marca.</summary>
        static void PaintCell(Entry entry, Vector2Int cell, byte state, Color32 markTint, float markStrength)
        {
            SceneryStamp stamp = entry.Stamp;
            int ppc = SurfaceSceneryPainter.PixelsPerCell;
            int x0 = stamp.PivotX + cell.x * ppc - ppc / 2;
            int y0 = stamp.PivotY + cell.y * ppc;
            for (int py = Mathf.Max(0, y0); py < Mathf.Min(stamp.Height, y0 + ppc); py++)
            for (int px = Mathf.Max(0, x0); px < Mathf.Min(stamp.Width, x0 + ppc); px++)
            {
                int i = py * stamp.Width + px;
                Color32 c = state == 0 ? default : stamp.Pixels[i];
                if (state == 2 && c.a > 0)
                {
                    Color32 t = Color32.Lerp(c, markTint, markStrength);
                    c = new Color32(t.r, t.g, t.b, c.a);
                }
                entry.Pixels[i] = c;
            }
        }

        static void Release(Entry entry)
        {
            if (entry.Renderer != null)
                Object.Destroy(entry.Renderer.gameObject);
            if (entry.Sprite != null)
                Object.Destroy(entry.Sprite);
            if (entry.Texture != null)
                Object.Destroy(entry.Texture);
        }

        public void Dispose() => Clear();
    }
}
