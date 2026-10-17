# Runtime

Contém somente código executado na build, organizado por domínio. A tabela de domínios fica em [../README.md](../README.md).

Dependências devem apontar para contratos claros do domínio necessário. Evite dependências circulares entre features e não faça código de runtime depender de `Editor`.

## Pastas

| Pasta | Propósito |
|---|---|
| `World/` | `PixelGrid` (grade, marcas e blocos 16x16 acordados/versionados), `CellMaterial`/`CellMaterials` (regras e cores), `GranularSimulation` (queda de grãos), `RainSimulation` + `RainSettings`/`Weather`/`RainDrop` (chuva leve e forte), `WaterSimulation` + `WaterSettings` (escoamento da água e erosão/consumo só dentro do formigueiro; a camada de água fica em `PixelGrid`), `TerrainGenerator` + `TerrainSettings`/`TerrainLayout` (geração do mapa por região, mundo expansível), `TerrainMapCodec` (PNG ↔ grade), `VegetationSimulation` + `VegetationSettings`/`PlantKind`/`PlantShape` (plantas da superfície: onde nascem, forma em células, crescimento e regra de colheita; as camadas de sala de nutrientes e nutriente guardado ficam em `PixelGrid`). |
| `Colony/` | `AntColony` (comportamento, economia de nutrientes, colheita e entrega à rainha, rainha e sala da rainha), `Ant` (estado), `ColonySettings`, `FlowField` + `AntNavigation` (BFS e caminhabilidade), `WalkabilityMap` (cache de caminhabilidade por bloco, usado pelo BFS). |
| `Save/` | `GameSnapshot` (retrato da partida + arquivo binário gzip), `AntRecord`, `SaveStorage` (leitura/gravação atômica do save rápido). Guia: [AntFarmGuide.md](AntFarm/AntFarmGuide.md). |
| `AntFarm/` | `MatchTimer` (cronômetro da partida), `AntFarmGame` + `MapSource` (bootstrap, modo do mapa, geração sob demanda, passo fixo, input, HUD), `MapCamera` (pan, zoom e limitador da câmera), `AntFarmView` (textura do terreno na janela visível), `AntSwarmView` (formigas em sprite), `SurfaceSceneryView` + `SurfaceSceneryPainter`/`SurfaceScenerySettings` (cenário da superfície na escala das formigas, com paralaxe), `SceneryStamps.cs` (`SceneryElement`, `SceneryStamp`, `SceneryStampLibrary`: elementos isolados do cenário), `SceneryArtSet` (catálogo de PNGs por elemento), `PlantSprites` (plantas colhíveis desenhadas com os sprites de grama/trevo, recortados pelas células que sobraram; criado pelo `SurfaceSceneryView`). Guia: [AntFarmGuide.md](AntFarm/AntFarmGuide.md). |
