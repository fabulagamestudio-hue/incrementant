using UnityEngine;

namespace Fabula.Incrementant.Juice
{
    /// <summary>
    /// Animação de entrada de um painel: toda vez que o objeto é ativado (<c>SetActive(true)</c>) ele aparece com fade
    /// (e, no estilo <see cref="PanelStyle.Pop"/>, sobe 8 px e cresce de 0,96 a 1 com overshoot). A saída é imediata, para
    /// não atrasar o <c>activeSelf</c> em que o HUD e o ESC se baseiam. Não mexe em <c>interactable</c>.
    /// Completo = fade + movimento; Reduzido = só fade; Desligado = nada. Tempo não escalado.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PanelAnimator : MonoBehaviour
    {
        public enum PanelStyle
        {
            /// <summary>Só fade: telas cheias e fundos escuros, que não podem se deslocar/escalar.</summary>
            Fade,
            /// <summary>Fade + subida de 8 px + escala 0,96→1: submenus, alertas e caixas.</summary>
            Pop,
        }

        const float FadeSeconds = 0.14f;
        const float PopSeconds = 0.20f;
        const float SlidePixels = -8f;
        const float StartScale = 0.96f;

        PanelStyle _style;
        CanvasGroup _group;
        RectTransform _rect;
        Vector2 _restPosition;
        Vector3 _restScale = Vector3.one;
        float _t = 1f;
        bool _animating;

        /// <summary>Liga a animação de entrada ao painel (idempotente). Chame com o painel ainda desativado.</summary>
        public static PanelAnimator Attach(GameObject panel, PanelStyle style)
        {
            if (!panel.TryGetComponent(out PanelAnimator animator))
                animator = panel.AddComponent<PanelAnimator>();
            animator._style = style;
            return animator;
        }

        void Awake()
        {
            _rect = (RectTransform)transform;
            if (!TryGetComponent(out _group))
                _group = gameObject.AddComponent<CanvasGroup>();
        }

        void OnEnable()
        {
            if (_group == null || !Vfx.Motion)
                return;
            _restPosition = _rect.anchoredPosition;
            _restScale = _rect.localScale;
            _t = 0f;
            _animating = true;
            Apply(0f);
        }

        void OnDisable() => Finish();

        void Update()
        {
            if (!_animating)
                return;
            if (!Vfx.Motion)
            {
                Finish();
                return;
            }
            float seconds = _style == PanelStyle.Pop && Vfx.Strong ? PopSeconds : FadeSeconds;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / seconds);
            Apply(_t);
            if (_t >= 1f)
                Finish();
        }

        void Apply(float t)
        {
            _group.alpha = Easing.OutQuad(t);
            if (_style != PanelStyle.Pop || !Vfx.Strong)
                return;
            float k = Easing.OutBack(t);
            _rect.anchoredPosition = _restPosition + new Vector2(0f, SlidePixels * (1f - k));
            _rect.localScale = _restScale * Mathf.LerpUnclamped(StartScale, 1f, k);
        }

        // Estado exato de repouso: alfa 1, posição e escala originais.
        void Finish()
        {
            if (!_animating || _group == null)
                return;
            _animating = false;
            _t = 1f;
            _group.alpha = 1f;
            _rect.anchoredPosition = _restPosition;
            _rect.localScale = _restScale;
        }
    }
}
