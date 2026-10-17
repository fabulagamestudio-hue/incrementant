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
        /// <summary>Extensão opaca perto da linha do chão (1 célula acima e abaixo do pivô), em pixels relativos ao pivô.</summary>
        public readonly int BaseLeft;
        public readonly int BaseRight;

        public SceneryStamp(int width, int height, Color32[] pixels, int pivotX, int pivotY)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
            PivotX = pivotX;
            PivotY = pivotY;

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

        /// <summary>Lê um sprite importado com Read/Write; o pivô do sprite é o ponto de apoio. Null se a textura não for legível.</summary>
        public static SceneryStamp FromSprite(Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            if (!texture.isReadable)
                return null;
            Rect r = sprite.rect;
            int w = Mathf.RoundToInt(r.width), h = Mathf.RoundToInt(r.height);
            Color[] colors = texture.GetPixels(Mathf.RoundToInt(r.x), Mathf.RoundToInt(r.y), w, h);
            var pixels = new Color32[colors.Length];
            for (int i = 0; i < colors.Length; i++)
                pixels[i] = colors[i];
            return new SceneryStamp(w, h, pixels, Mathf.RoundToInt(sprite.pivot.x), Mathf.RoundToInt(sprite.pivot.y));
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
