using System;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Cronômetro da partida: tempo real jogado, não afetado pela velocidade da simulação (x1/x3/x8).
    /// Recomeça a cada mapa novo e é guardado no save.
    /// </summary>
    public sealed class MatchTimer
    {
        public double Seconds { get; private set; }

        public void Advance(float unscaledDeltaTime)
        {
            if (unscaledDeltaTime > 0f)
                Seconds += unscaledDeltaTime;
        }

        public void Reset() => Seconds = 0.0;

        public void Set(double seconds) => Seconds = Math.Max(0.0, seconds);

        /// <summary>"mm:ss", ou "h:mm:ss" a partir de uma hora.</summary>
        public static string Format(double seconds)
        {
            var span = TimeSpan.FromSeconds(Math.Max(0.0, seconds));
            return span.TotalHours >= 1.0
                ? $"{(int)span.TotalHours}:{span.Minutes:00}:{span.Seconds:00}"
                : $"{span.Minutes:00}:{span.Seconds:00}";
        }

        public override string ToString() => Format(Seconds);
    }
}
