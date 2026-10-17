# UI — assets e preparação para montagem das telas

Guia do material visual disponível em `Assets/Project/Art/UI`, analisado em 17/10/2026. Este documento orienta a importação e o uso dos arquivos; não comprova que a interface esteja implementada ou validada no Editor.

## 1. Estrutura e finalidade

| Pasta | Conteúdo | Uso |
| --- | --- | --- |
| `Sprites/` | 55 PNGs gerais e 72 PNGs do conjunto da árvore | Peças para compor a interface |
| `Refs de Telas/` | 5 PNGs de telas completas | Referências de composição; não usar como interface funcional |
| `Fontes/` | `PixelifySans-PTBR-Unity.zip` | Fontes Regular e Bold, caracteres PT-BR, licença e instruções |

Total analisado: **127 sprites, 5 referências e 1 ZIP de fontes**. Os dois fundos da árvore estão incluídos na contagem de sprites.

A direção visual combina madeira, papel claro, terra e vegetação. Verde destaca ações e aquisições; dourado destaca seleção e disponibilidade; cinza e tons escuros ajudam a comunicar indisponibilidade. Estados também precisam de texto ou símbolos, sem depender apenas da cor.

## 2. Importação na Unity

Na análise inicial não havia arquivos `.meta` dentro desta pasta. As configurações abaixo são uma proposta de preparação, a confirmar no Editor. Preserve os GUIDs gerados pela Unity a partir da primeira importação.

### Sprites de interface

Para os PNGs de peças em `Sprites/`, começar com:

| Propriedade | Configuração inicial |
| --- | --- |
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single: cada PNG já é uma peça separada |
| Mesh Type | Full Rect |
| Pivot | Center; ajustar apenas quando a composição exigir |
| Filter Mode | Point (no filter) |
| Generate Mip Maps | Desativado |
| Wrap Mode | Clamp |
| Compression | None, para preservar os pixels na primeira validação |
| Alpha Is Transparency | Ativado nas peças com transparência |
| Max Size | Igual ou superior à maior dimensão do arquivo, evitando redução na importação |
| Pixels Per Unit | Manter um padrão inicial de 100 e compatibilizar com o Canvas antes de ajustar tamanhos nativos |

Não aplicar redução de resolução ou compressão automaticamente antes de conferir a qualidade. Não é necessário cortar uma spritesheet: os arquivos já estão separados.

### Bordas e redimensionamento

- Painéis, botões, abas e trilhos são candidatos a bordas no Sprite Editor e `Image Type = Sliced` no uGUI.
- Definir as bordas por arquivo: elas precisam preservar molduras e cantos reais. Não copiar uma margem numérica única para todos os sprites.
- Usar `Simple` e proporção preservada em ícones, nós, ponteiros e decorações.
- Manter folhas e musgos em peças separadas quando o redimensionamento do painel deformar a decoração.
- Barras de progresso devem separar fundo e preenchimento. Validar o preenchimento em zero, metade e completo para evitar bordas deformadas.

### Referências e fundos

Os PNGs de `Refs de Telas/` são imagens de consulta: não precisam da configuração de Sprite nem devem ser incluídos em prefabs ou referenciados pelo runtime.

Os fundos `skill_tree_earth_640x360.png` e `skill_tree_earth_1920x1080.png` são opacos. Escolher uma variante conforme a composição e a resolução; evitar usar ambas na mesma tela. Preservar a proporção 16:9, sem expandir por nove fatias.

## 3. Catálogo dos componentes gerais

Os caminhos desta seção são relativos a `Sprites/`.

| Pasta | Quantidade | Arquivos e finalidade |
| --- | --- | --- |
| `buttons/` | 9 | `button_normal`, `button_focus`, `button_pressed`, `button_disabled`: estados gerais; `button_action`: ação principal; `button_danger`: ação destrutiva; `icon_button`, `icon_button_focus`, `icon_button_disabled`: botões com ícone |
| `controls/` | 4 | `checkbox_on/off` e `toggle_on/off`: opções de configuração |
| `decor/` | 2 | `moss_corner` e `seedling`: acabamento visual |
| `icons/` | 22 | `ant`, `crown`, `egg`, `larva`, `pupa`, `leaf`, `mushroom`, `crate`, `shovel`, `warning`, `lock`, `gear`, `speaker`, `play`, `pause`, `plus`, `minus`, `close` e quatro `arrow_*` |
| `indicators/` | 1 | `badge`: base de contador ou indicador de aviso; número composto como texto |
| `panels/` | 7 | `hud_card`, `panel_plain`, `panel_moss`, `panel_header`, `notification`, `tooltip`, `tooltip_pointer` |
| `progress/` | 3 | `progress_track`, `progress_green`, `progress_amber` |
| `scrollbar/` | 2 | `scrollbar_track`, `scrollbar_thumb` |
| `sliders/` | 3 | `slider_track`, `slider_fill`, `slider_handle` |
| `tabs/` | 2 | `tab_active`, `tab_inactive` |

