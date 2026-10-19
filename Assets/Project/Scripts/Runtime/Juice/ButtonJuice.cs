using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fabula.Incrementant.Juice
{
    /// <summary>
    /// Animação de um <see cref="Button"/>/<see cref="Toggle"/>: flutuar em repouso (idle, todos dessincronizados),
    /// realce no hover, afundar ao pressionar, retorno elástico ao soltar e tremidinha + cor de erro quando a ação é negada.
    /// Mexe em escala e deslocamento do próprio RectTransform (ortogonal ao <c>SpriteSwap</c> e ao destaque por código do
    /// HUD); a cor de erro passa pela cor de vértice do gráfico alvo e volta ao valor exato. O deslocamento é aditivo
    /// (aplica só a diferença do frame anterior), então convive com layouts que reposicionam o botão.
    /// Respeita <see cref="Vfx"/>: Completo = tudo; Reduzido = leve afundar e cor de erro; Desligado = nada. Tempo não escalado.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public sealed class ButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
                                      IPointerDownHandler, IPointerUpHandler
    {
        const float HoverScale = 1.06f;
        const float PressX = 1.05f;
        const float PressY = 0.94f;
        const float ReducedPress = 0.97f;
        const float ShakeSeconds = 0.18f;
        const float ShakePixels = 3f;
        const int ShakeCycles = 3;
        const float DenyColorSeconds = 0.15f;
        const float DenyColorAmount = 0.65f;
        // Todos os botões flutuam, cada um com fase e período próprios (decisão do desenvolvedor em 2026-10-19: um botão
        // por vez ficou ruim). Valores provisórios.
        const float IdlePixels = 2f;
        const float IdlePeriodMin = 1.2f;
        const float IdlePeriodMax = 2.0f;
        const float IdleFade = 0.25f;

        [SerializeField, Tooltip("Flutuar leve enquanto o botão está parado e disponível.")]
        bool _idle = true;

        Selectable _selectable;
        RectTransform _rect;
        Vector3 _restScale = Vector3.one;
        Vector2 _from = Vector2.one;
        Vector2 _to = Vector2.one;
        float _t = 1f;
        float _duration = 0.1f;
        Easing.Fn _curve = Easing.OutQuad;
        bool _hover;
        bool _pressed;
        float _shakeT = 1f;
        float _denyColorT = 1f;
        Color _denyRestColor;
        Color _denyApplied;   // última cor escrita pela cor de erro (detecta troca feita por outro código)
        float _idleWeight;
        float _idlePhase;
        float _idlePeriod = 1.6f;
        Vector2 _applied; // deslocamento que este componente soma à posição do botão

        void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _rect = (RectTransform)transform;
            _restScale = _rect.localScale;
            // Fase e período por botão, para não flutuarem em uníssono. Os ids de instância são quase sequenciais,
            // então embaralha com a razão áurea (fração de n·φ) em vez de usar o id direto.
            int id = Mathf.Abs(GetInstanceID());
            _idlePhase = Fraction(id * 0.6180339887f);
            _idlePeriod = Mathf.Lerp(IdlePeriodMin, IdlePeriodMax, Fraction(id * 0.7548776662f + 0.3f));
        }

        static float Fraction(float v) => v - Mathf.Floor(v);

        void OnDisable() => Snap();

        public void OnPointerEnter(PointerEventData e)
        {
            _hover = true;
            if (CanReact && Vfx.Strong)
                Go(HoverScale, HoverScale, 0.10f, Easing.OutQuad);
        }

        public void OnPointerExit(PointerEventData e)
        {
            _hover = false;
            if (!_pressed && Vfx.Motion)
                Go(1f, 1f, 0.12f, Easing.OutQuad);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || !CanReact)
                return;
            _pressed = true;
            if (Vfx.Strong)
                Go(PressX, PressY, 0.06f, Easing.OutQuad);
            else
                Go(ReducedPress, ReducedPress, 0.06f, Easing.OutQuad);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_pressed)
                return;
            _pressed = false;
            if (!Vfx.Motion)
                return;
            float rest = _hover && Vfx.Strong ? HoverScale : 1f;
            Go(rest, rest, 0.22f, Easing.OutBack);
        }

        /// <summary>
        /// Ação negada (sem custo, bloqueada): cor de erro curta (Completo e Reduzido) e tremidinha horizontal (só Completo).
        /// Par visual do <c>SfxId.UiDeny</c>: a recusa não depende só do som.
        /// </summary>
        public void Deny()
        {
            if (!Vfx.Motion || !isActiveAndEnabled)
                return;
            if (Vfx.Strong)
                _shakeT = 0f;
            Graphic g = _selectable.targetGraphic;
            if (g == null)
                return;
            if (_denyColorT >= 1f)
            {
                _denyRestColor = g.color;
                _denyApplied = g.color;
            }
            _denyColorT = 0f;
        }

        /// <summary>Tremidinha de negado no botão dado (no-op se ele não tiver <see cref="ButtonJuice"/>).</summary>
        public static void Deny(GameObject button)
        {
            if (button != null && button.TryGetComponent(out ButtonJuice juice))
                juice.Deny();
        }

        bool CanReact => Vfx.Motion && _selectable.IsInteractable();

        void Go(float x, float y, float seconds, Easing.Fn curve)
        {
            _from = new Vector2(_rect.localScale.x / _restScale.x, _rect.localScale.y / _restScale.y);
            _to = new Vector2(x, y);
            _duration = seconds;
            _t = 0f;
            _curve = curve;
        }

        void Update()
        {
            if (!Vfx.Motion)
            {
                // Inclui a escala: desligar com o cursor em cima deixaria o botão preso no tamanho do hover.
                if (_t < 1f || _shakeT < 1f || _denyColorT < 1f || _applied != Vector2.zero || _rect.localScale != _restScale)
                    Snap();
                return;
            }
            float dt = Time.unscaledDeltaTime;
            if (_t < 1f)
            {
                _t = Mathf.Min(1f, _t + dt / _duration);
                Vector2 s = Vector2.LerpUnclamped(_from, _to, _curve(_t));
                _rect.localScale = new Vector3(_restScale.x * s.x, _restScale.y * s.y, _restScale.z);
            }

            Vector2 offset = Vector2.zero;
            if (_shakeT < 1f)
            {
                _shakeT = Mathf.Min(1f, _shakeT + dt / ShakeSeconds);
                if (_shakeT < 1f)
                    offset.x = Mathf.Sin(_shakeT * ShakeCycles * 2f * Mathf.PI) * ShakePixels * (1f - _shakeT);
            }

            if (_denyColorT < 1f)
            {
                _denyColorT = Mathf.Min(1f, _denyColorT + dt / DenyColorSeconds);
                SetDenyColor(_denyColorT >= 1f
                    ? _denyRestColor
                    : Color.Lerp(_denyRestColor, Pulse.Loss, Mathf.Sin(_denyColorT * Mathf.PI) * DenyColorAmount));
            }

            // Flutuar: só no Completo, com o botão parado e disponível; some suavemente no hover/clique/desabilitado.
            bool floating = _idle && Vfx.Strong && !_hover && !_pressed && _selectable.IsInteractable();
            _idleWeight = Mathf.MoveTowards(_idleWeight, floating ? 1f : 0f, dt / IdleFade);
            if (_idleWeight > 0f)
            {
                float wave = Mathf.Sin((Time.unscaledTime / _idlePeriod + _idlePhase) * 2f * Mathf.PI);
                // Em passos de 1 px: movimento "de pixel art", sem subpixel borrado.
                offset.y = Mathf.Round(wave * IdlePixels * _idleWeight);
            }

            ApplyOffset(offset);
        }

        // Se outro código trocou a cor no meio do efeito (o HUD pinta alguns botões), desiste sem sobrescrever.
        void SetDenyColor(Color c)
        {
            Graphic g = _selectable.targetGraphic;
            if (g == null || g.color != _denyApplied)
            {
                _denyColorT = 1f;
                return;
            }
            g.color = c;
            _denyApplied = c;
        }

        void ApplyOffset(Vector2 offset)
        {
            if (offset == _applied)
                return;
            _rect.anchoredPosition += offset - _applied;
            _applied = offset;
        }

        // Nunca deixa o botão preso em escala/posição de meio de animação.
        void Snap()
        {
            if (_rect == null)
                return;
            _rect.localScale = _restScale;
            ApplyOffset(Vector2.zero);
            if (_denyColorT < 1f)
            {
                Graphic g = _selectable != null ? _selectable.targetGraphic : null;
                if (g != null && g.color == _denyApplied)
                    g.color = _denyRestColor;
            }
            _t = 1f;
            _shakeT = 1f;
            _denyColorT = 1f;
            _idleWeight = 0f;
            _hover = false;
            _pressed = false;
        }

        /// <summary>Põe o componente em todos os botões e toggles de uma tela (inclusive desativados), uma vez só.</summary>
        public static void AttachAll(Transform root)
        {
            foreach (Selectable s in root.GetComponentsInChildren<Selectable>(true))
                if ((s is Button || s is Toggle) && s.GetComponent<ButtonJuice>() == null)
                    s.gameObject.AddComponent<ButtonJuice>();
        }
    }
}
