using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.Upgrades
{
    /// <summary>
    /// Primeira versão automática do <see cref="UpgradeTreeDrawing"/> (documento 48). Os nós partem do layout em grade
    /// (<see cref="UpgradeLayout"/>) e se acomodam: atraídos em x pelos ligados (pouco pelos de outro domínio), puxados para o
    /// eixo da coluna e afastados uns dos outros, sem sair da caixa do domínio na faixa do nível (<see cref="Box"/>). Túneis
    /// entre domínios se encontram em salões: a saída de um requisito com vários filhos de fora (<c>out:Id</c>) e a entrada
    /// de um nó com vários pais de fora (<c>in:Id</c>). Nós, encontros e túneis fixados ficam como estão. Determinístico.
    /// </summary>
    public static class UpgradeTreeGenerator
    {
        /// <summary>Distância mínima entre nós (a mesma do teste do layout em grade).</summary>
        public const float MinSpacing = 125f;
        /// <summary>Meia largura da caixa do nó em volta do eixo da coluna: a coluna continua reconhecível.</summary>
        public const float ColumnHalfWidth = UpgradeLayout.ColumnWidth * 0.5f - 60f;
        /// <summary>Folga abaixo do topo da faixa (o salão do domínio) e acima do fim dela.</summary>
        public const float TopClearance = UpgradeLayout.BandTop + 50f, BottomClearance = UpgradeLayout.BandBottom + 20f;

        const int Iterations = 240, SeparationPasses = 120;
        const float Spacing = 150f, LinkPull = 0.08f, CrossPull = 0.02f, ColumnPull = 0.03f;
        // Encontro: fração do caminho entre o nó e o centro dos ligados de fora; distância mínima a qualquer nó.
        const float JunctionReach = 0.4f, JunctionClearance = 90f, JunctionStep = 30f;

        /// <summary>Caixa onde o nó do domínio e nível dados pode ficar.</summary>
        public static Rect Box(UpgradeLayout layout, UpgradeDomain domain, int tier)
        {
            float x = layout.DomainX(domain);
            return Rect.MinMaxRect(x - ColumnHalfWidth, layout.TierBottom(tier) + BottomClearance,
                                   x + ColumnHalfWidth, layout.TierTop(tier) - TopClearance);
        }

        public static string ExitId(string parent) => "out:" + parent;
        public static string EntryId(string child) => "in:" + child;

        /// <summary>
        /// Refaz o desenho a partir do catálogo. <paramref name="keepPinned"/> false desfixa tudo antes (recomeça do zero).
        /// Remove nós, túneis e encontros que o catálogo não tem mais.
        /// </summary>
        public static void Generate(UpgradeTreeDrawing drawing, bool keepPinned = true)
        {
            if (!keepPinned)
            {
                drawing.Nodes.ForEach(n => n.Pinned = false);
                drawing.Junctions.ForEach(j => j.Pinned = false);
                drawing.Tunnels.ForEach(t => t.Pinned = false);
            }
            var layout = new UpgradeLayout();
            var defs = new List<UpgradeDef>();
            foreach (UpgradeDef def in UpgradeCatalog.All)
                if (def.Domain != UpgradeDomain.Root)
                    defs.Add(def);
            PlaceNodes(drawing, layout, defs);
            RouteTunnels(drawing, defs);
        }

        // ---------------------------------------------------------------- nós

        static void PlaceNodes(UpgradeTreeDrawing drawing, UpgradeLayout layout, List<UpgradeDef> defs)
        {
            int count = defs.Count;
            var index = new Dictionary<string, int>();
            var pos = new Vector2[count];
            var fixedNode = new bool[count];
            var box = new Rect[count];
            for (int i = 0; i < count; i++)
            {
                UpgradeDef def = defs[i];
                index[def.Id] = i;
                box[i] = Box(layout, def.Domain, def.Tier);
                UpgradeTreeDrawing.Node node = drawing.FindNode(def.Id);
                fixedNode[i] = node != null && node.Pinned;
                pos[i] = fixedNode[i] ? node.Position : Clamp(layout.Positions[def.Id], box[i]);
            }
            var links = new List<(int a, int b, float pull)>();
            foreach (UpgradeDef def in defs)
                foreach (UpgradeRequirement r in def.Requires)
                    if (index.TryGetValue(r.Id, out int parent))
                        links.Add((parent, index[def.Id], UpgradeCatalog.Get(r.Id).Domain == def.Domain ? LinkPull : CrossPull));

            for (int it = 0; it < Iterations; it++)
            {
                foreach ((int a, int b, float pull) in links)
                {
                    float dx = (pos[b].x - pos[a].x) * pull;
                    Move(pos, fixedNode, a, b, new Vector2(dx, 0f));
                }
                for (int i = 0; i < count; i++)
                    if (!fixedNode[i])
                        pos[i].x += (box[i].center.x - pos[i].x) * ColumnPull;
                Separate(pos, fixedNode, Spacing);
                for (int i = 0; i < count; i++)
                    if (!fixedNode[i])
                        pos[i] = Clamp(pos[i], box[i]);
            }
            // Acabamento: só afastar, até a distância mínima com uma folga.
            for (int pass = 0; pass < SeparationPasses; pass++)
            {
                if (!Separate(pos, fixedNode, MinSpacing + 5f))
                    break;
                for (int i = 0; i < count; i++)
                    if (!fixedNode[i])
                        pos[i] = Clamp(pos[i], box[i]);
            }

            drawing.Nodes.RemoveAll(n => !index.ContainsKey(n.Id ?? ""));
            for (int i = 0; i < count; i++)
            {
                UpgradeTreeDrawing.Node node = drawing.FindNode(defs[i].Id);
                if (node == null)
                    drawing.Nodes.Add(node = new UpgradeTreeDrawing.Node { Id = defs[i].Id });
                if (!node.Pinned)
                    node.Position = Round(pos[i]);
            }
            // Na ordem do catálogo, para o asset ficar legível e estável.
            drawing.Nodes.Sort((x, y) => index[x.Id].CompareTo(index[y.Id]));
        }

        /// <summary>Puxa a e b um para o outro (b recebe −delta); se um está fixo, o outro anda o dobro.</summary>
        static void Move(Vector2[] pos, bool[] fixedNode, int a, int b, Vector2 delta)
        {
            if (fixedNode[a] && fixedNode[b])
                return;
            if (fixedNode[a])
                pos[b] -= delta * 2f;
            else if (fixedNode[b])
                pos[a] += delta * 2f;
            else
            {
                pos[a] += delta;
                pos[b] -= delta;
            }
        }

        /// <summary>Afasta cada par mais perto que <paramref name="spacing"/>; diz se algum par estava perto.</summary>
        static bool Separate(Vector2[] pos, bool[] fixedNode, float spacing)
        {
            bool any = false;
            for (int i = 0; i < pos.Length; i++)
            for (int j = i + 1; j < pos.Length; j++)
            {
                Vector2 d = pos[j] - pos[i];
                float dist = d.magnitude;
                if (dist >= spacing)
                    continue;
                any = true;
                // Nós no mesmo ponto: separa na horizontal, pelo índice (determinístico).
                Vector2 dir = dist > 0.01f ? d / dist : Vector2.right;
                Move(pos, fixedNode, i, j, -dir * ((spacing - dist) * 0.5f));
            }
            return any;
        }

        static Vector2 Clamp(Vector2 p, Rect box) =>
            new Vector2(Mathf.Clamp(p.x, box.xMin, box.xMax), Mathf.Clamp(p.y, Mathf.Min(box.yMin, box.yMax), box.yMax));

        static Vector2 Round(Vector2 p) => new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));

        // ---------------------------------------------------------------- túneis e encontros

        static void RouteTunnels(UpgradeTreeDrawing drawing, List<UpgradeDef> defs)
        {
            var pos = new Dictionary<string, Vector2>();
            foreach (UpgradeTreeDrawing.Node n in drawing.Nodes)
                pos[n.Id] = n.Position;

            // Ligações entre domínios (sem a raiz), por pai e por filho.
            var outOf = new Dictionary<string, List<string>>();
            var into = new Dictionary<string, List<string>>();
            var wanted = new List<(string from, string to)>();
            foreach (UpgradeDef def in defs)
                foreach (UpgradeRequirement r in def.Requires)
                {
                    wanted.Add((r.Id, def.Id));
                    UpgradeDef parent = UpgradeCatalog.Get(r.Id);
                    if (parent.Domain == UpgradeDomain.Root || parent.Domain == def.Domain)
                        continue;
                    Add(outOf, r.Id, def.Id);
                    Add(into, def.Id, r.Id);
                }

            drawing.Tunnels.RemoveAll(t => !wanted.Contains((t.From, t.To)));
            var used = new HashSet<string>();
            var junctionPos = new Dictionary<string, Vector2>();
            foreach ((string from, string to) in wanted)
            {
                UpgradeTreeDrawing.Tunnel tunnel = drawing.FindTunnel(from, to);
                if (tunnel == null)
                    drawing.Tunnels.Add(tunnel = new UpgradeTreeDrawing.Tunnel { From = from, To = to });
                if (!tunnel.Pinned)
                {
                    tunnel.Via.Clear();
                    UpgradeDomain fromDomain = UpgradeCatalog.Get(from).Domain, toDomain = UpgradeCatalog.Get(to).Domain;
                    if (fromDomain == UpgradeDomain.Root || fromDomain == toDomain)
                        continue;
                    if (outOf.TryGetValue(from, out List<string> kids) && kids.Count > 1)
                    {
                        tunnel.Via.Add(ExitId(from));
                        junctionPos[ExitId(from)] = Toward(pos[from], kids, pos);
                    }
                    if (into.TryGetValue(to, out List<string> parents) && parents.Count > 1)
                    {
                        tunnel.Via.Add(EntryId(to));
                        junctionPos[EntryId(to)] = Toward(pos[to], parents, pos);
                    }
                }
                foreach (string id in tunnel.Via)
                    used.Add(id);
            }

            drawing.Junctions.RemoveAll(j => !used.Contains(j.Id));
            foreach (KeyValuePair<string, Vector2> pair in junctionPos)
            {
                UpgradeTreeDrawing.Junction junction = drawing.FindJunction(pair.Key);
                if (junction == null)
                    drawing.Junctions.Add(junction = new UpgradeTreeDrawing.Junction { Id = pair.Key });
                if (!junction.Pinned)
                    junction.Position = Round(Clear(pair.Value, pos));
            }
            // Um túnel fixado pode citar um encontro que sumiu: a dobra é descartada.
            foreach (UpgradeTreeDrawing.Tunnel t in drawing.Tunnels)
                t.Via.RemoveAll(id => drawing.FindJunction(id) == null);
            drawing.Junctions.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        }

        static void Add(Dictionary<string, List<string>> map, string key, string value)
        {
            if (!map.TryGetValue(key, out List<string> list))
                map[key] = list = new List<string>();
            list.Add(value);
        }

        /// <summary>Ponto entre o nó e o centro dos ligados de fora: o salão onde os túneis dele se juntam.</summary>
        static Vector2 Toward(Vector2 from, List<string> others, Dictionary<string, Vector2> pos)
        {
            Vector2 sum = Vector2.zero;
            foreach (string id in others)
                sum += pos[id];
            return Vector2.Lerp(from, sum / others.Count, JunctionReach);
        }

        /// <summary>Desce o encontro até ficar longe de todos os nós (no máximo algumas tentativas).</summary>
        static Vector2 Clear(Vector2 p, Dictionary<string, Vector2> pos)
        {
            for (int tries = 0; tries < 12; tries++)
            {
                bool near = false;
                foreach (Vector2 n in pos.Values)
                    if ((n - p).sqrMagnitude < JunctionClearance * JunctionClearance)
                    {
                        near = true;
                        break;
                    }
                if (!near)
                    break;
                p.y -= JunctionStep;
            }
            return p;
        }
    }
}
