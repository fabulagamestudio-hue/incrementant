using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fabula.Incrementant.Menu
{
    /// <summary>
    /// Splash da Fábula, a primeira cena do build: a imagem aparece, fica um tempo e some, e então carrega o menu principal.
    /// Qualquer tecla ou clique pula. Tempo real (<see cref="Time.unscaledDeltaTime"/>). Montada por
    /// <c>Incrementant/Setup/Criar cena Splash</c>.
    /// </summary>
    public sealed class SplashScreen : MonoBehaviour
    {
        [Tooltip("Imagem de tela cheia; o fundo atrás dela usa a cor da câmera da cena.")]
        [SerializeField] Graphic _fade;
        [Tooltip("Logotipo transparente central, separado do fundo.")]
        [SerializeField] Graphic _logo;
        [Tooltip("Opcional: dissolve o logotipo em blocos de terra depois do tempo parado. Sem ele, só há o fade de saída.")]
        [SerializeField] SplashLogoDissolve _dissolve;
        [SerializeField] float _fadeInSeconds = 0.7f;
        [SerializeField] float _holdSeconds = 1.8f;
        [Tooltip("Duração da dissolução do logotipo (só com o dissolve atribuído); o fundo permanece até ela acabar.")]
        [SerializeField] float _dissolveSeconds = 4.5f;
        [Tooltip("Tempo em que o sprite do logotipo some por baixo dos blocos, logo que a dissolução começa.")]
        [SerializeField] float _logoHandoffSeconds = 0.2f;
        [SerializeField] float _fadeOutSeconds = 0.7f;
        [Tooltip("Cena carregada no fim do splash.")]
        [SerializeField] string _nextScene = GameLaunch.MenuSceneName;

        float _elapsed;
        bool _leaving;

        void Awake()
        {
            if (_fade == null)
            {
                Debug.LogError($"{name}: SplashScreen sem a imagem (rode Incrementant/Setup/Criar cena Splash).", this);
                enabled = false;
                return;
            }
            SetAlpha(0f);
        }

        void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            float dissolveStart = _fadeInSeconds + _holdSeconds;
            float fadeOutStart = dissolveStart + DissolveDuration;
            float total = fadeOutStart + _fadeOutSeconds;
            if (!_leaving && _elapsed > 0.3f && SkipPressed())
            {
                // Pulado: sai pelo mesmo fade de saída, a partir do alfa atual.
                _elapsed = Mathf.Max(_elapsed, fadeOutStart);
            }
            if (_dissolve != null && !_dissolve.Playing && _elapsed >= dissolveStart && _elapsed < fadeOutStart)
                _dissolve.Play();

            if (_elapsed >= total)
            {
                _leaving = true;
                SceneManager.LoadScene(_nextScene);
                enabled = false;
                return;
            }
            SetAlpha(AlphaAt(_elapsed), LogoAlphaAt(_elapsed));
        }

        float DissolveDuration => _dissolve != null ? _dissolveSeconds : 0f;

        float AlphaAt(float t)
        {
            if (t < _fadeInSeconds)
                return Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, _fadeInSeconds));
            t -= _fadeInSeconds + _holdSeconds + DissolveDuration;
            if (t < 0f)
                return 1f;
            return Mathf.SmoothStep(1f, 0f, t / Mathf.Max(0.01f, _fadeOutSeconds));
        }

        // Com a dissolução, o sprite do logotipo cede lugar aos blocos assim que ela começa.
        float LogoAlphaAt(float t)
        {
            float alpha = AlphaAt(t);
            if (_dissolve == null)
                return alpha;
            float sinceDissolve = t - (_fadeInSeconds + _holdSeconds);
            if (sinceDissolve <= 0f)
                return alpha;
            return Mathf.Min(alpha, 1f - Mathf.Clamp01(sinceDissolve / Mathf.Max(0.01f, _logoHandoffSeconds)));
        }

        void SetAlpha(float alpha) => SetAlpha(alpha, alpha);

        void SetAlpha(float alpha, float logoAlpha)
        {
            Color c = _fade.color;
            c.a = alpha;
            _fade.color = c;
            if (_logo != null)
            {
                c = _logo.color;
                c.a = logoAlpha;
                _logo.color = c;
            }
        }

        static bool SkipPressed()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            return (keyboard != null && keyboard.anyKey.wasPressedThisFrame) ||
                   (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame));
        }
    }
}
