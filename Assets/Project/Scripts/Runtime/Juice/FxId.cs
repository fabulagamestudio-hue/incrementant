namespace Fabula.Incrementant.Juice
{
    /// <summary>Fatos visíveis da simulação que viram efeito no mundo (ver <c>AntColony.Fx</c> e <see cref="WorldFx"/>).</summary>
    public enum FxId
    {
        /// <summary>Formiga tirou terra de uma célula.</summary>
        Dig,
        /// <summary>Terra despejada fora do formigueiro.</summary>
        Drop,
        /// <summary>Pixel de obra encaixado.</summary>
        Build,
        /// <summary>Formiga nasceu.</summary>
        Hatch,
        /// <summary>Cupim ferido.</summary>
        TermiteHit,
        /// <summary>Cupim morto.</summary>
        TermiteKilled,
        /// <summary>A rainha consumiu um nutriente (ganho para o contador).</summary>
        NutrientGain,
        /// <summary>Formiga ferida por um cupim.</summary>
        AntHurt,
    }
}
