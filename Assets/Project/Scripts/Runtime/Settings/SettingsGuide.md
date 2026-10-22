# Configurações do jogo — guia

Como as configurações funcionam e como ligar sistemas novos nelas. Onde estão os arquivos: [Runtime/README.md](../README.md). A tela (menu do ESC) está em [HudGuide.md](../UI/HudGuide.md).

## Como funciona

- **Dados.** `GameSettingsData` guarda só valores: volumes geral, música e efeitos sonoros (0 a 1), nível de efeitos visuais (`VfxLevel`: Completo, Reduzido, Desligado), tela cheia, resolução (0x0 = nativa), VSync, limite de FPS (30, 60, 120, 144 ou sem limite; só vale com VSync desligado) e se o canto mostra FPS e tempo. `BlockBuildOnRooms` (padrão ligado) é a trava de segurança "Bloquear obra em salas": com ela, o pincel de construir do `AntFarmGame` não marca células pintadas como qualquer sala (`PixelGrid.IsAnyRoom`); apagar a marca continua livre e marcas já existentes não são removidas. `ShowRooms` (padrão ligado) mostra ou esconde a pintura das salas no terreno; não tem linha na tela de configurações desde 2026-10-22: quem a troca é o botão "mostrar ambientes" da barra inferior do HUD, que grava o arquivo a cada clique, e quem aplica é o `AntFarmGame`, ver [HudGuide](../UI/HudGuide.md). `Clamp` corrige valores fora da faixa; `FromJson` devolve os padrões para texto vazio ou inválido e mantém o padrão de campos que o arquivo não tem.
- **Serviço.** `GameSettings` é estático de propósito: as configurações valem em qualquer cena. Ciclo de vida: `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` lê `Application.persistentDataPath/settings.json` e aplica antes da primeira cena (no Windows, `%USERPROFILE%/AppData/LocalLow/Fabula/Incrementant/settings.json`). Com o domain reload desligado, o mesmo método relê o arquivo e solta ouvintes antigos a cada Play.
- **Mudança.** Quem altera `GameSettings.Current` chama `GameSettings.NotifyChanged()`, que corrige, aplica e dispara `GameSettings.Changed`. A tela grava o arquivo (`Save`, temporário + troca, como o save da partida) ao sair dela e ao sair do jogo, não a cada arrasto de slider.
- **Idioma.** `Language` (`pt-BR` ou `en`) vai para `Loc.SetLanguage` em `Apply`; a tela tem a linha IDIOMA. Ver [TextsGuide.md](../Texts/TextsGuide.md).
- **O que é aplicado aqui.** Volume geral em `AudioListener.volume`; VSync em `QualitySettings.vSyncCount`; limite em `Application.targetFrameRate` (-1 com VSync ou sem limite); modo e resolução com `Screen.SetResolution`, só quando mudam e **só fora do Editor** (a Game view ignora; mexer nisso alteraria a janela do Editor).
- **Bloom.** `Bloom` (padrão ligado, linha BRILHO (BLOOM) da tela) não é aplicado em `GameSettings`: o `PostProcessSettingsBinder`, no Volume global `PostProcess` de cada cena com o mundo, ouve `GameSettings.Changed` e liga ou desliga o efeito Bloom na cópia do perfil (`Volume.profile`), nunca no asset `Assets/Project/Settings/PostProcessProfile.asset`. Vinheta e cor quente não dependem da opção. Desde 2026-10-21 o bloom tem limiar acima do branco e só pega o que emite em HDR: hoje, o cogumelo luminoso. O perfil e o Volume são montados por `Incrementant/Setup/Configurar pós-processamento` (também chamado por `AntFarmSceneSetup.BuildWorld`); os valores e o motivo de cada um estão no [documento 40, etapa 7](../../../../../Docs/02_World_Art_UI/40_Direcao_de_arte_unificada_e_terreno_iluminado.md).
- **O que fica para cada sistema.** Efeitos sonoros já seguem o slider (`GameAudio`, ver [AudioGuide.md](../Audio/AudioGuide.md)); música e efeitos visuais não têm sistema ainda e os valores existem para eles lerem.

## Como ligar SFX, música e VFX

- **Efeito sonoro:** volume final da fonte = volume do próprio som × `GameSettings.SfxVolume` (o geral já entra pelo `AudioListener`). Fontes que tocam por muito tempo se inscrevem em `GameSettings.Changed` (em `OnEnable`/`OnDisable`) para acompanhar o slider; tiros curtos podem ler o valor na hora de tocar.
- **Música:** igual, com `GameSettings.MusicVolume`.
- **Efeitos visuais:** consulte `GameSettings.Vfx` antes de criar partículas ou tremores: `Off` não cria, `Reduced` diminui quantidade/intensidade. Defina por efeito o que "reduzido" significa.
- Se o áudio passar a usar um `AudioMixer`, troque a aplicação dos volumes para parâmetros expostos do mixer (em decibéis) dentro de `GameSettings`, sem mudar a API de leitura.

## O que evitar

- Não grave o arquivo em caminhos quentes (`Update`, arrasto de slider); use `Save` ao fechar a tela.
- Opção nova: campo em `GameSettingsData` com valor padrão, regra em `Clamp`, aplicação em `GameSettings` (se for global) e linha nova na tela (`SettingsScreen` + `BuildSettingsScreen` no setup do HUD). Teste em `GameSettingsTests`.
