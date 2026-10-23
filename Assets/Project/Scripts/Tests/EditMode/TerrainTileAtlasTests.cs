using Fabula.Incrementant.AntFarm;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Formato do atlas RGB do terreno (<see cref="CellTileArt"/>, <see cref="TerrainTileAtlas"/>): tamanho, repetição,
    /// média perto da cor de referência no atlas gerado e recusa de um atlas desenhado fora do formato.
    /// </summary>
    public sealed class TerrainTileAtlasTests
    {
        static Color32[] References()
        {
            var references = new Color32[CellTileArt.RowCount];
            for (int row = 0; row < references.Length; row++)
                // Cores diferentes por linha, sempre no meio da faixa: com mais de 40 linhas a conta antiga (120 - 3 × linha)
                // dava a volta no byte e encostava no branco, onde o brilho satura e a média não segue a referência.
                references[row] = new Color32((byte)(40 + row * 8 % 160), (byte)(60 + row * 3 % 60), 90, 255);
            return references;
        }

        static void Release(TerrainTileAtlas atlas, Texture2D drawn = null)
        {
            if (atlas.Texture != drawn)
                Object.DestroyImmediate(atlas.Texture);
            Object.DestroyImmediate(atlas.Colors);
        }

        [Test]
        public void GeneratedAtlasHasEveryRowOfTheFormat()
        {
            TerrainTileAtlas atlas = TerrainTileAtlas.Create(null, References(), null);
            try
            {
                Assert.AreEqual(CellTileArt.TileSize, atlas.Texture.width);
                Assert.AreEqual(CellTileArt.TileSize * CellTileArt.RowCount, atlas.Texture.height);
                Assert.AreEqual(CellTileArt.MaterialCount, CellTileArt.BackgroundRow);
                Assert.AreEqual(2, atlas.Colors.width);
                Assert.AreEqual(CellTileArt.RowCount, atlas.Colors.height);
                Assert.IsFalse(atlas.IsFallback);
            }
            finally
            {
                Release(atlas);
            }
        }

        /// <summary>Salas construídas (REQ-058): bloco e fundo de cada família logo depois da parede de fundo, sem sobra.</summary>
        [Test]
        public void RoomRowsComeInPairsAfterTheBackground()
        {
            Assert.AreEqual(CellTileArt.BackgroundRow + 1, CellTileArt.RoomBlockRow(RoomStyle.Queen));
            var seen = new System.Collections.Generic.HashSet<int> { CellTileArt.BackgroundRow };
            for (int style = 0; style < CellTileArt.RoomStyleCount; style++)
            {
                int block = CellTileArt.RoomBlockRow((RoomStyle)style), back = CellTileArt.RoomBackRow((RoomStyle)style);
                Assert.AreEqual(block + 1, back);
                Assert.IsTrue(seen.Add(block) && seen.Add(back), $"estilo {(RoomStyle)style}");
            }
            Assert.AreEqual(CellTileArt.SoilGlowRow - 1, CellTileArt.RoomBackRow(RoomStyle.Wardrobe), "A última família vem logo antes da linha do cogumelo de terra.");
            Assert.AreEqual(CellTileArt.RowCount - 1, CellTileArt.SoilGlowRow, "O cogumelo de terra fecha o atlas.");
            Assert.AreEqual(CellTileArt.SoilGlowRow - CellTileArt.MaterialCount, seen.Count);
        }

        [Test]
        public void EveryPaintedRoomHasAStyleAndDepotsShareOne()
        {
            Assert.AreEqual(RoomStyle.Queen, CellTileArt.StyleOf(Colony.PaintRoom.Queen));
            Assert.AreEqual(RoomStyle.Farm, CellTileArt.StyleOf(Colony.PaintRoom.Farm));
            foreach (Colony.PaintRoom depot in new[] { Colony.PaintRoom.FoodStore, Colony.PaintRoom.BlockStore, Colony.PaintRoom.WaterStore,
                                                       Colony.PaintRoom.SandStore, Colony.PaintRoom.MushroomStore })
                Assert.AreEqual(RoomStyle.Store, CellTileArt.StyleOf(depot));
            // Fundição, forja e armazéns de equipamento com visual próprio (2026-10-23).
            Assert.AreEqual(RoomStyle.Smelter, CellTileArt.StyleOf(Colony.PaintRoom.Smelter));
            Assert.AreEqual(RoomStyle.Forge, CellTileArt.StyleOf(Colony.PaintRoom.Forge));
            Assert.AreEqual(RoomStyle.Armory, CellTileArt.StyleOf(Colony.PaintRoom.Armory));
            Assert.AreEqual(RoomStyle.Wardrobe, CellTileArt.StyleOf(Colony.PaintRoom.Wardrobe));
            Assert.AreEqual(RoomStyle.Store, CellTileArt.StyleOf(Colony.PaintRoom.OreStore));
        }

        [Test]
        public void TexelRepeatsEveryTileInBothDirections()
        {
            TerrainTileAtlas atlas = TerrainTileAtlas.Create(null, References(), null);
            try
            {
                int size = CellTileArt.TileSize;
                for (int row = 0; row < CellTileArt.RowCount; row++)
                {
                    Assert.AreEqual(atlas.Texel(row, 5, 9), atlas.Texel(row, 5 + size, 9));
                    Assert.AreEqual(atlas.Texel(row, 5, 9), atlas.Texel(row, 5, 9 - size * 3));
                }
            }
            finally
            {
                Release(atlas);
            }
        }

        /// <summary>O padrão gerado varia em volta da referência: a média (o que sobra com o zoom aberto) fica perto dela.</summary>
        [Test]
        public void GeneratedMeanStaysNearReference()
        {
            Color32[] references = References();
            TerrainTileAtlas atlas = TerrainTileAtlas.Create(null, references, null);
            try
            {
                for (int row = 0; row < CellTileArt.RowCount; row++)
                {
                    if (row == (int)World.CellMaterial.Rock || row == (int)World.CellMaterial.Bedrock)
                        continue; // pedras: o contorno escuro entre elas puxa a média para baixo, como antes
                    Color32 mean = atlas.Mean(row), reference = references[row];
                    Assert.LessOrEqual(Mathf.Abs(mean.r - reference.r), 12, $"linha {row}");
                    Assert.LessOrEqual(Mathf.Abs(mean.g - reference.g), 12, $"linha {row}");
                    Assert.LessOrEqual(Mathf.Abs(mean.b - reference.b), 12, $"linha {row}");
                }
            }
            finally
            {
                Release(atlas);
            }
        }

        [Test]
        public void DrawnAtlasInFormatIsUsedAsIs()
        {
            var drawn = new Texture2D(CellTileArt.TileSize, CellTileArt.TileSize * CellTileArt.RowCount, TextureFormat.RGBA32, false);
            var pixels = new Color32[drawn.width * drawn.height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = (i / (CellTileArt.TileSize * CellTileArt.TileSize)) % 2 == 0 ? new Color32(255, 0, 0, 255) : new Color32(0, 0, 255, 255);
            drawn.SetPixels32(pixels);
            drawn.Apply();
            TerrainTileAtlas atlas = TerrainTileAtlas.Create(drawn, References(), null);
            try
            {
                Assert.AreSame(drawn, atlas.Texture);
                Assert.AreEqual(new Color32(255, 0, 0, 255), atlas.Texel(0, 3, 3));
                Assert.AreEqual(new Color32(0, 0, 255, 255), atlas.Texel(1, 3, 3));
                Assert.AreEqual(new Color32(255, 0, 0, 255), atlas.Mean(2));
            }
            finally
            {
                Release(atlas, drawn);
                Object.DestroyImmediate(drawn);
            }
        }

        [Test]
        public void DrawnAtlasOutOfFormatFallsBackToGenerated()
        {
            var drawn = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("fora do formato"));
            TerrainTileAtlas atlas = TerrainTileAtlas.Create(drawn, References(), null);
            try
            {
                Assert.IsTrue(atlas.IsFallback);
                Assert.AreNotSame(drawn, atlas.Texture);
                Assert.AreEqual(CellTileArt.TileSize * CellTileArt.RowCount, atlas.Texture.height);
            }
            finally
            {
                Release(atlas);
                Object.DestroyImmediate(drawn);
            }
        }
    }
}
