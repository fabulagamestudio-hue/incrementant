# HUD da partida — guia

Como o HUD (uGUI) funciona e como mexer nele. Onde estão os arquivos: [Runtime/README.md](../README.md) e [Editor/README.md](../../Editor/README.md).

## Como funciona

- **Hierarquia gerada.** O objeto `HUD` da cena `AntFarm` (Canvas de referência 1920x1080 (`ReferenceResolution` no setup: aumente para diminuir o HUD), escala por tela com Match 0,5; layout baseado em `Art/UI/Refs de Telas/05-hud-partida.png`) é recriado do zero por **Incrementant/Setup/Montar HUD da partida** (também chamado por *Criar cena AntFarm*). Ajustes à mão dentro dele se perdem: mude o layout em `Editor/UI/AntFarmHudSetup.cs`.
- **Sprites.** `Editor/UI/UiSpriteImporter.cs` (menu **Incrementant/UI/Aplicar importação e 9-slice dos sprites**) importa `Art/UI/Sprites` como sprite Point e grava as bordas 9-slice (tabela `Borders`, em pixels L/B/R/T). Sprite novo que estica entra nessa tabela; ícones, checkbox, toggle e badge ficam sem borda. `FormigueiroSkillTree/` não é tocado. As imagens usam `Image.Type.Sliced` com PPU 100 (1 pixel do sprite = 1 unidade do canvas).
- **Runtime.** `AntFarmHud` só lê `AntFarmGame` (`Colony`, `Timer`, `SpeedIndex`, `Paused`, `BrushIndex`, `SaveStatus`) e chama `SetSpeed`, `SetPaused` e `SetBrush`. Textos atualizam 4x por segundo; o destaque dos botões (sprite `*_focus`) todo frame. Os listeners são inscritos em `OnEnable` e removidos em `OnDisable`.
- **Mouse sobre o HUD.** `AntFarmGame.PointerOverHud()` consulta o `EventSystem` (módulo `InputSystemUIInputModule`, criado pelo setup), então pintar e rolar o pincel não passam por baixo dos botões.
- **Fonte.** `Text` legado com a **Press Start 2P** (`Art/UI/Fontes/PressStart2P`, SIL OFL; ver o LEIA-ME da pasta). Ela é desenhada numa grade de 8 px e tem traço sempre de 1 pixel da fonte, então só tamanhos **múltiplos de 8** saem com pixels inteiros: o HUD usa 16, 24, 32 e 48 (`TextSmall`, `TextMedium`, `TextLarge`, `TextTitle` em `AntFarmHudSetup`; `BuildText` ajusta qualquer pedido ao mais próximo). É **monoespaçada e larga**: cada letra ocupa o próprio tamanho de largura (16 px por letra em 16), então textos novos precisam de caixa de `tamanho × número de letras`. Só tem um peso (sem Bold); o destaque vem do tamanho. `UiFontImporter` (menu **Incrementant/UI/Aplicar importação das fontes**, chamado pelo setup) usa fonte dinâmica com Hinted Raster. **Limite conhecido:** as maiúsculas acentuadas têm a mesma altura das sem acento, então o acento fica comprimido dentro da letra (`Í` parece um `i`, `Ô` um `O` com um ponto); as minúsculas acentuadas saem bem. Linhas quebradas precisam de `lineSpacing` maior que 1 (a altura de linha da fonte é igual ao corpo). Os TTFs da Pixelify Sans continuam em `Art/UI/Fontes/PixelifySans-PTBR`, sem uso.

## O que existe e o que é placeholder

