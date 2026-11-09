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
        [NoScaleOffset] _CloudSilhouetteMask("큰 구름 마스크 (R, 흰색=구름)", 2D) = "white" {}
        _CloudSilhouetteStrength("큰 구름 마스크 적용", Range(0, 1)) = 0
        _CloudSilhouetteScale("큰 구름 마스크 반복 배율", Range(0.1, 4)) = 0.5
        _CloudLightColor("구름 밝은 면", Color) = (1, 0.97, 0.89, 1)
        _CloudShadowColor("구름 그늘", Color) = (0.50, 0.65, 0.78, 1)
        _CloudCoverage("구름량", Range(0, 1)) = 0.32
        _CloudSoftness("구름 경계 부드러움", Range(0.01, 0.4)) = 0.10
        _CloudHeight("구름층 높이 (m)", Float) = 1800
        _CloudScale("구름 반복 크기 (m)", Float) = 5000
        _CloudVelocity("구름 이동 (XZ, m/기준 초; 세계 시계 사용 시 m/게임 분)", Vector) = (3, 1, 0, 0)
        _CloudEvolution("구름 형태 변화 속도", Range(0, 0.01)) = 0.0005
        _CloudDetail("구름 디테일 강도", Range(0, 0.3)) = 0.08
        _CloudShadeOffset("구름 음영 표본 간격", Range(0.001, 0.1)) = 0.025
        _CloudRimStrength("구름 가장자리 빛", Range(0, 1)) = 0.2
        _CloudHorizonFade("구름 수평선 소실 높이", Range(0.02, 0.5)) = 0.22
        _Exposure("하늘 밝기", Range(0, 3)) = 1
        _SphericalProjection("구면층 투영", Range(0, 1)) = 0
        _PlanetRadius("구면 반경 (m)", Float) = 200000
        _CloudThickness("구름 두께 음영", Range(0, 1)) = 0
        _HighCloudCoverage("높은 얇은 구름량", Range(0, 1)) = 0
        _HighCloudSpeed("높은 구름 속도 배율", Range(0, 3)) = 0.45
        [HideInInspector] _CloudClock("세계 시각 구름 위상", Float) = 0
        [HideInInspector] _UseCloudClock("세계 시각 사용", Float) = 0
        [HideInInspector] _CloudWindOffset("누적 날씨 바람 (m)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _CloudFlowMultiplier("구름 운송 배율", Float) = 1
        [HideInInspector] _CloudMotionScale("공통 운송 반복 크기 (m, 0=현재 크기)", Float) = 0
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
            TEXTURE2D(_CloudSilhouetteMask); SAMPLER(sampler_CloudSilhouetteMask);
            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor, _MiddleColor, _HorizonColor, _GroundColor;
                half4 _SunColor, _HaloColor, _CloudLightColor, _CloudShadowColor;
                float4 _CloudVelocity, _SunDirection;
                float _GradientPower, _SunRadius, _SunSoftness, _HaloStrength, _HaloPower;
                float _CloudCoverage, _CloudSoftness, _CloudHeight, _CloudScale;
                float _CloudEvolution, _CloudDetail, _CloudShadeOffset, _CloudRimStrength;
                float _CloudHorizonFade, _Exposure, _SunVisibility;
                float _SphericalProjection, _PlanetRadius, _CloudThickness, _HighCloudCoverage, _HighCloudSpeed;
                float _CloudClock, _UseCloudClock;
                float _CloudFlowMultiplier, _CloudMotionScale;
                float4 _CloudWindOffset;
                float _CloudSilhouetteStrength, _CloudSilhouetteScale;
            CBUFFER_END

            float ApplyCloudMask(float shape, float2 uv)
            {
                if (_CloudSilhouetteStrength > 0)
                {
                    float mask = SAMPLE_TEXTURE2D(_CloudSilhouetteMask, sampler_CloudSilhouetteMask,
                        uv * _CloudSilhouetteScale).r;
                    shape = lerp(shape, mask, _CloudSilhouetteStrength);
                }
                return shape;
            }

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
                if (_SphericalProjection > 0)
                {
                    float radius = max(_PlanetRadius, 1);
                    float3 origin = _WorldSpaceCameraPos + float3(0, radius, 0);
                    float projection = dot(origin, view);
                    float sphereDistance = -projection + sqrt(max(0, projection * projection
                        + (radius + _CloudHeight) * (radius + _CloudHeight) - dot(origin, origin)));
                    distanceToLayer = lerp(distanceToLayer, max(0, sphereDistance), _SphericalProjection);
                }
                float clock = lerp(_Time.y, _CloudClock, _UseCloudClock);
                float2 travel = _CloudVelocity.xy * clock + _CloudWindOffset.xy;
                float2 cloudPosition = _WorldSpaceCameraPos.xz + view.xz * distanceToLayer;
                float spatialScale = max(_CloudScale, 1);
                float motionScale = _CloudMotionScale > 0 ? _CloudMotionScale : spatialScale;
                float2 travelUV = travel * _CloudFlowMultiplier / motionScale;
                float2 uv = cloudPosition / spatialScale + travelUV;
                float2 shape = SAMPLE_TEXTURE2D(_CloudMap, sampler_CloudMap, uv).rg;
                float detail = SAMPLE_TEXTURE2D(_CloudMap, sampler_CloudMap, uv * 2.71 + clock * _CloudEvolution).g;
                float threshold = lerp(1.05, 0.15, _CloudCoverage);
                float largeShape = ApplyCloudMask(shape.r, uv);
                // 디테일은 큰 형태의 안쪽에서만 바꿔 여백과 외곽선을 보존한다.
                float interior = smoothstep(threshold, threshold + _CloudSoftness * 2, largeShape);
                float density = largeShape + (detail - 0.5) * _CloudDetail * interior;
                float softness = max(_CloudSoftness, fwidth(density));
                float alpha = smoothstep(threshold, threshold + softness, density);
                alpha *= fade * smoothstep(0, 0.03, _CloudCoverage);
                float2 towardSun = sun.xz / max(length(sun.xz), 0.001);
                float2 neighborUV = uv + towardSun * _CloudShadeOffset;
                float neighbor = ApplyCloudMask(SAMPLE_TEXTURE2D(_CloudMap, sampler_CloudMap, neighborUV).r, neighborUV);
                float thickness = saturate((density - threshold) / max(softness * 3, 0.001));
                float lighting = saturate(0.65 + (density - neighbor) * 4 - thickness * _CloudThickness * 0.45);
                half3 cloud = lerp(_CloudShadowColor.rgb, _CloudLightColor.rgb, lighting);
                float rim = (1 - smoothstep(threshold, threshold + softness * 3, density)) * pow(sunDot, 8);
                cloud += _HaloColor.rgb * rim * _CloudRimStrength * visibility;
                // 높은 얇은 구름은 별도 속도로 흘려 주 구름층의 큰 형태를 보존한다.
                if (_HighCloudCoverage > 0)
                {
                    float2 highUV = cloudPosition * 0.65 / spatialScale + travelUV * _HighCloudSpeed;
                    float highShape = SAMPLE_TEXTURE2D(_CloudMap, sampler_CloudMap, highUV).g;
                    // 주 구름의 디테일을 섞으면 서로 다른 층의 진행 방향이 흐려진다.
                    float highDensity = highShape;
                    float highThreshold = lerp(1.05, 0.35, _HighCloudCoverage);
                    float highAlpha = smoothstep(highThreshold, highThreshold + 0.15, highDensity) * fade * 0.35;
                    sky = lerp(sky, _CloudLightColor.rgb, highAlpha);
                }
                // 두꺼운 중심의 태양 원반을 더 가리고 얇은 가장자리의 빛을 남긴다.
                float occlusion = alpha * thickness * _CloudThickness;
                sky -= _SunColor.rgb * disk * visibility * occlusion;
                return half4(lerp(sky, cloud, alpha) * _Exposure, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
