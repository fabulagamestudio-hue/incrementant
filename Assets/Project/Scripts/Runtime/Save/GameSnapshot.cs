using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Fabula.Incrementant.Colony;
using Fabula.Incrementant.World;
using UnityEngine;

namespace Fabula.Incrementant.Save
{
    /// <summary>Estado de uma formiga no arquivo de save (só o que a simulação precisa para continuar).</summary>
    public struct AntRecord
    {
        public int X, Y, FacingX;
        public byte State;
        public float Cooldown;
        public int DigX, DigY, DumpTargetX;
        public byte Carried, CarriedShade;
        public float UnderwaterSeconds;
    }

    /// <summary>
    /// Retrato da partida: terreno (com a água) da área ativa, colônia, chuva e cronômetro. Não guarda configuração (Inspector),
    /// relevo nem cenário: o mapa procedural volta da mesma seed e o da imagem volta da imagem da cena, e o retrato é
    /// aplicado por cima. Fica de fora o que se refaz sozinho: gotas no ar, campos de navegação, o estado do sorteio
    /// das formigas (o sorteio recomeça da seed) e o tempo de contato da terra com a água (a erosão recomeça a contar).
    /// As plantas da superfície são células comuns (vão em <see cref="Cells"/>); a fase de crescimento delas recomeça.
    /// </summary>
    public sealed class GameSnapshot
    {
        /// <summary>
        /// 2: água (camada, contadores e tempo debaixo d'água das formigas). 3: rainha e sala da rainha.
        /// 4: plantas, sala de nutrientes, nutrientes guardados e economia de nutrientes.
        /// 5: linhas ainda enterradas de cada planta (crescimento pela base).
        /// </summary>
        public const int CurrentVersion = 5;
        /// <summary>
        /// Versão mais antiga que ainda carrega (a 2 volta sem sala pintada e com a rainha no ninho; a 2 e a 3 voltam sem
        /// sala de nutrientes e com 0 nutrientes).
        /// </summary>
        const int OldestReadableVersion = 2;
        const int Magic = 0x544E4349;

        /// <summary>Origem do mapa (<c>MapSource</c> como int): um save não carrega em outro modo.</summary>
        public int MapSource;
        public int Seed;
        public int GridWidth;
        public int GridHeight;
        public RectInt Active;
        public double MatchSeconds;

        public int Weather;
        public int DissolvedCells;
        public int PackedCells;
        public int RainWaterCells;
        public int ErodedCells;
        /// <summary>Legado: a água não evapora mais (só é consumida no formigueiro). Mantido no arquivo, sempre 0.</summary>
        public int EvaporatedCells;
        public int AbsorbedCells;
        public int DrownedAnts;

        public int StoredPixels;
        public int TotalDelivered;
        public float SimulationTime;
        public int Nutrients;
        public int TotalNutrients;
        public int HarvestedPixels;
        public AntRecord[] Ants = Array.Empty<AntRecord>();
        /// <summary>False em saves da versão 2 (sem rainha): ela fica onde a colônia nova a pôs.</summary>
        public bool HasQueen;
        public int QueenX, QueenY, QueenFacingX;

        // Células da área ativa, linha a linha (y de baixo para cima).
        public byte[] Cells = Array.Empty<byte>();
        public byte[] Shades = Array.Empty<byte>();
        public byte[] Marked = Array.Empty<byte>();
        public byte[] Wetness = Array.Empty<byte>();
        public byte[] Water = Array.Empty<byte>();
        /// <summary>Sala da rainha pintada (1) por célula.</summary>
        public byte[] QueenRoom = Array.Empty<byte>();
        /// <summary>Por célula: bit 0 = sala de nutrientes pintada, bit 1 = nutriente guardado.</summary>
        public byte[] NutrientRoom = Array.Empty<byte>();
        /// <summary>
        /// Por coluna da área ativa: linhas da forma da planta com raiz ali ainda enterradas (até 255). Vazio em saves
        /// anteriores à versão 5: as plantas voltam todas para fora (a que estiver com a ponta colhida rebrota).
        /// </summary>
        public byte[] PlantSunk = Array.Empty<byte>();

