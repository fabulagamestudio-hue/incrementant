# Catálogo — UI (botões, painéis, HUD, telas)

Valores são pontos de partida a afinar em Play Mode. Todos usam `Time.unscaledDeltaTime`, respeitam `Vfx` (coluna **Reduzido/Off**) e terminam em estado exato (escala 1, alfa 1).

## 1. Botões (`ButtonJuice`, aplicado por `AttachAll(transform.root)`)

Tipos de botão no projeto: ícone/retangular com `SpriteSwap`, pincéis e abas com `Transition.None` (destaque por código), toggles e sliders. O componente anima o `RectTransform` do botão (ou de um filho "visual") e **não troca sprite nem `transition`**.

| Estado | Efeito | Parâmetros iniciais | Reduzido / Off |
|---|---|---|---|
| Hover entra | Escala sobe e leve brilho de cor | 1,0→1,06 em 0,10 s `OutQuad`; cor ×1,08 | só cor / nada |
| Hover sai | Volta | 0,12 s `OutQuad` | idem |
| Pressionar | Afunda (squash) | escala (1,05, 0,94) em 0,06 s; desloca −1 px "da grade" abaixo | escala 0,97 / nada |
| Soltar (clique válido) | Retorno elástico + pulso de anel | →1 em 0,22 s `OutBack` (overshoot 1,1); anel expansivo no ponto do clique (opcional) | só retorno / nada |
| Clique negado (sem custo, bloqueado) | Tremidinha horizontal + cor de erro curta | ±3 px, 3 ciclos em 0,18 s; cor vermelho-terra 0,15 s; par com `SfxId.UiDeny` | só cor / nada |
| Desabilitado | Sem hover, sem pressão; cursor normal | — | — |
| Foco por teclado/gamepad | Mesmo realce do hover + contorno | — | — |
| Alternar (toggle) | "Pop" do marcador ao ligar | 1→1,25→1 em 0,15 s | nada |

Regras:
- Não acumule: se o hover estiver em andamento e vier um clique, parta do valor atual (não do repouso).
- Ao desabilitar/desativar o objeto no meio da animação, volte à escala 1 em `OnDisable` (senão o botão fica "preso" em 1,06).
- Ícone dentro do botão pode ter micro-movimento próprio (ex.: balanço de 2° no hover do sino de recolhimento).
- O sino (`_recallButton`) merece mais: balanço pendular em 3 ciclos ao ligar, em sincronia com `EmergencyBell`.
- Pixel art: prefira escala que resulte em tamanho de tela inteiro quando em repouso; durante a animação, pequenas frações são aceitáveis.

## 2. Painéis e telas (`PanelAnimator`)

Hoje aparecem/somem em `SetActive`. Padrão proposto com `CanvasGroup`:

| Elemento | Entrada | Saída |
|---|---|---|
| Menu do ESC / Configurações | fundo escurece 0→alvo em 0,15 s; painel sobe de 12 px e escala 0,96→1 `OutBack` 0,2 s | inverso em 0,12 s |
| Tela Evolução (tela cheia) | fade 0,18 s + nós aparecem em cascata (atraso 12 ms por nó, máx. 0,3 s) | fade 0,12 s |
| Game over | fundo escurece lento (0,8 s), título desce com `OutBounce` leve, botões surgem em sequência | — |
| Submenus (Pintar, Ambientes, Rainha) | deslizar 8 px + fade 0,12 s | fade 0,08 s |
| Alerta de cupins | desliza do topo 0,2 s `OutBack`; pulso vermelho-terra 2x enquanto a contagem é iminente | sobe e some 0,2 s |
| Dica (tooltip) | fade 0,08 s | imediato |
| Pop-ups do tutorial | pop 0,9→1 `OutBack` 0,2 s | fade 0,1 s |

Regras: interatividade só liga ao fim da entrada (`CanvasGroup.interactable`), mas **não bloqueie o ESC** (a ordem de camadas em `AntFarmHud.HandleEscape` manda). Com `Vfx.Off`, estado final imediato.

## 3. Contadores do HUD (`Pulse`)

