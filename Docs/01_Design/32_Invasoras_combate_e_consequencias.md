# 32 — Invasoras combate e consequências

Fonte: `Increment_Ant_Game_Design_Completo_Revisado.docx`, seção 32. Consolidação do documento: 2026-10-17; conversão: 2026-10-17.

[Índice da documentação](../README.md)

> Transcrição da referência de design. Valores propostos e decisões em aberto conservam o estado indicado no texto; esta documentação não comprova implementação no jogo.

## Intenção e percurso definidos

Invasoras entram por um acesso e seguem os caminhos do formigueiro, atacando as formigas que encontram como se fossem comendo cada uma. Elas não perseguem especificamente a rainha nem calculam uma rota global até ela. Se a rainha está perto da entrada, podem encontrá-la cedo; se está mais abaixo, podem encontrá-la no fim do percurso.

Ao encontrar a rainha, a invasora ataca como outra vítima. Vida da rainha chegando a zero gera game over. A interpretação anterior de roubar nutrientes e fugir foi substituída. Não atribuir roubo de comida a esse inimigo sem uma nova decisão.

## Proposta inicial de invasão

| Parâmetro | Valor base proposto | Evolução sugerida para teste |
| --- | --- | --- |
| Primeira elegibilidade | Após 360 s e fim do tutorial | Aviso de 10 s antes da chegada |
| Frequência | Intervalo 180 a 300 s | Espaçar ao menos 45 s após outro evento negativo |
| Quantidade | 1 invasora | Depois de 900 s, até 2 por evento |
| Vida | 10 pontos | Pode subir para 14 na segunda faixa |
| Velocidade | 18 células/s | Manter menor que a trabalhadora em fuga |
| Dano | 2 pontos a cada 1 s | Só aplicar em alcance e contato válidos |
| Alcance | 2 células além do contato | Sem atravessar paredes |
| Escolha de caminho | Seguir ramo acessível; bifurcação local | Não usar alvo rainha para navegação global |

No primeiro teste, considerar invasão adiada até a defesa ser apresentada e existir possibilidade de produzir soldados. Não exigir um soldado já vivo para toda invasão do jogo completo, pois a defesa é uma decisão do jogador. O critério da primeira ameaça é uma regra operacional a confirmar.

Não fazer vítimas aparecerem mortas por proximidade abstrata: movimento, contato, ataque e vida precisam corresponder. Cadáveres ficam no mundo e podem ser removidos para resíduos. Contenção de S3 empurra somente para posição física válida. Pausa congela combate e o relógio do aviso.

## Percurso da invasão e exposição da rainha

A invasora segue o formigueiro e encontra vítimas pelo caminho. Não conhece a posição da rainha para evitar outras áreas e ir direto até ela. Se a rainha está perto de uma entrada, pode ser uma das primeiras vítimas; se está profunda, pode ser alcançada mais tarde.

O design de movimento ou realocação da rainha entre câmaras ainda não está fechado. Este documento não presume um comando de transferir rainha ou uma escolha automática do lugar mais seguro. A posição existente influencia a invasão, e qualquer controle novo de realocação precisa de uma decisão própria.

A proposta atual não atribui roubo de alimento à invasora. Ela ameaça a população e a rainha. Sem produtivas, a recuperação especial pode ocorrer se a rainha sobreviver. Com rainha morta, o resultado é game over, mesmo que ainda existam soldados e alimento.

---

[← 31 — Jardim frutas folhas e visitantes](../02_World_Art_UI/31_Jardim_frutas_folhas_e_visitantes.md) · [33 — Menus fluxo de telas e configurações →](../02_World_Art_UI/33_Menus_fluxo_de_telas_e_configuracoes.md)
