using System;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    [Serializable]
    public sealed class ColonySettings
    {
        [SerializeField, Min(0)] int _initialAnts = 6;
        [SerializeField, Min(1)] int _maxAnts = 4000;
        [Tooltip("Células por segundo.")]
        [SerializeField, Min(0.1f)] float _antSpeed = 20f;
        [Tooltip("Multiplica a velocidade de quem está ociosa, só vagando sem trabalho (0,5 = metade).")]
        [SerializeField, Range(0.05f, 1f)] float _idleSpeedRate = 0.5f;
        [Tooltip("Multiplica o tempo de escavação de cada material.")]
        [SerializeField, Min(0.01f)] float _digTimeMultiplier = 1f;
        [Tooltip("Multiplica a velocidade de andar da operária comum (as outras classes não pagam isso): é o que justifica as classes operárias. 1 = sem penalidade.")]
        [SerializeField, Range(0.1f, 1f)] float _workerSpeedRate = 1f;
        [Tooltip("Multiplica o tempo das tarefas da operária comum (cavar, construir, cortar, cuidar). 1 = sem penalidade; a cena usa mais de 1 para a comum ser a mais lenta.")]
        [SerializeField, Min(1f)] float _workerTaskTimeFactor = 1f;
        [Tooltip("Mutirão: formigas ociosas ao lado de um bloco que outra está soltando somam o trabalho delas a ele (acelera o bloco); só a primeira pega o pixel. Desligado, cada bloco é de uma formiga só.")]
        [SerializeField] bool _assistDig = false;
        [Tooltip("Segundos que uma folha leva para virar um nutriente na sala de folhas (cada folha tem o próprio tempo, em paralelo).")]
        [SerializeField, Min(0.1f)] float _leafNutrientSeconds = 30f;
        [Tooltip("Pausa ao soltar o pixel no despejo.")]
        [SerializeField, Min(0f)] float _dropSeconds = 0.1f;
        [Tooltip("Segundos (de jogo) que um canteiro da sala de nutrientes (terra fofa + planta + água) leva para brotar e pôr um nutriente pronto em cada lado livre. Só corre com formiga cuidando da sala; a formiga que rega não espera.")]
        [SerializeField, Min(0f)] float _nutrientCureSeconds = 30f;
        [Tooltip("Intervalo mínimo entre recálculos dos campos de navegação.")]
        [SerializeField, Min(0f)] float _fieldRebuildInterval = 0.1f;
        [Tooltip("Segundos (de jogo) mínimos entre varreduras da água para o campo de buscar água. Na chuva a água escorre a cada passo; refazer o campo por isso em toda rodada pesava a rodada inteira.")]
        [SerializeField, Min(0f)] float _waterFieldRefreshSeconds = 1f;
        [Tooltip("Chance, a cada passo da rainha dentro da sala, de parar um pouco antes de seguir.")]
        [SerializeField, Range(0f, 1f)] float _queenRestChance = 0.15f;
        [Tooltip("Pausa máxima (s) quando a rainha para dentro da sala.")]
        [SerializeField, Min(0f)] float _queenRestSeconds = 1.5f;
        [Tooltip("Aberturas na superfície que ganham faixas de despejo (as mais perto da entrada). Cada uma custa dois campos de navegação.")]
        [SerializeField, Min(1)] int _maxDumpOpenings = 4;
        [Tooltip("Segundos que a escavadora com E2 leva para quebrar uma célula de rocha (design: 4 células/s; o protótipo escava cerca de 4x mais devagar que o design, então 1 s).")]
        [SerializeField, Min(0.05f)] float _rockBreakSeconds = 1f;
        [Tooltip("Segundos que a comum leva para pegar ou largar uma cria no berçário; a cuidadora é 4/3 mais rápida.")]
        [SerializeField, Min(0.05f)] float _carePauseSeconds = 1f;
        [Tooltip("Blocos (crias, canteiros que brotam) por formiga que a região chama: quanto mais blocos, mais formigas vão trabalhar nela.")]
        [SerializeField, Min(1)] int _blocksPerTender = 2;
        [Tooltip("Parte das comuns que pode estar cuidando de regiões ao mesmo tempo (o resto continua cavando, construindo e cortando).")]
        [SerializeField, Range(0f, 1f)] float _commonTenderShare = 0.5f;
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
        [SerializeField, Min(1)] int _cellsPerAdultSlot = 72;
        [Tooltip("Células de um B de canteiro da sala de nutrientes (escala do protótipo). Separado de Cells Per Adult Slot " +
                 "para que o ritmo de escavação das vagas não mude a cobertura dos canteiros.")]
        [SerializeField, Min(1)] int _cellsPerGardenBlock = 24;
        [Tooltip("Células de berçário (vazias e alcançáveis) por vaga de cria. O design dá 2 vagas por B de 256 células; " +
                 "mesma escala do protótipo de Cells Per Adult Slot.")]
        [SerializeField, Min(1)] int _cellsPerNurserySlot = 16;

        [Header("Sala Gotas de Orvalho")]
        [Tooltip("Segundos (de jogo) entre uma gota e a seguinte, com a sala pronta (terra fofa e folhas postas).")]
        [SerializeField, Min(0.1f)] float _dewDropSeconds = 24f;
        [Tooltip("Parte de cima da sala (por coluna) que as formigas cobrem de terra fofa.")]
        [SerializeField, Range(0.05f, 0.9f)] float _dewSoilShare = 0.3f;
        [Tooltip("Altura mínima (células pintadas na coluna) para a coluna receber terra fofa e folha: abaixo disso a coluna fecharia a passagem.")]
        [SerializeField, Min(2)] int _dewMinColumnHeight = 4;
        [Tooltip("Água máxima (células) acumulada dentro da sala por célula de altura da sala (a coluna mais alta), por ponto de queda (coluna): cheia, as gotas esperam as formigas levarem a água.")]
        [SerializeField, Min(1)] int _dewWaterPerHeight = 3;
        [Tooltip("Teto vertical da água na sala (parte da altura de cada coluna, de baixo para cima), com ou sem upgrades: acima dele a sala não solta gota, para a água não escorrer para os túneis (KI-050).")]
        [SerializeField, Range(0.1f, 1f)] float _dewMaxWaterShare = 0.4f;
        [Tooltip("Quanto o cuidado acelera a queda das gotas com todos os pontos de queda cobertos (1 = +100% ao ritmo); cobrindo só parte, soma a parte proporcional.")]
        [SerializeField, Min(0f)] float _dewTendBoost = 1f;
        [Tooltip("Pontos de queda vizinhos que uma jardineira cuida (um bloco da sala de orvalho); a sala chama uma jardineira por bloco.")]
        [SerializeField, Min(1)] int _dewPointsPerTender = 4;

        [Header("Fazenda de pulgões")]
        [Tooltip("Pulgões que a fazenda cria sem upgrades (A+1 soma um por nível). Cada um ainda precisa de uma baia livre no chão da fazenda.")]
        [SerializeField, Min(0)] int _aphidBaseCap = 2;
        [Tooltip("Segundos (de jogo) entre um pulgão novo e o seguinte, enquanto houver vaga no limite e baia livre (A1 corta pela metade).")]
        [SerializeField, Min(1f)] float _aphidBreedSeconds = 60f;
        [Tooltip("Segundos (de jogo) entre uma água e a seguinte do pulgão sem fazendeira ordenhando (não come nada para isso).")]
        [SerializeField, Min(1f)] float _aphidDripSeconds = 40f;
        [Tooltip("Segundos de ordenha (fazendeira ao lado de um pulgão que já comeu) para uma colheita.")]
        [SerializeField, Min(1f)] float _aphidMilkSeconds = 20f;
        [Tooltip("Velocidade (células/s) do pulgão passeando pela fazenda e indo até os cochos.")]
        [SerializeField, Min(0.5f)] float _aphidSpeed = 4f;
        [Tooltip("Segundos que o pulgão passa nos cochos comendo a areia e bebendo a água (os dois cochos esvaziam no fim).")]
        [SerializeField, Min(0.1f)] float _aphidEatSeconds = 2f;
        [Tooltip("Pausa máxima (segundos) do pulgão parado entre um passeio e o seguinte (sorteada entre um terço e ela).")]
        [SerializeField, Min(0f)] float _aphidRestSeconds = 4f;
        [Tooltip("Areia, folhas e água que cada ordenha solta na fazenda.")]
        [SerializeField, Min(0)] int _milkSand = 2;
        [SerializeField, Min(0)] int _milkLeaves = 1;
        [SerializeField, Min(0)] int _milkWater = 2;
        [Tooltip("Chance de cada ordenha soltar também um cogumelo luminoso (o material do pincel Cogumelo luminoso).")]
        [SerializeField, Range(0f, 1f)] float _milkMushroomChance = 0.25f;
        [Tooltip("Vida do pulgão preto (o que o cupim mordeu e passou a atacar as formigas).")]
        [SerializeField, Min(1f)] float _blackAphidHealth = 12f;
        [Tooltip("Dano de cada mordida do pulgão preto.")]
        [SerializeField, Min(0f)] float _blackAphidDamage = 1.5f;
        [Tooltip("Velocidade do pulgão preto (células/s): mais lento que o cupim.")]
        [SerializeField, Min(0.5f)] float _blackAphidSpeed = 8f;

        [Tooltip("Escavação: a formiga que chega primeiro a um pixel marcado fica com ele e a que o reservou e ainda está longe volta a procurar outro (troca de função, pedido de 2026-10-21). Desligado, vale quem reservou primeiro.")]
        [SerializeField] bool _stealDigReservations = true;

        [Header("Limites de pintura por ambiente (KI-043)")]
        [Tooltip("Células que o pincel pode pintar de sala da rainha, sem upgrades (Q+4 soma por nível).")]
        [SerializeField, Min(1)] int _queenRoomCells = 200;
        [Tooltip("Células de sala de nutrientes, sem upgrades (C+4 soma por nível).")]
        [SerializeField, Min(1)] int _nutrientRoomCells = 150;
        [Tooltip("Células de berçário, sem upgrades (B+3 soma por nível).")]
        [SerializeField, Min(1)] int _nurseryRoomCells = 200;
        [Tooltip("Células de área de resíduos, sem upgrades (R+1 soma por nível).")]
        [SerializeField, Min(1)] int _wasteRoomCells = 120;
        [Tooltip("Células de sala Gotas de Orvalho, sem upgrades (G+3 soma por nível).")]
        [SerializeField, Min(1)] int _dewRoomCells = 100;
        [Tooltip("Células de cada depósito (alimentos, construção, água, areia e cogumelos têm limites separados), sem upgrades (D+3 soma por nível).")]
        [SerializeField, Min(1)] int _depotCells = 150;
        [Tooltip("Células de fazenda de pulgões, sem upgrades (A+3 soma por nível).")]
        [SerializeField, Min(1)] int _farmRoomCells = 120;
        [Tooltip("Salas não conectadas (sem lado em comum) de um mesmo ambiente, sem upgrades (T+3 soma por nível); todas dividem o limite de células dele.")]
        [SerializeField, Min(1)] int _maxPaintRooms = 2;

        public bool StealDigReservations => _stealDigReservations;
        public int MaxPaintRooms => _maxPaintRooms;

        /// <summary>Células que o pincel pode pintar do ambiente, sem upgrades.</summary>
        public int PaintCells(PaintRoom room)
        {
            switch (room)
            {
                case PaintRoom.Queen: return _queenRoomCells;
                case PaintRoom.Nutrient: return _nutrientRoomCells;
                case PaintRoom.Nursery: return _nurseryRoomCells;
                case PaintRoom.Waste:
                case PaintRoom.WasteLeaves:
                case PaintRoom.WasteDirt:
                case PaintRoom.WasteNutrient: return _wasteRoomCells; // o mesmo valor, mas cada modo gasta o próprio limite
                case PaintRoom.Dew: return _dewRoomCells;
                case PaintRoom.Farm: return _farmRoomCells;
                default: return _depotCells;
            }
        }
        public int InitialAnts => _initialAnts;
        public int MaxAnts => _maxAnts;
        public float AntSpeed => _antSpeed;
        public float IdleSpeedRate => _idleSpeedRate;
        public float DigTimeMultiplier => _digTimeMultiplier;
        public bool AssistDig => _assistDig;
        public float LeafNutrientSeconds => _leafNutrientSeconds;
        public float WorkerSpeedRate => _workerSpeedRate;
        public float WorkerTaskTimeFactor => _workerTaskTimeFactor;
        public float DropSeconds => _dropSeconds;
        public float NutrientCureSeconds => _nutrientCureSeconds;
        public float FieldRebuildInterval => _fieldRebuildInterval;
        public float WaterFieldRefreshSeconds => _waterFieldRefreshSeconds;
        public float QueenRestChance => _queenRestChance;
        public float QueenRestSeconds => _queenRestSeconds;
        public int MaxDumpOpenings => _maxDumpOpenings;
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
        public int CellsPerGardenBlock => _cellsPerGardenBlock;
        public int CellsPerNurserySlot => _cellsPerNurserySlot;
        public float DewDropSeconds => _dewDropSeconds;
        public float DewSoilShare => _dewSoilShare;
        public int DewMinColumnHeight => _dewMinColumnHeight;
        public int DewWaterPerHeight => _dewWaterPerHeight;
        public float DewMaxWaterShare => _dewMaxWaterShare;
        public float DewTendBoost => _dewTendBoost;
        public int DewPointsPerTender => _dewPointsPerTender;
        public int AphidBaseCap => _aphidBaseCap;
        public float AphidBreedSeconds => _aphidBreedSeconds;
        public float AphidDripSeconds => _aphidDripSeconds;
        public float AphidMilkSeconds => _aphidMilkSeconds;
        public float AphidSpeed => _aphidSpeed;
        public float AphidEatSeconds => _aphidEatSeconds;
        public float AphidRestSeconds => _aphidRestSeconds;
        public int MilkSand => _milkSand;
        public int MilkLeaves => _milkLeaves;
        public int MilkWater => _milkWater;
        public float MilkMushroomChance => _milkMushroomChance;
        public float BlackAphidHealth => _blackAphidHealth;
        public float BlackAphidDamage => _blackAphidDamage;
        public float BlackAphidSpeed => _blackAphidSpeed;
    }
}
