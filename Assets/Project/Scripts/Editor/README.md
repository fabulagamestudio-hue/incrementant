# Editor

Código exclusivo do Editor da Unity (namespace `Fabula.Incrementant.Editor` ou com o domínio). Nada daqui entra na build.

## Pastas

| Pasta | Propósito |
|---|---|
| `Setup/` | Scripts idempotentes que criam ou atualizam cenas, prefabs e assets. Rodar duas vezes não duplica nada. |
| `Setup/AntFarmSceneSetup.cs` | Menu **Incrementant/Setup/Criar cena AntFarm**: cria/atualiza `Assets/Project/Scenes/AntFarm.unity` e a põe no build. Inclui o filho `Scenery` (cenário da superfície). Menu **Incrementant/Arte/Ligar arte das formigas por papel e dos cupins**: só a arte do `AntSwarmView`. |
| `Setup/AntFarmMapTools.cs` | Menu **Incrementant/Mapa/Sortear e fixar mapa na cena**: grava o PNG do mapa, reposiciona os marcadores e põe o modo Image. |
| `AntFarm/AntSpriteGenerator.cs` | Gera a arte provisória das formigas (`Assets/Project/Art/Ants/`); menu **Incrementant/Arte/Regerar sprites das formigas**. |
| `AntFarm/AntSpriteImporter.cs` | Importação dos sprites: adultos Walk/Top/Fly/Land a 36 px por célula, cria e base do cenário 6 px por célula; pivôs preservados. |
| `AntFarm/SceneryArtExporter.cs` | Exporta os elementos do cenário como PNGs em `Assets/Project/Art/Scenery/` e mantém o catálogo `Assets/Project/Data/SceneryArtSet.asset`; menus **Incrementant/Arte/Exportar elementos do cenário** e **Atualizar catálogo do cenário**. |
| `AntFarm/SceneryArtImporter.cs` | Importação dos PNGs do cenário (Point, Read/Write, 6 ppu) e atualização automática do catálogo. |
| `AntFarm/AntFarmGameEditor.cs` | Inspector do `AntFarmGame`: preview da cena e câmera conforme o modo do mapa. |
| `Setup/AntFarmMapImporter.cs` | Configurações de importação dos PNGs em `Assets/Project/Art/Maps/`. |
| `UI/UiSpriteImporter.cs` | Importa `Art/UI/Sprites` como sprites de UI e grava as bordas 9-slice; menu **Incrementant/UI/Aplicar importação e 9-slice dos sprites**. |
| `UI/UiFontImporter.cs` | Configura o TTF da Press Start 2P em `Art/UI/Fontes/PressStart2P` (dinâmico, Hinted Raster); menu **Incrementant/UI/Aplicar importação das fontes**, chamado pelo setup do HUD. |
| `UI/SkillTreeSpriteImporter.cs` | Importa o kit `Art/UI/Sprites/FormigueiroSkillTree/Textures` como sprite de pixel art (Point, malha completa) e grava as bordas 9-slice de abas, painel e botões; menu **Incrementant/UI/Aplicar importação dos sprites da árvore** (também chamado pelo setup do HUD). |
| `UI/AntFarmHudSetup.cs` | Menu **Incrementant/Setup/Montar HUD da partida**: recria o objeto `HUD` (Canvas), com o menu do ESC e a tela de configurações, e o EventSystem na cena do AntFarm. Guia: [HudGuide](../Runtime/UI/HudGuide.md). |
| `Validation/` | Validadores de conteúdo e de referências, executáveis por menu e em batch. |

Ferramentas de um domínio ficam em uma pasta com o nome do domínio de `Runtime`. Cada ferramenta de menu usa o prefixo `Incrementant/` para ficar agrupada.
