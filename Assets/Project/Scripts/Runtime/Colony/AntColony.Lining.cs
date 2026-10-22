using System.Collections.Generic;
using Fabula.Incrementant.Audio;
using Fabula.Incrementant.Juice;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>Valor do reboco gravado na grade (<see cref="PixelGrid.GetLining"/>): o ambiente + 1, 0 = sem reboco. Vai para o save: não renumerar.</summary>
    public static class RoomLining
    {
        public static byte KindOf(PaintRoom room) => (byte)((int)room + 1);

        public static bool TryGetRoom(byte kind, out PaintRoom room)
        {
            room = (PaintRoom)(kind - 1);
            return kind != 0 && kind <= PaintBudget.RoomCount;
        }

        /// <summary>Terra que a formiga reboca: pedra, rocha-mãe, plantas, restos e o cogumelo ficam como estão.</summary>
        public static bool CanLine(CellMaterial material) =>
            material == CellMaterial.Dirt || material == CellMaterial.Grass || material == CellMaterial.Sand ||
            material == CellMaterial.Spoil || material == CellMaterial.PackedSpoil || material == CellMaterial.PackedSand ||
            material == CellMaterial.Mud;
    }

    /// <summary>
    /// Salas construídas (REQ-058, decisões do desenvolvedor em 2026-10-22). Os slots são as paredes de terra em contato com o
    /// vão de cada sala pintada, nas 8 direções: piso, teto, lados e quinas em volta da área pintada e a terra dentro dela que
    /// encosta no vão (correção do desenvolvedor no mesmo dia; antes o teto e o que ficava dentro da área eram naturais). A
    /// formiga leva um pixel de terra a cada um e o reboca, e a célula passa a desenhar o bloco do ambiente
    /// (<see cref="PixelGrid.GetLining"/>). Com o reboco completo e nada mais para cavar, o trecho conectado da sala (vizinhança
    /// de 4 lados, como no limite de pintura) fica pronto e o vão desenha o fundo do ambiente (<see cref="PixelGrid.IsRoomFinished"/>).
    /// Só visual: a sala funciona igual desde a pintura e o vão não perde células. O reboco é obra (seção Obra): mesma terra
    /// (pilha de fora ou depósito de construção), mesmo transporte e mesmo encaixe das marcas de construção.
    /// </summary>
    public sealed partial class AntColony
    {
        // Slots sem reboco (índice da célula), refeitos quando a pintura, a estrutura ou o reboco mudam.
        readonly HashSet<int> _liningSlots = new HashSet<int>();
        // Varredura das salas: células já vistas e o trecho em curso.
        readonly HashSet<int> _roomSeen = new HashSet<int>();
        readonly List<int> _roomPart = new List<int>();
        // Células de sala pronta: as marcadas na grade e as da varredura nova.
        HashSet<int> _finishedRooms = new HashSet<int>();
        HashSet<int> _finishedNext = new HashSet<int>();
        int _liningStructure = -1, _liningMarks = -1, _liningGridVersion = -1;
        // Slots sem reboco que alguma formiga alcança (vizinho caminhável ligado à saída), contados na rodada da obra.
        int _liningReachable;

        /// <summary>Slots de reboco ainda sem terra.</summary>
        public int LiningPending => _liningSlots.Count;

        /// <summary>A célula é um slot de reboco ainda sem terra (da última rodada de campos).</summary>
        public bool HasLiningSlot(int x, int y) => _grid.InBounds(x, y) && _liningSlots.Contains(_grid.ToIndex(x, y));

        /// <summary>Slots rebocados pelas formigas nesta sessão (não vai para o save).</summary>
        public int CellsLined { get; private set; }

        /// <summary>Obra pedida: marcas de construção mais os slots de reboco que alguma formiga alcança.</summary>
        int BuildWorkCount => _grid.BuildMarkedCount + _liningReachable;

        bool LiningStale =>
            _grid.StructureVersion != _liningStructure || _grid.MarkVersion != _liningMarks || _grid.LiningVersion != _liningGridVersion;

        /// <summary>
        /// Célula que pode receber reboco (quem chama confere o contato com o vão): terra (<see cref="RoomLining.CanLine"/>) no
        /// formigueiro, sem marca de escavação (vai virar túnel). Dentro de uma sala, a terra fofa fica de fora: é o canteiro ou
        /// o teto da sala Gotas de Orvalho, que as formigas puseram ali de propósito.
        /// </summary>
        bool IsLiningSlot(int x, int y)
        {
            if (!_grid.InBounds(x, y) || y > _grid.GetSurfaceY(x) || _grid.IsMarked(x, y))
                return false;
            CellMaterial material = _grid.Get(x, y);
            if (!RoomLining.CanLine(material))
                return false;
            return !_grid.IsAnyRoom(x, y) || material != CellMaterial.Spoil && _grid.GetBedStage(x, y) == BedStage.None;
        }

        /// <summary>
        /// Refaz os slots sem reboco e as salas prontas. Varre só os blocos com sala; cada trecho conectado de um ambiente é
        /// visto uma vez. Roda na rodada de campos quando a pintura, a estrutura ou o reboco mudam.
        /// </summary>
        void UpdateRoomLining()
        {
            _liningStructure = _grid.StructureVersion;
            _liningMarks = _grid.MarkVersion;
            _liningGridVersion = _grid.LiningVersion;
            _liningSlots.Clear();
            _roomSeen.Clear();
            _finishedNext.Clear();

            int w = _grid.Width;
            RectInt area = _grid.Active;
            int cxMin = area.xMin >> PixelGrid.ChunkShift, cxMax = (area.xMax - 1) >> PixelGrid.ChunkShift;
            int cyMin = area.yMin >> PixelGrid.ChunkShift, cyMax = (area.yMax - 1) >> PixelGrid.ChunkShift;
            for (int cy = cyMin; cy <= cyMax; cy++)
            for (int cx = cxMin; cx <= cxMax; cx++)
            {
                if (_grid.GetChunkAnyRoomCount(cx, cy) == 0)
                    continue;
                int x0 = Mathf.Max(area.xMin, cx << PixelGrid.ChunkShift);
                int x1 = Mathf.Min(area.xMax, (cx + 1) << PixelGrid.ChunkShift);
                int y0 = Mathf.Max(area.yMin, cy << PixelGrid.ChunkShift);
                int y1 = Mathf.Min(area.yMax, (cy + 1) << PixelGrid.ChunkShift);
                for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                {
                    int i = y * w + x;
                    if (!_roomSeen.Contains(i) && PaintBudget.TryGetRoomAt(_grid, x, y, out PaintRoom room))
                        ScanRoomPart(i, room);
                }
            }

            foreach (int i in _finishedRooms)
            {
                if (!_finishedNext.Contains(i))
                    _grid.SetRoomFinished(i % w, i / w, false);
            }
            foreach (int i in _finishedNext)
                _grid.SetRoomFinished(i % w, i / w, true);
            (_finishedRooms, _finishedNext) = (_finishedNext, _finishedRooms);
        }

        /// <summary>
        /// Percorre o trecho conectado do ambiente a partir de <paramref name="start"/>, junta os slots sem reboco em volta do vão
        /// e, se não sobrar nenhum nem marca de escavação no trecho, põe as células dele entre as de sala pronta.
        /// </summary>
        void ScanRoomPart(int start, PaintRoom room)
        {
            int w = _grid.Width;
            _roomPart.Clear();
            _roomPart.Add(start);
            _roomSeen.Add(start);
            bool open = false, pending = false, digging = false;
            for (int head = 0; head < _roomPart.Count; head++)
            {
                int i = _roomPart[head];
                int x = i % w, y = i / w;
                digging |= _grid.IsMarked(x, y);
                if (!_grid.IsSolid(x, y))
                {
                    open = true;
                    for (int k = 0; k < 8; k++)
                    {
                        int nx = x + AntNavigation.Dx[k], ny = y + AntNavigation.Dy[k];
                        if (_grid.GetLining(nx, ny) != 0 || !IsLiningSlot(nx, ny))
                            continue;
                        pending = true;
                        _liningSlots.Add(ny * w + nx);
                    }
                }
                for (int k = 0; k < 4; k++)
                {
                    int nx = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ny = y + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (!_grid.InBounds(nx, ny))
                        continue;
                    int ni = ny * w + nx;
                    if (!_roomSeen.Contains(ni) && PaintBudget.IsRoom(_grid, room, nx, ny))
                    {
                        _roomSeen.Add(ni);
                        _roomPart.Add(ni);
                    }
                }
            }
            if (!open || pending || digging)
                return;
            foreach (int i in _roomPart)
                _finishedNext.Add(i);
        }

        /// <summary>
        /// Rodada da obra: as células caminháveis ligadas à saída ao lado de um slot sem reboco viram origem do campo da obra
        /// (as do slot já reservado, só da fila de espera). Conta os slots alcançáveis, que entram na obra pedida. O reboco é
        /// construção como a obra marcada e vem antes das automatizações (canteiros, teto do orvalho, área de resíduos,
        /// abastecimento dos depósitos): decisão do desenvolvedor em 2026-10-22, depois de a sala não ser rebocada em jogo
        /// quando esperava por elas.
        /// </summary>
        void AddLiningSources()
        {
            _liningReachable = 0;
            int w = _grid.Width;
            foreach (int slot in _liningSlots)
            {
                int x = slot % w, y = slot / w;
                bool reserved = _reserved.Contains(slot);
                bool reachable = false;
                for (int k = 0; k < 8; k++)
                {
                    int nx = x + AntNavigation.Dx[k], ny = y + AntNavigation.Dy[k];
                    if (!_walkability.IsWalkable(nx, ny) || _grid.IsSolid(nx, ny) || _exitField.Get(nx, ny) == FlowField.Unreachable)
                        continue;
                    reachable = true;
                    if (!reserved)
                        _buildSources.Add(ny * w + nx);
                    _siteSources.Add(ny * w + nx);
                }
                if (reachable)
                    _liningReachable++;
            }
        }

        /// <summary>Slot sem reboco e livre: a formiga com terra ao lado dele pode rebocá-lo.</summary>
        bool IsLiningOpen(int x, int y)
        {
            if (!_grid.InBounds(x, y))
                return false;
            int i = _grid.ToIndex(x, y);
            return _liningSlots.Contains(i) && !_reserved.Contains(i) && _grid.GetLining(x, y) == 0 && IsLiningSlot(x, y);
        }

        /// <summary>Ambiente do reboco do slot: o da própria célula (parede dentro da área pintada) ou o do vão que encosta nela.</summary>
        byte LiningKindAt(int x, int y)
        {
            if (PaintBudget.TryGetRoomAt(_grid, x, y, out PaintRoom room))
                return RoomLining.KindOf(room);
            for (int k = 0; k < 8; k++)
            {
                int rx = x + AntNavigation.Dx[k], ry = y + AntNavigation.Dy[k];
                if (!_grid.IsSolid(rx, ry) && PaintBudget.TryGetRoomAt(_grid, rx, ry, out room))
                    return RoomLining.KindOf(room);
            }
            return 0;
        }

        /// <summary>Reboca o slot reservado pela formiga com o pixel de cima da carga; se ele deixou de valer, ela segue com a terra.</summary>
        void FinishLining(Ant ant)
        {
            int x = ant.DigX, y = ant.DigY;
            if (ant.Carried == CellMaterial.GlowMushroom || !_grid.InBounds(x, y) || !_liningSlots.Contains(_grid.ToIndex(x, y)) ||
                _grid.GetLining(x, y) != 0 || !IsLiningSlot(x, y))
                return;
            byte kind = LiningKindAt(x, y);
            if (kind == 0)
                return;
            _grid.Set(x, y, CellMaterials.ToPacked(ant.Carried), ant.CarriedShade);
            _grid.SetLining(x, y, kind);
            CellsLined++;
            Emit(SfxId.Build, x, y);
            EmitFx(FxId.Build, x, y, ant.Carried);
            ant.DropTop();
            ant.State = ant.CarriedCount > 0 ? AntState.Hauling : AntState.Idle;
        }
    }
}
