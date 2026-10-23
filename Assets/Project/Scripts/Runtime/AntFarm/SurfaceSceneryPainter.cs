using System;
using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Quantidade de cada elemento do cenário da superfície, por 100 colunas do bioma a que ele pertence (documento 44,
    /// "Cenário na geração do mapa"): elemento grande só no bioma dele e gramado central limpo. Pedras, cogumelos e grãos
    /// saíram da geração em 2026-10-23 e voltam com a margem úmida.
    /// </summary>
    [Serializable]
    public sealed class SurfaceScenerySettings
    {
        // Grama, trevos e flores não são cenário: são plantas colhíveis (VegetationSimulation + PlantSprites), só no gramado.
        [Tooltip("Gravetos por 100 colunas da Serapilheira.")]
        [SerializeField, Min(0f)] float _litterTwigs = 1.2f;
        [Tooltip("Folhas secas grandes por 100 colunas da Serapilheira.")]
        [SerializeField, Min(0f)] float _litterDryLeaves = 2f;

        public float LitterTwigs => _litterTwigs;
        public float LitterDryLeaves => _litterDryLeaves;
    }

    /// <summary>
    /// Pixels de uma camada do cenário. Origem no canto inferior esquerdo; <see cref="OriginY"/> em células do mapa.
    /// Com <see cref="WrapX"/>, a camada é um ladrilho que se repete na horizontal (o que sai de um lado entra no outro).
    /// </summary>
    public sealed class SceneryLayer
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int OriginY;
        public readonly int PixelsPerCell;
        public readonly bool WrapX;
        public readonly Color32[] Pixels;
        /// <summary>Props detalhados: a posição usa pixels desta camada, com origem inferior esquerda.</summary>
        public readonly List<(SceneryStamp Stamp, int Left, int Bottom)> SpriteStamps = new();

        public SceneryLayer(int width, int height, int originY, int pixelsPerCell, bool wrapX = false)
        {
            Width = width;
            Height = height;
            OriginY = originY;
            PixelsPerCell = pixelsPerCell;
            WrapX = wrapX;
            Pixels = new Color32[width * height];
        }

        public void Fill(Color32 color)
        {
            for (int i = 0; i < Pixels.Length; i++)
                Pixels[i] = color;
        }

        public void Put(int x, int y, Color32 color)
        {
            if (WrapX)
                x = ((x % Width) + Width) % Width;
            if ((uint)x < (uint)Width && (uint)y < (uint)Height)
            {
                Pixels[y * Width + x] = color;
                if (x < MinX) MinX = x;
                if (x > MaxX) MaxX = x;
                if (y < MinY) MinY = y;
                if (y > MaxY) MaxY = y;
            }
        }

        /// <summary>Retângulo dos pixels gravados com <see cref="Put"/> desde o último <see cref="ResetBounds"/> (vazio se MaxX &lt; MinX).</summary>
        public int MinX { get; private set; } = int.MaxValue;
        public int MaxX { get; private set; } = int.MinValue;
        public int MinY { get; private set; } = int.MaxValue;
        public int MaxY { get; private set; } = int.MinValue;

        public void ResetBounds()
        {
            MinX = MinY = int.MaxValue;
            MaxX = MaxY = int.MinValue;
        }

        public Color32 Get(int x, int y) =>
            (uint)x < (uint)Width && (uint)y < (uint)Height ? Pixels[y * Width + x] : default;
    }

    /// <summary>
    /// Camadas do cenário, de trás para a frente: céu (coluna de 1 pixel com o gradiente, esticada na vista),
    /// grama distante e grama média (ladrilhos horizontais para o paralaxe; <see cref="SceneryLayer.OriginY"/> é a
    /// base do ladrilho com a câmera na âncora), perto (atrás do terreno) e frente (sobre as formigas).
    /// </summary>
    public readonly struct SceneryLayers
    {
        public readonly SceneryLayer Sky;
        public readonly SceneryLayer Far;
        public readonly SceneryLayer Mid;
        public readonly SceneryLayer Near;
        public readonly SceneryLayer Front;
        /// <summary>
        /// Células cobertas pela arte de cogumelos, galhos, folhas secas e pedrinhas (sem os grãos): índice
        /// <c>(y - Near.OriginY) * larguraDoMapa + x</c>. Vira o fundo caminhável da grade (<see cref="SurfaceSceneryPainter.ApplyBackdrop"/>).
        /// </summary>
        public readonly bool[] Backdrop;

        public SceneryLayers(SceneryLayer sky, SceneryLayer far, SceneryLayer mid, SceneryLayer near, SceneryLayer front,
                             bool[] backdrop = null)
        {
            Sky = sky;
            Far = far;
            Mid = mid;
            Near = near;
            Front = front;
            Backdrop = backdrop ?? Array.Empty<bool>();
        }
    }

    /// <summary>
    /// Monta o cenário da superfície em pixel art na mesma densidade das formigas (<see cref="PixelsPerCell"/>),
    /// na escala delas: com a formiga de 3 células (~5 mm), 1 célula ≈ 1,7 mm, então uma folha de grama passa de
    /// 25 células e sai pelo alto do mapa, um trevo tem ~12 de largura e as pedrinhas têm de 1 a 6.
    /// Grama, flores, trevos, cogumelos, galhos, folhas secas, pedrinhas e grãos são carimbos isolados
    /// (<see cref="SceneryStamp"/>, vindos dos PNGs ou de <see cref="PaintElement"/>) que <see cref="Paint"/> só posiciona;
    /// céu e faixas distantes são pintados aqui. Nada entra na grade como material; só a área coberta pelos elementos
    /// (<see cref="SceneryLayers.Backdrop"/>) vira fundo caminhável para as formigas. Determinístico pela seed.
    /// </summary>
    public static class SurfaceSceneryPainter
    {
        /// <summary>
        /// Texels por célula das camadas de perto e da frente e das plantas: cada pixel dos PNGs (1 célula) vira um bloco
        /// destes. Na tela, o shader Incrementant/SceneryCells desenha cada célula a _DetailTexels texels (8 no experimento 8x8;
        /// antes 16, etapa 10 do documento 40).
        /// </summary>
        public const int PixelsPerCell = 6;
        /// <summary>
        /// Texels por célula das faixas de grama distante, pintadas por código: a densidade do mundo
        /// (<see cref="CellTileArt.TexelsPerCell"/>, 8 no experimento 8x8 de 2026-10-22; antes 16, documento 40).
        /// </summary>
        public const int BandPixelsPerCell = CellTileArt.TexelsPerCell;
        /// <summary>Células abaixo do solo mais baixo incluídas nas camadas, para as bases ficarem enterradas atrás do terreno.</summary>
        const int Embed = 3;
        /// <summary>Células de céu acima do topo do mapa quando quem chama não informa a folga da câmera.</summary>
        public const int DefaultSkyAbove = 40;
        /// <summary>Altura máxima, em células acima de <c>originY</c>, das camadas de perto e da frente.</summary>
        public const int NearLayerRows = 260;

        /// <summary>Largura do ladrilho das faixas de grama distante, em células.</summary>
        public const int BandTileCells = 160;
        /// <summary>
        /// Células de campo liso abaixo da base das faixas distantes: com o paralaxe a faixa sobe menos que o terreno
        /// quando a câmera sobe, e esse campo cobre o vão entre ela e o solo.
        /// </summary>
        public const int BandFill = 72;

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        /// <param name="skyBaseY">Linha do mapa onde o gradiente do céu tem a cor de baixo; o topo do mapa tem a de cima.</param>
        /// <param name="art">Carimbos de cada elemento; sem ele (ou num elemento sem carimbos) usa <see cref="SceneryStampLibrary.Default"/>.</param>
        /// <param name="isLakeColumn">Colunas com laguinho (opcional): nada do cenário perto/frente nasce nelas.</param>
        /// <param name="skyAbove">Células de céu pintadas acima do topo do mapa; cubra até onde a câmera pode subir.</param>
        public static SceneryLayers Paint(PixelGrid grid, TerrainLayout layout, SurfaceScenerySettings settings, int seed,
                                          Color32 skyTop, Color32 skyBottom, int skyBaseY = 0, SceneryStampLibrary art = null,
                                          Func<int, bool> isLakeColumn = null, int skyAbove = DefaultSkyAbove)
        {
            int minSurface = grid.Height - 1;
            for (int x = 0; x < grid.Width; x++)
                minSurface = Mathf.Min(minSurface, grid.GetSurfaceY(x));
            int originY = Mathf.Max(0, minSurface - Embed);
            int width = grid.Width * PixelsPerCell;
            // As camadas de perto e da frente só guardam a faixa onde há cenário (cogumelos, galhos, pedras); o céu extra
            // acima do mundo (TerrainSettings.SkyHeadroom) não precisa de textura.
            int height = Mathf.Clamp(grid.Height - originY, 1, NearLayerRows) * PixelsPerCell;

            var rng = new System.Random(seed);
            var painter = new Painter(grid, originY) { Skip = isLakeColumn };

            var sky = new SceneryLayer(1, (grid.Height + Mathf.Max(0, skyAbove) - originY) * PixelsPerCell, originY, PixelsPerCell);
            PaintSky(sky, skyBaseY, grid.Height - 1, skyTop, skyBottom);

            // Duas faixas de grama distante, cada vez mais apagadas na cor do céu (perspectiva atmosférica).
            // A base fica no nível do solo da entrada, que é a altura do horizonte com a câmera no enquadramento inicial.
            int horizon = grid.GetSurfaceY(layout.EntranceX) + 1;
            SceneryLayer far = DistantBand(rng, horizon, skyBottom, haze: 0.72f, minHeight: 30f, maxHeight: 60f, minStep: 1.2f, maxStep: 2.4f);
            SceneryLayer mid = DistantBand(rng, horizon, skyBottom, haze: 0.48f, minHeight: 14f, maxHeight: 38f, minStep: 0.8f, maxStep: 1.8f);

            SceneryStamp Pick(SceneryElement element)
            {
                SceneryStamp[] stamps = art?.Get(element);
                if (stamps == null || stamps.Length == 0)
                    stamps = SceneryStampLibrary.Default.Get(element);
                return stamps[rng.Next(stamps.Length)];
            }

            var near = new SceneryLayer(width, height, originY, PixelsPerCell);
            near.Fill(Clear);
            painter.Target = near;
            // Sem grama, trevos e flores: toda planta da superfície é colhível e desenhada por PlantSprites.
            // Com a arte em escala de célula (SceneryStamp.ArtScale), cada elemento tem dezenas de células de largura:
            // posições são sorteadas sem sobrepor os já postos (FindX), em vez de caírem umas sobre as outras.
            // Cada elemento grande só nasce inteiro dentro do bioma dele (documento 44); o gramado fica limpo.
            SurfaceBiomes biomes = grid.Biomes;
            float litterPer100 = CountColumns(biomes, SurfaceBiome.LeafLitter) / 100f;
            var bigUsed = new List<Vector2>();
            void PlaceIn(SurfaceBiome biome, SceneryStamp stamp, List<Vector2> used, float gap)
            {
                if (FindX(rng, grid.Width, stamp, used, gap, out float x, biomes, biome))
                    painter.Place(stamp, x, 0f);
            }
            Repeat(rng, settings.LitterTwigs * litterPer100, () => PlaceIn(SurfaceBiome.LeafLitter, Pick(SceneryElement.Twig), bigUsed, 12f));
            Repeat(rng, settings.LitterDryLeaves * litterPer100, () => PlaceIn(SurfaceBiome.LeafLitter, Pick(SceneryElement.DryLeaf), bigUsed, 12f));
            bool[] backdrop = CoveredCells(near, grid.Width);

            // Camada da frente (na frente das formigas) vazia: os tufos que ficavam nela viraram plantas colhíveis
            // (variantes da grama em PlantSprites), já que tudo o que é grama em primeiro plano pode ser colhido.
            var front = new SceneryLayer(width, height, originY, PixelsPerCell);
            front.Fill(Clear);

            return new SceneryLayers(sky, far, mid, near, front, backdrop);
        }

        /// <summary>Células (de <paramref name="cellColumns"/> colunas) com algum pixel opaco da camada.</summary>
        static bool[] CoveredCells(SceneryLayer layer, int cellColumns)
        {
            int rows = layer.Height / PixelsPerCell;
            var covered = new bool[cellColumns * rows];
            for (int py = 0; py < layer.Height; py++)
            {
                int row = py * layer.Width;
                int cellRow = (py / PixelsPerCell) * cellColumns;
                for (int px = 0; px < layer.Width; px++)
                {
                    if (layer.Pixels[row + px].a != 0)
                        covered[cellRow + px / PixelsPerCell] = true;
                }
            }
            foreach (var placed in layer.SpriteStamps)
            {
                SceneryStamp stamp = placed.Stamp;
                for (int y = 0; y < stamp.Height; y++)
                for (int x = 0; x < stamp.Width; x++)
                {
                    int px = placed.Left + x, py = placed.Bottom + y;
                    if ((uint)px < (uint)layer.Width && (uint)py < (uint)layer.Height &&
                        stamp.Pixels[y * stamp.Width + x].a != 0)
                        covered[(py / PixelsPerCell) * cellColumns + px / PixelsPerCell] = true;
                }
            }
            return covered;
        }

        /// <summary>
        /// Passa a área coberta pelo cenário de perto (<see cref="SceneryLayers.Backdrop"/>) para a grade
        /// (<see cref="PixelGrid.SetBackdrop"/>), tirando a do mapa anterior. A formiga anda por ela como pelo fundo do túnel.
        /// </summary>
        public static void ApplyBackdrop(PixelGrid grid, SceneryLayers layers)
        {
            grid.ClearBackdrop();
            bool[] backdrop = layers.Backdrop;
            if (backdrop.Length == 0)
                return;
            int originY = layers.Near.OriginY;
            for (int i = 0; i < backdrop.Length; i++)
            {
                if (backdrop[i])
                    grid.SetBackdrop(i % grid.Width, originY + i / grid.Width, true);
            }
        }

        /// <summary>Pedrinha meio enterrada: <paramref name="bury"/> = 1 afunda 70% do raio vertical.</summary>
        static void PlacePebble(Painter p, SceneryStamp stamp, float x, float bury) =>
            p.Place(stamp, x, sink: bury * 0.7f * stamp.Height / (2f * PixelsPerCell));

        // ---------- Carimbos ----------

        /// <summary>Tela onde os elementos são pintados antes do recorte, em células; o chão fica em <see cref="CanvasGround"/>.</summary>
        const int CanvasWidth = 200;
        const int CanvasHeight = 120;
        const int CanvasGround = 20;

        /// <summary>
        /// Pinta uma variante de <paramref name="element"/> sobre chão plano e recorta: o pivô é o ponto de apoio no chão
        /// (centro do elemento na horizontal, topo do solo na vertical); o que fica abaixo dele é a parte enterrada.
        /// </summary>
        public static SceneryStamp PaintElement(SceneryElement element, int seed) =>
            PaintElement(element, new System.Random(seed), CreateElementCanvas());

        /// <summary>Tela reaproveitável para várias chamadas de <see cref="PaintElement(SceneryElement, System.Random, SceneryLayer)"/>.</summary>
        internal static SceneryLayer CreateElementCanvas() =>
            new SceneryLayer(CanvasWidth * PixelsPerCell, CanvasHeight * PixelsPerCell, 0, PixelsPerCell);

        internal static SceneryStamp PaintElement(SceneryElement element, System.Random rng, SceneryLayer canvas)
        {
            // A tela chega limpa (nova ou limpa no fim da chamada anterior): limpar só o recorte é bem mais barato.
            var p = new Painter(_ => CanvasGround, 0) { Target = canvas };
            float x = CanvasWidth * 0.5f;
            switch (element)
            {
                case SceneryElement.Grass: GrassClump(p, rng, x, tall: true); break;
                case SceneryElement.Tuft: GrassClump(p, rng, x, tall: false); break;
                case SceneryElement.Flower: Flower(p, rng, x); break;
                case SceneryElement.Clover: Clover(p, rng, x); break;
                case SceneryElement.Mushroom: Mushroom(p, rng, x, rng.NextDouble() < 0.4 ? 1f : Range(rng, 0.45f, 0.8f)); break;
                case SceneryElement.Twig: Twig(p, rng, x); break;
                case SceneryElement.DryLeaf: DryLeaf(p, rng, x); break;
                case SceneryElement.Pebble: Pebble(p, rng, x); break;
                case SceneryElement.Grain: Grain(p, rng, x); break;
                default: throw new ArgumentOutOfRangeException(nameof(element), element, null);
            }

            if (canvas.MaxX < canvas.MinX)
                throw new InvalidOperationException($"Elemento {element} não pintou nenhum pixel.");
            int w = canvas.MaxX - canvas.MinX + 1, h = canvas.MaxY - canvas.MinY + 1;
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                int row = (canvas.MinY + y) * canvas.Width + canvas.MinX;
                Array.Copy(canvas.Pixels, row, pixels, y * w, w);
                Array.Clear(canvas.Pixels, row, w);
            }
            int pivotX = Mathf.FloorToInt(x * PixelsPerCell) - canvas.MinX;
            int pivotY = CanvasGround * PixelsPerCell - canvas.MinY;
            canvas.ResetBounds();
            return new SceneryStamp(w, h, pixels, pivotX, pivotY);
        }

        // ---------- Elementos ----------

        static void PaintSky(SceneryLayer layer, int baseY, int topY, Color32 top, Color32 bottom)
        {
            // Mesmo gradiente do céu do AntFarmView, para o terreno e o fundo combinarem.
            float span = Mathf.Max(1, topY - baseY);
            for (int py = 0; py < layer.Height; py++)
            {
                float y = layer.OriginY + (py + 0.5f) / layer.PixelsPerCell;
                Color32 c = Color32.Lerp(bottom, top, Mathf.Clamp01((y - baseY) / span));
                int row = py * layer.Width;
                for (int px = 0; px < layer.Width; px++)
                    layer.Pixels[row + px] = c;
            }
        }

        /// <summary>
        /// Ladrilho de grama distante, repetível na horizontal: relevo suave periódico, campo liso embaixo
        /// (<see cref="BandFill"/>) e folhas por cima. A base do ladrilho fica a <see cref="BandFill"/> células sob o horizonte.
        /// </summary>
        static SceneryLayer DistantBand(System.Random rng, int horizon, Color32 hazeColor, float haze,
                                        float minHeight, float maxHeight, float minStep, float maxStep)
        {
            int heightCells = BandFill + Mathf.CeilToInt(maxHeight) + 4;
            var band = new SceneryLayer(BandTileCells * BandPixelsPerCell, heightCells * BandPixelsPerCell, horizon - BandFill, BandPixelsPerCell, wrapX: true);
            band.Fill(Clear);

            // Ondulação com período que divide o ladrilho, para emendar sem costura.
            float phaseA = Range(rng, 0f, Mathf.PI * 2f);
            float phaseB = Range(rng, 0f, Mathf.PI * 2f);
            float k = Mathf.PI * 2f / BandTileCells;
            float Ground(float x) => BandFill + 1.2f * Mathf.Sin(2f * k * x + phaseA) + 0.8f * Mathf.Sin(5f * k * x + phaseB);
            var painter = new Painter(Ground, 0) { Target = band };

            Color32 field = Color32.Lerp(Color32.Lerp(Greens[0], hazeColor, haze), Black, 0.06f);
            for (int px = 0; px < band.Width; px++)
            {
                int top = Mathf.FloorToInt((Ground((px + 0.5f) / BandPixelsPerCell) - 1f) * BandPixelsPerCell);
                for (int py = 0; py < top && py < band.Height; py++)
                    band.Pixels[py * band.Width + px] = field;
            }

            for (float x = 0f; x < BandTileCells; x += Range(rng, minStep, maxStep))
            {
                Color32 green = Greens[rng.Next(Greens.Length)];
                Color32 c = Color32.Lerp(green, hazeColor, haze);
                var tones = new Tones(Color32.Lerp(c, Black, 0.08f), c, Color32.Lerp(c, hazeColor, 0.25f));
                painter.Blade(x, Ground(x) - 1f, Range(rng, minHeight, maxHeight), Range(rng, 1.4f, 2.6f), Range(rng, -10f, 10f), tones, rng);
            }
            return band;
        }

        static void GrassClump(Painter p, System.Random rng, float x, bool tall)
        {
            int blades = rng.Next(3, tall ? 8 : 6);
            for (int i = 0; i < blades; i++)
            {
                float bx = x + Range(rng, -3f, 3f) * (tall ? 1f : 0.7f);
                float height = tall ? Range(rng, 25f, 70f) : Range(rng, 5f, 12f);
                float width = tall ? Range(rng, 1.6f, 3f) : Range(rng, 1.1f, 1.8f);
                // Abre em leque: quanto mais longe do centro da touceira, mais a folha tomba para fora.
                float bend = tall
                    ? (bx - x) * Range(rng, 1.5f, 4f) + Range(rng, -10f, 10f)
                    : (bx - x) * 0.8f + Range(rng, -2.4f, 2.4f);
                Color32 g = Greens[rng.Next(Greens.Length)];
                // Os da frente estão mais perto e na sombra: mais escuros.
                if (!tall)
                    g = Color32.Lerp(g, Black, 0.35f);
                p.Blade(bx, p.Ground(bx) - 1f, height, width, bend, Tones.Of(g), rng);
            }
        }

        static void Clover(Painter p, System.Random rng, float x)
        {
            float height = Range(rng, 7f, 15f);
            float bend = Range(rng, -2.5f, 2.5f);
            Color32 g = Color32.Lerp(new Color32(70, 128, 52, 255), Greens[rng.Next(Greens.Length)], 0.3f);
            Tones tones = Tones.Of(g);
            float baseY = p.Ground(x) - 1f;
            p.Stem(x, baseY, height, 0.6f, bend, tones);
            float tx = x + bend, ty = baseY + height;
            // De lado: folíolos laterais achatados e o do meio de frente, com a mancha clara em V do trevo.
            p.Ellipse(tx - 3.6f, ty + 0.4f, 3.4f, 1.5f, tones);
            p.Ellipse(tx + 3.6f, ty + 0.4f, 3.4f, 1.5f, tones);
            p.Ellipse(tx, ty + 2.2f, 3f, 2.6f, tones);
            p.Ellipse(tx, ty + 2.4f, 1.6f, 0.7f, new Tones(tones.Light, tones.Light, tones.Light), outline: false);
        }

        static void Flower(Painter p, System.Random rng, float x)
        {
            float height = Range(rng, 32f, 46f);
            float bend = Range(rng, -5f, 5f);
            float baseY = p.Ground(x) - 1f;
            // Caule mais escuro que a grama, para não sumir no meio das folhas.
            Tones stem = Tones.Of(new Color32(56, 92, 40, 255));
            p.Stem(x, baseY, height, 1.3f, bend, stem);
            // Duas folhas compridas saindo da base.
            p.Blade(x, baseY, Range(rng, 12f, 18f), 2.2f, Range(rng, -12f, -6f), stem, rng);
            p.Blade(x, baseY, Range(rng, 10f, 16f), 2f, Range(rng, 6f, 12f), stem, rng);

            // Margarida de lado: pétalas finas em leque, caídas para baixo, e o miolo em domo por cima.
            float hx = x + bend, hy = baseY + height;
            var petals = new Tones(new Color32(200, 198, 206, 255), new Color32(242, 240, 232, 255), new Color32(255, 255, 250, 255));
            var center = new Tones(new Color32(196, 136, 24, 255), new Color32(236, 182, 40, 255), new Color32(255, 220, 96, 255));
            const int petalCount = 9;
            for (int i = 0; i < petalCount; i++)
            {
                float angle = Mathf.Lerp(-170f, -10f, i / (petalCount - 1f)) * Mathf.Deg2Rad;
                float length = Range(rng, 5.5f, 7f);
                // Achatada na vertical: de lado, o disco de pétalas aparece como uma saia.
                p.Segment(hx, hy + 0.8f, hx + Mathf.Cos(angle) * length, hy + 0.8f + Mathf.Sin(angle) * length * 0.45f, 1.2f, petals, rng, knots: false);
            }
            p.Ellipse(hx, hy + 1.6f, 2.6f, 1.8f, center);
        }

        static void Mushroom(Painter p, System.Random rng, float x, float size)
        {
            float height = Range(rng, 10f, 18f) * size;
            float capRx = Range(rng, 6f, 8f) * size;
            float baseY = p.Ground(x) - 1f;
            var stem = new Tones(new Color32(176, 160, 132, 255), new Color32(222, 208, 180, 255), new Color32(240, 232, 212, 255));
            p.Stem(x, baseY, height, Mathf.Max(1.4f, 2.4f * size), Range(rng, -1f, 1f), stem);
            Color32 cap = rng.NextDouble() < 0.5 ? new Color32(150, 88, 50, 255) : new Color32(188, 132, 76, 255);
            p.Cap(x, baseY + height, capRx, capRx * 0.62f, Tones.Of(cap), new Color32(98, 72, 52, 255));
        }

        static void Twig(Painter p, System.Random rng, float x)
        {
            float length = Range(rng, 30f, 60f);
            float x0 = x - length * 0.5f, x1 = x + length * 0.5f;
            float thickness = Range(rng, 1.4f, 2.4f);
            // Apoiado no ponto mais baixo do trecho, para não ficar boiando.
            float y0 = p.MinGround(x0, x1) - 0.3f + thickness * 0.5f;
            float y1 = y0 + Range(rng, -0.5f, 3f);
            if (rng.NextDouble() < 0.5)
                (y0, y1) = (y1, y0);
            var bark = new Tones(new Color32(70, 48, 32, 255), new Color32(110, 78, 50, 255), new Color32(146, 108, 72, 255));
            p.Segment(x0, y0, x1, y1, thickness, bark, rng);
            // Um galhinho saindo para cima.
            float t = Range(rng, 0.3f, 0.7f);
            float bx = Mathf.Lerp(x0, x1, t), by = Mathf.Lerp(y0, y1, t);
            p.Segment(bx, by, bx + Range(rng, -6f, 6f), by + Range(rng, 4f, 9f), thickness * 0.55f, bark, rng);
        }

        static void DryLeaf(Painter p, System.Random rng, float x)
        {
            float length = Range(rng, 18f, 34f);
            Color32[] colors =
            {
                new Color32(150, 104, 52, 255), new Color32(176, 120, 48, 255),
                new Color32(132, 84, 46, 255), new Color32(168, 140, 70, 255),
            };
            Tones tones = Tones.Of(colors[rng.Next(colors.Length)]);
            float baseY = p.MinGround(x - length * 0.5f, x + length * 0.5f) - 0.2f;
            p.Leaf(x, baseY, length, Range(rng, 1.6f, 2.6f), Range(rng, 2f, 6f), rng.NextDouble() < 0.5, tones);
        }

        /// <summary>Pedrinha apoiada no chão (a base da elipse no topo do solo); quanto enterrar fica para <see cref="PlacePebble"/>.</summary>
        static void Pebble(Painter p, System.Random rng, float x)
        {
            // Viés para pedras pequenas: a maioria do tamanho da cabeça da formiga, algumas maiores que ela.
            float r = Range(rng, 0f, 1f);
            float rx = 0.6f + 5f * r * r;
            float ry = rx * Range(rng, 0.55f, 0.85f);
            Color32[] colors =
            {
                new Color32(128, 124, 130, 255), new Color32(150, 140, 126, 255),
                new Color32(110, 104, 100, 255), new Color32(170, 158, 136, 255),
            };
            p.Ellipse(x, p.Ground(x) + ry, rx, ry, Tones.Of(colors[rng.Next(colors.Length)]));
        }

        static void Grain(Painter p, System.Random rng, float x)
        {
            float rx = Range(rng, 0.35f, 0.8f);
            Color32[] colors =
            {
                new Color32(196, 172, 120, 255), new Color32(140, 110, 80, 255), new Color32(210, 200, 170, 255),
            };
            p.Ellipse(x, p.Ground(x) + rx * 0.5f, rx, rx * 0.8f, Tones.Of(colors[rng.Next(colors.Length)]), outline: false);
        }

        // ---------- Utilidades ----------

        static readonly Color32 Black = new Color32(0, 0, 0, 255);
        static readonly Color32 White = new Color32(255, 255, 255, 255);

        static readonly Color32[] Greens =
        {
            new Color32(86, 122, 52, 255), new Color32(72, 112, 44, 255), new Color32(102, 138, 58, 255),
            new Color32(118, 140, 62, 255), new Color32(64, 100, 50, 255),
        };

        static float Range(System.Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

        /// <summary>
        /// Sorteia uma posição em que o carimbo (largura em células, mais <paramref name="gap"/> de cada lado) não cruza
        /// nenhum intervalo de <paramref name="used"/> e a registra. Falha (false) depois de 40 tentativas: o elemento some.
        /// </summary>
        /// <param name="biomes">Com ele, só vale x em que o elemento inteiro (sem a folga) fica dentro de <paramref name="biome"/>.</param>
        static bool FindX(System.Random rng, int mapWidth, SceneryStamp stamp, List<Vector2> used, float gap, out float x,
                          SurfaceBiomes biomes = null, SurfaceBiome biome = SurfaceBiome.Meadow)
        {
            float left = -stamp.PivotX / (float)PixelsPerCell - gap;
            float right = (stamp.Width - stamp.PivotX) / (float)PixelsPerCell + gap;
            // O bioma ocupa só parte do mapa: mais tentativas, para a mesma chance de achar lugar.
            int attempts = biomes == null ? 40 : 160;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                x = RandomX(rng, mapWidth);
                if (biomes != null && !Inside(biomes, biome, x + left + gap, x + right - gap))
                    continue;
                bool free = true;
                foreach (Vector2 u in used)
                {
                    if (x + right > u.x && x + left < u.y)
                    {
                        free = false;
                        break;
                    }
                }
                if (!free)
                    continue;
                used.Add(new Vector2(x + left, x + right));
                return true;
            }
            x = 0f;
            return false;
        }

        static bool Inside(SurfaceBiomes biomes, SurfaceBiome biome, float from, float to)
        {
            for (int c = Mathf.FloorToInt(from), end = Mathf.FloorToInt(to); c <= end; c++)
                if (biomes.At(c) != biome)
                    return false;
            return true;
        }

        static int CountColumns(SurfaceBiomes biomes, SurfaceBiome biome)
        {
            int count = 0;
            for (int x = 0; x < biomes.Width; x++)
                if (biomes.At(x) == biome)
                    count++;
            return count;
        }

        static float RandomX(System.Random rng, int mapWidth) => Range(rng, 2f, mapWidth - 2f);

        /// <summary>Executa <paramref name="action"/> round(count) vezes, sorteando a fração.</summary>
        static void Repeat(System.Random rng, float count, Action action)
        {
            int n = Mathf.FloorToInt(count) + (rng.NextDouble() < count - Mathf.Floor(count) ? 1 : 0);
            for (int i = 0; i < n; i++)
                action();
        }

        readonly struct Tones
        {
            public readonly Color32 Dark;
            public readonly Color32 Base;
            public readonly Color32 Light;

            public Tones(Color32 dark, Color32 baseColor, Color32 light)
            {
                Dark = dark;
                Base = baseColor;
                Light = light;
            }

            public static Tones Of(Color32 c) => new Tones(Color32.Lerp(c, Black, 0.3f), c, Color32.Lerp(c, White, 0.22f));
        }

        /// <summary>Desenha em coordenadas de célula do mapa (float) numa camada em pixels.</summary>
        sealed class Painter
        {
            readonly Func<float, float> _ground;
            readonly int _originY;
            public SceneryLayer Target;

            /// <summary>Texels por célula da camada de destino (as faixas distantes têm mais que as de perto).</summary>
            float P => Target != null ? Target.PixelsPerCell : PixelsPerCell;

            /// <summary>Colunas onde nada é carimbado (pivô nelas). Opcional.</summary>
            public Func<int, bool> Skip;

            public Painter(PixelGrid grid, int originY)
                : this(x => grid.GetSurfaceY(Mathf.Clamp(Mathf.FloorToInt(x), 0, grid.Width - 1)) + 1f, originY)
            {
            }

            /// <param name="ground">Topo do chão (em células) para cada x.</param>
            public Painter(Func<float, float> ground, int originY)
            {
                _ground = ground;
                _originY = originY;
            }

            /// <summary>Topo do solo original na coluna (borda de cima da célula de superfície).</summary>
            public float Ground(float x) => _ground(x);

            public float MinGround(float x0, float x1)
            {
                float min = float.MaxValue;
                for (float x = x0; x <= x1; x += 1f)
                    min = Mathf.Min(min, Ground(x));
                return Mathf.Min(min, Ground(x1));
            }

            int PxX(float x) => Mathf.FloorToInt(x * P);
            int PxY(float y) => Mathf.FloorToInt((y - _originY) * P);

            /// <summary>
            /// Carimba <paramref name="stamp"/> com o pivô em <paramref name="x"/>, apoiado no ponto mais baixo do chão
            /// sob a base (para não boiar em desnível) e afundado <paramref name="sink"/> células. Alfa parcial é composto por cima.
            /// </summary>
            public void Place(SceneryStamp stamp, float x, float sink = 0f)
            {
                if (Skip != null && Skip(Mathf.FloorToInt(x)))
                    return;
                float ground = MinGround(x + stamp.BaseLeft / P, x + stamp.BaseRight / P) - sink;
                int left = PxX(x) - stamp.PivotX;
                int bottom = PxY(ground) - stamp.PivotY;
                if (stamp.VisualSprite != null)
                {
                    Target.SpriteStamps.Add((stamp, left, bottom));
                    return;
                }
                for (int y = 0; y < stamp.Height; y++)
                for (int sx = 0; sx < stamp.Width; sx++)
                {
                    Color32 c = stamp.Pixels[y * stamp.Width + sx];
                    if (c.a == 0)
                        continue;
                    int px = left + sx, py = bottom + y;
                    Target.Put(px, py, c.a == 255 ? c : Over(c, Target.Get(px, py)));
                }
            }

            static Color32 Over(Color32 src, Color32 dst)
            {
                float sa = src.a / 255f, da = dst.a / 255f * (1f - sa), a = sa + da;
                if (a <= 0f)
                    return default;
                return new Color32((byte)((src.r * sa + dst.r * da) / a), (byte)((src.g * sa + dst.g * da) / a),
                                   (byte)((src.b * sa + dst.b * da) / a), (byte)(a * 255f));
            }

            /// <summary>Folha de grama: afina até a ponta e curva com o quadrado da altura. Sombra à esquerda, luz à direita.</summary>
            public void Blade(float x, float baseY, float height, float width, float bend, Tones tones, System.Random rng)
            {
                int rows = Mathf.Max(1, Mathf.CeilToInt(height * P));
                int y0 = PxY(baseY);
                int seed = rng.Next();
                for (int i = 0; i < rows; i++)
                {
                    float t = i / (float)rows;
                    float cx = (x + bend * t * t) * P;
                    float half = Mathf.Max(0.5f, width * P * 0.5f * Mathf.Pow(1f - t, 0.8f));
                    Span(cx, half, y0 + i, tones, seed);
                }
            }

            /// <summary>Caule de largura quase constante.</summary>
            public void Stem(float x, float baseY, float height, float width, float bend, Tones tones)
            {
                int rows = Mathf.Max(1, Mathf.CeilToInt(height * P));
                int y0 = PxY(baseY);
                for (int i = 0; i < rows; i++)
                {
                    float t = i / (float)rows;
                    float cx = (x + bend * t * t) * P;
                    float half = Mathf.Max(0.5f, width * P * 0.5f * (1f - 0.25f * t));
                    Span(cx, half, y0 + i, tones, 0);
                }
            }

            void Span(float cx, float half, int py, Tones tones, int seed)
            {
                int left = Mathf.FloorToInt(cx - half);
                int right = Mathf.CeilToInt(cx + half) - 1;
                for (int px = left; px <= right; px++)
                {
                    float u = (px + 0.5f - (cx - half)) / (2f * half);
                    Color32 c = u < 0.3f ? tones.Dark : u > 0.72f ? tones.Light : tones.Base;
                    // Estrias finas ao longo da folha.
                    if (seed != 0 && c.Equals(tones.Base) && Hash(px, py / 7, seed) < 0.12f)
                        c = Color32.Lerp(tones.Base, tones.Dark, 0.4f);
                    Target.Put(px, py, c);
                }
            }

            /// <summary>Elipse com luz vinda do alto à esquerda e contorno escuro.</summary>
            public void Ellipse(float cx, float cy, float rx, float ry, Tones tones, bool outline = true)
            {
                float pcx = cx * P, pcy = (cy - _originY) * P, prx = Mathf.Max(0.6f, rx * P), pry = Mathf.Max(0.6f, ry * P);
                for (int py = Mathf.FloorToInt(pcy - pry); py <= Mathf.CeilToInt(pcy + pry); py++)
                for (int px = Mathf.FloorToInt(pcx - prx); px <= Mathf.CeilToInt(pcx + prx); px++)
                {
                    float dx = (px + 0.5f - pcx) / prx;
                    float dy = (py + 0.5f - pcy) / pry;
                    float d = dx * dx + dy * dy;
                    if (d > 1f)
                        continue;
                    float light = -dx * 0.5f + dy * 0.8f;
                    Color32 c = light > 0.45f ? tones.Light : light < -0.35f ? tones.Dark : tones.Base;
                    if (outline && d > 0.78f && prx > 4f)
                        c = tones.Dark;
                    Target.Put(px, py, c);
                }
            }

            /// <summary>Chapéu de cogumelo: metade de cima da elipse, com a lamela escura embaixo.</summary>
            public void Cap(float cx, float baseY, float rx, float ry, Tones tones, Color32 gills)
            {
                float pcx = cx * P, pcy = (baseY - _originY) * P, prx = rx * P, pry = ry * P;
                int gillRows = Mathf.Max(2, Mathf.RoundToInt(P * 0.5f));
                for (int py = Mathf.FloorToInt(pcy) - gillRows; py <= Mathf.CeilToInt(pcy + pry); py++)
                for (int px = Mathf.FloorToInt(pcx - prx); px <= Mathf.CeilToInt(pcx + prx); px++)
                {
                    float dx = (px + 0.5f - pcx) / prx;
                    if (py < pcy)
                    {
                        if (Mathf.Abs(dx) < 0.92f)
                            Target.Put(px, py, gills);
                        continue;
                    }
                    float dy = (py + 0.5f - pcy) / pry;
                    float d = dx * dx + dy * dy;
                    if (d > 1f)
                        continue;
                    float light = -dx * 0.5f + dy * 0.9f;
                    Color32 c = light > 0.55f ? tones.Light : light < -0.2f ? tones.Dark : tones.Base;
                    if (d > 0.85f)
                        c = tones.Dark;
                    Target.Put(px, py, c);
                }
            }

            /// <summary>Segmento grosso (galho, pétala), claro em cima e escuro embaixo; com <paramref name="knots"/>, nós de casca.</summary>
            public void Segment(float x0, float y0, float x1, float y1, float thickness, Tones tones, System.Random rng, bool knots = true)
            {
                Vector2 a = new Vector2(x0 * P, (y0 - _originY) * P);
                Vector2 b = new Vector2(x1 * P, (y1 - _originY) * P);
                float r = thickness * P * 0.5f;
                Vector2 ab = b - a;
                float len2 = Mathf.Max(0.0001f, ab.sqrMagnitude);
                Vector2 normal = new Vector2(-ab.y, ab.x).normalized;
                if (normal.y < 0f)
                    normal = -normal;
                int seed = rng.Next();
                int minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - r), maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + r);
                int minY = Mathf.FloorToInt(Mathf.Min(a.y, b.y) - r), maxY = Mathf.CeilToInt(Mathf.Max(a.y, b.y) + r);
                for (int py = minY; py <= maxY; py++)
                for (int px = minX; px <= maxX; px++)
                {
                    Vector2 q = new Vector2(px + 0.5f, py + 0.5f);
                    float t = Mathf.Clamp01(Vector2.Dot(q - a, ab) / len2);
                    Vector2 off = q - (a + ab * t);
                    if (off.sqrMagnitude > r * r)
                        continue;
                    float side = Vector2.Dot(off, normal) / r;
                    Color32 c = side > 0.35f ? tones.Light : side < -0.3f ? tones.Dark : tones.Base;
                    // Nós da casca a cada poucos pixels ao longo do comprimento.
                    if (knots && Hash(Mathf.RoundToInt(t * 40f), 0, seed) < 0.15f && Mathf.Abs(side) < 0.5f)
                        c = tones.Dark;
                    Target.Put(px, py, c);
                }
            }

            /// <summary>Folha seca caída de lado: lente fina com nervura, uma ponta enrolada para cima.</summary>
            public void Leaf(float cx, float baseY, float length, float thickness, float curl, bool curlRight, Tones tones)
            {
                float half = length * 0.5f * P;
                int left = Mathf.FloorToInt(cx * P - half), right = Mathf.CeilToInt(cx * P + half);
                float baseP = (baseY - _originY) * P;
                for (int px = left; px <= right; px++)
                {
                    float u = (px + 0.5f - cx * P) / half;
                    if (Mathf.Abs(u) > 1f)
                        continue;
                    float lift = curl * P * Mathf.Pow(Mathf.Max(0f, curlRight ? u : -u), 3f);
                    float h = thickness * P * Mathf.Pow(1f - u * u, 0.6f);
                    float mid = baseP + lift + h;
                    for (int py = Mathf.FloorToInt(baseP + lift); py <= Mathf.CeilToInt(mid + h); py++)
                    {
                        float v = (py + 0.5f - mid) / Mathf.Max(0.5f, h);
                        if (Mathf.Abs(v) > 1f)
                            continue;
                        Color32 c = v > 0.4f ? tones.Light : v < -0.45f ? tones.Dark : tones.Base;
                        if (Mathf.Abs(v) < 0.15f || ((px + py) % 9 == 0 && Mathf.Abs(v) < 0.7f))
                            c = tones.Dark; // nervura central e laterais
                        Target.Put(px, py, c);
                    }
                }
            }

            static float Hash(int x, int y, int seed)
            {
                unchecked
                {
                    uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
                    h = (h ^ (h >> 13)) * 1274126177u;
                    return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
                }
            }
        }
    }
}
