using System.Linq;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.EditorTools.Setup;
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
    public static class AntFarmHudSetup
    {
        const string HudName = "HUD";
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

        /// <summary>Recria o HUD na cena dada (sem salvar). Chamado também pelo setup da cena.</summary>
        public static bool Build(Scene scene)
        {
            AntFarmGame game = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<AntFarmGame>(true)).FirstOrDefault();
            if (game == null)
            {
                Debug.LogError("[AntFarmHudSetup] AntFarmGame não encontrado na cena.");
                return false;
            }
            UiSpriteImporter.Apply();
            SkillTreeSpriteImporter.Apply();
            UiFontImporter.Apply();
            _font = UiFontImporter.Load();
            if (_font == null)
            {
                Debug.LogError("[AntFarmHudSetup] Fontes de Art/UI/Fontes ausentes: usando a fonte padrão da Unity.");
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

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
            string[] entries = { "PINTAR", "EVOLUÇÃO", "RAINHA", "AMBIENTES", "AVISOS" };
            string[] entryIcons = { "icons/shovel", "icons/leaf", "icons/crown", "icons/mushroom", "icons/warning" };
            for (int i = 0; i < entries.Length; i++)
            {
                // Pintar, Evolução e Rainha existem no jogo; os demais ficam desativados até as telas deles serem feitas.
                bool exists = i <= 2;
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
            }

            // --- submenu Pintar (ao lado do menu; escondido até clicar em Pintar)
            int brushCount = AntFarmGame.BrushCount;
            string[] brushIcons = { "icons/shovel", "icons/crate", "icons/crown", "icons/mushroom", "icons/egg" };
            Image paint = BuildPanelWithHeader(root, "SubmenuPintar", new Vector2(0, 1), new Vector2(286, -214),
                                               new Vector2(400, 62 + brushCount * 50 + 12), "PINTAR");
            var brushButtons = new Button[brushCount];
            var brushImages = new Image[brushCount];
            for (int i = 0; i < brushCount; i++)
                brushButtons[i] = BuildButton(paint.transform, "Pincel" + i, new Vector2(0.5f, 1), new Vector2(0, -56 - i * 50),
                                              new Vector2(368, 42), brushIcons[i % brushIcons.Length],
                                              AntFarmGame.BrushLabel(i).ToUpperInvariant(), interactable: true, out brushImages[i]);
            paint.gameObject.SetActive(false);
            SetObject(refs, "_paintPanel", paint.gameObject);
            SetArray(refs, "_brushButtons", brushButtons);
            SetArray(refs, "_brushImages", brushImages);

            BuildQueenPanel(root, refs);

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

            SetObject(refs, "_buttonIdle", Load("buttons/button_normal"));
            SetObject(refs, "_buttonActive", Load("buttons/button_focus"));
            SetObject(refs, "_iconButtonIdle", iconIdle);
            SetObject(refs, "_iconButtonActive", Load("buttons/icon_button_focus"));
            refs.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // ---------------------------------------------------------------- blocos

        /// <summary>Submenu da rainha: modo (automático, específico, pausado), tipo, o que vem a seguir e o botão de criar.</summary>
        static void BuildQueenPanel(Transform root, SerializedObject refs)
        {
            Image panel = BuildPanelWithHeader(root, "SubmenuRainha", new Vector2(0, 1), new Vector2(286, -214),
                                               new Vector2(560, 62 + 3 * 50 + 16 + AntFarmHud.CreatableRoles.Length * 50 + 4 * 24 + 20), "RAINHA");
            QueenMode[] modes = { QueenMode.Automatic, QueenMode.Specific, QueenMode.Paused };
            string[] modeIcons = { "icons/gear", "icons/ant", "icons/pause" };
            var modeButtons = new Button[modes.Length];
            var modeImages = new Image[modes.Length];
            for (int i = 0; i < modes.Length; i++)
                modeButtons[i] = BuildButton(panel.transform, "Modo" + i, new Vector2(0.5f, 1), new Vector2(0, -56 - i * 50),
                                             new Vector2(528, 42), modeIcons[i], AntFarmHud.ModeLabel(modes[i]), true, out modeImages[i]);

            var typeButtons = new Button[AntFarmHud.CreatableRoles.Length];
            var typeImages = new Image[typeButtons.Length];
            var typeLabels = new Text[typeButtons.Length];
            int typesTop = 56 + modes.Length * 50 + 16;
            for (int i = 0; i < typeButtons.Length; i++)
            {
                typeButtons[i] = BuildButton(panel.transform, "Tipo" + i, new Vector2(0.5f, 1), new Vector2(0, -typesTop - i * 50),
                                             new Vector2(528, 42), "icons/ant", AntFarmHud.RoleLabel(AntFarmHud.CreatableRoles[i]),
                                             true, out typeImages[i]);
                typeImages[i].transform.Find("Icone").GetComponent<Image>().color = AntSwarmView.RoleTint(AntFarmHud.CreatableRoles[i]);
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
            SetArray(refs, "_typeButtons", typeButtons);
            SetArray(refs, "_typeImages", typeImages);
            SetArray(refs, "_typeLabels", typeLabels);
            SetText(refs, "_queenState", state);
            SetText(refs, "_queenNext", next);
            SetText(refs, "_queenBrood", brood);
            SetText(refs, "_queenSpace", space);
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
