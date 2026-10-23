// Terreno do formigário com cada célula da grade desenhada como um bloco de 8x8 texels (experimento 8x8 de 2026-10-22,
// branch experimento/arte-8x8; antes 16x16). Todos os valores em texels abaixo estão nessa escala: metade dos aprovados a 16.
// _MainTex: cor de cada célula (1 texel por célula, montada na CPU pelo AntFarmView: tom, marcas, salas, água), sem a luz.
// _CellMaterials: 1 texel por célula (RGBA32). R = material (255 = céu, sem textura); G = 128 + distância de xadrez com sinal
// até a borda terra/vazio (1..4 na terra, -1..-4 no vazio, 4 = 4 ou mais; AntFarmView.SignedDistanceRange), + 64 na célula
// de fronteira entre materiais que se misturam (AntFarmView.MergeFlag); B = nível de
// luz (0..255 = 0..AntFarmView.LightLevels); A = 255 quando a cor já vem iluminada da CPU (terra com marca por cima),
// 128 quando a luz da célula vem de um cogumelo luminoso (tom _GlowLightTint, sem a penumbra azulada e sem degraus:
// gradiente suave, pedido de 2026-10-21), 0 no resto.
// _TileAtlas (etapa 4 do documento 40): atlas RGB, uma textura repetível de _TileSize texels por linha, empilhadas na
// vertical (linha = id do material; depois deles, _BackTileId é a parede de fundo; por último, de 2 em 2, bloco e fundo de
// cada sala construída, CellTileArt.RoomBlockRow/RoomBackRow). O R da célula é a linha: o bloco é sólido e o fundo de sala é
// vazio como o túnel (sombra de canto, luz mínima), só com outra arte. O texel é a cor do material. A textura é
// lida em coordenadas de mundo (texel = célula * _TexelsPerCell), então células vizinhas do mesmo material formam uma superfície
// contínua; a borda só aparece onde o material muda.
// _MaterialColors: 2 texels por linha do atlas: x = 0 a cor de referência (a que a CPU grava numa célula do material sem
// tom nem marca), x = 1 a média do atlas. A cor desenhada é lerp(média, texel, força) × (cor da célula / referência), em
// espaço linear: o tom, a umidade, as marcas e a água da CPU entram como ajuste sobre a arte (TerrainTileAtlas).
// Parede de fundo (etapa 3 do documento 40): no túnel, usa a textura de _BackTileId e escurece em degraus perto da terra
// (sombra de canto), em texels inteiros (xadrez), então nada fica mais fino que o pixel de arte. A terra não tem contorno
// (decisão de 2026-10-21). Valores provisórios até a paleta mestra.
// Luz (etapa 6; substitui a névoa): entra de fora e sai dos cogumelos luminosos. O nível de cada célula é interpolado entre
// as 4 células mais próximas na posição do texel, sem degraus, e passa por uma curva que chega a zero _LightFadeStart antes
// do fim e desce ao quadrado: gradiente suave até o escuro (2026-10-21; antes eram 5 degraus). O túnel não fica abaixo de
// _InteriorAmbient (penumbra do formigueiro); a terra longe da luz fica preta. O céu não recebe luz.
// Emissão: o material _GlowMaterialId (cogumelo luminoso) sai com a cor × _GlowEmission, acima de 1 (HDR), para o bloom
// do pós-processamento, com limiar acima do branco, brilhar só nele.
// Transbordo (experimento de 2026-10-22): numa célula vazia (céu ou túnel) com _OverhangMaterialId (grama) embaixo, desenha
// _OverhangTex (uma célula de altura, _OverhangSize texels de largura, lida no x do mundo) por cima: a grama passa do topo do
// bloco sem mudar a célula, que continua vazia para a lógica. Usa o tom e a luz do texel de cima do bloco de baixo.
// Borda lascada (experimento de 2026-10-22): pedaços da beira das faces expostas e das quinas convexas de terra, grama,
// terra despejada, areia e pedra (cada uma no seu ritmo, ChipStyleOf) são desenhados como a vizinha vazia (ChipFrom); a célula
// continua sólida.
// Junção (experimento de 2026-10-22): na fronteira entre terra, grama, terra despejada, areia e pedra, texels da beira
// passam ao material vizinho (BoundaryOffset), com a cor da célula de origem; grama sobre terra usa línguas e montinhos
// próprios (MergeProfile). Só nas células que a CPU marca no canal G (+64, AntFarmView.MergeFlag).
// Contorno (experimento 8x8 de 2026-10-22, pedido do desenvolvedor: "linha preta ao redor dos elementos", preto da paleta):
// o texel sólido que encosta, nas 4 direções, num texel vazio sai em _OutlineColor (por dentro do bloco, depois das lascas);
// as folhas do transbordo contam como cheias, e o texel vazio que encosta numa folha sai em _OutlineColor (por fora dela).
// Itens guardados (2026-10-23, pedido do desenvolvedor: "desenhe os itens guardados com a textura do atlas"): _CellItems tem
// 1 texel por célula, R = linha do atlas do item + 1 (0 = sem item; a água guardada continua na cor da CPU). Na célula vazia
// com item, o item é um objeto de _TexelsPerCell - 2 texels de lado, com 1 texel de folga da borda, a arte da linha dele e
// contorno _OutlineColor, desenhado sobre o fundo (parede do túnel ou fundo de sala): fica "na parede de fundo".
// Valores a partir de ItemSpriteBase (200) apontam para um sprite de _ItemSprites (8 x 8 cada, lado a lado), desenhado na
// célula inteira com o alfa dele: o minério solto usa o mesmo sprite da boca da formiga (Art/Ants/Item_*Ore).
Shader "Incrementant/TerrainTiles"
{
    Properties
    {
        [PerRendererData] _MainTex ("Cores das células", 2D) = "white" {}
        _CellMaterials ("Material das células", 2D) = "black" {}
        _CellItems ("Item guardado das células (o AntFarmView sobrescreve)", 2D) = "black" {}
        _ItemSprites ("Sprites dos itens com arte própria, lado a lado (o AntFarmView sobrescreve)", 2D) = "clear" {}
        _ItemSpriteCount ("Quantos sprites há em _ItemSprites", Float) = 0
        _TileAtlas ("Atlas RGB de materiais (o AntFarmView sobrescreve)", 2D) = "gray" {}
        _MaterialColors ("Referência e média de cada linha do atlas (o AntFarmView sobrescreve)", 2D) = "gray" {}
        _TileCount ("Linhas do atlas (o AntFarmView sobrescreve)", Float) = 33
        _TileSize ("Lado da textura de cada linha, em texels", Float) = 64
        _TexelsPerCell ("Texels por célula", Float) = 8
        _WindowOrigin ("Janela: xy = célula (0,0) no mundo, zw = largura e altura em células", Vector) = (0, 0, 0, 0)
        _DetailStrength ("Força da textura", Range(0, 2)) = 1
        _BackTileId ("Linha do atlas usada como parede de fundo do túnel (o AntFarmView sobrescreve)", Float) = 18
        _BackDetail ("Força da textura da parede de fundo", Range(0, 2)) = 0.6
        _WallShadowStrength ("Sombra de canto: escurecimento junto da terra", Range(0, 1)) = 0.45
        _WallShadowWidth ("Sombra de canto: alcance, em texels", Range(1, 8)) = 3
        _WallShadowSteps ("Sombra de canto: degraus", Range(1, 6)) = 3
        _InteriorAmbient ("Brilho mínimo do túnel longe da luz (o AntFarmView sobrescreve)", Range(0, 1)) = 0.22
        _LightTint ("Cor da luz cheia", Color) = (1.06, 1, 0.9, 1)
        _GlowLightTint ("Tom da luz dos cogumelos (o AntFarmView sobrescreve)", Color) = (1, 0.93, 0.72, 1)
        _LightFadeStart ("Luz: nível (0 a 1) em que ela chega a zero", Range(0, 0.5)) = 0.09375
        _GlowMaterialId ("Material emissivo: id no atlas (o AntFarmView sobrescreve)", Float) = 17
        _GlowEmission ("Emissão do material emissivo (o AntFarmView sobrescreve)", Range(1, 8)) = 2.5
        _ShadowTint ("Cor da penumbra", Color) = (0.8, 0.85, 1, 1)
        _OverhangTex ("Transbordo da grama (o AntFarmView sobrescreve)", 2D) = "black" {}
        _OverhangSize ("Transbordo: largura da textura, em texels", Float) = 64
        _OverhangMaterialId ("Transbordo: material de baixo que o desenha (o AntFarmView sobrescreve; -1 = desligado)", Float) = -1
        [Toggle] _ChipEnabled ("Borda lascada: ligada", Float) = 1
        _ChipDirt ("Borda lascada, terra e terra mole: profundidade, topo, quina, trecho (texels)", Vector) = (4, 1, 6, 5)
        _ChipSpoil ("Borda lascada, terra despejada (solta e compactada): profundidade, topo, quina, trecho", Vector) = (3, 1, 3, 3)
        _ChipSand ("Borda lascada, areia (solta e compactada): profundidade, topo, quina, trecho", Vector) = (2, 1, 3, 2)
        _ChipRock ("Borda lascada, pedra: profundidade, topo, quina, trecho", Vector) = (3, 1, 5, 6)
        _ChipGrass ("Borda lascada, grama: profundidade, topo, quina, trecho (topo 0: as folhas do transbordo já quebram a linha)", Vector) = (3, 0, 4, 4)
        _ChipGrassStyle ("Borda lascada, grama: chance de mordida, inclinação", Vector) = (0.75, 1, 0, 0)
        _ChipChance ("Borda lascada: chance de mordida por trecho (terra, despejada, areia, pedra)", Vector) = (0.75, 0.8, 0.9, 0.5)
        _ChipSlope ("Borda lascada: inclinação da mordida, 1 = degraus de 1 texel, menor = arredondada, maior = reta (terra, despejada, areia, pedra)", Vector) = (1, 0.8, 1, 3)
        [Toggle] _MergeEnabled ("Junção: grama escorre na terra de baixo", Float) = 1
        _Drip ("Junção grama/terra: comprimento máximo da língua, trecho, chance de língua, linha base máxima (texels)", Vector) = (5, 4, 0.7, 1)
        _Rise ("Junção grama/terra: montinho de terra subindo na grama: altura máxima, trecho, chance (texels)", Vector) = (3, 5, 0.6, 0)
        _MergeDirt ("Junção, terra avançando na vizinha: altura máxima, trecho, chance, 1 = lasca reta", Vector) = (4, 6, 0.8, 0)
        _MergeGrass ("Junção, grama avançando de lado ou para cima: altura máxima, trecho, chance, 1 = lasca reta", Vector) = (3, 5, 0.7, 0)
        _MergeSpoil ("Junção, terra despejada: altura máxima, trecho, chance, 1 = lasca reta", Vector) = (4, 5, 0.85, 0)
        _MergeSand ("Junção, areia: altura máxima, trecho, chance, 1 = lasca reta", Vector) = (3, 5, 0.9, 0)
        _MergeRock ("Junção, pedra (ninguém avança nela): altura máxima, trecho, chance, 1 = lasca reta", Vector) = (3, 6, 0.6, 1)
        [Toggle] _OutlineEnabled ("Contorno: ligado", Float) = 1
        _OutlineColor ("Contorno: cor (preto da paleta mestra, índice 0)", Color) = (0.106, 0.094, 0.082, 1)
        [Toggle] _DebugSignedDistance ("Depuração: distância com sinal", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "PreviewType" = "Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_CellMaterials); SAMPLER(sampler_CellMaterials);
            TEXTURE2D(_CellItems); SAMPLER(sampler_CellItems);
            TEXTURE2D(_ItemSprites); SAMPLER(sampler_ItemSprites);
            TEXTURE2D(_TileAtlas); SAMPLER(sampler_TileAtlas);
            TEXTURE2D(_MaterialColors); SAMPLER(sampler_MaterialColors);
            TEXTURE2D(_OverhangTex); SAMPLER(sampler_OverhangTex);

            CBUFFER_START(UnityPerMaterial)
                float _TileCount;
                float _TileSize;
                float _TexelsPerCell;
                float _ItemSpriteCount;
                float4 _WindowOrigin;
                float _DetailStrength;
                float _BackTileId;
                float _BackDetail;
                float _WallShadowStrength;
                float _WallShadowWidth;
                float _WallShadowSteps;
                float _InteriorAmbient;
                float4 _LightTint;
                float4 _ShadowTint;
                float4 _GlowLightTint;
                float _LightFadeStart;
                float _GlowMaterialId;
                float _GlowEmission;
                float _DebugSignedDistance;
                float _OverhangSize;
                float _OverhangMaterialId;
                float _ChipEnabled;
                float _OutlineEnabled;
                half4 _OutlineColor;
                float4 _ChipDirt;
                float4 _ChipSpoil;
                float4 _ChipSand;
                float4 _ChipRock;
                float4 _ChipGrass;
                float4 _ChipGrassStyle;
                float4 _ChipChance;
                float4 _ChipSlope;
                float _MergeEnabled;
                float4 _Drip;
                float4 _Rise;
                float4 _MergeDirt;
                float4 _MergeGrass;
                float4 _MergeSpoil;
                float4 _MergeSand;
                float4 _MergeRock;
            CBUFFER_END

            static const float SkyId = 255.0;
            static const float EmptyId = 0.0;
            static const float SignedDistanceRange = 4.0;
            static const float FarTexels = 1000.0;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            // Valor de _CellItems a partir do qual o item é o sprite (valor - ItemSpriteBase) de _ItemSprites (AntFarmView.ItemSpriteBase).
            #define ItemSpriteBase 200

            // Linha do atlas do item guardado na célula, ou -1 sem item.
            float ItemAt(float2 cell)
            {
                float2 uv = (cell + 0.5) / _WindowOrigin.zw;
                return round(SAMPLE_TEXTURE2D_LOD(_CellItems, sampler_CellItems, uv, 0).r * 255.0) - 1.0;
            }

            float MaterialAt(float2 cell)
            {
                float2 uv = (cell + 0.5) / _WindowOrigin.zw;
                return round(SAMPLE_TEXTURE2D_LOD(_CellMaterials, sampler_CellMaterials, uv, 0).r * 255.0);
            }

            // Distância com sinal até a borda terra/vazio: > 0 na terra, < 0 no vazio (túnel, céu).
            float SignedDistanceAt(float2 cell)
            {
                float2 uv = (cell + 0.5) / _WindowOrigin.zw;
                float g = round(SAMPLE_TEXTURE2D_LOD(_CellMaterials, sampler_CellMaterials, uv, 0).g * 255.0);
                return (g >= 160.0 ? g - 64.0 : g) - 128.0;
            }

            // A CPU soma 64 ao G da célula que fica numa fronteira entre famílias de junção diferentes (AntFarmView.MergeFlag).
            bool MergeFlagAt(float2 cell)
            {
                float2 uv = (cell + 0.5) / _WindowOrigin.zw;
                return round(SAMPLE_TEXTURE2D_LOD(_CellMaterials, sampler_CellMaterials, uv, 0).g * 255.0) >= 160.0;
            }

            // Depuração: terra em tons quentes, vazio em frios; quanto mais perto da borda, mais claro.
            half3 SignedDistanceColor(float d)
            {
                half t = (abs(d) - 1.0) / (SignedDistanceRange - 1.0);
                half3 nearColor = d > 0 ? half3(1.0, 0.92, 0.35) : half3(0.45, 0.95, 1.0);
                half3 farColor = d > 0 ? half3(0.45, 0.08, 0.04) : half3(0.04, 0.08, 0.4);
                return lerp(nearColor, farColor, t);
            }

            // Nível de luz da célula, de 0 a 1.
            float LightAt(float2 cell)
            {
                float2 uv = (cell + 0.5) / _WindowOrigin.zw;
                return SAMPLE_TEXTURE2D_LOD(_CellMaterials, sampler_CellMaterials, uv, 0).b;
            }

            // 1 onde a luz da célula vem de um cogumelo (canal A = 128) ou a célula é o próprio cogumelo; 0 no resto.
            float GlowAt(float2 cell)
            {
                float2 uv = (cell + 0.5) / _WindowOrigin.zw;
                float a = SAMPLE_TEXTURE2D_LOD(_CellMaterials, sampler_CellMaterials, uv, 0).a;
                return (a > 0.25 && a < 0.75) || MaterialAt(cell) == _GlowMaterialId ? 1.0 : 0.0;
            }

            // Nível da célula contando só a luz pedida (wantGlow 1 = de cogumelo, 0 = de fora); a outra conta como zero.
            float LightOf(float2 cell, float wantGlow)
            {
                return GlowAt(cell) == wantGlow ? LightAt(cell) : 0.0;
            }

            // Luz no centro do texel, interpolada entre as 4 células mais próximas, só da luz pedida. As duas luzes são
            // interpoladas à parte: misturadas, o arredondamento dos degraus da de fora subia a ponta fraca da de cogumelo e
            // deixava uma linha na borda dela.
            float SmoothLight(float2 cell, float2 inner, float wantGlow)
            {
                float2 p = cell + (inner + 0.5) / _TexelsPerCell - 0.5;
                float2 c0 = floor(p);
                float2 f = p - c0;
                float bottom = lerp(LightOf(c0, wantGlow), LightOf(c0 + float2(1, 0), wantGlow), f.x);
                float top = lerp(LightOf(c0 + float2(0, 1), wantGlow), LightOf(c0 + float2(1, 1), wantGlow), f.x);
                return lerp(bottom, top, f.y);
            }

            // Luz contínua no texel (gradiente suave, pedido de 2026-10-21; feito primeiro nos cogumelos e depois estendido à
            // luz de fora). A queda linear terminava num degrau visível (a última célula, de nível 1 ou 2, caía para 0 de uma
            // vez e a tela realça os tons escuros): a curva chega a zero _LightFadeStart antes do fim e desce ao quadrado.
            half CurvedLight(float2 cell, float2 inner, float wantGlow)
            {
                float g = saturate((SmoothLight(cell, inner, wantGlow) - _LightFadeStart) / (1.0 - _LightFadeStart));
                return g * g;
            }

            // Cor da linha do atlas: column 0 = referência, 1 = média.
            float3 RowColor(float row, float column)
            {
                float2 uv = float2((column + 0.5) / 2.0, (row + 0.5) / _TileCount);
                return SAMPLE_TEXTURE2D_LOD(_MaterialColors, sampler_MaterialColors, uv, 0).rgb;
            }

            float2 AtlasUv(float id, float2 inner, float2 cell)
            {
                float2 worldTexel = (_WindowOrigin.xy + cell) * _TexelsPerCell + inner;
                float2 tileTexel = fmod(worldTexel, _TileSize);
                return float2((tileTexel.x + 0.5) / _TileSize, (id * _TileSize + tileTexel.y + 0.5) / (_TileCount * _TileSize));
            }

            // Distância de xadrez, em texels inteiros, do texel de uma célula vazia até a terra vizinha mais próxima (0 = o texel
            // encosta nela). Olha as 8 vizinhas pelo sinal da distância.
            float TexelsToSolid(float2 cell, float2 inner)
            {
                float last = _TexelsPerCell - 1;
                float best = FarTexels;
                [unroll] for (int dy = -1; dy <= 1; dy++)
                [unroll] for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    if (SignedDistanceAt(cell + float2(dx, dy)) < 0)
                        continue;
                    float tx = dx < 0 ? inner.x : (dx > 0 ? last - inner.x : 0);
                    float ty = dy < 0 ? inner.y : (dy > 0 ? last - inner.y : 0);
                    best = min(best, max(tx, ty));
                }
                return best;
            }

            // Parede de fundo do túnel: escurece em _WallShadowSteps degraus até _WallShadowWidth texels da terra.
            half3 WallLight(float texels)
            {
                if (texels >= _WallShadowWidth)
                    return half3(1, 1, 1);
                float level = floor(texels * _WallShadowSteps / _WallShadowWidth);
                return (half3)(1.0 - _WallShadowStrength * (1.0 - level / _WallShadowSteps));
            }

            // Luz do mapa no texel: de fora e de cogumelo, cada uma interpolada à parte; fica a maior. O tom segue a proporção
            // das duas: amarelado sutil onde manda o cogumelo, sem a penumbra azulada.
            half3 MapLight(float2 cell, float2 inner, bool solid)
            {
                half outside = CurvedLight(cell, inner, 0.0);
                half glowQ = CurvedLight(cell, inner, 1.0);
                half q = max(outside, glowQ);
                half glow = glowQ / max(glowQ + outside, 0.0001);
                if (!solid)
                    q = max(q, _InteriorAmbient);
                half3 low = lerp(_ShadowTint.rgb, _GlowLightTint.rgb, glow);
                half3 high = lerp(_LightTint.rgb, _GlowLightTint.rgb, glow);
                return q * lerp(low, high, q);
            }

            // Transbordo numa célula vazia: rgb já com tom e luz, a = cobertura (0 = nada). O tom e a luz são os do texel de
            // cima do bloco de baixo, para a grama continuar o bloco (marca, umidade e sombra passam junto).
            half4 Overhang(float2 cell, float2 inner, half fade)
            {
                float2 below = cell - float2(0, 1);
                if (_OverhangMaterialId < 0 || MaterialAt(below) != _OverhangMaterialId)
                    return 0;
                float worldX = (_WindowOrigin.x + cell.x) * _TexelsPerCell + inner.x;
                float2 uv = float2((fmod(worldX, _OverhangSize) + 0.5) / _OverhangSize, (inner.y + 0.5) / _TexelsPerCell);
                half4 art = SAMPLE_TEXTURE2D_LOD(_OverhangTex, sampler_OverhangTex, uv, 0);
                if (art.a < 0.5)
                    return 0;
                float2 belowUv = (below + 0.5) / _WindowOrigin.zw;
                float3 tone = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, belowUv, 0).rgb
                    / max(RowColor(_OverhangMaterialId, 0), 0.0005);
                float4 data = SAMPLE_TEXTURE2D_LOD(_CellMaterials, sampler_CellMaterials, belowUv, 0);
                half3 light = data.a < 0.75 ? MapLight(below, float2(inner.x, _TexelsPerCell - 1), true) : half3(1, 1, 1);
                return half4(art.rgb * tone * light, fade);
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Ritmo da borda lascada de um material: depth/top/corner/segment em texels (máximo nas laterais e no teto, no
            // topo, desbaste extra na quina e comprimento do trecho), chance de mordida por trecho e inclinação da mordida.
            struct ChipStyle
            {
                float depth;
                float top;
                float corner;
                float segment;
                float chance;
                float slope;
            };

            ChipStyle MakeChipStyle(float4 shape, float chance, float slope)
            {
                ChipStyle style;
                style.depth = shape.x;
                style.top = shape.y;
                style.corner = shape.z;
                style.segment = max(shape.w, 2);
                style.chance = chance;
                style.slope = slope;
                return style;
            }

            // Ids de CellMaterial (mesma ordem do enum e das linhas do atlas). Terra mole segue a terra; os compactados seguem
            // o material solto. Devolve false nos materiais sem borda lascada.
            bool ChipStyleOf(float id, out ChipStyle style)
            {
                style = MakeChipStyle(_ChipDirt, _ChipChance.x, _ChipSlope.x);
                if (id == 1 || id == 9)          // Dirt, Mud
                    return true;
                if (id == 5 || id == 7)          // Spoil, PackedSpoil
                {
                    style = MakeChipStyle(_ChipSpoil, _ChipChance.y, _ChipSlope.y);
                    return true;
                }
                if (id == 3 || id == 8)          // Sand, PackedSand
                {
                    style = MakeChipStyle(_ChipSand, _ChipChance.z, _ChipSlope.z);
                    return true;
                }
                if (id == 2 || id == 18 || id == 19) // Grass, Litter, Compost
                {
                    style = MakeChipStyle(_ChipGrass, _ChipGrassStyle.x, _ChipGrassStyle.y);
                    return true;
                }
                if (id == 4 || (id >= 21 && id <= 23)) // Rock, minérios
                {
                    style = MakeChipStyle(_ChipRock, _ChipChance.w, _ChipSlope.w);
                    return true;
                }
                return false;
            }

            // Uma "mordida" num trecho de 'len' texels: com a chance do estilo falta um pedaço de 1 a maxDepth texels, mais
            // fundo no meio e raso nas pontas, em vez de um dente por texel. A inclinação diz quanto ela aprofunda por texel
            // a partir da ponta: 1 = degraus de 1 texel, menos de 1 = arredondada (monte), mais = paredes retas (pedra).
            float Bite(float along, float len, float salt, float maxDepth, ChipStyle style)
            {
                float seg = floor(along / len);
                if (Hash(float2(seg, salt)) > style.chance)
                    return 0;
                float depth = 1 + floor(Hash(float2(seg, salt + 7.3)) * maxDepth);
                float t = along - seg * len;
                return min(depth, floor((min(t, len - 1 - t) + 1) * style.slope));
            }

            // Profundidade do entalhe numa face exposta, em texels, no texel do mundo 'along' ao longo da face: dois ritmos de
            // mordidas sobrepostos (o trecho e ~1,6×), para o padrão não se repetir a cada célula.
            float ChipDepth(float along, float salt, float maxDepth, ChipStyle style)
            {
                float a = Bite(along, style.segment, salt, maxDepth, style);
                float b = Bite(along + 3, round(style.segment * 1.6), salt + 19.1, maxDepth, style);
                return min(max(a, b), maxDepth);
            }

            bool IsVoid(float2 cell)
            {
                return SignedDistanceAt(cell) < 0;
            }

            // Borda lascada (experimento de 2026-10-22): nos materiais com estilo (ChipStyleOf), os texels da beira de uma face
            // que dá para o vazio podem virar vazio no desenho. Devolve a direção da vizinha vazia que "invade" o texel, ou
            // (0, 0) se ele fica sólido. Topo raso (style.top, o chão onde as formigas andam), laterais e teto até style.depth,
            // quinas convexas mais roídas (style.corner). A célula continua sólida para a lógica.
            float2 ChipFrom(float2 cell, float2 inner, ChipStyle style)
            {
                float last = _TexelsPerCell - 1;
                float2 world = (_WindowOrigin.xy + cell) * _TexelsPerCell + inner;
                bool up = IsVoid(cell + float2(0, 1));
                bool down = IsVoid(cell - float2(0, 1));
                bool left = IsVoid(cell - float2(1, 0));
                bool right = IsVoid(cell + float2(1, 0));
                // Quina convexa: as duas faces expostas. O desbaste cresce em degraus rumo ao canto (de 1 texel; de 2 nos
                // materiais de mordida reta, para a quina sair em lasca angulosa), com tamanho sorteado por quina (de metade
                // a todo style.corner).
                float fromTop = last - inner.y;
                float fromBottom = inner.y;
                float side = left ? -1 : 1;
                // Bloco fino (vazio dos dois lados opostos, como um bloco solto de 1 célula): metade do entalhe, para ele não
                // virar uma bolinha.
                float thin = (left && right) || (up && down) ? 0.5 : 1;
                float corner = style.corner * thin;
                float depth = style.depth * thin;
                float cornerX = 0;
                if ((left || right) && up)
                    cornerX = max(cornerX, corner * (0.5 + 0.5 * Hash(cell + _WindowOrigin.xy + float2(side, 1))) - fromTop);
                if ((left || right) && down)
                    cornerX = max(cornerX, corner * (0.5 + 0.5 * Hash(cell + _WindowOrigin.xy + float2(side, -1))) + 1 - fromBottom);
                float step = style.slope >= 2 ? 2 : 1;
                cornerX = floor(max(cornerX, 0) / step) * step;
                if (left && inner.x < ChipDepth(world.y, 3, depth, style) + (up || down ? cornerX : 0))
                    return float2(-1, 0);
                if (right && last - inner.x < ChipDepth(world.y, 4, depth, style) + (up || down ? cornerX : 0))
                    return float2(1, 0);
                if (up && fromTop < ChipDepth(world.x, 1, style.top, style))
                    return float2(0, 1);
                if (down && fromBottom < ChipDepth(world.x, 2, depth, style))
                    return float2(0, -1);
                return float2(0, 0);
            }

            // Uma língua num trecho de 'len' texels: com chance 'chance' (sorteio pelo 'salt'), de 1 a 2 texels de largura em
            // posição sorteada no trecho, com comprimento de 2 a 'maxLength'; nas línguas largas as colunas de fora são 1 mais
            // curtas, para a ponta afinar (escala 8x8: metade dos 1 a 3 e 3 de antes). 0 fora da língua.
            float Tongue(float x, float len, float chance, float maxLength, float salt)
            {
                float seg = floor(x / len);
                if (Hash(float2(seg, salt)) >= chance)
                    return 0;
                float width = 1 + floor(Hash(float2(seg, salt + 4.0)) * 2);
                float start = floor(Hash(float2(seg, salt + 10.0)) * (len - width + 1));
                float t = x - seg * len - start;
                if (t < 0 || t >= width)
                    return 0;
                float tongue = 2 + floor(Hash(float2(seg, salt + 16.0)) * max(maxLength - 1, 1));
                float edge = min(t, width - 1 - t);
                return tongue - (width >= 2 && edge == 0 ? 1 : 0);
            }

            // Um montinho num trecho de 'len' texels: com chance 'chance', de 'minWidth' a minWidth + widthRange - 1 texels de
            // largura, com altura de 1 a 'maxHeight' no meio, caindo em degraus inteiros para as pontas (perfil de cúpula). 0 fora.
            float Mound(float x, float len, float chance, float maxHeight, float salt, float minWidth, float widthRange)
            {
                float seg = floor(x / len);
                if (Hash(float2(seg, salt)) >= chance)
                    return 0;
                float width = minWidth + floor(Hash(float2(seg, salt + 4.0)) * widthRange);
                float start = floor(Hash(float2(seg, salt + 10.0)) * (len - width + 1));
                float t = x - seg * len - start;
                if (t < 0 || t >= width)
                    return 0;
                float height = 1 + floor(Hash(float2(seg, salt + 16.0)) * maxHeight);
                float d = (t + 0.5) / width * 2 - 1;   // -1..1 do começo ao fim do montinho
                return max(floor(height * sqrt(saturate(1 - d * d)) + 0.5), 1);
            }

            // Junção grama/terra (experimento de 2026-10-22), no texel do mundo x: x = quantos texels, a partir do topo da
            // célula de terra, mostram a grama de cima; y = quantos texels, a partir da base da célula de grama, mostram a terra
            // de baixo. Línguas de grama descendo (_Drip) e montinhos de terra subindo (_Rise), nunca as duas na mesma coluna (a que desce
            // tem preferência); fora delas, uma linha base irregular de 0 a _Drip.w texels de grama na terra.
            float2 MergeProfile(float x)
            {
                float down = Tongue(x, max(_Drip.y, 2), _Drip.z, _Drip.x, 43.0);
                if (down > 0)
                    return float2(down, 0);
                float up = Mound(x + 3, max(_Rise.y, 3), _Rise.z, _Rise.x, 71.0, 2, 2);
                if (up > 0)
                    return float2(0, up);
                return float2(floor(Hash(float2(x, 41.0)) * (_Drip.w + 1)), 0);
            }

            // Família de junção do material (a mesma de AntFarmView.MergeFamily): 0 terra e terra mole, 1 grama, serapilheira e terra adubada, 2 terra
            // despejada, 3 areia (soltas e compactadas), 4 pedra e minérios; -1 não mistura.
            float MergeFamily(float id)
            {
                if (id == 1 || id == 9) return 0;
                if (id == 2 || id == 18 || id == 19) return 1;
                if (id == 5 || id == 7) return 2;
                if (id == 3 || id == 8) return 3;
                if (id == 4 || (id >= 21 && id <= 23)) return 4;
                return -1;
            }

            // Uma lasca reta num trecho de 'len' texels: com chance 'chance', de 2 a 4 texels de largura e altura constante de 1
            // a 'maxHeight' (bloco anguloso, como a pedra). 0 fora dela.
            float Slab(float x, float len, float chance, float maxHeight, float salt)
            {
                float seg = floor(x / len);
                if (Hash(float2(seg, salt)) >= chance)
                    return 0;
                float width = 2 + floor(Hash(float2(seg, salt + 4.0)) * 3);
                float start = floor(Hash(float2(seg, salt + 10.0)) * (len - width + 1));
                float t = x - seg * len - start;
                if (t < 0 || t >= width)
                    return 0;
                return 1 + floor(Hash(float2(seg, salt + 16.0)) * maxHeight);
            }

            // Quantos texels o material da família f avança na vizinha, no texel 'a' ao longo da fronteira.
            float Reach(float f, float a, float salt)
            {
                float4 m = f == 0 ? _MergeDirt : (f == 1 ? _MergeGrass : (f == 2 ? _MergeSpoil : (f == 3 ? _MergeSand : _MergeRock)));
                return m.w > 0.5 ? Slab(a, max(m.y, 5), m.z, m.x, salt) : Mound(a, max(m.y, 5), m.z, m.x, salt, 2, 3);
            }

            // Fronteira entre P (em cima ou à esquerda) e Q (embaixo ou à direita), no texel 'a' ao longo dela: > 0 = P avança
            // tantos texels em Q, < 0 = Q avança em P, nunca os dois na mesma coluna. As duas células calculam o mesmo valor, então
            // não sobra buraco nem sobreposição. 'boundary' é a posição da fronteira no mundo (sorteio próprio por fronteira). Grama
            // sobre terra mantém o perfil aprovado (MergeProfile); ninguém avança na pedra.
            float BoundaryOffset(float fp, float fq, float a, float boundary, bool vertical)
            {
                float offset;
                if (vertical && fp == 1 && fq == 0)
                {
                    float2 m = MergeProfile(a);
                    offset = m.x > 0 ? m.x : -m.y;
                }
                else
                {
                    float salt = 101.0 + frac(boundary * 0.1373) * 977.0 + (vertical ? 0 : 53.0);
                    float p = fq == 4 ? 0 : Reach(fp, a, salt);
                    float q = fp == 4 ? 0 : Reach(fq, a + 3, salt + 29.0);
                    bool pFirst = Hash(float2(floor(a / 8), salt + 7.0)) < 0.5;
                    // Fora dos avanços, a linha ainda oscila ±1 texel por texel (trechos de 2 a 16x16), para nenhuma emenda ficar reta (pedra
                    // não é invadida nem na oscilação).
                    float wobble = floor(Hash(float2(a, salt + 13.0)) * 3) - 1;
                    if (fq == 4) wobble = min(wobble, 0);
                    if (fp == 4) wobble = max(wobble, 0);
                    offset = p > 0 && (pFirst || q == 0) ? p : (q > 0 ? -q : wobble);
                }
                return offset;
            }

            // Texel vizinho: anda 'step' texels a partir de (cell, inner) e passa para a célula ao lado quando sai da célula.
            void StepTexel(inout float2 cell, inout float2 inner, float2 step)
            {
                inner += step;
                float2 carry = floor(inner / _TexelsPerCell);
                cell += carry;
                inner -= carry * _TexelsPerCell;
            }

            // O texel é terra no desenho: célula sólida (distância com sinal positiva) e não entalhado pela borda lascada.
            bool SolidTexel(float2 cell, float2 inner)
            {
                if (IsVoid(cell))
                    return false;
                ChipStyle style;
                if (_ChipEnabled > 0.5 && SignedDistanceAt(cell) == 1.0 && ChipStyleOf(MaterialAt(cell), style))
                    return all(ChipFrom(cell, inner, style) == 0);
                return true;
            }

            // O texel é folha do transbordo da grama (mesmo teste e mesmo corte da quina roída do desenho).
            bool LeafTexel(float2 cell, float2 inner)
            {
                if (_OverhangMaterialId < 0)
                    return false;
                float id = MaterialAt(cell);
                float2 below = cell - float2(0, 1);
                if ((id != SkyId && id != EmptyId) || MaterialAt(below) != _OverhangMaterialId)
                    return false;
                float worldX = (_WindowOrigin.x + cell.x) * _TexelsPerCell + inner.x;
                float2 uv = float2((fmod(worldX, _OverhangSize) + 0.5) / _OverhangSize, (inner.y + 0.5) / _TexelsPerCell);
                if (SAMPLE_TEXTURE2D_LOD(_OverhangTex, sampler_OverhangTex, uv, 0).a < 0.5)
                    return false;
                ChipStyle style;
                return !(_ChipEnabled > 0.5 && SignedDistanceAt(below) == 1.0 && ChipStyleOf(MaterialAt(below), style)
                         && any(ChipFrom(below, float2(inner.x, _TexelsPerCell - 1), style) != 0));
            }

            // Contorno da terra: algum dos 4 texels vizinhos não é terra nem folha.
            bool SolidOutline(float2 cell, float2 inner)
            {
                const float2 steps[4] = { float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1) };
                [unroll] for (int i = 0; i < 4; i++)
                {
                    float2 c = cell, t = inner;
                    StepTexel(c, t, steps[i]);
                    if (!SolidTexel(c, t) && !LeafTexel(c, t))
                        return true;
                }
                return false;
            }

            // Contorno das folhas, por fora: o texel vazio encosta numa folha.
            bool LeafOutline(float2 cell, float2 inner)
            {
                const float2 steps[4] = { float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1) };
                [unroll] for (int i = 0; i < 4; i++)
                {
                    float2 c = cell, t = inner;
                    StepTexel(c, t, steps[i]);
                    if (LeafTexel(c, t))
                        return true;
                }
                return false;
            }

            // over por cima de base (alpha comum, sem pré-multiplicar).
            half4 OverlayOn(half4 over, half4 base)
            {
                half a = over.a + base.a * (1 - over.a);
                half3 rgb = (over.rgb * over.a + base.rgb * base.a * (1 - over.a)) / max(a, 0.0001);
                return half4(rgb, a);
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 cellColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float2 cellPos = input.uv * _WindowOrigin.zw;   // em células da janela
                float2 cell = floor(cellPos);
                if (_DebugSignedDistance > 0.5)
                    return half4(lerp(cellColor.rgb, SignedDistanceColor(SignedDistanceAt(cell)), 0.8), 1.0);
                float id = MaterialAt(cell);

                // Texel dentro da célula (0.._TexelsPerCell-1); a textura é lida no mundo para emendar entre células.
                float2 inner = floor(frac(cellPos) * _TexelsPerCell);
                // Com o zoom aberto (texel menor que um pixel da tela), a textura vira ruído: some aos poucos.
                float texelsPerPixel = max(fwidth(cellPos.x), fwidth(cellPos.y)) * _TexelsPerCell;
                half fade = saturate((3.0 - texelsPerPixel) * 0.5);

                half4 over = id == SkyId || id == EmptyId ? Overhang(cell, inner, fade) : half4(0, 0, 0, 0);
                // As folhas só nascem onde o topo do bloco de baixo existe: na coluna em que a borda lascada comeu o texel de
                // cima dele (quina roída), elas são cortadas, para não ficarem flutuando sobre o buraco.
                if (over.a > 0 && _ChipEnabled > 0.5 && fade > 0.5)
                {
                    float2 below = cell - float2(0, 1);
                    ChipStyle belowStyle;
                    if (SignedDistanceAt(below) == 1.0 && ChipStyleOf(MaterialAt(below), belowStyle)
                        && any(ChipFrom(below, float2(inner.x, _TexelsPerCell - 1), belowStyle) != 0))
                        over = half4(0, 0, 0, 0);
                }
                // Contorno das folhas, por fora: só nas células vazias com grama embaixo ou ao lado de uma (a folha vizinha
                // pode estar na célula da esquerda, da direita ou de cima, que também é vazia).
                bool outline = _OutlineEnabled > 0.5 && fade > 0.5;
                if (outline && over.a <= 0 && (id == SkyId || id == EmptyId) && SignedDistanceAt(cell) >= -2.0 && LeafOutline(cell, inner))
                    return half4(_OutlineColor.rgb, 1);
                if (id == SkyId)
                    return over.a > 0 ? OverlayOn(over, cellColor) : cellColor;
                // Borda lascada: o texel entalhado é desenhado como a vizinha vazia (céu, túnel ou fundo de sala), com a cor
                // dela e a sombra de canto mais forte, porque encosta na terra.
                bool chipped = false;
                ChipStyle chipStyle;
                // Só a célula encostada no vazio (distância 1) pode lascar: o miolo da terra sai com uma leitura só, sem as
                // 4 vizinhas nem as mordidas (a borda lascada dobrava o custo do shader com a tela cheia de terra).
                if (_ChipEnabled > 0.5 && fade > 0.5 && SignedDistanceAt(cell) == 1.0 && ChipStyleOf(id, chipStyle))
                {
                    float2 from = ChipFrom(cell, inner, chipStyle);
                    if (any(from != 0))
                    {
                        float2 voidCell = cell + from;
                        cellColor = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, (voidCell + 0.5) / _WindowOrigin.zw, 0);
                        id = MaterialAt(voidCell);
                        if (id == SkyId)
                            return cellColor;
                        chipped = true;
                    }
                }
                // Junção: nas células que a CPU marcou como fronteira entre famílias diferentes, cada uma das 4 fronteiras pode
                // ceder texels da beira para o material vizinho (BoundaryOffset), que entra com a arte e a cor da célula dele
                // (tom, umidade, marca). Só desenho: as células não mudam.
                float mergeFamily = MergeFamily(id);
                if (!chipped && _MergeEnabled > 0.5 && fade > 0.5 && mergeFamily >= 0 && MergeFlagAt(cell))
                {
                    float last = _TexelsPerCell - 1;
                    float2 world = (_WindowOrigin.xy + cell) * _TexelsPerCell + inner;
                    float2 lineBase = _WindowOrigin.xy + cell;
                    float2 from = float2(0, 0);
                    float2 n = cell + float2(0, 1);
                    float fn = MergeFamily(MaterialAt(n));
                    if (fn >= 0 && fn != mergeFamily && last - inner.y < BoundaryOffset(fn, mergeFamily, world.x, lineBase.y + 1, true))
                        from = float2(0, 1);
                    n = cell - float2(0, 1);
                    fn = MergeFamily(MaterialAt(n));
                    if (all(from == 0) && fn >= 0 && fn != mergeFamily
                        && inner.y < -BoundaryOffset(mergeFamily, fn, world.x, lineBase.y, true))
                        from = float2(0, -1);
                    n = cell - float2(1, 0);
                    fn = MergeFamily(MaterialAt(n));
                    if (all(from == 0) && fn >= 0 && fn != mergeFamily
                        && inner.x < BoundaryOffset(fn, mergeFamily, world.y, lineBase.x, false))
                        from = float2(-1, 0);
                    n = cell + float2(1, 0);
                    fn = MergeFamily(MaterialAt(n));
                    if (all(from == 0) && fn >= 0 && fn != mergeFamily
                        && last - inner.x < -BoundaryOffset(mergeFamily, fn, world.y, lineBase.x + 1, false))
                        from = float2(1, 0);
                    if (any(from != 0))
                    {
                        n = cell + from;
                        id = MaterialAt(n);
                        cellColor = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, (n + 0.5) / _WindowOrigin.zw, 0);
                    }
                }
                // Fundo de sala construída: linhas pares depois da parede de fundo (as ímpares são os blocos).
                bool roomBack = id > _BackTileId && fmod(id - _BackTileId, 2.0) < 0.5;
                bool solid = id != EmptyId && !roomBack && !chipped;
                float row = solid || roomBack ? id : _BackTileId;
                float3 texel = SAMPLE_TEXTURE2D_LOD(_TileAtlas, sampler_TileAtlas, AtlasUv(row, inner, cell), 0).rgb;
                half detailStrength = solid ? _DetailStrength : _BackDetail;
                // Ajuste da CPU sobre a arte: a cor da célula dividida pela referência da linha.
                float3 tone = cellColor.rgb / max(RowColor(row, 0), 0.0005);

                // Sombra de canto: só nas células vazias encostadas na terra (d = -1). Com o zoom aberto ela passa a valer
                // para a célula inteira (mais fraca), para o mapa afastado não ficar chapado.
                half3 light = half3(1, 1, 1);
                if (chipped)
                    light = WallLight(0);
                else if (!solid && SignedDistanceAt(cell) == -1.0)
                    light = lerp((half3)(1.0 - 0.5 * _WallShadowStrength), WallLight(TexelsToSolid(cell, inner)), fade);

                // Luz do mapa, menos na terra com marca por cima: essa já vem iluminada da CPU, para a marca não escurecer.
                float4 data = SAMPLE_TEXTURE2D_LOD(_CellMaterials, sampler_CellMaterials, (cell + 0.5) / _WindowOrigin.zw, 0);
                if (data.a < 0.75)
                    light *= MapLight(cell, inner, solid);
                float3 surface = lerp(RowColor(row, 1), texel, detailStrength * fade) * tone;
                half emission = id == _GlowMaterialId ? _GlowEmission : 1.0;
                half4 result = half4(surface * light * emission, cellColor.a);
                // Contorno da terra: só as células a até 2 da borda podem encostar num texel vazio (as lascas ficam na de
                // distância 1 e podem chegar perto da de distância 2 nas quinas).
                float sd = SignedDistanceAt(cell);
                if (outline && solid && sd > 0 && sd <= 2.0 && SolidOutline(cell, inner))
                    result.rgb = _OutlineColor.rgb;
                // Item guardado: objeto com a arte da linha dele sobre o fundo, 1 texel de folga e contorno por dentro. Com o
                // zoom aberto, a média da linha no lugar da arte, como a terra.
                if (!solid && !chipped)
                {
                    float item = ItemAt(cell);
                    float last = _TexelsPerCell - 1;
                    // Item com sprite próprio (minério solto, 2026-10-23): o mesmo sprite da boca da formiga, célula inteira,
                    // recortado pelo alfa, sobre o fundo e com a luz do vão.
                    float sprite = item - (ItemSpriteBase - 1);
                    if (sprite >= 0 && sprite < _ItemSpriteCount)
                    {
                        float2 spriteUv = (float2(sprite * _TexelsPerCell + inner.x, inner.y) + 0.5)
                                          / float2(_ItemSpriteCount * _TexelsPerCell, _TexelsPerCell);
                        half4 spriteTexel = SAMPLE_TEXTURE2D_LOD(_ItemSprites, sampler_ItemSprites, spriteUv, 0);
                        if (spriteTexel.a > 0.5)
                            result.rgb = spriteTexel.rgb * (data.a < 0.75 ? MapLight(cell, inner, false) : half3(1, 1, 1));
                    }
                    else if (item >= 0 && all(inner >= 1) && all(inner <= last - 1))
                    {
                        if (outline && (inner.x == 1 || inner.y == 1 || inner.x == last - 1 || inner.y == last - 1))
                            result.rgb = _OutlineColor.rgb;
                        else
                        {
                            float3 itemTexel = SAMPLE_TEXTURE2D_LOD(_TileAtlas, sampler_TileAtlas, AtlasUv(item, inner, cell), 0).rgb;
                            float3 itemSurface = lerp(RowColor(item, 1), itemTexel, _DetailStrength * fade);
                            // A luz é a do vão (com a penumbra do formigueiro), não a da terra: o item está na frente da parede de fundo.
                            result.rgb = itemSurface * (data.a < 0.75 ? MapLight(cell, inner, false) : half3(1, 1, 1));
                        }
                    }
                }
                return over.a > 0 ? OverlayOn(over, result) : result;
            }
            ENDHLSL
        }
    }
}
