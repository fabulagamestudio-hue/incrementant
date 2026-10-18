using System.Collections.Generic;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Ataques de cupins (pedido do desenvolvedor em 2026-10-18). Depois de a colônia chegar a <see cref="RaidSettings.MinAnts"/>
    /// formigas, a cada 1 a 5 minutos vem um alerta e, no fim dele, um bando de cupins (40% das formigas) desce voando em
    /// pontos sorteados do mapa, perde as asas e vai ao formigueiro: destrói as crias, depois os canteiros, depois duas
    /// camadas de parede em volta dos setores (menos a sala da rainha) e sai pelas laterais do mapa. Enquanto há cupim no
    /// chão, as formigas largam o trabalho e vão mordê-los. O cupim morto vira um corpo que as formigas desmembram em folhas
    /// (para os canteiros) e restos (para a área de resíduos, ver <c>AntColony.Waste</c>). Depois do primeiro ataque, o
    /// soldado e a área de resíduos ficam liberados. Cupins, corpos e o ataque em curso não vão para o save.
    /// </summary>
    public sealed partial class AntColony
    {
        readonly List<Termite> _termites = new List<Termite>();
        readonly List<int> _raidSources = new List<int>();
        readonly HashSet<int> _raidSourceSeen = new HashSet<int>();
        readonly HashSet<int> _wallTargets = new HashSet<int>();
        readonly List<int> _wallStale = new List<int>();
        // Criados só no primeiro uso: cada campo custa memória do tamanho da grade.
        FlowField _raidField;
        FlowField _combatField;
        System.Random _raidRng;
        float _raidTimer;
        float _stallTimer;
        float _leaveTimer;
        bool _raidDirty;
        bool _raidFieldsIdle = true;
        int _groundTermites;
        int _corpsePieces;
        int _dismemberers;

        /// <summary>Números do ataque e do combate. <see cref="AntFarmGame"/> passa os do Inspector.</summary>
        public RaidSettings RaidSettings { get; set; } = new RaidSettings();

        /// <summary>
        /// Os ataques acontecem (o jogo liga; desligado nos testes que não tratam deles, para não mudar o resto da simulação).
        /// </summary>
        public bool RaidsEnabled { get; set; }

        /// <summary>A colônia já chegou ao mínimo de formigas: o relógio dos ataques corre (vai para o save).</summary>
        public bool RaidsUnlocked { get; private set; }

        /// <summary>Ataques que terminaram (todos os cupins mortos ou idos). O primeiro libera o soldado e a área de resíduos.</summary>
        public int RaidsSurvived { get; private set; }

        /// <summary>O pincel da área de resíduos está liberado (depois do primeiro ataque).</summary>
        public bool WasteUnlocked => RaidsSurvived > 0;

        public RaidState RaidState { get; private set; }

        /// <summary>O que os cupins destroem agora.</summary>
        public RaidObjective RaidObjective { get; private set; }

        /// <summary>No alerta, segundos até os cupins chegarem; sem ataque, até o próximo alerta (se os ataques já começaram).</summary>
        public float SecondsToNextRaid => _raidTimer;

        /// <summary>Cupins do ataque em curso, vivos e mortos (corpos).</summary>
        public IReadOnlyList<Termite> Termites => _termites;

        /// <summary>Cupins vivos (voando ou no chão).</summary>
        public int LiveTermites
        {
            get
            {
                int n = 0;
                foreach (Termite t in _termites)
                    n += t.Alive ? 1 : 0;
                return n;
            }
        }

        /// <summary>Teto de cupins por formiga para o próximo ataque (cresce a cada ataque que termina).</summary>
        public float TermiteCap => RaidSettings.TermiteCap(RaidsSurvived);

        /// <summary>Cupins por formiga sorteados no último ataque (0 = nenhum ainda; não vai para o save).</summary>
        public float LastRaidShare { get; private set; }

        public int TermitesKilled { get; private set; }
        public int TermitesEscaped { get; private set; }
        public int AntsKilledInCombat { get; private set; }
        public int BroodDestroyed { get; private set; }
        public int BedsDestroyed { get; private set; }
        public int WallsDestroyed { get; private set; }

        /// <summary>Restaura o estado dos ataques de um save (o ataque em curso não é salvo: volta sem cupins).</summary>
        public void RestoreRaids(bool raidsUnlocked, int raidsSurvived, float secondsToNextRaid)
        {
            RaidsUnlocked = raidsUnlocked;
            RaidsSurvived = Mathf.Max(0, raidsSurvived);
            Tree.SoldiersAvailable = RaidsSurvived > 0;
            _raidTimer = Mathf.Max(0f, secondsToNextRaid);
            _termites.Clear();
            RaidState = RaidState.Calm;
            _raidDirty = true;
        }

        /// <summary>
        /// Começa um ataque agora, mesmo antes do mínimo de formigas (cheat F4 e testes). Sem
        /// <paramref name="skipWarning"/>, passa pelo alerta antes.
        /// </summary>
        public void ForceRaid(bool skipWarning = false)
        {
            if (RaidState == RaidState.Active)
                return;
            if (skipWarning)
            {
                LaunchRaid();
                return;
            }
            RaidState = RaidState.Warning;
            _raidTimer = RaidSettings.WarningSeconds;
        }

        /// <summary>Um cupim já pousado na célula (testes e depuração). Abre um ataque se não houver um.</summary>
        public Termite SpawnTermite(int x, int y)
        {
            if (RaidState != RaidState.Active)
                BeginRaid();
            Termite t = NewTermite(x, y);
            t.Phase = TermitePhase.Raiding;
            _termites.Add(t);
            return t;
        }

        System.Random RaidRng => _raidRng ??= new System.Random(_seed ^ 0x5EED7E);

        float RollRaidInterval() =>
            Mathf.Lerp(RaidSettings.MinIntervalSeconds, RaidSettings.MaxIntervalSeconds, (float)RaidRng.NextDouble());

        // ---------------------------------------------------------------- relógio e chegada

        /// <summary>Relógio do próximo ataque, alerta, voo e pouso. Roda no começo do Tick.</summary>
        void TickRaids(float deltaTime)
        {
            if (RaidsEnabled && !RaidsUnlocked && _ants.Count >= RaidSettings.MinAnts)
            {
                RaidsUnlocked = true;
                // Um ataque forçado (F4) já pode estar no alerta: o relógio dele não é trocado pelo do próximo.
                if (RaidState == RaidState.Calm)
                    _raidTimer = RollRaidInterval();
            }

            switch (RaidState)
            {
                case RaidState.Calm:
                    if (!RaidsEnabled || !RaidsUnlocked)
                        break;
                    _raidTimer -= deltaTime;
                    if (_raidTimer <= 0f)
                    {
                        RaidState = RaidState.Warning;
                        _raidTimer = RaidSettings.WarningSeconds;
                    }
                    break;
                case RaidState.Warning:
                    _raidTimer -= deltaTime;
                    if (_raidTimer <= 0f)
                        LaunchRaid();
                    break;
                case RaidState.Active:
                    TickActiveRaid(deltaTime);
                    break;
            }

            _groundTermites = 0;
            foreach (Termite t in _termites)
                _groundTermites += t.OnGround ? 1 : 0;
        }

        void BeginRaid()
        {
            RaidState = RaidState.Active;
            RaidObjective = RaidObjective.Brood;
            _stallTimer = 0f;
            _leaveTimer = 0f;
            _raidElapsed = 0f;
            _wallTargets.Clear();
            _wallsCollected = false;
            _raidDirty = true;
        }

        float _raidElapsed;
        bool _wallsCollected;

        /// <summary>Os cupins aparecem no céu: um por <see cref="RaidSettings.TermitesPerAnt"/> formiga, em colunas sorteadas.</summary>
        void LaunchRaid()
        {
            BeginRaid();
            // Sorteio entre o mínimo (40%) e o teto, que cresce a cada ataque até 200% (pedido do desenvolvedor em 2026-10-18).
            float share = Mathf.Lerp(RaidSettings.TermitesPerAnt, TermiteCap, (float)RaidRng.NextDouble());
            LastRaidShare = share;
            int count = Mathf.Max(1, Mathf.CeilToInt(_ants.Count * share));
            for (int i = 0; i < count; i++)
            {
                if (!TryPickLanding(out int x, out int y))
                    continue;
                Termite t = NewTermite(x, y);
                t.Phase = TermitePhase.Flying;
                t.PhaseTimer = RaidSettings.FlightSeconds * (0.8f + 0.4f * (float)RaidRng.NextDouble());
                t.PhaseStart = SimulationTime;
                t.PhaseDuration = t.PhaseTimer;
                t.FlyFromX = x + RaidRng.Next(-24, 25);
                t.FlyFromY = y + RaidSettings.FlightHeight * (0.8f + 0.4f * (float)RaidRng.NextDouble());
                t.Body.FacingX = t.FlyFromX <= x ? 1 : -1;
                _termites.Add(t);
            }
            // Sem lugar para pousar (mapa todo de água?), o ataque termina sem cupins no próximo passo.
        }

        Termite NewTermite(int x, int y)
        {
            var t = new Termite { Health = RaidSettings.TermiteHealth };
            t.Body.X = x;
            t.Body.Y = y;
            t.Body.FacingX = 1;
            t.Body.SpeedOverride = RaidSettings.TermiteSpeed;
            SnapVisual(t.Body);
            t.Body.MoveStart = SimulationTime;
            return t;
        }

        /// <summary>
        /// Um ponto de pouso sorteado na área ativa: a primeira célula vazia, de cima para baixo, com algo sólido embaixo
        /// (chão, monte, planta, obra), sem água e caminhável.
        /// </summary>
        bool TryPickLanding(out int x, out int y)
        {
            RectInt area = _grid.Active;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                x = RaidRng.Next(area.xMin + 3, Mathf.Max(area.xMin + 4, area.xMax - 3));
                y = area.yMax - 1;
                while (y > area.yMin + 1 && !_grid.IsSolid(x, y - 1))
                    y--;
                if (_grid.IsSolid(x, y) || _grid.HasWater(x, y) || !AntNavigation.IsWalkable(_grid, x, y))
                    continue;
                return true;
            }
            x = y = 0;
            return false;
        }

        void TickActiveRaid(float deltaTime)
        {
            _raidElapsed += deltaTime;
            foreach (Termite t in _termites)
            {
                if (t.Phase != TermitePhase.Flying && t.Phase != TermitePhase.Landing)
                    continue;
                t.PhaseTimer -= deltaTime;
                if (t.PhaseTimer > 0f)
                    continue;
                if (t.Phase == TermitePhase.Flying)
                {
                    t.Phase = TermitePhase.Landing;
                    t.PhaseTimer = RaidSettings.LandingSeconds;
                    t.PhaseStart = SimulationTime;
                    t.PhaseDuration = t.PhaseTimer;
                }
                else
                {
                    t.Phase = RaidObjective == RaidObjective.Leave ? TermitePhase.Leaving : TermitePhase.Raiding;
                }
                _raidDirty = true;
            }

            if (RaidObjective == RaidObjective.Leave)
            {
                // Quem não acha a saída (preso num buraco) acaba indo embora voando.
                _leaveTimer += deltaTime;
                if (_leaveTimer > RaidSettings.StallSeconds)
                {
                    for (int i = _termites.Count - 1; i >= 0; i--)
                    {
                        if (!_termites[i].Alive)
                            continue;
                        _termites.RemoveAt(i);
                        TermitesEscaped++;
                    }
                }
            }
            else
            {
                _stallTimer += deltaTime;
                if (_stallTimer > RaidSettings.StallSeconds)
                    AdvanceObjective();
                if (_raidElapsed > RaidSettings.MaxRaidSeconds)
                    SetObjective(RaidObjective.Leave);
            }

            if (LiveTermites == 0)
                EndRaid();
        }

        /// <summary>Todos os cupins morreram ou foram embora: conta o ataque e sorteia o próximo. Os corpos ficam.</summary>
        void EndRaid()
        {
            RaidState = RaidState.Calm;
            RaidsSurvived++;
            Tree.SoldiersAvailable = true;
            _raidTimer = RollRaidInterval();
            _raidDirty = true;
        }

        void AdvanceObjective()
        {
            if (RaidObjective < RaidObjective.Leave)
                SetObjective(RaidObjective + 1);
        }

        void SetObjective(RaidObjective objective)
        {
            RaidObjective = objective;
            _stallTimer = 0f;
            _leaveTimer = 0f;
            _raidDirty = true;
            foreach (Termite t in _termites)
            {
                t.WorkCell = -1;
                t.WorkBrood = -1;
                if (objective == RaidObjective.Leave && t.Phase == TermitePhase.Raiding)
                    t.Phase = TermitePhase.Leaving;
            }
        }

        // ---------------------------------------------------------------- cupins

        /// <summary>Movimento, mordidas e destruição dos cupins no chão; tira as formigas mortas em combate. Depois das formigas.</summary>
        void UpdateTermites(float deltaTime)
        {
            for (int i = _termites.Count - 1; i >= 0; i--)
            {
                Termite t = _termites[i];
                switch (t.Phase)
                {
                    case TermitePhase.Raiding:
                    case TermitePhase.Leaving:
                        if (UpdateTermite(t, deltaTime))
                        {
                            _termites.RemoveAt(i);
                            TermitesEscaped++;
                            _raidDirty = true;
                        }
                        break;
                    case TermitePhase.Dead:
                        if (t.LeavesLeft + t.RemainsLeft <= 0)
                        {
                            _termites.RemoveAt(i);
                            _raidDirty = true;
                        }
                        else
                        {
                            UpdateCorpse(t);
                        }
                        break;
                }
            }
            RemoveCasualties();
        }

        /// <summary>Um passo de um cupim vivo no chão. <c>true</c> = saiu do mapa.</summary>
        bool UpdateTermite(Termite t, float deltaTime)
        {
            Ant body = t.Body;
            int x = body.X, y = body.Y;
            if (UpdateSupport(body, deltaTime))
            {
                _raidDirty |= body.X != x || body.Y != y;
                return false;
            }
            t.AttackTimer -= deltaTime;
            body.Cooldown -= deltaTime;

            // Formiga ao alcance: para e morde (a autodefesa das formigas faz o mesmo do outro lado).
            Ant victim = AntInReach(body.X, body.Y);
            if (victim != null)
            {
                t.WorkCell = -1;
                t.WorkBrood = -1;
                body.State = AntState.Digging;
                body.DigX = victim.X;
                body.DigY = victim.Y;
                if (victim.X != body.X)
                    body.FacingX = victim.X > body.X ? 1 : -1;
                if (t.AttackTimer <= 0f)
                {
                    t.AttackTimer = RaidSettings.AttackSeconds;
                    victim.Wounds += RaidSettings.TermiteDamage;
                    victim.LastHitTime = SimulationTime;
                }
                return false;
            }
            if (body.Cooldown > 0f)
                return false;

            if (t.WorkCell >= 0)
            {
                FinishDestroy(t);
                return false;
            }
            body.State = AntState.Idle;

            FlowField field = _raidField;
            int d = field != null ? field.Get(body.X, body.Y) : FlowField.Unreachable;
            if (t.Phase == TermitePhase.Leaving)
            {
                if (d == 0 && RaidObjective == RaidObjective.Leave)
                    return true;
                if (d != FlowField.Unreachable && d > 0 && StepDownhill(body, field))
                    _raidDirty = true;
                else
                    WanderTermite(body);
                return false;
            }

            if (d == 0)
            {
                if (!TryStartDestroy(t))
                    Hold(body);
                return false;
            }
            if (d != FlowField.Unreachable && StepDownhill(body, field))
            {
                _raidDirty = true;
                return false;
            }
            WanderTermite(body);
            return false;
        }

        void WanderTermite(Ant body)
        {
            Wander(body);
            _raidDirty = true;
        }

        /// <summary>O cupim morto cai se ficar sem apoio (não se agarra como as vivas).</summary>
        void UpdateCorpse(Termite t)
        {
            Ant body = t.Body;
            if (_grid.IsSolid(body.X, body.Y) && !AntNavigation.IsWalkable(_grid, body.X, body.Y))
                PushOutOfSolid(body);
            if (!AntNavigation.IsWalkable(_grid, body.X, body.Y) && !_grid.IsSolid(body.X, body.Y - 1) && _grid.InBounds(body.X, body.Y - 1))
            {
                BeginMove(body, 0.05f);
                body.Y--;
                _raidDirty = true;
            }
        }

        /// <summary>A formiga mais perto ao alcance da mordida (distância de xadrez), ou null.</summary>
        Ant AntInReach(int x, int y)
        {
            int reach = RaidSettings.AttackReach;
            Ant best = null;
            int bestDistance = int.MaxValue;
            foreach (Ant ant in _ants)
            {
                int distance = Mathf.Max(Mathf.Abs(ant.X - x), Mathf.Abs(ant.Y - y));
                if (distance > reach || distance >= bestDistance)
                    continue;
                best = ant;
                bestDistance = distance;
            }
            return best;
        }

        /// <summary>O cupim vivo no chão mais perto ao alcance da mordida, ou null.</summary>
        Termite TermiteInReach(int x, int y)
        {
            int reach = RaidSettings.AttackReach;
            Termite best = null;
            int bestDistance = int.MaxValue;
            foreach (Termite t in _termites)
            {
                if (!t.OnGround)
                    continue;
                int distance = Mathf.Max(Mathf.Abs(t.X - x), Mathf.Abs(t.Y - y));
                if (distance > reach || distance >= bestDistance)
                    continue;
                best = t;
                bestDistance = distance;
            }
            return best;
        }

        /// <summary>Formigas que passaram da vida do papel morrem (o que levavam se perde, o ovo volta ao chão).</summary>
        void RemoveCasualties()
        {
            for (int i = _ants.Count - 1; i >= 0; i--)
            {
                Ant ant = _ants[i];
                if (ant.Wounds < Tree.MaxHealth(ant.Role))
                    continue;
                RemoveAnt(i);
                AntsKilledInCombat++;
            }
        }

        /// <summary>Recupera 1 ponto de vida por segundo depois de um tempo sem levar mordida (design 27).</summary>
        void HealAnts(float deltaTime)
        {
            float delay = RaidSettings.HealDelaySeconds;
            foreach (Ant ant in _ants)
            {
                if (ant.Wounds > 0f && SimulationTime - ant.LastHitTime >= delay)
                    ant.Wounds = Mathf.Max(0f, ant.Wounds - deltaTime);
            }
        }

        void HitTermite(Termite t, float damage)
        {
            t.Health -= damage;
            t.LastHitTime = SimulationTime;
            if (t.Health > 0f)
                return;
            t.Phase = TermitePhase.Dead;
            t.Body.State = AntState.Idle;
            t.WorkCell = -1;
            t.WorkBrood = -1;
            t.LeavesLeft = RaidSettings.CorpseLeaves;
            t.RemainsLeft = RaidSettings.CorpseRemains;
            TermitesKilled++;
            _raidDirty = true;
        }

        // ---------------------------------------------------------------- destruição

        /// <summary>Encostado no objetivo: começa a destruir a cria, o canteiro ou a célula de parede vizinha.</summary>
        bool TryStartDestroy(Termite t)
        {
            Ant body = t.Body;
            int cell = -1, brood = -1;
            switch (RaidObjective)
            {
                case RaidObjective.Brood:
                    foreach (BroodRecord b in _brood.Brood)
                    {
                        if (!TryGetBroodCell(b, out int bx, out int by) ||
                            Mathf.Max(Mathf.Abs(bx - body.X), Mathf.Abs(by - body.Y)) > 1)
                            continue;
                        cell = _grid.ToIndex(bx, by);
                        brood = b.Id;
                        break;
                    }
                    break;
                case RaidObjective.Farms:
                    cell = AdjacentCell(body.X, body.Y, (x, y) => _grid.GetBedStage(x, y) != BedStage.None);
                    break;
                case RaidObjective.Walls:
                    cell = AdjacentCell(body.X, body.Y, (x, y) => _wallTargets.Contains(_grid.ToIndex(x, y)) && _grid.IsSolid(x, y));
                    break;
            }
            if (cell < 0)
            {
                _raidDirty = true; // o campo ainda aponta para um alvo que sumiu
                return false;
            }
            t.WorkCell = cell;
            t.WorkBrood = brood;
            body.State = AntState.Digging;
            body.DigX = cell % _grid.Width;
            body.DigY = cell / _grid.Width;
            if (body.DigX != body.X)
                body.FacingX = body.DigX > body.X ? 1 : -1;
            body.Cooldown = RaidSettings.DestroySeconds;
            return true;
        }

        int AdjacentCell(int x, int y, System.Func<int, int, bool> match)
        {
            for (int k = 0; k < 8; k++)
            {
                int nx = x + AntNavigation.Dx[k], ny = y + AntNavigation.Dy[k];
                if (_grid.InBounds(nx, ny) && match(nx, ny))
                    return _grid.ToIndex(nx, ny);
            }
            return -1;
        }

        void FinishDestroy(Termite t)
        {
            int x = t.WorkCell % _grid.Width, y = t.WorkCell / _grid.Width;
            int brood = t.WorkBrood;
            t.WorkCell = -1;
            t.WorkBrood = -1;
            t.Body.State = AntState.Idle;
            bool done = false;
            switch (RaidObjective)
            {
                case RaidObjective.Brood:
                    if (brood >= 0 && _brood.Remove(brood))
                    {
                        BroodDestroyed++;
                        _nurseryDirty = true;
                        done = true;
                    }
                    break;
                case RaidObjective.Farms:
                    if (_grid.GetBedStage(x, y) != BedStage.None)
                    {
                        MakeRubble(x, y);
                        BedsDestroyed++;
                        done = true;
                    }
                    break;
                case RaidObjective.Walls:
                    if (_wallTargets.Remove(_grid.ToIndex(x, y)) && _grid.IsSolid(x, y))
                    {
                        MakeRubble(x, y);
                        WallsDestroyed++;
                        done = true;
                    }
                    break;
            }
            if (done)
                _stallTimer = 0f;
            _raidDirty = true;
        }

        /// <summary>A célula vira entulho solto, já marcado para as formigas levarem embora (o canteiro se desfaz junto).</summary>
        void MakeRubble(int x, int y)
        {
            byte shade = _grid.GetShade(x, y);
            _grid.Set(x, y, CellMaterial.Rubble, shade);
            _grid.SetMarked(x, y, true);
        }

        /// <summary>Célula onde a cria está: no chão onde a rainha a pôs, ou no ponto do berçário. A que está na boca de uma formiga não conta.</summary>
        bool TryGetBroodCell(BroodRecord b, out int x, out int y)
        {
            if (b.Phase == EggPhase.Laid || b.Phase == EggPhase.Claimed)
            {
                x = b.LaidX;
                y = b.LaidY;
                return true;
            }
            if (b.Phase == EggPhase.Placed && b.Slot >= 0 && b.Slot < _nurserySpots.Count)
            {
                x = _nurserySpots[b.Slot].x;
                y = _nurserySpots[b.Slot].y;
                return true;
            }
            x = y = 0;
            return false;
        }

        /// <summary>
        /// Paredes em volta dos setores: células sólidas do formigueiro a até <see cref="RaidSettings.WallLayers"/> células de
        /// uma célula aberta do berçário, da sala de nutrientes ou da área de resíduos. Fica de fora o que está perto da sala
        /// da rainha, rocha, rocha-mãe, canteiros e resíduos. Montado uma vez, quando o objetivo passa a ser as paredes.
        /// </summary>
        void CollectWallTargets()
        {
            _wallsCollected = true;
            _wallTargets.Clear();
            int layers = RaidSettings.WallLayers;
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cy = cyMin; cy <= cyMax; cy++)
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                if (_grid.GetChunkNurseryRoomCount(cx, cy) == 0 && _grid.GetChunkNutrientRoomCount(cx, cy) == 0 &&
                    _grid.GetChunkWasteRoomCount(cx, cy) == 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    if (!IsSector(x, y) || _grid.IsSolid(x, y))
                        continue;
                    for (int dy = -layers; dy <= layers; dy++)
                    for (int dx = -layers; dx <= layers; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (IsWallCandidate(nx, ny, layers))
                            _wallTargets.Add(_grid.ToIndex(nx, ny));
                    }
                    if (_wallTargets.Count >= MaxWallTargets)
                        return;
                }
            }
        }

        const int MaxWallTargets = 20000;

        bool IsSector(int x, int y) => _grid.IsNurseryRoom(x, y) || _grid.IsNutrientRoom(x, y) || _grid.IsWasteRoom(x, y);

        bool IsWallCandidate(int x, int y, int layers)
        {
            if (!_grid.InBounds(x, y) || y > _grid.GetSurfaceY(x) || IsSector(x, y) || _grid.IsQueenRoom(x, y))
                return false;
            CellMaterial material = _grid.Get(x, y);
            if (!CellMaterials.IsDiggable(material) || material == CellMaterial.Rubble || _grid.GetBedStage(x, y) != BedStage.None)
                return false;
            for (int dy = -layers; dy <= layers; dy++)
            for (int dx = -layers; dx <= layers; dx++)
            {
                if (_grid.IsQueenRoom(x + dx, y + dy))
                    return false;
            }
            return true;
        }

        // ---------------------------------------------------------------- campos

        /// <summary>
        /// Campos dos cupins (até o objetivo, ou até a saída pela lateral), do combate (até os cupins no chão ou, sem eles, até
        /// os corpos com parte para tirar) e da área de resíduos. Parados (sem ataque, corpo nem área), não custam nada.
        /// </summary>
        void BuildRaidFields()
        {
            _raidDirty = false;
            BuildWasteField();
            if (_termites.Count == 0)
            {
                if (!_raidFieldsIdle)
                {
                    _raidField?.Invalidate();
                    _combatField?.Invalidate();
                    _raidFieldsIdle = true;
                }
                _corpsePieces = 0;
                return;
            }
            _raidFieldsIdle = false;
            _raidField ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);
            _combatField ??= new FlowField(_grid.Width, _grid.Height, _fieldScratch);

            if (RaidState == RaidState.Active)
            {
                // Sem alvo do objetivo atual, passa ao próximo (no máximo até sair).
                while (!CollectObjectiveSources() && RaidObjective < RaidObjective.Leave)
                    SetObjective(RaidObjective + 1);
            }
            else
            {
                _raidSources.Clear();
            }
            UpdateField(_raidField, _raidSources);

            CollectCombatSources();
            UpdateField(_combatField, _raidSources);
            _raidSources.Clear();
        }

        /// <summary>Origens do campo dos cupins para o objetivo atual; <c>false</c> se o objetivo não tem mais alvo.</summary>
        bool CollectObjectiveSources()
        {
            _raidSources.Clear();
            _raidSourceSeen.Clear();
            bool any = false;
            switch (RaidObjective)
            {
                case RaidObjective.Brood:
                    foreach (BroodRecord b in _brood.Brood)
                    {
                        if (!TryGetBroodCell(b, out int bx, out int by))
                            continue;
                        any = true;
                        AddRaidSource(bx, by);
                        AddRaidStand(bx, by);
                    }
                    break;
                case RaidObjective.Farms:
                    for (int k = 0; k < _grid.BedCount; k++)
                    {
                        if (!_grid.TryGetBed(k, out int bx, out int by))
                            break;
                        any = true;
                        AddRaidStand(bx, by);
                    }
                    break;
                case RaidObjective.Walls:
                    if (!_wallsCollected)
                        CollectWallTargets();
                    _wallStale.Clear();
                    int w = _grid.Width;
                    foreach (int i in _wallTargets)
                    {
                        if (!_grid.IsSolid(i % w, i / w))
                        {
                            _wallStale.Add(i);
                            continue;
                        }
                        any = true;
                        AddRaidStand(i % w, i / w);
                    }
                    foreach (int i in _wallStale)
                        _wallTargets.Remove(i);
                    break;
                default:
                    any = true;
                    CollectExitSources();
                    break;
            }
            return any;
        }

        /// <summary>Saídas: o céu caminhável nas duas colunas junto de cada lateral da área ativa.</summary>
        void CollectExitSources()
        {
            RectInt area = _grid.Active;
            int[] columns = { area.xMin + 1, area.xMin + 2, area.xMax - 3, area.xMax - 2 };
            foreach (int x in columns)
            {
                for (int y = Mathf.Max(area.yMin, _grid.GetSurfaceY(x) + 1); y < area.yMax; y++)
                    AddRaidSource(x, y);
            }
        }

        /// <summary>Cupins no chão (as células em volta de cada um); sem nenhum, os corpos que ainda têm parte para tirar.</summary>
        void CollectCombatSources()
        {
            _raidSources.Clear();
            _raidSourceSeen.Clear();
            _corpsePieces = 0;
            foreach (Termite t in _termites)
            {
                if (_groundTermites > 0)
                {
                    if (!t.OnGround)
                        continue;
                }
                else
                {
                    int pieces = CorpsePiecesAvailable(t);
                    if (pieces <= 0)
                        continue;
                    _corpsePieces += pieces;
                }
                AddRaidSource(t.X, t.Y);
                AddRaidStand(t.X, t.Y);
            }
        }

        void AddRaidStand(int x, int y)
        {
            for (int k = 0; k < 8; k++)
                AddRaidSource(x + AntNavigation.Dx[k], y + AntNavigation.Dy[k]);
        }

        void AddRaidSource(int x, int y)
        {
            if (!_grid.InBounds(x, y) || !_walkability.IsWalkable(x, y))
                return;
            int i = _grid.ToIndex(x, y);
            if (_raidSourceSeen.Add(i))
                _raidSources.Add(i);
        }

        // ---------------------------------------------------------------- formigas no combate

        /// <summary>
        /// Cupim no chão: a formiga larga o que fazia e vai lutar. A carga na mão fica (é entregue depois); o ovo ou a cria na
        /// boca volta ao chão, o nutriente que ia à rainha fica onde ela está e o bloco de água se perde.
        /// </summary>
        void InterruptForCombat(Ant ant)
        {
            switch (ant.State)
            {
                case AntState.FetchingEgg:
                case AntState.CarryingEgg:
                case AntState.MovingBrood:
                    _brood.ReleaseEgg(ant.EggId, ant.X, ant.Y);
                    _nurseryDirty = true;
                    break;
                case AntState.Delivering:
                    DropNutrientNear(ant.X, ant.Y);
                    break;
            }
            ReleaseDumpSide(ant);
            ant.ClearSeek();
            ant.Interacting = false;
            ant.ReturningSoil = false;
            ant.Biting = false;
            ant.State = AntState.Fighting;
            ant.Cooldown = 0f;
        }

        void DropNutrientNear(int x, int y)
        {
            for (int k = -1; k < 8; k++)
            {
                int nx = k < 0 ? x : x + AntNavigation.Dx[k];
                int ny = k < 0 ? y : y + AntNavigation.Dy[k];
                if (_grid.SetNutrient(nx, ny, true))
                    return;
            }
        }

        /// <summary>
        /// Um cupim mordeu a formiga há pouco (no último ciclo de mordidas) e segue ao alcance dela: ela se defende. É o
        /// gatilho das formigas que não são soldado, que não vão atrás dos cupins.
        /// </summary>
        bool IsUnderAttack(Ant ant) =>
            SimulationTime - ant.LastHitTime <= RaidSettings.AttackSeconds + 0.05f && TermiteInReach(ant.X, ant.Y) != null;

        /// <summary>
        /// Morde o cupim ao alcance. O soldado, sem cupim ao alcance, desce o campo do combate até o mais perto; as demais só
        /// se defendem: sem cupim ao alcance, voltam ao trabalho. Sem cupim no chão, todas voltam.
        /// </summary>
        void UpdateFighting(Ant ant)
        {
            ant.Biting = false;
            if (_groundTermites == 0)
            {
                ant.State = AntState.Idle;
                return;
            }
            Termite target = TermiteInReach(ant.X, ant.Y);
            if (target != null)
            {
                ant.Biting = true;
                ant.DigX = target.X;
                ant.DigY = target.Y;
                if (target.X != ant.X)
                    ant.FacingX = target.X > ant.X ? 1 : -1;
                HitTermite(target, Tree.Damage(ant.Role) * RaidSettings.AttackSeconds);
                ant.Cooldown = RaidSettings.AttackSeconds;
                return;
            }
            if (ant.Role != AntRole.Soldier)
            {
                ant.State = AntState.Idle;
                return;
            }
            int d = _combatField != null ? _combatField.Get(ant.X, ant.Y) : FlowField.Unreachable;
            if (d != FlowField.Unreachable && d > 0 && StepDownhill(ant, _combatField))
                return;
            if (d == 0)
            {
                Hold(ant);
                return;
            }
            WanderHome(ant);
        }

        // ---------------------------------------------------------------- corpos

        /// <summary>Partes que dá para tirar agora: os restos e, se algum canteiro espera planta, as folhas.</summary>
        int CorpsePiecesAvailable(Termite t)
        {
            if (t.Alive)
                return 0;
            return t.RemainsLeft + (CanHarvest ? t.LeavesLeft : 0);
        }

        /// <summary>Ociosa que retira corpos: se há parte de corpo sem ninguém indo tirá-la, vai desmembrar o mais perto.</summary>
        bool TryStartDismember(Ant ant)
        {
            if (!AntRoles.ClearsCorpses(ant.Role) || _groundTermites > 0 || _corpsePieces <= _dismemberers || _combatField == null)
                return false;
            if (_combatField.Get(ant.X, ant.Y) == FlowField.Unreachable)
                return false;
            ant.State = AntState.Dismembering;
            ant.Biting = false;
            _dismemberers++;
            UpdateDismembering(ant);
            return true;
        }

        /// <summary>Vai até o corpo, trabalha nele <see cref="RaidSettings.DismemberSeconds"/> e sai com uma folha ou um resto na boca.</summary>
        void UpdateDismembering(Ant ant)
        {
            Termite corpse = CorpseNear(ant.X, ant.Y);
            if (ant.Biting)
            {
                ant.Biting = false;
                if (corpse != null && TakeCorpsePiece(corpse, ant))
                {
                    StartDelivery(ant);
                    return;
                }
                ant.State = AntState.Idle;
                return;
            }
            if (corpse != null)
            {
                ant.Biting = true;
                ant.DigX = corpse.X;
                ant.DigY = corpse.Y;
                if (corpse.X != ant.X)
                    ant.FacingX = corpse.X > ant.X ? 1 : -1;
                ant.Cooldown = RaidSettings.DismemberSeconds;
                return;
            }
            int d = _combatField != null && _corpsePieces > 0 ? _combatField.Get(ant.X, ant.Y) : FlowField.Unreachable;
            if (d != FlowField.Unreachable && d > 0 && StepDownhill(ant, _combatField))
                return;
            ant.State = AntState.Idle;
            Hold(ant);
        }

        Termite CorpseNear(int x, int y)
        {
            foreach (Termite t in _termites)
            {
                if (CorpsePiecesAvailable(t) > 0 && Mathf.Abs(t.X - x) <= 1 && Mathf.Abs(t.Y - y) <= 1)
                    return t;
            }
            return null;
        }

        /// <summary>Tira uma parte: a folha se algum canteiro espera planta, senão um resto.</summary>
        bool TakeCorpsePiece(Termite corpse, Ant ant)
        {
            if (corpse.LeavesLeft > 0 && CanHarvest)
            {
                corpse.LeavesLeft--;
                ant.PickUp(CellMaterial.PlantLeaf, 128);
                _plantsInFlight++;
            }
            else if (corpse.RemainsLeft > 0)
            {
                corpse.RemainsLeft--;
                ant.PickUp(CellMaterial.Refuse, 128);
            }
            else
            {
                return false;
            }
            _raidDirty = true;
            return true;
        }
    }
}
