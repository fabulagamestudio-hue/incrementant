using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.Juice
{
    /// <summary>
    /// Pulso de um número/ícone do HUD: escala sobe e volta a 1 exato (0,15 s) e a cor do <see cref="Graphic"/> vai até
    /// uma cor de destaque e volta (0,3 s). Reiniciar no meio parte do valor atual, sem acumular. Limita a um pulso por
    /// 0,25 s (a 8x o valor muda várias vezes por segundo). Completo = escala + cor; Reduzido = só cor; Desligado = nada.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Pulse : MonoBehaviour
    {
        public static readonly Color Gain = new Color32(232, 214, 92, 255);
        public static readonly Color Loss = new Color32(200, 72, 60, 255);

        const float ScaleSeconds = 0.15f;
        const float ColorSeconds = 0.30f;
        const float MinInterval = 0.25f;

        RectTransform _rect;
        Graphic _graphic;
        Vector3 _restScale = Vector3.one;
        Color _restColor;
        Color _accent;
        float _peak = 1f;
        float _scaleT = 1f;
        float _colorT = 1f;
        float _lastPlay = float.NegativeInfinity;

        void Awake()
        {
            _rect = (RectTransform)transform;
            _graphic = GetComponent<Graphic>();
            _restScale = _rect.localScale;
            if (_graphic != null)
                _restColor = _graphic.color;
        }

        void OnDisable() => Finish();

        /// <summary>Pulsa o objeto (adiciona o componente na primeira vez). <paramref name="peak"/> é a escala máxima.</summary>
        public static void Play(Component target, Color accent, float peak = 1.12f)
        {
            if (target == null || !Vfx.Motion)
                return;
            if (!target.TryGetComponent(out Pulse pulse))
                pulse = target.gameObject.AddComponent<Pulse>();
            pulse.Restart(accent, peak);
        }

        void Restart(Color accent, float peak)
        {
            if (!isActiveAndEnabled || Time.unscaledTime - _lastPlay < MinInterval)
                return;
            _lastPlay = Time.unscaledTime;
            _accent = accent;
            _peak = Vfx.Strong ? peak : 1f;
            _scaleT = _peak > 1f ? 0f : 1f;
            _colorT = _graphic != null ? 0f : 1f;
        }

        void Update()
        {
            if (_scaleT >= 1f && _colorT >= 1f)
                return;
            if (!Vfx.Motion)
            {
                Finish();
                return;
            }
            float dt = Time.unscaledDeltaTime;
            if (_scaleT < 1f)
            {
                _scaleT = Mathf.Min(1f, _scaleT + dt / ScaleSeconds);
                // Sobe até o pico na primeira metade e volta a 1 na segunda.
                float wave = Mathf.Sin(_scaleT * Mathf.PI);
                _rect.localScale = _restScale * (1f + (_peak - 1f) * wave);
                if (_scaleT >= 1f)
                    _rect.localScale = _restScale;
            }
            if (_colorT < 1f)
            {
                _colorT = Mathf.Min(1f, _colorT + dt / ColorSeconds);
                _graphic.color = _colorT >= 1f
                    ? _restColor
                    : Color.Lerp(_restColor, _accent, Mathf.Sin(_colorT * Mathf.PI));
            }
        }

        void Finish()
        {
            if (_rect == null)
                return;
            _rect.localScale = _restScale;
            if (_graphic != null)
                _graphic.color = _restColor;
            _scaleT = 1f;
            _colorT = 1f;
        }
    }
}
