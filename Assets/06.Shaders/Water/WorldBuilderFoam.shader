Shader "UPlayGround/WorldBuilder/Foam"
{
    Properties
    {
        _BaseColor("물거품 색", Color) = (0.75, 0.88, 0.87, 0.75)
        _EdgeSoftness("가장자리 감쇠", Range(0.05, 0.8)) = 0.4
        _FlowSpeed("움직임 속도", Range(0, 4)) = 0.8
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _EdgeSoftness;
                float _FlowSpeed;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // 접합 ribbon의 U=0..1, V=-1..1에서 사각 모서리를 부드럽게 지운다.
                float2 p = float2(input.uv.x * 2 - 1, input.uv.y);
                float edge = 1 - smoothstep(1 - _EdgeSoftness, 1, length(p));
                float pattern = 0.72 + 0.18 * sin(p.x * 21 + sin(p.y * 13) + _Time.y * _FlowSpeed)
                    + 0.1 * sin(p.y * 29 - _Time.y * _FlowSpeed * 1.3);
                return half4(_BaseColor.rgb, _BaseColor.a * edge * pattern);
            }
            ENDHLSL
        }
    }
}
