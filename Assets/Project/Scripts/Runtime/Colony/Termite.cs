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
        Farms,
        Walls,
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
        /// <summary>Formigas desmembrando este corpo neste passo (recontado a cada Tick).</summary>
        public int Workers;

        public bool Alive => Phase != TermitePhase.Dead;

        /// <summary>Já pousou e está vivo: entra no combate.</summary>
        public bool OnGround => Phase == TermitePhase.Raiding || Phase == TermitePhase.Leaving || Phase == TermitePhase.Landing;

        public int X => Body.X;
        public int Y => Body.Y;
    }
}
