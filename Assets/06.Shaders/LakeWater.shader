Shader "UPlayGround/Environment/Lake Water"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.12, 0.46, 0.43, 1)
        _DeepColor("Deep Color", Color) = (0.025, 0.19, 0.25, 1)
        _UnderwaterColor("Underside Color", Color) = (0.08, 0.31, 0.34, 1)
        _DepthDistance("Depth Color Distance (m)", Float) = 5
        _Absorption("Depth Absorption", Range(0.01, 3)) = 0.45
        _SurfaceOpacity("Minimum Surface Opacity", Range(0, 1)) = 0.3
        _UnderwaterOpacity("Underside Opacity", Range(0, 1)) = 0.85
        _EdgeDistance("Intersection Softness (m)", Float) = 0.35
        [Normal] _NormalMap("Wave Normal", 2D) = "bump" {}
        _WaveScale("Wave Scale (cycles/m)", Float) = 0.18
        _WaveStrength("Wave Strength", Range(0, 1)) = 0.18
        _WaveVelocity("Wave Velocities (XY / ZW)", Vector) = (0.025, 0.012, -0.016, 0.021)
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 0.4
        _Smoothness("Smoothness", Range(0, 1)) = 0.88
        _FoamColor("Shore Foam Color", Color) = (0.65, 0.82, 0.77, 1)
        _FoamWidth("Shore Foam Width (m)", Float) = 0.45
        _FoamStrength("Shore Foam Strength", Range(0, 1)) = 0.28
        _FoamScale("Foam Scale", Float) = 1.8
        _FoamSpeed("Foam Speed", Float) = 0.35
        _ShoreFade("UV Shore Fade (0 disables)", Range(0, 0.5)) = 0
        _Recession("Pool Recession", Range(0, 0.9)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "LakeWater"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor, _DeepColor, _UnderwaterColor, _FoamColor;
                float4 _WaveVelocity;
                float _DepthDistance, _Absorption, _EdgeDistance, _WaveScale;
                float _FoamWidth, _FoamScale, _FoamSpeed;
                half _SurfaceOpacity, _UnderwaterOpacity, _WaveStrength;
                half _ReflectionStrength, _Smoothness, _FoamStrength;
                float _ShoreFade, _Recession;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                bool isAbove = IS_FRONT_VFACE(face, true, false);
                // 깊이 텍스처와 역 VP에는 같은 렌더 타깃 좌표가 필요하다.
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                float rawDepth = SampleSceneDepth(screenUV);
                #if !UNITY_REVERSED_Z
                    rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, rawDepth);
                #endif
                // 월드 위치를 복원해 정사영과 원근 카메라에서 같은 수심을 사용한다.
                float3 scenePosition = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float depth = max(0, input.positionWS.y - scenePosition.y);
                float contactDistance = distance(scenePosition, input.positionWS);
                half edge = saturate(contactDistance / max(_EdgeDistance, 0.001));
                half depthBlend = 1 - exp2(-depth / max(_DepthDistance, 0.001));

                float2 waveUV = input.positionWS.xz * _WaveScale;
                half3 waveA = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap,
                    waveUV + _Time.y * _WaveVelocity.xy));
                half3 waveB = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap,
                    waveUV * 0.73 + _Time.y * _WaveVelocity.zw));
                half3 normal = normalize(half3((waveA.x + waveB.x) * _WaveStrength,
                    1, (waveA.y + waveB.y) * _WaveStrength));
                normal *= isAbove ? 1 : -1;
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = pow(1 - saturate(dot(normal, view)), 5);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 ambient = SampleSH(normal);
                half3 lightColor = ambient + sun.color * (saturate(dot(normal, sun.direction)) * sun.shadowAttenuation);
                half3 waterColor = isAbove ? lerp(_ShallowColor.rgb, _DeepColor.rgb, depthBlend) : _UnderwaterColor.rgb;
                half3 color = waterColor * lightColor;
                half3 reflection = GlossyEnvironmentReflection(reflect(-view, normal), 1 - _Smoothness, 1);
                color = lerp(color, reflection, _ReflectionStrength * (0.04 + 0.96 * fresnel));
                half3 halfway = SafeNormalize(view + sun.direction);
                half glint = pow(saturate(dot(normal, halfway)), exp2(3 + _Smoothness * 7));
                color += sun.color * sun.shadowAttenuation * glint * _ReflectionStrength;

                float foamPattern = sin(depth * _FoamScale - _Time.y * _FoamSpeed
                    + (waveA.x + waveB.y) * 4);
                half foam = (1 - smoothstep(0, max(_FoamWidth, 0.001), depth))
                    * smoothstep(-0.35, 0.65, foamPattern) * _FoamStrength;
                foam *= isAbove ? 1 : 0;
                color = lerp(color, _FoamColor.rgb * lightColor, foam);
                half opacity = lerp(_SurfaceOpacity, 1, 1 - exp2(-depth * _Absorption));
                opacity = max(opacity, fresnel * _ReflectionStrength);
                // 아랫면은 바닥 수심으로 페이드하면 사라지므로 별도의 투명도를 사용한다.
                opacity = isAbove ? saturate(opacity + foam) : _UnderwaterOpacity;
                // 원형 UV를 가진 작은 물 고임만 경계를 후퇴시켜 수평 수면을 유지한다.
                half shore = _ShoreFade > 0
                    ? 1 - smoothstep(1 - _Recession - _ShoreFade, 1 - _Recession,
                        length(input.uv * 2 - 1))
                    : 1;
                return half4(MixFog(color, input.fogFactor), opacity * edge * shore);
            }
            ENDHLSL
        }
    }
}
