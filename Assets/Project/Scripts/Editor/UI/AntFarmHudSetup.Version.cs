using Fabula.Incrementant.EditorTools.Localization;
using Fabula.Incrementant.UI;
using Iung.BuildAssistant.Config;
using Iung.BuildAssistant.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Parcial do montador do HUD: o prefab <c>Resources/BuildVersionOverlay</c>, com canvas próprio por cima de todas as
    /// telas, o texto da versão no canto inferior direito (um botão: barra o mundo por baixo e abre o histórico) e a janela
    /// do histórico de versões. Idempotente: regrava o prefab inteiro.
    /// </summary>
    public static partial class AntFarmHudSetup
    {
        const string VersionPrefabPath = "Assets/Project/Resources/" + Fabula.Incrementant.UI.BuildVersionOverlay.ResourcePath + ".prefab";
        // Acima do HUD, dos menus e da tela de carregamento.
        const int VersionSortingOrder = 32000;
        // A Press Start 2P é desenhada numa grade de 8 px: 8 é o menor tamanho com pixels inteiros.
        const int VersionFontSize = 8;
        const float HistoryWidth = 1360f, HistoryHeight = 840f, HistoryTop = 64f, HistoryFooter = 104f;
        const float HistoryListWidth = 440f, HistoryGap = 24f, HistoryItemHeight = 64f;

        [MenuItem("Incrementant/Setup/Criar overlay de versão")]
        public static void BuildVersionOverlay()
        {
            BuildAssistantSettingsService.EnsureSettingsAssetExists();
            var settings = AssetDatabase.LoadAssetAtPath<BuildAssistantSettings>(BuildAssistantPaths.SettingsAssetPath);
            if (settings == null)
            {
                Debug.LogError($"[AntFarmHudSetup] Settings do Build Assistant ausente em {BuildAssistantPaths.SettingsAssetPath}.");
                return;
            }
            PrepareAssets();

            var root = new GameObject(Fabula.Incrementant.UI.BuildVersionOverlay.ResourcePath, typeof(RectTransform), typeof(Canvas),
                                      typeof(CanvasScaler), typeof(GraphicRaycaster));
            try
            {
                root.layer = LayerMask.NameToLayer("UI");
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = VersionSortingOrder;
                canvas.pixelPerfect = true;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = ReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                Button open = BuildVersionButton(root.transform, settings, out Text label);
                VersionHistoryScreen history = BuildVersionHistory(root.transform);

                var overlay = root.AddComponent<Fabula.Incrementant.UI.BuildVersionOverlay>();
                var so = new SerializedObject(overlay);
                SetObject(so, "_settings", settings);
                SetObject(so, "_label", label);
                SetObject(so, "_open", open);
                SetObject(so, "_history", history);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, VersionPrefabPath, out bool saved);
                if (saved)
                    Debug.Log($"[AntFarmHudSetup] Overlay de versão gravado em {VersionPrefabPath}.");
                else
                    Debug.LogError($"[AntFarmHudSetup] Falha ao gravar {VersionPrefabPath}.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Área clicável do texto da versão: imagem transparente que acompanha o texto com folga (alvo maior que as letras e
        /// só do tamanho delas, para barrar o mundo apenas ali); o texto clareia ao passar o mouse.
        /// </summary>
        static Button BuildVersionButton(Transform root, BuildAssistantSettings settings, out Text label)
        {
            var area = new GameObject("Versao", typeof(RectTransform), typeof(Image), typeof(Button), typeof(HorizontalLayoutGroup),
                                      typeof(ContentSizeFitter));
            area.layer = LayerMask.NameToLayer("UI");
            area.transform.SetParent(root, false);
            var rect = (RectTransform)area.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(-2, 2);
            area.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var layout = area.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = area.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Sem texto-fonte: o rótulo é preenchido por código e não passa pela tabela de idiomas.
            label = BuildText(area.transform, "Texto", "", TextSmall, Cream, TextAnchor.LowerRight);
            label.fontSize = VersionFontSize;
            label.text = Fabula.Incrementant.UI.BuildVersionOverlay.ResolveVersion(settings);
            label.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1, -1);

            var button = area.GetComponent<Button>();
            button.targetGraphic = label;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = colors.selectedColor = new Color(1, 1, 1, 0.6f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        /// <summary>Janela do histórico: fundo escurecido em tela cheia, painel com a lista de versões à esquerda e as notas à direita.</summary>
        static VersionHistoryScreen BuildVersionHistory(Transform root)
        {
            var screenObject = new GameObject("HistoricoDeVersoes", typeof(RectTransform));
            screenObject.layer = LayerMask.NameToLayer("UI");
            screenObject.transform.SetParent(root, false);
            Stretch((RectTransform)screenObject.transform);
            Image dim = BuildPlain(screenObject.transform, "Escurecer", new Color(0.05f, 0.03f, 0.02f, 0.7f));
            dim.raycastTarget = true;
            Stretch(dim.rectTransform);

            Image panel = BuildPanelWithHeader(screenObject.transform, "Painel", new Vector2(0.5f, 0.5f), Vector2.zero,
                                               new Vector2(HistoryWidth, HistoryHeight), LocalizationCatalog.Pt("version.title"));
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Transform t = panel.transform;
            float bodyHeight = HistoryHeight - HistoryTop - HistoryFooter;

            ScrollRect listScroll = BuildVersionScroll(t, "Lista", new Vector2(32, -HistoryTop), new Vector2(HistoryListWidth, bodyHeight),
                                                       8, out RectTransform listContent);
            Button item = BuildVersionItem(listContent);

            float detailX = 32 + HistoryListWidth + HistoryGap;
            ScrollRect detailScroll = BuildVersionScroll(t, "Detalhe", new Vector2(detailX, -HistoryTop),
                                                         new Vector2(HistoryWidth - detailX - 32, bodyHeight), 24, out RectTransform detail);
            Text title = BuildText(detail, "Versao", "", TextLarge, Ink, TextAnchor.UpperLeft);
            Text date = BuildText(detail, "Data", "", TextSmall, InkDisabled, TextAnchor.UpperLeft);
            Text body = BuildText(detail, "Notas", "", TextSmall, Ink, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.supportRichText = true;
            body.lineSpacing = 1.5f;
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            date.gameObject.AddComponent<LayoutElement>().minHeight = 40;

            Button close = BuildButton(t, "Voltar", new Vector2(0.5f, 1), new Vector2(0, -(HistoryHeight - 84)), new Vector2(300, 48),
                                       "icons/arrow_left", LocalizationCatalog.Pt("ui.back"), true, out Image closeImage);
            closeImage.sprite = Load("buttons/button_action");

            var screen = screenObject.AddComponent<VersionHistoryScreen>();
            var so = new SerializedObject(screen);
            SetObject(so, "_listContent", listContent);
            SetObject(so, "_listScroll", listScroll);
            SetObject(so, "_itemTemplate", item);
            SetObject(so, "_itemNormal", Load("buttons/button_normal"));
            SetObject(so, "_itemSelected", Load("buttons/button_focus"));
            SetObject(so, "_detailScroll", detailScroll);
            SetObject(so, "_title", title);
            SetObject(so, "_date", date);
            SetObject(so, "_body", body);
            SetObject(so, "_close", close);
            so.ApplyModifiedPropertiesWithoutUndo();
            screenObject.SetActive(false);
            return screen;
        }

        /// <summary>Botão-modelo de uma versão (o runtime clona um por versão): rótulo em cima e, embaixo, "ATUAL" e a data.</summary>
        static Button BuildVersionItem(Transform content)
        {
            Image image = BuildSprite(content, "ModeloVersao", "buttons/button_normal", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero,
                                      new Vector2(0, HistoryItemHeight));
            image.gameObject.AddComponent<LayoutElement>().preferredHeight = HistoryItemHeight;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Load("buttons/button_pressed") };
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            Image icon = BuildSprite(image.transform, "Icone", "icons/leaf", new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                                     new Vector2(14, 0), new Vector2(28, 28));
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.rectTransform.pivot = new Vector2(0, 0.5f);
            icon.raycastTarget = false;

            Text label = BuildText(image.transform, "Texto", "", TextSmall, Ink, TextAnchor.MiddleLeft);
            label.rectTransform.anchorMin = new Vector2(0, 1);
            label.rectTransform.anchorMax = new Vector2(1, 1);
            label.rectTransform.offsetMin = new Vector2(54, -36);
            label.rectTransform.offsetMax = new Vector2(-12, -12);
            Text date = BuildText(image.transform, "Data", "", TextSmall, InkDisabled, TextAnchor.MiddleLeft);
            date.fontSize = VersionFontSize;
            date.rectTransform.anchorMin = new Vector2(0, 0);
            date.rectTransform.anchorMax = new Vector2(1, 0);
            date.rectTransform.offsetMin = new Vector2(54, 12);
            date.rectTransform.offsetMax = new Vector2(-12, 24);
            return button;
        }

        /// <summary>Rolagem vertical com fundo leve, conteúdo em coluna que cresce com os filhos e a barra do kit à direita.</summary>
        static ScrollRect BuildVersionScroll(Transform parent, string name, Vector2 position, Vector2 size, int padding, out RectTransform content)
        {
            Image background = BuildPlain(parent, name, new Color(Ink.r, Ink.g, Ink.b, 0.08f));
            background.raycastTarget = true;
            Place(background.rectTransform, position, size);
            var scroll = background.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.layer = LayerMask.NameToLayer("UI");
            viewport.transform.SetParent(background.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            Stretch(viewportRect);
            viewportRect.pivot = new Vector2(0, 1);

            var contentObject = new GameObject("Conteudo", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.layer = LayerMask.NameToLayer("UI");
            contentObject.transform.SetParent(viewport.transform, false);
            content = (RectTransform)contentObject.transform;
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = 8;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image track = BuildSprite(background.transform, "Barra", "scrollbar/scrollbar_track", new Vector2(1, 0), new Vector2(1, 1),
                                      new Vector2(-4, 0), new Vector2(12, -16));
            track.rectTransform.pivot = new Vector2(1, 0.5f);
            track.rectTransform.anchoredPosition = new Vector2(-4, 0);
            var slidingArea = new GameObject("Area", typeof(RectTransform));
            slidingArea.layer = LayerMask.NameToLayer("UI");
            slidingArea.transform.SetParent(track.transform, false);
            Stretch((RectTransform)slidingArea.transform);
            Image thumb = BuildSprite(slidingArea.transform, "Alca", "scrollbar/scrollbar_thumb", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Stretch(thumb.rectTransform);
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.handleRect = thumb.rectTransform;
            bar.targetGraphic = thumb;
            bar.navigation = new Navigation { mode = Navigation.Mode.None };

            scroll.viewport = viewportRect;
            scroll.content = content;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 4;
            return scroll;
        }
    }
}