Os cartões Nutrientes / Formigas / Rainha mudam de texto sem reação. Dopamina barata:

| Evento | Efeito |
|---|---|
| Nutrientes sobem | número pulsa 1→1,12→1 (0,15 s) + cor dourada 0,3 s; ícone dá um "tic" |
| Nutrientes descem (compra) | número fica vermelho-terra 0,2 s e "afunda" 2 px; custo sai flutuando (ver 4) |
| Formigas nascem | contador pulsa + leve brilho do cartão; ícone da formiga salta 4 px |
| Capacidade cheia | cor de aviso suave, **sem** pulso repetido (evita ruído) |
| Rainha (estado muda) | cartão pisca uma vez |

Agregação: no máximo um pulso por contador a cada 0,25 s (a 8x os valores mudam várias vezes por segundo; o texto já atualiza 4x/s, alinhe o pulso a isso).
Contagem animada ("rolling number") opcional: interpole o valor mostrado até o real em ≤0,25 s; nunca mostre valor maior que o real.

## 4. Números flutuantes e rótulos (`FloatingText`)

Canvas Overlay próprio, ordem abaixo do HUD (como `TermiteMarkers`), pool de `Text` (Press Start 2P, 16 px, contorno de 1 px escuro para legibilidade sobre o terreno).

| Uso | Texto | Cor | Movimento |
|---|---|---|---|
| Entrega à rainha / produção | "+12" agregado por sala em janela de 0,4–0,6 s | dourado | sobe 24 px em 0,8 s, fade nos últimos 30% |
| Compra | "−40" saindo do contador de nutrientes; nó ganha "ok" | vermelho-terra | cai 16 px |
| Dano à formiga / cupim | "−1" | vermelho / branco | arco curto com gravidade, 0,5 s |
| Ovo posto / nascimento | "+1" com ícone | verde-claro | sobe 16 px |
| Recusa | "!" ou "FALTA" localizado | cinza-avermelhado | tremidinha, 0,4 s |

Regras: teto de 12 simultâneos; os mais antigos terminam antes (fade rápido) ao estourar; nunca sobre o cursor; use `Loc.T` para qualquer texto de palavra (chaves novas em `LocalizationCatalog`, caixa mista); números não precisam de tradução.

## 5. Árvore de Evolução (`SkillNodeWidget` / `SkillTreeScreen`)

| Momento | Efeito |
|---|---|
| Hover em nó | anel pulsa levemente; ligações do nó acendem |
| Selecionar | anel entra com `OutBack` (0,15 s) |
| Desbloquear (compra) | losango "estoura" 1→1,3→1 (0,3 s), faíscas douradas em anel, ligações do pai ao filho **acendem em sequência** (onda de 0,05 s por segmento), cadeado some com giro rápido; `Buy` já toca |
| Nó novo fica disponível | brilho respirando lento (0,6 Hz) até ser visto |
| Compra recusada | nó treme + `UiDeny` (já existe) |
| Nova classe liberada | momento especial: silhueta da formiga em destaque, nome do papel, 1,5 s, dispensável (ver Marcos) |

## 6. Marcos (raros, memoráveis)

Usar com parcimônia; sempre dispensáveis e curtos (≤2 s) e **sem pausar o jogo**:
- **Nova classe liberada:** faixa no topo com a silhueta e o nome, confete de pixels da cor do papel, pulso no botão Rainha.
- **Ataque de cupins repelido:** carimbo "salvos" (localizado), pulso verde no alerta, pequena chuva de pixels dourados.
- **Primeiro nascimento de cada papel / marcos de população (10, 25, 50, 100):** pop no contador + confete discreto.
- **Game over:** ver painéis; sem celebração, mas com cuidado (escurecer, sino grave já existe em `GameOver`).

## 7. Menu principal e splash

Botões usam `ButtonJuice` (mesmo `AttachAll`). Título com flutuação lenta (±3 px, 4 s) e formigas do `MenuShowcase` com `WorldFx` ligado (poeira de escavação, folhas) para o menu parecer vivo mesmo parado. Splash já tem VFX Graph; só garantir que respeita `Vfx` quando a opção existir antes do primeiro jogo (padrão Completo).
