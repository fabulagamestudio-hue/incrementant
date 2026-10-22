# Editor

Código exclusivo do Editor da Unity (namespace `Fabula.Incrementant.Editor` ou com o domínio). Nada daqui entra na build.

## Pastas

| Pasta | Propósito |
|---|---|
| `Setup/` | Scripts idempotentes que criam ou atualizam cenas, prefabs e assets. Rodar duas vezes não duplica nada. |
| `Setup/AntFarmSceneSetup.cs` | Menu **Incrementant/Setup/Criar cena AntFarm**: cria/atualiza `Assets/Project/Scenes/AntFarm.unity` e a põe no build. Inclui o filho `Scenery` (cenário da superfície). Menu **Incrementant/Arte/Ligar arte das formigas por papel e dos cupins**: só a arte do `AntSwarmView` (papéis, rainha, cupins e pulgões). |
| `Setup/PostProcessSetup.cs` | Menu **Incrementant/Setup/Configurar pós-processamento**: perfil `Assets/Project/Settings/PostProcessProfile.asset` (bloom, vinheta, cor quente), Volume global `PostProcess` com `PostProcessSettingsBinder` e pós-processamento na câmera da cena aberta. `Build(scene, camera)` é chamado por `AntFarmSceneSetup.BuildWorld`. Guia: [SettingsGuide](../Runtime/Settings/SettingsGuide.md). |
| `Setup/AntFarmMapTools.cs` | Menu **Incrementant/Mapa/Sortear e fixar mapa na cena**: grava o PNG do mapa, reposiciona os marcadores e põe o modo Image. |
| `AntFarm/AntSpriteGenerator.cs` | Gera a arte provisória das formigas (`Assets/Project/Art/Ants/`); menu **Incrementant/Arte/Regerar sprites das formigas**. |
| `AntFarm/AphidSpriteImporter.cs` | Importa os 8 sprites de pulgão em `Art/Aphids/`: 72x48, Point, 36 PPU; dois terços do canvas das formigas. |
| `AntFarm/AntSpriteImporter.cs` | Importação dos sprites: adultos Walk/Top/Fly/Land a 36 px por célula, cria e base do cenário 6 px por célula; pivôs preservados. |
| `AntFarm/SceneryArtExporter.cs` | Exporta os elementos do cenário como PNGs em `Assets/Project/Art/Scenery/` e mantém o catálogo `Assets/Project/Data/SceneryArtSet.asset`; menus **Incrementant/Arte/Exportar elementos do cenário** e **Atualizar catálogo do cenário**. |
| `AntFarm/SceneryArtImporter.cs` | Importação dos PNGs do cenário (Point, Read/Write, 16 ppu na arte refinada (dimensões múltiplas de 16), limite de 2048 px e 6 ppu na arte legada) e atualização automática do catálogo. |
| `AntFarm/AntFarmGameEditor.cs` | Inspector do `AntFarmGame`: preview da cena e câmera conforme o modo do mapa. |
| `Setup/AntFarmMapImporter.cs` | Configurações de importação dos PNGs em `Assets/Project/Art/Maps/`. |
| `UI/UiSpriteImporter.cs` | Importa `Art/UI/Sprites` como sprites de UI e grava as bordas 9-slice; menu **Incrementant/UI/Aplicar importação e 9-slice dos sprites**. |
| `UI/UiFontImporter.cs` | Configura o TTF da Press Start 2P em `Art/UI/Fontes/PressStart2P` (dinâmico, Hinted Raster); menu **Incrementant/UI/Aplicar importação das fontes**, chamado pelo setup do HUD. |
| `UI/SkillTreeSpriteImporter.cs` | Importa o kit `Art/UI/Sprites/FormigueiroSkillTree/Textures` como sprite de pixel art (Point, malha completa) e grava as bordas 9-slice de abas, painel e botões; menu **Incrementant/UI/Aplicar importação dos sprites da árvore** (também chamado pelo setup do HUD). |
| `UI/SplashSceneSetup.cs` | Menu **Incrementant/Setup/Criar cena Splash**: importa `Art/UI/splash-fabula-fundo.png` e `splash-fabula-logo.png` como sprites de pixel art, monta `Scenes/Splash.unity` e a põe em primeiro no build. `UpdateLayers()` atualiza só as imagens da cena existente. |
| `UI/SplashDissolveSetup.cs` | Menu **Incrementant/Setup/Splash - dissolução da logo**: assa o logotipo em texturas (posição/cor/atraso), monta `VFX/SplashLogo/LogoDissolve.vfx` por reflexão sobre o editor do VFX Graph e liga câmera, canvas, objeto `LogoDissolve` e `SplashScreen` na cena Splash. Guia: [MenuGuide.md](../Runtime/Menu/MenuGuide.md). |
| `UI/AntFarmHudSetup.MainMenu.cs` | Parcial do montador do HUD: menu **Incrementant/Setup/Criar cena MainMenu** (`Scenes/MainMenu.unity`, canvas `MenuPrincipal`, vitrine do fundo; coloca a cena em primeiro no build). Guia: [MenuGuide](../Runtime/Menu/MenuGuide.md). |
| `UI/AntFarmHudSetup.Evolution.cs` | Parcial do montador do HUD: páginas da tela Evolução (janela arrastável, anéis, túneis, nós, galerias e portais) a partir de `UpgradeCatalog` e `UpgradeLayout`. Guia: [HudGuide](../Runtime/UI/HudGuide.md) ("Tela Evolução"). |
| `UI/AntFarmHudSetup.ColonyPanel.cs` | Parcial do montador do HUD: botão do painel da colônia (na `BarraInferior`, entre a pausa e o sino) e o painel `PainelColonia` (`ColonyPanel`). Guia: [HudGuide](../Runtime/UI/HudGuide.md). |
| `UI/GlossaryImageImporter.cs` | Importa as imagens de `Art/UI/Glossary` como Sprite Point, sem compressão. Guia: [GlossaryGuide](../Runtime/Glossary/GlossaryGuide.md). |
| `UI/AntFarmHudSetup.Glossary.cs` | Parcial do montador do HUD: janela `Glossario` (`GlossaryScreen`), com as imagens da pasta `Art/UI/Glossary`. Guia: [GlossaryGuide](../Runtime/Glossary/GlossaryGuide.md). |
| `UI/AntFarmHudSetup.cs` | Menu **Incrementant/Setup/Montar HUD da partida**: recria o objeto `HUD` (Canvas), com o menu do ESC e a tela de configurações, e o EventSystem na cena do AntFarm. Guia: [HudGuide](../Runtime/UI/HudGuide.md). |
| `UI/AntFarmHudSetup.Version.cs` | Parcial do montador do HUD: menu **Incrementant/Setup/Criar overlay de versão** monta `Assets/Project/Resources/BuildVersionOverlay.prefab` (canvas por cima de tudo, texto da versão clicável no canto inferior direito e a janela do histórico de versões, ligados ao settings do Build Assistant). Guia: [HudGuide](../Runtime/UI/HudGuide.md), "Versão da build". |
| `Localization/LocalizationCatalog.cs`, `LocalizationCatalog.Glossary.cs`, `LocalizationSetup.cs` | Catálogo de textos (português + inglês; os do glossário no parcial) e o menu **Incrementant/Setup/Configurar localização** (Locales, Localization Settings e tabela `Game` em `Assets/Project/Localization`); `LocalizationSetup.Bind` liga cada rótulo estático do HUD à tabela. Guia: [TextsGuide](../Runtime/Texts/TextsGuide.md). |
| `Validation/` | Validadores de conteúdo e de referências, executáveis por menu e em batch. |

Ferramentas de um domínio ficam em uma pasta com o nome do domínio de `Runtime`. Cada ferramenta de menu usa o prefixo `Incrementant/` para ficar agrupada.
