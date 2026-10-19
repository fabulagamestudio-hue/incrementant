using Fabula.Incrementant.Texts;
using Fabula.Incrementant.Audio;
using Fabula.Incrementant.Juice;
using System.IO;
using Fabula.Incrementant.Save;
using Fabula.Incrementant.Settings;
using Fabula.Incrementant.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Fabula.Incrementant.Menu
{
    /// <summary>
    /// Menu principal: iniciar colônia nova, continuar a salva, configurações, sobre o jogo e sair. Roda sobre a cena
    /// de vitrine (a colônia trabalhando ao fundo, ver <c>AntFarmGame</c> em modo vitrine). A hierarquia é montada por
    /// <c>Incrementant/Setup/Criar cena MainMenu</c>.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] GameObject _buttonsPanel;
        [SerializeField] Button _newGame;
        [SerializeField] Button _continue;
        [SerializeField] Button _settingsButton;
        [SerializeField] Button _about;
        [SerializeField] Button _quit;
        [Tooltip("Ícone e texto do Continuar: cadeado e cinza sem save, seta e escuro com save.")]
        [SerializeField] Image _continueIcon;
        [SerializeField] Text _continueLabel;
        [SerializeField] Sprite _continueLockedIcon;
        [SerializeField] Sprite _continueUnlockedIcon;
        [SerializeField] Text _footer;
        [Tooltip("Troca rápida de idioma (canto da tela); o texto mostra o idioma atual.")]
        [SerializeField] Button _language;
        [SerializeField] Text _languageLabel;
        [SerializeField] SettingsScreen _settings;
        [SerializeField] GameObject _aboutPanel;
        [SerializeField] Button _aboutClose;
        [Tooltip("Cena da partida carregada por Iniciar e Continuar.")]
        [SerializeField] string _gameScene = GameLaunch.GameSceneName;

        static readonly Color EnabledText = new Color(0.23f, 0.13f, 0.07f);
        static readonly Color DisabledText = new Color(0.45f, 0.45f, 0.47f);

        const string NoSaveKey = "menu.nosave";

        bool _valid;
        bool _hasSave;
        bool _loading;

        void Awake()
        {
            _valid = _buttonsPanel != null && _newGame != null && _continue != null && _settingsButton != null &&
                     _about != null && _quit != null && _footer != null && _continueIcon != null && _continueLabel != null && _settings != null && _aboutPanel != null &&
                     _aboutClose != null && _language != null && _languageLabel != null;
            if (!_valid)
            {
                Debug.LogError($"{name}: MainMenu com referências faltando (rode Incrementant/Setup/Criar cena MainMenu).", this);
                enabled = false;
                return;
            }
            ButtonSfx.AttachAll(transform.root);
            ButtonJuice.AttachAll(transform.root);
        }

        void OnEnable()
        {
            if (!_valid)
                return;
            _newGame.onClick.AddListener(StartNew);
            _continue.onClick.AddListener(ContinueSaved);
            _settingsButton.onClick.AddListener(OpenSettings);
            _about.onClick.AddListener(OpenAbout);
            _aboutClose.onClick.AddListener(CloseAbout);
            _quit.onClick.AddListener(Quit);
            _language.onClick.AddListener(NextLanguage);
            Loc.LanguageChanged += RefreshSave;
            _settings.gameObject.SetActive(false);
            _aboutPanel.SetActive(false);
            _buttonsPanel.SetActive(true);
            RefreshSave();
        }

        void OnDisable()
        {
            if (!_valid)
                return;
            _newGame.onClick.RemoveListener(StartNew);
            _continue.onClick.RemoveListener(ContinueSaved);
            _settingsButton.onClick.RemoveListener(OpenSettings);
            _about.onClick.RemoveListener(OpenAbout);
            _aboutClose.onClick.RemoveListener(CloseAbout);
            _quit.onClick.RemoveListener(Quit);
            _language.onClick.RemoveListener(NextLanguage);
            Loc.LanguageChanged -= RefreshSave;
        }

        /// <summary>Há um save rápido legível: Continuar só vale com o arquivo presente.</summary>
        void RefreshSave()
        {
            try
            {
                _hasSave = File.Exists(SaveStorage.QuickSavePath);
            }
            catch (IOException)
            {
                _hasSave = false;
            }
            _continue.interactable = _hasSave;
            _continueIcon.sprite = _hasSave ? _continueUnlockedIcon : _continueLockedIcon;
            _continueIcon.color = _hasSave ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            _continueLabel.color = _hasSave ? EnabledText : DisabledText;
            _footer.text = _hasSave ? string.Empty : Loc.T(NoSaveKey);
            _languageLabel.text = Loc.NativeName(GameSettings.Current.Language);
        }

        void NextLanguage()
        {
            GameSettings.Current.Language = Loc.Step(GameSettings.Current.Language, 1);
            GameSettings.NotifyChanged();
            GameSettings.Save();
            RefreshSave();
        }

        void StartNew()
        {
            if (_loading)
                return;
            GameLaunch.RequestNew();
            Load();
        }

        void ContinueSaved()
        {
            if (_loading || !_hasSave)
                return;
            GameLaunch.RequestContinue();
            Load();
        }

        void Load()
        {
            _loading = true;
            SceneManager.LoadScene(_gameScene);
        }

        void OpenSettings()
        {
            _buttonsPanel.SetActive(false);
            _settings.gameObject.SetActive(true);
        }

        void OpenAbout()
        {
            _buttonsPanel.SetActive(false);
            _aboutPanel.SetActive(true);
        }

        void CloseAbout()
        {
            _aboutPanel.SetActive(false);
            _buttonsPanel.SetActive(true);
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Update()
        {
            // As configurações se fecham sozinhas (VOLTAR ou ESC): a coluna de botões volta.
            if (!_settings.gameObject.activeSelf && !_aboutPanel.activeSelf && !_buttonsPanel.activeSelf)
                _buttonsPanel.SetActive(true);

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;
            if (_settings.gameObject.activeSelf)
                _settings.Close();
            else if (_aboutPanel.activeSelf)
                CloseAbout();
        }
    }
}
