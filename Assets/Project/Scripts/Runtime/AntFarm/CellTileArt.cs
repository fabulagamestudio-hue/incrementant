using System;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Visual de sala construída (REQ-058): cada família tem no atlas um bloco (a terra que as formigas põem nos slots do
    /// ambiente) e uma parede de fundo. Os cinco depósitos dividem <see cref="Store"/> (decisão de 2026-10-22).
    /// </summary>
    public enum RoomStyle { Queen, Nutrient, Nursery, Waste, Dew, Farm, Store, Smelter, Forge, Armory, Wardrobe }

    /// <summary>
    /// Formato do atlas RGB do terreno (etapa 4 do documento 40) e o atlas gerado por código usado enquanto não há arte.
    /// Uma textura repetível de <see cref="TileSize"/>² texels por linha, empilhadas na vertical, de baixo para cima: as
    /// linhas 0..<see cref="MaterialCount"/>-1 seguem a ordem de <see cref="CellMaterial"/>, depois vem a parede de fundo
    /// do túnel (<see cref="BackgroundRow"/>) e, por último, um par de linhas por <see cref="RoomStyle"/> (bloco e fundo da
    /// sala construída, <see cref="RoomBlockRow"/> e <see cref="RoomBackRow"/>). Cada texel é a cor final do material
    /// (RGB, sRGB); o tom da célula entra no shader como ajuste (ver <see cref="TerrainTileAtlas"/>). Um atlas desenhado no
    /// mesmo formato substitui este (<c>AntFarmView</c> → <b>Tile Atlas</b>).
    /// </summary>
    public static class CellTileArt
    {
        /// <summary>Texels por célula da grade (experimento 8x8 de 2026-10-22: a célula vira um bloco de 8x8 texels, mostrado em 2x a 16 px por célula).</summary>
        public const int TexelsPerCell = 8;

        /// <summary>Lado da textura repetível de cada linha: 8x8 células.</summary>
        public const int TileSize = 64;

        /// <summary>Células por lado da textura repetível (o padrão se repete a cada tantas células).</summary>
        public const int TileCells = TileSize / TexelsPerCell;

        /// <summary>
        /// Contorno de 1 texel dos elementos (experimento 8x8 de 2026-10-22): preto da paleta mestra (índice 0). O mesmo valor
        /// está em <c>_OutlineColor</c> do shader TerrainTiles e em <c>Art/Source~/outline_pixel_art.py</c>.
        /// </summary>
        public static readonly Color32 OutlineColor = new Color32(0x1B, 0x18, 0x15, 255);

        /// <summary>Valor gravado na textura de materiais para células de céu (sem textura).</summary>
        public const byte SkyId = 255;

        public static int MaterialCount => Enum.GetValues(typeof(CellMaterial)).Length;

        /// <summary>Linha do atlas com a parede de fundo do túnel, logo depois dos materiais.</summary>
        public static int BackgroundRow => MaterialCount;

        public static int RoomStyleCount => Enum.GetValues(typeof(RoomStyle)).Length;

        /// <summary>Linha do bloco construído da sala (sólido), logo depois da parede de fundo; o fundo da sala vem em seguida.</summary>
        public static int RoomBlockRow(RoomStyle style) => BackgroundRow + 1 + 2 * (int)style;

        /// <summary>Linha da parede de fundo da sala construída (o vazio dentro dela).</summary>
        public static int RoomBackRow(RoomStyle style) => RoomBlockRow(style) + 1;

        /// <summary>
        /// Linha do cogumelo luminoso de terra (o passageiro, sem fabricação dos pulgões), a última do atlas: a célula
        /// <see cref="CellMaterial.GlowMushroom"/> com <c>PixelGrid.IsFleetingGlow</c> grava este id na textura de materiais.
        /// </summary>
        public static int SoilGlowRow => MaterialCount + 1 + 2 * RoomStyleCount;

        public static int RowCount => SoilGlowRow + 1;

        /// <summary>Família visual do ambiente pintado: os cinco depósitos usam a mesma.</summary>
        public static RoomStyle StyleOf(PaintRoom room)
        {
            switch (room)
            {
                case PaintRoom.Queen: return RoomStyle.Queen;
                case PaintRoom.Nutrient: return RoomStyle.Nutrient;
                case PaintRoom.Nursery: return RoomStyle.Nursery;
                case PaintRoom.Waste: return RoomStyle.Waste;
                case PaintRoom.Dew: return RoomStyle.Dew;
                case PaintRoom.Farm: return RoomStyle.Farm;
                // Fundição, forja e armazéns de equipamento com visual próprio (pedido de 2026-10-23); os outros depósitos dividem Store.
                case PaintRoom.Smelter: return RoomStyle.Smelter;
                case PaintRoom.Forge: return RoomStyle.Forge;
                case PaintRoom.Armory: return RoomStyle.Armory;
                case PaintRoom.Wardrobe: return RoomStyle.Wardrobe;
                default: return RoomStyle.Store;
            }
        }

        /// <summary>Verdadeiro nas linhas de bloco de sala construída (sólidas); as de fundo são vazias como a parede do túnel.</summary>
        static bool IsRoomBlockRow(int row) => row > BackgroundRow && row < SoilGlowRow && (row - BackgroundRow) % 2 == 1;

        /// <summary>
        /// Texels do atlas gerado (linha a linha, de baixo para cima): o padrão de cada material sobre a cor de referência
        /// da linha (<paramref name="references"/>, uma por linha). Com a cor da célula igual à referência, o shader mostra o
        /// texel como está.
        /// </summary>
        public static Color32[] Generate(Color32[] references)
        {
            int rows = RowCount;
            var pixels = new Color32[TileSize * TileSize * rows];
            for (int row = 0; row < rows; row++)
            {
                Color linear = ((Color)references[row]).linear;
                int seed = 7919 * (row + 1);
                int offset = row * TileSize * TileSize;
                for (int y = 0; y < TileSize; y++)
                for (int x = 0; x < TileSize; x++)
                {
                    float factor = row == SoilGlowRow ? Shade(CellMaterial.GlowMushroom, x, y, seed)
                        : row < BackgroundRow ? Shade((CellMaterial)row, x, y, seed)
                        : IsRoomBlockRow(row) ? Bricks(x, y, seed) : Dirt(x, y, seed, 0.12f);
                    factor = Mathf.Clamp(1f + (factor - 1f) * Contrast, 0f, 2f);
                    Color c = new Color(linear.r * factor, linear.g * factor, linear.b * factor, 1f).gamma;
                    pixels[offset + y * TileSize + x] = c;
                }
            }
            return pixels;
        }

        /// <summary>Fator de brilho do texel (1 = cor de referência).</summary>
        static float Shade(CellMaterial material, int x, int y, int seed)
        {
            switch (material)
            {
                case CellMaterial.Empty:
                    // Não aparece (o vazio usa a parede de fundo), mas fica preenchida.
                    return 1f + 0.06f * Noise(x, y, 16, seed) + 0.04f * (Hash(x, y, seed) - 0.5f);
                case CellMaterial.Dirt:
                case CellMaterial.Grass:
                    return Dirt(x, y, seed, 0.12f);
                case CellMaterial.Spoil:
                    // Terra solta: torrões maiores e mais contraste.
                    return Clumps(x, y, seed);
                case CellMaterial.Mud:
                    return 1f + 0.1f * Noise(x, y, 32, seed) + Speck(x, y, seed, 0.02f, 0.25f);
                case CellMaterial.Sand:
                    return 1f + 0.05f * Noise(x, y, 16, seed) + 0.14f * (Hash(x, y, seed) - 0.5f) + Speck(x, y, seed + 1, 0.03f, 0.18f);
                case CellMaterial.PackedSpoil:
                case CellMaterial.PackedSand:
                    // Compactado pela chuva: camadas horizontais.
                    return Dirt(x, y, seed, 0.08f) + 0.07f * Mathf.Sin((y + 3f * Noise(x, y, 16, seed + 2)) * Mathf.PI / 4f);
                case CellMaterial.Rock:
                    return Stones(x, y, seed, 12, 1f);
                case CellMaterial.Bedrock:
                    return Stones(x, y, seed, 6, 0.85f);
                default:
                    // Plantas e restos: só um granulado leve.
                    return 1f + 0.08f * (Hash(x, y, seed) - 0.5f) + 0.06f * Noise(x, y, 16, seed);
            }
        }

        static float Dirt(int x, int y, int seed, float amount) =>
            1f + amount * Noise(x, y, 16, seed) + 0.5f * amount * Noise(x, y, 8, seed + 3) +
            Speck(x, y, seed + 5, 0.035f, -0.3f) + Speck(x, y, seed + 6, 0.02f, 0.22f);

        /// <summary>
        /// Bloco de sala construída no atlas gerado: tijolos de meia célula alinhados à grade, junta escura e face um pouco
        /// mais clara, para a média ficar perto da referência.
        /// </summary>
        static float Bricks(int x, int y, int seed)
        {
            int course = y / (TexelsPerCell / 2);
            int bx = x + (course % 2) * (TexelsPerCell / 2);
            bool joint = y % (TexelsPerCell / 2) == 0 || bx % TexelsPerCell == 0;
            if (joint)
                return 0.8f;
            return 1.05f + 0.06f * (Hash(bx / TexelsPerCell % TileCells, course, seed) - 0.5f) + 0.03f * (Hash(x, y, seed + 1) - 0.5f);
        }

        static float Clumps(int x, int y, int seed)
        {
            float n = Noise(x, y, 8, seed);
            float lump = n > 0.25f ? 0.16f : n < -0.35f ? -0.2f : 0f;
            return 1f + lump + 0.08f * Noise(x, y, 16, seed + 4) + Speck(x, y, seed + 7, 0.03f, -0.25f);
        }

        /// <summary>
        /// Pedras: diagrama de Voronoi repetível com <paramref name="perSide"/>² sementes; contorno escuro entre as
        /// pedras e luz no lado de cima de cada uma.
        /// </summary>
        static float Stones(int x, int y, int seed, int perSide, float brightness)
        {
            float cell = TileSize / (float)perSide;
            float best = float.MaxValue, second = float.MaxValue;
            Vector2 bestDelta = Vector2.zero;
            int bestIndex = 0;
            int cx = Mathf.FloorToInt(x / cell), cy = Mathf.FloorToInt(y / cell);
            for (int oy = -1; oy <= 1; oy++)
            for (int ox = -1; ox <= 1; ox++)
            {
                int gx = cx + ox, gy = cy + oy;
                int wx = Wrap(gx, perSide), wy = Wrap(gy, perSide);
                var site = new Vector2((gx + 0.15f + 0.7f * Hash(wx, wy, seed)) * cell,
                                       (gy + 0.15f + 0.7f * Hash(wx, wy, seed + 1)) * cell);
                Vector2 delta = new Vector2(x + 0.5f, y + 0.5f) - site;
                float d = delta.magnitude;
                if (d < best)
                {
                    second = best;
                    best = d;
                    bestDelta = delta;
                    bestIndex = wy * perSide + wx;
                }
                else if (d < second)
                {
                    second = d;
                }
            }

            if (second - best < 1.6f)
                return 0.55f * brightness;
            float tone = 0.9f + 0.2f * Hash(bestIndex, 0, seed + 2);
            float light = Mathf.Clamp(bestDelta.y / cell, -0.5f, 0.5f) * 0.2f;
            return brightness * (tone + light + 0.05f * (Hash(x, y, seed + 3) - 0.5f));
        }

        /// <summary>Ponto isolado: soma <paramref name="amount"/> com probabilidade <paramref name="chance"/>.</summary>
        static float Speck(int x, int y, int seed, float chance, float amount) =>
            Hash(x, y, seed) < chance ? amount : 0f;

        /// <summary>Ruído de valor repetível no tile, com nós a cada <paramref name="scale"/> texels, em [-1, 1].</summary>
        static float Noise(int x, int y, int scale, int seed)
        {
            int period = TileSize / scale;
            float fx = x / (float)scale, fy = y / (float)scale;
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            float tx = Smooth(fx - x0), ty = Smooth(fy - y0);
            float a = Hash(Wrap(x0, period), Wrap(y0, period), seed);
            float b = Hash(Wrap(x0 + 1, period), Wrap(y0, period), seed);
            float c = Hash(Wrap(x0, period), Wrap(y0 + 1, period), seed);
            float d = Hash(Wrap(x0 + 1, period), Wrap(y0 + 1, period), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty) * 2f - 1f;
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);

        static int Wrap(int value, int period) => ((value % period) + period) % period;

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        /// <summary>Amplia o desvio de cada texel em volta de 1: os padrões acima são desenhados suaves e ajustados aqui.</summary>
        const float Contrast = 1.8f;
    }
}
