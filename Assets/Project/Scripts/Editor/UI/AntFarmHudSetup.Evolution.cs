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

            // --- túneis (embaixo de tudo)
            var links = new List<(Image image, int parent, int child)>();
            Sprite linkLocked = LoadKit("Connections/link_locked");
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
                    Vector2 from = positions[target.Id];
                    links.Add((BuildContentLink(linkLayer, from, positions[def.Id], linkLocked), parent, i));
                    if (r.Level > 0)
                    {
                        labelLayer ??= Layer(content, "NiveisDosTuneis");
                        Text level = BuildText(labelLayer, "Nv" + r, "", TextSmall, Cream, TextAnchor.MiddleCenter);
                        level.text = "Nv " + r.Level;
                        level.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
                        PlaceInContent(level.rectTransform, (from + positions[def.Id]) * 0.5f, new Vector2(80, 26));
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

        /// <summary>Túnel entre dois pontos do conteúdo (y para cima): o sprite de 20x6 repetido, em escala 2.</summary>
        static Image BuildContentLink(Transform parent, Vector2 from, Vector2 to, Sprite sprite)
        {
            var go = new GameObject("Tunel", typeof(RectTransform), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            Vector2 delta = to - from;
            PlaceInContent(rect, (from + to) * 0.5f, new Vector2(delta.magnitude, 6 * PixelScale));
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Tiled;
            image.pixelsPerUnitMultiplier = 1f / PixelScale;
            image.raycastTarget = false;
            return image;
        }
    }
}
