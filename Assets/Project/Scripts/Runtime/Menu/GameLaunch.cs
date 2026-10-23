namespace Fabula.Incrementant.Menu
{
    /// <summary>
    /// Recado do menu principal para a cena da partida: começar uma colônia nova ou continuar a salva. Vive só na memória,
    /// porque o menu e a partida são cenas separadas; quem lê consome o pedido (vale uma vez por carregamento de cena).
    /// </summary>
    public static class GameLaunch
    {
        /// <summary>Nome da cena da partida (a mesma de <c>AntFarmSceneSetup.ScenePath</c>) e do menu, para <c>SceneManager.LoadScene</c>.</summary>
        public const string GameSceneName = "AntFarm";
        public const string MenuSceneName = "MainMenu";

        static bool _continueRequested;
        static bool _fromMenu;

        public static void RequestNew()
        {
            _continueRequested = false;
            _fromMenu = true;
        }

        public static void RequestContinue()
        {
            _continueRequested = true;
            _fromMenu = true;
        }

        /// <summary>
        /// Devolve se a cena da partida foi aberta pelo menu principal (Novo jogo ou Continuar) e zera o recado. Falso quando a
        /// cena AntFarm é aberta direto no Editor: aí o tutorial não roda.
        /// </summary>
        public static bool ConsumeFromMenu()
        {
            bool fromMenu = _fromMenu;
            _fromMenu = false;
            return fromMenu;
        }

        /// <summary>Devolve se o jogador pediu Continuar e zera o pedido.</summary>
        public static bool ConsumeContinue()
        {
            bool requested = _continueRequested;
            _continueRequested = false;
            return requested;
        }
    }
}
