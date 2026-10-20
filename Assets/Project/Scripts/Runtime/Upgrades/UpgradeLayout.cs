using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.Upgrades
{
    /// <summary>
    /// Posição de cada nó no formigueiro radial da tela Evolução (documento 39, "O padrão formigueiro"): a câmara central
    /// na origem, um túnel por ramo (8 direções a 45°, o primeiro logo à direita do topo, em sentido horário) e um anel por
    /// profundidade. Os nós de um mesmo ramo e anel formam uma grade de no máximo duas colunas ao lado do eixo do túnel,
    /// com as câmaras antes das galerias. Coordenadas em pixels de UI, y para cima, centro da árvore em (0, 0).
    /// </summary>
    public static class UpgradeLayout
    {
        public const float FirstRingRadius = 420f;
        public const float RingStep = 380f;
        /// <summary>Distância entre linhas (no sentido do raio) e entre as duas colunas (no sentido do anel).</summary>
        public const float RowSpacing = 140f;
        public const float ColumnSpacing = 130f;
        public const int BranchCount = 8;

        /// <summary>Raio do meio do anel (1 a 4).</summary>
        public static float RingRadius(int ring) => ring <= 0 ? 0f : FirstRingRadius + RingStep * (ring - 1);

        /// <summary>Raio da borda externa do anel: os anéis se encostam no meio do caminho entre os centros.</summary>
        public static float RingOuterRadius(int ring) => ring <= 0 ? FirstRingRadius - RingStep * 0.5f : RingRadius(ring) + RingStep * 0.5f;

        /// <summary>Direção do túnel do ramo (vetor unitário, y para cima).</summary>
        public static Vector2 BranchDirection(int branch)
        {
            float angle = (90f - (branch + 0.5f) * (360f / BranchCount)) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>Posições dos nós da aba (os da câmara central ficam todos em (0, 0)).</summary>
        public static Dictionary<string, Vector2> Positions(UpgradeTab tab)
        {
            var result = new Dictionary<string, Vector2>();
            var groups = new Dictionary<int, List<UpgradeDef>>();
            foreach (UpgradeDef def in UpgradeCatalog.OfTab(tab))
            {
                if (def.IsCenter)
                {
                    result[def.Id] = Vector2.zero;
                    continue;
                }
                int key = def.Branch * 10 + def.Ring;
                if (!groups.TryGetValue(key, out List<UpgradeDef> list))
                    groups[key] = list = new List<UpgradeDef>();
                list.Add(def);
            }
            foreach (KeyValuePair<int, List<UpgradeDef>> pair in groups)
            {
                List<UpgradeDef> list = pair.Value;
                // Câmaras primeiro (mais perto do centro e do eixo), galerias depois; a ordem da tabela desempata.
                var ordered = new List<UpgradeDef>(list.Count);
                foreach (UpgradeDef def in list)
                    if (def.Kind == UpgradeKind.Chamber)
                        ordered.Add(def);
                foreach (UpgradeDef def in list)
                    if (def.Kind == UpgradeKind.Gallery)
                        ordered.Add(def);
                Place(ordered, pair.Key / 10, pair.Key % 10, result);
            }
            return result;
        }

        static void Place(List<UpgradeDef> nodes, int branch, int ring, Dictionary<string, Vector2> result)
        {
            Vector2 axis = BranchDirection(branch);
            var side = new Vector2(axis.y, -axis.x); // perpendicular, no sentido horário
            int columns = nodes.Count == 1 ? 1 : 2;
            int rows = (nodes.Count + columns - 1) / columns;
            for (int i = 0; i < nodes.Count; i++)
            {
                int row = i / columns;
                int column = i % columns;
                // A última linha com um nó só fica no eixo.
                int inRow = Math.Min(columns, nodes.Count - row * columns);
                float lateral = inRow == 1 ? 0f : (column - 0.5f) * ColumnSpacing;
                float radius = RingRadius(ring) + (row - (rows - 1) * 0.5f) * RowSpacing;
                result[nodes[i].Id] = axis * radius + side * lateral;
            }
        }

        /// <summary>Raio que cobre todos os nós da aba mais uma margem: o tamanho do conteúdo arrastável.</summary>
        public static float ContentRadius() => RingOuterRadius(4) + 120f;
    }
}
