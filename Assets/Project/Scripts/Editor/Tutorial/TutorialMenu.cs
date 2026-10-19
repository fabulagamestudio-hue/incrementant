using Fabula.Incrementant.Tutorial;
using UnityEditor;
using UnityEngine;

namespace Fabula.Incrementant.EditorTools.Tutorial
{
    /// <summary>Atalhos de Editor para rever o tutorial da primeira abertura.</summary>
    public static class TutorialMenu
    {
        [MenuItem("Incrementant/Tutorial/Mostrar o tutorial na próxima partida")]
        static void ResetTutorial()
        {
            TutorialProgress.Reset();
            Debug.Log("[Tutorial] Marca de 'já visto' apagada: o tutorial volta na próxima partida nova.");
        }
    }
}
