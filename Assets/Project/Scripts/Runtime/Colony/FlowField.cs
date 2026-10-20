using System;
using System.Collections.Generic;
using Fabula.Incrementant.World;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Regras de onde uma formiga pode ficar. Abaixo do solo original, todo vazio é caminhável: é túnel e tem
    /// o fundo do formigueiro (a parede de trás) como apoio. No céu, células encostadas (8 vizinhos) em algo sólido e,
    /// em qualquer lugar, as cobertas pela arte do cenário de perto (<see cref="PixelGrid.IsBackdrop"/>); por dentro, a
    /// obra e a terra que as formigas trouxeram para fora (<see cref="IsOutdoorWall"/>).
    /// </summary>
    public static class AntNavigation
    {
        public static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 };
        public static readonly int[] Dy = { 0, 0, 1, -1, 1, -1, 1, -1 };

        /// <summary>
        /// Direção (índice de <see cref="Dx"/>) do vizinho caminhável de menor distância; -1 = nenhum desce.
        /// O campo dá custo 1 a todo vizinho, então quase sempre há empate (indo para a direita, leste, nordeste e sudeste
        /// costumam descer igual). Sortear a cada passo fazia a formiga ziguezaguear. O empate fica com o passo ortogonal
        /// (no máximo um desce: o do eixo mais longo), depois com <paramref name="previous"/>, e só então é sorteado; assim
        /// o caminho sai reto e só vira diagonal quando precisa. A direção anterior na frente faria um V: quem vinha na
        /// diagonal seguiria nela mesmo com o alvo à frente e voltaria depois.
        /// </summary>
        public static int Downhill(PixelGrid grid, FlowField field, Random rng, int x, int y, int previous)
        {
            int current = field.Get(x, y);
            int best = current;
            int ties = 0;
            int chosen = -1;
            int chosenRank = -1;
            for (int k = 0; k < 8; k++)
            {
                int nx = x + Dx[k];
                int ny = y + Dy[k];
                int d = field.Get(nx, ny);
                if (d > best || d == FlowField.Unreachable || !IsWalkable(grid, nx, ny))
                    continue;
                // 2 = ortogonal (índices 0–3), 1 = mantém a direção, 0 = outra diagonal.
                int rank = k < 4 ? 2 : k == previous ? 1 : 0;
                if (d < best)
                {
                    best = d;
                    chosen = k;
                    chosenRank = rank;
                    ties = 1;
                }
                else if (d < current)
                {
                    if (rank > chosenRank)
                    {
                        chosen = k;
                        chosenRank = rank;
                        ties = 1;
                    }
                    else if (rank == chosenRank && rng.Next(++ties) == 0)
                    {
                        chosen = k;
                    }
                }
            }
            return chosen;
        }

        public static bool IsWalkable(PixelGrid grid, int x, int y)
        {
            if (!grid.InBounds(x, y))
                return false;
            if (grid.IsSolid(x, y))
                // Planta é como o túnel do formigueiro: a formiga entra nela e sobe por ela (pedido do desenvolvedor em
                // 2026-10-17). Obra e terra trazida para fora também (pedidos de 2026-10-18 e 2026-10-19); o resto do sólido é parede.
                return CellMaterials.IsPlant(grid.Get(x, y)) || IsOutdoorWall(grid, x, y);
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
        /// Bloco acima do solo original em cuja face a formiga anda, como na parede de trás do formigueiro (pedido do
        /// desenvolvedor em 2026-10-19): a obra, a terra e a areia que elas despejaram (soltas, compactadas pela chuva ou
        /// amolecidas pela água). Acima do solo original não há terreno natural, então esse material só pode ter vindo
        /// das formigas. A planta solta (folha e grama caídas) também: ela escorre por cima do monte e, se fosse parede,
        /// prendia embaixo dela quem estava dentro do monte, sem vazio acima para soltar o grão (relato de 2026-10-19).
        /// </summary>
        public static bool IsOutdoorWall(PixelGrid grid, int x, int y)
        {
            if (y <= grid.GetSurfaceY(x))
                return false;
            if (grid.IsBuilt(x, y))
                return true;
            CellMaterial material = grid.Get(x, y);
            return material == CellMaterial.Spoil || material == CellMaterial.PackedSpoil || material == CellMaterial.Mud ||
                   material == CellMaterial.Sand || material == CellMaterial.PackedSand || CellMaterials.IsPlantDebris(material);
        }

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
        const int MaxIncrementalInvalid = 6000;

        readonly int[] _distance;
        readonly int[] _queue;
        // Deslocamento de índice dos 8 vizinhos (ordem de AntNavigation.Dx/Dy), válido longe da borda do mundo.
        readonly int[] _offset;
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
            _offset = new int[8];
            for (int k = 0; k < 8; k++)
                _offset[k] = AntNavigation.Dy[k] * width + AntNavigation.Dx[k];
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

            // Laço quente (uma passada pelo alcançado a cada Build): vetores locais, uma divisão por célula e, longe da
            // borda do mundo, os 8 vizinhos por deslocamento fixo. A ordem dos vizinhos é a de AntNavigation.Dx/Dy.
            int[] distance = _distance;
            int[] queue = _queue;
            bool[] walkable = walkability.WalkableCells;
            int width = _width;
            int height = _height;
            while (head < tail)
            {
                int i = queue[head++];
                int y = i / width;
                int x = i - y * width;
                int next = distance[i] + 1;

                if (!anthillOnly && x > 0 && x < width - 1 && y > 0 && y < height - 1)
                {
                    int n = i + 1;
                    if (distance[n] == Unreachable && walkable[n]) { distance[n] = next; queue[tail++] = n; }
                    n = i - 1;
                    if (distance[n] == Unreachable && walkable[n]) { distance[n] = next; queue[tail++] = n; }
                    n = i + width;
                    if (distance[n] == Unreachable && walkable[n]) { distance[n] = next; queue[tail++] = n; }
                    n = i - width;
                    if (distance[n] == Unreachable && walkable[n]) { distance[n] = next; queue[tail++] = n; }
                    n = i + width + 1;
                    if (distance[n] == Unreachable && walkable[n]) { distance[n] = next; queue[tail++] = n; }
                    n = i - width + 1;
                    if (distance[n] == Unreachable && walkable[n]) { distance[n] = next; queue[tail++] = n; }
                    n = i + width - 1;
                    if (distance[n] == Unreachable && walkable[n]) { distance[n] = next; queue[tail++] = n; }
                    n = i - width - 1;
                    if (distance[n] == Unreachable && walkable[n]) { distance[n] = next; queue[tail++] = n; }
                    continue;
                }

                for (int k = 0; k < 8; k++)
                {
                    int nx = x + AntNavigation.Dx[k];
                    int ny = y + AntNavigation.Dy[k];
                    if ((uint)nx >= (uint)width || (uint)ny >= (uint)height)
                        continue;
                    int ni = ny * width + nx;
                    if (distance[ni] != Unreachable || !walkable[ni])
                        continue;
                    if (anthillOnly && (ny > grid.GetSurfaceY(nx) || grid.IsBuildMarked(nx, ny)))
                        continue;
                    distance[ni] = next;
                    queue[tail++] = ni;
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
                Fallback(walkability, sourceIndices, !_synced ? Fallbacks.NotSynced : Fallbacks.TooManyChanges);
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
            // Nenhuma origem antiga sobrou (a rainha fora da sala andou uma célula): todo o campo dependia delas e seria
            // invalidado; refazer custa uma fração disso.
            if (scratch.AddedSources.Count + scratch.RemovedSources.Count > MaxIncrementalSourceChanges ||
                scratch.RemovedSources.Count > 0 && scratch.RemovedSources.Count == _sources.Count)
            {
                Fallback(walkability, sourceIndices,
                         scratch.RemovedSources.Count > 0 && scratch.RemovedSources.Count == _sources.Count
                             ? Fallbacks.AllSourcesGone : Fallbacks.TooManySourceChanges);
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
            // Invalidar mais da metade do alcançado (a origem única que andou uma célula invalida tudo) custa várias vezes um
            // Build: medido no campo de casa com a rainha andando, ~30 ms corrigindo contra ~6 ms refazendo. Até aqui as
            // distâncias não mudaram, então o Build parte de um estado válido; o resultado é o mesmo pelos dois caminhos.
            // O teto absoluto vale para o mundo grande: corrigir custa várias vezes o Build por célula invalidada (medido: ~20 ms
            // corrigindo 40 mil células contra ~5 ms refazendo um campo de 113 mil), então passa de MaxIncrementalInvalid o Build ganha.
            int maxInvalid = Math.Min(_reached / 2, MaxIncrementalInvalid);
            for (int head = 0; head < scratch.Invalid.Count; head++)
            {
                if (scratch.Invalid.Count > maxInvalid)
                {
                    Fallback(walkability, sourceIndices, Fallbacks.TooManyInvalid);
                    return;
                }
                int c = scratch.Invalid[head];
                int cy = c / _width;
                int cx = c - cy * _width;
                int dependent = _distance[c] + 1;
                bool interior = cx > 0 && cx < _width - 1 && cy > 0 && cy < _height - 1;
                for (int k = 0; k < 8; k++)
                {
                    int ni;
                    if (interior)
                    {
                        ni = c + _offset[k];
                    }
                    else
                    {
                        int nx = cx + AntNavigation.Dx[k];
                        int ny = cy + AntNavigation.Dy[k];
                        if ((uint)nx >= (uint)_width || (uint)ny >= (uint)_height)
                            continue;
                        ni = ny * _width + nx;
                    }
                    if (_distance[ni] != dependent || mark[ni] == stamp || _isSource[ni])
                        continue;
                    if (!HasSupport(ni, stamp))
                        MarkInvalid(ni, stamp);
                }
            }
            _queueIsReachedList = false;
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
            bool[] walkable = walkability.WalkableCells;
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
                int cy = c / _width;
                int cx = c - cy * _width;
                bool interior = cx > 0 && cx < _width - 1 && cy > 0 && cy < _height - 1;
                for (int k = 0; k < 8; k++)
                {
                    int ni;
                    if (interior)
                    {
                        ni = c + _offset[k];
                    }
                    else
                    {
                        int nx = cx + AntNavigation.Dx[k];
                        int ny = cy + AntNavigation.Dy[k];
                        if ((uint)nx >= (uint)_width || (uint)ny >= (uint)_height)
                            continue;
                        ni = ny * _width + nx;
                    }
                    if (_distance[ni] <= d + 1 || !walkable[ni])
                        continue;
                    if (_distance[ni] == Unreachable)
                        _reached++;
                    _distance[ni] = d + 1;
                    scratch.Wave.Add(((long)(d + 1) << 32) | (uint)ni);
                }
            }
        }

        /// <summary>Por que um <see cref="Update"/> caiu em <see cref="Build"/> (diagnóstico do painel e dos testes).</summary>
        public enum Fallbacks
        {
            NotSynced,
            TooManyChanges,
            TooManySourceChanges,
            AllSourcesGone,
            TooManyInvalid,
        }

        /// <summary>Quantas vezes cada motivo fez <see cref="Update"/> refazer o campo do zero.</summary>
        public readonly int[] FallbackCounts = new int[5];

        /// <summary>Tempo total (ticks do cronômetro) e chamadas de <see cref="Update"/>, para achar o campo mais caro.</summary>
        public long UpdateTicks;
        public int UpdateCalls;

        void Fallback(WalkabilityMap walkability, List<int> sourceIndices, Fallbacks reason)
        {
            FallbackCounts[(int)reason]++;
            Build(walkability, sourceIndices);
            LastUpdateWasIncremental = false;
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
