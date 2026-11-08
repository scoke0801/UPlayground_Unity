Shader "UPlayGround/Environment/Day Sky"
{
    Properties
    {
        _ZenithColor("천정 색", Color) = (0.10, 0.35, 0.70, 1)
        _MiddleColor("중간 하늘 색", Color) = (0.32, 0.63, 0.88, 1)
        _HorizonColor("수평선 색", Color) = (0.80, 0.88, 0.90, 1)
        _GroundColor("지평선 아래 색", Color) = (0.45, 0.57, 0.62, 1)
        _GradientPower("그라데이션 분포", Range(0.1, 3)) = 0.65
        [HDR] _SunColor("태양 색", Color) = (2.5, 2.1, 1.5, 1)
        _SunRadius("태양 각반경 (도)", Range(0.05, 3)) = 0.4
        _SunSoftness("태양 경계 폭 (도)", Range(0.01, 1)) = 0.08
        _HaloColor("광륜 색", Color) = (1, 0.76, 0.46, 1)
        _HaloStrength("광륜 강도", Range(0, 1)) = 0.18
        _HaloPower("광륜 집중도", Range(1, 128)) = 18
        [NoScaleOffset] _CloudMap("구름 형태 (R) / 디테일 (G)", 2D) = "gray" {}
        _CloudLightColor("구름 밝은 면", Color) = (1, 0.97, 0.89, 1)
        _CloudShadowColor("구름 그늘", Color) = (0.50, 0.65, 0.78, 1)
        _CloudCoverage("구름량", Range(0, 1)) = 0.32
        _CloudSoftness("구름 경계 부드러움", Range(0.01, 0.4)) = 0.10
        _CloudHeight("구름층 높이 (m)", Float) = 1800
        _CloudScale("구름 반복 크기 (m)", Float) = 5000
        _CloudVelocity("구름 이동 (XZ, m/s)", Vector) = (3, 1, 0, 0)
        _CloudEvolution("구름 형태 변화 속도", Range(0, 0.01)) = 0.0005
        _CloudDetail("구름 디테일 강도", Range(0, 0.3)) = 0.08
        _CloudShadeOffset("구름 음영 표본 간격", Range(0.001, 0.1)) = 0.025
        _CloudRimStrength("구름 가장자리 빛", Range(0, 1)) = 0.2
        _CloudHorizonFade("구름 수평선 소실 높이", Range(0.02, 0.5)) = 0.22
        _Exposure("하늘 밝기", Range(0, 3)) = 1
        [HideInInspector] _SunDirection("태양 방향 (W=명시 방향 사용)", Vector) = (0, 1, 0, 0)
        [HideInInspector] _SunVisibility("태양 가시성", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            Name "DaySky"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_CloudMap); SAMPLER(sampler_CloudMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor, _MiddleColor, _HorizonColor, _GroundColor;
                half4 _SunColor, _HaloColor, _CloudLightColor, _CloudShadowColor;
                float4 _CloudVelocity, _SunDirection;
                float _GradientPower, _SunRadius, _SunSoftness, _HaloStrength, _HaloPower;
                float _CloudCoverage, _CloudSoftness, _CloudHeight, _CloudScale;
                float _CloudEvolution, _CloudDetail, _CloudShadeOffset, _CloudRimStrength;
                float _CloudHorizonFade, _Exposure, _SunVisibility;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 directionWS : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                // Skybox 메시의 위치는 카메라 위치를 더하지 않은 시선 방향이다.
                output.directionWS = TransformObjectToWorldDir(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 view = normalize(input.directionWS);
                float height = pow(saturate(view.y), _GradientPower);
                half3 sky = lerp(_HorizonColor.rgb, _MiddleColor.rgb, smoothstep(0, 0.45, height));
                sky = lerp(sky, _ZenithColor.rgb, smoothstep(0.25, 1, height));
                sky = lerp(sky, _GroundColor.rgb, smoothstep(0, 0.18, -view.y));

                float3 sun = _SunDirection.w > 0.5 ? normalize(_SunDirection.xyz) : GetMainLight().direction;
                float sunDot = saturate(dot(view, sun));
                float visibility = saturate(_SunVisibility) * smoothstep(-0.03, 0.03, sun.y);
                float disk = smoothstep(cos(radians(_SunRadius + _SunSoftness)), cos(radians(_SunRadius)), sunDot);
                sky += _HaloColor.rgb * pow(sunDot, _HaloPower) * _HaloStrength * visibility;
                sky += _SunColor.rgb * disk * visibility;

                // 수평선 근처의 투영 발산을 제한하고 아래쪽 반구에서는 구름을 숨긴다.
                float fade = smoothstep(0.015, max(_CloudHorizonFade, 0.02), view.y);
                float distanceToLayer = max(_CloudHeight - _WorldSpaceCameraPos.y, 1) / max(view.y, 0.025);
                float2 uv = (_WorldSpaceCameraPos.xz + view.xz * distanceToLayer + _CloudVelocity.xy * _Time.y) / max(_CloudScale, 1);
                float2 shape = SAMPLE_TEXTURE2D(_CloudMap, sampler_CloudMap, uv).rg;
                float detail = SAMPLE_TEXTURE2D(_CloudMap, sampler_CloudMap, uv * 2.71 + _Time.y * _CloudEvolution).g;
                float density = shape.r + (detail - 0.5) * _CloudDetail;
                float threshold = lerp(1.05, 0.15, _CloudCoverage);
                float softness = max(_CloudSoftness, fwidth(density));
                float alpha = smoothstep(threshold, threshold + softness, density);
                alpha *= fade * smoothstep(0, 0.03, _CloudCoverage);
                float2 towardSun = sun.xz / max(length(sun.xz), 0.001);
                float neighbor = SAMPLE_TEXTURE2D(_CloudMap, sampler_CloudMap, uv + towardSun * _CloudShadeOffset).r;
                float lighting = saturate(0.65 + (density - neighbor) * 4);
                half3 cloud = lerp(_CloudShadowColor.rgb, _CloudLightColor.rgb, lighting);
                float rim = (1 - smoothstep(threshold, threshold + softness * 3, density)) * pow(sunDot, 8);
                cloud += _HaloColor.rgb * rim * _CloudRimStrength * visibility;
                return half4(lerp(sky, cloud, alpha) * _Exposure, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
