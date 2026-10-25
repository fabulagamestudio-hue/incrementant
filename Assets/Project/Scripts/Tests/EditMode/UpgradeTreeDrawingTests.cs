using System.Collections.Generic;
using Fabula.Incrementant.Upgrades;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.Tests
{
    /// <summary>Desenho da árvore (documento 48): o asset do projeto e o gerador da primeira versão.</summary>
    public sealed class UpgradeTreeDrawingTests
    {
        static UpgradeTreeDrawing LoadProjectDrawing()
        {
            var drawing = AssetDatabase.LoadAssetAtPath<UpgradeTreeDrawing>(UpgradeTreeDrawing.AssetPath);
            Assert.IsNotNull(drawing, $"{UpgradeTreeDrawing.AssetPath} ausente: rode Incrementant/Árvore/Gerar desenho");
            return drawing;
        }

        static UpgradeTreeDrawing Generated()
        {
            var drawing = ScriptableObject.CreateInstance<UpgradeTreeDrawing>();
            UpgradeTreeGenerator.Generate(drawing);
            return drawing;
        }

        static IEnumerable<UpgradeDef> Nodes()
        {
            foreach (UpgradeDef def in UpgradeCatalog.All)
                if (def.Domain != UpgradeDomain.Root)
                    yield return def;
        }

        /// <summary>Todo nó posicionado, na coluna do domínio e na faixa do nível, e a 125 px dos outros.</summary>
        static void AssertValid(UpgradeTreeDrawing drawing)
        {
            var layout = new UpgradeLayout(drawing);
            CollectionAssert.IsEmpty(layout.Missing, "nós sem posição no desenho");
            var defs = new List<UpgradeDef>(Nodes());
            for (int i = 0; i < defs.Count; i++)
            {
                UpgradeDef def = defs[i];
                Vector2 p = layout.Positions[def.Id];
                // Na faixa do nível: como o nível nunca fica acima do dos requisitos, nenhum nó fica acima de um requisito.
                Assert.That(p.y, Is.InRange(layout.TierBottom(def.Tier), layout.TierTop(def.Tier)), $"{def.Id} fora da faixa do nível {def.Tier}");
                Assert.LessOrEqual(Mathf.Abs(p.x - layout.DomainX(def.Domain)), UpgradeLayout.ColumnWidth * 0.5f, $"{def.Id} fora da coluna");
                for (int j = i + 1; j < defs.Count; j++)
                    Assert.GreaterOrEqual(Vector2.Distance(p, layout.Positions[defs[j].Id]), UpgradeTreeGenerator.MinSpacing,
                                          $"{def.Id} e {defs[j].Id} se sobrepõem");
            }
            foreach (UpgradeTreeDrawing.Tunnel t in drawing.Tunnels)
            {
                UpgradeDef to = UpgradeCatalog.Get(t.To);
                Assert.IsTrue(new List<UpgradeRequirement>(to.Requires).Exists(r => r.Id == t.From), $"túnel {t.From} → {t.To} sem requisito");
                foreach (string id in t.Via)
                    Assert.IsNotNull(drawing.FindJunction(id), $"túnel {t.From} → {t.To} passa pelo encontro {id}, que não existe");
            }
        }

        [Test]
        public void ProjectDrawingPlacesEveryNodeInsideItsColumnAndTier() => AssertValid(LoadProjectDrawing());

        [Test]
        public void GeneratedDrawingIsValid() => AssertValid(Generated());

        [Test]
        public void GeneratorIsDeterministic()
        {
            UpgradeTreeDrawing a = Generated(), b = Generated();
            Assert.AreEqual(a.Nodes.Count, b.Nodes.Count);
            for (int i = 0; i < a.Nodes.Count; i++)
                Assert.AreEqual(a.Nodes[i].Position, b.Nodes[i].Position, a.Nodes[i].Id);
        }

        [Test]
        public void RegeneratingKeepsPinnedNodesAndJunctions()
        {
            UpgradeTreeDrawing drawing = Generated();
            UpgradeTreeDrawing.Node node = drawing.FindNode("C1");
            node.Position += new Vector2(20f, -10f);
            node.Pinned = true;
            Vector2 pinned = node.Position;
            UpgradeTreeDrawing.Junction exit = drawing.FindJunction(UpgradeTreeGenerator.ExitId("C0"));
            exit.Position += new Vector2(0f, -40f);
            exit.Pinned = true;
            Vector2 exitPos = exit.Position;

            UpgradeTreeGenerator.Generate(drawing);
            Assert.AreEqual(pinned, drawing.FindNode("C1").Position);
            Assert.AreEqual(exitPos, drawing.FindJunction(UpgradeTreeGenerator.ExitId("C0")).Position);

            UpgradeTreeGenerator.Generate(drawing, keepPinned: false);
            Assert.IsFalse(drawing.FindNode("C1").Pinned);
        }

        [Test]
        public void CrossTunnelsMeetAtSharedHalls()
        {
            UpgradeTreeDrawing drawing = Generated();
            // C0 (Cultivo) libera B0, D0, R0 e B+3 em outros domínios: todos saem pelo mesmo salão.
            foreach (string child in new[] { "B0", "D0", "R0", "B+3" })
                CollectionAssert.Contains(drawing.FindTunnel("C0", child).Via, UpgradeTreeGenerator.ExitId("C0"), child);
            // M1 exige B3 e N2 (Berçário): os dois chegam pelo mesmo salão.
            CollectionAssert.Contains(drawing.FindTunnel("B3", "M1").Via, UpgradeTreeGenerator.EntryId("M1"));
            CollectionAssert.Contains(drawing.FindTunnel("N2", "M1").Via, UpgradeTreeGenerator.EntryId("M1"));
            // Ligações dentro do domínio vão direto.
            foreach (UpgradeTreeDrawing.Tunnel t in drawing.Tunnels)
                if (UpgradeCatalog.Get(t.From).Domain == UpgradeCatalog.Get(t.To).Domain)
                    CollectionAssert.IsEmpty(t.Via, $"{t.From} → {t.To}");
        }

        [Test]
        public void MissingNodeFallsBackToTheGrid()
        {
            UpgradeTreeDrawing drawing = Generated();
            drawing.Nodes.Remove(drawing.FindNode("C1"));
            var layout = new UpgradeLayout(drawing);
            CollectionAssert.AreEqual(new[] { "C1" }, layout.Missing);
            Assert.AreEqual(new UpgradeLayout().Positions["C1"], layout.Positions["C1"]);
        }
    }
}
