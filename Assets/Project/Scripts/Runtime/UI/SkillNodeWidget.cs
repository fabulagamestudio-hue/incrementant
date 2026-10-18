using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>Estado visual de um nó da árvore (a seleção é à parte: não apaga o estado).</summary>
    public enum SkillNodeState
    {
        Acquired,
        Available,
        Locked,
    }

    /// <summary>
    /// Um nó da tela de evolução, em camadas independentes: losango (estado), ícone, anel de seleção, cadeado e rótulo.
    /// O botão fica num filho transparente menor que o sprite, para os nós vizinhos não se cobrirem no clique.
    /// Montado pelo Editor (<c>AntFarmHudSetup</c>) e comandado por <see cref="SkillTreeScreen"/>.
    /// </summary>
    public sealed class SkillNodeWidget : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _diamond;
        [SerializeField] Image _icon;
        [SerializeField] Image _ring;
        [SerializeField] Image _lock;
        [SerializeField] Text _label;
        [SerializeField] Sprite _iconNormal;
        [Tooltip("Variante apagada do ícone; vazia = o ícone normal escurecido.")]
        [SerializeField] Sprite _iconLocked;
        [Tooltip("O nó central da árvore usa o losango da raiz e não muda de estado.")]
        [SerializeField] bool _root;

        static readonly Color Dim = new Color(0.55f, 0.55f, 0.55f, 0.85f);

        public Button Button => _button;

        public void Apply(SkillNodeState state, bool selected, Sprite acquired, Sprite available, Sprite locked, Sprite root)
        {
            if (_root)
            {
                _diamond.sprite = root;
                _icon.sprite = _iconNormal;
                _icon.color = Color.white;
                _lock.enabled = false;
                _ring.enabled = selected;
                return;
            }
            _diamond.sprite = state == SkillNodeState.Acquired ? acquired : state == SkillNodeState.Available ? available : locked;
            bool dim = state == SkillNodeState.Locked;
            _icon.sprite = dim && _iconLocked != null ? _iconLocked : _iconNormal;
            _icon.color = dim && _iconLocked == null ? Dim : Color.white;
            _lock.enabled = dim;
            _ring.enabled = selected;
        }
    }
}
