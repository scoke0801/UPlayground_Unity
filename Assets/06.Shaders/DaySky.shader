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
        _StarIntensity("별 밝기", Range(0, 5)) = 1.4
        _GalaxyIntensity("은하수 밝기", Range(0, 1)) = 0.12
        _MoonIntensity("달 밝기", Range(0, 5)) = 1.2
        _MoonRadius("달 각반경 (도)", Range(0.1, 3)) = 0.65
        _MoonPhase("달 위상 (0=삭, 1=보름)", Range(0, 1)) = 0.85
        _NightZenith("밤 천정 색 (지역 프로필 미사용 시)", Color) = (0.008, 0.018, 0.055, 1)
        _NightHorizon("밤 수평선 색 (지역 프로필 미사용 시)", Color) = (0.055, 0.08, 0.14, 1)
        [HideInInspector] _UseRegionalPalette("지역 팔레트 사용", Float) = 0
        [HideInInspector] _CelestialRotation("별 회전 (sin, cos)", Vector) = (0, 1, 0, 0)
        [HideInInspector] _Lightning("번개 방향 / 강도", Vector) = (0, 1, 0, 0)
        _SphericalProjection("구면층 투영", Range(0, 1)) = 0
        _PlanetRadius("구면 반경 (m)", Float) = 200000
        _CloudThickness("구름 두께 음영", Range(0, 1)) = 0
        [NoScaleOffset] _CloudVolume("구름 부피 (Perlin R / Worley GBA)", 3D) = "white" {}
        [Toggle] _CloudVolumeEnabled("볼류메트릭 구름", Float) = 0
        [NoScaleOffset] _CloudFormation("적운 군락 (거리 R / 분포 G)", 3D) = "black" {}
        [Toggle] _CloudFormationEnabled("수직 적운 군락", Float) = 0
        _CloudFormationScale("구름 군락 반복 크기 (m)", Float) = 24000
        _CloudEdgeWidth("구름 표면 전이 폭 (m)", Float) = 60
        _CloudDepth("구름층 두께 (m)", Float) = 4200
        _CloudDensity("구름 광학 밀도", Range(0.1, 4)) = 2.4
        _CloudErosion("구름 가장자리 침식", Range(0, 1)) = 0.6
        _CloudRaySteps("구름 시선 표본 수", Range(16, 128)) = 96
        _CloudMaxDistance("구름 원경 한계 (m)", Float) = 90000
        _CloudForwardScattering("구름 역광 산란", Range(0, 2)) = 0.65
        _CloudAmbientStrength("구름 내부 하늘빛", Range(0, 1)) = 0.45
        _CloudMultipleScattering("구름 내부 확산광", Range(0, 1)) = 0.65
        _CloudLightAbsorption("구름 자체 그림자 대비", Range(0.1, 4)) = 1.7
        _RainCloudLightColor("비구름 밝은 면", Color) = (0.58, 0.61, 0.65, 1)
        _RainCloudShadowColor("비구름 밑면", Color) = (0.50, 0.53, 0.57, 1)
        _OvercastSkyColor("비 예고 하늘색", Color) = (0.40, 0.45, 0.51, 1)
        _RainCloudDensityMultiplier("비구름 밀도 배율", Range(1, 3)) = 1.5
        [HideInInspector] _RainCloudIntensity("비구름 발달", Range(0, 1)) = 0
        _HighCloudCoverage("높은 얇은 구름량", Range(0, 1)) = 0
        _HighCloudSpeed("높은 구름 속도 배율", Range(0, 3)) = 0.45
        _HighCloudAltitude("얇은 구름 고도 (m)", Float) = 9000
        _HighCloudOpacity("얇은 구름 불투명도", Range(0, 1)) = 0.4
        _SunCoronaStrength("태양 주변 확산광", Range(0, 1)) = 0.18
        _SunCoronaRadius("태양 주변 확산 반경 (도)", Range(1, 15)) = 5
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
            TEXTURE3D(_CloudVolume); SAMPLER(sampler_CloudVolume);
            TEXTURE3D(_CloudFormation); SAMPLER(sampler_CloudFormation);
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
                float _StarIntensity, _GalaxyIntensity, _MoonIntensity, _MoonRadius, _MoonPhase;
                float4 _CelestialRotation, _Lightning;
                half4 _NightZenith, _NightHorizon;
                float _UseRegionalPalette;
                float _CloudVolumeEnabled, _CloudDepth, _CloudDensity, _CloudErosion;
                float _CloudRaySteps, _CloudMaxDistance, _CloudForwardScattering;
                float _CloudFormationEnabled, _CloudFormationScale, _CloudEdgeWidth;
                float _CloudAmbientStrength, _CloudLightAbsorption, _RainCloudIntensity, _RainCloudDensityMultiplier;
                float _CloudMultipleScattering;
                float _HighCloudAltitude, _HighCloudOpacity, _SunCoronaStrength, _SunCoronaRadius;
                half4 _RainCloudLightColor, _RainCloudShadowColor, _OvercastSkyColor;
            CBUFFER_END

            float3 HashStars(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.xxy + p.yxx) * p.zyx);
            }

            float GalaxyNoise(float3 position)
            {
                float3 p = floor(position), f = frac(position);
                f = f * f * (3 - 2 * f);
                float a = lerp(HashStars(p).x, HashStars(p + float3(1, 0, 0)).x, f.x);
                float b = lerp(HashStars(p + float3(0, 1, 0)).x, HashStars(p + float3(1, 1, 0)).x, f.x);
                float c = lerp(HashStars(p + float3(0, 0, 1)).x, HashStars(p + float3(1, 0, 1)).x, f.x);
                float d = lerp(HashStars(p + float3(0, 1, 1)).x, HashStars(p + float3(1, 1, 1)).x, f.x);
                return lerp(lerp(a, b, f.y), lerp(c, d, f.y), f.z);
            }

            half3 DrawNight(float3 view, float3 sun)
            {
                float night = 1 - smoothstep(-0.18, 0.04, sun.y);
                float horizon = smoothstep(0.01, 0.18, view.y);
                if (night * horizon <= 0) return 0;
                // 구름 운송과 분리된 천구 좌표라 이동·지역 진입 때 별이 미끄러지지 않는다.
                float3 axis = normalize(float3(0, 0.8, 0.6));
                float3 stars = view * _CelestialRotation.y + cross(axis, view) * _CelestialRotation.x
                    + axis * dot(axis, view) * (1 - _CelestialRotation.y);
                float3 cell = floor(stars * 180);
                float3 seed = HashStars(cell);
                float3 center = cell + 0.2 + seed * 0.6;
                float distanceToStar = length(stars * 180 - center);
                float size = lerp(0.035, 0.11, seed.z * seed.z);
                float pixel = max(length(fwidth(stars * 180)), 0.008);
                float footprint = size * size + pixel * pixel * 0.25;
                float starPoint = exp2(-2 * distanceToStar * distanceToStar / footprint) * size * size / footprint;
                float twinkle = 0.85 + 0.15 * sin(_Time.y * (1 + seed.x * 2) + seed.y * 60);
                half3 starColor = lerp(half3(0.65, 0.78, 1), half3(1, 0.86, 0.68), seed.x);
                half3 result = starColor * starPoint * step(0.975, seed.y) * twinkle * _StarIntensity;
                float band = pow(saturate(1 - abs(dot(stars, normalize(float3(0.3, 0.6, 0.74))))), 18);
                float dust = GalaxyNoise(stars * 18) * 0.7 + GalaxyNoise(stars * 53) * 0.3;
                result += half3(0.30, 0.38, 0.62) * band * smoothstep(0.2, 0.8, dust) * _GalaxyIntensity;
                float3 moon = -sun;
                float3 right = cross(float3(0, 1, 0), moon);
                right = dot(right, right) < 0.001 ? float3(1, 0, 0) : normalize(right);
                float3 up = cross(moon, right);
                float radius = sin(radians(max(_MoonRadius, 0.1)));
                float2 uv = float2(dot(view, right), dot(view, up)) / radius;
                float r2 = dot(uv, uv);
                float edge = max(fwidth(r2), 0.01);
                float disk = (1 - smoothstep(1 - edge, 1 + edge, r2)) * step(0, dot(view, moon));
                float phaseCos = _MoonPhase * 2 - 1;
                float lit = saturate(uv.x * sqrt(saturate(1 - phaseCos * phaseCos))
                    + sqrt(saturate(1 - r2)) * phaseCos);
                float maria = lerp(0.6, 1, GalaxyNoise(float3(uv * 4, 3)));
                half3 moonColor = half3(0.74, 0.82, 1) * _MoonIntensity;
                result *= 1 - disk;
                result += moonColor * disk * smoothstep(0, 0.12, lit) * maria;
                result += moonColor * pow(saturate(dot(view, moon)), 600) * 0.04 * _MoonPhase;
                return result * night * horizon;
            }

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

            #include "DaySkyCloudVolume.hlsl"

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
                float nightPalette = (1 - smoothstep(-0.18, 0.04, sun.y)) * (1 - _UseRegionalPalette);
                sky = lerp(sky, lerp(_NightHorizon.rgb, _NightZenith.rgb, height), nightPalette);
                float sunDot = saturate(dot(view, sun));
                float visibility = saturate(_SunVisibility) * smoothstep(-0.03, 0.03, sun.y);
                float disk = smoothstep(cos(radians(_SunRadius + _SunSoftness)), cos(radians(_SunRadius)), sunDot);
                half3 haloColor = lerp(_HaloColor.rgb, _CloudLightColor.rgb, smoothstep(0.05, 0.35, sun.y));
                sky += haloColor * pow(sunDot, _HaloPower) * _HaloStrength * visibility;
                float corona = exp2(-(1 - sunDot) / max(1 - cos(radians(_SunCoronaRadius)), 0.0001));
                sky += haloColor * corona * _SunCoronaStrength * visibility;
                sky += _SunColor.rgb * disk * visibility;
                sky += DrawNight(view, sun);
                float rainDaylight = lerp(0.06, 1, smoothstep(-0.12, 0.12, sun.y));
                sky = lerp(sky, _OvercastSkyColor.rgb * rainDaylight * lerp(1.08, 0.8, height),
                    _RainCloudIntensity * 0.85);

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
                if (_CloudVolumeEnabled > 0.5)
                {
                    // 고층운은 볼륨 뒤에 두어 적운에 가려지고 서로 다른 높이로 읽히게 한다.
                    float highRadius = max(_PlanetRadius, 1);
                    float highAltitude = max(_HighCloudAltitude, _CloudHeight + _CloudDepth);
                    float3 highOrigin = _WorldSpaceCameraPos + float3(0, highRadius, 0);
                    float2 highHit = IntersectCloudSphere(highOrigin, view, highRadius + highAltitude);
                    float2 highPosition = (_WorldSpaceCameraPos + view * max(0, highHit.y)).xz;
                    float2 highUV = highPosition / spatialScale + travelUV * _HighCloudSpeed;
                    // 적운과 같은 부피 노이즈를 사용해 날카로운 사선 텍스처와의 화풍 차이를 없앤다.
                    float3 highVolumeUV = float3(highUV.x * 0.45, 0.43, highUV.y * 0.45);
                    float4 highShape = SAMPLE_TEXTURE3D(_CloudVolume, sampler_CloudVolume, highVolumeUV);
                    float wisps = dot(highShape.rg, float2(0.65, 0.35));
                    float patches = SAMPLE_TEXTURE2D(_CloudMap, sampler_CloudMap, highUV * 0.31 + 0.37).r;
                    float highAlpha = smoothstep(0.42, 0.72, wisps) * smoothstep(0.35, 0.7, patches)
                        * saturate(_HighCloudCoverage) * fade * _HighCloudOpacity;
                    float4 highNeighbor = SAMPLE_TEXTURE3D(_CloudVolume, sampler_CloudVolume,
                        highVolumeUV + sun * 0.08);
                    float highLighting = saturate(0.65 + (wisps - dot(highNeighbor.rg, float2(0.65, 0.35))) * 2);
                    half3 highColor = lerp(_CloudShadowColor.rgb, _CloudLightColor.rgb, highLighting);
                    highColor = lerp(highColor, _NightHorizon.rgb, nightPalette);
                    highColor = lerp(highColor, lerp(_HorizonColor.rgb, _NightHorizon.rgb, nightPalette), 0.25);
                    sky = lerp(sky, highColor, highAlpha);
                    return half4(RenderCloudVolume(view, sun, sky, nightPalette, travelUV, clock, input.positionCS.xy) * _Exposure, 1);
                }
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
                half3 cloud = lerp(lerp(_CloudShadowColor.rgb, _RainCloudShadowColor.rgb * rainDaylight, _RainCloudIntensity),
                    lerp(_CloudLightColor.rgb, _RainCloudLightColor.rgb * rainDaylight, _RainCloudIntensity), lighting);
                cloud = lerp(cloud, lerp(_NightZenith.rgb, _NightHorizon.rgb, lighting), nightPalette);
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
                    half3 highColor = lerp(_CloudLightColor.rgb, _NightHorizon.rgb, nightPalette);
                    sky = lerp(sky, highColor, highAlpha);
                }
                // 두꺼운 중심의 태양 원반을 더 가리고 얇은 가장자리의 빛을 남긴다.
                float occlusion = alpha * thickness * _CloudThickness;
                sky -= _SunColor.rgb * disk * visibility * occlusion;
                float flash = _Lightning.w * (0.2 + 0.8 * pow(saturate(dot(view, _Lightning.xyz)), 5));
                half3 stormLight = half3(0.63, 0.75, 1) * flash;
                return half4((lerp(sky, cloud, alpha) + stormLight * (0.04 + alpha * 0.2)) * _Exposure, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
