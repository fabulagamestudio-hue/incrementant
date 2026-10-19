using System.Collections.Generic;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.AntFarm
{
    /// <summary>
    /// Fundo do menu principal: escava um formigueiro já pronto (túneis e salas em volta do ninho inicial), marca as salas,
    /// põe uma colônia para trabalhar nele e, de tempos em tempos, marca um trecho novo para as escavadoras seguirem
    /// abrindo. É a simulação de verdade, sem input e sem ataques: só a colônia trabalhando. Criado pelo
    /// <see cref="AntFarmGame"/> quando ele está em modo vitrine.
    /// </summary>
    public sealed class MenuShowcase
    {
        // Coordenadas locais (células): x a partir da entrada do ninho, y a partir do chão da câmara inicial.
        // A câmara inicial (20x8) fica em (0, +4); a superfície, uns 30 acima.

        /// <summary>Sala: centro, semi-eixos e o que ela vira.</summary>
        readonly struct Room
        {
            public readonly Vector2Int Center;
            public readonly Vector2Int Radius;
            public readonly RoomKind Kind;

            public Room(int x, int y, int rx, int ry, RoomKind kind)
            {
                Center = new Vector2Int(x, y);
                Radius = new Vector2Int(rx, ry);
                Kind = kind;
            }
        }

        enum RoomKind { None, Queen, Nursery, Nutrient }

        static readonly Room[] Rooms =
        {
            new Room(0, 4, 8, 3, RoomKind.Queen),
            new Room(-34, -22, 13, 6, RoomKind.Nursery),
            new Room(44, -14, 15, 7, RoomKind.Nutrient),
            new Room(4, -52, 16, 6, RoomKind.None),
            new Room(-43, 8, 7, 4, RoomKind.None),
        };

        /// <summary>Túneis: raio e pontos da poligonal.</summary>
        static readonly (float radius, int[] points)[] Tunnels =
        {
            (2f, new[] { -8, 2, -18, -4, -26, -12, -32, -18 }),
            (2f, new[] { 8, 2, 22, -2, 32, -8, 38, -12 }),
            (2f, new[] { -34, -26, -30, -38, -16, -48, -8, -52 }),
            (2f, new[] { 46, -20, 50, -34, 36, -46, 20, -52 }),
            (1.8f, new[] { -6, 6, -22, 10, -34, 10, -42, 8 }),
            (1.6f, new[] { 4, -58, 10, -70, 22, -78 }),
            (1.6f, new[] { -40, -24, -56, -30, -60, -40 }),
        };

        /// <summary>Trechos que as escavadoras abrem, um por vez, a partir das pontas dos túneis sem saída.</summary>
        static readonly Vector2Int[] DigTargets =
        {
            new Vector2Int(26, -81), new Vector2Int(-62, -45), new Vector2Int(63, -14),
            new Vector2Int(31, -85), new Vector2Int(-60, -51), new Vector2Int(68, -17),
        };

        const float DigTargetRadius = 2.5f;
        const float SecondsBetweenDigs = 18f;

        static readonly (AntRole role, int count)[] Population =
        {
            (AntRole.Worker, 8), (AntRole.Gardener, 5), (AntRole.Carrier, 5),
            (AntRole.Cutter, 6), (AntRole.Digger, 5), (AntRole.Nurse, 4),
        };

        readonly PixelGrid _grid;
        readonly Vector2Int _origin;
        int _nextTarget;
        float _digTimer = 6f;

        MenuShowcase(PixelGrid grid, Vector2Int origin)
        {
            _grid = grid;
            _origin = origin;
        }

        /// <summary>Escava o formigueiro, marca as salas e povoa a colônia. Devolve o diretor, que segue marcando escavações.</summary>
        public static MenuShowcase Prepare(PixelGrid grid, AntColony colony, TerrainLayout layout, int seed)
        {
            var showcase = new MenuShowcase(grid, new Vector2Int(layout.EntranceX, layout.Nest.y));
            List<Vector2Int> open = showcase.Carve();
            showcase.MarkRooms();
            showcase.Populate(colony, open, seed);
            return showcase;
        }

        /// <summary>Avança o relógio da simulação: quando o trecho anterior acabou de ser aberto, marca o próximo.</summary>
        public void Tick(float deltaTime)
        {
            if (_nextTarget >= DigTargets.Length)
                return;
            _digTimer -= deltaTime;
            if (_digTimer > 0f || _grid.MarkedCount > 0)
                return;
            Vector2Int target = Local(DigTargets[_nextTarget++]);
            int radius = Mathf.CeilToInt(DigTargetRadius);
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy > DigTargetRadius * DigTargetRadius)
                    continue;
                int x = target.x + dx, y = target.y + dy;
                if (_grid.IsSolid(x, y) && _grid.Get(x, y) != CellMaterial.Bedrock)
                    _grid.SetMarked(x, y, true);
            }
            _digTimer = SecondsBetweenDigs;
        }

        Vector2Int Local(Vector2Int p) => new Vector2Int(_origin.x + p.x, _origin.y + p.y);

        List<Vector2Int> Carve()
        {
            var open = new List<Vector2Int>();
            var seen = new HashSet<int>();
            foreach (Room room in Rooms)
                StampEllipse(room, open, seen);
            foreach ((float radius, int[] points) in Tunnels)
            {
                for (int i = 0; i + 3 < points.Length; i += 2)
                    StampLine(new Vector2(points[i], points[i + 1]), new Vector2(points[i + 2], points[i + 3]), radius, open, seen);
            }
            SettleSand();
            return open;
        }

        void StampEllipse(Room room, List<Vector2Int> open, HashSet<int> seen)
        {
            Vector2Int c = Local(room.Center);
            int rx = room.Radius.x, ry = room.Radius.y;
            for (int dy = -ry - 1; dy <= ry + 1; dy++)
            for (int dx = -rx - 1; dx <= rx + 1; dx++)
            {
                // Borda irregular: o raio varia de leve com o ângulo, para a sala não parecer um oval perfeito.
                float angle = Mathf.Atan2(dy / (float)ry, dx / (float)rx);
                float wobble = 1f + 0.12f * Mathf.Sin(angle * 3f + room.Center.x * 0.37f) + 0.07f * Mathf.Sin(angle * 5f + room.Center.y);
                float u = dx / (float)rx, v = dy / (float)ry;
                if (u * u + v * v <= wobble * wobble)
                    Open(c.x + dx, c.y + dy, open, seen);
            }
        }

        void StampLine(Vector2 from, Vector2 to, float radius, List<Vector2Int> open, HashSet<int> seen)
        {
            float length = Vector2.Distance(from, to);
            int steps = Mathf.Max(1, Mathf.CeilToInt(length * 2f));
            int reach = Mathf.CeilToInt(radius);
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(from, to, i / (float)steps);
                int cx = _origin.x + Mathf.RoundToInt(p.x), cy = _origin.y + Mathf.RoundToInt(p.y);
                for (int dy = -reach; dy <= reach; dy++)
                for (int dx = -reach; dx <= reach; dx++)
                    if (dx * dx + dy * dy <= radius * radius)
                        Open(cx + dx, cy + dy, open, seen);
            }
        }

        void Open(int x, int y, List<Vector2Int> open, HashSet<int> seen)
        {
            if (!_grid.InBounds(x, y) || _grid.Get(x, y) == CellMaterial.Bedrock)
                return;
            _grid.Set(x, y, CellMaterial.Empty, 0);
            if (seen.Add(_grid.ToIndex(x, y)))
                open.Add(new Vector2Int(x, y));
        }

        /// <summary>Areia é granular e desabaria dentro dos túneis novos: onde ela está perto do formigueiro, vira terra firme.</summary>
        void SettleSand()
        {
            const int margin = 90;
            for (int y = _origin.y - margin; y <= _origin.y + 40; y++)
            for (int x = _origin.x - margin; x <= _origin.x + margin; x++)
            {
                if (_grid.Get(x, y) == CellMaterial.Sand)
                    _grid.Set(x, y, CellMaterial.Dirt, _grid.GetShade(x, y));
            }
        }

        void MarkRooms()
        {
            foreach (Room room in Rooms)
            {
                if (room.Kind == RoomKind.None)
                    continue;
                Vector2Int c = Local(room.Center);
                // Só o miolo da sala: a borda irregular fica de fora para a marca não vazar para a parede.
                int rx = room.Radius.x - 1, ry = room.Radius.y - 1;
                for (int dy = -ry; dy <= ry; dy++)
                for (int dx = -rx; dx <= rx; dx++)
                {
                    float u = dx / (float)rx, v = dy / (float)ry;
                    int x = c.x + dx, y = c.y + dy;
                    if (u * u + v * v > 1f || _grid.IsSolid(x, y))
                        continue;
                    switch (room.Kind)
                    {
                        case RoomKind.Queen: _grid.SetQueenRoom(x, y, true); break;
                        case RoomKind.Nursery: _grid.SetNurseryRoom(x, y, true); break;
                        case RoomKind.Nutrient: _grid.SetNutrientRoom(x, y, true); break;
                    }
                }
            }
        }

        void Populate(AntColony colony, List<Vector2Int> open, int seed)
        {
            // Reserva de nutrientes para a rainha pôr ovos: o berçário tem cria e as formigas têm o que carregar.
            colony.RestoreNutrients(30, 30, 0);
            var rng = new System.Random(seed ^ 0x5eed);
            foreach ((AntRole role, int count) in Population)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2Int cell = open[rng.Next(open.Count)];
                    colony.SpawnAt(role, cell.x, cell.y);
                }
            }
        }
    }
}
