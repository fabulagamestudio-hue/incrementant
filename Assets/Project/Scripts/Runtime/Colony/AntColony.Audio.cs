using System;
using Fabula.Incrementant.Audio;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Gancho de som da colônia. A simulação não conhece o áudio: só avisa o que aconteceu e onde, e quem escuta
    /// (<c>AntFarmGame</c>, fora do menu) decide tocar. Sem ouvintes (testes, fundo do menu) não custa nada.
    /// </summary>
    public sealed partial class AntColony
    {
        /// <summary>
        /// Algo sonoro aconteceu na colônia (escavar, nascer, ataque...), com a célula de origem; <c>null</c> = som da colônia
        /// inteira (compra, aviso de ataque, game over), ouvido igual em qualquer ponto do mapa. Inscreva e remova de forma simétrica.
        /// </summary>
        public event Action<SfxId, Vector2Int?> Sfx;

        void Emit(SfxId id) => Sfx?.Invoke(id, null);

        void Emit(SfxId id, int x, int y) => Sfx?.Invoke(id, new Vector2Int(x, y));
    }
}
