# Formiga inimiga alada

## Cupins refinados — 2026-10-21 (REQ-050)

Os 16 quadros atuais receberam o tratamento de carapaça segmentada e luz superior esquerda aprovado para as formigas e a Rainha. Mantidas a carapaça carvão/violeta, as patas vinho, os olhos âmbar e as asas claras: somente `1B1815`, `3A1A5C`, `504D55`, `6B1F45`, `D6872F`, `F5EDD9` e `BBB29F`, todas da paleta mestra. Canvas **64×64**, **16 PPU**, filtro Point, sem mipmaps/compressão; alpha exclusivamente 0/255, inclusive nas asas.

Top, Walk, Fly e Land continuam com quatro quadros cada. O enquadramento foi registrado pela carapaça original, sem recentrar o inseto conforme as asas mudam; a carapaça foi estabilizada entre os estados e as asas claras ficam por cima. Land passa de asas erguidas a recolhidas, depois desprendidas e, no quadro 3, ausentes. `Ant_Land_3.png` é igual a `Ant_Walk_0.png`, evitando salto de pose na transição. Pivôs preservados: Top (32,32), demais (32,16). Os caminhos e os 16 `.meta`, incluindo GUIDs e spriteIDs, permaneceram idênticos aos anteriores à tarefa.

Masters em `../Source~/TermiteRefined/`, editados com ImageGen integrado. Prompts em `../Source~/termite-refinement-prompts.json`. Preparação: `normalize-termite.ps1` aplica grade, proporção, sete cores e alpha binário; `fit-termite-layout.ps1` registra o enquadramento; `stabilize-termite-refined.ps1` mantém a carapaça. Para reexportar os masters finais, sem depender do serviço de imagem:

```powershell
& "Assets/Project/Art/Ants/Source~/bake-termite-refined.ps1"
```

Validação em 2026-10-21: 16 PNGs e 65.536 pixels conferidos (dimensões, paleta e alpha); quatro quadros distintos por estado; prancha completa inspecionada; importação síncrona dos 16 sprites na Unity 6000.3.11f1 sem divergências de dimensões, PPU, pivôs, Point, compressão, mipmaps ou modo Single; 16 metadados idênticos após importação. [Prancha final](../../../../../Docs/02_World_Art_UI/Cupins_refinados_2026-10-21.png). Reprodução em Play Mode e aprovação visual do conjunto pendentes (ART-015). O console continha mensagens de RenderTexture e atlas associadas a uma execução de testes; sem investigação fora do escopo dos sprites. Nenhuma build gerada.

## Histórico

O gerador procedural citado abaixo sobrescreve este refinamento. Para restaurar a aparência atual, use o exportador dos masters acima.

**Etapa 8 (2026-10-21):** os 16 quadros foram redesenhados em 64 × 64 a 16 px por célula, na paleta mestra, pelo gerador de `../Source~/ant_sprites.py` (ver o [README das formigas](../README.md)). O texto abaixo descreve a arte anterior (144 × 144).

Arte de 2026-10-18 (REQ-027). Mesma linguagem de pixel art das formigas da colônia, com carapaça carvão/índigo, detalhes violáceos, olhos âmbar e asas claras. A mesma formiga chega voando, pousa, perde as asas e continua andando.

| Arquivos | Quadros | Uso |
|---|---|---|
| `Ant_Fly_0.png` a `Ant_Fly_3.png` | 4 | Chegada em voo; fases do bater das asas |
| `Ant_Land_0.png` a `Ant_Land_3.png` | 4 | Patas tocam o chão, asas se recolhem, se desprendem e desaparecem |
| `Ant_Walk_0.png` a `Ant_Walk_3.png` | 4 | Caminhada lateral após perder as asas |
| `Ant_Top_0.png` a `Ant_Top_3.png` | 4 | Caminhada sem asas vista de cima, para o fundo do ambiente |

Os 16 PNGs têm canvas comum de 144x144 px, 36 pixels por unidade, com espaço para as asas e o corpo na escala aproximada das demais formigas. Âncora lateral: pivô normalizado (0,5; 0,25); vista superior: (0,5; 0,5). Não reenquadrar cada quadro separadamente: o canvas comum preserva a posição do corpo entre os estados. A arte não precisa crescer para caberem as asas.

`AntSpriteImporter` reconhece Walk, Top, Fly e Land. Metadados preparados com filtro Point, sem mipmaps nem compressão, e GUIDs/spriteIDs novos. Paleta compartilhada de 24 cores; alpha exclusivamente 0/255, sem franjas semitransparentes. As asas são representadas por cores claras, sem alpha parcial.

Criado com `image_gen.imagegen` integrado a partir de uma prancha das formigas existentes como referência de estilo e alinhamento. Os recortes foram amostrados sem suavização e preparados na resolução final; [prompt e preparação](Generation.json).

Validação: 16 arquivos e 331.776 pixels conferidos (144x144, conteúdo não vazio, até 24 cores e alpha binário); prévias dos quatro estados inspecionadas. Importação/compilação no Editor de Incrementant e reprodução em Play Mode pendentes: apenas outra instância de projeto estava conectada ao MCP. Nenhuma build gerada.

Em uso desde 2026-10-18 como os **cupins** dos ataques (REQ-029): Fly na chegada, Land tocado uma vez no pouso, depois Walk/Top sem asas; o corpo morto usa Top virado. Ligação na cena e comportamento: [RaidGuide](../../../Scripts/Runtime/Colony/RaidGuide.md).
