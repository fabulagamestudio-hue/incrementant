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
        [SerializeField, Min(0)] int _antBaseCost = 25;
        [SerializeField, Min(1f)] float _antCostGrowth = 1.12f;

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
        public int AntBaseCost => _antBaseCost;
        public float AntCostGrowth => _antCostGrowth;
    }
}
