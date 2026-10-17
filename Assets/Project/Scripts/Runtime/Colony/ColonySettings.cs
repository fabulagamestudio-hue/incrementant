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
        [Tooltip("Intervalo mínimo entre recálculos dos campos de navegação.")]
        [SerializeField, Min(0f)] float _fieldRebuildInterval = 0.1f;
        [Tooltip("Formigas ociosas vagam até esta distância do ninho.")]
        [SerializeField, Min(0)] int _idleHomeRadius = 8;
        [SerializeField, Min(0)] int _antBaseCost = 25;
        [SerializeField, Min(1f)] float _antCostGrowth = 1.12f;

        public int InitialAnts => _initialAnts;
        public int MaxAnts => _maxAnts;
        public float AntSpeed => _antSpeed;
        public float DigTimeMultiplier => _digTimeMultiplier;
        public float DropSeconds => _dropSeconds;
        public float FieldRebuildInterval => _fieldRebuildInterval;
        public int IdleHomeRadius => _idleHomeRadius;
        public int AntBaseCost => _antBaseCost;
        public float AntCostGrowth => _antCostGrowth;
    }
}
