using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Upgrades;
using Fabula.Incrementant.World;
using NUnit.Framework;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>
    /// Minérios e forja na árvore de evolução (REQ-071, documento 46, etapa 4): as câmaras V0, V1, Y0, Y1 e Y2 liberam os
    /// pincéis das salas, e as galerias aceleram a fundição e a forja, endurecem o equipamento e reforçam quem o usa.
    /// </summary>
    public sealed class WorkshopUpgradeTests
    {
        static readonly TerrainLayout Layout = new TerrainLayout(50, new Vector2Int(50, 2), 60, 70);

        static AntColony MakeColony(PixelGrid grid = null)
        {
            grid = grid ?? MakeWorld();
            var colony = new AntColony(grid, new ColonySettings(), Layout, seed: 5) { RaidsEnabled = false };
            colony.RestoreQueen(95, 2, 1);
            colony.RestoreNutrients(200000, 200000, 0);
            return colony;
        }

        static PixelGrid MakeWorld()
        {
            var grid = new PixelGrid(120, 40);
            for (int x = 0; x < grid.Width; x++)
            {
                grid.Set(x, 0, CellMaterial.Rock);
                grid.SetSurfaceY(x, 20);
                for (int y = 1; y <= 20; y++)
                    grid.Set(x, y, CellMaterial.Dirt);
            }
            for (int x = 10; x <= 100; x++)
            for (int y = 1; y <= 3; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 50; x <= 51; x++)
            for (int y = 4; y <= 20; y++)
                grid.Set(x, y, CellMaterial.Empty);
            for (int x = 88; x <= 98; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetQueenRoom(x, y, true));
            return grid;
        }

        /// <summary>Galerias de área e de obra no teto e a profundidade que abre todos os níveis.</summary>
        static void OpenAllTiers(AntColony colony)
        {
            foreach (string id in new[] { "Q+4", "C+4", "B+3", "D+3", "R+1", "G+3", "A+3", "A+1" })
                colony.Galleries.Set(id, 10);
            colony.Galleries.Set("T+3", 3);
            QueenTreeTests.OpenTier(colony, UpgradeCatalog.TierCount - 1); // REQ-077: a profundidade do nível 5 passou a 380
        }

        [Test]
        public void TheNewNodesSitInTheOresAndForgeColumns()
        {
            foreach (string id in new[] { "V0", "V1", "V+1" })
            {
                Assert.AreEqual(UpgradeDomain.Ores, UpgradeCatalog.Get(id).Domain, id);
                Assert.AreEqual(4, UpgradeCatalog.Get(id).Tier, id);
            }
            foreach (string id in new[] { "Y0", "Y1", "Y2", "Y+1", "Y+2" })
            {
                Assert.AreEqual(UpgradeDomain.Forge, UpgradeCatalog.Get(id).Domain, id);
                Assert.AreEqual(5, UpgradeCatalog.Get(id).Tier, id);
            }
            Assert.AreEqual(UpgradeDomain.Ants, UpgradeCatalog.Get("O+4").Domain);
            Assert.AreEqual(UpgradeDomain.Digging, UpgradeCatalog.Get("E+4").Domain);
            Assert.AreEqual(UpgradeDomain.Guard, UpgradeCatalog.Get("S+5").Domain);
            Assert.AreEqual(UpgradeKind.Gallery, UpgradeCatalog.Get("Y+2").Kind);
            Assert.AreEqual(UpgradeKind.Chamber, UpgradeCatalog.Get("Y0").Kind);
        }

        [Test]
        public void TheEquipmentGalleriesAskForTheStorageOfTheirItem()
        {
            Assert.AreEqual("Y2", UpgradeCatalog.Get("O+4").Requires[0].Id, "Armadura reforçada pede o armazém de roupas e armaduras.");
            Assert.AreEqual("Y1", UpgradeCatalog.Get("E+4").Requires[0].Id, "Ferramentas afiadas pedem o armazém de ferramentas e armas.");
            Assert.AreEqual("Y1", UpgradeCatalog.Get("S+5").Requires[0].Id, "Armas balanceadas pedem o armazém de ferramentas e armas.");
        }

        [Test]
        public void TheBrushesStartLocked_AndEachChamberUnlocksItsOwn()
        {
            AntColony colony = MakeColony();
            Assert.IsFalse(colony.OreStoreUnlocked || colony.SmelterUnlocked || colony.ForgeUnlocked || colony.ArmoryUnlocked || colony.WardrobeUnlocked);
            Assert.AreEqual(BuyBlock.NeedsDepth, colony.TryBuyUpgrade(UpgradeCatalog.Get("V0"), 1, out _), "Nível 4: profundidade 240.");

            OpenAllTiers(colony);
            Assert.AreEqual(BuyBlock.MissingRequirement, colony.TryBuyUpgrade(UpgradeCatalog.Get("V1"), 1, out _), "A fundição vem depois do depósito de minério.");
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(UpgradeCatalog.Get("V0"), 1, out _));
            Assert.IsTrue(colony.OreStoreUnlocked);
            Assert.IsFalse(colony.SmelterUnlocked);

            Assert.AreEqual(BuyBlock.MissingRequirement, colony.TryBuyUpgrade(UpgradeCatalog.Get("Y0"), 1, out _));
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(UpgradeCatalog.Get("V1"), 1, out _));
            Assert.IsTrue(colony.SmelterUnlocked);
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(UpgradeCatalog.Get("Y0"), 1, out _));
            Assert.IsTrue(colony.ForgeUnlocked);
            Assert.IsFalse(colony.ArmoryUnlocked || colony.WardrobeUnlocked);
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(UpgradeCatalog.Get("Y1"), 1, out _));
            Assert.IsTrue(colony.ArmoryUnlocked);
            Assert.IsFalse(colony.WardrobeUnlocked);
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(UpgradeCatalog.Get("Y2"), 1, out _));
            Assert.IsTrue(colony.WardrobeUnlocked);
        }

        [Test]
        public void ForgeNodes_NeedTheDeepestTier()
        {
            AntColony colony = MakeColony();
            // Só o nível 4 aberto (profundidade 240 a 379): a forja, no nível 5, espera.
            QueenTreeTests.OpenTier(colony, 4);
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(UpgradeCatalog.Get("V0"), 1, out _));
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(UpgradeCatalog.Get("V1"), 1, out _));
            Assert.AreEqual(BuyBlock.NeedsDepth, colony.TryBuyUpgrade(UpgradeCatalog.Get("Y0"), 1, out _));
            Assert.IsFalse(colony.ForgeUnlocked);
        }

        [Test]
        public void WithoutPurchases_EverythingIsNeutral()
        {
            UpgradeEffects effects = MakeColony().Effects;
            Assert.AreEqual(1f, effects.SmeltTime);
            Assert.AreEqual(1f, effects.ForgeTime);
            Assert.AreEqual(1f, effects.EquipDurability);
            Assert.AreEqual(1f, effects.SharpToolTime);
            Assert.AreEqual(1f, effects.WeaponDamageBoost);
            Assert.AreEqual(1f, effects.ArmorTakenBoost);
        }

        [Test]
        public void TheGalleriesScaleTheirFactorsPerLevel()
        {
            AntColony colony = MakeColony();
            colony.RestoreGalleries(new[] { "V+1", "Y+1", "Y+2", "E+4", "S+5", "O+4" }, new[] { 10, 10, 10, 10, 10, 10 });
            UpgradeEffects effects = colony.Effects;
            Assert.AreEqual(0.5f, effects.SmeltTime, 0.001f, "Fundição rápida: 5% por nível.");
            Assert.AreEqual(0.6f, effects.ForgeTime, 0.001f, "Forja rápida: 4% por nível.");
            Assert.AreEqual(2f, effects.EquipDurability, 0.001f, "Têmpera: 10% por nível.");
            Assert.AreEqual(0.7f, effects.SharpToolTime, 0.001f, "Ferramentas afiadas: 3% por nível.");
            Assert.AreEqual(1.4f, effects.WeaponDamageBoost, 0.001f, "Armas balanceadas: 4% por nível.");
            Assert.AreEqual(0.8f, effects.ArmorTakenBoost, 0.001f, "Armadura reforçada: 2% por nível.");
        }

        [Test]
        public void FastSmelting_ShortensTheTimeOfTheBar()
        {
            PixelGrid grid = MakeWorld();
            for (int x = 20; x <= 26; x++)
            for (int y = 1; y <= 3; y++)
                Assert.IsTrue(grid.SetStorageRoom(x, y, StorageKind.Smelter, true));
            Assert.IsTrue(grid.SetStored(21, 1, StoredItems.Of(CellMaterial.IronOre)));
            Assert.IsTrue(grid.SetStored(23, 1, StoredItems.Litter));
            RoomLiningTests.LineAllRooms(grid);
            AntColony colony = MakeColony(grid);
            colony.RestoreGalleries(new[] { "V+1" }, new[] { 10 });

            float half = AntColony.SmeltSeconds * 0.5f;
            for (float t = 0f; t < half - 1f; t += 0.1f)
                colony.Tick(0.1f);
            Assert.AreEqual(0, colony.BarsSmelted, "Ainda dentro do tempo reduzido.");
            for (float t = 0f; t < 3f; t += 0.1f)
                colony.Tick(0.1f);
            Assert.AreEqual(1, colony.BarsSmelted, "Com a fundição rápida no nível 10 a barra fica pronta na metade do tempo.");
        }

        [Test]
        public void TheNewNodesHaveASystemAndTexts()
        {
            foreach (string id in new[] { "V0", "V1", "V+1", "Y0", "Y1", "Y2", "Y+1", "Y+2", "O+4", "E+4", "S+5" })
                Assert.IsTrue(UpgradeBoard.HasSystem(UpgradeCatalog.Get(id)), id);
        }
    }
}
