using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Texts;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Tela de game over (a rainha morreu): fundo escurecido em tela cheia que cobre o mundo e o HUD, o motivo com o tempo de
    /// partida e as saídas (carregar o último save ou começar um mapa novo). Aparece sozinha quando
    /// <see cref="AntFarmGame.Colony"/> informa <c>GameOver</c> e some quando a partida recomeça. Montada pelo setup do HUD.
    /// </summary>
    public sealed class GameOverScreen : MonoBehaviour
    {
        [SerializeField] AntFarmGame _game;
        [SerializeField] GameObject _panel;
        [SerializeField] Text _body;
        [SerializeField] Button _load;
        [SerializeField] Button _newMap;
        [SerializeField] Text _status;

        bool _valid;
        bool _shown;

        void Awake()
        {
            _valid = _game != null && _panel != null && _body != null && _load != null && _newMap != null && _status != null;
            if (!_valid)
            {
                Debug.LogError($"{name}: GameOverScreen com referências faltando (rode Incrementant/Setup/Montar HUD da partida).", this);
                enabled = false;
                return;
            }
            _panel.SetActive(false);
        }

        void OnEnable()
        {
            if (!_valid)
                return;
            _load.onClick.AddListener(Load);
            _newMap.onClick.AddListener(NewMap);
        }

        void OnDisable()
        {
            if (!_valid)
                return;
            _load.onClick.RemoveListener(Load);
            _newMap.onClick.RemoveListener(NewMap);
        }

        void Load() => _game.LoadGame();

        void NewMap() => _game.NewMap();

        void Update()
        {
            bool over = _game.Colony != null && _game.Colony.GameOver;
            if (over != _shown)
            {
                _shown = over;
                _panel.SetActive(over);
            }
            if (!over)
                return;
            // O tempo para de contar com a rainha morta; o aviso de "sem save" aparece aqui, já que o HUD fica coberto.
            _body.text = Loc.T("gameover.body", MatchTimer.Format(_game.Timer.Seconds));
            _status.text = _game.SaveStatus.ToUpperInvariant();
        }
    }
}
