using System;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Atlas das texturas de material usadas pelo shader <c>Incrementant/TerrainTiles</c>: uma textura repetível de
    /// <see cref="TileSize"/>² texels por material, empilhadas na vertical (linha = valor de <see cref="CellMaterial"/>).
    /// Os texels são cinza em volta de 128 (= cor da célula sem mudança): o shader multiplica a cor da célula por
    /// texel × 2. Gerado por código enquanto não há arte; um atlas desenhado com o mesmo layout pode substituí-lo
    /// (<c>AntFarmView</c> → <b>Tile Atlas</b>).
    /// </summary>
    public static class CellTileArt
    {
        /// <summary>Texels por célula da grade (a célula vira um bloco de 16x16 na tela).</summary>
        public const int TexelsPerCell = 16;

        /// <summary>Lado da textura repetível de cada material: 4x4 células.</summary>
        public const int TileSize = 64;

        /// <summary>Valor gravado na textura de materiais para células de céu (sem textura).</summary>
        public const byte SkyId = 255;

        public static int TileCount => Enum.GetValues(typeof(CellMaterial)).Length;

        static byte[] _pixels;

        /// <summary>Texels do atlas (linha a linha, de baixo para cima), gerados uma vez e guardados.</summary>
        static byte[] Pixels
        {
            get
            {
                if (_pixels != null)
                    return _pixels;
                int count = TileCount;
                _pixels = new byte[TileSize * TileSize * count];
                for (int id = 0; id < count; id++)
                    Paint((CellMaterial)id, _pixels, id * TileSize * TileSize);
                return _pixels;
            }
        }

        /// <summary>
        /// Fator de brilho do texel (<paramref name="tx"/>, <paramref name="ty"/>) do bloco 16x16 da célula
        /// (<paramref name="cellX"/>, <paramref name="cellY"/>): o mesmo que o shader aplica a ela no terreno.
        /// </summary>
        public static float Detail(CellMaterial material, int cellX, int cellY, int tx, int ty)
        {
            int px = (cellX * TexelsPerCell + tx) % TileSize;
            int py = (cellY * TexelsPerCell + ty) % TileSize;
            return Pixels[((int)material * TileSize + py) * TileSize + px] / 128f;
        }

        public static Texture2D CreateAtlas()
        {
            int count = TileCount;
            byte[] pixels = Pixels;

            var texture = new Texture2D(TileSize, TileSize * count, TextureFormat.R8, false, true)
            {
                name = "TerrainTileAtlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixelData(pixels, 0);
            texture.Apply(false, true);
            return texture;
        }

        static void Paint(CellMaterial material, byte[] pixels, int offset)
        {
            int seed = 7919 * ((int)material + 1);
            for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
                pixels[offset + y * TileSize + x] = ToByte(Shade(material, x, y, seed));
        }

        /// <summary>Fator de brilho do texel (1 = cor da célula).</summary>
        static float Shade(CellMaterial material, int x, int y, int seed)
        {
            switch (material)
            {
                case CellMaterial.Empty:
                    // Fundo do túnel: granulado suave e manchas largas.
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
                    return Stones(x, y, seed, 6, 1f);
                case CellMaterial.Bedrock:
                    return Stones(x, y, seed, 3, 0.85f);
                default:
                    // Plantas e restos: só um granulado leve.
                    return 1f + 0.08f * (Hash(x, y, seed) - 0.5f) + 0.06f * Noise(x, y, 16, seed);
            }
        }

        static float Dirt(int x, int y, int seed, float amount) =>
            1f + amount * Noise(x, y, 16, seed) + 0.5f * amount * Noise(x, y, 8, seed + 3) +
            Speck(x, y, seed + 5, 0.035f, -0.3f) + Speck(x, y, seed + 6, 0.02f, 0.22f);

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

        static byte ToByte(float factor) => (byte)Mathf.Clamp(Mathf.RoundToInt((1f + (factor - 1f) * Contrast) * 128f), 0, 255);
    }
}
