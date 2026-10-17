using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fabula.Incrementant.World
{
    /// <summary>Tempo atual do formigário. Serializado como int; não reordene.</summary>
    public enum Weather
    {
        Clear = 0,
        /// <summary>Gotas molham a terra despejada (<see cref="CellMaterial.Spoil"/>) até dissolvê-la.</summary>
        LightRain = 1,
        /// <summary>Gotas compactam os materiais granulares, que deixam de cair (<see cref="CellMaterials.ToPacked"/>).</summary>
        HeavyRain = 2,
    }

    [Serializable]
    public sealed class RainSettings
    {
        [Header("Chuva leve")]
        [Tooltip("Gotas por segundo a cada 100 colunas do mapa.")]
        [Min(0f)] public float LightDropsPer100Columns = 40f;
        [Tooltip("Células que a gota cai por passo.")]
        [Min(1)] public int LightFallSpeed = 3;
        [Tooltip("Gotas que uma célula de terra despejada aguenta antes de se dissolver.")]
        [Range(1, 50)] public int LightHitsToDissolve = 4;

        [Header("Chuva forte")]
        [Min(0f)] public float HeavyDropsPer100Columns = 150f;
        [Min(1)] public int HeavyFallSpeed = 5;
        [Tooltip("Quantas células granulares seguidas (de cima para baixo) cada gota compacta.")]
        [Range(1, 10)] public int HeavySoakDepth = 2;
        [Tooltip("Chance de a gota que não compactou nada virar um pixel de água. Só a chuva forte cria água.")]
        [Range(0f, 1f)] public float HeavyWaterChance = 0.1f;

        [Header("Secagem")]
        [Tooltip("Fora da chuva leve, a cada quantos segundos simulados toda célula molhada perde 1 de umidade.")]
        [Min(0.1f)] public float DrySeconds = 1.5f;
    }

    /// <summary>Gota de chuva em queda, em células da grade.</summary>
    public struct RainDrop
    {
        public int X;
        public int Y;
        /// <summary>Células por passo: a gota é desenhada como um risco deste comprimento.</summary>
        public int Speed;
        /// <summary>Gota da chuva forte (compacta) ou da leve (molha).</summary>
        public bool Heavy;
    }

    /// <summary>
    /// Chuva em passo fixo, determinística pela seed. As gotas nascem no topo da área ativa, caem reto pelas células
    /// vazias e agem na primeira célula sólida (ou com água) que encontram:
    /// <list type="bullet">
    /// <item>chuva leve: soma umidade em <see cref="CellMaterial.Spoil"/>; ao chegar em
    /// <see cref="RainSettings.LightHitsToDissolve"/>, a célula some (o que estava em cima desaba pela física granular);</item>
    /// <item>chuva forte: troca o material granular atingido (e os de baixo, até <see cref="RainSettings.HeavySoakDepth"/>)
    /// pela versão compactada, que não cai mais.</item>
    /// <item>só na chuva forte: a gota que não compactou nada (caiu em outro material ou na água) vira água na célula
    /// em que parou, com chance <see cref="RainSettings.HeavyWaterChance"/>.</item>
    /// </list>
    /// Gotas já no ar terminam a queda quando o tempo muda, com o efeito do tipo em que nasceram.
    /// </summary>
    public sealed class RainSimulation
    {
        readonly RainSettings _settings;
        readonly System.Random _rng;
        readonly List<RainDrop> _drops = new List<RainDrop>();
        float _spawnAccumulator;
        float _dryTimer;

        public RainSimulation(RainSettings settings, int seed)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _rng = new System.Random(seed ^ 0x5A17);
        }

        public Weather Weather { get; set; }

        public IReadOnlyList<RainDrop> Drops => _drops;

        /// <summary>Células de terra dissolvidas pela chuva leve desde o começo do mapa.</summary>
        public int DissolvedCells { get; private set; }

        /// <summary>Células granulares compactadas pela chuva forte desde o começo do mapa.</summary>
        public int PackedCells { get; private set; }

        /// <summary>Pixels de água que a chuva criou desde o começo do mapa.</summary>
        public int WaterCells { get; private set; }

        /// <summary>Repõe os contadores de um save (as gotas no ar não são salvas).</summary>
        public void RestoreCounters(int dissolvedCells, int packedCells, int waterCells = 0)
        {
            DissolvedCells = dissolvedCells;
            PackedCells = packedCells;
            WaterCells = waterCells;
        }

        public void Step(PixelGrid grid, float deltaTime)
        {
            Spawn(grid, deltaTime);
            Fall(grid);
            Dry(grid, deltaTime);
        }

        void Spawn(PixelGrid grid, float deltaTime)
        {
            if (Weather == Weather.Clear)
            {
                _spawnAccumulator = 0f;
                return;
            }

            bool heavy = Weather == Weather.HeavyRain;
            RectInt area = grid.Active;
            float perColumn = (heavy ? _settings.HeavyDropsPer100Columns : _settings.LightDropsPer100Columns) * 0.01f;
            _spawnAccumulator += perColumn * area.width * deltaTime;
            int speed = Mathf.Max(1, heavy ? _settings.HeavyFallSpeed : _settings.LightFallSpeed);
            // Nascem na última linha da área ativa; se ela estiver ocupada, a gota morre no primeiro passo.
            int top = area.yMax - 1;
            while (_spawnAccumulator >= 1f)
            {
                _spawnAccumulator -= 1f;
                _drops.Add(new RainDrop { X = _rng.Next(area.xMin, area.xMax), Y = top, Speed = speed, Heavy = heavy });
            }
        }

        void Fall(PixelGrid grid)
        {
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                RainDrop drop = _drops[i];
                bool alive = true;
                for (int s = 0; s < drop.Speed; s++)
                {
                    int y = drop.Y - 1;
                    // Fora da área ativa conta como rocha-mãe: a gota para na borda. Também para na água.
                    if (!grid.IsOpen(drop.X, y))
                    {
                        if (!Hit(grid, drop.X, y, drop.Heavy))
                            MakeWater(grid, drop.X, drop.Y, drop.Heavy);
                        alive = false;
                        break;
                    }
                    drop.Y = y;
                }
                if (alive)
                {
                    _drops[i] = drop;
                }
                else
                {
                    // Ordem não importa: troca com o último para remover em O(1).
                    _drops[i] = _drops[_drops.Count - 1];
                    _drops.RemoveAt(_drops.Count - 1);
                }
            }
        }

        /// <summary>Efeito da gota na célula atingida. Devolve false se ela não teve efeito (pode virar água).</summary>
        bool Hit(PixelGrid grid, int x, int y, bool heavy)
        {
            if (grid.HasWater(x, y))
                return false;
            if (heavy)
            {
                bool packed = false;
                for (int d = 0; d < _settings.HeavySoakDepth; d++, y--)
                {
                    CellMaterial material = grid.Get(x, y);
                    if (!CellMaterials.IsGranular(material) || CellMaterials.IsPlantDebris(material))
                        break;
                    grid.Set(x, y, CellMaterials.ToPacked(material), grid.GetShade(x, y));
                    PackedCells++;
                    packed = true;
                }
                return packed;
            }

            if (grid.Get(x, y) != CellMaterial.Spoil)
                return false;
            int wetness = grid.GetWetness(x, y) + 1;
            if (wetness >= _settings.LightHitsToDissolve)
            {
                grid.Set(x, y, CellMaterial.Empty, 0);
                DissolvedCells++;
            }
            else
            {
                grid.SetWetness(x, y, (byte)wetness);
            }
            return true;
        }

        void MakeWater(PixelGrid grid, int x, int y, bool heavy)
        {
            // Só a chuva forte cria água; a gota leve que não molhou terra despejada só some.
            float chance = _settings.HeavyWaterChance;
            if (heavy && chance > 0f && _rng.NextDouble() < chance && grid.SetWater(x, y, true))
                WaterCells++;
        }

        void Dry(PixelGrid grid, float deltaTime)
        {
            if (Weather == Weather.LightRain || grid.WetCount == 0)
            {
                _dryTimer = 0f;
                return;
            }
            _dryTimer += deltaTime;
            if (_dryTimer < _settings.DrySeconds)
                return;
            _dryTimer -= _settings.DrySeconds;
            grid.DryAll();
        }
    }
}
