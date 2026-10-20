using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.EditorTools.Localization;
using Fabula.Incrementant.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>Painel da colônia (botão à esquerda do sino): abas Estatísticas e Configurações. Ver <see cref="ColonyPanel"/>.</summary>
    public static partial class AntFarmHudSetup
    {
        static readonly Vector2 ColonyPanelSize = new Vector2(1440, 760);
        // Área das páginas dentro do painel: abaixo das abas, com margem.
        const float ColonyPageTop = 124f, ColonyPageMargin = 40f;
        const float ColonyRowTop = 36f, ColonyRowStep = 56f;
        // Controles do limite na linha: "-", slider, "+" e o texto do limite (página com 1360 px de largura).
        const float ColonyMinusX = 680f, ColonySliderX = 748f, ColonySliderWidth = 304f, ColonyPlusX = 1064f;
        const float ColonyLimitX = 1132f, ColonyLimitWidth = 228f;

        /// <summary>Botão do painel no canto inferior esquerdo, onde ficava o sino (o sino passa para a direita dele).</summary>
        static void BuildColonyButton(Transform root, SerializedObject refs)
        {
            Image panel = BuildSprite(root, "PainelColoniaBotao", "panels/hud_card", new Vector2(0, 0), new Vector2(0, 0),
                                      new Vector2(108, 16), new Vector2(84, 76));
            Button button = BuildIconButton(panel.transform, "Botao", 0, "icons/ant", null, out Image image);
            SetObject(refs, "_colonyButton", button);
            SetObject(refs, "_colonyImage", image);
        }

        static ColonyPanel BuildColonyPanel(Transform root, AntFarmGame game)
        {
            Image panel = BuildPanelWithHeader(root, "PainelColonia", new Vector2(0.5f, 0.5f), Vector2.zero, ColonyPanelSize,
                                               LocalizationCatalog.Pt("colony.title"));
            var screen = panel.gameObject.AddComponent<ColonyPanel>();
            var so = new SerializedObject(screen);
            SetObject(so, "_game", game);

            // --- abas e fechar
            Button statsTab = BuildTab(panel.transform, "AbaEstatisticas", 40, LocalizationCatalog.Pt("colony.tab.stats"), true, out Image statsImage);
            Button settingsTab = BuildTab(panel.transform, "AbaConfiguracoes", 360, LocalizationCatalog.Pt("menu.settings"), false, out Image settingsImage);
            SetObject(so, "_statsTab", statsTab);
            SetObject(so, "_statsTabImage", statsImage);
            SetObject(so, "_settingsTab", settingsTab);
            SetObject(so, "_settingsTabImage", settingsImage);
            SetObject(so, "_tabActive", Load("tabs/tab_active"));
            SetObject(so, "_tabInactive", Load("tabs/tab_inactive"));
            SetObject(so, "_close", BuildSmallIconButton(panel.transform, "Fechar", "icons/close",
                                                         new Vector2(ColonyPanelSize.x - 40 - 56, 60)));

            // --- página Estatísticas: três colunas de texto (27 letras de 16 px cada)
            RectTransform stats = BuildColonyPage(panel.transform, "PaginaEstatisticas");
            string[] columns = { "_antsColumn", "_workColumn", "_placesColumn" };
            for (int i = 0; i < columns.Length; i++)
            {
                Text column = BuildText(stats, "Coluna" + i, "", TextSmall, Ink, TextAnchor.UpperLeft);
                column.lineSpacing = 1.5f;
                Place(column.rectTransform, new Vector2(i * 456, 0), new Vector2(432, 600));
                SetText(so, columns[i], column);
            }
            SetObject(so, "_statsPage", stats.gameObject);

            // --- página Configurações: uma linha por seção, depois o trabalho da rainha
            RectTransform settings = BuildColonyPage(panel.transform, "PaginaConfiguracoes");
            ColonyHeader(settings, "colony.col.section", 120, 380, TextAnchor.MiddleLeft);
            ColonyHeader(settings, "colony.col.now", 520, 140, TextAnchor.MiddleLeft);
            ColonyHeader(settings, "colony.col.limit", ColonyMinusX, ColonyLimitX + ColonyLimitWidth - ColonyMinusX, TextAnchor.MiddleCenter);
            int n = WorkPolicy.Count;
            var toggles = new Button[n];
            var toggleImages = new Image[n];
            var labels = new Text[n];
            var now = new Text[n];
            var minus = new Button[n];
            var plus = new Button[n];
            var sliders = new Slider[n];
            var limits = new Text[n];
            for (int i = 0; i < n; i++)
            {
                float top = ColonyRowTop + i * ColonyRowStep;
                Image toggle = BuildScaledSprite(settings, "Interruptor" + i, "controls/toggle_on", 2f);
                toggle.type = Image.Type.Simple;
                toggle.raycastTarget = true;
                Place(toggle.rectTransform, new Vector2(0, -top), new Vector2(100, 52));
                toggles[i] = toggle.gameObject.AddComponent<Button>();
                toggles[i].targetGraphic = toggle;
                toggles[i].transition = Selectable.Transition.None;
                toggleImages[i] = toggle;

                labels[i] = ColonyRowText(settings, "Secao" + i, 120, top, 380, TextAnchor.MiddleLeft);
                now[i] = ColonyRowText(settings, "Agora" + i, 520, top, 140, TextAnchor.MiddleLeft);
                minus[i] = BuildSmallIconButton(settings, "Menos" + i, "icons/minus", new Vector2(ColonyMinusX, top));
                // Faixa e valor são do ColonyPanel (dependem do total de formigas); aqui fica só a forma.
                sliders[i] = BuildSlider(settings, "Slider" + i, new Vector2(ColonySliderX, top + 26), ColonySliderWidth);
                sliders[i].wholeNumbers = true;
                plus[i] = BuildSmallIconButton(settings, "Mais" + i, "icons/plus", new Vector2(ColonyPlusX, top));
                limits[i] = ColonyRowText(settings, "Limite" + i, ColonyLimitX, top, ColonyLimitWidth, TextAnchor.MiddleCenter);
            }
            SetArray(so, "_toggles", toggles);
            SetArray(so, "_toggleImages", toggleImages);
            SetArray(so, "_sectionLabels", labels);
            SetArray(so, "_sectionNow", now);
            SetArray(so, "_minus", minus);
            SetArray(so, "_plus", plus);
            SetArray(so, "_limitSliders", sliders);
            SetArray(so, "_limits", limits);
            SetObject(so, "_toggleOn", Load("controls/toggle_on"));
            SetObject(so, "_toggleOff", Load("controls/toggle_off"));

            float queenTop = ColonyRowTop + n * ColonyRowStep + 16;
            ColonyHeader(settings, "colony.queen.title", 0, 600, TextAnchor.MiddleLeft, queenTop);
            // Um botão por QueenJob, na ordem do enum (336 px cabem 17 letras depois do ícone).
            string[] jobKeys = { "colony.queen.nest", "colony.queen.forage", "colony.queen.nutrients" };
            string[] jobIcons = { "icons/egg", "icons/leaf", "icons/mushroom" };
            var jobs = new Button[jobKeys.Length];
            var jobImages = new Image[jobKeys.Length];
            for (int i = 0; i < jobKeys.Length; i++)
                jobs[i] = BuildButton(settings, "TrabalhoRainha" + i, new Vector2(0, 1), new Vector2(168 + i * 344, -(queenTop + 32)),
                                      new Vector2(336, 42), jobIcons[i], LocalizationCatalog.Pt(jobKeys[i]), true, out jobImages[i]);
            SetArray(so, "_queenJobs", jobs);
            SetArray(so, "_queenJobImages", jobImages);
            SetObject(so, "_reset", BuildButton(settings, "TudoAtivo", new Vector2(0, 1), new Vector2(1210, -(queenTop + 32)),
                                                new Vector2(300, 42), "icons/play", LocalizationCatalog.Pt("colony.reset"), true, out _));
            Text jobState = BuildText(settings, "EstadoRainha", "", TextSmall, Ink, TextAnchor.MiddleLeft);
            Place(jobState.rectTransform, new Vector2(0, -(queenTop + 84)), new Vector2(ColonyPanelSize.x - 2 * ColonyPageMargin, 28));
            SetText(so, "_queenJobState", jobState);
            SetObject(so, "_buttonIdle", Load("buttons/button_normal"));
            SetObject(so, "_buttonActive", Load("buttons/button_focus"));
            SetObject(so, "_settingsPage", settings.gameObject);
            settings.gameObject.SetActive(false);

            so.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
            return screen;
        }

        static RectTransform BuildColonyPage(Transform panel, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(panel, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0, 1);
            rect.offsetMin = new Vector2(ColonyPageMargin, 24);
            rect.offsetMax = new Vector2(-ColonyPageMargin, -ColonyPageTop);
            return rect;
        }

        /// <summary>Aba (sprite <c>tabs/tab_*</c>) com o rótulo, na linha logo abaixo do título do painel.</summary>
        static Button BuildTab(Transform panel, string name, float x, string label, bool active, out Image image)
        {
            image = BuildSprite(panel, name, active ? "tabs/tab_active" : "tabs/tab_inactive", new Vector2(0, 1), new Vector2(0, 1),
                                new Vector2(x, -56), new Vector2(300, 52));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            Text text = BuildText(image.transform, "Texto", label, TextSmall, Ink, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        /// <summary>Botão quadrado com ícone (56x52), posicionado pelo canto superior esquerdo do pai (y para baixo).</summary>
        static Button BuildSmallIconButton(Transform parent, string name, string icon, Vector2 position)
        {
            Image image = BuildSprite(parent, name, "buttons/icon_button", new Vector2(0, 1), new Vector2(0, 1),
                                      new Vector2(position.x, -position.y), new Vector2(56, 52));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Load("buttons/icon_button_focus"), disabledSprite = Load("buttons/icon_button_disabled") };
            Image iconImage = BuildSprite(image.transform, "Icone", icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                          Vector2.zero, new Vector2(24, 28));
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            return button;
        }

        static void ColonyHeader(Transform page, string key, float x, float width, TextAnchor alignment, float top = 0f)
        {
            Text text = BuildText(page, "Cabecalho", LocalizationCatalog.Pt(key), TextSmall, Ink, alignment);
            Place(text.rectTransform, new Vector2(x, -top), new Vector2(width, 28));
        }

        static Text ColonyRowText(Transform page, string name, float x, float top, float width, TextAnchor alignment)
        {
            Text text = BuildText(page, name, "", TextSmall, Ink, alignment);
            Place(text.rectTransform, new Vector2(x, -top), new Vector2(width, 52));
            return text;
        }
    }
}
