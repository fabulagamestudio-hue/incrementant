# Editor

Código exclusivo do Editor da Unity (namespace `Fabula.Incrementant.Editor` ou com o domínio). Nada daqui entra na build.

## Pastas

| Pasta | Propósito |
|---|---|
| `Setup/` | Scripts idempotentes que criam ou atualizam cenas, prefabs e assets. Rodar duas vezes não duplica nada. |
| `Setup/AntFarmSceneSetup.cs` | Menu **Incrementant/Setup/Criar cena AntFarm**: cria/atualiza `Assets/Project/Scenes/AntFarm.unity` e a põe no build. Inclui o filho `Scenery` (cenário da superfície). |
| `Setup/AntFarmMapTools.cs` | Menu **Incrementant/Mapa/Sortear e fixar mapa na cena**: grava o PNG do mapa, reposiciona os marcadores e põe o modo Image. |
| `AntFarm/AntSpriteGenerator.cs` | Gera a arte provisória das formigas (`Assets/Project/Art/Ants/`); menu **Incrementant/Arte/Regerar sprites das formigas**. |
| `AntFarm/AntSpriteImporter.cs` | Configurações de importação dos sprites de formiga (6 px por célula, pivô nas patas). |
| `AntFarm/SceneryArtExporter.cs` | Exporta os elementos do cenário como PNGs em `Assets/Project/Art/Scenery/` e mantém o catálogo `Assets/Project/Data/SceneryArtSet.asset`; menus **Incrementant/Arte/Exportar elementos do cenário** e **Atualizar catálogo do cenário**. |
| `AntFarm/SceneryArtImporter.cs` | Importação dos PNGs do cenário (Point, Read/Write, 6 ppu) e atualização automática do catálogo. |
| `AntFarm/AntFarmGameEditor.cs` | Inspector do `AntFarmGame`: preview da cena e câmera conforme o modo do mapa. |
| `Setup/AntFarmMapImporter.cs` | Configurações de importação dos PNGs em `Assets/Project/Art/Maps/`. |
| `Validation/` | Validadores de conteúdo e de referências, executáveis por menu e em batch. |

Ferramentas de um domínio ficam em uma pasta com o nome do domínio de `Runtime`. Cada ferramenta de menu usa o prefixo `Incrementant/` para ficar agrupada.