### Cuidados com estados e tamanho

As imagens de um mesmo controle nem sempre têm dimensões iguais:

- `button_normal`: 54×24; `button_focus`, `button_pressed` e `button_disabled`: 52×24.
- `button_action`: 54×26; `button_danger`: 54×24.
- `icon_button`: 38×36; `icon_button_focus`: 40×36.
- `checkbox_off`: 32×32; `checkbox_on`: 34×32.
- `toggle_off`: 48×26; `toggle_on`: 50×26.

Manter o RectTransform e a área clicável estáveis entre os estados. Conferir alinhamento, bordas e margens; usar ajustes de composição ou bordas específicos caso a troca produza deslocamento aparente. O tamanho do PNG não determina sozinho o tamanho final do controle.

Os botões de ação e perigo não possuem todos os estados dedicados no pacote. Definir o tratamento de foco, pressionado e desabilitado dessas variantes durante a preparação dos componentes.

## 4. Conjunto da árvore do formigueiro

Base: `Sprites/FormigueiroSkillTree/Textures/`.

| Pasta | Finalidade |
| --- | --- |
| `Backgrounds/` | Fundos de terra em 640×360 e 1920×1080 |
| `Buttons/` | `button_back` e `button_unlock` |
| `Tabs/` | Estados ativo e inativo das abas |
| `Panels/` | `skill_details_panel`: fundo do painel de detalhes |
| `Nodes/` | Losangos adquirido, disponível, bloqueado, raiz e variantes de seleção |
| `Icons/` | Ícones C0–C4, B0–B3, D0–D2, R0–R1 e HUB em dois tamanhos |
| `IconsLocked/` | Versões bloqueadas dos 14 ícones de habilidades |
| `Connections/` | Segmentos e marcadores genéricos adquirido, disponível e bloqueado |
| `ConnectionsLayout/` | 16 conexões diagonais preparadas para o arranjo da referência |
| `Effects/` | `selection_ring`: sobreposição de seleção |
| `Decor/` | Musgo para acabamento |
| `UI/` | Folha, cadeado e pausa, incluindo variantes pequenas |

### Correspondência com o design

| Prefixo | Ramo | Sequência de referência |
| --- | --- | --- |
| C | Cultivo | C0 → C1 → C2 e C3 → C4, que exige ambos |
| B | Berçário | B0 → B1 e B2 → B3, que exige ambos |
| D | Depósito | D0 → D1 → D2 |
| R | Resíduos | R0 → R1 |
| HUB | Centro visual da colônia | Não presumir custo ou compra apenas pela existência do sprite |

Fonte dos requisitos: [Mapa da árvore do formigueiro](../../../../Docs/01_Design/17_Mapa_da_arvore_do_formigueiro.md). A geometria de uma conexão não define a regra de desbloqueio.

### Composição e estados

Compor cada nó com base, ícone, identificação textual e seleção como camadas independentes. Selecionar uma habilidade não deve apagar a informação de que ela está adquirida ou bloqueada. Escolher entre a variante completa de seleção e a sobreposição, evitando duplicar o destaque.

As conexões em `ConnectionsLayout/` já têm trajetos e cores específicos. Não tratá-las como conexões capazes de mudar de estado automaticamente. Antes da montagem funcional, escolher entre segmentos genéricos compostos conforme os dados ou variantes de cada trajeto. Custos, requisitos e efeitos vêm do gameplay, não dos nomes das imagens.

Há peças com funções repetidas entre o kit geral e o kit da árvore, como abas, folha, cadeado e pausa. Escolher a variante adequada ao contexto e manter uma referência explícita; não excluir arquivos apenas porque têm nomes semelhantes.

## 5. Fontes

O ZIP contém `PixelifySans-Regular.ttf`, `PixelifySans-Bold.ttf`, `Caracteres-PTBR.txt`, `OFL.txt`, instruções, prévia, script auxiliar e relatório de cobertura.

1. Extrair os dois TTFs, a lista de caracteres e a licença para uma subpasta autoral, como `Fontes/PixelifySans/`.
2. Gerar os Font Assets usados pelo TextMeshPro incluindo os caracteres do arquivo PT-BR. Não limitar o atlas a ASCII.
3. Atribuir Regular aos textos e Bold aos títulos conforme a composição.
4. Validar a fonte no tamanho real de uso, com escala coerente com os sprites.
5. Conferir: `Colônia · Evolução · Berçário · Resíduos · Configurações · Próxima cria · 0,25 N · +2,40 N/s`.

O relatório incluído informa 125 caracteres testados, sem ausências nos TTFs. Isso não valida o atlas nem a exibição no Editor. Escolher renderização e material do Font Asset após conferir a nitidez; o filtro dos sprites não resolve a renderização do texto.

Não executar `prepare_fonts.py` para importar: os TTFs já estão no pacote. Preservar a licença junto dos arquivos da fonte. Prévia, ZIP, relatório e script são materiais de apoio, sem referências necessárias no runtime.

