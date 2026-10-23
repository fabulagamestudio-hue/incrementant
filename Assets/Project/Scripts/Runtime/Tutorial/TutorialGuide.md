# Tutorial (tour da interface) — guia

Como o tutorial funciona e como mexer nele. Onde estão os arquivos: [Runtime/README.md](../README.md) (pasta `Tutorial/`).

## Quando roda

- Em **todo jogo novo** (Novo jogo no menu principal), mesmo que o jogador já o tenha concluído ou pulado antes: não há marca de "já visto" (decisão de 2026-10-22).
- Não roda ao **continuar um save** (`AntFarmGame.StartedFromSave`) nem na **vitrine** do menu principal (`AntFarmGame.Showcase`).
- Só roda se a cena da partida veio do **menu principal** (`AntFarmGame.StartedFromMenu`, lido de `GameLaunch.ConsumeFromMenu`, pedido do desenvolvedor em 2026-10-23). Abrir a cena `AntFarm` direto no Editor e dar Play começa uma colônia nova sem o tutorial; para testá-lo, entre pelo `MainMenu`. O recado vale uma vez: voltar ao menu e escolher Novo jogo de novo liga outra vez.
- Se o mapa for regenerado dentro da cena (R, ou "novo mapa" na tela de fim de jogo) ou um save for carregado no meio do tour (`AntFarmGame.Generation` mudou), ele termina e não recomeça nesse mapa; só no próximo Novo jogo (decisão de 2026-10-22).

## Como funciona

- **Objetivo** (pedido de 2026-10-22): explicar como ler a interface e o que cada botão faz, sem obrigar nenhuma ação; o jogador clica onde quiser durante o tour. O roteiro guiado anterior (cavar câmaras sugeridas, pintar nutrientes e berçário, árvore, cupins) foi substituído; está no histórico do Git.
- **Roteiro** (`TutorialDirector.Stops`, em ordem): boas-vindas (pop-up central) → câmera → cartões Nutrientes, Formigas e Rainha → menus Ferramenta, Ambientes, Rainha e Evolução → barra inferior (pausa, painel da colônia, sino, mostrar ambientes) → cupins → dicas e glossário (destaca o botão `?`) → despedida (pop-up central, cita o glossário). Cada parada tem uma peça do HUD (`AntFarmHud.Spot`, ou nenhuma), um título e um texto.
- **Avanço.** Próximo, Voltar (apagado na primeira parada) e "Pular tutorial" (canto inferior direito, escondido na despedida). O contador mostra a parada atual. Nada avança sozinho.
- **Liberdade.** O pop-up lateral não tem fundo: HUD, menus e mapa seguem clicáveis. Só os pop-ups centrais travam o mundo (`AntFarmGame.InputLocked`) e fazem o `AntFarmHud` ignorar o ESC (`ModalOpen`).
- **Pausa e cupins.** O jogo pausa ao abrir o tour; se o jogador despausar no meio, a escolha é dele (o diretor não reaplica a pausa). Os ataques ficam desligados (`AntColony.RaidsEnabled`) até o fim, quando o valor anterior volta e o jogo despausa.
- **Câmera.** Só na abertura: salta para a vista mais aberta e desce até o abrigo da rainha (`AntFarmGame.FrameCellsFromOverview`). Depois o jogador mexe livre.
- **Pop-up lateral.** Cartões do topo: embaixo do cartão, nunca sobre a coluna do menu Colônia (`ColonyMenuRight`). Botões da barra inferior: acima dela. Menus e paradas sem peça: no meio da direita, para não cobrir os submenus que o jogador abrir. Altura calculada pelo texto.
- **Menu Colônia.** Fica recuado fora do tutorial (ver HudGuide); nas paradas Ferramenta, Ambientes, Rainha e Evolução o HUD o mantém aberto (`HoldsColonyMenu`).
- **Destaque.** Moldura dourada pulsante (`HighlightFrame`, criada em código) em volta da peça; some se a peça estiver escondida ou com a tela de evolução aberta.
- **Montagem.** Pop-ups e botões vêm do setup do HUD (`BuildTutorial` em `Editor/UI/AntFarmHudSetup.cs`: painéis `panel_moss` e `panel_plain`, botões verdes `button_action`). Ajustes à mão no objeto `Tutorial` se perdem ao remontar o HUD.
- **Textos.** Chaves `tutorial.*` em `LocalizationCatalog` (pt-BR e inglês); depois de mudar, rode **Incrementant/Setup/Configurar localização**. A fonte tem 16 px por letra: o texto lateral cabe em cerca de 34 letras por linha.

## Como estender

- Parada nova: uma entrada em `Stops` (peça, chave do título, chave do texto) e as chaves no catálogo. Peça do HUD nova: valor em `AntFarmHud.Spot` e o caso em `SpotRect` (devolve null quando a cena não tem o botão).

## O que evitar

- Não deixe o diretor mexer na simulação além da pausa e dos ataques.
- Não use o relógio para avançar uma parada; não bloqueie cliques no pop-up lateral.

## Pendências

- Falta uma pessoa jogar o tour inteiro com mouse; o agente percorreu as paradas pelo código em Play Mode (2026-10-22) e conferiu boas-vindas, Nutrientes, Ferramenta com o submenu aberto, Pausa e despedida.
