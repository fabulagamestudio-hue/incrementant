# Tutorial da primeira abertura — guia

Como o tutorial funciona e como mexer nele. Onde estão os arquivos: [Runtime/README.md](../README.md) (pasta `Tutorial/`).

## Quando roda

- Uma vez por instalação: `TutorialProgress` (`PlayerPrefs`, chave `Incrementant.TutorialDone`) guarda que ele foi concluído ou pulado. Fica fora de `settings.json` para o botão PADRÃO das configurações não trazê-lo de volta.
- Não roda ao **continuar um save** (`AntFarmGame.StartedFromSave`) nem na **vitrine** do menu principal (`AntFarmGame.Showcase`).
- Se o mapa for regenerado (R) ou um save for carregado no meio do caminho (`AntFarmGame.Generation` mudou), ele termina e conta como visto: as posições do roteiro deixaram de valer.
- Sair do jogo no meio não grava o progresso: na próxima abertura ele recomeça.
- Para rever: menu **Incrementant/Tutorial/Mostrar o tutorial na próxima partida** (apaga a marca).

## Como funciona

- **Quem avança é o jogador.** `TutorialDirector` só observa o jogo (marcas de escavação, salas pintadas, menus abertos, seleção de nós da árvore) e conduz a câmera e a pausa. Cada etapa (`Step`) decide a cada quadro o que mostrar (`Say`), o que destacar e qual contorno desenhar, e `Apply` aplica no fim do quadro.
- **Roteiro:** `Intro` → `ChooseDig` → `DigFirst` → `PaintNutrients` → `NutrientsInfo` → `DigSecond` → `PaintNursery` → `QueenInfo` → `BroodInfo` → `OpenTree` → `ExploreTree` → `ThreatArrival` → `ThreatDefense` → `Outro`. Se não couber a segunda câmara, as etapas do berçário são puladas.
- **Câmaras sugeridas** (`TutorialPlanner`, lógica pura, testada em `TutorialPlanTests`): o abrigo é achado por inundação a partir do ninho (`ShelterBounds`); cada câmara é uma elipse de 13x7 ligada ao abrigo por um túnel de 3 de altura, de um dos lados e em até 4 alturas, escolhendo o de menor `Penalty` (rocha, areia, água, romper a superfície, sobrepor a outra câmara). A segunda câmara evita a primeira. Células de rocha não entram na meta (o pincel de escavar não as cava).
- **Condições de avanço:** marcar = 60% das células a escavar marcadas ou já vazias, com alguma marca ainda de pé (acabou a marca antes da hora, volta a pedir marcação); escavar = 90% vazias e a câmara ligada ao ninho por células vazias (conferida a cada 0,5 s); nutrientes e berçário = 16 células do interior pintadas (berçário também pede `NurserySlots >= 1`); árvore = abrir, consultar um nó (`SkillTreeScreen.NodeSelections`) e fechar.
- **Pausa.** O jogo fica pausado na abertura, na escolha do pincel, até haver marcas suficientes, enquanto a árvore está aberta e na despedida (`HoldPause`; o diretor reaplica a pausa se o jogador apertar P ou uma velocidade, menos com o menu do ESC aberto). Os ataques de cupins ficam desligados (`AntColony.RaidsEnabled`) até o fim, e o valor anterior volta.
- **Pop-ups.** Central (abertura e despedida): fundo escuro que bloqueia cliques, `AntFarmGame.InputLocked` ligado e ESC ignorado (`ModalOpen`). Lateral: ao lado do alvo (UI ou ponto do mundo), embaixo dos cartões do topo, no canto livre embaixo à esquerda na tela de evolução, ou no meio da direita. Os botões das etapas da rainha e dos inimigos ("Continuar") não estão no roteiro original: servem para o jogador não ser apressado por um relógio.
- **Câmera.** `AntFarmGame.FrameCells` enquadra um retângulo em células com `MapCamera.MoveTo` (suave, tempo real). Durante o deslocamento o input da câmera fica ignorado; fora dele o jogador mexe livre, e nunca há movimento automático enquanto ele pinta. O botão **Ver objetivo** aparece quando o retângulo do objetivo não está inteiro na tela ou a vista está muito aberta (`AntFarmGame.IsFramed`). Não existe cinematic no projeto: a abertura salta para a vista mais aberta e desce até o abrigo.
- **Destaques.** Moldura dourada pulsante em volta do botão (menu → submenu → pincel, conforme o que está aberto); contorno das câmaras numa textura de 4 texels por célula, dourada na escavação e verde com brilho nas salas, desenhada por baixo do HUD; marcador vermelho (anel e seta, os mesmos gráficos das marcas de cupim) na entrada.
- **Montagem.** Os pop-ups e botões vêm do setup do HUD (`BuildTutorial` em `Editor/UI/AntFarmHudSetup.cs`: painéis `panel_moss` e `panel_plain`, botão verde `button_action`); molduras, contorno e marcador o diretor cria em código. Ajustes à mão no objeto `Tutorial` se perdem ao remontar o HUD. O `AntFarmHud` expõe o que o tutorial usa: `SpotRect`, `BrushRect`, `PaintMenuOpen`, `EnvironmentMenuOpen`, `TreeOpen`, `Tree` e o evento `BrushChosen`.

## Como estender

- Texto ou ordem: constantes `Text*` e `Enter`/`UpdateStep` em `TutorialDirector`. Índices de pincel (`BrushDig`, `BrushNutrientRoom`, `BrushNursery`) espelham `AntFarmGame.BrushMode`: ao reordenar o enum, ajuste-os.
- Etapa nova: valor em `Step`, `Enter` (câmera e pausa), `UpdateStep` (texto, destaque, condição) e, se tiver botão, `OnPopupButton`.
- Medidas novas (célula pintada, escavada…) entram em `TutorialPlanner` com teste.

## O que evitar

- Não deixe o diretor mexer na simulação além da pausa e dos ataques; ele não cria nem remove formigas, marcas ou salas.
- Não use o relógio para avançar uma etapa de ação do jogador.

## Pendências

- Etapas de inimigos e despedida e a posição dos pop-ups na árvore de evolução (depois do ajuste) não foram vistas em Play Mode; falta uma pessoa jogar o roteiro inteiro.
- Salvar o progresso no meio do tutorial.
