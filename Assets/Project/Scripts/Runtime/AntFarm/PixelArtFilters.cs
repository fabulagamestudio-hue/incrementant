using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Filtros de pixel art para sprites gerados em código (experimento 8x8 de 2026-10-22): as mesmas regras de
    /// <c>Art/Source~/halve_pixel_art.py</c> e <c>outline_pixel_art.py</c>, usadas nos PNGs.
    /// </summary>
    public static class PixelArtFilters
    {
        /// <summary>
        /// Metade do tamanho, sem cor nova: cada 2x2 fica opaco com pelo menos 2 texels opacos, na cor mais frequente entre
        /// eles; no empate, a mais escura (contornos e frestas sobrevivem). <paramref name="width"/> e <paramref name="height"/>
        /// são pares.
        /// </summary>
        public static Color32[] Halve(Color32[] pixels, int width, int height)
        {
            int w = width / 2, h = height / 2;
            var result = new Color32[w * h];
            var block = new Color32[4];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int opaque = 0;
                for (int i = 0; i < 4; i++)
                {
                    Color32 c = pixels[(2 * y + i / 2) * width + 2 * x + i % 2];
                    if (c.a >= 128)
                        block[opaque++] = c;
                }
                if (opaque < 2)
                    continue;
                int best = -1;
                long bestScore = long.MinValue;
                for (int i = 0; i < opaque; i++)
                {
                    int count = 0;
                    for (int j = 0; j < opaque; j++)
                        if (block[j].r == block[i].r && block[j].g == block[i].g && block[j].b == block[i].b)
                            count++;
                    long score = count * 1000000L - (block[i].r * 299 + block[i].g * 587 + block[i].b * 114);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = i;
                    }
                }
                Color32 chosen = block[best];
                result[y * w + x] = new Color32(chosen.r, chosen.g, chosen.b, 255);
            }
            return result;
        }

        /// <summary>
        /// Contorno de 1 texel por dentro em <see cref="CellTileArt.OutlineColor"/>: o texel opaco que encosta, nas 4 direções,
        /// num transparente ou na borda da imagem.
        /// </summary>
        public static void Outline(Color32[] pixels, int width, int height)
        {
            var edge = new bool[pixels.Length];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a < 128)
                    continue;
                edge[y * width + x] = !Opaque(pixels, width, height, x - 1, y) || !Opaque(pixels, width, height, x + 1, y)
                                      || !Opaque(pixels, width, height, x, y - 1) || !Opaque(pixels, width, height, x, y + 1);
            }
            for (int i = 0; i < pixels.Length; i++)
                if (edge[i])
                    pixels[i] = CellTileArt.OutlineColor;
        }

        static bool Opaque(Color32[] pixels, int width, int height, int x, int y) =>
            x >= 0 && y >= 0 && x < width && y < height && pixels[y * width + x].a >= 128;
    }
}
