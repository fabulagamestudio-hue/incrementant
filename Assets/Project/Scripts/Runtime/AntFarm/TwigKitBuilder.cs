using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>Imagem RGBA em memória (linha 0 embaixo, como a <see cref="Texture2D"/>).</summary>
    public sealed class PixelImage
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Pixels;

        public PixelImage(int width, int height, Color32[] pixels = null)
        {
            Width = width;
            Height = height;
            Pixels = pixels ?? new Color32[width * height];
        }

        /// <summary>Lê uma textura com Read/Write ligado; null se ela não for legível.</summary>
        public static PixelImage From(Texture2D texture) =>
            texture != null && texture.isReadable ? new PixelImage(texture.width, texture.height, texture.GetPixels32()) : null;

        public Color32 Get(int x, int y) => Pixels[y * Width + x];
    }

    /// <summary>Peças do kit do graveto (ver <c>Art/SceneryKits/Twig/Source~/twig_kit.py</c>).</summary>
    public sealed class TwigPieces
    {
        /// <summary>Casca repetível nos dois sentidos; as fibras correm na horizontal (ao longo do graveto).</summary>
        public PixelImage Bark;
        /// <summary>Face do corte, na ponta grossa: a altura dela é a espessura do tronco ali.</summary>
        public PixelImage CutEnd;
        /// <summary>Lascas da ponta fina, apontando para a direita: a altura dela é a espessura do tronco ali.</summary>
        public PixelImage BrokenEnd;
        /// <summary>Lascas da ponta do galho, apontando para cima: a largura dela é a espessura do galho ali.</summary>
        public PixelImage BranchTip;
        public PixelImage[] Knots = System.Array.Empty<PixelImage>();
    }

    /// <summary>Medidas de um graveto, em células (sorteadas pelo <see cref="TwigKit"/>).</summary>
    public struct TwigShape
    {
        public float Length;
        /// <summary>Quanto a ponta fina sobe (negativo: desce) em relação à grossa.</summary>
        public float Rise;
        /// <summary>Barriga do tronco no meio (positivo: para cima).</summary>
        public float Bow;
        /// <summary>Posição do galho ao longo do tronco (0 = ponta grossa, 1 = fina).</summary>
        public float BranchAt;
        public float BranchLength;
        /// <summary>Ângulo do galho a partir da horizontal, em graus.</summary>
        public float BranchAngle;
        /// <summary>Galho inclinado para o lado da ponta fina (senão, para o da grossa).</summary>
        public bool BranchTowardTip;
        public int Knots;
        /// <summary>Ponta grossa à direita (espelha o desenho inteiro).</summary>
        public bool Mirror;
        public int Seed;
    }

    /// <summary>
    /// Monta um graveto inteiro a partir das peças do kit (piloto de 2026-10-23, experimento 8x8): o tronco e o galho são
    /// preenchidos com a casca lida no espaço deles (as fibras acompanham o comprimento) e sombreados como um cilindro em
    /// degraus da própria rampa da casca (as cores dela ordenadas por brilho, então nada sai da paleta); depois entram os nós,
    /// a face do corte, as lascas das pontas e o contorno preto de 1 texel (<see cref="PixelArtFilters.Outline"/>).
    /// O tamanho no mundo é o das medidas, a <see cref="CellTileArt.TexelsPerCell"/> texels por célula.
    /// </summary>
    public static class TwigKitBuilder
    {
        /// <summary>Folga, em texels, entre o ponto mais baixo do tronco e a base da imagem (e nas outras bordas).</summary>
        public const int Margin = 2;

        public static PixelImage Build(TwigPieces pieces, TwigShape shape)
        {
            const int texels = CellTileArt.TexelsPerCell;
            var rng = new System.Random(shape.Seed);
            List<Color32> ramp = Ramp(pieces.Bark);

            float length = Mathf.Max(8f, shape.Length) * texels;
            float baseRadius = pieces.CutEnd.Height * 0.5f;
            float tipRadius = pieces.BrokenEnd.Height * 0.5f;
            float branchRadius = pieces.BranchTip.Width * 0.5f;
            float rise = shape.Rise * texels;
            float bow = shape.Bow * texels;

            // Tronco começa depois da metade da face do corte; a ponta fina termina nas lascas.
            float x0 = pieces.CutEnd.Width * 0.5f + Margin;
            float x1 = x0 + length;
            float Center(float x)
            {
                float t = Mathf.Clamp01((x - x0) / length);
                return rise * t + bow * Mathf.Sin(Mathf.PI * t);
            }
            float Radius(float x) => Mathf.Lerp(baseRadius, tipRadius, Mathf.Clamp01((x - x0) / length));

            // Desloca tudo para o ponto mais baixo do tronco ficar na margem.
            float lowest = float.MaxValue;
            for (float x = x0; x <= x1; x += 1f)
                lowest = Mathf.Min(lowest, Center(x) - Radius(x));
            float offsetY = Margin - lowest;

            // Galho: sai do eixo do tronco e sobe no ângulo pedido.
            float ax = Mathf.Lerp(x0, x1, Mathf.Clamp(shape.BranchAt, 0.15f, 0.85f));
            float ay = Center(ax) + offsetY;
            float angle = Mathf.Clamp(shape.BranchAngle, 20f, 89f) * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(angle) * (shape.BranchTowardTip ? 1f : -1f), Mathf.Sin(angle));
            float branchLength = Mathf.Max(0f, shape.BranchLength) * texels;
            Vector2 branchEnd = new Vector2(ax, ay) + dir * branchLength;

            float top = 0f;
            for (float x = x0; x <= x1; x += 1f)
                top = Mathf.Max(top, Center(x) + offsetY + Radius(x));
            top = Mathf.Max(top, branchEnd.y + pieces.BranchTip.Height + pieces.BranchTip.Width);
            int width = Mathf.CeilToInt(Mathf.Max(x1 + pieces.BrokenEnd.Width, branchEnd.x + pieces.BranchTip.Height + pieces.BranchTip.Width)) + Margin;
            int height = Mathf.CeilToInt(top + Mathf.Max(pieces.CutEnd.Height, pieces.BrokenEnd.Height) * 0.5f) + Margin;
            var image = new PixelImage(width, height);

            // Galho primeiro: o tronco passa por cima da junta.
            int bx0 = Mathf.FloorToInt(Mathf.Min(ax, branchEnd.x) - branchRadius * 2f);
            int bx1 = Mathf.CeilToInt(Mathf.Max(ax, branchEnd.x) + branchRadius * 2f);
            int by0 = Mathf.FloorToInt(ay - branchRadius * 2f);
            int by1 = Mathf.CeilToInt(branchEnd.y);
            var normal = new Vector2(-dir.y, dir.x);   // lado de cima/esquerda do galho
            for (int y = Mathf.Max(0, by0); y <= Mathf.Min(height - 1, by1); y++)
            for (int x = Mathf.Max(0, bx0); x <= Mathf.Min(width - 1, bx1); x++)
            {
                var p = new Vector2(x + 0.5f - ax, y + 0.5f - ay);
                float along = Vector2.Dot(p, dir);
                if (along < 0f || along > branchLength)
                    continue;
                float r = Mathf.Lerp(branchRadius * 1.3f, branchRadius, along / Mathf.Max(1f, branchLength));
                float across = Vector2.Dot(p, normal);
                if (Mathf.Abs(across) > r)
                    continue;
                Color32 bark = BarkAt(pieces.Bark, Mathf.FloorToInt(along) + 17, Mathf.FloorToInt(across + r) + 31);
                image.Pixels[y * width + x] = Shade(ramp, bark, across / r);
            }

            // Ponta do galho: a peça (lascas apontando para cima) é lida no espaço do galho, então as lascas seguem a direção
            // dele e a base cobre o fim do galho (4 texels de sobra).
            const float tipOverlap = 4f;
            float tipReach = pieces.BranchTip.Height + pieces.BranchTip.Width;
            for (int y = Mathf.Max(0, Mathf.FloorToInt(branchEnd.y - tipReach)); y <= Mathf.Min(height - 1, Mathf.CeilToInt(branchEnd.y + tipReach)); y++)
            for (int x = Mathf.Max(0, Mathf.FloorToInt(branchEnd.x - tipReach)); x <= Mathf.Min(width - 1, Mathf.CeilToInt(branchEnd.x + tipReach)); x++)
            {
                var p = new Vector2(x + 0.5f - branchEnd.x, y + 0.5f - branchEnd.y);
                int tx = Mathf.FloorToInt(Vector2.Dot(p, -normal) + pieces.BranchTip.Width * 0.5f);
                int ty = Mathf.FloorToInt(Vector2.Dot(p, dir) + tipOverlap);
                if (tx < 0 || ty < 0 || tx >= pieces.BranchTip.Width || ty >= pieces.BranchTip.Height)
                    continue;
                Color32 c = pieces.BranchTip.Get(tx, ty);
                if (c.a >= 128)
                    image.Pixels[y * width + x] = c;
            }

            var wavePhase = new Vector2((float)rng.NextDouble() * 6.283f, (float)rng.NextDouble() * 6.283f);
            // Tronco, coluna a coluna (o centro desce em degraus inteiros, como a escada de pixels do terreno).
            for (int x = Mathf.FloorToInt(x0); x <= Mathf.CeilToInt(x1) && x < width; x++)
            {
                float cy = Center(x + 0.5f) + offsetY;
                float r = Radius(x + 0.5f);
                int ya = Mathf.Max(0, Mathf.FloorToInt(cy - r)), yb = Mathf.Min(height - 1, Mathf.CeilToInt(cy + r));
                for (int y = ya; y <= yb; y++)
                {
                    float d = y + 0.5f - cy;
                    if (Mathf.Abs(d) > r)
                        continue;
                    // As fibras ondulam devagar ao longo do tronco (sem degrau entre colunas), para a repetição da peça
                    // de casca não aparecer.
                    int along = x - Mathf.FloorToInt(x0);
                    int shiftRows = Mathf.RoundToInt(4f * Mathf.Sin(along * 0.031f + wavePhase.x) + 3f * Mathf.Sin(along * 0.013f + wavePhase.y));
                    Color32 bark = BarkAt(pieces.Bark, along, Mathf.FloorToInt(d + r) + shiftRows);
                    image.Pixels[y * width + x] = Shade(ramp, bark, d / r);
                }
            }

            // Nós: carimbados só onde já há tronco, longe das pontas e do galho.
            for (int i = 0; i < shape.Knots && pieces.Knots.Length > 0; i++)
            {
                PixelImage knot = pieces.Knots[rng.Next(pieces.Knots.Length)];
                float t = 0.12f + 0.76f * (float)rng.NextDouble();
                float kx = Mathf.Lerp(x0, x1, t);
                if (Mathf.Abs(kx - ax) < knot.Width * 1.5f)
                    kx = Mathf.Lerp(x0, x1, t < 0.5f ? t + 0.25f : t - 0.25f);
                float ky = Center(kx) + offsetY + Radius(kx) * ((float)rng.NextDouble() * 0.6f - 0.3f);
                Stamp(image, knot, Mathf.RoundToInt(kx - knot.Width * 0.5f), Mathf.RoundToInt(ky - knot.Height * 0.5f), onlyOver: true);
            }

            // Pontas: a face do corte centrada no começo do tronco; as lascas entrando um pouco no fim dele.
            Stamp(image, pieces.CutEnd, Mathf.RoundToInt(x0 - pieces.CutEnd.Width * 0.5f),
                  Mathf.RoundToInt(Center(x0) + offsetY - pieces.CutEnd.Height * 0.5f), onlyOver: false);
            Clear(image, Mathf.RoundToInt(x1) - 6, width);
            Stamp(image, pieces.BrokenEnd, Mathf.RoundToInt(x1) - 6, Mathf.RoundToInt(Center(x1) + offsetY - pieces.BrokenEnd.Height * 0.5f),
                  onlyOver: false);

            PixelArtFilters.Outline(image.Pixels, width, height);
            if (shape.Mirror)
                MirrorX(image);
            return image;
        }

        /// <summary>Cores opacas da casca, do escuro ao claro: o sombreado anda nessa escada.</summary>
        static List<Color32> Ramp(PixelImage bark)
        {
            var colors = new List<Color32>();
            foreach (Color32 c in bark.Pixels)
            {
                if (c.a < 128 || colors.Exists(o => o.r == c.r && o.g == c.g && o.b == c.b))
                    continue;
                colors.Add(new Color32(c.r, c.g, c.b, 255));
            }
            colors.Sort((a, b) => Luma(a).CompareTo(Luma(b)));
            return colors;
        }

        static int Luma(Color32 c) => c.r * 299 + c.g * 587 + c.b * 114;

        static Color32 BarkAt(PixelImage bark, int along, int across)
        {
            int x = ((along % bark.Width) + bark.Width) % bark.Width;
            int y = ((across % bark.Height) + bark.Height) % bark.Height;
            return bark.Get(x, y);
        }

        /// <summary>
        /// Cilindro iluminado de cima: <paramref name="side"/> vai de -1 (embaixo) a 1 (em cima). Um degrau a mais de luz no
        /// terço de cima, um a menos embaixo e dois na faixa junto ao chão.
        /// </summary>
        static Color32 Shade(List<Color32> ramp, Color32 bark, float side)
        {
            int shift = side > 0.45f ? 1 : side < -0.75f ? -2 : side < -0.35f ? -1 : 0;
            int index = ramp.FindIndex(o => o.r == bark.r && o.g == bark.g && o.b == bark.b);
            if (index < 0)
                return new Color32(bark.r, bark.g, bark.b, 255);
            return ramp[Mathf.Clamp(index + shift, 0, ramp.Count - 1)];
        }

        /// <param name="onlyOver">Só onde a imagem já é opaca (nós sobre o tronco).</param>
        static void Stamp(PixelImage image, PixelImage piece, int left, int bottom, bool onlyOver)
        {
            for (int y = 0; y < piece.Height; y++)
            for (int x = 0; x < piece.Width; x++)
            {
                int ix = left + x, iy = bottom + y;
                if (ix < 0 || iy < 0 || ix >= image.Width || iy >= image.Height)
                    continue;
                Color32 c = piece.Get(x, y);
                if (c.a < 128)
                    continue;
                int i = iy * image.Width + ix;
                if (onlyOver && image.Pixels[i].a < 128)
                    continue;
                image.Pixels[i] = c;
            }
        }

        /// <summary>
        /// Apaga as colunas a partir de <paramref name="fromColumn"/> (o fim do tronco, onde entram as lascas, para os vãos
        /// entre elas ficarem vazios). O galho nunca chega ali (<see cref="TwigShape.BranchAt"/> fica entre 0,15 e 0,85).
        /// </summary>
        static void Clear(PixelImage image, int fromColumn, int toColumn)
        {
            for (int y = 0; y < image.Height; y++)
            for (int x = Mathf.Max(0, fromColumn); x < Mathf.Min(image.Width, toColumn); x++)
                image.Pixels[y * image.Width + x] = default;
        }

        static void MirrorX(PixelImage image)
        {
            for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width / 2; x++)
            {
                int a = y * image.Width + x, b = y * image.Width + image.Width - 1 - x;
                (image.Pixels[a], image.Pixels[b]) = (image.Pixels[b], image.Pixels[a]);
            }
        }
    }
}
