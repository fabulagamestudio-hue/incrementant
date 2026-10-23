using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.UI;
using Fabula.Incrementant.Upgrades;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Formigueiro da tela Evolução (documento 46): janela arrastável com a Câmara real no topo, a galeria logo abaixo, uma
    /// coluna por domínio com o salão no nível de origem e as câmaras laterais nos níveis dos nós, as faixas dos níveis (terra
    /// escura quando fechadas) e as ligações de dependência pelos túneis. As posições vêm de <see cref="UpgradeLayout"/> e a
    /// ordem dos widgets de <see cref="SkillTreeScreen.NodesOf"/>. Arte provisória: losango do kit para os dois tipos (galeria
    /// menor), ícones gerais onde o kit não tem ícone.
    /// </summary>
    public static partial class AntFarmHudSetup
    {
        // Área da janela da árvore na tela 1920x1080 (abaixo do cabeçalho; o detalhe e a legenda ficam por cima).
        static readonly Vector2 TreeViewTop = new Vector2(0, 210);
        static readonly Vector2 TreeViewSize = new Vector2(1920, 790);
        const float GalleryScale = 0.75f;
        static readonly Color TierLabelColor = new Color(0.72f, 0.66f, 0.55f, 0.9f);
        static readonly Color TierLineColor = new Color(0.30f, 0.22f, 0.14f, 0.85f);
        static readonly Color ClosedTierColor = new Color(0.08f, 0.05f, 0.03f, 0.78f);
        // Abertura: um pouco abaixo da Câmara real, para mostrar a raiz e o começo das colunas.
        const float StartZoom = 0.5f, MinZoom = 0.2f;
        static readonly Vector2 StartOffset = new Vector2(0f, -330f);

        static void BuildAnthillPage(Transform page, SerializedObject screen, string field)
        {
            var layout = new UpgradeLayout();
            Rect bounds = layout.Bounds;

            // --- janela (recebe arrasto e roda) e conteúdo
            var viewportGo = new GameObject("Janela", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(TreeViewport));
            viewportGo.layer = LayerMask.NameToLayer("UI");
            viewportGo.transform.SetParent(page, false);
            var viewportRect = (RectTransform)viewportGo.transform;
            PlaceTop(viewportRect, TreeViewTop, TreeViewSize);
            var catcher = viewportGo.GetComponent<Image>();
            catcher.color = new Color(0, 0, 0, 0); // invisível, só para receber o arrasto
            catcher.raycastTarget = true;

            var contentGo = new GameObject("Conteudo", typeof(RectTransform));
            contentGo.layer = LayerMask.NameToLayer("UI");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var content = (RectTransform)contentGo.transform;
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
            // A origem das coordenadas é o centro do conteúdo; o retângulo cobre o formigueiro inteiro em volta dela, para as
            // malhas esticadas nele (cavernas, ligações) não serem cortadas pelo RectMask2D.
            float halfX = Mathf.Max(Mathf.Abs(bounds.xMin), Mathf.Abs(bounds.xMax));
            float halfY = Mathf.Max(Mathf.Abs(bounds.yMin), Mathf.Abs(bounds.yMax));
            content.sizeDelta = new Vector2(halfX * 2f, halfY * 2f);
            content.anchoredPosition = Vector2.zero;
            content.localScale = new Vector3(StartZoom, StartZoom, 1f);

            var viewport = viewportGo.GetComponent<TreeViewport>();
            var viewportSo = new SerializedObject(viewport);
            SetObject(viewportSo, "_content", content);
            viewportSo.FindProperty("_contentBounds").rectValue = bounds;
            viewportSo.FindProperty("_startPoint").vector2Value = layout.Root + StartOffset;
            viewportSo.FindProperty("_startZoom").floatValue = StartZoom;
            viewportSo.FindProperty("_minZoom").floatValue = MinZoom;
            viewportSo.ApplyModifiedPropertiesWithoutUndo();

            List<UpgradeDef> defs = SkillTreeScreen.NodesOf();
            IReadOnlyDictionary<string, Vector2> positions = layout.Positions;
            var index = new Dictionary<string, int>();
            for (int i = 0; i < defs.Count; i++)
                index[defs[i].Id] = i;

            // --- faixas dos níveis: linha no topo e rótulo à esquerda, embaixo de tudo
            int tiers = UpgradeCatalog.TierCount;
            Transform bandLayer = Layer(content, "Faixas");
            var tierLabels = new Text[tiers];
            for (int t = 0; t < tiers; t++)
            {
                if (t > 0)
                {
                    Image line = BuildPlain(bandLayer, "Linha" + t, TierLineColor);
                    line.raycastTarget = false;
                    PlaceInContent(line.rectTransform, new Vector2(bounds.center.x, layout.TierTop(t)), new Vector2(bounds.width, 4f));
                }
                Text label = BuildText(bandLayer, "Nivel" + t, "", TextLarge, TierLabelColor, TextAnchor.MiddleLeft);
                label.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
                float labelX = bounds.xMin + UpgradeLayout.TierLabelInset + UpgradeLayout.TierLabelWidth * 0.5f;
                PlaceInContent(label.rectTransform, new Vector2(labelX, layout.TierTop(t) - 44f), new Vector2(UpgradeLayout.TierLabelWidth, 44));
                tierLabels[t] = label;
            }

            // --- terreno escavado: galeria, colunas e Câmara real, embaixo dos túneis
            CaveMap caves = BuildCaves(content, defs, layout);

            // --- ligações de dependência, pelos túneis (sobre a caverna)
            var links = new List<(TunnelPathGraphic path, int parent, int child, bool cross)>();
            Transform linkLayer = Layer(content, "Tuneis");
            Transform labelLayer = null;
            for (int i = 0; i < defs.Count; i++)
            {
                UpgradeDef def = defs[i];
                foreach (UpgradeRequirement r in def.Requires)
                {
                    UpgradeDef target = UpgradeCatalog.Get(r.Id);
                    bool fromRoot = target.Domain == UpgradeDomain.Root;
                    bool cross = fromRoot || target.Domain != def.Domain;
                    Vector2[] route = caves.Route(target, positions[target.Id], def, positions[def.Id]);
                    links.Add((BuildPathLink(linkLayer, route), fromRoot ? -1 : index[target.Id], i, cross));
                    // O "Nv N" só nas ligações que ficam sempre à vista (as da mesma coluna).
                    if (r.Level > 0 && !cross)
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

            // --- níveis fechados: terra escura sobre cavernas e túneis, abaixo dos nós, com o aviso de profundidade
            Transform shadeLayer = Layer(content, "Terra");
            var shades = new Image[tiers];
            var warnings = new Text[tiers];
            for (int t = 0; t < tiers; t++)
            {
                Image shade = BuildPlain(shadeLayer, "Terra" + t, ClosedTierColor);
                float top = layout.TierTop(t), bottom = t == tiers - 1 ? bounds.yMin : layout.TierBottom(t);
                PlaceInContent(shade.rectTransform, new Vector2(bounds.center.x, (top + bottom) * 0.5f), new Vector2(bounds.width, top - bottom));
                shade.raycastTarget = false;
                shade.enabled = false;
                shades[t] = shade;
                Text warning = BuildText(shadeLayer, "Aviso" + t, "", TextLarge, TierLabelColor, TextAnchor.MiddleCenter);
                PlaceInContent(warning.rectTransform, new Vector2(layout.Root.x, top - 44f), new Vector2(1400, 44));
                warnings[t] = warning;
            }

            // --- nomes dos salões, acima da terra (um domínio num nível fechado ainda mostra o nome)
            Transform hallLayer = Layer(content, "Saloes");
            var hallLabels = new Text[UpgradeCatalog.Domains.Length];
            for (int c = 0; c < hallLabels.Length; c++)
            {
                UpgradeDomain domain = UpgradeCatalog.Domains[c].Domain;
                Text label = BuildText(hallLayer, "Salao" + domain, "", TextMedium, Cream, TextAnchor.MiddleCenter);
                label.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
                PlaceInContent(label.rectTransform, layout.Hall(domain), new Vector2(UpgradeLayout.ColumnWidth - 40f, 40));
                hallLabels[c] = label;
            }

            // --- Câmara real e nós
            var root = BuildNode(content, "Centro", null, Vector2.zero, "icons/crown", null, 40, true, out _);
            ToContent(root, layout.Root, 1f);
            var widgets = new SkillNodeWidget[defs.Count];
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
            }

            string prefix = field + ".";
            SetObject(screen, prefix + "Viewport", viewport);
            SetObject(screen, prefix + "Root", root);
            SetArray(screen, prefix + "Nodes", widgets);
            WriteLinks(screen, prefix + "Links", links);
            SetArray(screen, prefix + "TierShades", shades);
            SetArray(screen, prefix + "TierLabels", tierLabels);
            SetArray(screen, prefix + "TierWarnings", warnings);
            SetArray(screen, prefix + "HallLabels", hallLabels);
        }

        // Terra escavada: parede clara (cópia engordada por baixo) e vão escuro, com um tom por coluna para demarcar os domínios.
        static readonly Color CaveWallColor = new Color(0.52f, 0.38f, 0.25f, 1f);
        static readonly Color CaveBaseColor = new Color(0.075f, 0.05f, 0.038f, 1f);
        // Câmara lateral: folga em volta dos nós (câmara/galeria), folga até o piso, proporção mínima largura/altura.
        const float CavePadChamber = 72f, CavePadGallery = 56f, CaveFloorGap = 48f, CaveMinAspect = 1.3f;
        const float CaveTunnelWidth = 34f, CaveWall = 13f;
        const float CaveMaxHalfWidth = UpgradeLayout.ColumnWidth * 0.5f - 30f;
        // Câmara real e salão do domínio: meia largura e meia altura.
        static readonly Vector2 CaveHallSize = new Vector2(150f, 112f);
        static readonly Vector2 CaveDomainHallSize = new Vector2(170f, 46f);

        /// <summary>Tom da coluna: matizes espalhados pela roda de cor, na ordem das colunas.</summary>
        static Color CaveTint(int column) => Color.HSVToRGB(column / (float)UpgradeCatalog.Domains.Length, 0.55f, 0.85f);

        /// <summary>Vão da coluna: o escuro da caverna puxado para o tom dela, opaco (a parede clara fica só na borda).</summary>
        static Color CaveFill(int column)
        {
            Color fill = Color.Lerp(CaveBaseColor, CaveTint(column) * 0.5f, 0.6f);
            fill.a = 1f;
            return fill;
        }

        /// <summary>
        /// O formigueiro em corte: a galeria horizontal (um túnel sinuoso entre os pontos de cada coluna e o da Câmara real), a
        /// Câmara real ligada a ela e, em cada coluna, um túnel que desce da galeria ao salão do domínio e dele a cada câmara
        /// lateral (uma por nível com nós, mais larga que alta e de piso plano, em volta da grade que o <see cref="UpgradeLayout"/>
        /// forma). Cada peça desenha a parede e depois o vão (<see cref="CaveGraphic"/>); a Câmara real fica por último.
        /// </summary>
        static CaveMap BuildCaves(Transform content, List<UpgradeDef> defs, UpgradeLayout layout)
        {
            var map = new CaveMap(layout);
            int columns = UpgradeCatalog.Domains.Length;
            // Caixa dos nós de cada câmara lateral (coluna, nível): mínimo, máximo e a maior folga dos nós dela.
            var boxes = new SortedDictionary<int, (Vector2 min, Vector2 max, float pad)>();
            foreach (UpgradeDef def in defs)
            {
                Vector2 p = layout.Positions[def.Id];
                float pad = def.Kind == UpgradeKind.Gallery ? CavePadGallery : CavePadChamber;
                int key = layout.Column(def.Domain) * 10 + def.Tier;
                boxes[key] = boxes.TryGetValue(key, out var box)
                    ? (Vector2.Min(box.min, p), Vector2.Max(box.max, p), Mathf.Max(box.pad, pad))
                    : (p, p, pad);
            }

            var chambers = new List<Vector4>[columns];
            var tunnels = new List<Vector4>[columns];
            var previous = new Vector2[columns];
            for (int c = 0; c < columns; c++)
            {
                Vector2 hall = layout.Hall(UpgradeCatalog.Domains[c].Domain);
                chambers[c] = new List<Vector4> { new Vector4(hall.x, hall.y, CaveDomainHallSize.x, CaveDomainHallSize.y) };
                tunnels[c] = new List<Vector4> { new Vector4(hall.x, UpgradeLayout.GalleryY, hall.x, hall.y) };
                previous[c] = hall;
            }
            foreach (KeyValuePair<int, (Vector2 min, Vector2 max, float pad)> pair in boxes) // níveis em ordem crescente
            {
                int c = pair.Key / 10;
                (Vector2 min, Vector2 max, float pad) = pair.Value;
                // O piso fica uma folga abaixo do nó mais baixo e o teto uma folga acima do mais alto; o centro do salão sobe para
                // o piso cortado (CaveGraphic.FloorFraction da meia altura) cair nessa linha.
                float height = (max.y + pad) - (min.y - CaveFloorGap);
                float halfHeight = height / 1.82f;
                // Mais larga que alta quando cabe; a coluna limita a largura, para a câmara não invadir a vizinha.
                float halfWidth = Mathf.Min(Mathf.Max((max.x - min.x) * 0.5f + pad, halfHeight * CaveMinAspect), CaveMaxHalfWidth);
                var center = new Vector2((min.x + max.x) * 0.5f, max.y + pad - halfHeight);
                map.Chamber[pair.Key] = chambers[c].Count;
                chambers[c].Add(new Vector4(center.x, center.y, halfWidth, halfHeight));
                tunnels[c].Add(new Vector4(previous[c].x, previous[c].y, center.x, center.y));
                previous[c] = center;
            }

            // Galeria: paradas nas colunas e na Câmara real, da esquerda para a direita.
            var stops = new List<float> { layout.Root.x };
            for (int c = 0; c < columns; c++)
                stops.Add(UpgradeLayout.ColumnX(c));
            stops.Sort();
            var gallery = new List<Vector4>();
            for (int k = 0; k + 1 < stops.Count; k++)
                gallery.Add(new Vector4(stops[k], UpgradeLayout.GalleryY, stops[k + 1], UpgradeLayout.GalleryY));
            var rootTunnel = new Vector4(layout.Root.x, layout.Root.y, layout.Root.x, UpgradeLayout.GalleryY);

            // Todas as paredes antes de todos os vãos: assim a parede de uma peça nunca fecha a boca de outra, e os encontros da
            // galeria com as colunas e com a Câmara real ficam abertos (antes, a parede de cada coluna cortava a galeria).
            Transform layer = Layer(content, "Cavernas");
            var none = new List<Vector4>();
            var galleryAll = new List<Vector4>(gallery) { rootTunnel };
            var hallChamber = new List<Vector4> { new Vector4(layout.Root.x, layout.Root.y, CaveHallSize.x, CaveHallSize.y) };
            BuildCave(layer, "ParedeGaleria", none, galleryAll, CaveWallColor, CaveWall);
            for (int c = 0; c < columns; c++)
                BuildCave(layer, "Parede" + c, chambers[c], tunnels[c], CaveWallColor, CaveWall);
            BuildCave(layer, "ParedeCentro", hallChamber, none, CaveWallColor, CaveWall);
            // O vão da galeria vem depois do das colunas: o trilho horizontal passa contínuo pelos encontros.
            for (int c = 0; c < columns; c++)
                BuildCave(layer, "Coluna" + c, chambers[c], tunnels[c], CaveFill(c), 0f);
            BuildCave(layer, "Galeria", none, galleryAll, CaveBaseColor, 0f);
            BuildCave(layer, "Centro", hallChamber, none, CaveBaseColor, 0f);
            map.Tunnels = tunnels;
            map.Gallery = gallery;
            map.GalleryStops = stops;
            map.RootTunnel = rootTunnel;
            return map;
        }

        /// <summary>
        /// Salões e túneis montados por <see cref="BuildCaves"/>, para traçar as ligações por dentro deles. O túnel k de uma
        /// coluna liga a câmara k − 1 (ou a parada da coluna na galeria, para k = 0) à câmara k; a câmara 0 é o salão do
        /// domínio. O trecho k da galeria liga a parada k à k + 1.
        /// </summary>
        sealed class CaveMap
        {
            readonly UpgradeLayout _layout;
            public readonly Dictionary<int, int> Chamber = new Dictionary<int, int>(); // coluna × 10 + nível → câmara na coluna
            public List<Vector4>[] Tunnels;
            public List<Vector4> Gallery;
            public List<float> GalleryStops;
            public Vector4 RootTunnel;

            public CaveMap(UpgradeLayout layout) => _layout = layout;

            /// <summary>
            /// Caminho de <paramref name="from"/> até <paramref name="to"/>: do nó ao centro da câmara dele, pelo túnel da coluna
            /// (subindo até a galeria e andando nela quando as colunas são outras, ou descendo da Câmara real) até a câmara do
            /// destino e então ao nó.
            /// </summary>
            public Vector2[] Route(UpgradeDef from, Vector2 fromPos, UpgradeDef to, Vector2 toPos)
            {
                var points = new List<Vector2> { fromPos };
                int toColumn = _layout.Column(to.Domain);
                int toChamber = Chamber[toColumn * 10 + to.Tier];
                if (from.Domain == UpgradeDomain.Root)
                {
                    AddTunnel(points, RootTunnel, false);
                    WalkGallery(points, _layout.Root.x, UpgradeLayout.ColumnX(toColumn));
                    Walk(points, toColumn, -1, toChamber);
                }
                else
                {
                    int fromColumn = _layout.Column(from.Domain);
                    int fromChamber = Chamber[fromColumn * 10 + from.Tier];
                    if (fromColumn == toColumn)
                        Walk(points, toColumn, fromChamber, toChamber);
                    else
                    {
                        Walk(points, fromColumn, fromChamber, -1);
                        WalkGallery(points, UpgradeLayout.ColumnX(fromColumn), UpgradeLayout.ColumnX(toColumn));
                        Walk(points, toColumn, -1, toChamber);
                    }
                }
                Add(points, toPos);
                return points.ToArray();
            }

            // Da câmara "from" à câmara "to" da mesma coluna (-1 = parada da coluna na galeria), pelo eixo dos túneis do meio.
            void Walk(List<Vector2> points, int column, int from, int to)
            {
                List<Vector4> tunnels = Tunnels[column];
                for (int k = from + 1; k <= to; k++)
                    AddTunnel(points, tunnels[k], false);
                for (int k = from; k > to; k--)
                    AddTunnel(points, tunnels[k], true);
            }

            // Da parada em fromX à parada em toX, pelos trechos da galeria.
            void WalkGallery(List<Vector2> points, float fromX, float toX)
            {
                int from = GalleryStops.IndexOf(fromX), to = GalleryStops.IndexOf(toX);
                for (int k = from; k < to; k++)
                    AddTunnel(points, Gallery[k], false);
                for (int k = from - 1; k >= to; k--)
                    AddTunnel(points, Gallery[k], true);
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

        /// <summary>Ícone do nó: os do kit quando existem (C, B, D, R antigos); senão, o ícone geral do ramo de origem (documento 39).</summary>
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

        static Transform Layer(Transform content, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(content, false);
            Stretch((RectTransform)go.transform);
            return go.transform;
        }

        /// <summary>Posiciona no sistema do conteúdo da árvore (origem no centro do conteúdo, y para cima).</summary>
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
