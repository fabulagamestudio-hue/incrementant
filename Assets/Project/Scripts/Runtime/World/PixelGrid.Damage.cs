using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    /// <summary>
    /// Dano dos blocos (REQ-072, pedido do desenvolvedor em 2026-10-23): quanto de uma célula sólida as formigas já golpearam
    /// para soltá-la, de 0 a 1. O render desenha uma rachadura que cresce com ele (<see cref="GetCrackStage"/>). Fica na
    /// célula, não na formiga: quem larga o bloco no meio deixa a rachadura, e a próxima continua dali. Some quando o material
    /// da célula muda ou o grão se move. Só as poucas células sendo golpeadas têm dano, então é um dicionário. Não vai para o save.
    /// </summary>
    public sealed partial class PixelGrid
    {
        /// <summary>Estágios da rachadura desenhados (1 a <see cref="CrackStages"/>; 0 = sem rachadura).</summary>
        public const int CrackStages = 4;

        readonly Dictionary<int, byte> _damage = new Dictionary<int, byte>();

        /// <summary>Células com dano agora.</summary>
        public int DamagedCount => _damage.Count;

        /// <summary>Quanto da célula já foi golpeado, de 0 a 1.</summary>
        public float GetDamage(int x, int y) =>
            InBounds(x, y) && _damage.TryGetValue(y * Width + x, out byte d) ? d / 255f : 0f;

        /// <summary>
        /// Estágio da rachadura: o dano em <see cref="CrackStages"/> + 1 faixas iguais, a primeira sem desenho (com 4 estágios,
        /// a primeira rachadura aparece com 20% do bloco golpeado).
        /// </summary>
        public int GetCrackStage(int x, int y) => InBounds(x, y) ? CrackStageAt(y * Width + x) : 0;

        int CrackStageAt(int index) =>
            _damage.TryGetValue(index, out byte d) ? Mathf.Min(CrackStages, d * (CrackStages + 1) / 256) : 0;

        /// <summary>
        /// Grava o dano da célula (só célula sólida). O dano não diminui: um valor menor que o atual é ignorado. Devolve
        /// false se nada mudou.
        /// </summary>
        public bool SetDamage(int x, int y, float damage)
        {
            if (!InBounds(x, y))
                return false;
            int i = y * Width + x;
            if (_cells[i] == CellMaterial.Empty)
                return false;
            byte value = (byte)Mathf.Clamp(Mathf.RoundToInt(damage * 255f), 0, 255);
            _damage.TryGetValue(i, out byte old);
            if (value <= old)
                return false;
            int stage = CrackStageAt(i);
            _damage[i] = value;
            // A rachadura só se desenha por estágio: o render só refaz a célula quando ele muda.
            if (CrackStageAt(i) != stage)
                NoteRender(i);
            return true;
        }

        void ClearDamageAt(int index)
        {
            if (_damage.Count > 0 && _damage.Remove(index))
                NoteRender(index);
        }
    }
}
