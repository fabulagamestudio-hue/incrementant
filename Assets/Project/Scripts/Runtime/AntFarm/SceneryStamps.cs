using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>Elementos isolados do cenário da superfície. Cada um tem sua pasta de PNGs (ver SceneryArtSet).</summary>
    public enum SceneryElement
    {
        /// <summary>Touceira de grama alta atrás do formigário.</summary>
        Grass,
        /// <summary>Tufo baixo e escuro na frente das formigas.</summary>
        Tuft,
        Flower,
        /// <summary>Um raminho de trevo; o cenário junta de 1 a 3.</summary>
        Clover,
        /// <summary>Um cogumelo; o cenário junta de 1 a 3.</summary>
        Mushroom,
        Twig,
        DryLeaf,
        /// <summary>Pedrinha apoiada no chão; o cenário a enterra um pouco.</summary>
        Pebble,
        /// <summary>Grão ou migalha de menos de 1 célula.</summary>
        Grain,
    }

    /// <summary>
    /// Pixels de um elemento isolado. O pivô (<see cref="PivotX"/>, <see cref="PivotY"/>, em pixels a partir do canto
    /// inferior esquerdo) é o ponto de apoio no topo do solo; as linhas abaixo dele ficam enterradas.
    /// </summary>
    public sealed class SceneryStamp
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Pixels;
        public readonly int PivotX;
        public readonly int PivotY;
        /// <summary>Arte a 16 px/célula, desenhada separadamente da máscara de navegação.</summary>
        public readonly Sprite VisualSprite;
        /// <summary>Extensão opaca perto da linha do chão (1 célula acima e abaixo do pivô), em pixels relativos ao pivô.</summary>
        public readonly int BaseLeft;
        public readonly int BaseRight;

        public SceneryStamp(int width, int height, Color32[] pixels, int pivotX, int pivotY, Sprite visualSprite = null)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
            PivotX = pivotX;
            PivotY = pivotY;
            VisualSprite = visualSprite;

            int band = SurfaceSceneryPainter.PixelsPerCell;
            int min = int.MaxValue, max = int.MinValue;
            for (int y = Mathf.Max(0, pivotY - band); y < Mathf.Min(height, pivotY + band); y++)
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a == 0)
                    continue;
                min = Mathf.Min(min, x);
                max = Mathf.Max(max, x);
            }
            if (max < min)
            {
                min = 0;
                max = width - 1;
            }
            BaseLeft = min - pivotX;
            BaseRight = max - pivotX;
        }

        /// <summary>
        /// Escala da arte do cenário (pedido do desenvolvedor em 2026-10-17): cada pixel dos PNGs vira
        /// <c>ArtScale</c> x <c>ArtScale</c> pixels do cenário, então a arte fica <c>ArtScale</c> vezes maior (um pixel da
        /// arte = <c>ArtScale</c> pixels da formiga). Vale só para a arte em PNG; a arte procedural já é pintada na grade.
        /// </summary>
        public const int ArtScale = 6;

        /// <summary>Cópia com cada pixel repetido <paramref name="factor"/> x <paramref name="factor"/> vezes (o pivô acompanha).</summary>
        public SceneryStamp Scaled(int factor)
        {
            if (factor <= 1)
                return this;
            int w = Width * factor, h = Height * factor;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = Pixels[(y / factor) * Width + x / factor];
            return new SceneryStamp(w, h, pixels, PivotX * factor, PivotY * factor);
        }

        /// <summary>Lê um sprite importado com Read/Write; o pivô do sprite é o ponto de apoio. Null se a textura não for legível.</summary>
        public static SceneryStamp FromSprite(Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            if (!texture.isReadable)
                return null;
            Rect r = sprite.rect;
            int w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
            Color32[] colors = texture.GetPixels32();
            int sourceX = Mathf.RoundToInt(r.x), sourceY = Mathf.RoundToInt(r.y);
            if (Mathf.Approximately(sprite.pixelsPerUnit, 16f))
            {
                // A máscara continua na grade de 6 px/célula; a arte conserva os 16 px/célula.
                int maskWidth = Mathf.Max(1, Mathf.RoundToInt(w * ArtScale / sprite.pixelsPerUnit));
                int maskHeight = Mathf.Max(1, Mathf.RoundToInt(h * ArtScale / sprite.pixelsPerUnit));
                var mask = new Color32[maskWidth * maskHeight];
                for (int y = 0; y < maskHeight; y++)
                for (int x = 0; x < maskWidth; x++)
                {
                    // Cobertura conservadora: pontas e caules de um texel não desaparecem entre amostras.
                    int x0 = x * w / maskWidth, x1 = Mathf.Min(w, Mathf.CeilToInt((x + 1f) * w / maskWidth));
                    int y0 = y * h / maskHeight, y1 = Mathf.Min(h, Mathf.CeilToInt((y + 1f) * h / maskHeight));
                    bool found = false;
                    for (int sy = y0; sy < y1 && !found; sy++)
                    for (int sx = x0; sx < x1; sx++)
                    {
                        Color32 color = colors[(sourceY + sy) * texture.width + sourceX + sx];
                        if (color.a == 0) continue;
                        mask[y * maskWidth + x] = color;
                        found = true;
                        break;
                    }
                }
                return new SceneryStamp(maskWidth, maskHeight, mask,
                    Mathf.RoundToInt(sprite.pivot.x * ArtScale / sprite.pixelsPerUnit),
                    Mathf.RoundToInt(sprite.pivot.y * ArtScale / sprite.pixelsPerUnit), sprite);
            }
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
                System.Array.Copy(colors, (sourceY + y) * texture.width + sourceX, pixels, y * w, w);
            return new SceneryStamp(w, h, pixels, Mathf.RoundToInt(sprite.pivot.x), Mathf.RoundToInt(sprite.pivot.y)).Scaled(ArtScale);
        }
    }

    /// <summary>Variantes de cada <see cref="SceneryElement"/> que o cenário sorteia ao posicionar.</summary>
    public sealed class SceneryStampLibrary
    {
        /// <summary>Seed das variantes procedurais (as mesmas exportadas como PNG pelo Editor).</summary>
        public const int DefaultSeed = 2026;

        static readonly int ElementCount = System.Enum.GetValues(typeof(SceneryElement)).Length;
        static SceneryStampLibrary _default;

        readonly SceneryStamp[][] _stamps = new SceneryStamp[ElementCount][];

        /// <summary>Variantes procedurais, criadas na primeira vez que forem pedidas. Usadas quando falta arte.</summary>
        public static SceneryStampLibrary Default => _default ??= CreateProcedural(DefaultSeed);

        /// <summary>Variantes do elemento (vazio se não houver).</summary>
        public SceneryStamp[] Get(SceneryElement element) => _stamps[(int)element] ?? System.Array.Empty<SceneryStamp>();

        public void Set(SceneryElement element, SceneryStamp[] stamps) => _stamps[(int)element] = stamps;

        /// <summary>Quantas variantes procedurais cada elemento ganha.</summary>
        public static int VariantCount(SceneryElement element) => element switch
        {
            SceneryElement.Grass => 10,
            SceneryElement.Tuft => 8,
            SceneryElement.Flower => 4,
            SceneryElement.Clover => 6,
            SceneryElement.Mushroom => 8,
            SceneryElement.Twig => 5,
            SceneryElement.DryLeaf => 6,
            SceneryElement.Pebble => 12,
            SceneryElement.Grain => 8,
            _ => 4,
        };

        public static SceneryStampLibrary CreateProcedural(int seed)
        {
            var library = new SceneryStampLibrary();
            var rng = new System.Random(seed);
            var canvas = SurfaceSceneryPainter.CreateElementCanvas();
            foreach (SceneryElement element in System.Enum.GetValues(typeof(SceneryElement)))
            {
                var stamps = new SceneryStamp[VariantCount(element)];
                for (int i = 0; i < stamps.Length; i++)
                    stamps[i] = SurfaceSceneryPainter.PaintElement(element, rng, canvas);
                library.Set(element, stamps);
            }
            return library;
        }
    }
}