## 6. Referências de telas

| Arquivo | Dimensão | Orientação para montagem |
| --- | --- | --- |
| `05-hud-partida.png` | 640×360 | Cartões superiores; navegação lateral; controles de tempo e menu nas bordas inferiores |
| `07-selecao-pinceis.png` | 640×360 | Lista de pincéis ao lado da navegação, com tarifa e disponibilidade |
| `17-residuos.png` | 640×360 | Detalhes do ambiente selecionado na lateral direita |
| `22-arvore-formigueiro-radial.png` | 1920×1080 | Árvore em tela própria, abas, legenda, detalhes e compra |
| `25-menu-pausa.png` | 640×360 | Modal central sobre a partida escurecida |

Usar as imagens para hierarquia e linguagem visual. Textos, números, atalhos e estados ilustrados precisam ser ligados às regras reais. Não incorporar os textos das referências como imagens.

## 7. Ordem de montagem proposta

Proposta para a próxima etapa; este guia não executa importação, configura componentes nem modifica a cena.

1. **Base reutilizável:** importar sprites e fontes; preparar botões, cartões, painéis, barras, tooltip e estados. Validar escala e acentos em uma composição de teste.
2. **HUD do protótipo:** apresentar nutrientes, população atual e situação da rainha com dados reais; manter métricas técnicas em painel de desenvolvimento separado.
3. **Pincéis:** apresentar primeiro Cavar, Construir, Sala da rainha e Sala de nutrientes, já existentes no protótipo; mostrar seleção e raio.
4. **Pausa:** implementar navegação, congelamento da simulação e bloqueio das ações de gameplay; conectar apenas ações de sessão suportadas.
5. **Painéis de seleção:** introduzir informações por ambiente conforme os sistemas correspondentes estiverem disponíveis.
6. **Árvore do formigueiro:** montar nós e conexões a partir dos dados de requisitos, custo, compra e persistência.
7. **Telas complementares:** avançar para menu principal, configurações, créditos, game over e árvore da rainha conforme conteúdo e regras forem definidos.

### Integração com o protótipo

- A UI atual está em `AntFarmGame.OnGUI`, em `Assets/Project/Scripts/Runtime/AntFarm/AntFarmGame.cs`.
- A proposta é usar uGUI para as telas, com apresentação separada da simulação. O pacote `com.unity.ugui` já consta no projeto.
- Substituir o bloqueio de mouse baseado no retângulo fixo `_hudRect` por um bloqueio que acompanhe painéis, controles e modais. Validar também câmera, scroll e atalhos durante a interação com a UI.
- Preservar inicialmente as velocidades implementadas, **1×, 3× e 8×**. A referência mostra 1×, 2× e 3×; mudar isso exige uma decisão de gameplay.
- Produção em N/s, adultas por tipo, reservas, ovos, larvas e pupas devem entrar quando houver dados reais correspondentes. Não usar números ilustrativos em uma tela funcional.
- Como ponto de partida visual, testar composição em 640×360 e ampliações inteiras. O comportamento em outras proporções e resoluções ainda precisa ser validado.

## 8. Pendências antes das telas correspondentes

| Pendência | Encaminhamento |
| --- | --- |
| Pausa ao abrir árvores e painéis de gestão | A imagem da árvore mostra tempo pausado, mas o design ainda registra a regra em aberto |
| Ações de recuperação no menu de pausa | A referência inclui retomar ponto anterior e reiniciar; o documento de menus coloca essas ações no game over |
| Árvore da rainha e classes | Não há um conjunto equivalente identificado neste pacote; avaliar reutilização das bases e produção dos ícones necessários |
| Estados faltantes de ações especiais | Definir foco, pressionado e indisponibilidade para botões de ação e perigo |
| Escala e legibilidade | Validar textos, ícones, recortes e área útil do mundo nas resoluções alvo |

Fontes de comportamento: [Menus e configurações](../../../../Docs/02_World_Art_UI/33_Menus_fluxo_de_telas_e_configuracoes.md) e [Interface durante a partida](../../../../Docs/02_World_Art_UI/34_Interface_durante_a_partida.md).

## 9. Verificação da primeira integração

- Sprites nítidos, sem redução involuntária de resolução ou bordas deformadas.
- Texto PT-BR legível, sem glifos ausentes ou cortes.
- Troca de estado sem deslocar controles ou alterar a área clicável.
- Nenhuma pintura, zoom ou ação de gameplay disparada ao operar painéis e modais.
- Pausa e retorno sem avanço indevido da simulação.
- Valores do HUD correspondentes ao estado real da colônia.
- Referências de tela e materiais de apoio sem dependências no runtime.
- Compilação e comportamento confirmados no Unity Editor, sem gerar build distribuível.

**Estado deste documento:** inventário e planejamento baseados em inspeção dos arquivos e do código. Importação, configuração de bordas, geração dos Font Assets e montagem das telas continuam pendentes de execução e validação na Unity.
