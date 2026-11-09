Shader "UPlayGround/GlidingSpeedLines"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 screen : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.screen = output.positionCS.xy / output.positionCS.w;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // 이동 방향과 무관하게 중앙의 착지 지점과 캐릭터를 가리지 않는다.
                input.color.a *= smoothstep(0.55, 0.75, length(input.screen));
                return input.color;
            }
            ENDHLSL
        }
    }
}

