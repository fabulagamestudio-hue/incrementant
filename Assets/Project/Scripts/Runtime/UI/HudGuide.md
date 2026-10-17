# HUD da partida — guia

Como o HUD (uGUI) funciona e como mexer nele. Onde estão os arquivos: [Runtime/README.md](../README.md) e [Editor/README.md](../../Editor/README.md).

## Como funciona

- **Hierarquia gerada.** O objeto `HUD` da cena `AntFarm` (Canvas de referência 1920x1080 (`ReferenceResolution` no setup: aumente para diminuir o HUD), escala por tela com Match 0,5; layout baseado em `Art/UI/Refs de Telas/05-hud-partida.png`) é recriado do zero por **Incrementant/Setup/Montar HUD da partida** (também chamado por *Criar cena AntFarm*). Ajustes à mão dentro dele se perdem: mude o layout em `Editor/UI/AntFarmHudSetup.cs`.
- **Sprites.** `Editor/UI/UiSpriteImporter.cs` (menu **Incrementant/UI/Aplicar importação e 9-slice dos sprites**) importa `Art/UI/Sprites` como sprite Point e grava as bordas 9-slice (tabela `Borders`, em pixels L/B/R/T). Sprite novo que estica entra nessa tabela; ícones, checkbox, toggle e badge ficam sem borda. `FormigueiroSkillTree/` não é tocado. As imagens usam `Image.Type.Sliced` com PPU 100 (1 pixel do sprite = 1 unidade do canvas).
- **Runtime.** `AntFarmHud` só lê `AntFarmGame` (`Colony`, `Timer`, `SpeedIndex`, `Paused`, `BrushIndex`, `SaveStatus`) e chama `SetSpeed`, `SetPaused` e `SetBrush`. Textos atualizam 4x por segundo; o destaque dos botões (sprite `*_focus`) todo frame. Os listeners são inscritos em `OnEnable` e removidos em `OnDisable`.
- **Mouse sobre o HUD.** `AntFarmGame.PointerOverHud()` consulta o `EventSystem` (módulo `InputSystemUIInputModule`, criado pelo setup), então pintar e rolar o pincel não passam por baixo dos botões.
- **Fonte.** `Text` legado com a fonte padrão da Unity (`LegacyRuntime.ttf`); não há fonte pixel no projeto.

## O que existe e o que é placeholder

| Elemento | Estado |
|---|---|
| Cartões Nutrientes / Formigas / Rainha | Reais: nutrientes e consumidos; adultas / capacidade e ovos, larvas e pupas; estado da sala da rainha |
| Menu Colônia → Pintar → 5 pincéis | Real (troca o pincel de `AntFarmGame`; o 5º é o Berçário) |
| Menu Colônia → Rainha | Real: modo (Automático, Tipo específico, Pausado), tipo (Comum, Jardineira), próxima formiga com custo, contagem por tipo e botão Criar formiga (ver "Política da rainha") |
| Evolução, Ambientes, Avisos | Desativados (sem sistema por trás) |
| Pausa e 1x/3x/8x | Reais (P também pausa; 1/2/3 trocam a velocidade) |
| Dica do rodapé | Mostra o aviso de salvar/carregar por 5 s |
| "Próxima" postura e reservadas do cartão, taxa de nutrientes, [ESC] MENU | Só o painel Rainha mostra a próxima postura; o resto não existe |

O HUD IMGUI antigo (contadores de simulação, clima, salvar/carregar) continua no `AntFarmGame`, escondido; **F1** o mostra (`_showDebugHud`).

## Política da rainha

`AntColony.QueenMode` (`Automatic`, `Specific`, `Paused`) e `QueenTarget` decidem o papel do próximo ovo; `SetQueenPolicy` os muda e `NextAntRole` (nulo se pausada) alimenta o painel. No automático, cada compra vai para o papel mais abaixo da parte do design (comuns 20, carregadoras 20, cortadeiras 10, escavadoras 15, jardineiras 15, renormalizadas entre os cinco que existem); no específico, o papel escolhido; pausada, a rainha não põe ovos novos (as crias existentes seguem). Escolher um tipo no painel já liga o modo específico. Não há botão de criar: a rainha põe ovos sozinha (ciclo, berçário e capacidade em [AntFarmGuide](../AntFarm/AntFarmGuide.md), "Criação de formigas"). O painel mostra o estado da reprodução, o próximo ovo com custo, ovos/larvas/pupas, e adultas e berçário com suas vagas; o cartão Formigas mostra adultas / capacidade e ovos, larvas e pupas. Os papéis e o que cada um muda estão em [AntFarmGuide](../AntFarm/AntFarmGuide.md) ("Papéis das formigas"); o modo, o tipo e as crias vão no save (versão 8).

## Como estender

- Novo botão do menu: adicione na lista do setup e ligue um campo `[SerializeField]` em `AntFarmHud`.
- Nova tela (pausa, Evolução…): crie outro painel sob o mesmo Canvas pelo setup, com os sprites de `panels/`.
- Custos e contadores da referência só entram quando o dado existir em `AntColony`.
