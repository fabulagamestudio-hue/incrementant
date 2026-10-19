using Fabula.Incrementant.Texts;
using Fabula.Incrementant.AntFarm;
using Fabula.Incrementant.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Menu do ESC: continuar, configurações, salvar, carregar e sair. Abrir pausa a simulação e trava o input do mundo
    /// (<see cref="AntFarmGame.InputLocked"/>); fechar devolve a pausa como estava. Quem decide o que o ESC faz é o
    /// <see cref="AntFarmHud"/>, que chama <see cref="Open"/> e <see cref="Back"/>. Montado pelo setup do HUD.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] AntFarmGame _game;
        [SerializeField] GameObject _mainPanel;
        [SerializeField] SettingsScreen _settings;
        [SerializeField] Button _resume;
        [SerializeField] Button _settingsButton;
        [SerializeField] Button _save;
        [SerializeField] Button _load;
        [SerializeField] Button _mainMenu;
        [SerializeField] Button _quit;
        [SerializeField] Text _status;

        bool _wasPaused;
        string _localStatus = "";
        float _localStatusUntil;
        bool _valid;

        public bool IsOpen => gameObject.activeSelf;

        void Awake()
        {
            _valid = _game != null && _mainPanel != null && _settings != null && _resume != null && _settingsButton != null &&
                     _save != null && _load != null && _mainMenu != null && _quit != null && _status != null;
            if (!_valid)
            {
                Debug.LogError($"{name}: PauseMenu com referências faltando (rode Incrementant/Setup/Montar HUD da partida).", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            if (!_valid)
                return;
            _resume.onClick.AddListener(Close);
            _settingsButton.onClick.AddListener(OpenSettings);
            _save.onClick.AddListener(Save);
            _load.onClick.AddListener(Load);
            _mainMenu.onClick.AddListener(GoToMainMenu);
            _quit.onClick.AddListener(Quit);
        }

        void OnDisable()
        {
            if (!_valid)
                return;
            _resume.onClick.RemoveListener(Close);
            _settingsButton.onClick.RemoveListener(OpenSettings);
            _save.onClick.RemoveListener(Save);
            _load.onClick.RemoveListener(Load);
            _mainMenu.onClick.RemoveListener(GoToMainMenu);
            _quit.onClick.RemoveListener(Quit);
        }

        public void Open()
        {
            if (IsOpen)
                return;
            _wasPaused = _game.Paused;
            _game.SetPaused(true);
            _game.InputLocked = true;
            _localStatus = "";
            gameObject.SetActive(true);
            _settings.gameObject.SetActive(false);
            _mainPanel.SetActive(true);
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            if (_settings.gameObject.activeSelf)
                _settings.Close();
            gameObject.SetActive(false);
            _game.InputLocked = false;
            _game.SetPaused(_wasPaused);
        }

        /// <summary>ESC com o menu aberto: das configurações volta ao menu; do menu, volta ao jogo.</summary>
        public void Back()
        {
            if (_settings.gameObject.activeSelf)
                _settings.Close();
            else
                Close();
        }

        void OpenSettings()
        {
            _mainPanel.SetActive(false);
            _settings.gameObject.SetActive(true);
        }

        void Save() => _game.SaveGame();

        void Load() => _game.LoadGame();

        /// <summary>Volta ao menu principal sem salvar: a partida não gravada se perde (o aviso fica a cargo de quem salva antes).</summary>
        void GoToMainMenu()
        {
            GameSettings.Save();
            SceneManager.LoadScene(Menu.GameLaunch.MenuSceneName);
        }

        void Quit()
        {
            if (Application.isEditor)
            {
                ShowStatus(Loc.T("pause.quitnote"));
                return;
            }
            GameSettings.Save();
            Application.Quit();
        }

        void ShowStatus(string message)
        {
            _localStatus = message;
            _localStatusUntil = Time.unscaledTime + 4f;
        }

        void Update()
        {
            // As configurações se fecham sozinhas (VOLTAR): o menu principal reaparece.
            if (!_settings.gameObject.activeSelf && !_mainPanel.activeSelf)
                _mainPanel.SetActive(true);
            string status = _localStatus.Length > 0 && Time.unscaledTime < _localStatusUntil ? _localStatus : _game.SaveStatus;
            _status.text = status.ToUpperInvariant();
        }
    }
}
