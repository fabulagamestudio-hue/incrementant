# 27 — Soldado habilidades e números

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 27. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

| Habilidade e requisito | Custo em N | Base e resultado proposto |
| --- | --- | --- |
| S0  Soldado<br>Exige Árvore da rainha | 220 | Defesa comum: dano 1/s; vida 6<br>Após compra: Dano 3/s; vida 18 |
| S1  Mandíbulas fortes<br>Exige S0 | 220 | Dano 3/s<br>Após compra: Dano 4,5/s (+50%) |
| S2  Carapaça resistente<br>Exige S0 | 220 | Vida 18<br>Após compra: Vida 27 (+50%) |
| S3  Contenção<br>Exige S1 e S2 | 500 | Combate sem empurrão<br>Após compra: Empurra invasora 4 células a cada 3 s; sem dano extra |

S0 Soldado. Patrulha e interceptação básica. A defesa básica funciona no nascimento da classe e inclui patrulhar e aproximar-se da ameaça.

S1 Mandíbulas fortes. Cabeça e mandíbulas maiores. O dano maior acompanha uma alteração de cabeça e mandíbulas, sem prometer que a animação mata sempre em um golpe.

S2 Carapaça resistente. Corpo mais robusto. Mais vida acompanha corpo visualmente robusto. Vida máxima proposta sobe de 18 para 27.

S3 Contenção. Defesa visível de uma passagem. Só depois das duas compras anteriores. Empurrão procura deslocamento válido e não atravessa parede nem teletransporta.

## Combate proposto

Ataques a cada 1 s, alcance de duas células além do contato visual e uma aplicação de dano por ciclo. Soldado base causa 3 de dano/s e suporta 18 pontos. Com S1 e S2, causa 4,5 de dano/s e suporta 27. Contenção desloca invasora até quatro células a cada 3 s, limitado pela colisão.

Produtivas têm autodefesa básica, mas soldados atendem prioritariamente a invasão. A rainha não é uma combatente eficiente e não deve sair caçando. Recuperação de vida proposta: após 10 s fora de combate, recuperar 1 ponto/s para sobreviventes; isso não ressuscita uma formiga.

Alimento por novo soldado: 34 N. Força, vida, frequência de invasão e espaço de luta devem ser testados juntos. A capacidade de contenção representa uma ação nova; se o corredor não permite empurrar, o ataque comum continua, sem exigir uma sala de quartel.

## Defesa e contenção são momentos diferentes

S0 já produz um defensor funcional. S1 melhora o ataque e S2 melhora resistência, em qualquer ordem. S3 acrescenta empurrão e defesa de passagem depois de ambos. O jogador vê o papel defensivo desde o primeiro soldado, sem precisar completar o ramo para combater.

Empurrar exige espaço de destino possível. Se há parede, água perigosa ou geometria que impede deslocamento, a contenção não atravessa o mundo. O ataque básico continua existindo. O resultado de um confronto depende de quantidade, vida, dano, caminho e atendimento, não de uma vitória automática pelo desbloqueio.

---

[← 26 — Jardineira e cuidadora habilidades e números](../01_Design/26_Jardineira_e_cuidadora_habilidades_e_numeros.md) · [28 — Custos totais e regras de aplicação de melhorias →](../04_Data/28_Custos_totais_e_regras_de_aplicacao_de_melhorias.md)
