using System.Linq;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Audio;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.EditorTools.Localization;
using Fabula.Incrementant.EditorTools.Setup;
using Fabula.Incrementant.Tutorial;
using Fabula.Incrementant.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fabula.Incrementant.EditorTools.UI
{
    /// <summary>
    /// Monta o HUD da partida (referência <c>Refs de Telas/05-hud-partida.png</c>) na cena do AntFarm, com os sprites 9-slice
    /// de <c>Art/UI/Sprites</c>. Idempotente: o objeto <c>HUD</c> é descartado e recriado do zero a cada execução, então
    /// ajustes manuais dentro dele não sobrevivem; mexa no layout aqui. O canvas usa 1920x1080 de referência (`ReferenceResolution`); mude lá para escalar o HUD todo.
    /// </summary>
    public static partial class AntFarmHudSetup
    {
        const string HudName = "HUD";
        const string MenuMusicPath = "Assets/Tribal Jungle Music Free Pack/Primal.ogg";
        const string GameMusicPath = "Assets/Tribal Jungle Music Free Pack/Lost in the Jungle.ogg";
        const string Sprites = UiSpriteImporter.SpritesFolder;

        static readonly Color Ink = new Color(0.23f, 0.13f, 0.07f);
        static readonly Color InkDisabled = new Color(0.36f, 0.33f, 0.30f);
        static readonly Color Cream = new Color(1f, 0.96f, 0.84f);

        // Quanto maior a resolução de referência, menor o HUD na tela. 1920x1080 deixa 1 pixel de sprite = 1 pixel em tela cheia.
        static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);

        // Press Start 2P (Art/UI/Fontes/PressStart2P) é desenhada numa grade de 8 px: só múltiplos de 8 saem com pixels
        // inteiros (16 = 2 px por pixel da fonte, 24 = 3, 32 = 4, 48 = 6). É monoespaçada: cada letra ocupa o próprio
        // tamanho de largura (16 px por letra em 16). Todo texto do HUD usa um destes tamanhos (BuildText ajusta).
        const int TextSmall = 16, TextMedium = 24, TextLarge = 32, TextTitle = 48;
        static Font _font;

        [MenuItem("Incrementant/Setup/Montar HUD da partida")]
        public static void CreateOrUpdate()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AntFarmSceneSetup.ScenePath) == null)
            {
                Debug.LogError("[AntFarmHudSetup] Cena do AntFarm não existe: rode Incrementant/Setup/Criar cena AntFarm.");
                return;
            }
            Scene scene = EditorSceneManager.OpenScene(AntFarmSceneSetup.ScenePath, OpenSceneMode.Single);
            if (!Build(scene))
                return;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, AntFarmSceneSetup.ScenePath);
            Debug.Log("[AntFarmHudSetup] HUD montado na cena do AntFarm.");
        }

        /// <summary>Põe no objeto o <see cref="SceneMusic"/> com a faixa da cena (o crossfade entre cenas é do <see cref="MusicPlayer"/>).</summary>
        static void AddSceneMusic(GameObject target, string clipPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
                Debug.LogError($"[AntFarmHudSetup] Música ausente: {clipPath}");
            var music = new SerializedObject(target.AddComponent<SceneMusic>());
            music.FindProperty("_track").objectReferenceValue = clip;
            music.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Reaplica os importadores de sprites e fontes de UI e carrega a fonte do HUD. Usado também pelo menu principal.</summary>
        static void PrepareAssets()
        {
            // Tabela de textos pronta antes de qualquer rótulo ser montado (cada BuildText liga o seu a ela).
            LocalizationSetup.Prepare(force: true);
            UiSpriteImporter.Apply();
            SkillTreeSpriteImporter.Apply();
            UiFontImporter.Apply();
            _font = UiFontImporter.Load();
            if (_font == null)
            {
                Debug.LogError("[AntFarmHudSetup] Fontes de Art/UI/Fontes ausentes: usando a fonte padrão da Unity.");
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        /// <summary>Recria o HUD na cena dada (sem salvar). Chamado também pelo setup da cena.</summary>
        public static bool Build(Scene scene)
        {
            AntFarmGame game = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<AntFarmGame>(true)).FirstOrDefault();
            if (game == null)
            {
                Debug.LogError("[AntFarmHudSetup] AntFarmGame não encontrado na cena.");
                return false;
            }
            PrepareAssets();

            foreach (GameObject old in scene.GetRootGameObjects().Where(g => g.name == HudName).ToArray())
                Object.DestroyImmediate(old);

            var canvasObject = new GameObject(HudName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvasObject.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem(scene);

            AddSceneMusic(canvasObject, GameMusicPath);
            var hud = canvasObject.AddComponent<AntFarmHud>();
            var refs = new SerializedObject(hud);
            refs.FindProperty("_game").objectReferenceValue = game;
            Transform root = canvasObject.transform;

            // --- cartões do topo
            BuildCard(root, "CardNutrientes", new Vector2(0, 1), new Vector2(16, -16), new Vector2(380, 100),
                      "icons/leaf", "NUTRIENTES", out Text nv, out Text nd);
            BuildCard(root, "CardFormigas", new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(560, 100),
                      "icons/ant", "FORMIGAS", out Text av, out Text ad);
            BuildCard(root, "CardRainha", new Vector2(1, 1), new Vector2(-16, -16), new Vector2(380, 100),
                      "icons/crown", "RAINHA", out Text qv, out Text qd);
            SetText(refs, "_nutrientsValue", nv); SetText(refs, "_nutrientsDetail", nd);
            SetText(refs, "_antsValue", av); SetText(refs, "_antsDetail", ad);
            SetText(refs, "_queenValue", qv); SetText(refs, "_queenDetail", qd);

            // --- FPS e tempo jogado (canto superior direito, abaixo do cartão da rainha)
            Text stats = BuildText(root, "FpsTempo", "", TextSmall, Cream, TextAnchor.UpperRight);
            stats.lineSpacing = 1.5f;
            var statsRect = stats.rectTransform;
            statsRect.anchorMin = statsRect.anchorMax = statsRect.pivot = new Vector2(1, 1);
            statsRect.anchoredPosition = new Vector2(-20, -124);
            statsRect.sizeDelta = new Vector2(360, 56);
            stats.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1, -1);
            SetText(refs, "_stats", stats);

            // --- menu Colônia (esquerda)
            Image menu = BuildPanelWithHeader(root, "MenuColonia", new Vector2(0, 1), new Vector2(16, -214), new Vector2(260, 316), "COLÔNIA");
            string[] entries = { "FERRAMENTA", "EVOLUÇÃO", "RAINHA", "AMBIENTES", "AVISOS" };
            string[] entryIcons = { "icons/shovel", "icons/leaf", "icons/crown", "icons/mushroom", "icons/warning" };
            for (int i = 0; i < entries.Length; i++)
            {
                // Pintar, Evolução, Rainha e Ambientes existem no jogo; Avisos fica desativado até a tela dele ser feita.
                bool exists = i <= 3;
                Button b = BuildButton(menu.transform, entries[i], new Vector2(0.5f, 1), new Vector2(0, -56 - i * 50),
                                       new Vector2(228, 42), entryIcons[i], entries[i], interactable: exists, out Image image);
                if (i == 0)
                {
                    SetObject(refs, "_paintButton", b);
                    SetObject(refs, "_paintButtonImage", image);
                }
                else if (i == 1)
                {
                    SetObject(refs, "_treeButton", b);
                    SetObject(refs, "_treeButtonImage", image);
                }
                else if (i == 2)
                {
                    SetObject(refs, "_queenButton", b);
                    SetObject(refs, "_queenButtonImage", image);
                }
                else if (i == 3)
                {
                    SetObject(refs, "_environmentButton", b);
                    SetObject(refs, "_environmentButtonImage", image);
                }
            }

            // --- submenus de pincéis (ao lado do menu; escondidos até clicar em Pintar ou Ambientes). Os botões ficam num
            // vetor único na ordem de AntFarmGame.BrushLabels; cada grupo só decide em qual painel o botão mora.
            int brushCount = AntFarmGame.BrushCount;
            string[] brushIcons = { "icons/shovel", "icons/crate", "icons/crown", "icons/mushroom", "icons/egg", "icons/close", "icons/hand_index_up", "icons/leaf" };
            int[] paintBrushes = { 0, 1, 6 };       // Cavar, Construir, Dedo
            int[] environmentBrushes = { 2, 3, 4, 5, 7 }; // Sala da rainha, Sala de nutrientes, Berçário, Resíduos, Gotas de orvalho
            var brushButtons = new Button[brushCount];
            var brushImages = new Image[brushCount];
            Image paint = BuildBrushPanel(root, "SubmenuFerramenta", "FERRAMENTA", paintBrushes, brushIcons, brushButtons, brushImages);
            Image environment = BuildBrushPanel(root, "SubmenuAmbientes", "AMBIENTES", environmentBrushes, brushIcons, brushButtons, brushImages);
            SetObject(refs, "_paintPanel", paint.gameObject);
            SetObject(refs, "_environmentPanel", environment.gameObject);
            SetArray(refs, "_brushButtons", brushButtons);
            SetArray(refs, "_brushImages", brushImages);

            BuildQueenPanel(root, refs);
            BuildRaidAlert(root, refs);

            // --- velocidade (canto inferior esquerdo)
            Image speedPanel = BuildSprite(root, "Velocidade", "panels/hud_card", new Vector2(0, 0), new Vector2(0, 0),
                                           new Vector2(16, 16), new Vector2(284, 76));
            Sprite iconIdle = Load("buttons/icon_button");
            Button pause = BuildIconButton(speedPanel.transform, "Pausa", 0, "icons/pause", null, out Image pauseImage);
            int speedCount = AntFarmGame.SpeedCount;
            var speedButtons = new Button[speedCount];
            var speedImages = new Image[speedCount];
            for (int i = 0; i < speedCount; i++)
                speedButtons[i] = BuildIconButton(speedPanel.transform, "Velocidade" + i, i + 1, null,
                                                  AntFarmGame.SpeedMultiplier(i) + "X", out speedImages[i]);
            SetObject(refs, "_pauseButton", pause);
            SetObject(refs, "_pauseImage", pauseImage);
            SetArray(refs, "_speedButtons", speedButtons);
            SetArray(refs, "_speedImages", speedImages);

            // --- sino (acima da velocidade): recolhe as formigas ao formigueiro / manda de volta ao trabalho
            Image recallPanel = BuildSprite(root, "Sino", "panels/hud_card", new Vector2(0, 0), new Vector2(0, 0),
                                            new Vector2(16, 100), new Vector2(84, 76));
            Button recall = BuildIconButton(recallPanel.transform, "SinoBotao", 0, "icons/bell", null, out Image recallImage);
            SetObject(refs, "_recallButton", recall);
            SetObject(refs, "_recallImage", recallImage);

            // --- dica no rodapé
            Text hint = BuildText(root, "Dica", "", TextSmall, Cream, TextAnchor.MiddleCenter);
            var hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0, 0);
            hintRect.anchorMax = new Vector2(1, 0);
            hintRect.pivot = new Vector2(0.5f, 0);
            hintRect.offsetMin = new Vector2(320, 28);
            hintRect.offsetMax = new Vector2(-320, 60);
            hint.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1, -1);
            SetText(refs, "_hint", hint);

            // A tela de evolução vai por último: cobre o resto do HUD e fica escondida até o botão EVOLUÇÃO.
            SetObject(refs, "_treePanel", BuildTreeScreen(root, game));
            // O tutorial fica acima da tela de evolução (ele a explica) e abaixo do menu do ESC.
            SetObject(refs, "_tutorial", BuildTutorial(root, game, hud));
            // O menu do ESC fica acima de tudo, inclusive da tela de evolução e do tutorial.
            SetObject(refs, "_pauseMenu", BuildPauseMenu(root, game));

            SetObject(refs, "_fingerCursor", AssetDatabase.LoadAssetAtPath<Texture2D>(UiCursorImporter.CursorsFolder + "/cursor_finger.png"));
            SetObject(refs, "_buttonIdle", Load("buttons/button_normal"));
            SetObject(refs, "_buttonActive", Load("buttons/button_focus"));
            SetObject(refs, "_iconButtonIdle", iconIdle);
            SetObject(refs, "_iconButtonActive", Load("buttons/icon_button_focus"));
            refs.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // ---------------------------------------------------------------- blocos

        /// <summary>
        /// Alerta dos ataques de cupins: faixa no centro do topo, logo abaixo do cartão Formigas. Escondida fora do ataque; não
        /// bloqueia o pincel (sem raycast).
        /// </summary>
        static void BuildRaidAlert(Transform root, SerializedObject refs)
        {
            Image panel = BuildSprite(root, "AlertaCupins", "panels/notification", new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                                      new Vector2(0, -128), new Vector2(1000, 60));
            panel.raycastTarget = false;
            Image icon = BuildSprite(panel.transform, "Icone", "icons/warning", new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                                     new Vector2(18, 0), new Vector2(36, 36));
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Text text = BuildText(panel.transform, "Texto", "", TextSmall, Ink, TextAnchor.MiddleCenter);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(64, 0);
            rect.offsetMax = new Vector2(-16, 0);
            panel.gameObject.SetActive(false);
            SetObject(refs, "_raidAlert", panel.gameObject);
            SetText(refs, "_raidAlertText", text);
        }

        /// <summary>Painel de pincéis ao lado do menu Colônia, com um botão por índice de <paramref name="brushes"/>; começa escondido.</summary>
        static Image BuildBrushPanel(Transform root, string name, string title, int[] brushes, string[] icons, Button[] buttons, Image[] images)
        {
            Image panel = BuildPanelWithHeader(root, name, new Vector2(0, 1), new Vector2(286, -214),
                                               new Vector2(400, 62 + brushes.Length * 50 + 12), title);
            for (int row = 0; row < brushes.Length; row++)
            {
                int brush = brushes[row];
                buttons[brush] = BuildButton(panel.transform, "Pincel" + brush, new Vector2(0.5f, 1), new Vector2(0, -56 - row * 50),
                                             new Vector2(368, 42), icons[brush % icons.Length],
                                             AntFarmGame.BrushLabel(brush).ToUpperInvariant(), interactable: true, out images[brush]);
            }
            panel.gameObject.SetActive(false);
            return panel;
        }

        /// <summary>Submenu da rainha: modo (automático, específico, pausado), tipo, o que vem a seguir e o botão de criar.</summary>
        static void BuildQueenPanel(Transform root, SerializedObject refs)
        {
            Image panel = BuildPanelWithHeader(root, "SubmenuRainha", new Vector2(0, 1), new Vector2(286, -214),
                                               new Vector2(560, 62 + 3 * 50 + 16 + 2 * 50 + 16 + AntFarmHud.CreatableRoles.Length * 50 + 4 * 24 + 20), "RAINHA");
            QueenMode[] modes = { QueenMode.Automatic, QueenMode.Specific, QueenMode.Paused };
            string[] modeIcons = { "icons/gear", "icons/ant", "icons/pause" };
            var modeButtons = new Button[modes.Length];
            var modeImages = new Image[modes.Length];
            for (int i = 0; i < modes.Length; i++)
                modeButtons[i] = BuildButton(panel.transform, "Modo" + i, new Vector2(0.5f, 1), new Vector2(0, -56 - i * 50),
                                             new Vector2(528, 42), modeIcons[i], LocalizationCatalog.Pt(AntFarmHud.ModeKey(modes[i])), true, out modeImages[i]);

            var typeButtons = new Button[AntFarmHud.CreatableRoles.Length];
            var typeImages = new Image[typeButtons.Length];
            var typeLabels = new Text[typeButtons.Length];
            // Postura diante dos cupins: defensiva e ofensiva.
            QueenStance[] stances = { QueenStance.Defensive, QueenStance.Offensive };
            string[] stanceIcons = { "icons/lock", "icons/warning" };
            var stanceButtons = new Button[stances.Length];
            var stanceImages = new Image[stances.Length];
            int stancesTop = 56 + modes.Length * 50 + 16;
            for (int i = 0; i < stances.Length; i++)
                stanceButtons[i] = BuildButton(panel.transform, "Postura" + i, new Vector2(0.5f, 1), new Vector2(0, -stancesTop - i * 50),
                                               new Vector2(528, 42), stanceIcons[i], LocalizationCatalog.Pt(AntFarmHud.StanceKey(stances[i])), true, out stanceImages[i]);

            int typesTop = stancesTop + stances.Length * 50 + 16;
            for (int i = 0; i < typeButtons.Length; i++)
            {
                typeButtons[i] = BuildButton(panel.transform, "Tipo" + i, new Vector2(0.5f, 1), new Vector2(0, -typesTop - i * 50),
                                             new Vector2(528, 42), "icons/ant", LocalizationCatalog.Pt(AntFarmHud.RoleKey(AntFarmHud.CreatableRoles[i])).ToUpperInvariant(),
                                             true, out typeImages[i]);
                // O ícone é a própria arte da classe (já colorida); sem ela, cai no ícone genérico tingido.
                Image roleIcon = typeImages[i].transform.Find("Icone").GetComponent<Image>();
                Sprite roleArt = Setup.AntFarmSceneSetup.RoleIcon(AntFarmHud.CreatableRoles[i]);
                if (roleArt != null)
                    roleIcon.sprite = roleArt;
                roleIcon.color = roleArt != null ? Color.white : AntSwarmView.RoleColor(AntFarmHud.CreatableRoles[i]);
                typeLabels[i] = typeButtons[i].transform.Find("Texto").GetComponent<Text>();
                typeLabels[i].fontSize = TextSmall;
            }

            int infoTop = typesTop + typeButtons.Length * 50 + 4;
            Text state = BuildText(panel.transform, "Estado", "", TextSmall, Ink, TextAnchor.MiddleCenter);
            Place(state.rectTransform, new Vector2(16, -infoTop), new Vector2(528, 24));
            Text next = BuildText(panel.transform, "Proximo", "", TextSmall, Ink, TextAnchor.MiddleCenter);
            Place(next.rectTransform, new Vector2(16, -infoTop - 24), new Vector2(528, 24));
            Text brood = BuildText(panel.transform, "Crias", "", TextSmall, Ink, TextAnchor.MiddleCenter);
            Place(brood.rectTransform, new Vector2(16, -infoTop - 48), new Vector2(528, 24));
            Text space = BuildText(panel.transform, "Espaco", "", TextSmall, Ink, TextAnchor.MiddleCenter);
            Place(space.rectTransform, new Vector2(16, -infoTop - 72), new Vector2(528, 24));

            panel.gameObject.SetActive(false);
            SetObject(refs, "_queenPanel", panel.gameObject);
            SetArray(refs, "_modeButtons", modeButtons);
            SetArray(refs, "_modeImages", modeImages);
            SetArray(refs, "_stanceButtons", stanceButtons);
            SetArray(refs, "_stanceImages", stanceImages);
            SetArray(refs, "_typeButtons", typeButtons);
            SetArray(refs, "_typeImages", typeImages);
            SetArray(refs, "_typeLabels", typeLabels);
            SetText(refs, "_queenState", state);
            SetText(refs, "_queenNext", next);
            SetText(refs, "_queenBrood", brood);
            SetText(refs, "_queenSpace", space);
        }


        // ---------------------------------------------------------------- tutorial

        /// <summary>
        /// Pop-ups do tutorial da primeira abertura: central (abertura e despedida, com fundo que bloqueia cliques), lateral
        /// (instruções ao lado do alvo) e os botões "Ver objetivo" e "Pular tutorial". Tudo começa escondido; o
        /// <see cref="TutorialDirector"/> posiciona, preenche e liga conforme o roteiro. Destaques, contorno das câmaras e o
        /// marcador da entrada o diretor cria em código.
        /// </summary>
        static TutorialDirector BuildTutorial(Transform root, AntFarmGame game, AntFarmHud hud)
        {
            var go = new GameObject("Tutorial", typeof(RectTransform), typeof(TutorialDirector));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(root, false);
            Stretch((RectTransform)go.transform);
            var so = new SerializedObject(go.GetComponent<TutorialDirector>());
            SetObject(so, "_game", game);
            SetObject(so, "_hud", hud);

            // --- pop-up central
            Image backdrop = BuildPlain(go.transform, "PopupCentral", new Color(0.05f, 0.03f, 0.02f, 0.45f));
            backdrop.raycastTarget = true;
            Stretch(backdrop.rectTransform);
            Image modalPanel = BuildSprite(backdrop.transform, "Painel", "panels/panel_moss", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                           Vector2.zero, new Vector2(900, 340));
            modalPanel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Text modalTitle = BuildText(modalPanel.transform, "Titulo", "", TextLarge, Ink, TextAnchor.UpperCenter);
            PlaceStretchTop(modalTitle.rectTransform, 36, 40, 56);
            Text modalText = BuildText(modalPanel.transform, "Texto", "", TextMedium, Ink, TextAnchor.UpperLeft);
            modalText.horizontalOverflow = HorizontalWrapMode.Wrap;
            modalText.lineSpacing = 1.4f;
            PlaceStretchTop(modalText.rectTransform, 100, 400, 56);
            Button modalButton = BuildActionButton(modalPanel.transform, "Botao", new Vector2(360, 56), TextMedium, out Text modalLabel);
            SetBottomCenter((RectTransform)modalButton.transform, 36f);
            backdrop.gameObject.SetActive(false);
            SetObject(so, "_modal", backdrop.gameObject);
            SetObject(so, "_modalPanel", modalPanel.rectTransform);
            SetText(so, "_modalTitle", modalTitle);
            SetText(so, "_modalText", modalText);
            SetObject(so, "_modalButton", modalButton);
            SetText(so, "_modalButtonLabel", modalLabel);

            // --- pop-up lateral
            Image sidePanel = BuildSprite(go.transform, "PopupLateral", "panels/panel_plain", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                          Vector2.zero, new Vector2(520, 220));
            sidePanel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Text sideText = BuildText(sidePanel.transform, "Texto", "", TextSmall, Ink, TextAnchor.UpperLeft);
            sideText.horizontalOverflow = HorizontalWrapMode.Wrap;
            sideText.lineSpacing = 1.5f;
            PlaceStretchTop(sideText.rectTransform, 28, 300, 28);
            Button sideButton = BuildActionButton(sidePanel.transform, "Botao", new Vector2(240, 48), TextSmall, out Text sideLabel);
            SetBottomCenter((RectTransform)sideButton.transform, 28f);
            sidePanel.gameObject.SetActive(false);
            SetObject(so, "_side", sidePanel.gameObject);
            SetObject(so, "_sidePanel", sidePanel.rectTransform);
            SetText(so, "_sideText", sideText);
            SetObject(so, "_sideButton", sideButton);
            SetText(so, "_sideButtonLabel", sideLabel);

            // --- "Ver objetivo" (acima da dica do rodapé) e "Pular tutorial" (canto inferior direito)
            Button viewGoal = BuildActionButton(go.transform, "VerObjetivo", new Vector2(340, 52), TextSmall, out Text viewLabel);
            LocalizationSetup.Bind(viewLabel, "Ver objetivo");
            var viewRect = (RectTransform)viewGoal.transform;
            viewRect.anchorMin = viewRect.anchorMax = new Vector2(0.5f, 0f);
            viewRect.pivot = new Vector2(0.5f, 0f);
            viewRect.anchoredPosition = new Vector2(0f, 76f);
            viewGoal.gameObject.SetActive(false);
            SetObject(so, "_viewGoalButton", viewGoal);

            Image skipImage = BuildSprite(go.transform, "Pular", "buttons/button_normal", new Vector2(1, 0), new Vector2(1, 0),
                                          new Vector2(-16, 16), new Vector2(330, 44));
            skipImage.rectTransform.pivot = new Vector2(1, 0);
            var skip = skipImage.gameObject.AddComponent<Button>();
            skip.targetGraphic = skipImage;
            Text skipLabel = BuildText(skipImage.transform, "Texto", "Pular tutorial", TextSmall, Ink, TextAnchor.MiddleCenter);
            Stretch(skipLabel.rectTransform);
            skipImage.gameObject.SetActive(false);
            SetObject(so, "_skipButton", skip);

            so.ApplyModifiedPropertiesWithoutUndo();
            return go.GetComponent<TutorialDirector>();
        }

        /// <summary>Botão verde de ação (sprite <c>button_action</c>) com o texto creme, tamanho fixo.</summary>
        static Button BuildActionButton(Transform parent, string name, Vector2 size, int fontSize, out Text label)
        {
            Image image = BuildSprite(parent, name, "buttons/button_action", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            label = BuildText(image.transform, "Texto", "", fontSize, Cream, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            label.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            return button;
        }

        /// <summary>Faixa na largura do pai, <paramref name="top"/> abaixo do topo, com <paramref name="side"/> de margem.</summary>
        static void PlaceStretchTop(RectTransform rect, float top, float height, float side)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = new Vector2(side, -top - height);
            rect.offsetMax = new Vector2(-side, -top);
        }

        static void SetBottomCenter(RectTransform rect, float bottom)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
        }

        // ---------------------------------------------------------------- menu do ESC e configurações

        /// <summary>
        /// Menu do ESC: fundo escurecido em tela cheia (bloqueia cliques no mundo e no HUD), o painel do menu no centro e a
        /// tela de configurações, que troca de lugar com ele. Começa escondido; o <see cref="AntFarmHud"/> abre no ESC.
        /// </summary>
        static PauseMenu BuildPauseMenu(Transform root, AntFarmGame game)
        {
            var go = new GameObject("MenuPausa", typeof(RectTransform), typeof(Image), typeof(PauseMenu));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(root, false);
            Stretch((RectTransform)go.transform);
            var backdrop = go.GetComponent<Image>();
            backdrop.color = new Color(0.05f, 0.03f, 0.02f, 0.6f);
            backdrop.raycastTarget = true;

            string[] labels = { "CONTINUAR", "CONFIGURAÇÕES", "SALVAR PARTIDA", "CARREGAR PARTIDA", "MENU PRINCIPAL", "SAIR DO JOGO" };
            string[] icons = { "icons/play", "icons/gear", "icons/arrow_down", "icons/arrow_up", "icons/arrow_left", "icons/close" };
            const float width = 520f;
            Image panel = BuildPanelWithHeader(go.transform, "PainelMenu", new Vector2(0.5f, 0.5f), Vector2.zero,
                                               new Vector2(width, 62 + labels.Length * 56 + 52), "MENU");
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                buttons[i] = BuildButton(panel.transform, "Botao" + i, new Vector2(0.5f, 1), new Vector2(0, -56 - i * 56),
                                         new Vector2(width - 48, 46), icons[i], labels[i], true, out Image image);
                if (i == labels.Length - 1)
                    image.sprite = Load("buttons/button_danger");
            }
            Text status = BuildText(panel.transform, "Aviso", "", TextSmall, Ink, TextAnchor.MiddleCenter);
            Place(status.rectTransform, new Vector2(16, -56 - labels.Length * 56 - 4), new Vector2(width - 32, 32));

            SettingsScreen settings = BuildSettingsScreen(go.transform);

            var so = new SerializedObject(go.GetComponent<PauseMenu>());
            SetObject(so, "_game", game);
            SetObject(so, "_mainPanel", panel.gameObject);
            SetObject(so, "_settings", settings);
            SetObject(so, "_resume", buttons[0]);
            SetObject(so, "_settingsButton", buttons[1]);
            SetObject(so, "_save", buttons[2]);
            SetObject(so, "_load", buttons[3]);
            SetObject(so, "_mainMenu", buttons[4]);
            SetObject(so, "_quit", buttons[5]);
            SetText(so, "_status", status);
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(false);
            return go.GetComponent<PauseMenu>();
        }

        // Tela de configurações: uma linha por opção, rótulo à esquerda e controle à direita.
        const float SettingsWidth = 980f, SettingsRow = 64f, SettingsTop = 64f, SettingsControlX = 500f;

        static SettingsScreen BuildSettingsScreen(Transform parent)
        {
            string[] rows =
            {
                "VOLUME GERAL", "MÚSICA", "EFEITOS SONOROS", "EFEITOS VISUAIS", "TELA CHEIA", "RESOLUÇÃO", "VSYNC",
                "LIMITE DE FPS", "MOSTRAR FPS E TEMPO",
                "IDIOMA",
            };
            Image panel = BuildPanelWithHeader(parent, "TelaConfiguracoes", new Vector2(0.5f, 0.5f), Vector2.zero,
                                               new Vector2(SettingsWidth, SettingsTop + rows.Length * SettingsRow + 84), "CONFIGURAÇÕES");
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var screen = panel.gameObject.AddComponent<SettingsScreen>();
            var so = new SerializedObject(screen);
            Transform t = panel.transform;

            for (int i = 0; i < rows.Length; i++)
            {
                Text label = BuildText(t, "Rotulo" + i, rows[i], TextSmall, Ink, TextAnchor.MiddleLeft);
                Place(label.rectTransform, new Vector2(40, -RowTop(i)), new Vector2(440, SettingsRow));
                if (i > 0)
                {
                    Image line = BuildPlain(t, "Divisoria" + i, new Color(0.23f, 0.13f, 0.07f, 0.15f));
                    Place(line.rectTransform, new Vector2(32, -RowTop(i)), new Vector2(SettingsWidth - 64, 2));
                }
            }

            BuildSliderRow(t, 0, so, "_master", "_masterValue");
            BuildSliderRow(t, 1, so, "_music", "_musicValue");
            BuildSliderRow(t, 2, so, "_sfx", "_sfxValue");
            BuildCyclerRow(t, 3, so, "_vfxPrevious", "_vfxNext", "_vfxValue");
            BuildToggleRow(t, 4, so, "_fullscreen", "_fullscreenImage");
            BuildCyclerRow(t, 5, so, "_resolutionPrevious", "_resolutionNext", "_resolutionValue");
            BuildToggleRow(t, 6, so, "_vsync", "_vsyncImage");
            BuildCyclerRow(t, 7, so, "_fpsPrevious", "_fpsNext", "_fpsValue");
            BuildToggleRow(t, 8, so, "_showStats", "_showStatsImage");
            BuildCyclerRow(t, 9, so, "_languagePrevious", "_languageNext", "_languageValue");

            float footer = RowTop(rows.Length) + 16;
            Button defaults = BuildButton(t, "Padrao", new Vector2(0.5f, 1), new Vector2(-150, -footer), new Vector2(280, 48),
                                          "icons/gear", "PADRÃO", true, out _);
            Button back = BuildButton(t, "Voltar", new Vector2(0.5f, 1), new Vector2(150, -footer), new Vector2(280, 48),
                                      "icons/arrow_left", "[ESC] VOLTAR", true, out Image backImage);
            backImage.sprite = Load("buttons/button_action");
            SetObject(so, "_defaults", defaults);
            SetObject(so, "_back", back);
            SetObject(so, "_toggleOn", Load("controls/toggle_on"));
            SetObject(so, "_toggleOff", Load("controls/toggle_off"));
            so.ApplyModifiedPropertiesWithoutUndo();
            panel.gameObject.SetActive(false);
            return screen;
        }

        static float RowTop(int row) => SettingsTop + row * SettingsRow;

        /// <summary>Centro vertical da linha, a partir do topo do painel (y para baixo).</summary>
        static float RowCenter(int row) => RowTop(row) + SettingsRow * 0.5f;

        /// <summary>Slider de 0 a 1 em escala 2 (trilho, preenchimento e alça do kit) e o valor em % à direita.</summary>
        static void BuildSliderRow(Transform panel, int row, SerializedObject so, string sliderField, string valueField)
        {
            const float scale = 2f, width = 330f;
            var go = new GameObject("Slider" + row, typeof(RectTransform), typeof(Slider));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(panel, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(SettingsControlX, -RowCenter(row));
            rect.sizeDelta = new Vector2(width, 56);

            Image track = BuildScaledSprite(go.transform, "Trilho", "sliders/slider_track", scale);
            var trackRect = track.rectTransform;
            trackRect.anchorMin = new Vector2(0, 0.5f);
            trackRect.anchorMax = new Vector2(1, 0.5f);
            trackRect.sizeDelta = new Vector2(0, 12 * scale);
            track.raycastTarget = true; // clicar no trilho também move o valor

            var fillArea = new GameObject("AreaPreenchimento", typeof(RectTransform));
            fillArea.layer = go.layer;
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.anchorMin = new Vector2(0, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(0, 12 * scale);
            Image fill = BuildScaledSprite(fillArea.transform, "Preenchimento", "sliders/slider_fill", scale);
            Stretch(fill.rectTransform);

            var handleArea = new GameObject("AreaAlca", typeof(RectTransform));
            handleArea.layer = go.layer;
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRect = (RectTransform)handleArea.transform;
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(18, 0); // meia alça de folga: ela não sai do trilho nos extremos
            handleAreaRect.offsetMax = new Vector2(-18, 0);
            Image handle = BuildScaledSprite(handleArea.transform, "Alca", "sliders/slider_handle", scale);
            handle.type = Image.Type.Simple;
            handle.raycastTarget = true;
            // O Slider estica a alça na altura da área (56 = 28 px do sprite em escala 2): só a largura vai no sizeDelta.
            handle.rectTransform.sizeDelta = new Vector2(18 * scale, 0);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 1f;

            Text value = BuildText(panel, "Valor" + row, "100%", TextSmall, Ink, TextAnchor.MiddleRight);
            float valueX = SettingsControlX + width + 8;
            Place(value.rectTransform, new Vector2(valueX, -RowTop(row)), new Vector2(SettingsWidth - 40 - valueX, SettingsRow));
            SetObject(so, sliderField, slider);
            SetText(so, valueField, value);
        }

        /// <summary>Seletor com duas setas e o valor escolhido no meio.</summary>
        static void BuildCyclerRow(Transform panel, int row, SerializedObject so, string previousField, string nextField, string valueField)
        {
            const float arrow = 52f, span = 440f;
            Button previous = BuildArrowButton(panel, "Anterior" + row, "icons/arrow_left", new Vector2(SettingsControlX, -RowCenter(row)), arrow);
            Button next = BuildArrowButton(panel, "Proximo" + row, "icons/arrow_right",
                                           new Vector2(SettingsControlX + span - arrow, -RowCenter(row)), arrow);
            Text value = BuildText(panel, "Valor" + row, "", TextSmall, Ink, TextAnchor.MiddleCenter);
            Place(value.rectTransform, new Vector2(SettingsControlX + arrow, -RowTop(row)), new Vector2(span - 2 * arrow, SettingsRow));
            SetObject(so, previousField, previous);
            SetObject(so, nextField, next);
            SetText(so, valueField, value);
        }

        static Button BuildArrowButton(Transform panel, string name, string icon, Vector2 position, float size)
        {
            Image image = BuildSprite(panel, name, "buttons/icon_button", new Vector2(0, 1), new Vector2(0, 1), position, new Vector2(size, size));
            image.rectTransform.pivot = new Vector2(0, 0.5f);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                pressedSprite = Load("buttons/icon_button_focus"),
                disabledSprite = Load("buttons/icon_button_disabled"),
            };
            Image iconImage = BuildSprite(image.transform, "Icone", icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                          Vector2.zero, new Vector2(28, 22));
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return button;
        }

        /// <summary>Interruptor liga/desliga (sprites <c>controls/toggle_*</c> em escala 2); a tela troca o sprite.</summary>
        static void BuildToggleRow(Transform panel, int row, SerializedObject so, string buttonField, string imageField)
        {
            Image image = BuildScaledSprite(panel, "Interruptor" + row, "controls/toggle_on", 2f);
            image.type = Image.Type.Simple;
            image.raycastTarget = true;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 0.5f);
            rect.anchoredPosition = new Vector2(SettingsControlX, -RowCenter(row));
            rect.sizeDelta = new Vector2(50 * 2, 26 * 2);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            SetObject(so, buttonField, button);
            SetObject(so, imageField, image);
        }

        /// <summary>Sprite do kit geral desenhado a <paramref name="scale"/> pixels de canvas por pixel de sprite (9-slice incluso).</summary>
        static Image BuildScaledSprite(Transform parent, string name, string key, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = Load(key);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f / scale;
            image.raycastTarget = false;
            return image;
        }

        // ---------------------------------------------------------------- tela de evolução (árvore)

        const string Kit = SkillTreeSpriteImporter.Folder;
        // O kit é pixel art: 1 pixel do sprite = 2 pixels do canvas (1920x1080), então tudo sai crisp.
        const float PixelScale = 2f;

        static readonly Color Green = new Color(0.55f, 0.78f, 0.35f);
        static readonly Color Muted = new Color(0.72f, 0.66f, 0.55f);

        static Sprite LoadKit(string key)
        {
            string path = $"{Kit}/{key}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[AntFarmHudSetup] Sprite da árvore ausente: {path}");
            return sprite;
        }

        /// <summary>
        /// Tela de evolução em tela cheia (referência <c>22-arvore-formigueiro-radial.png</c>): fundo de terra, título,
        /// nutrientes, VOLTAR, abas, a árvore do formigueiro (só exibição), a da rainha e classes, painel de detalhe e
        /// legenda. Fica por último sob o canvas para cobrir o resto do HUD. Os nós seguem a ordem de
        /// <see cref="QueenNode"/> e de <see cref="HiveTreeInfo.All"/>, que é o que o <see cref="SkillTreeScreen"/> espera.
        /// </summary>
        static GameObject BuildTreeScreen(Transform root, AntFarmGame game)
        {
            var go = new GameObject("TelaEvolucao", typeof(RectTransform), typeof(Image), typeof(SkillTreeScreen));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(root, false);
            Stretch((RectTransform)go.transform);
            var background = go.GetComponent<Image>();
            background.sprite = LoadKit("Backgrounds/skill_tree_earth_1920x1080");
            background.type = Image.Type.Simple;
            background.raycastTarget = true;
            Transform screen = go.transform;
            var so = new SerializedObject(go.GetComponent<SkillTreeScreen>());
            SetObject(so, "_game", game);

            // --- cabeçalho
            Text title = BuildText(screen, "Titulo", "EVOLUÇÃO DA COLÔNIA", TextTitle, Cream, TextAnchor.UpperLeft);
            PlaceTop(title.rectTransform, new Vector2(70, 34), new Vector2(980, 60));
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);
            Image leaf = BuildKitImage(screen, "IconeFolha", "UI/icon_leaf", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, 1f);
            PlaceTop(leaf.rectTransform, new Vector2(1160, 40), new Vector2(48, 48));
            Text nutrients = BuildText(screen, "Nutrientes", "", TextLarge, Cream, TextAnchor.MiddleLeft);
            PlaceTop(nutrients.rectTransform, new Vector2(1224, 40), new Vector2(280, 48));
            SetText(so, "_nutrients", nutrients);
            Button back = BuildKitButton(screen, "Voltar", "Buttons/button_back", new Vector2(1920 - 70 - 270, 34), new Vector2(270, 64),
                                         "[ESC] VOLTAR", Ink, TextSmall, out _);
            SetObject(so, "_backButton", back);
            Image rule = BuildPlain(screen, "Linha", new Color(0.45f, 0.36f, 0.26f, 0.9f));
            PlaceTop(rule.rectTransform, new Vector2(70, 118), new Vector2(1780, 2));

            // --- abas
            Button hiveTab = BuildKitButton(screen, "AbaFormigueiro", "Tabs/tab_inactive", new Vector2(70, 140), new Vector2(340, 60),
                                            "FORMIGUEIRO", Ink, TextMedium, out Image hiveTabImage);
            Button queenTab = BuildKitButton(screen, "AbaRainha", "Tabs/tab_active", new Vector2(430, 140), new Vector2(440, 60),
                                             "RAINHA E CLASSES", Ink, TextMedium, out Image queenTabImage);
            SetObject(so, "_hiveTab", hiveTab);
            SetObject(so, "_hiveTabImage", hiveTabImage);
            SetObject(so, "_queenTab", queenTab);
            SetObject(so, "_queenTabImage", queenTabImage);
            SetObject(so, "_tabActive", LoadKit("Tabs/tab_active"));
            SetObject(so, "_tabInactive", LoadKit("Tabs/tab_inactive"));

            // --- páginas
            Transform hivePage = BuildPage(screen, "PaginaFormigueiro");
            Transform queenPage = BuildPage(screen, "PaginaRainha");
            SetObject(so, "_hivePage", hivePage.gameObject);
            SetObject(so, "_queenPage", queenPage.gameObject);
            BuildQueenPage(queenPage, so);
            BuildHivePage(hivePage, so);

            // --- detalhe do nó escolhido (canto inferior direito)
            Image detail = BuildKitImage(screen, "Detalhe", "Panels/skill_details_panel", new Vector2(1, 0), new Vector2(1, 0),
                                         new Vector2(-40, 40), PixelScale);
            detail.type = Image.Type.Sliced;
            detail.pixelsPerUnitMultiplier = 1f / PixelScale;
            detail.rectTransform.pivot = new Vector2(1, 0);
            detail.rectTransform.sizeDelta = new Vector2(640, 310);
            Text nodeTitle = BuildText(detail.transform, "NoTitulo", "", TextMedium, Cream, TextAnchor.UpperLeft);
            nodeTitle.horizontalOverflow = HorizontalWrapMode.Wrap; // nomes longos quebram em duas linhas
            nodeTitle.lineSpacing = 1.4f; // a fonte tem altura de linha igual ao corpo: sem folga as linhas se tocam
            Place(nodeTitle.rectTransform, new Vector2(24, -16), new Vector2(592, 66));
            Text effect = BuildText(detail.transform, "NoEfeito", "", TextSmall, Green, TextAnchor.UpperLeft);
            effect.horizontalOverflow = HorizontalWrapMode.Wrap;
            effect.lineSpacing = 1.5f;
            Place(effect.rectTransform, new Vector2(24, -90), new Vector2(592, 66));
            Text info = BuildText(detail.transform, "NoInfo", "", TextSmall, Muted, TextAnchor.UpperLeft);
            info.horizontalOverflow = HorizontalWrapMode.Wrap;
            info.lineSpacing = 1.5f;
            Place(info.rectTransform, new Vector2(24, -162), new Vector2(592, 44));
            Button buy = BuildKitButton(detail.transform, "Comprar", "Buttons/button_unlock", new Vector2(0, 0), new Vector2(592, 60),
                                        "DESBLOQUEAR", Cream, TextMedium, out Image buyImage);
            var buyRect = (RectTransform)buy.transform;
            buyRect.anchorMin = buyRect.anchorMax = new Vector2(0.5f, 0);
            buyRect.pivot = new Vector2(0.5f, 0);
            buyRect.anchoredPosition = new Vector2(0, 24);
            SetText(so, "_title", nodeTitle);
            SetText(so, "_effect", effect);
            SetText(so, "_info", info);
            SetObject(so, "_buyButton", buy);
            SetObject(so, "_buyImage", buyImage);
            SetText(so, "_buyLabel", buy.transform.Find("Texto").GetComponent<Text>());

            BuildLegend(screen);

            SetObject(so, "_diamondAcquired", LoadKit("Nodes/diamond_acquired"));
            SetObject(so, "_diamondAvailable", LoadKit("Nodes/diamond_available"));
            SetObject(so, "_diamondLocked", LoadKit("Nodes/diamond_locked"));
            SetObject(so, "_diamondRoot", LoadKit("Nodes/diamond_root"));
            SetObject(so, "_linkAcquired", LoadKit("Connections/link_acquired"));
            SetObject(so, "_linkAvailable", LoadKit("Connections/link_available"));
            SetObject(so, "_linkLocked", LoadKit("Connections/link_locked"));
            so.ApplyModifiedPropertiesWithoutUndo();
            go.SetActive(false);
            return go;
        }

        // ---------------------------------------------------------------- árvore da rainha

        // Colunas (uma por ramo) e linhas (profundidade na compra) da árvore da rainha, em coordenadas da tela 1920x1080.
        const float QueenColumn0 = 155f, QueenColumnStep = 230f;
        static readonly float[] QueenRows = { 400f, 525f, 650f };
        const float QueenRootY = 245f, QueenTrunkY = 320f;

        /// <summary>Posição de cada nó da rainha: (coluna do ramo, profundidade, deslocamento horizontal).</summary>
        static (int depth, float dx) QueenSlot(QueenNode node)
        {
            switch (node)
            {
                case QueenNode.Q1: case QueenNode.O2: case QueenNode.F1: case QueenNode.E1: case QueenNode.J1: case QueenNode.N1: return (1, 0);
                case QueenNode.Q2: case QueenNode.E2: case QueenNode.J2: case QueenNode.N2: case QueenNode.S3: return (2, 0);
                case QueenNode.O1: return (1, -78);
                case QueenNode.O3: return (1, 78);
                case QueenNode.L1: case QueenNode.S1: return (1, -45);
                case QueenNode.L3: case QueenNode.S2: return (1, 45);
                case QueenNode.L2: return (2, -45);
                default: return (0, 0);
            }
        }

        static Vector2 QueenPosition(QueenNode node)
        {
            QueenNodeDef def = QueenTree.Def(node);
            (int depth, float dx) = QueenSlot(node);
            return new Vector2(QueenColumn0 + QueenColumnStep * (int)def.Branch + dx, QueenRows[depth]);
        }

        static string QueenIcon(QueenBranch branch)
        {
            switch (branch)
            {
                case QueenBranch.Queen: return "icons/egg";
                case QueenBranch.Common: return "icons/ant";
                case QueenBranch.Carrier: return "icons/crate";
                case QueenBranch.Cutter: return "icons/leaf";
                case QueenBranch.Digger: return "icons/shovel";
                case QueenBranch.Gardener: return "icons/mushroom";
                case QueenBranch.Nurse: return "icons/larva";
                default: return "icons/warning";
            }
        }

        static void BuildQueenPage(Transform page, SerializedObject screen)
        {
            var links = new System.Collections.Generic.List<(Image image, int parent, int child)>();
            Sprite linkLocked = LoadKit("Connections/link_locked");

            // Tronco: da raiz desce ao tronco horizontal e sobe um toco até o primeiro nó de cada ramo. Os ramos mais
            // distantes são desenhados antes, para o trecho dos mais perto ficar por cima na própria cor.
            var rootPos = new Vector2(960, QueenRootY);
            links.Add((BuildLink(page, rootPos, new Vector2(960, QueenTrunkY), linkLocked), -1, (int)QueenNode.Q0));
            var order = new System.Collections.Generic.List<int>();
            for (int b = 0; b < QueenTree.BranchCount; b++)
                order.Add(b);
            order.Sort((x, y) => Mathf.Abs(QueenColumn0 + QueenColumnStep * y - 960f).CompareTo(Mathf.Abs(QueenColumn0 + QueenColumnStep * x - 960f)));
            foreach (int b in order)
            {
                QueenNodeDef first = QueenTree.NodesOf((QueenBranch)b)[0];
                float x = QueenColumn0 + QueenColumnStep * b;
                links.Add((BuildLink(page, new Vector2(960, QueenTrunkY), new Vector2(x, QueenTrunkY), linkLocked), -1, (int)first.Node));
                links.Add((BuildLink(page, new Vector2(x, QueenTrunkY), QueenPosition(first.Node), linkLocked), -1, (int)first.Node));
            }
            foreach (QueenNodeDef def in QueenTree.All)
            {
                foreach (QueenNode parent in def.Requires)
                    links.Add((BuildLink(page, QueenPosition(parent), QueenPosition(def.Node), linkLocked), (int)parent, (int)def.Node));
            }

            for (int b = 0; b < QueenTree.BranchCount; b++)
            {
                Text label = BuildText(page, "Ramo" + b, QueenTree.BranchLabel((QueenBranch)b), TextSmall, Green, TextAnchor.MiddleCenter);
                PlaceCentered(label.rectTransform, new Vector2(QueenColumn0 + QueenColumnStep * b, QueenTrunkY - 26), new Vector2(228, 24));
            }

            // Raiz (a rainha) e os 26 nós, na ordem do enum.
            BuildNode(page, "Raiz", "RAINHA", rootPos, "icons/crown", null, 40, true, out _);
            var widgets = new SkillNodeWidget[QueenTree.All.Count];
            foreach (QueenNodeDef def in QueenTree.All)
                widgets[(int)def.Node] = BuildNode(page, def.Id, def.Id, QueenPosition(def.Node), QueenIcon(def.Branch), null, 40, false, out _);
            SetArray(screen, "_queenNodes", widgets);
            WriteLinks(screen, "_queenLinks", links);
        }

        // ---------------------------------------------------------------- árvore do formigueiro (só exibição)

        static void BuildHivePage(Transform page, SerializedObject screen)
        {
            var links = new System.Collections.Generic.List<(Image image, int parent, int child)>();
            Sprite linkLocked = LoadKit("Connections/link_locked");
            var hub = new Vector2(960, 590);
            for (int i = 0; i < HiveTreeInfo.All.Count; i++)
            {
                HiveTreeInfo.Node node = HiveTreeInfo.All[i];
                foreach (int parent in node.Parents)
                {
                    Vector2 from = parent < 0 ? hub : new Vector2(HiveTreeInfo.All[parent].X, HiveTreeInfo.All[parent].Y);
                    links.Add((BuildLink(page, from, new Vector2(node.X, node.Y), linkLocked), parent, i));
                }
            }
            foreach ((string label, float x, float y) in HiveTreeInfo.Branches)
            {
                Text text = BuildText(page, "Ramo" + label, label, TextMedium, Green, TextAnchor.MiddleCenter);
                PlaceCentered(text.rectTransform, new Vector2(x, y), new Vector2(240, 38));
            }
            Text hubLabel = BuildText(page, "RotuloColonia", "COLÔNIA", TextMedium, Muted, TextAnchor.MiddleCenter);
            PlaceCentered(hubLabel.rectTransform, new Vector2(960, 590 + 100), new Vector2(240, 32));

            BuildNode(page, "Centro", null, hub, "Icons/skill_HUB_large", null, 64, true, out _, kitIcon: true);
            var widgets = new SkillNodeWidget[HiveTreeInfo.All.Count];
            for (int i = 0; i < widgets.Length; i++)
            {
                HiveTreeInfo.Node node = HiveTreeInfo.All[i];
                widgets[i] = BuildNode(page, node.Id, node.Id, new Vector2(node.X, node.Y), $"Icons/skill_{node.Id}",
                                       $"IconsLocked/skill_{node.Id}_locked", 48, false, out _, kitIcon: true);
            }
            SetArray(screen, "_hiveNodes", widgets);
            WriteLinks(screen, "_hiveLinks", links);
        }

        // ---------------------------------------------------------------- peças

        /// <summary>
        /// Nó em camadas (losango, ícone, cadeado, anel de seleção, rótulo) com o botão num filho transparente de 76x76.
        /// <paramref name="position"/> está no sistema da tela (origem no canto superior esquerdo, y para baixo).
        /// </summary>
        static SkillNodeWidget BuildNode(Transform parent, string name, string label, Vector2 position, string icon, string iconLocked,
                                         float iconSize, bool isRoot, out Button button, bool kitIcon = false)
        {
            var go = new GameObject("No" + name, typeof(RectTransform), typeof(SkillNodeWidget));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            PlaceCentered(rect, position, new Vector2(56 * PixelScale, 56 * PixelScale));

            Image diamond = BuildKitImage(go.transform, "Losango", "Nodes/diamond_locked", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                          Vector2.zero, PixelScale);
            diamond.rectTransform.sizeDelta = rect.sizeDelta;
            Image iconImage = kitIcon
                ? BuildKitImage(go.transform, "Icone", icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, 1f)
                : BuildSprite(go.transform, "Icone", icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one);
            iconImage.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            Image padlock = BuildKitImage(go.transform, "Cadeado", "UI/icon_lock_small", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                          new Vector2(34, -26), PixelScale);
            padlock.enabled = !isRoot; // o centro nunca é bloqueado
            Image ring = BuildKitImage(go.transform, "Selecao", "Effects/selection_ring", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                       Vector2.zero, PixelScale);
            ring.rectTransform.sizeDelta = rect.sizeDelta;
            ring.enabled = false;
            Text text = null;
            if (!string.IsNullOrEmpty(label))
            {
                // O centro leva o rótulo ao lado; os demais nós, embaixo.
                text = BuildText(go.transform, "Rotulo", label, TextMedium, isRoot ? Muted : Cream,
                                 isRoot ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
                PlaceCentered(text.rectTransform, isRoot ? new Vector2(170, 0) : new Vector2(0, -58), new Vector2(isRoot ? 200 : 160, 32), relative: true);
                text.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            }

            var hit = new GameObject("Clique", typeof(RectTransform), typeof(Image), typeof(Button));
            hit.layer = LayerMask.NameToLayer("UI");
            hit.transform.SetParent(go.transform, false);
            var hitRect = (RectTransform)hit.transform;
            hitRect.anchorMin = hitRect.anchorMax = hitRect.pivot = new Vector2(0.5f, 0.5f);
            hitRect.anchoredPosition = Vector2.zero;
            hitRect.sizeDelta = new Vector2(76, 76);
            var hitImage = hit.GetComponent<Image>();
            hitImage.color = new Color(1, 1, 1, 0);
            button = hit.GetComponent<Button>();
            button.targetGraphic = hitImage;
            button.transition = Selectable.Transition.None;

            var widget = go.GetComponent<SkillNodeWidget>();
            var so = new SerializedObject(widget);
            SetObject(so, "_button", button);
            SetObject(so, "_diamond", diamond);
            SetObject(so, "_icon", iconImage);
            SetObject(so, "_ring", ring);
            SetObject(so, "_lock", padlock);
            SetObject(so, "_label", text);
            SetObject(so, "_iconNormal", iconImage.sprite);
            SetObject(so, "_iconLocked", iconLocked == null ? null : LoadKit(iconLocked));
            so.FindProperty("_root").boolValue = isRoot;
            so.ApplyModifiedPropertiesWithoutUndo();
            return widget;
        }

        /// <summary>Segmento entre dois pontos da tela (centro a centro): o sprite de 20x6 repetido, em escala 2.</summary>
        static Image BuildLink(Transform parent, Vector2 from, Vector2 to, Sprite sprite)
        {
            var go = new GameObject("Ligacao", typeof(RectTransform), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Vector2 mid = (from + to) * 0.5f;
            rect.anchoredPosition = new Vector2(mid.x, -mid.y);
            Vector2 delta = new Vector2(to.x - from.x, -(to.y - from.y));
            rect.sizeDelta = new Vector2(delta.magnitude, 6 * PixelScale);
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Tiled;
            image.pixelsPerUnitMultiplier = 1f / PixelScale;
            image.raycastTarget = false;
            return image;
        }

        static void WriteLinks(SerializedObject so, string field, System.Collections.Generic.List<(Image image, int parent, int child)> links)
        {
            SerializedProperty list = so.FindProperty(field);
            list.arraySize = links.Count;
            for (int i = 0; i < links.Count; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Image").objectReferenceValue = links[i].image;
                element.FindPropertyRelative("Parent").intValue = links[i].parent;
                element.FindPropertyRelative("Child").intValue = links[i].child;
            }
        }

        /// <summary>Legenda de estados (rodapé): adquirido, disponível, selecionado e bloqueado.</summary>
        static void BuildLegend(Transform screen)
        {
            string[] sprites = { "Nodes/diamond_acquired", "Nodes/diamond_available", "Nodes/diamond_selected", "Nodes/diamond_locked" };
            string[] labels = { "ADQUIRIDO", "DISPONÍVEL", "SELECIONADO", "BLOQUEADO" };
            for (int i = 0; i < sprites.Length; i++)
            {
                float x = 80 + i * 260;
                Image icon = BuildKitImage(screen, "Legenda" + i, sprites[i], new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, 1f);
                PlaceCentered(icon.rectTransform, new Vector2(x, 1030), new Vector2(40, 40));
                Text text = BuildText(screen, "LegendaTexto" + i, labels[i], TextSmall, Green, TextAnchor.MiddleLeft);
                PlaceCentered(text.rectTransform, new Vector2(x + 40 + 90, 1030), new Vector2(180, 30));
            }
        }

        static Transform BuildPage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform);
            return go.transform;
        }

        static Image BuildKitImage(Transform parent, string name, string kitKey, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            var image = go.GetComponent<Image>();
            image.sprite = LoadKit(kitKey);
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            rect.sizeDelta = image.sprite != null ? image.sprite.rect.size * scale : Vector2.zero;
            return image;
        }

        static Image BuildPlain(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Botão de 9-slice do kit (borda em escala 2), com o texto por cima, posicionado pelo canto superior esquerdo da tela.</summary>
        static Button BuildKitButton(Transform parent, string name, string kitKey, Vector2 position, Vector2 size, string label,
                                     Color textColor, int fontSize, out Image image)
        {
            image = BuildKitImage(parent, name, kitKey, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, 1f);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f / PixelScale;
            image.raycastTarget = true;
            var rect = image.rectTransform;
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            Text text = BuildText(image.transform, "Texto", label, fontSize, textColor, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            if (textColor == Cream)
                text.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);
            return button;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Posiciona pelo canto superior esquerdo da tela (y para baixo).</summary>
        static void PlaceTop(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
        }

        /// <summary>
        /// Centraliza no ponto dado, no sistema da tela (y para baixo); com <paramref name="relative"/>, o ponto é um
        /// deslocamento do centro do pai (y para cima, como o anchoredPosition).
        /// </summary>
        static void PlaceCentered(RectTransform rect, Vector2 position, Vector2 size, bool relative = false)
        {
            if (relative)
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = position;
            }
            else
            {
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(position.x, -position.y);
            }
            rect.sizeDelta = size;
        }

        static void BuildCard(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, string icon, string title,
                              out Text value, out Text detail)
        {
            Image card = BuildSprite(parent, name, "panels/hud_card", anchor, anchor, position, size);
            Image iconImage = BuildSprite(card.transform, "Icone", icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                                          new Vector2(22, 0), new Vector2(44, 48));
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            Text header = BuildText(card.transform, "Titulo", title, TextSmall, Ink, TextAnchor.UpperLeft);
            Place(header.rectTransform, new Vector2(78, -16), new Vector2(size.x - 92, 20));
            value = BuildText(card.transform, "Valor", "", TextMedium, Ink, TextAnchor.UpperLeft);
            Place(value.rectTransform, new Vector2(78, -40), new Vector2(size.x - 92, 28));
            detail = BuildText(card.transform, "Detalhe", "", TextSmall, Ink, TextAnchor.UpperLeft);
            Place(detail.rectTransform, new Vector2(78, -74), new Vector2(size.x - 92, 20));
        }

        static Image BuildPanelWithHeader(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, string title)
        {
            Image panel = BuildSprite(parent, name, "panels/panel_header", anchor, anchor, position, size);
            Text header = BuildText(panel.transform, "Titulo", title, TextSmall, Ink, TextAnchor.MiddleCenter);
            // A barra de madeira do sprite é fina (15 px): o título fica logo abaixo dela, no papel.
            var rect = header.rectTransform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.offsetMin = new Vector2(8, -42);
            rect.offsetMax = new Vector2(-8, -18);
            return panel;
        }

        static Button BuildButton(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, string icon, string label,
                                  bool interactable, out Image image)
        {
            image = BuildSprite(parent, name, "buttons/button_normal", anchor, anchor, position, size);
            image.rectTransform.pivot = new Vector2(0.5f, 1);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Load("buttons/button_pressed"), disabledSprite = Load("buttons/button_disabled") };
            button.interactable = interactable;

            Image iconImage = BuildSprite(image.transform, "Icone", icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                                          new Vector2(12, 0), new Vector2(28, 28));
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.color = interactable ? Color.white : new Color(1, 1, 1, 0.45f);
            Text text = BuildText(image.transform, "Texto", label, TextSmall, interactable ? Ink : InkDisabled, TextAnchor.MiddleLeft);
            var rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(50, 0);
            rect.offsetMax = new Vector2(-8, 0);
            return button;
        }

        static Button BuildIconButton(Transform parent, string name, int slot, string icon, string label, out Image image)
        {
            image = BuildSprite(parent, name, "buttons/icon_button", new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                                new Vector2(14 + slot * 64, 0), new Vector2(56, 52));
            image.rectTransform.pivot = new Vector2(0, 0.5f);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            if (icon != null)
            {
                Image iconImage = BuildSprite(image.transform, "Icone", icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                              Vector2.zero, new Vector2(24, 28));
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
            }
            else
            {
                Text text = BuildText(image.transform, "Texto", label, TextMedium, Ink, TextAnchor.MiddleCenter);
                var rect = text.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            return button;
        }

        // ---------------------------------------------------------------- primitivas

        static Image BuildSprite(Transform parent, string name, string spriteKey, Vector2 anchorMin, Vector2 anchorMax,
                                 Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x, anchorMin.y);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.sprite = Load(spriteKey);
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            return image;
        }

        /// <summary>Ajusta um tamanho pedido ao múltiplo de 8 em uso: até 20 vira 16, até 28 vira 24, até 40 vira 32, acima disso 48.</summary>
        static int SnapTextSize(int size) => size <= 20 ? TextSmall : size <= 28 ? TextMedium : size <= 40 ? TextLarge : TextTitle;

        static Text BuildText(Transform parent, string name, string content, int size, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            size = SnapTextSize(size);
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal; // o peso vem do arquivo (Bold ou Regular), sem negrito sintético
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = content;
            // Rótulo estático: o texto vem da tabela de strings e acompanha o idioma (vazio = preenchido por código).
            LocalizationSetup.Bind(text, content);
            return text;
        }

        /// <summary>Posiciona um filho pelo canto superior esquerdo do pai.</summary>
        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static Sprite Load(string key)
        {
            string path = $"{Sprites}/{key}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                Debug.LogError($"[AntFarmHudSetup] Sprite ausente: {path}");
            return sprite;
        }

        static void EnsureEventSystem(Scene scene)
        {
            EventSystem existing = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EventSystem>(true)).FirstOrDefault();
            GameObject go;
            if (existing != null)
                go = existing.gameObject;
            else
            {
                go = new GameObject("EventSystem", typeof(EventSystem));
                SceneManager.MoveGameObjectToScene(go, scene);
            }
            // Input System ativo: o módulo antigo (StandaloneInputModule) não funciona.
            foreach (StandaloneInputModule old in go.GetComponents<StandaloneInputModule>())
                Object.DestroyImmediate(old);
            if (!go.TryGetComponent(out InputSystemUIInputModule _))
                go.AddComponent<InputSystemUIInputModule>();
        }

        static void SetText(SerializedObject o, string field, Text value) => SetObject(o, field, value);

        static void SetObject(SerializedObject o, string field, Object value) =>
            o.FindProperty(field).objectReferenceValue = value;

        static void SetArray<T>(SerializedObject o, string field, T[] values) where T : Object
        {
            SerializedProperty list = o.FindProperty(field);
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
