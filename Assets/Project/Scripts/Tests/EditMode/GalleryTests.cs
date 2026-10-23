using System.IO;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.Upgrades;
using Fabula.Incrementant.World;
using NUnit.Framework;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Galerias com sistema (hoje só O+1, velocidade da operária): compra, teto, requisitos, efeito e save.</summary>
    public sealed class GalleryTests
    {
        static (AntColony colony, PixelGrid grid, TerrainSettings settings) MakeColony()
        {
            var settings = new TerrainSettings();
            var grid = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout = TerrainGenerator.Generate(grid, settings, 99);
            return (new AntColony(grid, new ColonySettings(), layout, 99), grid, settings);
        }

        /// <summary>Cinco nós comprados: profundidade 5, que abre o anel A2 onde fica O+1.</summary>
        static void OpenRingTwo(AntColony colony)
        {
            foreach (QueenNode node in new[] { QueenNode.Q1, QueenNode.O1, QueenNode.O2, QueenNode.L0, QueenNode.F0 })
                Assert.AreEqual(BuyBlock.None, colony.Tree.Grant(node));
        }

        [Test]
        public void OnlyTheListedGalleryHasASystem()
        {
            Assert.IsTrue(UpgradeBoard.HasSystem(UpgradeCatalog.Get("O+1")));
            Assert.IsTrue(UpgradeBoard.HasSystem(UpgradeCatalog.Get("C1")));
            Assert.IsFalse(UpgradeBoard.HasSystem(UpgradeCatalog.Get("D+1")));
            Assert.IsFalse(UpgradeBoard.HasSystem(UpgradeCatalog.Get("C2")));
        }

        [Test]
        public void BuyingNeedsTheRequirementTheRingAndTheNutrients()
        {
            var (colony, _, _) = MakeColony();
            UpgradeDef def = UpgradeCatalog.Get("O+1");
            colony.RestoreNutrients(10000, 10000, 0);

            Assert.AreEqual(BuyBlock.NeedsDepth, colony.TryBuyUpgrade(def, 1, out int bought)); // nível 2 fechado (e sem O2)
            Assert.AreEqual(0, bought);
            Assert.AreEqual(10000, colony.Nutrients);

            OpenRingTwo(colony);
            colony.RestoreNutrients(10, 10, 0);
            Assert.AreEqual(BuyBlock.InsufficientNutrients, colony.TryBuyUpgrade(def, 1, out bought));
            Assert.AreEqual(0, colony.Galleries.Level("O+1"));
            Assert.AreEqual(10, colony.Nutrients);
        }

        [Test]
        public void EachLevelPaysItsOwnGrowingCost_AndSpeedsUpTheWorkers()
        {
            var (colony, _, _) = MakeColony();
            UpgradeDef def = UpgradeCatalog.Get("O+1");
            OpenRingTwo(colony);
            colony.RestoreNutrients(10000, 10000, 0);

            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(def, 2, out int bought));
            Assert.AreEqual(2, bought);
            Assert.AreEqual(2, colony.Galleries.Level("O+1"));
            Assert.AreEqual(10000 - (80 + 108), colony.Nutrients); // 80 × 1,35^0 + 80 × 1,35^1
            Assert.AreEqual(1.08f, colony.Galleries.Multiplier("O+1"), 1e-4f);
        }

        [Test]
        public void MaxBuysWhatTheBalancePays_UpToTheCap()
        {
            var (colony, _, _) = MakeColony();
            UpgradeDef def = UpgradeCatalog.Get("O+1");
            OpenRingTwo(colony);
            colony.RestoreNutrients(200, 200, 0); // 80 + 108 = 188 pagam 2 níveis; o terceiro (146) não cabe
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(def, 0, out int bought));
            Assert.AreEqual(2, bought);

            colony.RestoreNutrients(1000000, 1000000, 0);
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(def, 0, out bought));
            Assert.AreEqual(def.Cap - 2, bought);
            Assert.AreEqual(def.Cap, colony.Galleries.Level("O+1"));
            Assert.AreEqual(BuyBlock.Owned, colony.TryBuyUpgrade(def, 1, out bought));
            Assert.AreEqual(0, bought);
        }

        [Test]
        public void ClassGalleriesNeedTheirNodeAndScaleByTheirOwnPerLevelEffect()
        {
            var (colony, _, _) = MakeColony();
            colony.RestoreNutrients(1000000, 1000000, 0);
            // Nível 2 aberto sem mexer nas classes (T+3 completa: 3 níveis + 3 da galeria completa), para o bloqueio ser o do nó.
            colony.Galleries.Set("T+3", 3);
            foreach (string id in new[] { "L+2", "F+1", "E+1", "O+3" })
                Assert.AreEqual(BuyBlock.MissingRequirement, colony.TryBuyUpgrade(UpgradeCatalog.Get(id), 1, out _), id);

            colony.Tree.UnlockAllImplemented();
            foreach (string id in new[] { "L+2", "F+1", "E+1", "O+3" })
                Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(UpgradeCatalog.Get(id), 3, out _), id);

            Assert.AreEqual(1.12f, colony.Galleries.Multiplier("L+2"), 1e-4f); // +4% por nível
            Assert.AreEqual(0.88f, colony.Galleries.Multiplier("F+1"), 1e-4f); // −4% de tempo por nível
            Assert.AreEqual(0.88f, colony.Galleries.Multiplier("E+1"), 1e-4f);
            Assert.AreEqual(0.91f, colony.Galleries.Multiplier("O+3"), 1e-4f); // −3% de tempo por nível
        }


        [Test]
        public void ChambersAreBoughtOnceAndFeedTheEffects()
        {
            var (colony, _, _) = MakeColony();
            colony.RestoreNutrients(10000, 10000, 0);
            UpgradeDef c1 = UpgradeCatalog.Get("C1");
            Assert.AreEqual(1f, colony.Effects.BedTime, 1e-4f);
            Assert.AreEqual(BuyBlock.None, colony.TryBuyUpgrade(c1, 1, out int bought));
            Assert.AreEqual(1, bought);
            Assert.AreEqual(10000 - c1.Cost, colony.Nutrients);
            Assert.AreEqual(1f / 1.3f, colony.Effects.BedTime, 1e-4f); // canteiros 30% mais rápidos
            Assert.AreEqual(BuyBlock.Owned, colony.TryBuyUpgrade(c1, 1, out _));
            Assert.AreEqual(BuyBlock.NeedsDepth, colony.TryBuyUpgrade(UpgradeCatalog.Get("B3"), 1, out _)); // nível 3 fechado
            colony.Galleries.Set("T+3", 3); // abre o nível 2 (profundidade 6) sem mexer nos efeitos
            Assert.AreEqual(BuyBlock.MissingRequirement, colony.TryBuyUpgrade(UpgradeCatalog.Get("B1"), 1, out _)); // exige B0
        }

        [Test]
        public void EveryLiveNodeIsInTheCatalogAndSurvivesTheSave()
        {
            var (colony, grid, settings) = MakeColony();
            colony.Tree.UnlockAllImplemented();
            colony.RestoreNutrients(int.MaxValue / 2, int.MaxValue / 2, 0);
            // Compra em passes, na ordem do catálogo, até nada mais mudar (os pré-requisitos vêm de nós comprados antes).
            // Condições de mundo (ataques, adultas) e anéis fechados deixam alguns nós de fora; o teste cobre o que abre.
            colony.RestoreRaids(true, 5, 0f);
            bool changed = true;
            for (int pass = 0; pass < 8 && changed; pass++)
            {
                changed = false;
                foreach (UpgradeDef def in UpgradeCatalog.All)
                {
                    if (def.QueenNode.HasValue || !UpgradeBoard.HasSystem(def))
                        continue;
                    if (colony.TryBuyUpgrade(def, def.Kind == UpgradeKind.Gallery ? 0 : 1, out int n) == BuyBlock.None && n > 0)
                        changed = true;
                }
            }
            Assert.Greater(colony.Galleries.Level("C1"), 0);
            Assert.Greater(colony.Galleries.Level("C+1"), 0);
            Assert.Greater(colony.Galleries.Level("B+1"), 0);

            var rain = new RainSimulation(new RainSettings(), 99);
            var water = new WaterSimulation(new WaterSettings(), grid, 99);
            var stream = new MemoryStream();
            GameSnapshot.Capture(grid, colony, rain, water, 0, 99, 1.0).Write(stream);
            stream.Position = 0;
            GameSnapshot loaded = GameSnapshot.Read(stream);
            var grid2 = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout2 = TerrainGenerator.Generate(grid2, settings, 99);
            loaded.ApplyTo(grid2);
            var colony2 = new AntColony(grid2, new ColonySettings(), layout2, 99);
            loaded.ApplyTo(colony2);
            colony.Galleries.Export(out string[] ids, out int[] levels);
            for (int i = 0; i < ids.Length; i++)
                Assert.AreEqual(levels[i], colony2.Galleries.Level(ids[i]), ids[i]);
            Assert.AreEqual(colony.Effects.BedTime, colony2.Effects.BedTime, 1e-5f);
        }

        [Test]
        public void GalleryLevelsRoundTripThroughTheSave()
        {
            var (colony, grid, settings) = MakeColony();
            OpenRingTwo(colony);
            colony.RestoreNutrients(10000, 10000, 0);
            colony.TryBuyUpgrade(UpgradeCatalog.Get("O+1"), 3, out _);

            var rain = new RainSimulation(new RainSettings(), 99);
            var water = new WaterSimulation(new WaterSettings(), grid, 99);
            var stream = new MemoryStream();
            GameSnapshot.Capture(grid, colony, rain, water, 0, 99, 1.0).Write(stream);
            stream.Position = 0;
            GameSnapshot loaded = GameSnapshot.Read(stream);

            var grid2 = new PixelGrid(settings.Width, settings.Height);
            TerrainLayout layout2 = TerrainGenerator.Generate(grid2, settings, 99);
            loaded.ApplyTo(grid2);
            var colony2 = new AntColony(grid2, new ColonySettings(), layout2, 99);
            loaded.ApplyTo(colony2);

            Assert.AreEqual(3, colony2.Galleries.Level("O+1"));
            Assert.AreEqual(1.12f, colony2.Galleries.Multiplier("O+1"), 1e-4f);
        }

        [Test]
        public void RestoreDropsUnknownIdsGalleriesWithoutSystemAndLevelsAboveTheCap()
        {
            var levels = new UpgradeLevels();
            levels.Restore(new[] { "O+1", "D+1", "nada", "Q1" }, new[] { 99, 5, 3, 1 });
            Assert.AreEqual(UpgradeCatalog.Get("O+1").Cap, levels.Level("O+1"));
            Assert.AreEqual(0, levels.Level("D+1")); // galeria sem sistema
            Assert.AreEqual(0, levels.Level("Q1")); // nó da rainha: o estado é da QueenTree
            Assert.AreEqual(0, levels.Level("nada"));
        }
    }
}
