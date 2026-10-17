# Scripts

Código autoral de Incrementant, organizado por domínio. Este arquivo é um **mapa**: diz onde está cada coisa. Como cada sistema funciona fica nos `*Guide.md` ao lado do código; bugs e dívidas ficam em [KnownIssues.md](KnownIssues.md).

## Estrutura

- `Runtime`: código incluído na build, separado por feature/domínio ([README](Runtime/README.md)).
- `Editor`: importadores, ferramentas de setup e validações exclusivas do Editor ([README](Editor/README.md)).
- `Tests`: testes EditMode (`Tests/EditMode`) e PlayMode (`Tests/PlayMode`).

## Domínios de runtime

| Pasta | Namespace | Propósito | Guia |
|---|---|---|---|
| `Runtime/World` | `Fabula.Incrementant.World` | Grade de pixels do terreno, materiais, física granular e geração do mapa. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) |
| `Runtime/Colony` | `Fabula.Incrementant.Colony` | Formigas, campos de navegação e economia da colônia. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) |
| `Runtime/AntFarm` | `Fabula.Incrementant.AntFarm` | Bootstrap do protótipo: loop, input, HUD e render. | [AntFarmGuide](Runtime/AntFarm/AntFarmGuide.md) |

Assembly de runtime: `Runtime/Fabula.Incrementant.asmdef`. Testes EditMode: `Tests/EditMode/Fabula.Incrementant.Tests.EditMode.asmdef` ([AntFarmSimulationTests.cs](Tests/EditMode/AntFarmSimulationTests.cs), [SurfaceSceneryTests.cs](Tests/EditMode/SurfaceSceneryTests.cs), [WorldExpansionTests.cs](Tests/EditMode/WorldExpansionTests.cs), [ChunkedSimulationTests.cs](Tests/EditMode/ChunkedSimulationTests.cs), [RainSimulationTests.cs](Tests/EditMode/RainSimulationTests.cs)).

Adicione uma linha por domínio quando a pasta nascer. Dados configuráveis e `ScriptableObject`s ficam perto da feature que os usa. Não crie pastas genéricas como `Managers`, `Utils` ou `Data`.

## Convenções

- Namespace raiz: `Fabula.Incrementant`, mais o domínio quando ajudar.
- Código de Runtime não depende de `UnityEditor`.
- Mapa só diz onde; comportamento, fluxos e regras vão para o guia do sistema.
