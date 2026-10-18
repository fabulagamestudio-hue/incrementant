using UnityEngine;

namespace Fabula.Incrementant.World
{
    /// <summary>
    /// Converte entre a grade e uma imagem de mapa (1 pixel = 1 célula, y = 0 embaixo).
    /// Cada material é uma cor fixa (<see cref="CellMaterials.BaseColor"/>); transparente = céu; <see cref="TunnelColor"/> = vazio subterrâneo;
    /// <see cref="WaterColor"/> = água (conta como céu para o solo original: pinte laguinhos acima do chão).
    /// A decodificação aceita a cor mais próxima da paleta, para tolerar pequenas diferenças de pintura.
    /// O tom (variação de cor) não é salvo: é recalculado de forma determinística ao carregar.
    /// </summary>
    public static class TerrainMapCodec
    {
        public static readonly Color32 SkyColor = new Color32(0, 0, 0, 0);
        public static readonly Color32 TunnelColor = new Color32(54, 37, 27, 255);
        public static readonly Color32 WaterColor = new Color32(60, 120, 200, 255);

        static readonly CellMaterial[] PaletteMaterials =
        {
            CellMaterial.Dirt, CellMaterial.Grass, CellMaterial.Sand, CellMaterial.Rock, CellMaterial.Spoil, CellMaterial.Bedrock,
            CellMaterial.PackedSpoil, CellMaterial.PackedSand, CellMaterial.Mud, CellMaterial.PlantGrass, CellMaterial.PlantLeaf,
            CellMaterial.FallenGrass, CellMaterial.FallenLeaf, CellMaterial.Rubble, CellMaterial.Refuse,
        };

        public static Color32[] Encode(PixelGrid grid)
        {
            var pixels = new Color32[grid.Width * grid.Height];
            for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
            {
                CellMaterial material = grid.Get(x, y);
                Color32 color = material != CellMaterial.Empty
                    ? CellMaterials.BaseColor(material)
                    : grid.HasWater(x, y) ? WaterColor
                    : y > grid.GetSurfaceY(x) ? SkyColor : TunnelColor;
                pixels[y * grid.Width + x] = color;
            }
            return pixels;
        }

        public static PixelGrid Decode(Color32[] pixels, int width, int height)
        {
            var grid = new PixelGrid(width, height);
            var isSky = new bool[width * height];

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                CellMaterial material = Classify(pixels[i], out bool sky, out bool water);
                // Planta fica acima do chão: conta como céu para achar o solo original.
                isSky[i] = sky || water || CellMaterials.IsPlant(material);
                grid.Set(x, y, material, 128);
                if (water)
                    grid.SetWater(x, y, true);
            }

            // Solo original de cada coluna = pixel não-céu mais alto (túneis pintados contam como subterrâneo).
            for (int x = 0; x < width; x++)
            {
                int surface = -1;
                for (int y = height - 1; y >= 0; y--)
                {
                    if (!isSky[y * width + x])
                    {
                        surface = y;
                        break;
                    }
                }
                grid.SetSurfaceY(x, surface);
            }

            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                CellMaterial material = grid.Get(x, y);
                if (material != CellMaterial.Empty)
                    grid.Set(x, y, material, ShadeAt(x, y, grid.GetSurfaceY(x)));
            }
            return grid;
        }

        /// <summary>Variação de tom determinística por posição: faixas suaves (Perlin) + granulação fina (hash).</summary>
        public static byte ShadeAt(int x, int y, int surfaceY)
        {
            float layered = (Mathf.PerlinNoise(x * 0.18f + 31.7f, y * 0.18f + 11.3f) - 0.5f) * 90f;
            uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            int grain = (int)(h % 33) - 16;
            int depth = Mathf.Max(0, surfaceY - y);
            return (byte)Mathf.Clamp(128 + (int)layered + grain - depth / 6, 0, 255);
        }

        static CellMaterial Classify(Color32 color, out bool sky, out bool water)
        {
            sky = color.a < 128;
            water = false;
            if (sky)
                return CellMaterial.Empty;

            CellMaterial best = CellMaterial.Empty; // túnel
            int bestDistance = Distance(color, TunnelColor);
            int waterDistance = Distance(color, WaterColor);
            if (waterDistance < bestDistance)
            {
                bestDistance = waterDistance;
                water = true;
            }
            foreach (CellMaterial material in PaletteMaterials)
            {
                int d = Distance(color, CellMaterials.BaseColor(material));
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = material;
                    water = false;
                }
            }
            return best;
        }

        static int Distance(Color32 a, Color32 b)
        {
            int dr = a.r - b.r, dg = a.g - b.g, db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }
    }
}
