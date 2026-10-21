// Terreno do formigário com cada célula da grade desenhada como um bloco de 16x16 texels.
// _MainTex: cor de cada célula (1 texel por célula, montada na CPU pelo AntFarmView: tom, marcas, salas, água), sem a luz.
// _CellMaterials: 1 texel por célula (RGBA32). R = material (255 = céu, sem textura); G = 128 + distância de xadrez com sinal
// até a borda terra/vazio (1..4 na terra, -1..-4 no vazio, 4 = 4 ou mais; AntFarmView.SignedDistanceRange); B = nível de
// luz (0..255 = 0..AntFarmView.LightLevels); A = 255 quando a cor já vem iluminada da CPU (terra com marca por cima).
// _TileAtlas: uma textura repetível de _TileSize texels por material, empilhadas na vertical (linha = id do material).
// A textura do material é lida em coordenadas de mundo (texel = célula * 16), então células vizinhas do mesmo
// material formam uma superfície contínua; a borda só aparece onde o material muda.
// Parede de fundo (etapa 3 do documento 40): no túnel, usa a textura de _BackTileId e escurece em degraus perto da terra
// (sombra de canto), em texels inteiros (xadrez), então nada fica mais fino que o pixel de arte. A terra não tem contorno
// (decisão de 2026-10-21). Valores provisórios até a paleta mestra.
// Luz (etapa 6; substitui a névoa): só entra de fora. O nível de cada célula é interpolado entre as 4 células mais próximas
// na posição do texel e cortado em _LightSteps degraus, para virar faixas de pixel art em vez de degradê; o túnel não
// fica abaixo de _InteriorAmbient. O céu não recebe luz.
Shader "Incrementant/TerrainTiles"
{
    Properties
    {
        [PerRendererData] _MainTex ("Cores das células", 2D) = "white" {}
        _CellMaterials ("Material das células", 2D) = "black" {}
        _TileAtlas ("Atlas de materiais", 2D) = "gray" {}
        _TileCount ("Quantidade de materiais no atlas", Float) = 15
        _TileSize ("Lado da textura de cada material, em texels", Float) = 64
        _TexelsPerCell ("Texels por célula", Float) = 16
        _WindowOrigin ("Janela: xy = célula (0,0) no mundo, zw = largura e altura em células", Vector) = (0, 0, 0, 0)
        _DetailStrength ("Força da textura", Range(0, 2)) = 1
        _BackTileId ("Material do atlas usado como parede de fundo do túnel", Float) = 1
        _BackDetail ("Força da textura da parede de fundo", Range(0, 2)) = 0.6
        _WallShadowStrength ("Sombra de canto: escurecimento junto da terra", Range(0, 1)) = 0.45
        _WallShadowWidth ("Sombra de canto: alcance, em texels", Range(1, 16)) = 6
        _WallShadowSteps ("Sombra de canto: degraus", Range(1, 6)) = 3
        _LightSteps ("Degraus de luz (o AntFarmView sobrescreve)", Float) = 5
        _InteriorAmbient ("Brilho mínimo do túnel no breu (o AntFarmView sobrescreve)", Range(0, 1)) = 0.08
        _LightTint ("Cor da luz cheia", Color) = (1.06, 1, 0.9, 1)
        _ShadowTint ("Cor da penumbra", Color) = (0.8, 0.85, 1, 1)
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
            TEXTURE2D(_TileAtlas); SAMPLER(sampler_TileAtlas);

            CBUFFER_START(UnityPerMaterial)
                float _TileCount;
                float _TileSize;
                float _TexelsPerCell;
                float4 _WindowOrigin;
                float _DetailStrength;
                float _BackTileId;
                float _BackDetail;
                float _WallShadowStrength;
                float _WallShadowWidth;
                float _WallShadowSteps;
                float _LightSteps;
                float _InteriorAmbient;
                float4 _LightTint;
                float4 _ShadowTint;
                float _DebugSignedDistance;
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

            float MaterialAt(float2 cell)
            {
                float2 uv = (cell + 0.5) / _WindowOrigin.zw;
                return round(SAMPLE_TEXTURE2D_LOD(_CellMaterials, sampler_CellMaterials, uv, 0).r * 255.0);
            }

            // Distância com sinal até a borda terra/vazio: > 0 na terra, < 0 no vazio (túnel, céu).
            float SignedDistanceAt(float2 cell)
            {
                float2 uv = (cell + 0.5) / _WindowOrigin.zw;
                return round(SAMPLE_TEXTURE2D_LOD(_CellMaterials, sampler_CellMaterials, uv, 0).g * 255.0) - 128.0;
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

            // Luz no centro do texel: interpolada entre as 4 células mais próximas e cortada em _LightSteps degraus.
            half SteppedLight(float2 cell, float2 inner)
            {
                float2 p = cell + (inner + 0.5) / _TexelsPerCell - 0.5;
                float2 c0 = floor(p);
                float2 f = p - c0;
                float bottom = lerp(LightAt(c0), LightAt(c0 + float2(1, 0)), f.x);
                float top = lerp(LightAt(c0 + float2(0, 1)), LightAt(c0 + float2(1, 1)), f.x);
                return round(lerp(bottom, top, f.y) * _LightSteps) / _LightSteps;
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

            half4 frag(Varyings input) : SV_Target
            {
                half4 cellColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float2 cellPos = input.uv * _WindowOrigin.zw;   // em células da janela
                float2 cell = floor(cellPos);
                if (_DebugSignedDistance > 0.5)
                    return half4(lerp(cellColor.rgb, SignedDistanceColor(SignedDistanceAt(cell)), 0.8), 1.0);
                float id = MaterialAt(cell);
                if (id == SkyId)
                    return cellColor;

                // Texel dentro da célula (0.._TexelsPerCell-1); a textura é lida no mundo para emendar entre células.
                float2 inner = floor(frac(cellPos) * _TexelsPerCell);
                bool solid = id != EmptyId;
                half detail = SAMPLE_TEXTURE2D_LOD(_TileAtlas, sampler_TileAtlas, AtlasUv(solid ? id : _BackTileId, inner, cell), 0).r * 2.0;
                half detailStrength = solid ? _DetailStrength : _BackDetail;

                // Com o zoom aberto (texel menor que um pixel da tela), a textura vira ruído: some aos poucos.
                float texelsPerPixel = max(fwidth(cellPos.x), fwidth(cellPos.y)) * _TexelsPerCell;
                half fade = saturate((3.0 - texelsPerPixel) * 0.5);

                // Sombra de canto: só nas células vazias encostadas na terra (d = -1). Com o zoom aberto ela passa a valer
                // para a célula inteira (mais fraca), para o mapa afastado não ficar chapado.
                half3 light = half3(1, 1, 1);
                if (!solid && SignedDistanceAt(cell) == -1.0)
                    light = lerp((half3)(1.0 - 0.5 * _WallShadowStrength), WallLight(TexelsToSolid(cell, inner)), fade);

                // Luz do mapa, menos na terra com marca por cima: essa já vem iluminada da CPU, para a marca não escurecer.
                float4 data = SAMPLE_TEXTURE2D_LOD(_CellMaterials, sampler_CellMaterials, (cell + 0.5) / _WindowOrigin.zw, 0);
                if (data.a < 0.5)
                {
                    half q = SteppedLight(cell, inner);
                    if (!solid)
                        q = max(q, _InteriorAmbient);
                    light *= q * lerp(_ShadowTint.rgb, _LightTint.rgb, q);
                }
                half factor = lerp(1.0, detail, detailStrength * fade);
                return half4(cellColor.rgb * factor * light, cellColor.a);
            }
            ENDHLSL
        }
    }
}
