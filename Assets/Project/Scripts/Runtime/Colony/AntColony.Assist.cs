using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Mutirão de escavação (pedido do desenvolvedor em 2026-10-24): várias formigas podem golpear o mesmo bloco e o trabalho
    /// delas soma; só a primeira, a dona do bloco, pega o pixel. A ajudante fica ao lado do bloco no estado de escavar, mas o
    /// tempo corre na dona (<see cref="Ant.Cooldown"/>), então a rachadura (<see cref="UpdateHits"/>) cresce mais rápido sem
    /// código à parte. Ligado por <see cref="ColonySettings.AssistDig"/>.
    /// </summary>
    public sealed partial class AntColony
    {
        // Quem está soltando cada bloco inteiro (índice da célula → dona). Entradas velhas são conferidas ao usar.
        readonly System.Collections.Generic.Dictionary<int, Ant> _digOwners = new System.Collections.Generic.Dictionary<int, Ant>();

        /// <summary>Menos que isto de tempo restante e a ajuda nem chega: a dona termina antes.</summary>
        const float AssistMinRemaining = 1f;

        /// <summary>Ajudantes que cada dona tem neste passo (recontados a cada Tick em <see cref="CountWork"/>).</summary>
        readonly System.Collections.Generic.Dictionary<Ant, int> _assistHelpers = new System.Collections.Generic.Dictionary<Ant, int>();

        /// <summary>Ajudantes por bloco, além da dona (as oito células em volta não cabem mais que isso sem formigas empilhadas).</summary>
        const int AssistMax = 3;

        bool IsAssistable(Ant owner, int cell)
        {
            if (owner.State != AntState.Digging || owner.AssistOf != null || owner.HitCell != cell || owner.Cooldown <= AssistMinRemaining)
                return false;
            _assistHelpers.TryGetValue(owner, out int helpers);
            return helpers < AssistMax;
        }

        /// <summary>O bloco está sendo solto por uma dona que ainda aceita ajuda (os campos de escavação apontam para ele).</summary>
        bool IsAssistableBlock(int cell) =>
            _settings.AssistDig && _digOwners.TryGetValue(cell, out Ant owner) && IsAssistable(owner, cell);

        /// <summary>Entra no mutirão do bloco da célula, se houver dona que aceite ajuda. <c>false</c> = o bloco é livre (ou não aceita).</summary>
        bool JoinAssist(Ant ant, int cell)
        {
            if (!_settings.AssistDig || !_digOwners.TryGetValue(cell, out Ant owner) || ReferenceEquals(owner, ant) || !IsAssistable(owner, cell))
                return false;
            ant.ClearSeek();
            ant.AssistOf = owner;
            ant.DigX = cell % _grid.Width;
            ant.DigY = cell / _grid.Width;
            ant.HitCell = -1;
            if (ant.DigX != ant.X)
                ant.FacingX = ant.DigX > ant.X ? 1 : -1;
            ant.State = AntState.Digging;
            ant.Cooldown = owner.Cooldown;
            _assistHelpers.TryGetValue(owner, out int helpers);
            _assistHelpers[owner] = helpers + 1;
            return true;
        }

        /// <summary>
        /// A formiga ociosa sem bloco livre ao lado ajuda quem está soltando um bloco vizinho. <c>false</c> com o mutirão
        /// desligado, mão ocupada ou nenhum bloco em andamento ao alcance.
        /// </summary>
        bool TryAssistDig(Ant ant)
        {
            if (!_settings.AssistDig || _digOwners.Count == 0)
                return false;
            for (int k = 0; k < 8; k++)
            {
                int nx = ant.X + AntNavigation.Dx[k], ny = ant.Y + AntNavigation.Dy[k];
                if (!_grid.InBounds(nx, ny))
                    continue;
                if (JoinAssist(ant, _grid.ToIndex(nx, ny)))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Passo da ajudante: soma o trabalho dela ao tempo da dona (mais rápida que a dona, soma mais). Sem dona no mesmo
        /// bloco, ou longe dele, volta a procurar o que fazer.
        /// </summary>
        void UpdateAssist(Ant ant, float deltaTime)
        {
            Ant owner = ant.AssistOf;
            int cell = _grid.ToIndex(ant.DigX, ant.DigY);
            bool adjacent = Mathf.Abs(ant.DigX - ant.X) <= 1 && Mathf.Abs(ant.DigY - ant.Y) <= 1;
            if (!adjacent || !_digOwners.TryGetValue(cell, out Ant current) || !ReferenceEquals(current, owner) ||
                owner.State != AntState.Digging || owner.AssistOf != null || owner.HitCell != cell)
            {
                ant.AssistOf = null;
                ant.State = AntState.Idle;
                ant.Cooldown = 0f;
                return;
            }
            CellMaterial material = _grid.Get(ant.DigX, ant.DigY);
            float rate = Mathf.Clamp(WorkTimeFactor(owner, material) / Mathf.Max(0.01f, WorkTimeFactor(ant, material)), 0.25f, 4f);
            owner.Cooldown -= deltaTime * rate;
            ant.Cooldown = owner.Cooldown;
        }
    }
}
