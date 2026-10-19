using System;
using Fabula.Incrementant.Juice;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Gancho de efeito visual da colônia, irmão de <see cref="Sfx"/>. A simulação só avisa o que aconteceu e onde
    /// (célula da grade e material, para a cor); quem escuta (<c>AntFarmGame</c>, fora do menu) decide o visual.
    /// Sem ouvintes (testes, fundo do menu) não custa nada.
    /// </summary>
    public sealed partial class AntColony
    {
        /// <summary>Algo visível aconteceu na célula dada. <c>CellMaterial.Empty</c> = sem cor própria. Inscreva e remova de forma simétrica.</summary>
        public event Action<FxId, Vector2Int, CellMaterial> Fx;

        void EmitFx(FxId id, int x, int y, CellMaterial material = CellMaterial.Empty) =>
            Fx?.Invoke(id, new Vector2Int(x, y), material);
    }
}
