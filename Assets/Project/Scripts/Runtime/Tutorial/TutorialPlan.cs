using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Tutorial
{
    /// <summary>
    /// Uma câmara sugerida pelo tutorial: elipse ligada ao abrigo por um túnel de 3 células de altura. Guarda só o que estava
    /// sólido na hora do planejamento (<see cref="DigCells"/>); o resto já era vazio. Células de rocha ficam de fora, porque
    /// as operárias não as cavam com o pincel de escavação.
    /// </summary>
    public sealed class DigArea
    {
        /// <summary>Todas as células do desenho (túnel + câmara), sólidas ou não.</summary>
        public Vector2Int[] Cells;
        /// <summary>Células sólidas a escavar (túnel + câmara).</summary>
        public Vector2Int[] DigCells;
        /// <summary>Interior da câmara (a elipse inteira, sem o túnel), onde se pinta a sala.</summary>
        public Vector2Int[] ChamberCells;
        public RectInt Bounds;
        public RectInt ChamberBounds;
        public int Side;
        /// <summary>Quanto o terreno atrapalha (rocha, areia, água, superfície); 0 é terra limpa.</summary>
        public int Penalty;

        public Rect BoundsRect => new Rect(Bounds.x, Bounds.y, Bounds.width, Bounds.height);

        public Rect ChamberRect => new Rect(ChamberBounds.x, ChamberBounds.y, ChamberBounds.width, ChamberBounds.height);
    }

    /// <summary>Planejamento e medição das câmaras do tutorial (lógica pura sobre a grade, sem UI).</summary>
    public static class TutorialPlanner
    {
        public const int ChamberRadiusX = 6;
        public const int ChamberRadiusY = 3;
        public const int TunnelLength = 4;

        const int ShelterScanX = 30;
        const int ShelterScanUp = 14;

        /// <summary>Caixa do abrigo: células vazias ligadas ao ninho, perto dele (o poço da entrada não entra).</summary>
        public static RectInt ShelterBounds(PixelGrid grid, Vector2Int nest)
        {
            int minX = nest.x, maxX = nest.x, minY = nest.y, maxY = nest.y;
            int limitY = nest.y + ShelterScanUp;
            var seen = new HashSet<Vector2Int> { nest };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(nest);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
                minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var n = new Vector2Int(c.x + dx, c.y + dy);
                    if (n.y > limitY || Mathf.Abs(n.x - nest.x) > ShelterScanX || !grid.InBounds(n.x, n.y) ||
                        grid.IsSolid(n.x, n.y) || !seen.Add(n))
                        continue;
                    queue.Enqueue(n);
                }
            }
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <summary>
        /// Planeja a câmara do lado <paramref name="side"/> (+1 direita, -1 esquerda) do abrigo. Devolve nulo se ela sair da área ativa.
        /// <paramref name="avoid"/> são caixas já usadas (outra câmara): ficam de fora, com 3 células de folga.
        /// </summary>
        public static DigArea Plan(PixelGrid grid, Vector2Int nest, RectInt shelter, int side, IReadOnlyList<RectInt> avoid = null, int rowOffset = 0)
        {
            // O abrigo é uma elipse: a linha do meio é a mais larga, e é nela que o túnel sai (rowOffset desce a câmara um pouco
            // quando o relevo do lado é baixo demais).
            int rowY = shelter.y + shelter.height / 2 + rowOffset;
            int edge = nest.x;
            while (grid.InBounds(edge + side, rowY) && !grid.IsSolid(edge + side, rowY) && Mathf.Abs(edge - nest.x) < ShelterScanX)
                edge += side;
            int cx = edge + side * (TunnelLength + ChamberRadiusX + 1);
            int cy = rowY;

            var cells = new HashSet<Vector2Int>();
            var chamber = new List<Vector2Int>();
            for (int y = cy - ChamberRadiusY; y <= cy + ChamberRadiusY; y++)
            for (int x = cx - ChamberRadiusX; x <= cx + ChamberRadiusX; x++)
            {
                float dx = (x - cx) / (float)ChamberRadiusX, dy = (y - cy) / (float)ChamberRadiusY;
                if (dx * dx + dy * dy > 1f)
                    continue;
                chamber.Add(new Vector2Int(x, y));
                cells.Add(new Vector2Int(x, y));
            }
            // Túnel: da borda do abrigo até o centro da câmara.
            for (int x = Mathf.Min(edge, cx); x <= Mathf.Max(edge, cx); x++)
            for (int y = cy - 1; y <= cy + 1; y++)
                cells.Add(new Vector2Int(x, y));

            var dig = new List<Vector2Int>();
            int penalty = 0;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (Vector2Int c in cells)
            {
                minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x);
                minY = Mathf.Min(minY, c.y); maxY = Mathf.Max(maxY, c.y);
                if (!grid.InBounds(c.x, c.y))
                    return null;
                CellMaterial m = grid.Get(c.x, c.y);
                if (c.y >= grid.GetSurfaceY(c.x) - 2)
                    penalty += 100; // não rompe a superfície
                if (m == CellMaterial.Empty)
                    continue;
                if (m == CellMaterial.Rock || m == CellMaterial.Bedrock)
                    penalty += 20;
                else
                {
                    if (CellMaterials.IsGranular(m))
                        penalty += 2; // desaba e entulha a câmara
                    dig.Add(c);
                }
                if (grid.HasWater(c.x, c.y))
                    penalty += 5;
            }
            var bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
            if (avoid != null)
            {
                foreach (RectInt other in avoid)
                {
                    var padded = new RectInt(other.x - 3, other.y - 3, other.width + 6, other.height + 6);
                    if (padded.Overlaps(bounds))
                        penalty += 1000;
                }
            }

            int cminX = int.MaxValue, cminY = int.MaxValue, cmaxX = int.MinValue, cmaxY = int.MinValue;
            foreach (Vector2Int c in chamber)
            {
                cminX = Mathf.Min(cminX, c.x); cmaxX = Mathf.Max(cmaxX, c.x);
                cminY = Mathf.Min(cminY, c.y); cmaxY = Mathf.Max(cmaxY, c.y);
            }
            return new DigArea
            {
                Cells = new List<Vector2Int>(cells).ToArray(),
                DigCells = dig.ToArray(),
                ChamberCells = chamber.ToArray(),
                Bounds = bounds,
                ChamberBounds = new RectInt(cminX, cminY, cmaxX - cminX + 1, cmaxY - cminY + 1),
                Side = side,
                Penalty = penalty,
            };
        }

        /// <summary>Entre os dois lados do abrigo (e alguns degraus mais baixos), o que menos atrapalha (empate: direita, mais alto). Nulo se nenhum cabe.</summary>
        public static DigArea Choose(PixelGrid grid, Vector2Int nest, RectInt shelter, IReadOnlyList<RectInt> avoid = null)
        {
            DigArea best = null;
            foreach (int rowOffset in new[] { 0, -1, -2, -3 })
            foreach (int side in new[] { 1, -1 })
            {
                DigArea candidate = Plan(grid, nest, shelter, side, avoid, rowOffset);
                if (candidate != null && (best == null || candidate.Penalty < best.Penalty))
                    best = candidate;
            }
            return best;
        }

        /// <summary>Fração das células a escavar que já está vazia.</summary>
        public static float OpenFraction(PixelGrid grid, DigArea area)
        {
            if (area.DigCells.Length == 0)
                return 1f;
            int open = 0;
            foreach (Vector2Int c in area.DigCells)
                if (!grid.IsSolid(c.x, c.y))
                    open++;
            return open / (float)area.DigCells.Length;
        }

        /// <summary>Fração das células a escavar que já foi marcada ou já está vazia.</summary>
        public static float MarkedOrOpenFraction(PixelGrid grid, DigArea area)
        {
            if (area.DigCells.Length == 0)
                return 1f;
            int count = 0;
            foreach (Vector2Int c in area.DigCells)
                if (grid.IsMarked(c.x, c.y) || !grid.IsSolid(c.x, c.y))
                    count++;
            return count / (float)area.DigCells.Length;
        }

        /// <summary>Quantas células da área ainda têm marca de escavação.</summary>
        public static int MarkedCount(PixelGrid grid, DigArea area)
        {
            int count = 0;
            foreach (Vector2Int c in area.DigCells)
                if (grid.IsMarked(c.x, c.y))
                    count++;
            return count;
        }

        /// <summary>
        /// Escavada e acessível: pelo menos <paramref name="openFraction"/> das células a escavar vazias e a câmara ligada ao
        /// ninho por células vazias (as formigas andam em 8 direções).
        /// </summary>
        public static bool IsDugAndReachable(PixelGrid grid, Vector2Int nest, RectInt shelter, DigArea area, float openFraction = 0.9f)
        {
            if (OpenFraction(grid, area) < openFraction)
                return false;
            int x0 = Mathf.Min(shelter.xMin, area.Bounds.xMin) - 2, x1 = Mathf.Max(shelter.xMax, area.Bounds.xMax) + 2;
            int y0 = Mathf.Min(shelter.yMin, area.Bounds.yMin) - 2, y1 = Mathf.Max(shelter.yMax, area.Bounds.yMax) + 2;
            var seen = new HashSet<Vector2Int> { nest };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(nest);
            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var n = new Vector2Int(c.x + dx, c.y + dy);
                    if (n.x < x0 || n.x > x1 || n.y < y0 || n.y > y1 || !grid.InBounds(n.x, n.y) || grid.IsSolid(n.x, n.y) || !seen.Add(n))
                        continue;
                    queue.Enqueue(n);
                }
            }
            int open = 0, reached = 0;
            foreach (Vector2Int c in area.ChamberCells)
            {
                if (grid.IsSolid(c.x, c.y))
                    continue;
                open++;
                if (seen.Contains(c))
                    reached++;
            }
            return open > 0 && reached >= open * 0.9f;
        }

        /// <summary>Quantas células do interior da câmara têm a sala pintada (<paramref name="isPainted"/> diz qual sala).</summary>
        public static int PaintedCount(PixelGrid grid, DigArea area, System.Func<PixelGrid, int, int, bool> isPainted)
        {
            int count = 0;
            foreach (Vector2Int c in area.ChamberCells)
                if (isPainted(grid, c.x, c.y))
                    count++;
            return count;
        }
    }
}
