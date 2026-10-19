# Guia — Menu principal

Cena `Assets/Project/Scenes/MainMenu.unity` (depois da splash no build). Montada por **Incrementant/Setup/Criar cena MainMenu** (`Editor/UI/AntFarmHudSetup.MainMenu.cs`); idempotente: recria o canvas `MenuPrincipal` e reaproveita o mundo.

## Como funciona

- **Fundo = vitrine.** A cena tem um `AntFarmGame` com `Showcase` ligado, mapa procedural de seed fixa (1234), sem ataques de cupins nem morte das formigas. Em modo vitrine o jogo ignora input, câmera do jogador e cronômetro. `MenuShowcase.Prepare` (chamado em `Regenerate`) escava o formigueiro pronto (túneis e salas em volta do ninho), marca as salas (rainha, berçário, nutrientes), dá 30 nutrientes à rainha e espalha 33 formigas de vários papéis pelas galerias (`AntColony.SpawnAt`). `MenuShowcase.Tick` marca um trecho novo de escavação a cada ~18 s (só quando o anterior terminou), então as escavadoras seguem trabalhando; as coletoras colhem a vegetação sozinhas. Enquadramento: `_showcaseHalfHeight` e `_showcaseNestOnScreen` em `AntFarmGame`.
- **Botões** (`MainMenu`): Iniciar nova colônia (`GameLaunch.RequestNew` + carrega `AntFarm`), Continuar (só ativo com `quicksave.sav`; `GameLaunch.RequestContinue`; o `AntFarmGame` da partida lê o save em `Awake` antes de gerar o mundo e cai em colônia nova se ele for incompatível), Configurações (mesma `SettingsScreen` do ESC), Sobre o jogo (painel simples, créditos a definir) e Sair (para o Play no Editor). O menu de pausa da partida tem "Menu principal" (`PauseMenu`, carrega `MainMenu` sem salvar).
- **Splash da Unity** desligado em Player Settings (Splash Screen → Show Splash Screen). A splash da Fábula usa a cena `Splash` descrita abaixo.

## Como mexer

- Layout do menu: `BuildMainMenu` (referência 1920x1080, mesmo escalador do HUD). Fonte: a mesma do HUD (Press Start 2P). O logo é `Art/UI/Logo_Fabula_branco.png`, versão branca com fundo transparente gerada do logotipo oficial `Logotipo_FABULA_game-sdio_27052022.jpg` (o JPG tem fundo branco e não serve sobre o fundo escuro).
- Formigueiro do fundo: tabelas `Rooms`, `Tunnels` e `DigTargets` em `MenuShowcase` (coordenadas em células a partir da entrada do ninho).
- Mudou campo serializado de `AntFarmGame`/`MainMenu`? Rode o setup de novo.

## Pendências

- Confirmação ao iniciar nova colônia havendo save (proposta do design); hoje iniciar não apaga o save, só salvar por cima.

## Splash

`Scenes/Splash.unity` (montada por **Incrementant/Setup/Criar cena Splash**) é a primeira cena do build: mostra `Art/UI/splash-fabula-fundo.png` preenchendo a tela e `Art/UI/splash-fabula-logo.png` como camada transparente central, ajustada para caber inteira sem corte. A arte original `splash-fabula-terra.png` é preservada. As duas camadas têm fade de entrada (0,7 s), pausa (1,8 s), dissolução do logotipo (4,5 s, ver abaixo) e fade de saída do fundo (0,7 s), e então carregam o `MainMenu`. Qualquer tecla ou clique, depois de 0,3 s, pula direto para o fade de saída. Os tempos e a cena seguinte são campos do `SplashScreen`; `_fade` referencia o fundo e `_logo` o texto. `SplashSceneSetup.UpdateLayers()` atualiza só as camadas na cena existente, preservando câmera e tempos. Rodar o setup do menu principal mantém o splash em primeiro no build. Abrir o `MainMenu` direto no Editor não passa pelo splash.

### Dissolução do logotipo (VFX Graph)

Depois da pausa, o logotipo se desfaz em blocos de terra que caem, da esquerda para a direita e de cima para baixo. Peças:

- **`Assets/Project/VFX/SplashLogo/`**: `LogoDissolve.vfx` (grafo) e três texturas de float, com um texel por bloco: `LogoPositions` (posição em unidades de mundo), `LogoColors` (cor média da célula, em linear) e `LogoDelays` (atraso, em segundos, até o bloco soltar). Tudo é gerado, não editado à mão.
- **`SplashLogoDissolve`** (runtime): dispara o VFX (`Play`) e escala o objeto para casar com o logotipo da UI (que usa `FitInParent`), qualquer que seja a proporção da tela. `SplashScreen._dissolve` o referencia; sem ele (campo vazio) o splash volta ao fade simples.
- **Grafo**: spawner de burst único (um bloco por texel) → Initialize (`Set Position/Color/Lifetime from Map`, modo Sequential, id da partícula = texel; vida = atraso + 1,7 s) → Update (Gravity e Turbulence, ambas ligadas a um *gate* `saturate((idade − vida + queda) × 1000)`, que vale 0 até o atraso passar, deixando o bloco parado no logotipo) → Output Quad opaco. Idade negativa não foi usada de propósito: o primeiro teste com ela matava as partículas na hora.
- **Câmera e canvas**: o VFX é objeto de mundo, então o canvas do splash passou de *Overlay* para *Screen Space – Camera* (distância 100) e o VFX fica em z = 10, na frente; o culling mask da câmera deixou de ser 0 e o orthographic size é 5 (altura de mundo do logotipo = 10, ver `SplashLogoDissolve.BakedHeight`). Por isso o VFX é opaco: o canvas atrás é descartado pelo teste de profundidade.
- **Como regerar**: **Incrementant/Setup/Splash - dissolução da logo** assa as texturas a partir de `splash-fabula-logo.png`, reescreve o `.vfx` (mesmo caminho, GUID preservado) e liga câmera, canvas, objeto `LogoDissolve` e a referência do `SplashScreen`. Parâmetros (tamanho do bloco em pixels, onda, queda, turbulência) são constantes no topo de `SplashDissolveSetup`. `Criar cena Splash` e `UpdateLayers` religam a dissolução se o `.vfx` já existir.
- **Sprite do logotipo**: some em 0,2 s por baixo dos blocos quando a dissolução começa (`_logoHandoffSeconds`); os blocos são uma versão pixelada do mesmo logotipo.
- **Limitações**: VFX Graph exige compute shader (não roda em WebGL nem em celulares antigos). Não há pilha de terra no chão; os blocos saem da tela. Validado no Editor (simulação do VFX em Edit Mode e Play Mode), não em build.
