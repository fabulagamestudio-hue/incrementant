# Runtime

Contém somente código executado na build, organizado por domínio. A tabela de domínios fica em [../README.md](../README.md).

Dependências devem apontar para contratos claros do domínio necessário. Evite dependências circulares entre features e não faça código de runtime depender de `Editor`.

## Pastas

| Pasta | Propósito |
|---|---|
| `World/` | `PixelGrid` (grade, marcas e blocos 16x16 acordados/versionados), `CellMaterial`/`CellMaterials` (regras e cores), `GranularSimulation` (queda de grãos), `RainSimulation` + `RainSettings`/`Weather`/`RainDrop` (chuva leve e forte), `TerrainGenerator` + `TerrainSettings`/`TerrainLayout` (geração do mapa por região, mundo expansível), `TerrainMapCodec` (PNG ↔ grade). |
| `Colony/` | `AntColony` (comportamento e economia), `Ant` (estado), `ColonySettings`, `FlowField` + `AntNavigation` (BFS e caminhabilidade), `WalkabilityMap` (cache de caminhabilidade por bloco, usado pelo BFS). |
| `AntFarm/` | `AntFarmGame` + `MapSource` (bootstrap, modo do mapa, geração sob demanda, passo fixo, input, HUD), `MapCamera` (pan, zoom e limitador da câmera), `AntFarmView` (textura do terreno na janela visível), `AntSwarmView` (formigas em sprite), `SurfaceSceneryView` + `SurfaceSceneryPainter`/`SurfaceScenerySettings` (cenário da superfície na escala das formigas, com paralaxe), `SceneryStamps.cs` (`SceneryElement`, `SceneryStamp`, `SceneryStampLibrary`: elementos isolados do cenário), `SceneryArtSet` (catálogo de PNGs por elemento). Guia: [AntFarmGuide.md](AntFarm/AntFarmGuide.md). |
