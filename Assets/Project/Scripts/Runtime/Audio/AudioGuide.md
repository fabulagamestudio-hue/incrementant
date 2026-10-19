# Efeitos sonoros — guia

Como os SFX funcionam e como ligar um som novo. Onde estão os arquivos: [Runtime/README.md](../README.md). Os volumes vêm das configurações: [SettingsGuide.md](../Settings/SettingsGuide.md).

## Como funciona

- **Sem arquivos de áudio.** `SfxSynth` sintetiza cada `SfxId` em código (tons com envelope, ruído filtrado, arpejos), mono a 32 kHz, normalizado a 0,9 e determinístico. `GameAudio` cria o `AudioClip` na primeira vez que o som toca.
- **Serviço.** `GameAudio` (estático) toca por um conjunto fixo de 20 `AudioSource` num objeto `GameAudio` que sobrevive às cenas (`DontDestroyOnLoad`). Sem fonte livre, rouba a que toca há mais tempo. Cada som tem uma regra (volume, intervalo mínimo, variação de tom) em `GameAudio.BuildRules`; sons que a colônia dispara várias vezes por segundo (escavar, despejar) têm intervalo mínimo e volume baixo.
- **Slider.** Volume geral = `AudioListener.volume` (vale na hora, até no que já toca). Volume de efeitos = `GameSettings.SfxVolume`, aplicado ao tocar e **reaplicado a cada `GameSettings.Changed`** nos sons em andamento e na chuva. Arrastar os sliders de geral e efeitos toca um tique (`UiTick`) para ouvir o nível. O slider de música é aplicado pelo `MusicPlayer`.
- **Chuva.** `SetLoop(SfxId.Rain, nível)` liga/regula o laço com fade de 0,8 s; `AntFarmGame` chama conforme o clima e zera ao sair da cena.
- **Colônia.** A simulação não toca nada: `AntColony.Sfx` (`AntColony.Audio.cs`) avisa o que aconteceu e `AntFarmGame` escuta (`PlayColonySfx`), exceto no fundo mudo do menu.
- **Música.** `MusicPlayer` (estático) tem dois `AudioSource` num objeto `MusicPlayer` (`DontDestroyOnLoad`) que se revezam: pedir outra faixa faz crossfade de 2,5 s (a atual some enquanto a nova entra), sem corte; pedir a que já toca não faz nada. Cada cena declara a sua com o componente `SceneMusic` (faixa + duração do fade), posto pelos setups: menu = `Primal.ogg`, partida = `Lost in the Jungle.ogg` (pasta `Assets/Tribal Jungle Music Free Pack`, importadas como Streaming). Trocar de faixa: mude `MenuMusicPath`/`GameMusicPath` em `AntFarmHudSetup` e remonte menu e HUD. O fade usa tempo não escalado, então não para na pausa.
- **Botões.** `ButtonSfx.AttachAll(raiz)` põe o clique em todos os botões da tela (feito em `AntFarmHud.Awake` e `MainMenu.Awake`).

## Onde cada som toca

| Som | Origem |
|---|---|
| `Dig`, `Drop`, `Build`, `Plant` | escavar/pegar, despejar (também canteiro, orvalho, resíduo), obra, plantar (`AntColony*.cs`) |
| `EggLaid`, `AntBorn`, `AntDie`, `Drowned` | postura (relógio e clique na rainha), nascimento, velhice/combate, afogamento |
| `RaidWarning`, `RaidStart`, `RaidEnd`, `TermiteHit`, `TermiteKilled`, `AntHurt`, `Crumble`, `GameOver` | `AntColony.Raids.cs` |
| `Buy` | `AntColony.TryBuy`; `UiDeny` quando a compra é recusada (`SkillTreeScreen`) |
| `Mark`/`Unmark` | início do traço do pincel (`AntFarmGame.HandleInput`) |
| `UiClick`, `UiTick`, `UiDeny` | botões, sliders, clique na rainha recusado |
| `Rain` | clima (`AntFarmGame.ApplyWeatherSound`) |
| `EmergencyBell` | sino do HUD ao ligar ou desligar o recolhimento (`AntFarmHud.ToggleRecall`). Sino de aldeia tocado por corda: 11 batidas de badalo (lados alternados, tempo e força levemente irregulares) com parciais inarmônicos e cauda de ~1,3 s (`SfxSynth.EmergencyBell` / `Buffer.BellStrike`) |

## Como adicionar um som

1. Valor novo em `SfxId` (antes de `Count`).
2. `case` em `SfxSynth.Render` e duração em `SfxSynth.Seconds`.
3. Regra em `GameAudio.BuildRules`.
4. Dispare com `GameAudio.Play(id)` (UI/jogo) ou `Emit(id)` (colônia). O teste `SfxSynthTests` cobre faixa, determinismo e fim sem estalo.

## O que evitar

- Não toque som direto da simulação; use `Emit`.
- Não crie `AudioSource` soltos: passam por cima do slider.
- Se o áudio migrar para `AudioMixer`, troque só a aplicação dos volumes (veja o SettingsGuide).

## Pendências

- Sem posição no espaço: tudo toca centrado, sem atenuar por distância da câmera.
- Sem sons de tutorial/pop-up nem de passos.
- Não ouvido em Play Mode por mim: os níveis relativos (regras de volume) pedem ajuste de ouvido. O `EmergencyBell` foi só renderizado fora da Unity (pico, fim sem estalo, envelope); timbre (`baseHz` 760, tabela `BellPartials`) e volume pedem ouvido.
