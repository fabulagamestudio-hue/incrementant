namespace Fabula.Incrementant.Colony
{
    /// <summary>Etapa de um cupim no ataque.</summary>
    public enum TermitePhase : byte
    {
        /// <summary>Descendo do céu até o ponto de pouso (só desenho: a célula já é a do pouso).</summary>
        Flying,
        /// <summary>No chão, perdendo as asas.</summary>
        Landing,
        /// <summary>Andando atrás do objetivo do ataque (cria, canteiros, paredes) ou mordendo formigas.</summary>
        Raiding,
        /// <summary>Indo embora pela lateral do mapa.</summary>
        Leaving,
        /// <summary>Morto: o corpo fica no chão até as formigas o desmembrarem.</summary>
        Dead,
    }

    /// <summary>O que os cupins de um ataque estão destruindo agora (todos seguem o mesmo objetivo, nesta ordem).</summary>
    public enum RaidObjective : byte
    {
        Brood,
        /// <summary>Os pulgões da fazenda (pedido do desenvolvedor em 2026-10-22): o pulgão mordido fica preto e muda de lado.</summary>
        Aphids,
        Farms,
        /// <summary>Todo bloco sólido dentro da área pintada dos setores (antes das paredes em volta).</summary>
        Sectors,
        Walls,
        /// <summary>Atrás da rainha (se ela morrer, é game over); depois das paredes, antes de ir embora.</summary>
        Queen,
        Leave,
    }

    /// <summary>Em que ponto está o ataque.</summary>
    public enum RaidState : byte
    {
        /// <summary>Sem ataque (contando o tempo até o próximo, se os ataques já começaram).</summary>
        Calm,
        /// <summary>Alerta na tela: os cupins chegam quando o aviso acaba.</summary>
        Warning,
        /// <summary>Cupins no mapa.</summary>
        Active,
    }

    /// <summary>
    /// Um cupim. O corpo (<see cref="Body"/>) é um <see cref="Ant"/> só para reaproveitar a navegação e o desenho das
    /// formigas (posição, passo, apoio); ele nunca entra em <see cref="AntColony.Ants"/>. Não vai para o save.
    /// </summary>
    public sealed class Termite
    {
        public readonly Ant Body = new Ant();
        public TermitePhase Phase;
        public float Health;
        /// <summary>Segundos que faltam no voo ou no pouso.</summary>
        public float PhaseTimer;
        /// <summary>Tempo simulado em que o voo (ou o pouso) começou e de onde (células) o cupim veio voando: só desenho.</summary>
        public float PhaseStart;
        public float PhaseDuration;
        public float FlyFromX, FlyFromY;
        /// <summary>Tempo até a próxima mordida.</summary>
        public float AttackTimer;
        /// <summary>Destruindo: célula (índice da grade) e, na cria, o id dela; -1 = nada.</summary>
        public int WorkCell = -1;
        public int WorkBrood = -1;
        /// <summary>Tempo simulado da última mordida levada (o desenho pisca).</summary>
        public float LastHitTime = float.NegativeInfinity;
        /// <summary>Partes do corpo que ainda dá para tirar (morto).</summary>
        public int LeavesLeft, RemainsLeft;
        /// <summary>Segue um caminho cavado até o objetivo (guardado no corpo, como a reserva das formigas).</summary>
        public bool Tunneling;
        /// <summary>A célula em <see cref="WorkCell"/> é terra do caminho (vira túnel), não o alvo do objetivo.</summary>
        public bool DiggingPath;
        /// <summary>Formigas desmembrando este corpo neste passo (recontado a cada Tick).</summary>
        public int Workers;
        /// <summary>Formigas mordendo este cupim (recontado a cada Tick, e +1 na hora em que uma começa): as demais escolhem outro alvo.</summary>
        public int Attackers;
        /// <summary>
        /// Pulgão preto (pedido do desenvolvedor em 2026-10-22): o pulgão da fazenda que um cupim mordeu muda de lado e ataca as
        /// formigas até morrer. Usa o corpo e o combate dos cupins, mas não segue o objetivo do ataque nem vai embora.
        /// </summary>
        public bool Aphid;

        public bool Alive => Phase != TermitePhase.Dead;

        /// <summary>Já pousou e está vivo: entra no combate.</summary>
        public bool OnGround => Phase == TermitePhase.Raiding || Phase == TermitePhase.Leaving || Phase == TermitePhase.Landing;

        public int X => Body.X;
        public int Y => Body.Y;
    }

    /// <summary>
    /// Cuspe de uma cuspideira a caminho do cupim (pedido do desenvolvedor em 2026-10-21). Segue o alvo: o desenho vai da
    /// célula de onde saiu até a posição atual dele. Não vai para o save.
    /// </summary>
    public sealed class SpitShot
    {
        public readonly int FromX, FromY;
        public readonly Termite Target;
        /// <summary>Segundos no ar até acertar (distância ÷ <see cref="RaidSettings.SpitSpeed"/>).</summary>
        public readonly float Duration;
        public float Elapsed;
        /// <summary>Multiplicador do dano pela arma da cuspideira que atirou (documento 45).</summary>
        public readonly float DamageFactor;

        public SpitShot(int fromX, int fromY, Termite target, float duration, float damageFactor = 1f)
        {
            DamageFactor = damageFactor;
            FromX = fromX;
            FromY = fromY;
            Target = target;
            Duration = duration;
        }

        /// <summary>Fração do caminho já feita (0 a 1).</summary>
        public float Progress => Duration > 0f ? UnityEngine.Mathf.Clamp01(Elapsed / Duration) : 1f;
    }
}