        public static GameSnapshot Capture(PixelGrid grid, AntColony colony, RainSimulation rain, WaterSimulation water,
                                           int mapSource, int seed, double matchSeconds, VegetationSimulation vegetation = null)
        {
            RectInt area = grid.Active;
            int count = area.width * area.height;
            var snapshot = new GameSnapshot
            {
                MapSource = mapSource,
                Seed = seed,
                GridWidth = grid.Width,
                GridHeight = grid.Height,
                Active = area,
                MatchSeconds = matchSeconds,
                Weather = (int)rain.Weather,
                DissolvedCells = rain.DissolvedCells,
                PackedCells = rain.PackedCells,
                RainWaterCells = rain.WaterCells,
                ErodedCells = water.ErodedCells,
                AbsorbedCells = water.AbsorbedCells,
                DrownedAnts = colony.DrownedAnts,
                StoredPixels = colony.StoredPixels,
                TotalDelivered = colony.TotalDelivered,
                SimulationTime = colony.SimulationTime,
                Nutrients = colony.Nutrients,
                TotalNutrients = colony.TotalNutrients,
                HarvestedPixels = colony.HarvestedPixels,
                HasQueen = true,
                QueenX = colony.Queen.X,
                QueenY = colony.Queen.Y,
                QueenFacingX = colony.Queen.FacingX,
                Cells = new byte[count],
                Shades = new byte[count],
                Marked = new byte[count],
                Wetness = new byte[count],
                Water = new byte[count],
                QueenRoom = new byte[count],
                NutrientRoom = new byte[count],
            };

            int i = 0;
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++, i++)
            {
                snapshot.Cells[i] = (byte)grid.Get(x, y);
                snapshot.Shades[i] = grid.GetShade(x, y);
                snapshot.Marked[i] = grid.IsMarked(x, y) ? (byte)1 : grid.IsBuildMarked(x, y) ? (byte)2 : (byte)0;
                snapshot.Wetness[i] = grid.GetWetness(x, y);
                snapshot.Water[i] = grid.HasWater(x, y) ? (byte)1 : (byte)0;
                snapshot.QueenRoom[i] = grid.IsQueenRoom(x, y) ? (byte)1 : (byte)0;
                // Bits 2-7: segundos de cura que faltam (arredondados para cima, até 63). Saves antigos têm 0 = pronto.
                int cure = Mathf.Min(63, Mathf.CeilToInt(grid.GetNutrientCure(x, y)));
                snapshot.NutrientRoom[i] = (byte)((grid.IsNutrientRoom(x, y) ? 1 : 0) | (grid.HasNutrient(x, y) ? 2 : 0) | (cure << 2));
            }

            snapshot.PlantSunk = new byte[area.width];
            if (vegetation != null)
            {
                for (int x = area.xMin; x < area.xMax; x++)
                    snapshot.PlantSunk[x - area.xMin] = (byte)Mathf.Min(255, vegetation.GetSunkRows(x));
            }

            IReadOnlyList<Ant> ants = colony.Ants;
            snapshot.Ants = new AntRecord[ants.Count];
            for (int a = 0; a < ants.Count; a++)
            {
                Ant ant = ants[a];
                snapshot.Ants[a] = new AntRecord
                {
                    X = ant.X, Y = ant.Y, FacingX = ant.FacingX, State = (byte)ant.State, Cooldown = ant.Cooldown,
                    DigX = ant.DigX, DigY = ant.DigY, DumpTargetX = ant.DumpTargetX,
                    Carried = (byte)ant.Carried, CarriedShade = ant.CarriedShade, UnderwaterSeconds = ant.UnderwaterSeconds,
                };
            }
            return snapshot;
        }

        /// <summary>
        /// Escreve o terreno salvo na grade (que já tem o mapa regenerado). Amplia a área ativa até a salva e sobrescreve
        /// todas as células dela, então o que o gerador ainda não tinha criado ali também vem do save.
        /// </summary>
        public void ApplyTo(PixelGrid grid)
        {
            if (grid.Width != GridWidth || grid.Height != GridHeight)
                throw new InvalidDataException($"O save é de um mundo {GridWidth}x{GridHeight}, mas o atual é {grid.Width}x{grid.Height}.");

            grid.ExpandActive(Active);
            int i = 0;
            for (int y = Active.yMin; y < Active.yMax; y++)
            for (int x = Active.xMin; x < Active.xMax; x++, i++)
            {
                grid.Set(x, y, (CellMaterial)Cells[i], Shades[i]);
                if (Marked[i] == 1)
                    grid.SetMarked(x, y, true);
                else if (Marked[i] == 2)
                    grid.SetBuildMarked(x, y, true);
                if (Wetness[i] != 0)
                    grid.SetWetness(x, y, Wetness[i]);
                // Sobrescreve também a água que o gerador pôs (laguinhos) ou que Set empurrou para cima.
                grid.SetWater(x, y, Water[i] != 0);
                if (QueenRoom.Length != 0)
                    grid.SetQueenRoom(x, y, QueenRoom[i] != 0);
                if (NutrientRoom.Length != 0)
                {
                    grid.SetNutrientRoom(x, y, (NutrientRoom[i] & 1) != 0);
                    grid.SetNutrient(x, y, (NutrientRoom[i] & 2) != 0, NutrientRoom[i] >> 2);
                }
            }
        }

