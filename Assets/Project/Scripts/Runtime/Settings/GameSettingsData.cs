using System;
using UnityEngine;

namespace Fabula.Incrementant.Settings
{
    /// <summary>Quanto de efeito visual o jogo desenha (partículas, tremor, brilhos). Lido pelo sistema de VFX.</summary>
    public enum VfxLevel
    {
        Full = 0,
        Reduced = 1,
        Off = 2,
    }

    /// <summary>
    /// Preferências do jogador, sem lógica de aplicação: só os valores e as regras de limite. Gravadas em JSON por
    /// <see cref="GameSettings"/>; campos novos entram com o valor padrão quando o arquivo antigo não os tem.
    /// </summary>
    [Serializable]
    public sealed class GameSettingsData
    {
        /// <summary>Limites de FPS oferecidos, na ordem da tela; 0 é sem limite.</summary>
        public static readonly int[] FpsOptions = { 30, 60, 120, 144, 0 };

        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;

        [Header("Áudio (0 a 1)")]
        public float MasterVolume = 1f;
        public float MusicVolume = 0.7f;
        public float SfxVolume = 0.8f;

        [Header("Vídeo")]
        public VfxLevel Vfx = VfxLevel.Full;
        public bool Fullscreen = true;
        /// <summary>Resolução escolhida; 0x0 é a nativa do monitor (ou o tamanho atual da janela).</summary>
        public int ResolutionWidth;
        public int ResolutionHeight;
        public bool VSync = true;
        /// <summary>Um de <see cref="FpsOptions"/>; só vale com o VSync desligado.</summary>
        public int FpsLimit = 60;

        [Header("Interface")]
        public bool ShowStats = true;
        /// <summary>Código do idioma (um de <c>Loc.Codes</c>); o padrão é o português do Brasil.</summary>
        public string Language = Texts.Loc.DefaultCode;

        /// <summary>Corrige valores fora da faixa (arquivo editado à mão ou de versão antiga).</summary>
        public void Clamp()
        {
            MasterVolume = Clamp01(MasterVolume);
            MusicVolume = Clamp01(MusicVolume);
            SfxVolume = Clamp01(SfxVolume);
            if (!Enum.IsDefined(typeof(VfxLevel), Vfx))
                Vfx = VfxLevel.Full;
            if (ResolutionWidth <= 0 || ResolutionHeight <= 0)
                ResolutionWidth = ResolutionHeight = 0;
            if (Array.IndexOf(FpsOptions, FpsLimit) < 0)
                FpsLimit = 60;
            if (!Texts.Loc.IsSupported(Language))
                Language = Texts.Loc.DefaultCode;
            Version = CurrentVersion;
        }

        static float Clamp01(float value) => float.IsNaN(value) ? 1f : Mathf.Clamp01(value);

        public GameSettingsData Clone() => (GameSettingsData)MemberwiseClone();

        public string ToJson() => JsonUtility.ToJson(this, true);

        /// <summary>Lê o JSON; texto vazio ou inválido devolve os padrões.</summary>
        public static GameSettingsData FromJson(string json)
        {
            var data = new GameSettingsData();
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    JsonUtility.FromJsonOverwrite(json, data);
                }
                catch (ArgumentException)
                {
                    data = new GameSettingsData();
                }
            }
            data.Clamp();
            return data;
        }

        public static string VfxLabel(VfxLevel level) =>
            level == VfxLevel.Full ? "COMPLETO" : level == VfxLevel.Reduced ? "REDUZIDO" : "DESLIGADO";

        public static string FpsLabel(int limit) => limit <= 0 ? "SEM LIMITE" : limit.ToString();
    }
}
