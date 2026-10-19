using Fabula.Incrementant.Settings;
using UnityEngine;

namespace Fabula.Incrementant.Audio
{
    /// <summary>
    /// Serviço de música. Dois <see cref="AudioSource"/> num objeto que sobrevive às cenas se revezam: ao pedir outra faixa,
    /// a atual some e a nova entra ao mesmo tempo (crossfade), sem corte seco. Pedir a faixa que já toca não faz nada, então
    /// cada cena só declara a sua (<see cref="SceneMusic"/>). O volume final é <see cref="GameSettings.MusicVolume"/> por cima
    /// do nível do fade; o geral entra pelo <see cref="AudioListener.volume"/>.
    /// </summary>
    public static class MusicPlayer
    {
        /// <summary>Duração padrão da transição entre faixas, em segundos.</summary>
        public const float DefaultFadeSeconds = 2.5f;

        static Runner _runner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _runner = null;

        /// <summary>Passa a tocar <paramref name="clip"/> em laço, com crossfade. <c>null</c> faz a música atual sumir.</summary>
        public static void Play(AudioClip clip, float fadeSeconds = DefaultFadeSeconds)
        {
            if (!Application.isPlaying)
                return;
            if (_runner == null)
            {
                if (clip == null)
                    return;
                var go = new GameObject("MusicPlayer");
                Object.DontDestroyOnLoad(go);
                _runner = go.AddComponent<Runner>();
            }
            _runner.Crossfade(clip, Mathf.Max(0.01f, fadeSeconds));
        }

        sealed class Runner : MonoBehaviour
        {
            sealed class Voice
            {
                public AudioSource Source;
                public float Level;
                public float Target;
            }

            Voice[] _voices;
            int _active;
            float _fadeSeconds = DefaultFadeSeconds;

            void Awake()
            {
                _voices = new Voice[2];
                for (int i = 0; i < _voices.Length; i++)
                {
                    AudioSource source = gameObject.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = true;
                    source.spatialBlend = 0f;
                    _voices[i] = new Voice { Source = source };
                }
            }

            public void Crossfade(AudioClip clip, float fadeSeconds)
            {
                Voice current = _voices[_active];
                if (current.Source.clip == clip && current.Target > 0f)
                    return;
                if (clip == null && current.Target <= 0f)
                    return;
                _fadeSeconds = fadeSeconds;
                current.Target = 0f;
                if (clip == null)
                    return;

                _active = 1 - _active;
                Voice next = _voices[_active];
                next.Source.Stop();
                next.Source.clip = clip;
                next.Level = 0f;
                next.Target = 1f;
                next.Source.Play();
            }

            void Update()
            {
                float step = Time.unscaledDeltaTime / _fadeSeconds;
                float music = GameSettings.MusicVolume;
                foreach (Voice voice in _voices)
                {
                    voice.Level = Mathf.MoveTowards(voice.Level, voice.Target, step);
                    voice.Source.volume = voice.Level * music;
                    if (voice.Level <= 0f && voice.Target <= 0f && voice.Source.isPlaying)
                        voice.Source.Stop();
                }
            }
        }
    }
}
