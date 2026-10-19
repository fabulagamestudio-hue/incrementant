using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.Audio
{
    /// <summary>
    /// Som de clique num <see cref="Button"/>. <see cref="AttachAll"/> põe o componente em todos os botões de uma tela
    /// (inclusive os desativados), uma vez só, sem precisar editar cada botão da cena.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonSfx : MonoBehaviour
    {
        Button _button;

        void OnEnable()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Click);
        }

        void OnDisable()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Click);
        }

        static void Click() => GameAudio.Play(SfxId.UiClick);

        public static void AttachAll(Transform root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                if (button.GetComponent<ButtonSfx>() == null)
                    button.gameObject.AddComponent<ButtonSfx>();
        }
    }
}
