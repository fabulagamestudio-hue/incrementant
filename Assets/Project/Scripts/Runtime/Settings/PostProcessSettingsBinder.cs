using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fabula.Incrementant.Settings
{
    /// <summary>
    /// Aplica as preferências de pós-processamento (<see cref="GameSettingsData.Bloom"/>) no Volume global da cena. Mexe
    /// na cópia do perfil (<see cref="Volume.profile"/>), nunca no asset compartilhado, para jogar no Editor não alterar o
    /// perfil salvo. Montado por <c>Incrementant/Setup/Configurar pós-processamento</c>.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public sealed class PostProcessSettingsBinder : MonoBehaviour
    {
        VolumeProfile _profileCopy;
        Bloom _bloom;

        void Awake()
        {
            var volume = GetComponent<Volume>();
            if (volume.sharedProfile == null)
            {
                Debug.LogError($"{name}: Volume sem perfil (rode Incrementant/Setup/Configurar pós-processamento).", this);
                enabled = false;
                return;
            }
            _profileCopy = volume.profile;
            if (!_profileCopy.TryGet(out _bloom))
                Debug.LogWarning($"{name}: o perfil '{volume.sharedProfile.name}' não tem Bloom; a opção das configurações não terá efeito.", this);
        }

        void OnEnable()
        {
            GameSettings.Changed += Apply;
            Apply();
        }

        void OnDisable() => GameSettings.Changed -= Apply;

        void OnDestroy()
        {
            if (_profileCopy != null)
                Destroy(_profileCopy);
        }

        void Apply()
        {
            if (_bloom != null)
                _bloom.active = GameSettings.Current.Bloom;
        }
    }
}
