using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Fabula.Incrementant.Texts;
using NUnit.Framework;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Fabula.Incrementant.Tests
{
    public sealed class LocalizationTests
    {
        static StringTableCollection Collection => LocalizationEditorSettings.GetStringTableCollection(Loc.TableName);

        [Test]
        public void TableExistsWithEveryLanguage()
        {
            Assert.IsNotNull(Collection, "Rode Incrementant/Setup/Configurar localização.");
            foreach (string code in Loc.Codes)
                Assert.IsNotNull(Collection.GetTable(new LocaleIdentifier(code)), code);
        }

        [Test]
        public void EveryLanguageHasEveryKeyTranslated()
        {
            foreach (string code in Loc.Codes)
            {
                var table = (StringTable)Collection.GetTable(new LocaleIdentifier(code));
                foreach (var key in Collection.SharedData.Entries)
                {
                    StringTableEntry entry = table.GetEntry(key.Id);
                    Assert.IsFalse(string.IsNullOrEmpty(entry?.Value), $"{code}: '{key.Key}' sem texto");
                }
            }
        }

        [Test]
        public void KeysUsedInRuntimeCodeExistInTheTable()
        {
            string root = Path.Combine(Application.dataPath, "Project/Scripts/Runtime");
            var keys = new System.Collections.Generic.HashSet<string>();
            var call = new Regex(@"Loc\.T\(""([^""]+)""");
            var literal = new Regex(@"""((?:tutorial|hud|raid|breed|queen|buy|tree|save|gameover|settings|pause|menu|role)\.[a-z0-9.]+)""");
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string text = File.ReadAllText(file);
                foreach (Match m in call.Matches(text)) keys.Add(m.Groups[1].Value);
                foreach (Match m in literal.Matches(text))
                    if (!m.Groups[1].Value.EndsWith(".json"))
                        keys.Add(m.Groups[1].Value);
            }
            Assert.Greater(keys.Count, 40);
            var known = Collection.SharedData.Entries.Select(e => e.Key).ToHashSet();
            foreach (string key in keys.OrderBy(k => k))
                Assert.IsTrue(known.Contains(key), $"Chave '{key}' usada no código não existe na tabela");
        }

        [Test]
        public void TemplatesKeepTheSamePlaceholdersInEveryLanguage()
        {
            var pt = (StringTable)Collection.GetTable(new LocaleIdentifier("pt-BR"));
            var en = (StringTable)Collection.GetTable(new LocaleIdentifier("en"));
            var placeholder = new Regex(@"\{\d+\}");
            foreach (var key in Collection.SharedData.Entries)
            {
                var a = placeholder.Matches(pt.GetEntry(key.Id).Value).Select(m => m.Value).OrderBy(x => x);
                var b = placeholder.Matches(en.GetEntry(key.Id).Value).Select(m => m.Value).OrderBy(x => x);
                CollectionAssert.AreEqual(a, b, key.Key);
            }
        }

        [Test]
        public void LanguageCycleWrapsAround()
        {
            Assert.AreEqual("en", Loc.Step("pt-BR", 1));
            Assert.AreEqual("pt-BR", Loc.Step("en", 1));
            Assert.AreEqual("en", Loc.Step("pt-BR", -1));
        }
    }
}
