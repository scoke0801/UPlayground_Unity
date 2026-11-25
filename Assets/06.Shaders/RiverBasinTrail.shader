Shader "UPlayground/Environment/River Basin Trail"
{
    Properties
    {
        _Surface ("지면", 2D) = "white" {}
        _Gravel ("잔돌과 흙", 2D) = "gray" {}
        _Tint ("지면 색", Color) = (1, 1, 1, 1)
        _TileMeters ("지면 타일 간격", Float) = 2
        _GravelMix ("잔돌 비율", Range(0, 1)) = 0.3
        _Saturation ("채도", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-110"
        }

        Pass
        {
            Name "Trail"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex TrailVertex
            #pragma fragment TrailFragment
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Surface);
            SAMPLER(sampler_Surface);
            TEXTURE2D(_Gravel);
            SAMPLER(sampler_Gravel);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _TileMeters;
                half _GravelMix;
                half _Saturation;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half2 fogAndCoverage : TEXCOORD2;
            };

            Varyings TrailVertex(Attributes input)
            {
                Varyings o;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = position.positionCS;
                o.positionWS = position.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fogAndCoverage = half2(ComputeFogFactor(position.positionCS.z), input.color.a);
                return o;
            }

            half4 TrailFragment(Varyings input) : SV_Target
            {
                float2 ground = input.positionWS.xz;
                float2 uv = ground / max(_TileMeters, 0.1);
                half3 soil = SAMPLE_TEXTURE2D(_Surface, sampler_Surface, uv).rgb;
                half3 coarse = SAMPLE_TEXTURE2D(_Gravel, sampler_Gravel, uv * 0.77 + 0.29).rgb;
                half macro = SAMPLE_TEXTURE2D(_Surface, sampler_Surface, ground * 0.061).g;
                half gravel = saturate(_GravelMix * (0.65h + macro));
                half3 surface = lerp(soil, coarse, gravel);
                half3 albedo = lerp(Luminance(surface).xxx, surface, _Saturation);
                albedo *= _Tint.rgb * lerp(0.86h, 1.14h, macro);
                half coverage = input.fogAndCoverage.y;
                half brokenEdge = (macro - 0.45h) * 0.38h * coverage;
                half alpha = smoothstep(0.02h, 0.91h, coverage + brokenEdge);

                half3 normal = normalize(input.normalWS);
                // 한 삼각형이 캐스케이드 경계를 넘을 수 있다.
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS + normal * 0.10);
                Light sun = GetMainLight(shadowCoord);
                half lambert = saturate(dot(normal, sun.direction));
                half3 light = max(SampleSH(normal), 0.08h) + sun.color * (lambert * sun.shadowAttenuation);
#if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light fixture = GetAdditionalLight(lightIndex, inputData.positionWS);
                    half diffuse = saturate(dot(normal, fixture.direction));
                    light += fixture.color * (diffuse * fixture.distanceAttenuation);
                LIGHT_LOOP_END
#endif
                return half4(MixFog(albedo * light, input.fogAndCoverage.x), alpha);
            }
            ENDHLSL
        }
    }
}
