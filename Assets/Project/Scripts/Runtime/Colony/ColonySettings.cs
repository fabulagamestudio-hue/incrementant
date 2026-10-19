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
        [Tooltip("Segundos (de jogo) que um canteiro da sala de nutrientes (terra fofa + planta + água) leva para brotar e pôr um nutriente pronto em cada lado livre. Só corre com formiga cuidando da sala; a formiga que rega não espera.")]
        [SerializeField, Min(0f)] float _nutrientCureSeconds = 30f;
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
        [Tooltip("Segundos que a escavadora com E2 leva para quebrar uma célula de rocha (design: 4 células/s; o protótipo escava cerca de 4x mais devagar que o design, então 1 s).")]
        [SerializeField, Min(0.05f)] float _rockBreakSeconds = 1f;
        [Tooltip("Segundos que a comum leva para pegar ou largar uma cria no berçário; a cuidadora é 4/3 mais rápida.")]
        [SerializeField, Min(0.05f)] float _carePauseSeconds = 1f;
        [Tooltip("Blocos (crias, canteiros que brotam) por formiga que a região chama: quanto mais blocos, mais formigas vão trabalhar nela.")]
        [SerializeField, Min(1)] int _blocksPerTender = 2;
        [Tooltip("Parte das comuns que pode estar cuidando de regiões ao mesmo tempo (o resto continua cavando, construindo e cortando).")]
        [SerializeField, Range(0f, 1f)] float _commonTenderShare = 0.5f;
        [Header("Vida das formigas")]
        [Tooltip("Tempo de vida médio de uma formiga adulta, em segundos de jogo. 0 = as formigas não envelhecem.")]
        [SerializeField, Min(0f)] float _antLifespanSeconds = 600f;
        [Tooltip("Variação sorteada por formiga em torno do tempo de vida (0,2 = de 80% a 120%), para a colônia não morrer toda de uma vez.")]
        [SerializeField, Range(0f, 0.9f)] float _antLifespanVariation = 0.2f;
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

        [Header("Sala Gotas de Orvalho")]
        [Tooltip("Segundos (de jogo) entre uma gota e a seguinte, com a sala pronta (terra fofa e folhas postas).")]
        [SerializeField, Min(0.1f)] float _dewDropSeconds = 8f;
        [Tooltip("Parte de cima da sala (por coluna) que as formigas cobrem de terra fofa.")]
        [SerializeField, Range(0.05f, 0.9f)] float _dewSoilShare = 0.3f;
        [Tooltip("Altura mínima (células pintadas na coluna) para a coluna receber terra fofa e folha: abaixo disso a coluna fecharia a passagem.")]
        [SerializeField, Min(2)] int _dewMinColumnHeight = 4;
        [Tooltip("Pontos de terra fofa por formiga trabalhando na sala ao mesmo tempo (pelo menos uma).")]
        [SerializeField, Min(1)] int _dewSpotsPerWorker = 8;
        [Tooltip("Água máxima (células) acumulada dentro da sala: cheia, as gotas esperam as formigas levarem a água.")]
        [SerializeField, Min(1)] int _dewMaxRoomWater = 24;
        [Tooltip("Quanto o cuidado acelera a queda das gotas com todos os pontos de queda cobertos (1 = +100% ao ritmo); cobrindo só parte, soma a parte proporcional.")]
        [SerializeField, Min(0f)] float _dewTendBoost = 1f;
        [Tooltip("Pontos de queda vizinhos que uma jardineira cuida (um bloco da sala de orvalho); a sala chama uma jardineira por bloco.")]
        [SerializeField, Min(1)] int _dewPointsPerTender = 4;

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
        public float AntLifespanSeconds => _antLifespanSeconds;
        public float AntLifespanVariation => _antLifespanVariation;
        public float RockBreakSeconds => _rockBreakSeconds;
        public float CarePauseSeconds => _carePauseSeconds;
        public int BlocksPerTender => _blocksPerTender;
        public float CommonTenderShare => _commonTenderShare;
        public float LayIntervalSeconds => _layIntervalSeconds;
        public float EggSeconds => _eggSeconds;
        public float LarvaSeconds => _larvaSeconds;
        public float PupaSeconds => _pupaSeconds;
        public int MinNutrientReserve => _minNutrientReserve;
        public int CellsPerAdultSlot => _cellsPerAdultSlot;
        public int CellsPerNurserySlot => _cellsPerNurserySlot;
        public float DewDropSeconds => _dewDropSeconds;
        public float DewSoilShare => _dewSoilShare;
        public int DewMinColumnHeight => _dewMinColumnHeight;
        public int DewSpotsPerWorker => _dewSpotsPerWorker;
        public int DewMaxRoomWater => _dewMaxRoomWater;
        public float DewTendBoost => _dewTendBoost;
        public int DewPointsPerTender => _dewPointsPerTender;
    }
}
