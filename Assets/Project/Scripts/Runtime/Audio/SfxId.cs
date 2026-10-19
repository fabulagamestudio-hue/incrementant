namespace Fabula.Incrementant.Audio
{
    /// <summary>Efeitos sonoros do jogo. Cada valor tem um som sintetizado em <see cref="SfxSynth"/> e regras de volume em <see cref="GameAudio"/>.</summary>
    public enum SfxId
    {
        // Interface
        UiClick,
        UiConfirm,
        UiDeny,
        UiTick,
        Mark,
        Unmark,
        Buy,
        // Colônia
        Dig,
        Drop,
        Build,
        Plant,
        EggLaid,
        AntBorn,
        AntDie,
        Drowned,
        // Ataques de cupins
        RaidWarning,
        RaidStart,
        RaidEnd,
        TermiteHit,
        TermiteKilled,
        AntHurt,
        Crumble,
        GameOver,
        // Ambiente (em laço)
        Rain,
        Count,
    }
}
