using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Fabula.Incrementant.Texts
{
    /// <summary>
    /// Textos do jogo por código, em cima do Unity Localization. Tudo mora numa tabela de strings só (<see cref="TableName"/>,
    /// uma coluna por idioma); a chave é a identidade do texto e o idioma sai da configuração (<c>GameSettingsData.Language</c>).
    /// As entradas não usam Smart Format: <c>{0}</c> é preenchido aqui com cultura invariante, então o resultado não muda
    /// com o idioma do sistema. Uma chave sem entrada devolve a própria chave (visível na tela, nunca exceção).
    /// Os textos estáticos das telas não passam por aqui: ficam com um <c>LocalizeStringEvent</c> montado pelo setup do HUD.
    /// </summary>
    public static class Loc
    {
        public const string TableName = "Game";
        public const string DefaultCode = "pt-BR";

        /// <summary>Idiomas oferecidos, na ordem do seletor. O primeiro é o padrão e a fonte dos textos.</summary>
        public static readonly string[] Codes = { DefaultCode, "en" };

        static readonly string[] NativeNames = { "PORTUGUÊS", "ENGLISH" };
        static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();
        static string _wanted = DefaultCode;
        static bool _hooked;
        static bool _warned;

        /// <summary>Disparado depois que o idioma muda e o cache de textos já foi limpo.</summary>
        public static event Action LanguageChanged;

        /// <summary>Código do idioma em uso (o escolhido, mesmo que a localização ainda esteja inicializando).</summary>
        public static string Code => _wanted;

        public static bool IsSupported(string code) => IndexOf(code) >= 0;

        public static int IndexOf(string code) => Array.IndexOf(Codes, code);

        /// <summary>Nome do idioma na própria língua, para o seletor.</summary>
        public static string NativeName(string code)
        {
            int i = IndexOf(code);
            return i >= 0 ? NativeNames[i] : code;
        }

        /// <summary>Código seguinte da lista, voltando ao primeiro depois do último.</summary>
        public static string Step(string code, int step)
        {
            int n = Codes.Length;
            int i = Mathf.Max(0, IndexOf(code));
            return Codes[((i + step) % n + n) % n];
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            // Com o domain reload desligado os estáticos sobrevivem ao Play anterior.
            Cache.Clear();
            LanguageChanged = null;
            _wanted = DefaultCode;
            _hooked = false;
            _warned = false;
        }

        /// <summary>
        /// Escolhe o idioma. Se o Unity Localization ainda está inicializando (primeira chamada, antes da primeira cena),
        /// a escolha é aplicada assim que ele terminar. Código desconhecido volta ao padrão.
        /// </summary>
        public static void SetLanguage(string code)
        {
            if (!IsSupported(code))
                code = DefaultCode;
            _wanted = code;
            if (!Application.isPlaying)
                return;
            var init = LocalizationSettings.InitializationOperation;
            if (init.IsDone)
                ApplyWanted();
            else
                init.Completed += _ => ApplyWanted();
        }

        static void ApplyWanted()
        {
            Hook();
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(_wanted));
            if (locale == null)
            {
                Debug.LogWarning($"[Loc] Idioma '{_wanted}' não existe nos Locales (rode Incrementant/Setup/Configurar localização).");
                return;
            }
            if (LocalizationSettings.SelectedLocale != locale)
                LocalizationSettings.SelectedLocale = locale; // dispara SelectedLocaleChanged
            else
                OnLocaleChanged(locale);
        }

        static void Hook()
        {
            if (_hooked)
                return;
            _hooked = true;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }

        static void OnLocaleChanged(Locale _)
        {
            Cache.Clear();
            LanguageChanged?.Invoke();
        }

        // Nós das árvores: o texto-fonte (português) fica nas definições em código; as demais línguas, na tabela.
        public static string QueenName(Colony.QueenNode node) => T("queen." + node + ".name");

        public static string QueenEffect(Colony.QueenNode node) => T("queen." + node + ".effect");

        public static string HiveName(string id) => T("hive." + id + ".name");

        public static string HiveEffect(string id) => T("hive." + id + ".effect");

        /// <summary>Câmaras e galerias novas do documento 39 (id do <c>UpgradeCatalog</c>).</summary>
        public static string UpgradeName(string id) => T("upgrade." + id + ".name");

        public static string UpgradeEffect(string id) => T("upgrade." + id + ".effect");

        /// <summary>Texto da chave no idioma atual (o modelo cru, com os <c>{0}</c> ainda no lugar).</summary>
        public static string T(string key)
        {
            if (Cache.TryGetValue(key, out string cached))
                return cached;
            string value = Lookup(key);
            // Só guarda o que achou: durante a inicialização o resultado pode ser provisório.
            if (value != null)
                Cache[key] = value;
            return value ?? key;
        }

        /// <summary>Texto da chave com <c>{0}</c>, <c>{1}</c>… preenchidos pelos argumentos (cultura invariante).</summary>
        public static string T(string key, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, T(key), args);

        static string Lookup(string key)
        {
            try
            {
                var result = LocalizationSettings.StringDatabase.GetTableEntry(TableName, key);
                return result.Entry?.Value;
            }
            catch (Exception e)
            {
                if (!_warned)
                {
                    _warned = true;
                    Debug.LogWarning($"[Loc] Não consegui ler a tabela '{TableName}': {e.Message}");
                }
                return null;
            }
        }
    }
}