| Elemento | Estado |
|---|---|
| Cartões Nutrientes / Formigas / Rainha | Reais: nutrientes e consumidos; adultas / capacidade e ovos, larvas e pupas; estado da sala da rainha |
| Menu Colônia → Ferramenta → Cavar, Construir e Dedo | Real (troca o pincel de `AntFarmGame`; o **Dedo** não pinta: cada clique esquerdo fere o cupim mais perto da mira, `AntColony.HitTermiteAt`; com o jogo pausado o clique só toca o som de negado). Com o Dedo ativo e o mouse sobre o mapa, `AntFarmHud` troca o cursor do sistema por `Art/UI/Cursors/cursor_finger.png` (cópia de `icons/hand_index_up`, tipo Cursor via `UiCursorImporter`; ponta em `_fingerCursorHotspot`) |
| Menu Colônia → Ambientes → Sala da rainha, Sala de nutrientes, Berçário, Resíduos e Gotas de orvalho (índice 7, ícone de folha provisório; guia [DewRoomGuide](../Colony/DewRoomGuide.md)) | Real (painel `SubmenuAmbientes`; mesmos pincéis de antes, só mudaram de menu: o índice em `AntFarmGame.BrushLabels` e o save não mudam. Resíduos fica apagado e sem clique até o primeiro ataque de cupins, `AntFarmGame.IsBrushUnlocked`) |
| Alerta dos cupins (centro do topo, abaixo do cartão Formigas) | Real: `AntFarmHud._raidAlert` (sprite `panels/notification`, ícone de alerta, sem raycast). Mostra a contagem do ataque iminente, "CUPINS ATACANDO: N" com o objetivo (crias, canteiros, setores, paredes ou indo embora) e, por 6 s, o fim do ataque. Regras em [RaidGuide](../Colony/RaidGuide.md) |
| Menu Colônia → Rainha | Real: modo (Automático, Tipo específico, Pausado), postura diante dos cupins (Defensiva ou Ofensiva, `QueenStance`), tipo (Comum, Carregadora, Cortadeira, Escavadora, Jardineira, Cuidadora, Soldado; a lista vem de `AntRoles.All`, então ao mudá-la rode **Incrementant/Setup/Montar HUD da partida**), próxima formiga com custo, contagem por tipo e botão Criar formiga (ver "Política da rainha") |
| Menu Colônia → Evolução | Real: tela cheia `TelaEvolucao` com duas abas (ver "Tela Evolução"): Rainha e classes (compra de verdade) e Formigueiro (só exibição) |
| Avisos | Desativado (sem sistema por trás) |
| Pausa e 1x/3x/8x | Reais (P também pausa; 1/2/3 trocam a velocidade) |
| Sino (acima da velocidade, ícone `icons/bell`) | Real: alterna `AntColony.Recalled`. Ligado ("volte para o formigueiro"), as formigas terminam o que carregam e só vagam pelo formigueiro; cuidadoras e soldados seguem normais. Desligado ("volte ao trabalho"), retomam as tarefas. Não vai para o save |
| FPS e tempo jogado (canto superior direito, abaixo do cartão Rainha) | Real (some com "Mostrar FPS e tempo" desligado nas configurações): FPS médio a cada 0,25 s e relógio da sessão em HH:MM:SS (tempo real, ignora pausa e velocidade; não vai no save) |
| Dica do rodapé | Mostra o aviso de salvar/carregar por 5 s |
| Menu do ESC e Configurações | Reais (ver "Menu do ESC e configurações") |
| "Próxima" postura e reservadas do cartão, taxa de nutrientes | Só o painel Rainha mostra a próxima postura; o resto não existe |

O HUD IMGUI antigo (contadores de simulação, clima, salvar/carregar) continua no `AntFarmGame`, escondido; **F2** o mostra (`_showDebugHud`; o F1 é o cheat de formigas, ver [AntFarmGuide](../AntFarm/AntFarmGuide.md)).

## Política da rainha

`AntColony.QueenMode` (`Automatic`, `Specific`, `Paused`) e `QueenTarget` decidem o papel do próximo ovo; `SetQueenPolicy` os muda e `NextAntRole` (nulo se pausada) alimenta o painel. No automático, cada compra vai para o papel mais abaixo da parte do design (comuns 20, carregadoras 20, cortadeiras 10, escavadoras 15, jardineiras 15, renormalizadas entre os cinco que existem); no específico, o papel escolhido; pausada, a rainha não põe ovos novos (as crias existentes seguem). Escolher um tipo no painel já liga o modo específico. Não há botão de criar: a rainha põe ovos sozinha (ciclo, berçário e capacidade em [AntFarmGuide](../AntFarm/AntFarmGuide.md), "Criação de formigas"). O painel mostra o estado da reprodução, o próximo ovo com custo, ovos/larvas/pupas, e adultas e berçário com suas vagas; o cartão Formigas mostra adultas / capacidade e ovos, larvas e pupas. Os papéis e o que cada um muda estão em [AntFarmGuide](../AntFarm/AntFarmGuide.md) ("Papéis das formigas"); o modo, o tipo e as crias vão no save (versão 8).

## Tela Evolução

