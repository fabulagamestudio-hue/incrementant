using System;
using UnityEngine;

namespace Fabula.Incrementant.Colony
{
    /// <summary>
    /// Números dos ataques de cupins (pedido do desenvolvedor em 2026-10-18) e do combate. Vida e dano das formigas vêm da
    /// tabela 15 do design (<see cref="AntRoles.BaseHealth"/>); os dos cupins, da seção 32 (invasora: vida 10, dano 2 a cada
    /// 1 s, 18 células/s no design, aqui na escala da velocidade das formigas do protótipo).
    /// </summary>
    [Serializable]
    public sealed class RaidSettings
    {
        [Header("Quando")]
        [Tooltip("Formigas adultas que a colônia precisa ter alcançado uma vez para os ataques começarem.")]
        [SerializeField, Min(1)] int _minAnts = 15;
        [Tooltip("Intervalo mínimo (s de jogo) entre um ataque e o próximo, contado do fim do anterior.")]
        [SerializeField, Min(1f)] float _minIntervalSeconds = 60f;
        [Tooltip("Intervalo máximo (s de jogo) entre um ataque e o próximo.")]
        [SerializeField, Min(1f)] float _maxIntervalSeconds = 300f;
        [Tooltip("Segundos de aviso (alerta no topo da tela) antes de os cupins aparecerem no céu.")]
        [SerializeField, Min(0f)] float _warningSeconds = 8f;
        [Tooltip("Mínimo de cupins por formiga adulta (0,4 = 40%). Cada ataque sorteia entre este mínimo e o teto do momento; arredonda para cima, pelo menos 1.")]
        [SerializeField, Range(0.05f, 2f)] float _termitesPerAnt = 0.4f;
        [Tooltip("Teto final de cupins por formiga adulta (2 = 200%).")]
        [SerializeField, Range(0.05f, 5f)] float _maxTermitesPerAnt = 2f;
        [Tooltip("Quanto o teto sobe a cada ataque que termina (0,2 = +20 pontos percentuais): o primeiro ataque vem com o mínimo e o teto chega ao máximo depois de (máximo − mínimo) / este valor ataques.")]
        [SerializeField, Min(0f)] float _termitesPerAntGrowth = 0.2f;

        [Header("Chegada")]
        [Tooltip("Altura (células) acima do chão de onde cada cupim começa a descer voando.")]
        [SerializeField, Min(2)] int _flightHeight = 40;
        [Tooltip("Segundos de voo até pousar.")]
        [SerializeField, Min(0.1f)] float _flightSeconds = 3f;
        [Tooltip("Segundos da animação de pouso (as asas caem) antes de andar.")]
        [SerializeField, Min(0f)] float _landingSeconds = 1.2f;

        [Header("Cupim")]
        [SerializeField, Min(1f)] float _termiteHealth = 10f;
        [Tooltip("Dano por mordida (uma a cada Attack Seconds).")]
        [SerializeField, Min(0f)] float _termiteDamage = 2f;
        [Tooltip("Células por segundo (o design pede menos que a operária).")]
        [SerializeField, Min(0.1f)] float _termiteSpeed = 15f;
        [Tooltip("Segundos para destruir uma cria, um canteiro ou uma célula de parede.")]
        [SerializeField, Min(0.05f)] float _destroySeconds = 0.8f;
        [Tooltip("Camadas de parede destruídas em volta de cada setor (berçário, sala de nutrientes, área de resíduos); a sala da rainha fica de fora.")]
        [SerializeField, Range(1, 4)] int _wallLayers = 2;
        [Tooltip("Sem destruir nada por este tempo (s), os cupins passam para o próximo objetivo (alvo inalcançável, por exemplo).")]
        [SerializeField, Min(1f)] float _stallSeconds = 30f;
        [Tooltip("Duração máxima do ataque (s): passado isso, os cupins vivos vão embora.")]
        [SerializeField, Min(10f)] float _maxRaidSeconds = 300f;

        [Header("Combate")]
        [Tooltip("Segundos entre duas mordidas (formigas e cupins). Dano por mordida = dano/s × este tempo.")]
        [SerializeField, Min(0.1f)] float _attackSeconds = 1f;
        [Tooltip("Distância (células, em xadrez) em que formiga e cupim se mordem: o design pede o contato visual mais 2 células.")]
        [SerializeField, Range(1, 3)] int _attackReach = 2;
        [Tooltip("Segundos sem levar mordida até a formiga começar a recuperar a vida (1 ponto/s).")]
        [SerializeField, Min(0f)] float _healDelaySeconds = 10f;

        [Header("Corpos e resíduos")]
        [Tooltip("Folhas (pedaços de planta para os canteiros) que um cupim morto rende.")]
        [SerializeField, Min(0)] int _corpseLeaves = 2;
        [Tooltip("Restos (vão para a área de resíduos) que um cupim morto rende.")]
        [SerializeField, Min(0)] int _corpseRemains = 1;
        [Tooltip("Segundos para tirar uma parte do corpo.")]
        [SerializeField, Min(0.05f)] float _dismemberSeconds = 1f;
        [Tooltip("Segundos para cada célula de resíduo se decompor na área de resíduos (design R0: 120 s por UR).")]
        [SerializeField, Min(1f)] float _wasteDecaySeconds = 120f;

        public int MinAnts => _minAnts;
        public float MinIntervalSeconds => _minIntervalSeconds;
        public float MaxIntervalSeconds => Mathf.Max(_minIntervalSeconds, _maxIntervalSeconds);
        public float WarningSeconds => _warningSeconds;
        public float TermitesPerAnt => _termitesPerAnt;
        public float MaxTermitesPerAnt => Mathf.Max(_termitesPerAnt, _maxTermitesPerAnt);
        public float TermitesPerAntGrowth => _termitesPerAntGrowth;

        /// <summary>Teto de cupins por formiga depois de <paramref name="raidsSurvived"/> ataques: começa no mínimo e sobe até o máximo.</summary>
        public float TermiteCap(int raidsSurvived) =>
            Mathf.Min(MaxTermitesPerAnt, _termitesPerAnt + _termitesPerAntGrowth * Mathf.Max(0, raidsSurvived));
        public int FlightHeight => _flightHeight;
        public float FlightSeconds => _flightSeconds;
        public float LandingSeconds => _landingSeconds;
        public float TermiteHealth => _termiteHealth;
        public float TermiteDamage => _termiteDamage;
        public float TermiteSpeed => _termiteSpeed;
        public float DestroySeconds => _destroySeconds;
        public int WallLayers => _wallLayers;
        public float StallSeconds => _stallSeconds;
        public float MaxRaidSeconds => _maxRaidSeconds;
        public float AttackSeconds => _attackSeconds;
        public int AttackReach => _attackReach;
        public float HealDelaySeconds => _healDelaySeconds;
        public int CorpseLeaves => _corpseLeaves;
        public int CorpseRemains => _corpseRemains;
        public float DismemberSeconds => _dismemberSeconds;
        public float WasteDecaySeconds => _wasteDecaySeconds;
    }
}
