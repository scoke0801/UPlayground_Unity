Shader "UPlayground/Environment/River Basin Trail"
{
    Properties
    {
        _Surface ("지면", 2D) = "white" {}
        _Gravel ("잔돌과 흙", 2D) = "gray" {}
        [Normal] _GravelNormal ("잔돌 요철", 2D) = "bump" {}
        _Tint ("지면 색", Color) = (1, 1, 1, 1)
        _TileMeters ("지면 타일 간격", Float) = 2
        _GravelMix ("잔돌 비율", Range(0, 1)) = 0.3
        _Saturation ("채도", Range(0, 1)) = 0.6
        _VertexSurface ("정점별 다짐과 퇴적", Range(0, 1)) = 0
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
            TEXTURE2D(_GravelNormal);
            SAMPLER(sampler_GravelNormal);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _TileMeters;
                half _GravelMix;
                half _Saturation;
                half _VertexSurface;
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
                half3 surfaceWeights : TEXCOORD3;
            };

            Varyings TrailVertex(Attributes input)
            {
                Varyings o;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = position.positionCS;
                o.positionWS = position.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.fogAndCoverage = half2(ComputeFogFactor(position.positionCS.z), input.color.a);
                o.surfaceWeights = input.color.rgb;
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
                half3 normal = normalize(input.normalWS);
                if (_VertexSurface > 0.0h)
                {
                    half worn = input.surfaceWeights.r;
                    half deposits = input.surfaceWeights.g;
                    half detail = SAMPLE_TEXTURE2D(_Gravel, sampler_Gravel, ground * 1.37).g;
                    half stoneMix = saturate(deposits * lerp(0.16h, 2.0h, detail));
                    half3 compacted = lerp(soil, coarse, saturate(worn * 0.78h + stoneMix));
                    compacted *= _Tint.rgb * lerp(0.83h, 1.08h, input.surfaceWeights.b)
                        * lerp(0.83h, 1.20h, detail);
                    albedo = lerp(albedo, compacted, _VertexSurface);
                    coverage *= lerp(0.72h, 1.22h, detail);
                    half2 relief = UnpackNormal(SAMPLE_TEXTURE2D(_GravelNormal, sampler_GravelNormal, uv)).xy;
                    half3 tangent = normalize(half3(1.0h, 0.0h, 0.0h) - normal * normal.x);
                    half3 bitangent = cross(tangent, normal);
                    normal = normalize(normal + (tangent * relief.x + bitangent * relief.y)
                        * _VertexSurface * saturate(worn + deposits) * 0.42h);
                }
                half brokenEdge = (macro - 0.45h) * 0.38h * coverage;
                half alpha = smoothstep(0.02h, 0.91h, coverage + brokenEdge);

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
