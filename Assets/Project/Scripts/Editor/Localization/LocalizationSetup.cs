using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fabula.Incrementant.Texts;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace Fabula.Incrementant.EditorTools.Localization
{
    /// <summary>
    /// Cria e mantém a infraestrutura do Unity Localization do projeto: Localization Settings, os Locales (pt-BR e en) e a
    /// tabela de strings <see cref="Loc.TableName"/>, preenchida a partir de <see cref="LocalizationCatalog"/>. Idempotente:
    /// rodar de novo só acrescenta o que falta e atualiza os textos. Também liga um <c>LocalizeStringEvent</c> a cada rótulo
    /// estático do HUD (<see cref="Bind"/>), usado pelo setup do HUD e do menu principal.
    /// </summary>
    public static class LocalizationSetup
    {
        public const string Folder = "Assets/Project/Localization";
        const string TablesFolder = Folder + "/Tables";
        const string LocalesFolder = Folder + "/Locales";
        const string SettingsPath = Folder + "/Localization Settings.asset";

        static StringTableCollection _collection;
        static Dictionary<string, string> _keyByPt;
        static Dictionary<string, string> _keyByPtUpper;
        static Dictionary<string, LocalizationCatalog.Entry> _entryByKey;
        static readonly HashSet<string> Missing = new HashSet<string>();

        [MenuItem("Incrementant/Setup/Configurar localização")]
        public static void Run()
        {
            Prepare(force: true);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LocalizationSetup] Tabela '{Loc.TableName}' com {LocalizationCatalog.All.Count} textos em {string.Join(", ", Loc.Codes)}.");
        }

        /// <summary>Garante settings, Locales e tabela com todo o catálogo. Chamado pelos setups de cena antes de montar a UI.</summary>
        public static void Prepare(bool force = false)
        {
            if (_collection != null && !force)
                return;
            _keyByPt = null;
            Missing.Clear();
            Directory(Folder);
            Directory(TablesFolder);
            Directory(LocalesFolder);
            EnsureLocales();
            EnsureSettings();
            _collection = LocalizationEditorSettings.GetStringTableCollection(Loc.TableName)
                          ?? LocalizationEditorSettings.CreateStringTableCollection(Loc.TableName, TablesFolder);
            // Chaves automáticas de execuções anteriores saem; as que ainda forem precisas voltam em KeyFor.
            foreach (string stale in _collection.SharedData.Entries.Select(e => e.Key).Where(k => k.StartsWith("auto.")).ToList())
                _collection.RemoveEntry(stale);
            foreach (LocalizationCatalog.Entry entry in LocalizationCatalog.All)
                Write(entry.Key, entry.Pt, entry.En);
            Flush();
        }

        static void Directory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            Directory(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        static void EnsureLocales()
        {
            foreach (string code in Loc.Codes)
            {
                if (LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code)) != null)
                    continue;
                Locale locale = Locale.CreateLocale(new LocaleIdentifier(code));
                locale.name = code;
                AssetDatabase.CreateAsset(locale, $"{LocalesFolder}/{code}.asset");
                LocalizationEditorSettings.AddLocale(locale, false);
            }
            AssetDatabase.SaveAssets();
        }

        static void EnsureSettings()
        {
            LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
                if (settings == null)
                {
                    settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                    AssetDatabase.CreateAsset(settings, SettingsPath);
                }
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }
            // O idioma vem de GameSettings (settings.json), não do sistema operacional: começa sempre no padrão do jogo.
            IList<IStartupLocaleSelector> selectors = settings.GetStartupLocaleSelectors();
            if (selectors.Count != 1 || !(selectors[0] is SpecificLocaleSelector))
            {
                selectors.Clear();
                selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier(Loc.DefaultCode) });
                EditorUtility.SetDirty(settings);
            }
        }

        static void Write(string key, string pt, string en)
        {
            SharedTableData.SharedTableEntry shared = _collection.SharedData.GetEntry(key) ?? _collection.SharedData.AddKey(key);
            Set(Loc.DefaultCode, shared.Id, pt);
            Set("en", shared.Id, en);
        }

        static void Set(string code, long id, string value)
        {
            var table = (StringTable)_collection.GetTable(new LocaleIdentifier(code));
            StringTableEntry entry = table.GetEntry(id) ?? table.AddEntry(id, value);
            entry.Value = value;
            // Sem Smart Format: o código preenche {0} com string.Format (ver Loc).
            entry.IsSmart = false;
        }

        static void Flush()
        {
            // Salva só os assets da tabela (SaveAssets geral reconstrói os Text da cena aberta e eles tentam ler a tabela no meio da escrita).
            EditorUtility.SetDirty(_collection.SharedData);
            AssetDatabase.SaveAssetIfDirty(_collection.SharedData);
            foreach (LocalizationTable table in _collection.StringTables)
            {
                EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssetIfDirty(table);
            }
            EditorUtility.SetDirty(_collection);
            AssetDatabase.SaveAssetIfDirty(_collection);
        }

        static void BuildMaps()
        {
            if (_keyByPt != null)
                return;
            _keyByPt = new Dictionary<string, string>(StringComparer.Ordinal);
            _keyByPtUpper = new Dictionary<string, string>(StringComparer.Ordinal);
            _entryByKey = new Dictionary<string, LocalizationCatalog.Entry>(StringComparer.Ordinal);
            foreach (LocalizationCatalog.Entry e in LocalizationCatalog.All)
            {
                _entryByKey[e.Key] = e;
                // Em empate o primeiro vence: os textos iguais em telas diferentes têm a mesma tradução.
                if (!_keyByPt.ContainsKey(e.Pt))
                    _keyByPt[e.Pt] = e.Key;
                string upper = e.Pt.ToUpperInvariant();
                if (!_keyByPtUpper.ContainsKey(upper))
                    _keyByPtUpper[upper] = e.Key;
            }
        }

        /// <summary>
        /// Chave de um texto estático em português: a do catálogo, a variante em maiúsculas dela (<c>.U</c>, quando a tela mostra
        /// o texto em caixa alta) ou, sem correspondência, uma chave <c>auto.*</c> criada na hora com o inglês igual ao português
        /// (avisada no Console, para entrar no catálogo).
        /// </summary>
        public static string KeyFor(string pt)
        {
            BuildMaps();
            if (_keyByPt.TryGetValue(pt, out string key))
                return key;
            if (_keyByPtUpper.TryGetValue(pt, out key))
            {
                LocalizationCatalog.Entry source = _entryByKey[key];
                string upperKey = key + ".U";
                Write(upperKey, pt, source.En.ToUpperInvariant());
                Flush();
                return upperKey;
            }
            string auto = "auto." + Slug(pt);
            if (Missing.Add(auto))
                Debug.LogWarning($"[LocalizationSetup] Sem tradução no catálogo para \"{pt}\": criei '{auto}' com o mesmo texto em inglês. Acrescente em LocalizationCatalog.");
            Write(auto, pt, pt);
            Flush();
            return auto;
        }

        /// <summary>Texto igual em qualquer idioma (velocidade "2X", id de nó "Q0", números e símbolos): não vai para a tabela.</summary>
        static bool IsNeutral(string text) => System.Text.RegularExpressions.Regex.IsMatch(text, @"^([A-Z]\d|\d+X|[\d\W_]+)$");

        static string Slug(string text)
        {
            var chars = text.ToLowerInvariant().Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray();
            string slug = new string(chars).Trim('_');
            while (slug.Contains("__"))
                slug = slug.Replace("__", "_");
            return slug.Length > 40 ? slug.Substring(0, 40) : slug;
        }

        /// <summary>
        /// Liga o <paramref name="text"/> à tabela: um <see cref="LocalizeStringEvent"/> escreve o texto do idioma atual em
        /// <c>Text.text</c> quando o objeto liga e quando o idioma muda. Texto vazio fica de fora (é preenchido por código).
        /// Idempotente: reaproveita o evento que já existe no objeto.
        /// </summary>
        public static void Bind(Text text, string pt)
        {
            if (text == null || string.IsNullOrEmpty(pt) || IsNeutral(pt))
                return;
            Prepare();
            string key = KeyFor(pt);
            if (!text.TryGetComponent(out LocalizeStringEvent localize))
            {
                localize = text.gameObject.AddComponent<LocalizeStringEvent>();
                MethodInfo setter = typeof(Text).GetProperty(nameof(Text.text)).GetSetMethod();
                var action = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), text, setter);
                UnityEventTools.AddPersistentListener(localize.OnUpdateString, action);
            }
            localize.StringReference = new LocalizedString(Loc.TableName, key);
            // Mantém no objeto o texto-fonte (português), que é o que aparece antes da localização iniciar.
            text.text = pt;
        }
    }
}
