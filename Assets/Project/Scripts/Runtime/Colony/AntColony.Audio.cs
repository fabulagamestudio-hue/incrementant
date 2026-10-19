using System;
using Fabula.Incrementant.Audio;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Gancho de som da colônia. A simulação não conhece o áudio: só avisa o que aconteceu, e quem escuta
    /// (<c>AntFarmGame</c>, fora do menu) decide tocar. Sem ouvintes (testes, fundo do menu) não custa nada.
    /// </summary>
    public sealed partial class AntColony
    {
        /// <summary>Algo sonoro aconteceu na colônia (escavar, nascer, ataque...). Inscreva e remova de forma simétrica.</summary>
        public event Action<SfxId> Sfx;

        void Emit(SfxId id) => Sfx?.Invoke(id);
    }
}