        public void ApplyTo(AntColony colony)
        {
            var ants = new List<Ant>(Ants.Length);
            foreach (AntRecord r in Ants)
            {
                ants.Add(new Ant
                {
                    X = r.X, Y = r.Y, FacingX = r.FacingX, State = (AntState)r.State, Cooldown = r.Cooldown,
                    DigX = r.DigX, DigY = r.DigY, DumpTargetX = r.DumpTargetX,
                    Carried = (CellMaterial)r.Carried, CarriedShade = r.CarriedShade, UnderwaterSeconds = r.UnderwaterSeconds,
                });
            }
            colony.RestoreState(StoredPixels, TotalDelivered, SimulationTime, ants, DrownedAnts);
            colony.RestoreNutrients(Nutrients, TotalNutrients, HarvestedPixels);
            if (HasQueen)
                colony.RestoreQueen(QueenX, QueenY, QueenFacingX);
        }

        /// <summary>Linhas enterradas das plantas (chame depois de <c>MarkPopulated</c>; as células já vêm da grade).</summary>
        public void ApplyTo(VegetationSimulation vegetation)
        {
            for (int i = 0; i < PlantSunk.Length; i++)
                vegetation.SetSunkRows(Active.xMin + i, PlantSunk[i]);
        }

        public void ApplyTo(RainSimulation rain)
        {
            rain.Weather = (Weather)Weather;
            rain.RestoreCounters(DissolvedCells, PackedCells, RainWaterCells);
        }

        public void ApplyTo(WaterSimulation water) => water.RestoreCounters(ErodedCells, AbsorbedCells);

        // ---------------------------------------------------------------- arquivo

        public void Write(Stream stream)
        {
            using (var header = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                header.Write(Magic);
                header.Write(CurrentVersion);
            }
            using (var gzip = new GZipStream(stream, System.IO.Compression.CompressionLevel.Fastest, true))
            using (var w = new BinaryWriter(gzip))
            {
                w.Write(MapSource);
                w.Write(Seed);
                w.Write(GridWidth);
                w.Write(GridHeight);
                w.Write(Active.x); w.Write(Active.y); w.Write(Active.width); w.Write(Active.height);
                w.Write(MatchSeconds);
                w.Write(Weather);
                w.Write(DissolvedCells);
                w.Write(PackedCells);
                w.Write(RainWaterCells);
                w.Write(ErodedCells);
                w.Write(EvaporatedCells);
                w.Write(AbsorbedCells);
                w.Write(DrownedAnts);
                w.Write(StoredPixels);
                w.Write(TotalDelivered);
                w.Write(SimulationTime);

                w.Write(Ants.Length);
                foreach (AntRecord a in Ants)
                {
                    w.Write(a.X); w.Write(a.Y); w.Write(a.FacingX); w.Write(a.State); w.Write(a.Cooldown);
                    w.Write(a.DigX); w.Write(a.DigY); w.Write(a.DumpTargetX); w.Write(a.Carried); w.Write(a.CarriedShade);
                    w.Write(a.UnderwaterSeconds);
                }
                w.Write(QueenX); w.Write(QueenY); w.Write(QueenFacingX);
                w.Write(Nutrients); w.Write(TotalNutrients); w.Write(HarvestedPixels);

                w.Write(Cells);
                w.Write(Shades);
                w.Write(Marked);
                w.Write(Wetness);
                w.Write(Water);
                w.Write(QueenRoom);
                w.Write(NutrientRoom);
                w.Write(PlantSunk);
            }
        }

