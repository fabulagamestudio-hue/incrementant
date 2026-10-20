# Áudio — Acompanhamento de produção

**Projeto:** Incrementant
**Setor:** AUDIO
**Última avaliação:** 2026-10-20

## Resumo

Os efeitos sonoros existem e são sintetizados em código (`SfxSynth`), tocados por `GameAudio` com os volumes das configurações (AUD-001). Desde 2026-10-20 os sons da colônia são espaciais: volume pela distância ao centro da câmera e pelo zoom, e lado no estéreo (AUD-002, REQ-041), aprovado pelo desenvolvedor em 2026-10-20.

## Itens

| ID | Título | Estado | Prioridade | Evidência | Dependências | Verificado em |
|---|---|---|---|---|---|---|
| AUD-001 | Efeitos sonoros (SFX) do jogo, respeitando o volume de efeitos das configurações | IMPLEMENTED | UNDEFINED | [GameAudio.cs](../Assets/Project/Scripts/Runtime/Audio/GameAudio.cs), [SfxSynth.cs](../Assets/Project/Scripts/Runtime/Audio/SfxSynth.cs), [SfxSynthTests.cs](../Assets/Project/Scripts/Tests/EditMode/SfxSynthTests.cs); guia [AudioGuide.md](../Assets/Project/Scripts/Runtime/Audio/AudioGuide.md) | REQ-023, PRG-031 | 2026-10-20 |
| AUD-002 | Som espacial da colônia: volume pela distância ao centro da câmera e pelo zoom, lado no estéreo | DONE | UNDEFINED | [AntColony.Audio.cs](../Assets/Project/Scripts/Runtime/Colony/AntColony.Audio.cs), `AntFarmGame.SpatialSfxVolume`; curva conferida no editor com a câmera da cena; suíte EditMode 320/327 na Unity (as 7 falhas são as de KI-022); aprovado pelo desenvolvedor em 2026-10-20 | REQ-041, AUD-001 | 2026-10-20 |

## Detalhes

- Nenhum.

## Riscos e bloqueios

- Nenhum.

## Não verificado

- Os sons em si (AUD-001) não têm aprovação registrada; o som espacial (AUD-002) foi aprovado.
- REQ-023 segue PLANNED no registro, embora os SFX existam; falta o desenvolvedor dizer se a parte de SFX está atendida.
