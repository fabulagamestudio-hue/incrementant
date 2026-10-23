using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Golpes nos blocos (REQ-072, pedido do desenvolvedor em 2026-10-23): o tempo de soltar um bloco inteiro (terra, areia,
    /// minério, rocha, terra da pilha) é dividido em golpes de <see cref="HitSeconds"/>, e cada golpe soma dano à célula
    /// (<see cref="PixelGrid.SetDamage"/>), que o render desenha como rachadura. O tempo total não muda (decisão do
    /// desenvolvedor: as melhorias de velocidade tornam a escavação mais eficiente). Planta não racha.
    /// </summary>
    public sealed partial class AntColony
    {
        /// <summary>
        /// Intervalo entre golpes, em segundos (suposição do agente). Bloco que sai em menos de dois golpes não racha: o dano
        /// nem chegaria a aparecer antes de ele sair.
        /// </summary>
        public const float HitSeconds = 0.1f;

        static bool IsHitting(AntState state) =>
            state == AntState.Digging || state == AntState.Gathering || state == AntState.GatheringSoil;

        /// <summary>
        /// Chamado logo depois de pôr em <see cref="Ant.Cooldown"/> o tempo de soltar o bloco em (DigX, DigY): o dano que a
        /// célula já tem (de outra formiga que a largou) desconta o tempo, e a formiga passa a golpeá-la.
        /// </summary>
        void StartHits(Ant ant)
        {
            ant.HitCell = -1;
            float total = ant.Cooldown;
            if (!(total >= 2f * HitSeconds) || float.IsInfinity(total))
                return;
            ant.HitTotal = total;
            ant.HitCell = _grid.ToIndex(ant.DigX, ant.DigY);
            ant.Cooldown = total * (1f - _grid.GetDamage(ant.DigX, ant.DigY));
        }

        /// <summary>A cada passo da espera: os golpes já dados viram dano na célula.</summary>
        void UpdateHits(Ant ant)
        {
            if (!IsHitting(ant.State) || ant.HitCell != _grid.ToIndex(ant.DigX, ant.DigY))
            {
                ant.HitCell = -1;
                return;
            }
            float elapsed = ant.HitTotal - Mathf.Max(ant.Cooldown, 0f);
            int hits = Mathf.FloorToInt(elapsed / HitSeconds);
            if (hits > 0)
                _grid.SetDamage(ant.DigX, ant.DigY, hits * HitSeconds / ant.HitTotal);
        }
    }
}
