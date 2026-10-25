# 48 — Árvore desenhada: túneis interligados

Proposta do projeto de 2026-10-24 (REQ-078). A direção vem do desenvolvedor: gerar uma primeira versão automática do desenho e depois redesenhar à mão. **Estrutura técnica e etapas são suposição do agente.**

Muda só o **desenho** da tela Evolução do [documento 46](46_Arvore_de_evolucao_em_niveis_e_tuneis.md). Continuam valendo do 46: domínios, níveis, a regra "túneis só descem" e a compra.

## Pedido do desenvolvedor

- "A minha ideia é ter túneis de um formigueiro se interligando com habilidades de formigas/salas/rainha, tudo se encontrando." Árvores de habilidade costumam ser caóticas, "similar a um desenho de um mapa de estrelas".
- Escolheu desenhar os caminhos à mão (opção B), "podendo mesclar com a C, onde é gerado automaticamente a primeira versão e depois eu redesenho".

## Por que o layout atual não chega lá

O limite não é a UI ser gerada por código. É o algoritmo de `UpgradeLayout`: uma grade com uma coluna por domínio, uma faixa por nível e câmaras laterais com até 2 colunas de nós. Os requisitos entre domínios já existem nos dados (Q2 exige B0, J1 exige C1, E2 exige T+2), mas não viram túneis que se cruzam. O desenho em si (`CaveGraphic`, `TunnelPathGraphic`) já aceita câmaras e túneis em qualquer posição.

## Proposta

1. **Asset de layout:** um asset guarda, por nó, a posição na tela e, por ligação (requisito → nó), os pontos do túnel. Ele não entra no save: muda o desenho, não o progresso.
2. **Gerador (a primeira versão):** um comando de Editor monta o asset a partir do catálogo:
   - a profundidade vem do nível (`Tier`), como hoje;
   - os nós se acomodam por atração e repulsão entre os ligados, puxados para a coluna do domínio, sem cruzar a faixa do nível;
   - as ligações entre domínios viram túneis curvos que se encontram em salões compartilhados.

   Rodar de novo só reposiciona os nós marcados como "automáticos".
3. **Editor de desenho (o redesenho):** uma janela de Editor que mostra a árvore como no jogo e permite:
   - arrastar nós e pontos dos túneis;
   - marcar um nó como "fixado à mão", para o gerador não mexer mais nele;
   - salvar no asset.
4. **Tela no jogo:** lê as posições e os túneis do asset. Um nó sem posição (um nó novo, por exemplo) cai no layout automático e aparece num aviso do Editor.
5. **Testes:** todo nó do catálogo tem posição; nenhum nó fica acima de um requisito; nós não se sobrepõem (≥ 125 px, como hoje).

## Etapas sugeridas

1. Asset e leitura na tela, preenchido pelo layout atual: nada muda visualmente.
2. Gerador com túneis entre domínios.
3. Janela de edição.

## Decisões (desenvolvedor, 2026-10-25)

- **Colunas:** cada domínio continua uma coluna reconhecível. Os nós se espalham dentro dela (até 150 px do eixo) e os túneis cruzam entre colunas.
- **Túneis entre domínios:** sempre à vista, apagados, e acendem ao focar um nó.

## Estado (2026-10-25)

As 3 etapas estão implementadas (REQ-078, PRG-084). O 1 foi além de "nada muda visualmente": a tela passou direto ao desenho novo, com uma câmara por nó no lugar das câmaras laterais em grade. Os salões compartilhados ficaram assim: a saída de um requisito que libera dois ou mais nós de outro domínio e a entrada de um nó que exige dois ou mais de fora. Como funciona e como mexer: [UpgradesGuide](../../Assets/Project/Scripts/Runtime/Upgrades/UpgradesGuide.md), "Desenho da árvore". Falta o desenvolvedor ver em jogo e redesenhar à mão.
