using System;
using System.Collections.Generic;
using Fabula.Incrementant.World;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Regras de onde uma formiga pode ficar. Abaixo do solo original, todo vazio é caminhável: é túnel e tem
    /// o fundo do formigueiro (a parede de trás) como apoio. No céu, células encostadas (8 vizinhos) em algo sólido e,
    /// em qualquer lugar, as cobertas pela arte do cenário de perto (<see cref="PixelGrid.IsBackdrop"/>) e as de obra.
    /// </summary>
    public static class AntNavigation
    {
        public static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        public static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

        public static bool IsWalkable(PixelGrid grid, int x, int y)
        {
            if (!grid.InBounds(x, y))
                return false;
            if (grid.IsSolid(x, y))
                // Planta é como o túnel do formigueiro: a formiga entra nela e sobe por ela (pedido do desenvolvedor em
                // 2026-10-17). Obra do lado de fora também (pedido de 2026-10-18); o resto do sólido é parede.
                return CellMaterials.IsPlant(grid.Get(x, y)) || IsOutdoorBuilt(grid, x, y);
            if (IsBackground(grid, x, y))
                return true;
            return TouchesSolid(grid, x, y);
        }

        /// <summary>Vazio abaixo do solo original: a formiga se apoia na parede de trás do formigueiro.</summary>
        public static bool IsTunnelBackground(PixelGrid grid, int x, int y) => y <= grid.GetSurfaceY(x);

        /// <summary>
        /// Há um fundo para a formiga se apoiar: a parede de trás do formigueiro ou, no céu, a arte de um elemento do
        /// cenário (cogumelo, graveto, folha seca, pedrinha; pedido do desenvolvedor em 2026-10-18). Antes ela só andava
        /// pelo contorno do que é sólido.
        /// </summary>
        public static bool IsBackground(PixelGrid grid, int x, int y) =>
            IsTunnelBackground(grid, x, y) || grid.IsBackdrop(x, y);

        /// <summary>Obra (<see cref="PixelGrid.IsBuilt"/>) acima do solo original: a formiga anda por dentro dela.</summary>
        public static bool IsOutdoorBuilt(PixelGrid grid, int x, int y) =>
            y > grid.GetSurfaceY(x) && grid.IsBuilt(x, y);

        /// <summary>
        /// Algum dos 8 vizinhos é sólido. Acima do topo do mundo é céu aberto, não apoio: sem isso a última linha da
        /// grade (fora dela tudo conta como <see cref="CellMaterial.Bedrock"/>) viraria um teto onde as formigas andam.
        /// </summary>
        public static bool TouchesSolid(PixelGrid grid, int x, int y)
        {
            for (int k = 0; k < 8; k++)
            {
                int ny = y + Dy[k];
                if (ny < grid.Height && grid.IsSolid(x + Dx[k], ny))
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Campo de distância (BFS em 8 direções sobre células caminháveis) a partir de um conjunto de origens.
    /// Formigas descem o gradiente para chegar à origem mais próxima.
    /// <see cref="Build"/> refaz tudo; <see cref="Update"/> corrige só o que mudou (células que viraram ou deixaram de ser
    /// caminháveis e origens que entraram ou saíram) e dá exatamente o mesmo campo que <see cref="Build"/> daria.
    /// </summary>
    public sealed class FlowField
    {
        public const int Unreachable = int.MaxValue;

        /// <summary>Acima disto de células alteradas ou de origens trocadas, refazer do zero sai mais barato que corrigir.</summary>
        const int MaxIncrementalChanges = 2500;
        const int MaxIncrementalSourceChanges = 1500;

        readonly int[] _distance;
        readonly int[] _queue;
        readonly int _width;
        readonly int _height;
        int _reached;
        // O campo corresponde à última rodada de origens e de caminhabilidade vistas (Build ou Update): só então Update vale.
        bool _synced;
        // _queue guarda as células alcançadas pelo último Build: só então ele limpa só elas (Update bagunça a lista).
        bool _queueIsReachedList;

        // Estado do Update (o trabalho temporário fica no _scratch, que vários campos dividem).
        readonly bool[] _isSource;
        readonly List<int> _sources = new List<int>();
        readonly Scratch _scratch;

        /// <summary>
        /// Memória de trabalho do <see cref="Update"/>, dividida entre campos do mesmo tamanho (um por campo custaria dezenas
        /// de MB). Um <see cref="Update"/> nunca roda dentro de outro, então dividir é seguro.
        /// </summary>
        public sealed class Scratch
        {
            // Carimbos por célula: positivo = invalidada nesta passada; negativo = origem da lista nova (evita limpar o array).
            internal readonly int[] Mark;
            internal int Counter;
            internal readonly List<int> Invalid = new List<int>();
            internal readonly List<int> RemovedSources = new List<int>();
            internal readonly List<int> AddedSources = new List<int>();
            internal readonly List<long> Seeds = new List<long>();
            internal readonly List<long> Wave = new List<long>();

            public Scratch(int width, int height) => Mark = new int[width * height];
        }

        public FlowField(int width, int height, Scratch scratch = null)
        {
            _width = width;
            _height = height;
            _distance = new int[width * height];
            _queue = new int[width * height];
            _isSource = new bool[width * height];
            _scratch = scratch ?? new Scratch(width, height);
            Array.Fill(_distance, Unreachable);
        }

        /// <summary>Quantas células o campo alcança (as origens incluídas).</summary>
        public int ReachedCount => _reached;

        public int Get(int x, int y) =>
            (uint)x < (uint)_width && (uint)y < (uint)_height ? _distance[y * _width + x] : Unreachable;

        /// <summary>O último <see cref="Update"/> corrigiu só o que mudou (não caiu em <see cref="Build"/>). Para testes e diagnóstico.</summary>
        public bool LastUpdateWasIncremental { get; private set; }

        /// <summary>O campo deixa de valer como base do <see cref="Update"/>: o próximo vira um <see cref="Build"/> completo.</summary>
        public void Invalidate() => _synced = false;

        /// <summary>Monta o campo consultando a grade célula a célula. Conveniência para testes; o jogo usa o cache.</summary>
        public void Build(PixelGrid grid, List<int> sourceIndices)
        {
            var walkability = new WalkabilityMap(grid);
            walkability.Refresh();
            Build(walkability, sourceIndices);
        }

        /// <summary>Monta o campo sobre <paramref name="walkability"/>, que precisa estar atualizado (<see cref="WalkabilityMap.Refresh"/>).</summary>
        /// <param name="anthillOnly">
        /// Só se espalha pelo formigueiro: células no solo original ou abaixo dele (sem o céu) e sem marca de construção
        /// (não atravessa a obra). Esses campos não entram no <see cref="Update"/>.
        /// </param>
        public void Build(WalkabilityMap walkability, List<int> sourceIndices, bool anthillOnly = false)
        {
            PixelGrid grid = walkability.Grid;
            // Toda célula com distância passou pela fila do build anterior: limpar só ela custa o alcançado, não a grade.
            if (_queueIsReachedList)
            {
                for (int n = 0; n < _reached; n++)
                    _distance[_queue[n]] = Unreachable;
            }
            else
            {
                Array.Fill(_distance, Unreachable);
            }
            foreach (int old in _sources)
                _isSource[old] = false;
            _sources.Clear();
            int head = 0;
            int tail = 0;

            foreach (int source in sourceIndices)
            {
                _isSource[source] = true;
                _sources.Add(source);
                if (_distance[source] != Unreachable)
                    continue;
                _distance[source] = 0;
                _queue[tail++] = source;
            }

            while (head < tail)
            {
                int i = _queue[head++];
                int x = i % _width;
                int y = i / _width;
                int next = _distance[i] + 1;

                for (int k = 0; k < 8; k++)
                {
                    int nx = x + AntNavigation.Dx[k];
                    int ny = y + AntNavigation.Dy[k];
                    if ((uint)nx >= (uint)_width || (uint)ny >= (uint)_height)
                        continue;
                    int ni = ny * _width + nx;
                    if (_distance[ni] != Unreachable || !walkability.IsWalkableAt(ni))
                        continue;
                    if (anthillOnly && (ny > grid.GetSurfaceY(nx) || grid.IsBuildMarked(nx, ny)))
                        continue;
                    _distance[ni] = next;
                    _queue[tail++] = ni;
                }
            }
            _reached = tail;
            _queueIsReachedList = true;
            _synced = !anthillOnly;
        }

        /// <summary>
        /// Acerta o campo com as origens novas e com as mudanças de caminhabilidade registradas em
        /// <see cref="WalkabilityMap.ChangedCells"/> desde o último <see cref="Update"/>/<see cref="Build"/> deste campo; o
        /// resultado é igual ao de <see cref="Build"/> sobre o mesmo estado. Quem chama precisa passar pelo campo toda vez
        /// que a lista de mudanças for usada (ou chamar <see cref="Invalidate"/>), senão ele perde mudanças.
        /// Cai em <see cref="Build"/> na primeira vez, se o registro estourou ou se mudou coisa demais.
        /// </summary>
        public void Update(WalkabilityMap walkability, List<int> sourceIndices)
        {
            Scratch scratch = _scratch;
            int[] mark = scratch.Mark;
            List<int> changed = walkability.ChangedCells;
            LastUpdateWasIncremental = false;
            if (!_synced || walkability.ChangesOverflowed || changed.Count > MaxIncrementalChanges)
            {
                Build(walkability, sourceIndices);
                return;
            }

            // Sem nenhuma origem nada é alcançável: limpar direto custa menos que invalidar a árvore inteira, célula a célula.
            if (sourceIndices.Count == 0)
            {
                if (_reached > 0 || _sources.Count > 0)
                {
                    foreach (int old in _sources)
                        _isSource[old] = false;
                    _sources.Clear();
                    Array.Fill(_distance, Unreachable);
                    _reached = 0;
                    _queueIsReachedList = false;
                }
                LastUpdateWasIncremental = true;
                return;
            }

            // Origens: diferença entre as de antes e as de agora.
            scratch.RemovedSources.Clear();
            scratch.AddedSources.Clear();
            int stamp = ++scratch.Counter;
            foreach (int source in sourceIndices)
            {
                if (mark[source] == -stamp)
                    continue; // repetida
                mark[source] = -stamp;
                if (!_isSource[source])
                    scratch.AddedSources.Add(source);
            }
            foreach (int old in _sources)
            {
                if (mark[old] != -stamp)
                    scratch.RemovedSources.Add(old);
            }
            if (scratch.AddedSources.Count + scratch.RemovedSources.Count > MaxIncrementalSourceChanges)
            {
                Build(walkability, sourceIndices);
                return;
            }
            LastUpdateWasIncremental = true;
            if (changed.Count == 0 && scratch.AddedSources.Count == 0 && scratch.RemovedSources.Count == 0)
                return;

            foreach (int c in scratch.RemovedSources)
                _isSource[c] = false;
            foreach (int c in scratch.AddedSources)
                _isSource[c] = true;
            _sources.Clear();
            _sources.AddRange(sourceIndices);
            _queueIsReachedList = false;

            // Fase 1: células cuja distância pode crescer (deixaram de ser caminháveis ou de ser origem) e as que dependiam só delas.
            stamp = ++scratch.Counter;
            scratch.Invalid.Clear();
            for (int n = 0; n < changed.Count; n++)
            {
                int c = changed[n];
                if (_distance[c] != Unreachable && !walkability.IsWalkableAt(c) && !_isSource[c])
                    MarkInvalid(c, stamp);
            }
            foreach (int c in scratch.RemovedSources)
            {
                if (_distance[c] != Unreachable && !_isSource[c])
                    MarkInvalid(c, stamp);
            }
            for (int head = 0; head < scratch.Invalid.Count; head++)
            {
                int c = scratch.Invalid[head];
                int cx = c % _width;
                int cy = c / _width;
                int dependent = _distance[c] + 1;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + AntNavigation.Dx[k];
                    int ny = cy + AntNavigation.Dy[k];
                    if ((uint)nx >= (uint)_width || (uint)ny >= (uint)_height)
                        continue;
                    int ni = ny * _width + nx;
                    if (_distance[ni] != dependent || mark[ni] == stamp || _isSource[ni])
                        continue;
                    if (!HasSupport(ni, stamp))
                        MarkInvalid(ni, stamp);
                }
            }
            foreach (int c in scratch.Invalid)
            {
                _distance[c] = Unreachable;
                _reached--;
            }

            // Fase 2: o que ficou sem distância pega a melhor dos vizinhos; células novas (caminháveis ou origens) também.
            scratch.Seeds.Clear();
            foreach (int c in scratch.Invalid)
                SeedFromNeighbors(walkability, c);
            for (int n = 0; n < changed.Count; n++)
            {
                int c = changed[n];
                if (_distance[c] == Unreachable)
                    SeedFromNeighbors(walkability, c);
            }
            foreach (int c in scratch.AddedSources)
            {
                if (_distance[c] == 0)
                    continue;
                if (_distance[c] == Unreachable)
                    _reached++;
                _distance[c] = 0;
                scratch.Seeds.Add(c);
            }

            // Propaga em ordem crescente de distância: as sementes (ordenadas) e a fila de ondas (já em ordem) se intercalam.
            scratch.Seeds.Sort();
            scratch.Wave.Clear();
            int seed = 0;
            int wave = 0;
            while (seed < scratch.Seeds.Count || wave < scratch.Wave.Count)
            {
                long entry;
                if (wave >= scratch.Wave.Count || seed < scratch.Seeds.Count && scratch.Seeds[seed] <= scratch.Wave[wave])
                    entry = scratch.Seeds[seed++];
                else
                    entry = scratch.Wave[wave++];
                int c = (int)(entry & 0xFFFFFFFFL);
                int d = (int)(entry >> 32);
                if (_distance[c] != d)
                    continue; // já melhorou depois de entrar na fila
                int cx = c % _width;
                int cy = c / _width;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + AntNavigation.Dx[k];
                    int ny = cy + AntNavigation.Dy[k];
                    if ((uint)nx >= (uint)_width || (uint)ny >= (uint)_height)
                        continue;
                    int ni = ny * _width + nx;
                    if (_distance[ni] <= d + 1 || !walkability.IsWalkableAt(ni))
                        continue;
                    if (_distance[ni] == Unreachable)
                        _reached++;
                    _distance[ni] = d + 1;
                    scratch.Wave.Add(((long)(d + 1) << 32) | (uint)ni);
                }
            }
        }

        void MarkInvalid(int cell, int stamp)
        {
            _scratch.Mark[cell] = stamp;
            _scratch.Invalid.Add(cell);
        }

        /// <summary>Algum vizinho ainda válido está um passo mais perto da origem (então a distância da célula continua certa).</summary>
        bool HasSupport(int cell, int stamp)
        {
            int cx = cell % _width;
            int cy = cell / _width;
            int closer = _distance[cell] - 1;
            for (int k = 0; k < 8; k++)
            {
                int nx = cx + AntNavigation.Dx[k];
                int ny = cy + AntNavigation.Dy[k];
                if ((uint)nx >= (uint)_width || (uint)ny >= (uint)_height)
                    continue;
                int ni = ny * _width + nx;
                if (_distance[ni] == closer && _scratch.Mark[ni] != stamp)
                    return true;
            }
            return false;
        }

        /// <summary>Uma célula caminhável sem distância recebe 1 + a menor dos vizinhos e vira semente da propagação.</summary>
        void SeedFromNeighbors(WalkabilityMap walkability, int cell)
        {
            if (_distance[cell] != Unreachable || !walkability.IsWalkableAt(cell))
                return;
            int cx = cell % _width;
            int cy = cell / _width;
            int best = Unreachable;
            for (int k = 0; k < 8; k++)
            {
                int nx = cx + AntNavigation.Dx[k];
                int ny = cy + AntNavigation.Dy[k];
                if ((uint)nx >= (uint)_width || (uint)ny >= (uint)_height)
                    continue;
                int d = _distance[ny * _width + nx];
                if (d < best)
                    best = d;
            }
            if (best == Unreachable)
                return;
            _distance[cell] = best + 1;
            _reached++;
            _scratch.Seeds.Add(((long)(best + 1) << 32) | (uint)cell);
        }
    }
}
