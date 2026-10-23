using System.Collections.Generic;
using Fabula.Incrementant.AntFarm;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    public sealed class TwigKitBuilderTests
    {
        static readonly Color32 BarkDark = new Color32(56, 36, 20, 255);
        static readonly Color32 BarkMid = new Color32(98, 62, 35, 255);
        static readonly Color32 BarkLight = new Color32(158, 101, 52, 255);
        static readonly Color32 Wood = new Color32(222, 176, 109, 255);

        static PixelImage Solid(int width, int height, Color32 color)
        {
            var image = new PixelImage(width, height);
            for (int i = 0; i < image.Pixels.Length; i++)
                image.Pixels[i] = color;
            return image;
        }

        static TwigPieces Pieces()
        {
            var bark = new PixelImage(16, 8);
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 16; x++)
                bark.Pixels[y * 16 + x] = y < 3 ? BarkDark : y < 6 ? BarkMid : BarkLight;
            return new TwigPieces
            {
                Bark = bark,
                CutEnd = Solid(12, 40, Wood),
                BrokenEnd = Solid(12, 24, Wood),
                BranchTip = Solid(8, 6, Wood),
                Knots = new[] { Solid(6, 6, BarkDark) },
            };
        }

        static TwigShape Shape(int seed) => new TwigShape
        {
            Length = 60f, Rise = 3f, Bow = 1f, BranchAt = 0.5f, BranchLength = 10f, BranchAngle = 60f,
            BranchTowardTip = true, Knots = 2, Seed = seed,
        };

        [Test]
        public void Build_KeepsWorldLengthAndRestsOnTheMargin()
        {
            PixelImage image = TwigKitBuilder.Build(Pieces(), Shape(5));
            const int texels = CellTileArt.TexelsPerCell;
            // Tronco de 60 células, mais meia face do corte, as lascas e as margens.
            Assert.GreaterOrEqual(image.Width, 60 * texels);
            Assert.Less(image.Width, 60 * texels + 40);
            int lowest = int.MaxValue;
            for (int y = 0; y < image.Height && lowest == int.MaxValue; y++)
            for (int x = 0; x < image.Width; x++)
                if (image.Get(x, y).a >= 128) { lowest = y; break; }
            Assert.AreEqual(TwigKitBuilder.Margin, lowest, "O ponto mais baixo do tronco fica na margem de baixo da imagem.");
        }

        [Test]
        public void Build_UsesOnlyPieceColorsAndBlackOutline()
        {
            PixelImage image = TwigKitBuilder.Build(Pieces(), Shape(9));
            var allowed = new List<Color32> { BarkDark, BarkMid, BarkLight, Wood, CellTileArt.OutlineColor };
            bool outlined = false;
            foreach (Color32 c in image.Pixels)
            {
                if (c.a < 128)
                    continue;
                Assert.IsTrue(allowed.Exists(o => o.r == c.r && o.g == c.g && o.b == c.b), $"Cor fora das peças: {c}");
                outlined |= c.r == CellTileArt.OutlineColor.r && c.g == CellTileArt.OutlineColor.g && c.b == CellTileArt.OutlineColor.b;
            }
            Assert.IsTrue(outlined, "O graveto montado tem contorno preto.");
        }

        [Test]
        public void Build_SameSeedSameTwig()
        {
            PixelImage a = TwigKitBuilder.Build(Pieces(), Shape(3));
            PixelImage b = TwigKitBuilder.Build(Pieces(), Shape(3));
            Assert.AreEqual(a.Width, b.Width);
            Assert.AreEqual(a.Height, b.Height);
            CollectionAssert.AreEqual(a.Pixels, b.Pixels);
        }
    }
}
