using System;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    [Serializable]
    public sealed class ColonySettings
    {
        [SerializeField, Min(0)] int _initialAnts = 6;
        [SerializeField, Min(1)] int _maxAnts = 400;
        [Tooltip("Células por segundo.")]
        [SerializeField, Min(0.1f)] float _antSpeed = 20f;
        [Tooltip("Multiplica o tempo de escavação de cada material.")]
        [SerializeField, Min(0.01f)] float _digTimeMultiplier = 1f;
        [Tooltip("Pausa ao soltar o pixel no despejo.")]
        [SerializeField, Min(0f)] float _dropSeconds = 0.1f;
        [Tooltip("Segundos (de jogo) que o pixel de planta guardado na sala de nutrientes leva para virar nutriente que pode ser levado à rainha. A formiga não espera: larga e volta ao trabalho.")]
        [SerializeField, Min(0f)] float _nutrientCureSeconds = 10f;
        [Tooltip("Intervalo mínimo entre recálculos dos campos de navegação.")]
        [SerializeField, Min(0f)] float _fieldRebuildInterval = 0.1f;
        [Tooltip("Velocidade da rainha (células por segundo). Ela só anda dentro da maior sala da rainha pintada.")]
        [SerializeField, Min(0.1f)] float _queenSpeed = 4f;
        [Tooltip("Chance, a cada passo da rainha dentro da sala, de parar um pouco antes de seguir.")]
        [SerializeField, Range(0f, 1f)] float _queenRestChance = 0.15f;
        [Tooltip("Pausa máxima (s) quando a rainha para dentro da sala.")]
        [SerializeField, Min(0f)] float _queenRestSeconds = 1.5f;
        [Tooltip("Aberturas na superfície que ganham faixas de despejo (as mais perto da entrada). Cada uma custa dois campos de navegação.")]
        [SerializeField, Min(1)] int _maxDumpOpenings = 4;
        [Tooltip("Segundos seguidos debaixo d'água até a formiga se afogar.")]
        [SerializeField, Min(0.1f)] float _drownSeconds = 5f;
        [Tooltip("Quantas das formigas iniciais nascem jardineiras (colhem as plantas sozinhas). As demais são comuns.")]
        [SerializeField, Min(0)] int _initialGardeners = 1;
        [Header("Criação (seções 11, 12 e 19 do design)")]
        [Tooltip("Segundos entre uma postura e a seguinte.")]
        [SerializeField, Min(0.1f)] float _layIntervalSeconds = 18f;
        [SerializeField, Min(0.1f)] float _eggSeconds = 8f;
        [SerializeField, Min(0.1f)] float _larvaSeconds = 14f;
        [SerializeField, Min(0.1f)] float _pupaSeconds = 12f;
        [Tooltip("Nutrientes que a rainha preserva: só põe o ovo se sobrar isto depois de pagar a cria.")]
        [SerializeField, Min(0)] int _minNutrientReserve = 30;
        [Tooltip("Células escavadas e acessíveis por vaga de adulta. O design usa 256 (um B), mas as formigas do protótipo " +
                 "ocupam 3x2 células; este valor é a escala do protótipo e precisa de ajuste de balanceamento.")]
        [SerializeField, Min(1)] int _cellsPerAdultSlot = 24;
        [Tooltip("Células de berçário (vazias e alcançáveis) por vaga de cria. O design dá 2 vagas por B de 256 células; " +
                 "mesma escala do protótipo de Cells Per Adult Slot.")]
        [SerializeField, Min(1)] int _cellsPerNurserySlot = 16;

        public int InitialAnts => _initialAnts;
        public int MaxAnts => _maxAnts;
        public float AntSpeed => _antSpeed;
        public float DigTimeMultiplier => _digTimeMultiplier;
        public float DropSeconds => _dropSeconds;
        public float NutrientCureSeconds => _nutrientCureSeconds;
        public float FieldRebuildInterval => _fieldRebuildInterval;
        public float QueenSpeed => _queenSpeed;
        public float QueenRestChance => _queenRestChance;
        public float QueenRestSeconds => _queenRestSeconds;
        public int MaxDumpOpenings => _maxDumpOpenings;
        public float DrownSeconds => _drownSeconds;
        public int InitialGardeners => _initialGardeners;
        public float LayIntervalSeconds => _layIntervalSeconds;
        public float EggSeconds => _eggSeconds;
        public float LarvaSeconds => _larvaSeconds;
        public float PupaSeconds => _pupaSeconds;
        public int MinNutrientReserve => _minNutrientReserve;
        public int CellsPerAdultSlot => _cellsPerAdultSlot;
        public int CellsPerNurserySlot => _cellsPerNurserySlot;
    }
}
