using System;
using System.IO;
using UnityEngine;

namespace Fabula.Incrementant.Settings
{
    /// <summary>
    /// Configurações do jogo, globais por natureza (valem em qualquer cena). Ciclo de vida explícito: carregadas e
    /// aplicadas antes da primeira cena (<see cref="Initialize"/>); quem muda <see cref="Current"/> chama
    /// <see cref="NotifyChanged"/> para aplicar e avisar os ouvintes (<see cref="Changed"/>), e <see cref="Save"/> grava
    /// em <c>Application.persistentDataPath/settings.json</c>.
    /// O volume geral vai para <see cref="AudioListener.volume"/>; música e efeitos sonoros são multiplicadores que cada
    /// fonte de áudio aplica no próprio volume (<see cref="MusicVolume"/>, <see cref="SfxVolume"/>).
    /// </summary>
    public static class GameSettings
    {
        public const string FileName = "settings.json";

        static GameSettingsData _current;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static GameSettingsData Current
        {
            get
            {
                if (_current == null)
                    Load();
                return _current;
            }
        }

        /// <summary>Disparado depois de cada mudança aplicada. Inscreva em OnEnable e remova em OnDisable.</summary>
        public static event Action Changed;

        public static float MusicVolume => Current.MusicVolume;
        public static float SfxVolume => Current.SfxVolume;
        public static VfxLevel Vfx => Current.Vfx;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            // Com o domain reload desligado o estático sobrevive entre Plays: relê o arquivo e solta ouvintes antigos.
            Changed = null;
            Load();
            Apply();
        }

        /// <summary>Relê o arquivo (padrões se não existir ou não servir). Não aplica.</summary>
        public static void Load()
        {
            string json = null;
            try
            {
                if (File.Exists(FilePath))
                    json = File.ReadAllText(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GameSettings] Não consegui ler '{FilePath}', usando os padrões: {e.Message}");
            }
            _current = GameSettingsData.FromJson(json);
        }

        /// <summary>Grava num temporário e troca no fim, como o save da partida.</summary>
        public static void Save()
        {
            try
            {
                string path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temp = path + ".tmp";
                File.WriteAllText(temp, Current.ToJson());
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(temp, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameSettings] Falha ao gravar as configurações em '{FilePath}': {e}");
            }
        }

        /// <summary>Corrige, aplica e avisa os ouvintes. Chame depois de alterar campos de <see cref="Current"/>.</summary>
        public static void NotifyChanged()
        {
            Current.Clamp();
            Apply();
            Changed?.Invoke();
        }

        public static void ResetToDefaults()
        {
            _current = new GameSettingsData();
            NotifyChanged();
        }

        static void Apply()
        {
            GameSettingsData s = Current;
            AudioListener.volume = s.MasterVolume;
            Texts.Loc.SetLanguage(s.Language);
            QualitySettings.vSyncCount = s.VSync ? 1 : 0;
            Application.targetFrameRate = s.VSync || s.FpsLimit <= 0 ? -1 : s.FpsLimit;
            ApplyDisplay(s);
        }

        static void ApplyDisplay(GameSettingsData s)
        {
            // No Editor a Game view ignora modo e resolução; mexer nisso só altera a janela do próprio Editor.
            if (Application.isEditor)
                return;
            FullScreenMode mode = s.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            int width = s.ResolutionWidth, height = s.ResolutionHeight;
            if (width <= 0 || height <= 0)
            {
                width = s.Fullscreen ? Screen.currentResolution.width : Screen.width;
                height = s.Fullscreen ? Screen.currentResolution.height : Screen.height;
            }
            if (Screen.fullScreenMode != mode || Screen.width != width || Screen.height != height)
                Screen.SetResolution(width, height, mode);
        }
    }
}
