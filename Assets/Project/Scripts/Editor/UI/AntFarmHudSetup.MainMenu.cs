using System.Linq;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.EditorTools.Setup;
using Fabula.Incrementant.Menu;
using Fabula.Incrementant.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fabula.Incrementant.EditorTools.UI
{
    // Menu principal (referência: o print do menu no pedido do REQ do menu). Parcial do montador do HUD para reaproveitar os
    // primitivos de uGUI (painéis, botões do kit, tela de configurações); a cena e o que há nela é montado aqui.
    public static partial class AntFarmHudSetup
    {
        public const string MenuScenePath = "Assets/Project/Scenes/MainMenu.unity";
        const string MenuName = "MenuPrincipal";
        const string LogoPath = "Assets/Project/Art/UI/Logo_Fabula_branco.png";
        const string GameLogoPath = "Assets/Project/Art/UI/logoIncrementant.png";

        /// <summary>Semente do mapa de fundo do menu: fixa, para o fundo ser sempre o mesmo.</summary>
        const int MenuSeed = 1234;

        static readonly Color MenuGold = new Color(0.91f, 0.64f, 0.23f);
        static readonly Color MenuTitle = new Color(1f, 0.69f, 0.12f);
        static readonly Color MenuTitleOutline = new Color(0.42f, 0.2f, 0.03f);
        static readonly Color MenuFooter = new Color(0.78f, 0.66f, 0.47f);
        static readonly Color MenuLocked = new Color(0.45f, 0.45f, 0.47f);

        [MenuItem("Incrementant/Setup/Criar cena MainMenu")]
        public static void CreateMainMenuScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) != null
                ? EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            AntFarmGame game = AntFarmSceneSetup.BuildWorld(scene);
            ConfigureShowcase(game);
            BuildMainMenu(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MenuScenePath);
            EnsureMenuInBuildSettings();
            Debug.Log($"[AntFarmHudSetup] Cena do menu principal pronta em {MenuScenePath}.");
        }

        /// <summary>Liga a vitrine: mapa fixo, sem ataques de cupins e sem a morte das formigas (só a colônia trabalhando).</summary>
        static void ConfigureShowcase(AntFarmGame game)
        {
            var so = new SerializedObject(game);
            so.FindProperty("_showcase").boolValue = true;
            so.FindProperty("_mapSource").enumValueIndex = (int)MapSource.Procedural;
            so.FindProperty("_randomizeSeed").boolValue = false;
            so.FindProperty("_seed").intValue = MenuSeed;
            so.FindProperty("_termiteRaids").boolValue = false;
            so.FindProperty("_showDebugHud").boolValue = false;
            so.FindProperty("_showcaseHalfHeight").floatValue = 76f;
            so.FindProperty("_showcaseNestOnScreen").vector2Value = new Vector2(0.70f, 0.45f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Menu principal logo depois do splash (ou primeiro, sem ele) no build; a partida e a cena de template seguem na ordem em que já estavam.</summary>
        static void EnsureMenuInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != MenuScenePath).ToList();
            // O splash, quando existe, continua sendo a primeira cena; o menu vem logo depois.
            int first = scenes.Count > 0 && scenes[0].path == SplashSceneSetup.ScenePath ? 1 : 0;
            scenes.Insert(first, new EditorBuildSettingsScene(MenuScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void BuildMainMenu(Scene scene)
        {
            PrepareAssets();

            foreach (GameObject old in scene.GetRootGameObjects().Where(g => g.name == MenuName).ToArray())
                Object.DestroyImmediate(old);

            var canvasObject = new GameObject(MenuName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            // Casa a altura: a coluna de botões é ancorada no topo e vai até y ~1040, então em tela larga (21:9, 32:9) a altura
            // do canvas não pode encolher (com 0,5 ela encolhe e o rodapé sai da tela). Só a largura cresce.
            scaler.matchWidthOrHeight = 1f;
            EnsureEventSystem(scene);

            AddSceneMusic(canvasObject, MenuMusicPath);
            var menu = canvasObject.AddComponent<MainMenu>();
            var refs = new SerializedObject(menu);
            Transform root = canvasObject.transform;

            // --- logotipo do jogo (esquerda, abaixo da superfície)
            // PNG 3:1 no lugar do texto; preserveAspect mantém a proporção dentro do retângulo.
            Image title = new GameObject("Titulo", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            title.gameObject.layer = LayerMask.NameToLayer("UI");
            title.transform.SetParent(root, false);
            title.sprite = LoadLogo(GameLogoPath);
            title.preserveAspect = true;
            title.raycastTarget = false;
            Place(title.rectTransform, new Vector2(132, -330), new Vector2(600, 200));

            // --- coluna de botões
            var column = new GameObject("Botoes", typeof(RectTransform));
            column.layer = LayerMask.NameToLayer("UI");
            column.transform.SetParent(root, false);
            Stretch((RectTransform)column.transform);

            const float x = 140f, width = 503f, height = 48f, firstY = 656f, step = 74f;
            Button newGame = BuildMenuButton(column.transform, "IniciarNovaColonia", "decor/seedling", "INICIAR NOVA COLÔNIA",
                                             MenuButtonStyle.Primary, x, firstY, width, height, 38f, true, out _, out _);
            Button cont = BuildMenuButton(column.transform, "Continuar", "icons/lock", "CONTINUAR",
                                          MenuButtonStyle.Normal, x, firstY + step, width, height, 30f, false,
                                          out Image continueIcon, out Text continueLabel);
            Button settingsButton = BuildMenuButton(column.transform, "Configuracoes", "icons/gear", "CONFIGURAÇÕES",
                                                    MenuButtonStyle.Normal, x, firstY + step * 2, width, height, 36f, true, out _, out _);
            Button about = BuildMenuButton(column.transform, "SobreOJogo", "icons/leaf", "SOBRE O JOGO",
                                           MenuButtonStyle.Normal, x, firstY + step * 3, width, height, 36f, true, out _, out _);
            Button quit = BuildMenuButton(column.transform, "Sair", "icons/arrow_left", "SAIR",
                                          MenuButtonStyle.Normal, x, firstY + step * 4, width, height, 30f, true, out _, out _);

            Text footer = MenuText(column.transform, "Rodape", "", 16, MenuFooter, TextAnchor.UpperLeft);
            Place(footer.rectTransform, new Vector2(142, -1018), new Vector2(700, 24));

            // --- logo da Fábula (canto inferior direito): o logotipo oficial em branco, com fundo transparente
            Sprite logoSprite = LoadLogo(LogoPath);
            var logo = new GameObject("LogoFabula", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            logo.gameObject.layer = LayerMask.NameToLayer("UI");
            logo.transform.SetParent(root, false);
            logo.sprite = logoSprite;
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            var logoRect = logo.rectTransform;
            logoRect.anchorMin = logoRect.anchorMax = logoRect.pivot = new Vector2(1, 0);
            logoRect.anchoredPosition = new Vector2(-48, 40);
            logoRect.sizeDelta = new Vector2(330, 146);

            // --- troca rápida de idioma (canto superior direito); o nome do idioma atual é escrito pelo MainMenu
            Button language = BuildMenuButton(root, "Idioma", "icons/leaf", "", MenuButtonStyle.Normal, 1920 - 48 - 280, 64, 280, 48, 30f, true,
                                              out _, out Text languageLabel);
            languageLabel.alignment = TextAnchor.MiddleCenter;
            // Canto superior direito de verdade: com o canvas mais largo que 1920 a posição absoluta ficaria no meio da tela.
            var languageRect = (RectTransform)language.transform;
            languageRect.anchorMin = languageRect.anchorMax = new Vector2(1, 1);
            languageRect.pivot = new Vector2(1, 0.5f);
            languageRect.anchoredPosition = new Vector2(-48, -64);

            // --- configurações (a mesma tela do menu do ESC) e sobre o jogo, cada uma com o fundo escurecido atrás
            SettingsScreen settings = BuildSettingsScreen(root);
            AddDimmer(settings.transform);
            GameObject aboutPanel = BuildAboutPanel(root, out Button aboutClose);

            SetObject(refs, "_buttonsPanel", column);
            SetObject(refs, "_newGame", newGame);
            SetObject(refs, "_continue", cont);
            SetObject(refs, "_settingsButton", settingsButton);
            SetObject(refs, "_about", about);
            SetObject(refs, "_quit", quit);
            SetText(refs, "_footer", footer);
            SetObject(refs, "_language", language);
            SetText(refs, "_languageLabel", languageLabel);
            SetObject(refs, "_continueIcon", continueIcon);
            SetText(refs, "_continueLabel", continueLabel);
            SetObject(refs, "_continueUnlockedIcon", Load("icons/play"));
            SetObject(refs, "_continueLockedIcon", Load("icons/lock"));
            SetObject(refs, "_settings", settings);
            SetObject(refs, "_aboutPanel", aboutPanel);
            SetObject(refs, "_aboutClose", aboutClose);
            refs.FindProperty("_gameScene").stringValue = GameLaunch.GameSceneName;
            refs.ApplyModifiedPropertiesWithoutUndo();
        }

        enum MenuButtonStyle { Primary, Normal }

        /// <summary>
        /// Botão do menu: o sprite 9-slice do kit (verde para a ação principal, creme para o resto), o ícone à esquerda e o texto
        /// logo depois. Quando não está interativo, o sprite e o texto passam para o estado desativado (cinza).
        /// </summary>
        static Button BuildMenuButton(Transform parent, string name, string icon, string label, MenuButtonStyle style,
                                      float x, float y, float width, float height, float iconSize, bool interactable,
                                      out Image iconImage, out Text text)
        {
            string key = style == MenuButtonStyle.Primary ? "buttons/button_action" : "buttons/button_normal";
            Image image = BuildSprite(parent, name, key, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
            image.pixelsPerUnitMultiplier = 0.6f; // moldura mais grossa que a do HUD: o botão do menu é maior
            image.rectTransform.pivot = new Vector2(0, 0.5f);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                pressedSprite = Load("buttons/button_pressed"),
                highlightedSprite = style == MenuButtonStyle.Normal ? Load("buttons/button_focus") : null,
                selectedSprite = null,
                disabledSprite = Load("buttons/button_disabled"),
            };

            iconImage = BuildSprite(image.transform, "Icone", icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                                          new Vector2(18, 0), new Vector2(iconSize, iconSize));
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.rectTransform.pivot = new Vector2(0, 0.5f);

            bool primary = style == MenuButtonStyle.Primary;
            text = MenuText(image.transform, "Texto", label, 16, primary ? Color.white : Ink, TextAnchor.MiddleLeft);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(18 + iconSize + 16, 0);
            rect.offsetMax = new Vector2(-12, 0);
            if (primary)
                text.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            // Desativado (Continuar sem save): a cena guarda o visual de bloqueado e o MainMenu liga ao achar um save.
            button.interactable = interactable;
            if (!interactable)
                text.color = MenuLocked;
            return button;
        }

        /// <summary>Fundo escuro de tela cheia atrás de um painel central (filho do painel, desenhado primeiro).</summary>
        static void AddDimmer(Transform panel)
        {
            Image dim = BuildPlain(panel, "Escurecer", new Color(0.05f, 0.03f, 0.02f, 0.7f));
            dim.raycastTarget = true;
            var rect = dim.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(6000, 4000);
            rect.SetAsFirstSibling();
        }

        static GameObject BuildAboutPanel(Transform parent, out Button close)
        {
            const float width = 860f, height = 560f;
            Image panel = BuildPanelWithHeader(parent, "SobreOJogo", new Vector2(0.5f, 0.5f), Vector2.zero,
                                               new Vector2(width, height), "SOBRE O JOGO");
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Transform t = panel.transform;

            Text name = BuildText(t, "Nome", "INCREMENTANT", TextLarge, Ink, TextAnchor.UpperCenter);
            Place(name.rectTransform, new Vector2(0, -92), new Vector2(width, 48));
            Text tagline = BuildText(t, "Frase", "Uma colônia em constante transformação.", TextSmall, Ink, TextAnchor.UpperCenter);
            Place(tagline.rectTransform, new Vector2(0, -150), new Vector2(width, 28));
            Text body = BuildText(t, "Texto",
                                  "Cave, cultive e cuide da colônia.\nCada formiga tem o seu papel.\n\n" +
                                  "UM JOGO DA FÁBULA GAME STUDIO\n\n" +
                                  "Os nomes da equipe e os créditos\ndos assets serão incluídos aqui.",
                                  TextSmall, Ink, TextAnchor.UpperCenter);
            body.lineSpacing = 1.6f;
            Place(body.rectTransform, new Vector2(0, -214), new Vector2(width, 240));

            close = BuildButton(t, "Voltar", new Vector2(0.5f, 1), new Vector2(0, -(height - 84)), new Vector2(300, 48),
                                "icons/arrow_left", "[ESC] VOLTAR", true, out Image closeImage);
            closeImage.sprite = Load("buttons/button_action");
            AddDimmer(t);
            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        /// <summary>Logotipo (da Fábula ou do jogo, PNGs em Art/UI), importado como sprite.</summary>
        static Sprite LoadLogo(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[AntFarmHudSetup] Logo ausente: {path}");
            return sprite;
        }


        /// <summary>Texto do menu principal: a mesma fonte do HUD (Press Start 2P, só múltiplos de 8 saem com pixels inteiros).</summary>
        static Text MenuText(Transform parent, string name, string content, int size, Color color, TextAnchor alignment)
        {
            Text text = BuildText(parent, name, content, TextSmall, color, alignment);
            text.fontSize = size;
            return text;
        }
    }
}
