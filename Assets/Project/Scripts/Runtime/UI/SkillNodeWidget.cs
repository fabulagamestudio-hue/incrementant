using Fabula.Incrementant.Juice;
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
        [Tooltip("Selo de nível das galerias (\"NV 3/10\"); vazio nas câmaras.")]
        [SerializeField] Text _level;
        [Tooltip("Apaga o nó inteiro quando ele está num anel ainda fechado.")]
        [SerializeField] CanvasGroup _group;

        static readonly Color Dim = new Color(0.55f, 0.55f, 0.55f, 0.85f);
        const float BuriedAlpha = 0.45f;
        // Nó selecionado pulsa de tamanho entre estes fatores da escala montada, para se destacar dos vizinhos.
        const float SelectedScaleMin = 1.15f, SelectedScaleMax = 1.4f, SelectedPulseSpeed = 5f;

        Vector3 _baseScale;
        bool _hasBaseScale;
        bool _wasSelected;

        public Button Button => _button;

        /// <summary>Texto do selo de nível (só galerias).</summary>
        public void SetLevel(string text)
        {
            if (_level != null)
                _level.text = text;
        }

        /// <summary>Nó dentro de um anel fechado (terra ainda não escavada) fica apagado, mas continua clicável.</summary>
        public void SetBuried(bool buried)
        {
            if (_group != null)
                _group.alpha = buried ? BuriedAlpha : 1f;
        }

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
            PulseSelection(selected);
        }

        void PulseSelection(bool selected)
        {
            if (!_hasBaseScale)
            {
                _baseScale = transform.localScale;
                _hasBaseScale = true;
            }
            if (selected && !_wasSelected)
                transform.SetAsLastSibling(); // aumentado, fica por cima dos vizinhos
            _wasSelected = selected;
            float factor = 1f;
            if (selected)
            {
                // Sem movimento (efeitos desligados), fica parado no meio do pulso.
                float pulse = Vfx.Motion ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * SelectedPulseSpeed) : 0.5f;
                factor = Mathf.Lerp(SelectedScaleMin, SelectedScaleMax, pulse);
            }
            transform.localScale = _baseScale * factor;
        }
    }
}
