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
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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
            BuildCard(root, "CardNutrientes", new Vector2(0, 1), new Vector2(16, -16), new Vector2(300, 100),
                      "icons/leaf", "NUTRIENTES", out Text nv, out Text nd);
            BuildCard(root, "CardFormigas", new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(380, 100),
                      "icons/ant", "FORMIGAS", out Text av, out Text ad);
            BuildCard(root, "CardRainha", new Vector2(1, 1), new Vector2(-16, -16), new Vector2(300, 100),
                      "icons/crown", "RAINHA", out Text qv, out Text qd);
            SetText(refs, "_nutrientsValue", nv); SetText(refs, "_nutrientsDetail", nd);
            SetText(refs, "_antsValue", av); SetText(refs, "_antsDetail", ad);
            SetText(refs, "_queenValue", qv); SetText(refs, "_queenDetail", qd);

            // --- menu Colônia (esquerda)
            Image menu = BuildPanelWithHeader(root, "MenuColonia", new Vector2(0, 1), new Vector2(16, -214), new Vector2(220, 316), "COLÔNIA");
            string[] entries = { "PINTAR", "EVOLUÇÃO", "RAINHA", "AMBIENTES", "AVISOS" };
            string[] entryIcons = { "icons/shovel", "icons/leaf", "icons/crown", "icons/mushroom", "icons/warning" };
            for (int i = 0; i < entries.Length; i++)
            {
                // Só Pintar e Rainha existem no jogo; os demais ficam desativados até as telas deles serem feitas.
                bool exists = i == 0 || i == 2;
                Button b = BuildButton(menu.transform, entries[i], new Vector2(0.5f, 1), new Vector2(0, -56 - i * 50),
                                       new Vector2(188, 42), entryIcons[i], entries[i], interactable: exists, out Image image);
                if (i == 0)
                {
                    SetObject(refs, "_paintButton", b);
                    SetObject(refs, "_paintButtonImage", image);
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
            Image paint = BuildPanelWithHeader(root, "SubmenuPintar", new Vector2(0, 1), new Vector2(246, -214),
                                               new Vector2(320, 62 + brushCount * 50 + 12), "PINTAR");
            var brushButtons = new Button[brushCount];
            var brushImages = new Image[brushCount];
            for (int i = 0; i < brushCount; i++)
                brushButtons[i] = BuildButton(paint.transform, "Pincel" + i, new Vector2(0.5f, 1), new Vector2(0, -56 - i * 50),
                                              new Vector2(288, 42), brushIcons[i % brushIcons.Length],
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
            Text hint = BuildText(root, "Dica", "", 18, Cream, TextAnchor.MiddleCenter);
            var hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0, 0);
            hintRect.anchorMax = new Vector2(1, 0);
            hintRect.pivot = new Vector2(0.5f, 0);
            hintRect.offsetMin = new Vector2(320, 28);
            hintRect.offsetMax = new Vector2(-320, 60);
            hint.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1, -1);
            SetText(refs, "_hint", hint);

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
            Image panel = BuildPanelWithHeader(root, "SubmenuRainha", new Vector2(0, 1), new Vector2(246, -214),
                                               new Vector2(320, 62 + 3 * 50 + 16 + AntFarmHud.CreatableRoles.Length * 50 + 4 * 24 + 20), "RAINHA");
            QueenMode[] modes = { QueenMode.Automatic, QueenMode.Specific, QueenMode.Paused };
            string[] modeIcons = { "icons/gear", "icons/ant", "icons/pause" };
            var modeButtons = new Button[modes.Length];
            var modeImages = new Image[modes.Length];
            for (int i = 0; i < modes.Length; i++)
                modeButtons[i] = BuildButton(panel.transform, "Modo" + i, new Vector2(0.5f, 1), new Vector2(0, -56 - i * 50),
                                             new Vector2(288, 42), modeIcons[i], AntFarmHud.ModeLabel(modes[i]), true, out modeImages[i]);

            var typeButtons = new Button[AntFarmHud.CreatableRoles.Length];
            var typeImages = new Image[typeButtons.Length];
            var typeLabels = new Text[typeButtons.Length];
            int typesTop = 56 + modes.Length * 50 + 16;
            for (int i = 0; i < typeButtons.Length; i++)
            {
                typeButtons[i] = BuildButton(panel.transform, "Tipo" + i, new Vector2(0.5f, 1), new Vector2(0, -typesTop - i * 50),
                                             new Vector2(288, 42), "icons/ant", AntFarmHud.RoleLabel(AntFarmHud.CreatableRoles[i]),
                                             true, out typeImages[i]);
                typeImages[i].transform.Find("Icone").GetComponent<Image>().color = AntSwarmView.RoleTint(AntFarmHud.CreatableRoles[i]);
                typeLabels[i] = typeButtons[i].transform.Find("Texto").GetComponent<Text>();
                typeLabels[i].fontSize = 16;
            }

            int infoTop = typesTop + typeButtons.Length * 50 + 4;
            Text state = BuildText(panel.transform, "Estado", "", 15, Ink, TextAnchor.MiddleCenter);
            Place(state.rectTransform, new Vector2(16, -infoTop), new Vector2(288, 24));
            Text next = BuildText(panel.transform, "Proximo", "", 15, Ink, TextAnchor.MiddleCenter);
            Place(next.rectTransform, new Vector2(16, -infoTop - 24), new Vector2(288, 24));
            Text brood = BuildText(panel.transform, "Crias", "", 15, Ink, TextAnchor.MiddleCenter);
            Place(brood.rectTransform, new Vector2(16, -infoTop - 48), new Vector2(288, 24));
            Text space = BuildText(panel.transform, "Espaco", "", 15, Ink, TextAnchor.MiddleCenter);
            Place(space.rectTransform, new Vector2(16, -infoTop - 72), new Vector2(288, 24));

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

        static void BuildCard(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, string icon, string title,
                              out Text value, out Text detail)
        {
            Image card = BuildSprite(parent, name, "panels/hud_card", anchor, anchor, position, size);
            Image iconImage = BuildSprite(card.transform, "Icone", icon, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                                          new Vector2(22, 0), new Vector2(44, 48));
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            Text header = BuildText(card.transform, "Titulo", title, 18, Ink, TextAnchor.UpperLeft);
            Place(header.rectTransform, new Vector2(78, -14), new Vector2(size.x - 92, 22));
            value = BuildText(card.transform, "Valor", "", 24, Ink, TextAnchor.UpperLeft);
            Place(value.rectTransform, new Vector2(78, -36), new Vector2(size.x - 92, 28));
            detail = BuildText(card.transform, "Detalhe", "", 16, Ink, TextAnchor.UpperLeft);
            Place(detail.rectTransform, new Vector2(78, -66), new Vector2(size.x - 92, 20));
        }

        static Image BuildPanelWithHeader(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, string title)
        {
            Image panel = BuildSprite(parent, name, "panels/panel_header", anchor, anchor, position, size);
            Text header = BuildText(panel.transform, "Titulo", title, 18, Ink, TextAnchor.MiddleCenter);
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
            Text text = BuildText(image.transform, "Texto", label, 18, interactable ? Ink : InkDisabled, TextAnchor.MiddleLeft);
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
                Text text = BuildText(image.transform, "Texto", label, 20, Ink, TextAnchor.MiddleCenter);
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

        static Text BuildText(Transform parent, string name, string content, int size, Color color, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
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
