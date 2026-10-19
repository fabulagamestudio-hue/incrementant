using UnityEngine;

namespace Fabula.Incrementant.Tutorial
{
    /// <summary>
    /// Marca se o jogador já viu (ou pulou) o tutorial. Fica em <see cref="PlayerPrefs"/> e não em <c>settings.json</c>
    /// para o botão PADRÃO das configurações não trazer o tutorial de volta.
    /// </summary>
    public static class TutorialProgress
    {
        public const string Key = "Incrementant.TutorialDone";

        public static bool IsDone => PlayerPrefs.GetInt(Key, 0) != 0;

        public static void MarkDone()
        {
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Faz o tutorial voltar na próxima abertura do jogo.</summary>
        public static void Reset()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
