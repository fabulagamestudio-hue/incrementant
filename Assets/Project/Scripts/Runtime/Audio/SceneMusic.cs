using UnityEngine;

namespace Fabula.Incrementant.Audio
{
    /// <summary>Declara a música da cena: ao carregar, pede a faixa ao <see cref="MusicPlayer"/>, que faz o crossfade da anterior.</summary>
    public sealed class SceneMusic : MonoBehaviour
    {
        [SerializeField] AudioClip _track;
        [Tooltip("Segundos de transição desde a faixa anterior.")]
        [SerializeField, Min(0.1f)] float _fadeSeconds = MusicPlayer.DefaultFadeSeconds;

        void Start()
        {
            if (_track == null)
                Debug.LogWarning($"{name}: SceneMusic sem faixa; rode o setup da cena para atribuir a música.", this);
            else
                MusicPlayer.Play(_track, _fadeSeconds);
        }
    }
}
