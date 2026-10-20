using System;
using System.Collections.Generic;

namespace Fabula.Incrementant.Upgrades
{
    /// <summary>
    /// Níveis comprados das galerias e câmaras do <see cref="UpgradeCatalog"/>, por id textual ("O+1", "C1"; câmara comprada =
    /// nível 1): é o estado que vai no save. Só nós com sistema (<see cref="UpgradeBoard.HasSystem"/>) e fora da árvore da
    /// rainha (que guarda os dela em <see cref="Colony.QueenTree"/>) têm nível.
    /// </summary>
    public sealed class UpgradeLevels
    {
        readonly Dictionary<string, int> _levels = new Dictionary<string, int>();

        public int Level(string id) => _levels.TryGetValue(id, out int level) ? level : 0;

        public void Set(string id, int level)
        {
            if (level > 0)
                _levels[id] = level;
            else
                _levels.Remove(id);
        }

        /// <summary>Ids com nível maior que zero e os níveis, na mesma ordem (formato do save).</summary>
        public void Export(out string[] ids, out int[] levels)
        {
            ids = new string[_levels.Count];
            levels = new int[_levels.Count];
            int i = 0;
            foreach (KeyValuePair<string, int> pair in _levels)
            {
                ids[i] = pair.Key;
                levels[i++] = pair.Value;
            }
        }

        /// <summary>Substitui os níveis. Ids que o catálogo não conhece ou galerias sem sistema são descartados; o nível respeita o teto.</summary>
        public void Restore(string[] ids, int[] levels)
        {
            _levels.Clear();
            if (ids == null || levels == null)
                return;
            for (int i = 0; i < Math.Min(ids.Length, levels.Length); i++)
            {
                UpgradeDef def = UpgradeCatalog.Find(ids[i]);
                if (def == null || def.QueenNode.HasValue || !UpgradeBoard.HasSystem(def))
                    continue;
                int max = def.Kind == UpgradeKind.Chamber ? 1 : def.Capped ? def.Cap : int.MaxValue;
                Set(def.Id, Math.Min(levels[i], max));
            }
        }

        /// <summary>
        /// Multiplicador do atributo da galeria no nível atual, relativo ao valor base (1 sem níveis; 1,04 com um nível de
        /// 4% por nível). Só para galerias em modo %: as absolutas somam <see cref="UpgradeCatalog.ValueAt"/> direto.
        /// </summary>
        public float Multiplier(string id)
        {
            UpgradeDef def = UpgradeCatalog.Get(id);
            return UpgradeCatalog.ValueAt(def, Level(id)) / def.Base;
        }
    }
}
