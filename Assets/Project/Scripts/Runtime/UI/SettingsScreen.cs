using Fabula.Incrementant.Texts;
using Fabula.Incrementant.Audio;
using System.Collections.Generic;
using Fabula.Incrementant.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Tela de configurações (aberta pelo menu do ESC): volumes, efeitos visuais, brilho (bloom), tela cheia, resolução, VSync, limite de
    /// FPS e o contador do canto. Cada mudança é aplicada na hora por <see cref="GameSettings.NotifyChanged"/>; o arquivo é
    /// gravado ao sair da tela. A hierarquia é montada por <c>Incrementant/Setup/Montar HUD da partida</c>.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        [Header("Áudio")]
        [SerializeField] Slider _master;
        [SerializeField] Text _masterValue;
        [SerializeField] Slider _music;
        [SerializeField] Text _musicValue;
        [SerializeField] Slider _sfx;
        [SerializeField] Text _sfxValue;

        [Header("Vídeo")]
        [SerializeField] Button _vfxPrevious;
        [SerializeField] Button _vfxNext;
        [SerializeField] Text _vfxValue;
        [SerializeField] Button _bloom;
        [SerializeField] Image _bloomImage;
        [SerializeField] Button _fullscreen;
        [SerializeField] Image _fullscreenImage;
        [SerializeField] Button _resolutionPrevious;
        [SerializeField] Button _resolutionNext;
        [SerializeField] Text _resolutionValue;
        [SerializeField] Button _vsync;
        [SerializeField] Image _vsyncImage;
        [SerializeField] Button _fpsPrevious;
        [SerializeField] Button _fpsNext;
        [SerializeField] Text _fpsValue;

        [Header("Interface")]
        [SerializeField] Button _languagePrevious;
        [SerializeField] Button _languageNext;
        [SerializeField] Text _languageValue;
        [SerializeField] Button _showStats;
        [SerializeField] Image _showStatsImage;
        [SerializeField] Button _blockBuild;
        [SerializeField] Image _blockBuildImage;

        [Header("Rodapé")]
        [SerializeField] Button _defaults;
        [SerializeField] Button _back;

        [Header("Sprites do interruptor")]
        [SerializeField] Sprite _toggleOn;
        [SerializeField] Sprite _toggleOff;

        static readonly Color ValueDisabled = new Color(0.36f, 0.33f, 0.30f);
        static readonly Color ValueEnabled = new Color(0.23f, 0.13f, 0.07f);

        /// <summary>Resoluções oferecidas: a primeira é a nativa (0x0); depois as do monitor, sem repetir tamanho.</summary>
        readonly List<Vector2Int> _resolutions = new List<Vector2Int>();
        bool _valid;

        void Awake()
        {
            _valid = _master != null && _music != null && _sfx != null && _masterValue != null && _musicValue != null &&
                     _sfxValue != null && _vfxPrevious != null && _vfxNext != null && _vfxValue != null &&
                     _fullscreen != null && _fullscreenImage != null && _resolutionPrevious != null &&
                     _resolutionNext != null && _resolutionValue != null && _vsync != null && _vsyncImage != null &&
                     _fpsPrevious != null && _fpsNext != null && _fpsValue != null && _showStats != null &&
                     _showStatsImage != null && _defaults != null && _back != null && _languagePrevious != null &&
                     _languageNext != null && _languageValue != null && _blockBuild != null && _blockBuildImage != null &&
                     _bloom != null && _bloomImage != null;
            if (!_valid)
            {
                Debug.LogError($"{name}: SettingsScreen com referências faltando (rode Incrementant/Setup/Montar HUD da partida).", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            if (!_valid)
                return;
            CollectResolutions();
            _master.onValueChanged.AddListener(SetMaster);
            _music.onValueChanged.AddListener(SetMusic);
            _sfx.onValueChanged.AddListener(SetSfx);
            _vfxPrevious.onClick.AddListener(PreviousVfx);
            _vfxNext.onClick.AddListener(NextVfx);
            _bloom.onClick.AddListener(ToggleBloom);
            _fullscreen.onClick.AddListener(ToggleFullscreen);
            _resolutionPrevious.onClick.AddListener(PreviousResolution);
            _resolutionNext.onClick.AddListener(NextResolution);
            _vsync.onClick.AddListener(ToggleVSync);
            _fpsPrevious.onClick.AddListener(PreviousFps);
            _fpsNext.onClick.AddListener(NextFps);
            _showStats.onClick.AddListener(ToggleShowStats);
            _blockBuild.onClick.AddListener(ToggleBlockBuild);
            _languagePrevious.onClick.AddListener(PreviousLanguage);
            _languageNext.onClick.AddListener(NextLanguage);
            Loc.LanguageChanged += Refresh;
            _defaults.onClick.AddListener(RestoreDefaults);
            _back.onClick.AddListener(Close);
            Refresh();
        }

        void OnDisable()
        {
            if (!_valid)
                return;
            _master.onValueChanged.RemoveListener(SetMaster);
            _music.onValueChanged.RemoveListener(SetMusic);
            _sfx.onValueChanged.RemoveListener(SetSfx);
            _vfxPrevious.onClick.RemoveListener(PreviousVfx);
            _vfxNext.onClick.RemoveListener(NextVfx);
            _bloom.onClick.RemoveListener(ToggleBloom);
            _fullscreen.onClick.RemoveListener(ToggleFullscreen);
            _resolutionPrevious.onClick.RemoveListener(PreviousResolution);
            _resolutionNext.onClick.RemoveListener(NextResolution);
            _vsync.onClick.RemoveListener(ToggleVSync);
            _fpsPrevious.onClick.RemoveListener(PreviousFps);
            _fpsNext.onClick.RemoveListener(NextFps);
            _showStats.onClick.RemoveListener(ToggleShowStats);
            _blockBuild.onClick.RemoveListener(ToggleBlockBuild);
            _languagePrevious.onClick.RemoveListener(PreviousLanguage);
            _languageNext.onClick.RemoveListener(NextLanguage);
            Loc.LanguageChanged -= Refresh;
            _defaults.onClick.RemoveListener(RestoreDefaults);
            _back.onClick.RemoveListener(Close);
        }

        /// <summary>Grava e esconde a tela (VOLTAR ou ESC); o menu do ESC volta a aparecer.</summary>
        public void Close()
        {
            GameSettings.Save();
            gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- handlers

        static GameSettingsData S => GameSettings.Current;

        void SetMaster(float value) { S.MasterVolume = value; Changed(); GameAudio.Play(SfxId.UiTick); }

        void SetMusic(float value) { S.MusicVolume = value; Changed(); }

        void SetSfx(float value) { S.SfxVolume = value; Changed(); GameAudio.Play(SfxId.UiTick); }

        void PreviousVfx() { S.Vfx = (VfxLevel)Wrap((int)S.Vfx - 1, 3); Changed(); }

        void NextVfx() { S.Vfx = (VfxLevel)Wrap((int)S.Vfx + 1, 3); Changed(); }

        void ToggleBloom() { S.Bloom = !S.Bloom; Changed(); }

        void ToggleFullscreen() { S.Fullscreen = !S.Fullscreen; Changed(); }

        void PreviousResolution() => StepResolution(-1);

        void NextResolution() => StepResolution(1);

        void StepResolution(int step)
        {
            Vector2Int next = _resolutions[Wrap(ResolutionIndex() + step, _resolutions.Count)];
            S.ResolutionWidth = next.x;
            S.ResolutionHeight = next.y;
            Changed();
        }

        void ToggleVSync() { S.VSync = !S.VSync; Changed(); }

        void PreviousFps() => StepFps(-1);

        void NextFps() => StepFps(1);

        void StepFps(int step)
        {
            if (S.VSync)
                return; // com VSync o limite é a taxa do monitor
            int[] options = GameSettingsData.FpsOptions;
            S.FpsLimit = options[Wrap(System.Array.IndexOf(options, S.FpsLimit) + step, options.Length)];
            Changed();
        }

        void PreviousLanguage() { S.Language = Loc.Step(S.Language, -1); Changed(); }

        void NextLanguage() { S.Language = Loc.Step(S.Language, 1); Changed(); }

        void ToggleShowStats() { S.ShowStats = !S.ShowStats; Changed(); }

        void ToggleBlockBuild() { S.BlockBuildOnRooms = !S.BlockBuildOnRooms; Changed(); }

        void RestoreDefaults()
        {
            GameSettings.ResetToDefaults();
            Refresh();
        }

        void Changed()
        {
            GameSettings.NotifyChanged();
            Refresh();
        }

        static int Wrap(int value, int count) => (value % count + count) % count;

        // ---------------------------------------------------------------- exibição

        void Refresh()
        {
            GameSettingsData s = S;
            _master.SetValueWithoutNotify(s.MasterVolume);
            _music.SetValueWithoutNotify(s.MusicVolume);
            _sfx.SetValueWithoutNotify(s.SfxVolume);
            _masterValue.text = Percent(s.MasterVolume);
            _musicValue.text = Percent(s.MusicVolume);
            _sfxValue.text = Percent(s.SfxVolume);
            _vfxValue.text = Loc.T(s.Vfx == VfxLevel.Full ? "settings.vfx.full" : s.Vfx == VfxLevel.Reduced ? "settings.vfx.reduced" : "settings.vfx.off");
            _languageValue.text = Loc.NativeName(s.Language);
            _bloomImage.sprite = s.Bloom ? _toggleOn : _toggleOff;
            _fullscreenImage.sprite = s.Fullscreen ? _toggleOn : _toggleOff;
            _vsyncImage.sprite = s.VSync ? _toggleOn : _toggleOff;
            _showStatsImage.sprite = s.ShowStats ? _toggleOn : _toggleOff;
            _blockBuildImage.sprite = s.BlockBuildOnRooms ? _toggleOn : _toggleOff;
            Vector2Int resolution = _resolutions[ResolutionIndex()];
            _resolutionValue.text = resolution.x <= 0 ? Loc.T("settings.native") : $"{resolution.x}X{resolution.y}";
            _fpsValue.text = s.VSync ? "VSYNC" : s.FpsLimit <= 0 ? Loc.T("settings.nolimit") : s.FpsLimit.ToString();
            _fpsValue.color = s.VSync ? ValueDisabled : ValueEnabled;
            _fpsPrevious.interactable = _fpsNext.interactable = !s.VSync;
        }

        static string Percent(float value) => $"{Mathf.RoundToInt(value * 100f)}%";

        void CollectResolutions()
        {
            _resolutions.Clear();
            _resolutions.Add(Vector2Int.zero);
            foreach (Resolution r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!_resolutions.Contains(size))
                    _resolutions.Add(size);
            }
            // Uma resolução salva que o monitor atual não lista continua na lista, para não sumir da tela.
            var saved = new Vector2Int(S.ResolutionWidth, S.ResolutionHeight);
            if (saved.x > 0 && !_resolutions.Contains(saved))
                _resolutions.Add(saved);
            _resolutions.Sort(1, _resolutions.Count - 1, Comparer<Vector2Int>.Create((a, b) =>
                a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y)));
        }

        int ResolutionIndex()
        {
            int index = _resolutions.IndexOf(new Vector2Int(S.ResolutionWidth, S.ResolutionHeight));
            return index < 0 ? 0 : index;
        }
    }
}