O botão EVOLUÇÃO abre `TelaEvolucao` (`SkillTreeScreen`), em tela cheia sobre o resto do HUD, na linha da referência `Refs de Telas/22-arvore-formigueiro-radial.png`: fundo de terra, título, saldo de nutrientes, VOLTAR (ou ESC) e duas abas.

- **Rainha e classes** (aba aberta por padrão): os 26 nós da árvore da rainha, um ramo por coluna, com o centro (a rainha) e um tronco que sobe até o primeiro nó de cada ramo. Clicar num nó o seleciona; DESBLOQUEAR chama `AntColony.TryBuy`. Regras e efeitos: [QueenTreeGuide](../Colony/QueenTreeGuide.md).
- **Formigueiro:** cultivo, berçário, depósito e resíduos nas posições da referência. É só exibição: os sistemas por trás não existem, então todos os nós aparecem bloqueados e o botão diz EM BREVE. Os dados são `HiveTreeInfo` (seções 17 a 21 do design).
- **Estados.** Cada nó é feito de camadas (`SkillNodeWidget`): losango (adquirido, disponível ou bloqueado, sprites `Nodes/diamond_*`), ícone, cadeado nos bloqueados e anel de seleção por cima (a seleção não apaga o estado). As ligações usam o segmento `Connections/link_*` repetido: verde quando pai e filho estão adquiridos, dourado quando o filho pode ser comprado, apagado nos demais. A legenda do rodapé explica os quatro estados.
- **Fechar.** VOLTAR ou ESC; o ESC é tratado pelo `AntFarmHud` (ver "Menu do ESC e configurações").
- **Arte.** Os sprites vêm de `Art/UI/Sprites/FormigueiroSkillTree/Textures`, importados por `SkillTreeSpriteImporter` e desenhados a 2 pixels de tela por pixel de sprite (`PixelScale` em `AntFarmHudSetup`). Esse kit só tem ícones do formigueiro (C, B, D, R); os nós da rainha usam os ícones gerais de `Sprites/icons` (ovo, formiga, caixa, folha, pá, cogumelo, larva, alerta) por não haver arte própria.
- **Tempo.** Abrir a tela não pausa o jogo (regra em aberto no design, seção 34). A tela cobre o mundo, mas a simulação segue.
- **Montagem.** `BuildTreeScreen` (em `Editor/UI/AntFarmHudSetup.cs`) recria tudo junto com o HUD; as posições dos nós da rainha são `QueenColumn0`/`QueenColumnStep`/`QueenRows`/`QueenSlot`. Ajustes à mão se perdem. O `AntFarmHud` só abre e fecha a tela e mostra as classes ainda bloqueadas no painel Rainha.

## Menu do ESC e configurações

- **ESC.** Só o `AntFarmHud` lê o ESC, e cada toque fecha uma camada: configurações → menu → jogo; sem menu aberto, fecha a tela Evolução ou o submenu da colônia (Pintar, Rainha); sem nada aberto, abre o menu. Tela nova que feche com ESC entra nessa ordem em `HandleEscape`, em vez de ler a tecla sozinha (duas telas reagiriam ao mesmo ESC).
- **Menu (`PauseMenu`, objeto `MenuPausa`).** Fundo escurecido em tela cheia que bloqueia cliques no mundo e no HUD, e o painel MENU: Continuar, Configurações, Salvar partida (F5), Carregar partida (F9), Menu principal (volta à cena `MainMenu` sem salvar), Sair do jogo. Abrir pausa a simulação e liga `AntFarmGame.InputLocked` (sem atalhos, câmera nem pincel); fechar devolve a pausa como estava. O aviso de salvar/carregar aparece no próprio painel. Sair no Editor só mostra um aviso.
- **Configurações (`SettingsScreen`, objeto `TelaConfiguracoes`).** Troca de lugar com o painel do menu. Linhas: volume geral, música, efeitos sonoros (sliders com %), efeitos visuais (Completo/Reduzido/Desligado), tela cheia, resolução (nativa + as do monitor), VSync, limite de FPS (apagado com VSync ligado) e mostrar FPS e tempo; rodapé com PADRÃO e VOLTAR. Cada mudança é aplicada na hora; o arquivo é gravado ao sair da tela. Dados e aplicação: [SettingsGuide.md](../Settings/SettingsGuide.md).
- **Montagem.** `BuildPauseMenu` e `BuildSettingsScreen` em `Editor/UI/AntFarmHudSetup.cs`, por último sob o canvas (cobrem até a tela Evolução). Sliders, interruptores (`controls/toggle_*`) e alças em escala 2 (`BuildScaledSprite`); o Slider estica a alça na altura da área, então só a largura vai no `sizeDelta`.

