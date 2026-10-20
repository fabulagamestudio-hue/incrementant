using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Soterramento (KI-025, 2026-10-20). Terra que cai sobre a formiga não a empurra mais para cima: ela fica presa onde
    /// está, sem colisão com o terreno (<see cref="Ant.Buried"/>), parada e fora de combate, até o espaço em volta abrir.
    /// Com o nó O3 (Retirada de companheiras), operárias ociosas vão até a borda do túnel mais perto e a tiram de lá.
    /// </summary>
    public sealed partial class AntColony
    {
        /// <summary>Alcance (distância de tabuleiro) a partir de uma célula caminhável em que uma operária consegue tirar a soterrada.</summary>
        public const int RescueReach = 2;
        /// <summary>Segundos que a operária leva para tirar uma companheira da terra.</summary>
        public const float RescueSeconds = 1.5f;
        const float RescueSearchInterval = 1f;
        const int RescueSearchLimit = 3000;

        float _nextRescueSearch;
        readonly Dictionary<int, int> _rescueParent = new Dictionary<int, int>();
        readonly Queue<int> _rescueQueue = new Queue<int>();
        readonly List<Ant> _buriedScratch = new List<Ant>();

        /// <summary>Formigas presas na terra agora.</summary>
        public int BuriedCount
        {
            get
            {
                int count = 0;
                foreach (Ant ant in _ants)
                {
                    if (ant.Buried)
                        count++;
                }
                return count;
            }
        }

        /// <summary>Prende a formiga onde está: larga o trabalho, a carga e o que reservava.</summary>
        void Bury(Ant ant)
        {
            ReleaseDumpSide(ant);
            if (ant.State == AntState.FetchingEgg || ant.State == AntState.CarryingEgg || ant.State == AntState.MovingBrood)
                _brood.ReleaseEgg(ant.EggId, ant.X, ant.Y);
            ReleaseRescue(ant);
            ant.ClearLoad();
            ant.ClearSeek();
            ant.Errand = AntErrand.None;
            ant.StoreTarget = StorageKind.None;
            ant.State = AntState.Idle;
            ant.Biting = false;
            ant.Cooldown = 0f;
            ant.Buried = true;
            SnapVisual(ant);
        }

        /// <summary>Solta a ligação entre quem resgata e quem espera o resgate (qualquer um dos dois que saia).</summary>
        void ReleaseRescue(Ant ant)
        {
            if (ant.RescueTarget != null && ant.RescueTarget.RescuedBy == ant)
                ant.RescueTarget.RescuedBy = null;
            ant.RescueTarget = null;
            ant.RescuePath = null;
            if (ant.RescuedBy != null && ant.RescuedBy.RescueTarget == ant)
            {
                ant.RescuedBy.RescueTarget = null;
                ant.RescuedBy.RescuePath = null;
                if (ant.RescuedBy.State == AntState.Rescuing)
                {
                    ant.RescuedBy.Biting = false;
                    ant.RescuedBy.State = AntState.Idle;
                }
            }
            ant.RescuedBy = null;
        }

        /// <summary>Operária ociosa com O3: se há soterrada perto da borda de um túnel que ninguém atende, vai tirá-la.</summary>
        bool TryStartRescue(Ant ant)
        {
            if (ant.TaskRole != AntRole.Worker || !Tree.Owns(QueenNode.O3) || SimulationTime < _nextRescueSearch)
                return false;
            _buriedScratch.Clear();
            foreach (Ant other in _ants)
            {
                if (other.Buried && other.RescuedBy == null)
                    _buriedScratch.Add(other);
            }
            if (_buriedScratch.Count == 0)
                return false;
            if (!FindRescue(ant, out Ant target, out List<int> path))
            {
                _nextRescueSearch = SimulationTime + RescueSearchInterval;
                return false;
            }
            ant.State = AntState.Rescuing;
            ant.RescueTarget = target;
            ant.RescuePath = path;
            ant.Biting = false;
            target.RescuedBy = ant;
            UpdateRescuing(ant);
            return true;
        }

        /// <summary>Busca em largura pelas células caminháveis até uma que alcance uma soterrada; devolve o caminho (da formiga até lá).</summary>
        bool FindRescue(Ant ant, out Ant target, out List<int> path)
        {
            target = null;
            path = null;
            _rescueParent.Clear();
            _rescueQueue.Clear();
            int start = _grid.ToIndex(ant.X, ant.Y);
            _rescueParent[start] = -1;
            _rescueQueue.Enqueue(start);
            int width = _grid.Width;
            while (_rescueQueue.Count > 0 && _rescueParent.Count < RescueSearchLimit)
            {
                int cell = _rescueQueue.Dequeue();
                int cx = cell % width, cy = cell / width;
                foreach (Ant buried in _buriedScratch)
                {
                    if (Mathf.Max(Mathf.Abs(buried.X - cx), Mathf.Abs(buried.Y - cy)) > RescueReach)
                        continue;
                    target = buried;
                    path = new List<int>();
                    for (int c = cell; c != -1; c = _rescueParent[c])
                        path.Add(c);
                    path.Reverse();
                    return true;
                }
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + AntNavigation.Dx[k], ny = cy + AntNavigation.Dy[k];
                    if (nx < 0 || ny < 0 || nx >= width || ny >= _grid.Height || !AntNavigation.IsWalkable(_grid, nx, ny))
                        continue;
                    int next = _grid.ToIndex(nx, ny);
                    if (_rescueParent.ContainsKey(next))
                        continue;
                    _rescueParent[next] = cell;
                    _rescueQueue.Enqueue(next);
                }
            }
            return false;
        }

        /// <summary>Anda até a borda perto da soterrada, trabalha <see cref="RescueSeconds"/> e a solta na própria célula da operária.</summary>
        void UpdateRescuing(Ant ant)
        {
            Ant target = ant.RescueTarget;
            if (target == null || !target.Buried)
            {
                ant.Biting = false;
                ReleaseRescue(ant);
                ant.State = AntState.Idle;
                return;
            }
            if (ant.Biting)
            {
                ant.Biting = false;
                target.Buried = false;
                target.X = ant.X;
                target.Y = ant.Y;
                SnapVisual(target);
                target.Cooldown = 0f;
                target.RescuedBy = null;
                ant.RescueTarget = null;
                ant.RescuePath = null;
                ant.State = AntState.Idle;
                return;
            }
            if (Mathf.Max(Mathf.Abs(target.X - ant.X), Mathf.Abs(target.Y - ant.Y)) <= RescueReach)
            {
                ant.Biting = true;
                ant.DigX = target.X;
                ant.DigY = target.Y;
                if (target.X != ant.X)
                    ant.FacingX = target.X > ant.X ? 1 : -1;
                ant.Cooldown = RescueSeconds;
                return;
            }
            List<int> path = ant.RescuePath;
            int here = path != null ? path.IndexOf(_grid.ToIndex(ant.X, ant.Y)) : -1;
            if (here < 0 || here + 1 >= path.Count)
            {
                ReleaseRescue(ant);
                ant.State = AntState.Idle;
                return;
            }
            int nextCell = path[here + 1];
            int dx = nextCell % _grid.Width - ant.X, dy = nextCell / _grid.Width - ant.Y;
            for (int k = 0; k < 8; k++)
            {
                if (AntNavigation.Dx[k] == dx && AntNavigation.Dy[k] == dy && AntNavigation.IsWalkable(_grid, ant.X + dx, ant.Y + dy))
                {
                    MoveTo(ant, k);
                    return;
                }
            }
            ReleaseRescue(ant);
            ant.State = AntState.Idle;
        }
    }
}