        /// <exception cref="InvalidDataException">Arquivo que não é um save, de outra versão ou corrompido.</exception>
        public static GameSnapshot Read(Stream stream)
        {
            int version;
            try
            {
                using (var header = new BinaryReader(stream, Encoding.UTF8, true))
                {
                    if (header.ReadInt32() != Magic)
                        throw new InvalidDataException("O arquivo não é um save do Incrementant.");
                    version = header.ReadInt32();
                    if (version < OldestReadableVersion || version > CurrentVersion)
                        throw new InvalidDataException(
                            $"Versão de save {version} não suportada (esperada de {OldestReadableVersion} a {CurrentVersion}).");
                }

                using (var gzip = new GZipStream(stream, CompressionMode.Decompress, true))
                using (var r = new BinaryReader(gzip))
                {
                    var s = new GameSnapshot
                    {
                        MapSource = r.ReadInt32(),
                        Seed = r.ReadInt32(),
                        GridWidth = r.ReadInt32(),
                        GridHeight = r.ReadInt32(),
                    };
                    s.Active = new RectInt(r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
                    if (s.GridWidth < 3 || s.GridHeight < 3 || s.Active.width < 0 || s.Active.height < 0 ||
                        s.Active.xMin < 0 || s.Active.yMin < 0 || s.Active.xMax > s.GridWidth || s.Active.yMax > s.GridHeight)
                        throw new InvalidDataException("O save tem dimensões inválidas.");

                    s.MatchSeconds = r.ReadDouble();
                    s.Weather = r.ReadInt32();
                    s.DissolvedCells = r.ReadInt32();
                    s.PackedCells = r.ReadInt32();
                    s.RainWaterCells = r.ReadInt32();
                    s.ErodedCells = r.ReadInt32();
                    s.EvaporatedCells = r.ReadInt32();
                    s.AbsorbedCells = r.ReadInt32();
                    s.DrownedAnts = r.ReadInt32();
                    s.StoredPixels = r.ReadInt32();
                    s.TotalDelivered = r.ReadInt32();
                    s.SimulationTime = r.ReadSingle();

                    int antCount = r.ReadInt32();
                    if (antCount < 0 || antCount > 100000)
                        throw new InvalidDataException("O save tem uma contagem de formigas inválida.");
                    s.Ants = new AntRecord[antCount];
                    for (int i = 0; i < antCount; i++)
                    {
                        s.Ants[i] = new AntRecord
                        {
                            X = r.ReadInt32(), Y = r.ReadInt32(), FacingX = r.ReadInt32(), State = r.ReadByte(), Cooldown = r.ReadSingle(),
                            DigX = r.ReadInt32(), DigY = r.ReadInt32(), DumpTargetX = r.ReadInt32(),
                            Carried = r.ReadByte(), CarriedShade = r.ReadByte(), UnderwaterSeconds = r.ReadSingle(),
                        };
                    }
                    if (version >= 3)
                    {
                        s.HasQueen = true;
                        s.QueenX = r.ReadInt32();
                        s.QueenY = r.ReadInt32();
                        s.QueenFacingX = r.ReadInt32();
                    }
                    if (version >= 4)
                    {
                        s.Nutrients = r.ReadInt32();
                        s.TotalNutrients = r.ReadInt32();
                        s.HarvestedPixels = r.ReadInt32();
                    }

                    int count = s.Active.width * s.Active.height;
                    s.Cells = ReadBlock(r, count);
                    s.Shades = ReadBlock(r, count);
                    s.Marked = ReadBlock(r, count);
                    s.Wetness = ReadBlock(r, count);
                    s.Water = ReadBlock(r, count);
                    s.QueenRoom = version >= 3 ? ReadBlock(r, count) : Array.Empty<byte>();
                    s.NutrientRoom = version >= 4 ? ReadBlock(r, count) : Array.Empty<byte>();
                    s.PlantSunk = version >= 5 ? ReadBlock(r, s.Active.width) : Array.Empty<byte>();
                    return s;
                }
            }
            catch (EndOfStreamException e)
            {
                throw new InvalidDataException("O save está incompleto.", e);
            }
        }

        static byte[] ReadBlock(BinaryReader r, int count)
        {
            // Read pode devolver menos que o pedido num GZipStream: lê até completar.
            var data = new byte[count];
            int read = 0;
            while (read < count)
            {
                int n = r.Read(data, read, count - read);
                if (n <= 0)
                    throw new EndOfStreamException();
                read += n;
            }
            return data;
        }
    }
}
