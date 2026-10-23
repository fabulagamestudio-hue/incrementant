# Scripts

Código autoral de Incrementant, organizado por domínio. Este arquivo é um **mapa**: diz onde está cada coisa. Como cada sistema funciona fica nos `*Guide.md` ao lado do código; bugs e dívidas ficam em [KnownIssues.md](KnownIssues.md).

## Estrutura

- `Runtime`: código incluído na build, separado por feature/domínio ([README](Runtime/README.md)).
- `Editor`: importadores, ferramentas de setup e validações exclusivas do Editor ([README](Editor/README.md)).
- `Tests`: testes EditMode (`Tests/EditMode`) e PlayMode (`Tests/PlayMode`).

## Domínios de runtime

| Pasta | Namespace | Propósito | Guia |
|---|---|---|---|
| `Runtime/World` | `Fabula.Incrementant.World` | Grade de pixels do terreno, materiais, física granular, chuva, água, plantas da superfície e geração do mapa. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) |
| `Runtime/Colony` | `Fabula.Incrementant.Colony` | Formigas, campos de navegação e economia da colônia; ataques de cupins, área de resíduos, depósitos e fazenda de pulgões. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md), [RaidGuide](Runtime/Colony/RaidGuide.md), [StorageGuide](Runtime/Colony/StorageGuide.md), [FarmGuide](Runtime/Colony/FarmGuide.md) |
| `Runtime/Upgrades` | `Fabula.Incrementant.Upgrades` | Catálogo das melhorias (câmaras e galerias), fórmulas de custo e profundidade, estado lido da colônia e layout radial da tela Evolução. | [UpgradesGuide](Runtime/Upgrades/UpgradesGuide.md) |
| `Runtime/Save` | `Fabula.Incrementant.Save` | Save/load da partida (retrato do terreno, colônia e chuva). | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) |
| `Runtime/Settings` | `Fabula.Incrementant.Settings` | Configurações do jogo (áudio, vídeo, interface): dados, arquivo e aplicação. | [SettingsGuide](Runtime/Settings/SettingsGuide.md) |
| `Runtime/AntFarm` | `Fabula.Incrementant.AntFarm` | Bootstrap do protótipo: loop, input, HUD e render. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) |

Assembly de runtime: `Runtime/Fabula.Incrementant.asmdef`. Testes EditMode: `Tests/EditMode/Fabula.Incrementant.Tests.EditMode.asmdef` ([AntFarmSimulationTests.cs](Tests/EditMode/AntFarmSimulationTests.cs), [SurfaceSceneryTests.cs](Tests/EditMode/SurfaceSceneryTests.cs), [WorldExpansionTests.cs](Tests/EditMode/WorldExpansionTests.cs), [ChunkedSimulationTests.cs](Tests/EditMode/ChunkedSimulationTests.cs), [TerrainRenderCacheTests.cs](Tests/EditMode/TerrainRenderCacheTests.cs), [TerrainTileAtlasTests.cs](Tests/EditMode/TerrainTileAtlasTests.cs), [MapCameraZoomTests.cs](Tests/EditMode/MapCameraZoomTests.cs), [RainSimulationTests.cs](Tests/EditMode/RainSimulationTests.cs), [SaveLoadTests.cs](Tests/EditMode/SaveLoadTests.cs), [WaterSimulationTests.cs](Tests/EditMode/WaterSimulationTests.cs), [QueenTests.cs](Tests/EditMode/QueenTests.cs), [NutrientTests.cs](Tests/EditMode/NutrientTests.cs), [OutdoorWalkingTests.cs](Tests/EditMode/OutdoorWalkingTests.cs), [DigReservationTests.cs](Tests/EditMode/DigReservationTests.cs), [GameSettingsTests.cs](Tests/EditMode/GameSettingsTests.cs), [RaidTests.cs](Tests/EditMode/RaidTests.cs), [ColonyWorkTests.cs](Tests/EditMode/ColonyWorkTests.cs), [StorageTests.cs](Tests/EditMode/StorageTests.cs), [DownhillTieBreakTests.cs](Tests/EditMode/DownhillTieBreakTests.cs), [WaterReleaseTests.cs](Tests/EditMode/WaterReleaseTests.cs), [UpgradeCatalogTests.cs](Tests/EditMode/UpgradeCatalogTests.cs), [WalkabilityMapTests.cs](Tests/EditMode/WalkabilityMapTests.cs), [WasteModeTests.cs](Tests/EditMode/WasteModeTests.cs), [PaintBudgetTests.cs](Tests/EditMode/PaintBudgetTests.cs), [GlowMushroomTests.cs](Tests/EditMode/GlowMushroomTests.cs), [SpitterAndMiddenTests.cs](Tests/EditMode/SpitterAndMiddenTests.cs), [FarmTests.cs](Tests/EditMode/FarmTests.cs), [BlockCrackTests.cs](Tests/EditMode/BlockCrackTests.cs), [StorageStackTests.cs](Tests/EditMode/StorageStackTests.cs)).

Adicione uma linha por domínio quando a pasta nascer. Dados configuráveis e `ScriptableObject`s ficam perto da feature que os usa. Não crie pastas genéricas como `Managers`, `Utils` ou `Data`.

## Convenções

- Namespace raiz: `Fabula.Incrementant`, mais o domínio quando ajudar.
- Código de Runtime não depende de `UnityEditor`.
- Mapa só diz onde; comportamento, fluxos e regras vão para o guia do sistema.