## Como estender

- Novo pincel: entra em `AntFarmGame.BrushLabels` e, no setup, na lista `paintBrushes` ou `environmentBrushes` (decide em qual painel o botão mora); o `AntFarmHud` guarda todos num vetor único na ordem do jogo.
- Novo botão do menu: adicione na lista do setup e ligue um campo `[SerializeField]` em `AntFarmHud`.
- Nova tela (pausa, Evolução…): crie outro painel sob o mesmo Canvas pelo setup, com os sprites de `panels/`.
- Custos e contadores da referência só entram quando o dado existir em `AntColony`.

## Marcas dos cupins (2026-10-18)

`TermiteMarkers` (chamado por `AntFarmGame` a cada quadro, depois de desenhar o enxame) cria em código um canvas Overlay próprio (ordem -1, abaixo do HUD, sem alvo de raio). Cada cupim vivo (`Termite.Alive`, voando ou no chão) ganha um círculo vermelho; fora da tela, o círculo dá lugar a uma seta vermelha presa à borda, na linha do centro da tela até ele. Ao morrer, a marca é devolvida ao pool e some. Sprites do círculo e da seta são gerados em código. O tamanho do círculo segue o zoom (6 células, mínimo 36 px).

## Tutorial (2026-10-18)

O objeto `Tutorial` do HUD (montado por `BuildTutorial`, entre a tela de evolução e o menu do ESC) guarda os pop-ups e botões do tutorial da primeira abertura; quem o conduz é o `TutorialDirector`, descrito em [TutorialGuide](../Tutorial/TutorialGuide.md). Para ele, o `AntFarmHud` expõe `SpotRect`, `BrushRect`, `PaintMenuOpen`, `EnvironmentMenuOpen`, `TreeOpen`, `Tree` e o evento `BrushChosen`, e ignora o ESC enquanto o pop-up central está aberto; o `SkillTreeScreen` expõe `NodeSelections` e os retângulos das abas e do painel de detalhe.

## Dicas (tooltips) (2026-10-18)

`HudTooltips` (criado pelo `AntFarmHud` em `Awake`) monta em código um canvas Overlay próprio (ordem 1000, sem alvo de raio) com um painel e um `Text` na Press Start 2P a 16 px, 35 letras por linha. `Attach(componente, Func<string>)` põe um `TooltipTarget` no objeto; a dica aparece 0,35 s depois de o mouse parar sobre ele (o evento sobe da hierarquia, então um cartão inteiro vale pelo pai), segue o mouse, vira de lado nas bordas da tela e some ao clicar ou sair. A função só roda enquanto a dica está aberta, então pode ler o estado da colônia.

- **Botões:** Ferramenta, Ambientes, Rainha, Evolução, pausa, velocidades, pincéis, modos, posturas e tipos de formiga (descrição do papel e, se bloqueado, o nó de Evolução que o libera).
- **Seções (produção):** os pincéis das salas (rainha, nutrientes, berçário, resíduos, gotas de orvalho) e os cartões Nutrientes, Formigas e Rainha somam à descrição o estado: "Falta: …" (o que impede começar) ou "Produzindo/Funcionando: …" com os números. A lógica fica em `HudTooltipTexts`, que espelha as regras de `AntFarmGame.NutrientRoomStatus`/`DewRoomStatus` (painel F2); regra de sala nova muda os dois.
- **Textos:** chaves `tip.*` em `LocalizationCatalog`. Escreva-os em caixa mista (a fonte não acentua bem as maiúsculas). Para o teste de chaves, não monte chave por concatenação com prefixo literal (`"tip.x." + ...`): use um `switch`.
- **Cartões e raio:** o `AntFarmHud` liga o raio do `Image` do cartão (`AttachCardTooltip`); o cartão passa a bloquear o clique do mundo debaixo dele.
- **Fora de escopo:** botões do menu do ESC, configurações, árvore de evolução e nós não têm dica; a dica "4 células" da sala de orvalho está escrita no texto (`tip.st.dew.nospot`), não vem de `ColonySettings`.

