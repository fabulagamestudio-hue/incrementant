// Cenário da superfície e plantas colhíveis a 16 texels por célula (etapa 10 do documento 40), como o terreno.
// A arte continua em células (cada pixel dos PNGs de Art/Scenery = 1 célula, desenhado em _ArtPixelsPerCell texels da
// textura da camada); este shader desenha cada célula num bloco de _DetailTexels x _DetailTexels. Dentro da arte, a
// passagem de uma célula para a vizinha pode virar um pontilhado ordenado (Bayer 4x4) entre as duas cores, na faixa
// _BlendWidth perto da borda da célula (desligado por padrão: em teste, virou linhas de pontos nas bordas de contraste): cada texel fica com a cor de uma das células vizinhas, então nada sai da paleta e
// a silhueta não muda (só entram vizinhas opacas). Na silhueta, contorno de 1 texel na cor _OutlineColor (por dentro,
// para não engordar a forma), realce nos texels de cima/esquerda que dão para o vazio, sombra nos de baixo/direita, e um
// granulado em manchas de 2x2 texels. A posição é presa à grade de 16, então
// os grãos menores que uma célula (desenhados a 1/6) também caem nela. As cores vêm da paleta mestra nos PNGs; realce,
// sombra e granulado só clareiam ou escurecem, como a luz do terreno.
Shader "Incrementant/SceneryCells"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura da camada", 2D) = "white" {}
        _ArtPixelsPerCell ("Texels da textura por célula (SurfaceSceneryPainter.PixelsPerCell)", Float) = 6
        _DetailTexels ("Texels desenhados por célula", Float) = 16
        _OutlineColor ("Contorno (cor 0 da paleta mestra)", Color) = (0.106, 0.094, 0.082, 1)
        _OutlineStrength ("Força do contorno", Range(0, 1)) = 1
        _Highlight ("Realce de cima/esquerda", Range(0, 1)) = 0.22
        _Shade ("Sombra de baixo/direita", Range(0, 1)) = 0.3
        _BevelTexels ("Largura do realce e da sombra, em texels", Range(1, 4)) = 2
        _Grain ("Granulado (manchas de 2x2 texels)", Range(0, 0.3)) = 0.08
        _BlendWidth ("Pontilhado entre células: largura (0 = desligado, 1 = gradiente na célula toda)", Range(0, 1)) = 0
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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                float _ArtPixelsPerCell;
                float _DetailTexels;
                float4 _OutlineColor;
                float _OutlineStrength;
                float _Highlight;
                float _Shade;
                float _BevelTexels;
                float _Grain;
                float _BlendWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            // Cor no centro do texel de detalhe (d, em texels de detalhe a partir da origem da textura).
            half4 SampleDetail(float2 d)
            {
                float2 texel = (d + 0.5) * (_ArtPixelsPerCell / _DetailTexels);
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, texel * _MainTex_TexelSize.xy, 0);
            }

            float Opaque(float2 d)
            {
                return SampleDetail(d).a > 0.5 ? 1.0 : 0.0;
            }

            // Cor no centro da célula (cell em células a partir da origem da textura).
            half4 SampleCell(float2 cell)
            {
                float2 texel = (cell + 0.5) * _ArtPixelsPerCell;
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, texel * _MainTex_TexelSize.xy, 0);
            }

            // Limiar de Bayer 4x4 do texel, em (0, 1).
            static const float BayerMatrix[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            float Bayer(float2 d)
            {
                uint2 q = (uint2)(int2)d & 3u;
                return (BayerMatrix[q.y * 4u + q.x] + 0.5) / 16.0;
            }

            // Pontilhado entre as 4 células mais próximas: cada texel pega a cor de uma delas, com a chance dada pelo peso
            // bilinear (comprimido para a faixa _BlendWidth perto da borda). Células vazias não entram.
            half4 Blend(float2 d, half4 own)
            {
                float2 g = (d + 0.5) / _DetailTexels - 0.5;   // em células, a partir do centro da célula 0
                float2 i = floor(g);
                float2 f = g - i;
                float w = max(_BlendWidth, 0.001);
                f = saturate((f - 0.5) / w + 0.5);
                half4 c00 = SampleCell(i);
                half4 c10 = SampleCell(i + float2(1, 0));
                half4 c01 = SampleCell(i + float2(0, 1));
                half4 c11 = SampleCell(i + float2(1, 1));
                float w00 = (1 - f.x) * (1 - f.y) * step(0.5, c00.a);
                float w10 = f.x * (1 - f.y) * step(0.5, c10.a);
                float w01 = (1 - f.x) * f.y * step(0.5, c01.a);
                float w11 = f.x * f.y * step(0.5, c11.a);
                float sum = w00 + w10 + w01 + w11;
                if (sum <= 0.0)
                    return own;
                float r = Bayer(d) * sum;
                if (r < w00) return c00;
                if (r < w00 + w10) return c10;
                if (r < w00 + w10 + w01) return c01;
                return c11;
            }

            float Hash(float2 p)
            {
                uint2 q = (uint2)(int2)p;
                uint h = q.x * 374761393u + q.y * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535.0;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Texel de detalhe (grade de _DetailTexels por célula) sob o fragmento.
                float2 d = floor(input.uv * _MainTex_TexelSize.zw * (_DetailTexels / _ArtPixelsPerCell));
                half4 c = SampleDetail(d);
                if (c.a <= 0.0)
                    return half4(0, 0, 0, 0);
                // Só nas células lisas (a arte de 1 pixel por célula): o que tem detalhe menor que a célula (grãos) fica
                // como está.
                half4 center = SampleCell(floor((d + 0.5) / _DetailTexels));
                if (_BlendWidth > 0.0 && c.a >= 0.5 && all(abs(center - c) < 0.002))
                {
                    half4 blended = Blend(d, c);
                    c = half4(blended.rgb, c.a);
                }
                c *= input.color;
                if (c.a < 0.5)
                    return c;   // alfa parcial (sombras compostas): sem relevo

                // Contorno: algum vizinho direto é vazio.
                float edge = (1.0 - Opaque(d + float2(1, 0))) + (1.0 - Opaque(d - float2(1, 0)))
                           + (1.0 - Opaque(d + float2(0, 1))) + (1.0 - Opaque(d - float2(0, 1)));
                if (edge > 0.0 && _OutlineStrength > 0.0)
                    return half4(lerp(c.rgb, _OutlineColor.rgb, _OutlineStrength), c.a);

                // Realce e sombra: o vazio a até _BevelTexels texels para cima/esquerda (y cresce para cima) ou baixo/direita.
                half factor = 1.0;
                [unroll] for (int i = 2; i <= 4; i++)
                {
                    if (i > _BevelTexels + 1)
                        break;
                    if (Opaque(d + float2(0, i)) < 0.5 || Opaque(d - float2(i, 0)) < 0.5)
                    {
                        factor = 1.0 + _Highlight;
                        break;
                    }
                    if (Opaque(d - float2(0, i)) < 0.5 || Opaque(d + float2(i, 0)) < 0.5)
                    {
                        factor = 1.0 - _Shade;
                        break;
                    }
                }
                factor *= 1.0 + (Hash(floor(d * 0.5)) - 0.5) * 2.0 * _Grain;
                return half4(c.rgb * factor, c.a);
            }
            ENDHLSL
        }
    }
}
