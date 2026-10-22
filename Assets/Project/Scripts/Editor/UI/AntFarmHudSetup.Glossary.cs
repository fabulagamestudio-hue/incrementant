using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.EditorTools.Localization;
using Fabula.Incrementant.Glossary;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Parcial do montador do HUD: a janela do glossário (<see cref="GlossaryScreen"/>), no mesmo molde do histórico de
    /// versões: fundo escurecido, lista à esquerda (categorias e entradas; o runtime clona os modelos) e texto à direita.
    /// </summary>
    public static partial class AntFarmHudSetup
    {
        const float GlossaryWidth = 1360f, GlossaryHeight = 840f, GlossaryTop = 64f, GlossaryFooter = 104f;
        const float GlossaryListWidth = 480f, GlossaryGap = 24f, GlossaryItemHeight = 48f;
        static readonly Color GlossaryHeading = new Color(0.48f, 0.29f, 0.12f);

        static GlossaryScreen BuildGlossary(Transform root, AntFarmGame game)
        {
            var screenObject = new GameObject("Glossario", typeof(RectTransform));
            screenObject.layer = LayerMask.NameToLayer("UI");
            screenObject.transform.SetParent(root, false);
            Stretch((RectTransform)screenObject.transform);
            Image dim = BuildPlain(screenObject.transform, "Escurecer", new Color(0.05f, 0.03f, 0.02f, 0.7f));
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);

            Image panel = BuildPanelWithHeader(screenObject.transform, "Painel", new Vector2(0.5f, 0.5f), Vector2.zero,
                                               new Vector2(GlossaryWidth, GlossaryHeight), LocalizationCatalog.Pt("glossary.title"));
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Transform t = panel.transform;
            float bodyHeight = GlossaryHeight - GlossaryTop - GlossaryFooter;

            ScrollRect listScroll = BuildVersionScroll(t, "Lista", new Vector2(32, -GlossaryTop), new Vector2(GlossaryListWidth, bodyHeight),
                                                       8, out RectTransform listContent);
            listContent.GetComponent<VerticalLayoutGroup>().spacing = 6;
            Text heading = BuildText(listContent, "ModeloCategoria", "", TextSmall, GlossaryHeading, TextAnchor.LowerLeft);
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
            Button item = BuildGlossaryItem(listContent);

            float detailX = 32 + GlossaryListWidth + GlossaryGap;
            ScrollRect detailScroll = BuildVersionScroll(t, "Detalhe", new Vector2(detailX, -GlossaryTop),
                                                         new Vector2(GlossaryWidth - detailX - 32, bodyHeight), 24, out RectTransform detail);
            Text title = BuildText(detail, "Titulo", "", TextLarge, Ink, TextAnchor.UpperLeft);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.lineSpacing = 1.3f;
            Text category = BuildText(detail, "Categoria", "", TextSmall, InkDisabled, TextAnchor.UpperLeft);
            category.gameObject.AddComponent<LayoutElement>().minHeight = 40;
            Text body = BuildText(detail, "Texto", "", TextSmall, Ink, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.lineSpacing = 1.5f;

            Button close = BuildButton(t, "Voltar", new Vector2(0.5f, 1), new Vector2(0, -(GlossaryHeight - 84)), new Vector2(300, 48),
                                       "icons/arrow_left", LocalizationCatalog.Pt("ui.back"), true, out Image closeImage);
            closeImage.sprite = Load("buttons/button_action");

            var screen = screenObject.AddComponent<GlossaryScreen>();
            var so = new SerializedObject(screen);
            SetObject(so, "_game", game);
            SetObject(so, "_listContent", listContent);
            SetObject(so, "_listScroll", listScroll);
            SetObject(so, "_headingTemplate", heading);
            SetObject(so, "_itemTemplate", item);
            SetObject(so, "_itemNormal", Load("buttons/button_normal"));
            SetObject(so, "_itemSelected", Load("buttons/button_focus"));
            SetObject(so, "_detailScroll", detailScroll);
            SetObject(so, "_title", title);
            SetObject(so, "_category", category);
            SetObject(so, "_body", body);
            SetObject(so, "_close", close);
            so.ApplyModifiedPropertiesWithoutUndo();
            screenObject.SetActive(false);
            return screen;
        }

        /// <summary>Botão-modelo de uma entrada (o runtime clona um por entrada): só o título.</summary>
        static Button BuildGlossaryItem(Transform content)
        {
            Image image = BuildSprite(content, "ModeloEntrada", "buttons/button_normal", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero,
                                      new Vector2(0, GlossaryItemHeight));
            image.gameObject.AddComponent<LayoutElement>().preferredHeight = GlossaryItemHeight;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Load("buttons/button_pressed") };
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            Text label = BuildText(image.transform, "Texto", "", TextSmall, Ink, TextAnchor.MiddleLeft);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(16, 0);
            label.rectTransform.offsetMax = new Vector2(-8, 0);
            return button;
        }
    }
}
