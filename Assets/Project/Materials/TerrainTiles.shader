// Terreno do formigário com cada célula da grade desenhada como um bloco de 16x16 texels.
// _MainTex: cor final de cada célula (1 texel por célula, montada na CPU pelo AntFarmView: tom, névoa, marcas, água).
// _CellMaterials: material de cada célula (R8, 1 texel por célula; 255 = céu, sem textura).
// _TileAtlas: uma textura repetível de _TileSize texels por material, empilhadas na vertical (linha = id do material).
// A textura do material é lida em coordenadas de mundo (texel = célula * 16), então células vizinhas do mesmo
// material formam uma superfície contínua; a borda só aparece onde o material muda.
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
        _EdgeDarkening ("Escurecimento da borda com o vazio", Range(0, 1)) = 0.5
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
                float _EdgeDarkening;
            CBUFFER_END

            static const float SkyId = 255.0;
            static const float EmptyId = 0.0;

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

            bool IsOpen(float id) { return id == EmptyId || id == SkyId; }

            half4 frag(Varyings input) : SV_Target
            {
                half4 cellColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float2 cellPos = input.uv * _WindowOrigin.zw;   // em células da janela
                float2 cell = floor(cellPos);
                float id = MaterialAt(cell);
                if (id == SkyId)
                    return cellColor;

                // Texel dentro da célula (0.._TexelsPerCell-1) e no mundo, para a textura do material emendar entre células.
                float2 inner = floor(frac(cellPos) * _TexelsPerCell);
                float2 worldTexel = (_WindowOrigin.xy + cell) * _TexelsPerCell + inner;
                float2 tileTexel = fmod(worldTexel, _TileSize);
                float2 atlasUv = float2((tileTexel.x + 0.5) / _TileSize, (id * _TileSize + tileTexel.y + 0.5) / (_TileCount * _TileSize));
                half detail = SAMPLE_TEXTURE2D_LOD(_TileAtlas, sampler_TileAtlas, atlasUv, 0).r * 2.0;

                // Contorno de 1 texel no lado sólido de uma borda com o vazio (túnel ou céu).
                half edge = 0;
                if (!IsOpen(id))
                {
                    float last = _TexelsPerCell - 1;
                    if (inner.x == 0 && IsOpen(MaterialAt(cell + float2(-1, 0)))) edge = 1;
                    if (inner.x == last && IsOpen(MaterialAt(cell + float2(1, 0)))) edge = 1;
                    if (inner.y == 0 && IsOpen(MaterialAt(cell + float2(0, -1)))) edge = 1;
                    if (inner.y == last && IsOpen(MaterialAt(cell + float2(0, 1)))) edge = 1;
                }

                // Com o zoom aberto (texel menor que um pixel da tela), a textura vira ruído: some aos poucos.
                float texelsPerPixel = max(fwidth(cellPos.x), fwidth(cellPos.y)) * _TexelsPerCell;
                half fade = saturate((3.0 - texelsPerPixel) * 0.5);

                half factor = lerp(1.0, detail, _DetailStrength * fade) * (1.0 - edge * _EdgeDarkening * fade);
                return half4(cellColor.rgb * factor, cellColor.a);
            }
            ENDHLSL
        }
    }
}
