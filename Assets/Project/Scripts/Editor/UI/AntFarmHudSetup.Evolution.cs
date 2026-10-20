using System.Collections.Generic;
using System.Linq;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.EditorTools.Localization;
using Fabula.Incrementant.UI;
using Fabula.Incrementant.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Páginas da tela Evolução no padrão formigueiro (documento 39): janela arrastável com o conteúdo radial, anéis de
    /// profundidade, túneis, câmaras, galerias com selo de nível e portais para os requisitos da outra aba. As posições
    /// vêm de <see cref="UpgradeLayout"/> e a ordem dos widgets de <see cref="SkillTreeScreen.NodesOf"/>.
    /// Arte provisória: losango do kit para os dois tipos (galeria menor), ícones gerais onde o kit não tem ícone.
    /// </summary>
    public static partial class AntFarmHudSetup
    {
        // Área da janela da árvore na tela 1920x1080 (entre as abas e a legenda).
        static readonly Vector2 TreeViewTop = new Vector2(0, 210);
        static readonly Vector2 TreeViewSize = new Vector2(1920, 790);
        const float GalleryScale = 0.75f;
        static readonly Color PortalColor = new Color(0.30f, 0.22f, 0.14f, 0.95f);
        static readonly Color RingLabelColor = new Color(0.72f, 0.66f, 0.55f, 0.9f);

        static void BuildUpgradePage(Transform page, UpgradeTab tab, SerializedObject screen, string field)
        {
            // --- janela (recebe arrasto e roda) e conteúdo
            var viewportGo = new GameObject("Janela", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(TreeViewport));
            viewportGo.layer = LayerMask.NameToLayer("UI");
            viewportGo.transform.SetParent(page, false);
            var viewportRect = (RectTransform)viewportGo.transform;
            PlaceTop(viewportRect, TreeViewTop, TreeViewSize);
            var catcher = viewportGo.GetComponent<Image>();
            catcher.color = new Color(0, 0, 0, 0); // invisível, só para receber o arrasto
            catcher.raycastTarget = true;

            float radius = UpgradeLayout.ContentRadius();
            var contentGo = new GameObject("Conteudo", typeof(RectTransform));
            contentGo.layer = LayerMask.NameToLayer("UI");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var content = (RectTransform)contentGo.transform;
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(radius * 2f, radius * 2f);
            content.anchoredPosition = Vector2.zero;
            content.localScale = new Vector3(0.6f, 0.6f, 1f);

            var viewport = viewportGo.GetComponent<TreeViewport>();
            var viewportSo = new SerializedObject(viewport);
            SetObject(viewportSo, "_content", content);
            viewportSo.FindProperty("_contentRadius").floatValue = radius;
            viewportSo.ApplyModifiedPropertiesWithoutUndo();

            List<UpgradeDef> defs = SkillTreeScreen.NodesOf(tab);
            Dictionary<string, Vector2> positions = UpgradeLayout.Positions(tab);
            var index = new Dictionary<string, int>();
            for (int i = 0; i < defs.Count; i++)
                index[defs[i].Id] = i;

            // --- terreno escavado de cada ramo (KI-046), embaixo dos túneis
            CaveMap caves = BuildCaves(content, defs, positions, tab);

            // --- ligações de dependência, pelos túneis (sobre a caverna)
            var links = new List<(TunnelPathGraphic path, int parent, int child)>();
            Transform linkLayer = Layer(content, "Tuneis");
            Transform labelLayer = null;
            for (int i = 0; i < defs.Count; i++)
            {
                UpgradeDef def = defs[i];
                foreach (UpgradeRequirement r in def.Requires)
                {
                    UpgradeDef target = UpgradeCatalog.Get(r.Id);
                    if (target.Tab != tab)
                        continue;
                    int parent = target.IsCenter ? -1 : index[target.Id];
                    Vector2[] route = caves.Route(target, positions[target.Id], def, positions[def.Id]);
                    links.Add((BuildPathLink(linkLayer, route), parent, i));
                    if (r.Level > 0)
                    {
                        labelLayer ??= Layer(content, "NiveisDosTuneis");
                        Text level = BuildText(labelLayer, "Nv" + r, "", TextSmall, Cream, TextAnchor.MiddleCenter);
                        level.text = "Nv " + r.Level;
                        level.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
                        PlaceInContent(level.rectTransform, Halfway(route), new Vector2(80, 26));
                    }
                }
            }
            if (labelLayer != null)
                labelLayer.SetAsLastSibling();

            // --- anéis: terra escura dos fechados, acima dos túneis e abaixo dos nós
            Transform ringLayer = Layer(content, "Aneis");
            var rings = new RingGraphic[4];
            var ringLabels = new Text[4];
            for (int ring = 1; ring <= 4; ring++)
            {
                var ringGo = new GameObject("Anel" + ring, typeof(RectTransform), typeof(RingGraphic));
                ringGo.layer = LayerMask.NameToLayer("UI");
                ringGo.transform.SetParent(ringLayer, false);
                var ringRect = (RectTransform)ringGo.transform;
                float outer = UpgradeLayout.RingOuterRadius(ring);
                PlaceInContent(ringRect, Vector2.zero, new Vector2(outer * 2f, outer * 2f));
                var graphic = ringGo.GetComponent<RingGraphic>();
                graphic.SetRadii(UpgradeLayout.RingOuterRadius(ring - 1), outer);
                graphic.raycastTarget = false;
                graphic.enabled = false;
                rings[ring - 1] = graphic;
                Text label = BuildText(ringLayer, "AvisoAnel" + ring, "", TextMedium, RingLabelColor, TextAnchor.MiddleCenter);
                PlaceInContent(label.rectTransform, new Vector2(0, outer - 40f), new Vector2(700, 34));
                ringLabels[ring - 1] = label;
            }

            // --- rótulos dos ramos, perto do centro
            for (int b = 0; b < UpgradeLayout.BranchCount; b++)
            {
                string key = UpgradeCatalog.Branches[(int)tab][b];
                // Rótulo no começo do túnel, alinhado para fora (à esquerda dos ramos da esquerda, à direita dos da direita),
                // para os ramos de cima e de baixo, que ficam perto, não se encostarem.
                Vector2 axis = UpgradeLayout.BranchDirection(b);
                bool right = axis.x >= 0f;
                Text label = BuildText(content, "Ramo" + b, PtOf(key), TextSmall, Green, right ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
                label.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
                PlaceInContent(label.rectTransform, axis * 190f + new Vector2(right ? 110f : -110f, 0f), new Vector2(220, 26));
            }

            // --- câmara central e nós
            var root = tab == UpgradeTab.Hive
                ? BuildNode(content, "Centro", null, Vector2.zero, "Icons/skill_HUB_large", null, 64, true, out _, kitIcon: true)
                : BuildNode(content, "Centro", null, Vector2.zero, "icons/crown", null, 40, true, out _);
            ToContent(root, Vector2.zero, 1f);
            var widgets = new SkillNodeWidget[defs.Count];
            var portals = new List<(Button button, string target)>();
            for (int i = 0; i < defs.Count; i++)
            {
                UpgradeDef def = defs[i];
                bool gallery = def.Kind == UpgradeKind.Gallery;
                (string icon, string locked, bool kit) = UpgradeIcon(def);
                var widget = BuildNode(content, def.Id, null, Vector2.zero, icon, locked, gallery ? 32 : 44, false, out _, kitIcon: kit);
                ToContent(widget, positions[def.Id], gallery ? GalleryScale : 1f);
                var group = widget.gameObject.AddComponent<CanvasGroup>();
                // Os nós mostram só o ícone: sem ID, nome nem selo de nível (o nível fica no painel de detalhe).
                var widgetSo = new SerializedObject(widget);
                SetObject(widgetSo, "_group", group);
                widgetSo.ApplyModifiedPropertiesWithoutUndo();
                widgets[i] = widget;

                int crossIndex = 0;
                foreach (UpgradeRequirement r in def.Requires)
                {
                    if (UpgradeCatalog.Get(r.Id).Tab == tab)
                        continue;
                    portals.Add((BuildPortal(content, def, r, positions[def.Id], crossIndex++), r.Id));
                }
            }

            string prefix = field + ".";
            SetObject(screen, prefix + "Viewport", viewport);
            SetObject(screen, prefix + "Root", root);
            SetArray(screen, prefix + "Nodes", widgets);
            WriteLinks(screen, prefix + "Links", links);
            SetArray(screen, prefix + "Rings", rings);
            SetArray(screen, prefix + "RingLabels", ringLabels);
            SerializedProperty portalList = screen.FindProperty(prefix + "Portals");
            portalList.arraySize = portals.Count;
            for (int i = 0; i < portals.Count; i++)
            {
                SerializedProperty element = portalList.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Button").objectReferenceValue = portals[i].button;
                element.FindPropertyRelative("Target").stringValue = portals[i].target;
            }
        }

        // Terra escavada: parede clara (cópia engordada por baixo) e vão escuro, com um tom por ramo para demarcar as áreas.
        static readonly Color CaveWallColor = new Color(0.52f, 0.38f, 0.25f, 1f);
        static readonly Color CaveBaseColor = new Color(0.075f, 0.05f, 0.038f, 1f);
        static readonly Color[] CaveTints =
        {
            new Color(0.35f, 0.75f, 0.35f), new Color(0.80f, 0.55f, 0.25f), new Color(0.35f, 0.60f, 0.85f), new Color(0.85f, 0.35f, 0.35f),
            new Color(0.70f, 0.45f, 0.85f), new Color(0.85f, 0.80f, 0.30f), new Color(0.35f, 0.80f, 0.75f), new Color(0.85f, 0.50f, 0.65f),
        };
        // Salão de um grupo de nós: folga em volta dos nós (câmara/galeria), folga até o piso, proporção mínima largura/altura.
        const float CavePadChamber = 72f, CavePadGallery = 56f, CaveFloorGap = 48f, CaveMinAspect = 1.3f;
        const float CaveTunnelWidth = 34f, CaveWall = 13f;
        // Câmara da rainha: meia largura e meia altura.
        static readonly Vector2 CaveHallSize = new Vector2(150f, 112f);

        /// <summary>
        /// Cada ramo vira um trecho de formigueiro em corte: um salão por grupo de nós (os do mesmo anel, que o
        /// <see cref="UpgradeLayout"/> já junta em grade), mais largo que alto e de piso plano, e um túnel fino e sinuoso da
        /// câmara da rainha ao primeiro salão e de cada salão ao do anel seguinte. Tudo do ramo vai numa malha só, com o tom dele
        /// (<see cref="CaveGraphic"/>). Cada ramo desenha a parede e depois o vão, então o que passa por cima de outro ramo mostra
        /// a borda; a câmara central fica por último. Os anéis fechados cobrem tudo com terra escura.
        /// </summary>
        static CaveMap BuildCaves(Transform content, List<UpgradeDef> defs, Dictionary<string, Vector2> positions, UpgradeTab tab)
        {
            var map = new CaveMap();
            // Caixa dos nós de cada grupo (ramo, anel): mínimo, máximo e a maior folga dos nós dele.
            var boxes = new SortedDictionary<int, (Vector2 min, Vector2 max, float pad)>();
            foreach (UpgradeDef def in defs)
            {
                if (def.IsCenter)
                    continue;
                Vector2 p = positions[def.Id];
                float pad = def.Kind == UpgradeKind.Gallery ? CavePadGallery : CavePadChamber;
                int key = def.Branch * 10 + def.Ring;
                boxes[key] = boxes.TryGetValue(key, out var box)
                    ? (Vector2.Min(box.min, p), Vector2.Max(box.max, p), Mathf.Max(box.pad, pad))
                    : (p, p, pad);
            }

            var chambers = new List<Vector4>[UpgradeLayout.BranchCount];
            var tunnels = new List<Vector4>[UpgradeLayout.BranchCount];
            var previous = new Vector2[UpgradeLayout.BranchCount];
            for (int b = 0; b < UpgradeLayout.BranchCount; b++)
            {
                chambers[b] = new List<Vector4>();
                tunnels[b] = new List<Vector4>();
            }
            foreach (KeyValuePair<int, (Vector2 min, Vector2 max, float pad)> pair in boxes) // anéis em ordem crescente
            {
                int b = pair.Key / 10;
                (Vector2 min, Vector2 max, float pad) = pair.Value;
                // O piso fica uma folga abaixo do nó mais baixo e o teto uma folga acima do mais alto; o centro do salão sobe para
                // o piso cortado (CaveGraphic.FloorFraction da meia altura) cair nessa linha.
                float height = (max.y + pad) - (min.y - CaveFloorGap);
                float halfHeight = height / 1.82f;
                float halfWidth = Mathf.Max((max.x - min.x) * 0.5f + pad, halfHeight * CaveMinAspect);
                var center = new Vector2((min.x + max.x) * 0.5f, max.y + pad - halfHeight);
                map.Chamber[pair.Key] = chambers[b].Count;
                chambers[b].Add(new Vector4(center.x, center.y, halfWidth, halfHeight));
                tunnels[b].Add(new Vector4(previous[b].x, previous[b].y, center.x, center.y));
                previous[b] = center;
            }

            Transform layer = Layer(content, "Cavernas");
            for (int b = 0; b < UpgradeLayout.BranchCount; b++)
            {
                BuildCave(layer, "Parede" + b, chambers[b], tunnels[b], CaveWallColor, CaveWall);
                BuildCave(layer, "Ramo" + b, chambers[b], tunnels[b], Color.Lerp(CaveBaseColor, CaveTints[b] * 0.5f, 0.6f), 0f);
            }
            var hall = new List<Vector4> { new Vector4(0f, 0f, CaveHallSize.x, CaveHallSize.y) };
            BuildCave(layer, "ParedeCentro", hall, new List<Vector4>(), CaveWallColor, CaveWall);
            BuildCave(layer, "Centro", hall, new List<Vector4>(), CaveBaseColor, 0f);
            map.Tunnels = tunnels;
            return map;
        }

        /// <summary>
        /// Salões e túneis montados por <see cref="BuildCaves"/>, para traçar as ligações por dentro deles. O túnel k de um ramo
        /// liga o salão k − 1 (ou a câmara da rainha, para k = 0) ao salão k.
        /// </summary>
        sealed class CaveMap
        {
            public readonly Dictionary<int, int> Chamber = new Dictionary<int, int>(); // ramo × 10 + anel → salão no ramo
            public List<Vector4>[] Tunnels;

            /// <summary>
            /// Caminho de <paramref name="from"/> até <paramref name="to"/>: do nó ao centro do salão dele, pelos túneis do
            /// ramo (subindo até a câmara da rainha quando os ramos são outros) até o salão do destino e então ao nó.
            /// </summary>
            public Vector2[] Route(UpgradeDef from, Vector2 fromPos, UpgradeDef to, Vector2 toPos)
            {
                var points = new List<Vector2> { fromPos };
                int toChamber = Chamber[to.Branch * 10 + to.Ring];
                if (from.IsCenter)
                    Walk(points, to.Branch, -1, toChamber);
                else
                {
                    int fromChamber = Chamber[from.Branch * 10 + from.Ring];
                    if (from.Branch == to.Branch)
                        Walk(points, to.Branch, fromChamber, toChamber);
                    else
                    {
                        Walk(points, from.Branch, fromChamber, -1);
                        Walk(points, to.Branch, -1, toChamber);
                    }
                }
                Add(points, toPos);
                return points.ToArray();
            }

            // Do salão "from" ao salão "to" do mesmo ramo (-1 = câmara da rainha), pelo eixo dos túneis do meio.
            void Walk(List<Vector2> points, int branch, int from, int to)
            {
                List<Vector4> tunnels = Tunnels[branch];
                for (int k = from + 1; k <= to; k++)
                    AddTunnel(points, tunnels[k], false);
                for (int k = from; k > to; k--)
                    AddTunnel(points, tunnels[k], true);
            }

            static void AddTunnel(List<Vector2> points, Vector4 tunnel, bool backwards)
            {
                int pieces = CaveGraphic.TunnelPieces(new Vector2(tunnel.z - tunnel.x, tunnel.w - tunnel.y).magnitude);
                for (int i = 0; i <= pieces; i++)
                {
                    float u = i / (float)pieces;
                    Add(points, CaveGraphic.TunnelPoint(tunnel, backwards ? 1f - u : u));
                }
            }

            static void Add(List<Vector2> points, Vector2 p)
            {
                if ((points[points.Count - 1] - p).sqrMagnitude > 0.25f)
                    points.Add(p);
            }
        }

        /// <summary>Ponto no meio do comprimento do caminho (onde vai o "Nv N" da ligação).</summary>
        static Vector2 Halfway(Vector2[] route)
        {
            float total = 0f;
            for (int i = 0; i + 1 < route.Length; i++)
                total += (route[i + 1] - route[i]).magnitude;
            float left = total * 0.5f;
            for (int i = 0; i + 1 < route.Length; i++)
            {
                float length = (route[i + 1] - route[i]).magnitude;
                if (left <= length && length > 0f)
                    return Vector2.Lerp(route[i], route[i + 1], left / length);
                left -= length;
            }
            return route[route.Length - 1];
        }

        static void BuildCave(Transform parent, string name, List<Vector4> chambers, List<Vector4> tunnels, Color color, float grow)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CaveGraphic));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Stretch(rect);
            var graphic = go.GetComponent<CaveGraphic>();
            graphic.color = color;
            graphic.raycastTarget = false;
            graphic.Set(chambers.ToArray(), tunnels.ToArray(), CaveTunnelWidth, grow);
        }

        /// <summary>Ícone do nó: os do kit quando existem (C, B, D, R antigos); senão, o ícone geral do ramo.</summary>
        static (string icon, string locked, bool kit) UpgradeIcon(UpgradeDef def)
        {
            if (def.Tab == UpgradeTab.Hive && def.Kind == UpgradeKind.Chamber &&
                AssetDatabase.LoadAssetAtPath<Sprite>($"{Kit}/Icons/skill_{def.Id}.png") != null)
                return ($"Icons/skill_{def.Id}", $"IconsLocked/skill_{def.Id}_locked", true);
            if (def.Tab == UpgradeTab.Queen)
                return (QueenIcon((QueenBranch)def.Branch), null, false);
            string[] hive = { "icons/mushroom", "icons/larva", "icons/crate", "icons/minus", "icons/leaf", "icons/shovel", "icons/arrow_up", "icons/warning" };
            return (hive[def.Branch], null, false);
        }

        /// <summary>Abertura para um requisito da outra aba: etiqueta clicável acima e à direita do nó.</summary>
        static Button BuildPortal(Transform content, UpgradeDef def, UpgradeRequirement requirement, Vector2 nodePosition, int order)
        {
            Image image = BuildPlain(content, "Portal" + def.Id + "_" + requirement.Id, PortalColor);
            image.raycastTarget = true;
            PlaceInContent(image.rectTransform, nodePosition + new Vector2(78f, 40f - order * 32f), new Vector2(40, 28));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = BuildText(image.transform, "Texto", "", TextSmall, Cream, TextAnchor.MiddleCenter);
            text.text = "»"; // o ID do requisito não aparece para o jogador; o painel de detalhe lista o nome
            Stretch(text.rectTransform);
            return button;
        }

        /// <summary>Texto-fonte (português) de uma chave do catálogo, para o rótulo estático ficar ligado à tabela.</summary>
        static string PtOf(string key) => LocalizationCatalog.All.First(e => e.Key == key).Pt;

        static Transform Layer(Transform content, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(content, false);
            Stretch((RectTransform)go.transform);
            return go.transform;
        }

        /// <summary>Posiciona no sistema do conteúdo da árvore (centro na origem, y para cima).</summary>
        static void PlaceInContent(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static void ToContent(Component widget, Vector2 position, float scale)
        {
            var rect = (RectTransform)widget.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Ligação de dependência pelo caminho dos túneis (coordenadas do conteúdo, y para cima).</summary>
        static TunnelPathGraphic BuildPathLink(Transform parent, Vector2[] route)
        {
            var go = new GameObject("Ligacao", typeof(RectTransform), typeof(TunnelPathGraphic));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            var path = go.GetComponent<TunnelPathGraphic>();
            path.raycastTarget = false;
            path.SetPoints(route);
            return path;
        }
    }
}
