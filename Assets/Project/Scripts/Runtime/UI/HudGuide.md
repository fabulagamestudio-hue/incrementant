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
| Menu Colônia → Pintar → 5 pincéis | Real (troca o pincel de `AntFarmGame`; o 5º é o Berçário) |
| Menu Colônia → Rainha | Real: modo (Automático, Tipo específico, Pausado), tipo (Comum, Carregadora, Cortadeira, Escavadora, Jardineira, Cuidadora; a lista vem de `AntRoles.All`, então ao mudá-la rode **Incrementant/Setup/Montar HUD da partida**), próxima formiga com custo, contagem por tipo e botão Criar formiga (ver "Política da rainha") |
| Menu Colônia → Evolução | Real: tela cheia `TelaEvolucao` com duas abas (ver "Tela Evolução"): Rainha e classes (compra de verdade) e Formigueiro (só exibição) |
| Ambientes, Avisos | Desativados (sem sistema por trás) |
| Pausa e 1x/3x/8x | Reais (P também pausa; 1/2/3 trocam a velocidade) |
| Dica do rodapé | Mostra o aviso de salvar/carregar por 5 s |
| "Próxima" postura e reservadas do cartão, taxa de nutrientes, [ESC] MENU | Só o painel Rainha mostra a próxima postura; o resto não existe |

O HUD IMGUI antigo (contadores de simulação, clima, salvar/carregar) continua no `AntFarmGame`, escondido; **F2** o mostra (`_showDebugHud`; o F1 é o cheat de formigas, ver [AntFarmGuide](../AntFarm/AntFarmGuide.md)).

## Política da rainha

`AntColony.QueenMode` (`Automatic`, `Specific`, `Paused`) e `QueenTarget` decidem o papel do próximo ovo; `SetQueenPolicy` os muda e `NextAntRole` (nulo se pausada) alimenta o painel. No automático, cada compra vai para o papel mais abaixo da parte do design (comuns 20, carregadoras 20, cortadeiras 10, escavadoras 15, jardineiras 15, renormalizadas entre os cinco que existem); no específico, o papel escolhido; pausada, a rainha não põe ovos novos (as crias existentes seguem). Escolher um tipo no painel já liga o modo específico. Não há botão de criar: a rainha põe ovos sozinha (ciclo, berçário e capacidade em [AntFarmGuide](../AntFarm/AntFarmGuide.md), "Criação de formigas"). O painel mostra o estado da reprodução, o próximo ovo com custo, ovos/larvas/pupas, e adultas e berçário com suas vagas; o cartão Formigas mostra adultas / capacidade e ovos, larvas e pupas. Os papéis e o que cada um muda estão em [AntFarmGuide](../AntFarm/AntFarmGuide.md) ("Papéis das formigas"); o modo, o tipo e as crias vão no save (versão 8).

## Tela Evolução

O botão EVOLUÇÃO abre `TelaEvolucao` (`SkillTreeScreen`), em tela cheia sobre o resto do HUD, na linha da referência `Refs de Telas/22-arvore-formigueiro-radial.png`: fundo de terra, título, saldo de nutrientes, VOLTAR (ou ESC) e duas abas.

- **Rainha e classes** (aba aberta por padrão): os 26 nós da árvore da rainha, um ramo por coluna, com o centro (a rainha) e um tronco que sobe até o primeiro nó de cada ramo. Clicar num nó o seleciona; DESBLOQUEAR chama `AntColony.TryBuy`. Regras e efeitos: [QueenTreeGuide](../Colony/QueenTreeGuide.md).
- **Formigueiro:** cultivo, berçário, depósito e resíduos nas posições da referência. É só exibição: os sistemas por trás não existem, então todos os nós aparecem bloqueados e o botão diz EM BREVE. Os dados são `HiveTreeInfo` (seções 17 a 21 do design).
- **Estados.** Cada nó é feito de camadas (`SkillNodeWidget`): losango (adquirido, disponível ou bloqueado, sprites `Nodes/diamond_*`), ícone, cadeado nos bloqueados e anel de seleção por cima (a seleção não apaga o estado). As ligações usam o segmento `Connections/link_*` repetido: verde quando pai e filho estão adquiridos, dourado quando o filho pode ser comprado, apagado nos demais. A legenda do rodapé explica os quatro estados.
- **Arte.** Os sprites vêm de `Art/UI/Sprites/FormigueiroSkillTree/Textures`, importados por `SkillTreeSpriteImporter` e desenhados a 2 pixels de tela por pixel de sprite (`PixelScale` em `AntFarmHudSetup`). Esse kit só tem ícones do formigueiro (C, B, D, R); os nós da rainha usam os ícones gerais de `Sprites/icons` (ovo, formiga, caixa, folha, pá, cogumelo, larva, alerta) por não haver arte própria.
- **Tempo.** Abrir a tela não pausa o jogo (regra em aberto no design, seção 34). A tela cobre o mundo, mas a simulação segue.
- **Montagem.** `BuildTreeScreen` (em `Editor/UI/AntFarmHudSetup.cs`) recria tudo junto com o HUD; as posições dos nós da rainha são `QueenColumn0`/`QueenColumnStep`/`QueenRows`/`QueenSlot`. Ajustes à mão se perdem. O `AntFarmHud` só abre e fecha a tela e mostra as classes ainda bloqueadas no painel Rainha.

## Como estender

- Novo botão do menu: adicione na lista do setup e ligue um campo `[SerializeField]` em `AntFarmHud`.
- Nova tela (pausa, Evolução…): crie outro painel sob o mesmo Canvas pelo setup, com os sprites de `panels/`.
- Custos e contadores da referência só entram quando o dado existir em `AntColony`.
