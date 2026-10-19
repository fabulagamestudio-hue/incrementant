using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Dica flutuante dos botões e das seções do HUD. Canvas próprio criado em código (sem mexer na cena), acima do HUD e sem
    /// alvo de raio. Quem quer dica chama <see cref="Attach"/> com uma função que devolve o texto: ela só roda enquanto o
    /// mouse está parado em cima, então pode montar texto com o estado atual da colônia sem pesar nos quadros.
    /// </summary>
    public sealed class HudTooltips
    {
        const float DelaySeconds = 0.35f;
        const float TextWidth = 560f; // 35 letras da Press Start 2P a 16 px
        const float Padding = 12f;
        const float CursorOffset = 24f;
        const int FontSize = 16;

        static readonly Color PanelColor = new Color(0.1f, 0.07f, 0.05f, 0.96f);
        static readonly Color BorderColor = new Color(0.86f, 0.74f, 0.5f, 1f);
        static readonly Color TextColor = new Color(0.97f, 0.93f, 0.82f, 1f);

        readonly GameObject _canvasObject;
        readonly RectTransform _canvasRect;
        readonly RectTransform _panel;
        readonly Text _text;
        TooltipTarget _current;
        float _hoverStart;
        bool _suppressed;

        public HudTooltips(Font font)
        {
            _canvasObject = new GameObject("HudTooltip", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            _canvasRect = (RectTransform)_canvasObject.transform;

            var panelObject = new GameObject("Painel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panelObject.transform.SetParent(_canvasObject.transform, false);
            _panel = (RectTransform)panelObject.transform;
            _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0f, 1f);
            var background = panelObject.GetComponent<Image>();
            background.color = PanelColor;
            background.raycastTarget = false;
            var outline = panelObject.GetComponent<Outline>();
            outline.effectColor = BorderColor;
            outline.effectDistance = new Vector2(2f, -2f);

            var textObject = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(_panel, false);
            _text = textObject.GetComponent<Text>();
            _text.font = font;
            _text.fontSize = FontSize;
            _text.color = TextColor;
            _text.alignment = TextAnchor.UpperLeft;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.lineSpacing = 1.4f;
            _text.raycastTarget = false;
            var textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = textRect.anchorMax = new Vector2(0f, 1f);
            textRect.pivot = new Vector2(0f, 1f);
            textRect.anchoredPosition = new Vector2(Padding, -Padding);
            textRect.sizeDelta = new Vector2(TextWidth, 0f);
            _panel.gameObject.SetActive(false);
        }

        /// <summary>Liga a dica ao objeto (ou a um pai: o evento do ponteiro sobe pela hierarquia). O alvo precisa ter um gráfico com raio ligado.</summary>
        public void Attach(Component target, Func<string> text)
        {
            if (target == null || text == null)
                return;
            TooltipTarget tip = target.GetComponent<TooltipTarget>();
            if (tip == null)
                tip = target.gameObject.AddComponent<TooltipTarget>();
            tip.Owner = this;
            tip.Text = text;
        }

        public void Attach(Component target, string text) => Attach(target, () => text);

        /// <summary>Chamado todo quadro pelo HUD: mostra, atualiza e posiciona a dica do alvo sob o mouse.</summary>
        public void Tick()
        {
            if (_current == null || _suppressed || Time.unscaledTime - _hoverStart < DelaySeconds)
            {
                Hide();
                return;
            }
            string text = _current.Text();
            Mouse mouse = Mouse.current;
            if (string.IsNullOrEmpty(text) || mouse == null)
            {
                Hide();
                return;
            }
            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)
            {
                // Clicar dispensa a dica até o mouse sair e voltar.
                _suppressed = true;
                Hide();
                return;
            }

            _text.text = text;
            float height = _text.preferredHeight;
            _text.rectTransform.sizeDelta = new Vector2(TextWidth, height);
            _panel.sizeDelta = new Vector2(TextWidth + Padding * 2f, height + Padding * 2f);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, mouse.position.ReadValue(), null, out Vector2 local);
            Rect bounds = _canvasRect.rect;
            Vector2 size = _panel.sizeDelta;
            float x = local.x + CursorOffset;
            float y = local.y - CursorOffset;
            if (x + size.x > bounds.xMax)
                x = local.x - CursorOffset - size.x;
            if (y - size.y < bounds.yMin)
                y = local.y + CursorOffset + size.y;
            x = Mathf.Clamp(x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - size.x));
            y = Mathf.Clamp(y, bounds.yMin + size.y, bounds.yMax);
            _panel.anchoredPosition = new Vector2(x, y);
            if (!_panel.gameObject.activeSelf)
                _panel.gameObject.SetActive(true);
        }

        public void Dispose()
        {
            if (_canvasObject != null)
                UnityEngine.Object.Destroy(_canvasObject);
        }

        internal void Hover(TooltipTarget target)
        {
            _current = target;
            _hoverStart = Time.unscaledTime;
            _suppressed = false;
        }

        internal void Unhover(TooltipTarget target)
        {
            if (_current != target)
                return;
            _current = null;
            Hide();
        }

        void Hide()
        {
            if (_panel != null && _panel.gameObject.activeSelf)
                _panel.gameObject.SetActive(false);
        }
    }

    /// <summary>Ponteiro sobre o objeto (ou um filho dele): avisa o <see cref="HudTooltips"/> dono. Criado por <see cref="HudTooltips.Attach"/>.</summary>
    [DisallowMultipleComponent]
    sealed class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal HudTooltips Owner;
        internal Func<string> Text;

        public void OnPointerEnter(PointerEventData eventData) => Owner?.Hover(this);

        public void OnPointerExit(PointerEventData eventData) => Owner?.Unhover(this);

        void OnDisable() => Owner?.Unhover(this);
    }
}
