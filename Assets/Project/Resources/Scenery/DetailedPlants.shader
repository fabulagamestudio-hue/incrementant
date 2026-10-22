// Arte compartilhada entre plantas; cada instância atualiza só uma máscara de um texel por célula.
Shader "Incrementant/DetailedPlants"
{
    Properties
    {
        [PerRendererData] _MainTex ("Arte", 2D) = "white" {}
        [PerRendererData] _CellMask ("Estado das células", 2D) = "white" {}
        _MarkColor ("Cor da marca", Color) = (1, 0.84, 0.35, 1)
        _MarkStrength ("Força da marca", Float) = 0.5
        _MaskMapping ("Tamanho e pivô em células", Vector) = (1, 1, 0, 0)
        _MaskBounds ("Origem e tamanho da máscara", Vector) = (0, 0, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_CellMask); SAMPLER(sampler_CellMask);
            CBUFFER_START(UnityPerMaterial)
                float4 _MaskMapping;
                float4 _MaskBounds;
                float4 _MarkColor;
                float _MarkStrength;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                float2 cell = floor(input.uv * _MaskMapping.xy - _MaskMapping.zw + float2(0.5, 0));
                float2 maskUV = (cell - _MaskBounds.xy + 0.5) / _MaskBounds.zw;
                half4 mask = SAMPLE_TEXTURE2D(_CellMask, sampler_CellMask, maskUV);
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                color.rgb = lerp(color.rgb, _MarkColor.rgb, mask.r * _MarkStrength);
                color.a *= mask.a;
                return color;
            }
            ENDHLSL
        }
    }
}
