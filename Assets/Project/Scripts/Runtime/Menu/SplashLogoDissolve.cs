using UnityEngine;
using UnityEngine.VFX;

namespace Fabula.Incrementant.Menu
{
    /// <summary>
    /// Dissolve do logotipo do splash em blocos de terra: dispara o VFX Graph <c>LogoDissolve.vfx</c> e o mantém
    /// alinhado ao logotipo da UI. O grafo, as texturas de posição/cor/atraso e este objeto são montados por
    /// <c>Incrementant/Setup/Splash - dissolução da logo</c>.
    /// </summary>
    [RequireComponent(typeof(VisualEffect))]
    public sealed class SplashLogoDissolve : MonoBehaviour
    {
        // Altura do logotipo, em unidades de mundo, usada ao assar as texturas (2 x orthographic size da câmera do splash).
        public const float BakedHeight = 10f;

        [Tooltip("Câmera do splash (ortográfica); o canvas de tela cobre exatamente o seu enquadramento.")]
        [SerializeField] Camera _camera;
        [Tooltip("Proporção largura/altura do sprite do logotipo.")]
        [SerializeField] float _logoAspect = 1.7768332f;

        VisualEffect _effect;
        bool _playing;

        public bool Playing => _playing;

        void Awake()
        {
            _effect = GetComponent<VisualEffect>();
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null || !_camera.orthographic)
            {
                Debug.LogError($"{name}: SplashLogoDissolve precisa da câmera ortográfica do splash (rode Incrementant/Setup/Splash - dissolução da logo).", this);
                enabled = false;
                return;
            }
            Fit();
        }

        void LateUpdate() => Fit();

        /// <summary>Solta os blocos: cada um espera o seu atraso (assado na textura) e cai.</summary>
        public void Play()
        {
            if (_effect == null || _playing)
                return;
            _playing = true;
            _effect.Play();
        }

        // O logotipo da UI usa FitInParent: altura = min(altura da tela, largura da tela / proporção do sprite).
        void Fit()
        {
            float screenHeight = 2f * _camera.orthographicSize;
            float logoHeight = Mathf.Min(screenHeight, screenHeight * _camera.aspect / _logoAspect);
            float scale = logoHeight / BakedHeight;
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
