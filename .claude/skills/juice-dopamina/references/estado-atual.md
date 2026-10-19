# Estado atual do feedback (análise de 2026-10-18)

Levantado a partir do código em `Assets/Project/Scripts`. Reconfira antes de depender de um item: o projeto muda rápido.

## O que já existe

| Área | O que há | Onde |
|---|---|---|
| Som de UI | Clique em todo botão (`UiClick`), tique de slider, negado (`UiDeny`), compra (`Buy`), sino | `Audio/ButtonSfx.cs` (`AttachAll`), `GameAudio`, `SfxId` |
| Gancho da simulação | `AntColony.Sfx` (`Action<SfxId>`) emite 30+ fatos (escavar, despejar, obra, plantar, ovo, nascer, morrer, afogar, ataque, dano, game over…). **Sem posição.** | `Colony/AntColony.Audio.cs`; `AntFarmGame.PlayColonySfx` escuta |
| Botões | uGUI `Button`. `SpriteSwap` (só `pressedSprite`=`*_focus` e `disabledSprite`) ou `ColorTint`/`None` com destaque por código. **Sem hover animado, sem escala, sem retorno de clique.** | `Editor/UI/AntFarmHudSetup*.cs` (linhas ~445, 710, 1252…) |
| Destaque de pincel/aba | `AntFarmHud` troca sprites `*_focus` todo frame | `Runtime/UI/AntFarmHud.cs` |
| Dicas | `HudTooltips` (0,35 s, segue o mouse) | `Runtime/UI/HudTooltips.cs` |
| Alerta de cupins | `_raidAlert` aparece/some sem animação; 6 s de aviso de fim | `AntFarmHud` |
| Marcas dos cupins | Círculo vermelho + seta na borda, canvas Overlay próprio em código (ordem -1), pool | `UI/TermiteMarkers.cs` — **molde de como criar overlay por código** |
| Formigas | `SpriteRenderer` por formiga, patas animadas por distância, giro suave, pixel carregado; cupim pisca `_termiteHitColor` ao levar mordida (`LastHitTime`) | `AntFarm/AntSwarmView.cs` |
| Crias e cultivo | `BroodView` (ovo/larva/pupa), `CultivationView` (broto, água, nutrientes brotando, recebe `time`) | `AntFarm/` |
| Câmera | `MapCamera`: pan, zoom, limitador, `MoveTo`/`StepTransition` com `unscaledDeltaTime` | `AntFarm/MapCamera.cs` |
| Tutorial | Pop-ups, destaques, contorno de câmaras | `Tutorial/TutorialDirector.cs` |
| Splash | Fade e dissolução da logo em blocos de terra com **VFX Graph** | `Menu/SplashLogoDissolve.cs`, `VFX/SplashLogo` |
| Cursor | Cursor de dedo no mapa quando o pincel é "Dedo" | `AntFarmHud`, `UiCursorImporter` |
| Configuração | `VfxLevel { Full, Reduced, Off }` salvo e editável em Configurações | `Settings/GameSettingsData.cs`, `SettingsScreen` |
| Pacotes | `com.unity.modules.particlesystem`, VFX Graph, URP 2D Runtime, Input System, Localization, TextMeshPro (a UI usa `Text` legado) | `Packages/manifest.json` |

## Lacunas (o que a análise encontrou)

1. **`VfxLevel` não é lido por nenhum efeito.** A opção existe na tela de Configurações, mas nada a consulta. Primeira dívida: criar o ponto único de leitura.
2. **Nenhuma partícula no mundo.** Escavar, despejar, obra, nascimento, morte, dano e chuva sobre o formigueiro são só som (ou só mudança de textura).
3. **Nenhum botão tem hover/clique animado.** O clique só toca som; o hover só aparece pela dica.
4. **Nenhum número flutuante** (nutrientes entregues, custo gasto, dano, ovo posto).
5. **Eventos da simulação não carregam posição.** `AntColony.Sfx` só diz *o quê*; para partículas é preciso saber *onde* (célula) e, às vezes, *quem*.
6. **Painéis aparecem/somem sem transição** (menu do ESC, Configurações, Evolução, Game over, pop-ups do tutorial, alerta de cupins).
7. **Marcos sem momento próprio:** nova classe liberada, nó comprado, primeiro nascimento, ataque repelido, fim de partida. Hoje só um `Buy`/`GameOver` sonoro.
8. **Contadores (Nutrientes, Formigas, Rainha) mudam de texto sem reação** — oportunidade grande e barata de dopamina.
9. **Formigas não têm reação corporal** a nascer, ser ferida (só som `AntHurt`) ou entregar carga.
10. **Câmera não reage** a nada (sem tremor leve, sem foco em marco).
11. **Tooltips/telas novas ainda sem fade.**

## Restrições que moldam a solução

- Tudo em pixel art, canvas de referência 1920x1080 com escala por tela; HUD recriado pelo Editor.
- Simulação em passo fixo dentro do `AntFarmGame`; pausa e 1x/3x/8x controlados lá, **sem `Time.timeScale`**.
- O jogador passa muito tempo só observando: o mundo precisa estar vivo, mas barato.
- Texto acentuado em caixa alta é ruim na fonte atual.
