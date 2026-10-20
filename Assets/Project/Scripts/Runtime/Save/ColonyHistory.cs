using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fabula.Incrementant.Save
{
    /// <summary>Uma colônia do histórico: a seed a identifica, a duração é o tempo de jogo (<c>MatchTimer</c>) mais longo visto.</summary>
    [Serializable]
    public sealed class ColonyRecord
    {
        public int Seed;
        public double Seconds;
        /// <summary>A colônia acabou (rainha morta). Falso: o jogador a deixou (mapa novo ou outro save carregado).</summary>
        public bool GameOver;
        public string EndedUtc;
    }

    /// <summary>
    /// Histórico de durações das colônias (KI-040), em <c>Saves/colonies.json</c> ao lado do save rápido. Uma entrada por seed:
    /// regravar a mesma colônia (recarregar um save antigo dela) só substitui a duração se for maior.
    /// </summary>
    public static class ColonyHistory
    {
        public const string FileName = "colonies.json";

        // Só para o JsonUtility, que não lê lista na raiz.
        [Serializable]
        sealed class Document
        {
            public List<ColonyRecord> Colonies = new List<ColonyRecord>();
        }

        public static string FilePath => Path.Combine(SaveStorage.DirectoryPath, FileName);

        /// <summary>Todas as colônias registradas, da mais antiga para a mais recente. Arquivo ausente ou ilegível: lista vazia.</summary>
        public static List<ColonyRecord> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonUtility.FromJson<Document>(File.ReadAllText(FilePath))?.Colonies ?? new List<ColonyRecord>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ColonyHistory: não foi possível ler '{FilePath}': {e.Message}");
            }
            return new List<ColonyRecord>();
        }

        /// <summary>Registra (ou atualiza) a colônia da seed. Não faz nada com duração zero.</summary>
        public static void Record(int seed, double seconds, bool gameOver)
        {
            if (seconds <= 0.0)
                return;
            try
            {
                List<ColonyRecord> all = Load();
                ColonyRecord record = all.Find(r => r.Seed == seed);
                if (record == null)
                {
                    record = new ColonyRecord { Seed = seed };
                    all.Add(record);
                }
                record.Seconds = Math.Max(record.Seconds, seconds);
                record.GameOver |= gameOver;
                record.EndedUtc = DateTime.UtcNow.ToString("o");
                Directory.CreateDirectory(SaveStorage.DirectoryPath);
                string temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(new Document { Colonies = all }, true));
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                File.Move(temp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ColonyHistory: não foi possível gravar '{FilePath}': {e.Message}");
            }
        }
    }
}
