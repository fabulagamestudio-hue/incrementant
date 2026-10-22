using Iung.BuildAssistant.Config;
using Iung.BuildAssistant.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Fabula.Incrementant.UI
{
    /// <summary>
    /// Versão da build no canto inferior direito, em todas as cenas. O prefab (<see cref="ResourcePath"/>, montado por
    /// <c>Incrementant/Setup/Criar overlay de versão</c>) é instanciado uma vez antes da primeira cena e sobrevive às trocas
    /// (<c>DontDestroyOnLoad</c>). O texto vem do Build Assistant, que grava o rótulo no settings logo antes de compilar.
    /// A área do texto é um botão: barra os cliques no mundo por baixo e abre o <see cref="VersionHistoryScreen"/>.
    /// </summary>
    public sealed class BuildVersionOverlay : MonoBehaviour
    {
        public const string ResourcePath = "BuildVersionOverlay";

        [SerializeField] BuildAssistantSettings _settings;
        [SerializeField] Text _label;
        [SerializeField] Button _open;
        [SerializeField] VersionHistoryScreen _history;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Spawn()
        {
            var prefab = Resources.Load<BuildVersionOverlay>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[BuildVersionOverlay] Prefab 'Resources/{ResourcePath}' ausente; rode Incrementant/Setup/Criar overlay de versão.");
                return;
            }
            BuildVersionOverlay overlay = Instantiate(prefab);
            overlay.name = prefab.name;
            DontDestroyOnLoad(overlay.gameObject);
        }

        void Awake()
        {
            if (_label == null || _open == null || _history == null)
            {
                Debug.LogError($"[BuildVersionOverlay] '{name}' com referências faltando; rode Incrementant/Setup/Criar overlay de versão.", this);
                return;
            }
            _label.text = ResolveVersion(_settings);
            _history.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (_open != null)
                _open.onClick.AddListener(OpenHistory);
        }

        void OnDisable()
        {
            if (_open != null)
                _open.onClick.RemoveListener(OpenHistory);
        }

        void OpenHistory() => _history.Open(_settings != null ? _settings.RuntimeChangelog : null);

        /// <summary>
        /// Rótulo da última build feita pelo Build Assistant; sem ele, a última build com sucesso do histórico; sem
        /// histórico (build pelo menu da Unity ou projeto nunca construído), a versão do Player Settings.
        /// </summary>
        public static string ResolveVersion(BuildAssistantSettings settings)
        {
            if (settings != null)
            {
                if (!string.IsNullOrWhiteSpace(settings.RuntimeVersionLabel))
                    return settings.RuntimeVersionLabel;
                if (settings.RecentBuildHistory != null)
                {
                    foreach (BuildAssistantHistoryEntry entry in settings.RecentBuildHistory)
                    {
                        if (entry != null && entry.WasSuccessful && !string.IsNullOrWhiteSpace(entry.VersionLabel))
                            return entry.VersionLabel;
                    }
                }
            }
            return "v" + Application.version;
        }
    }
}
