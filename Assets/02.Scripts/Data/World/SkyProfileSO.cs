using System;
using UnityEngine;

namespace UPlayGround.Data.World
{
    /// <summary>세계 시각을 바꾸지 않고 지역의 하늘·원경·구름 미술을 정의한다.</summary>
    [CreateAssetMenu(fileName = "SkyProfile", menuName = "UPlayGround/월드/Sky Profile")]
    public sealed class SkyProfileSO : ScriptableObject
    {
        [Tooltip("자정=0, 정오=0.5. 양 끝 색을 같게 저작한다.")]
        public Gradient zenith = new();
        public Gradient middle = new();
        public Gradient horizon = new();
        public Gradient cloudLight = new();
        public Gradient cloudShadow = new();
        public Gradient fog = new();
        [Range(0, 1)] public float cloudCoverage = 0.3f;
        [Tooltip("날씨 구름량이 1일 때의 주 구름량. 기본 구름량보다 낮으면 기본값을 유지한다. 낮은 날씨 값에서는 하늘 여백을 우선 보존한다.")]
        [Range(0, 1)] public float overcastCloudCoverage = 0.58f;
        [Tooltip("2D 구름 폴백의 경계 폭. 볼륨 가장자리는 cloudErosion으로 조절한다.")]
        [Range(0.01f, 0.4f)] public float cloudSoftness = 0.08f;
        [Min(1)] public float cloudHeight = 1800;
        [Min(1)] public float cloudScale = 5000;
        [Range(0, 1)] public float highCloudCoverage = 0.15f;
        [Range(0, 1)] public float cloudThickness = 0.6f;
        [Tooltip("2D 구름 폴백의 디테일 강도.")]
        [Range(0, 0.3f)] public float cloudDetail = 0.05f;
        [Range(0, 1)] public float cloudRimStrength = 0.2f;
        [Min(1)] public float cloudDepth = 4200;
        [Tooltip("큰 적운 군락의 수평 크기. 세부 침식 노이즈 크기와 분리한다.")]
        [Min(1000)] public float cloudFormationScale = 24000;
        [Min(1)] public float cloudEdgeWidth = 60;
        [Range(0.1f, 4)] public float cloudDensity = 2.4f;
        [Range(0, 1)] public float cloudErosion = 0.6f;
        [Tooltip("기본 하늘 프로필에서만 사용. 세계 1분을 기준 초로 삼는 구름 운송 배율이며 지역 보간 대상이 아니다. 재입장 시 적용한다.")]
        [Min(0)] public float cloudFlowMultiplier = 3;
        [Tooltip("기본 하늘 프로필에서만 사용. 주 구름 UV 운송 대비 고층 비율. 재입장 시 적용한다.")]
        [Range(0, 3)] public float highCloudSpeed = 0.45f;
        [Range(0, 1)] public float sphericalProjection = 1;
        [Min(1)] public float planetRadius = 200000;
        [Range(0, 1)] public float haloStrength = 0.16f;
        [Min(0)] public float lightMultiplier = 1;
        [Min(0)] public float fogDensity = 0.001f;
        [Min(0)] public float fogStart = 80;
        [Min(1)] public float fogEnd = 1400;
        [Header("밤하늘")]
        [Min(0)] public float starIntensity = 1.4f;
        [Min(0)] public float galaxyIntensity = 0.12f;
        [Min(0)] public float moonIntensity = 1.2f;
        [Range(0.1f, 3)] public float moonRadius = 0.65f;
        [Range(0, 1)] public float moonPhase = 0.85f;

        /// <summary>시각별 팔레트와 지역 상수를 하나의 보간 가능한 값으로 평가한다.</summary>
        public SkySample Evaluate(float dayProgress)
        {
            float time = Mathf.Repeat(dayProgress, 1);
            return new SkySample
            {
                Zenith = zenith.Evaluate(time), Middle = middle.Evaluate(time),
                Horizon = horizon.Evaluate(time), CloudLight = cloudLight.Evaluate(time),
                CloudShadow = cloudShadow.Evaluate(time), Fog = fog.Evaluate(time),
                Coverage = cloudCoverage, OvercastCoverage = overcastCloudCoverage,
                Softness = cloudSoftness, Height = cloudHeight,
                Scale = cloudScale, HighCoverage = highCloudCoverage, Thickness = cloudThickness,
                Detail = cloudDetail, Rim = cloudRimStrength,
                Depth = cloudDepth, Density = cloudDensity, Erosion = cloudErosion,
                FormationScale = cloudFormationScale, EdgeWidth = cloudEdgeWidth,
                Projection = sphericalProjection, Radius = planetRadius, Halo = haloStrength,
                LightMultiplier = lightMultiplier, FogDensity = fogDensity,
                FogStart = fogStart, FogEnd = Mathf.Max(fogStart + 1, fogEnd),
                Stars = starIntensity, Galaxy = galaxyIntensity, Moon = moonIntensity,
                MoonRadius = moonRadius, MoonPhase = moonPhase
            };
        }
    }

    /// <summary>렌더링 구현을 참조하지 않는 지역·시각 보간 결과.</summary>
    [Serializable]
    public struct SkySample
    {
        public Color Zenith, Middle, Horizon, CloudLight, CloudShadow, Fog;
        public float Coverage, Softness, Height, Scale, HighCoverage, Thickness, Projection, Radius, Halo;
        public float LightMultiplier, FogDensity, FogStart, FogEnd;
        public float Detail, Rim;
        public float Depth, Density, Erosion;
        public float FormationScale, EdgeWidth;
        public float OvercastCoverage;
        public float Stars, Galaxy, Moon, MoonRadius, MoonPhase;

        /// <summary>날씨별 목표 구름량으로 보간해 지역 구름량과의 중복 합산을 막는다.</summary>
        public float EvaluateCloudCoverage(float weather)
        {
            float weight = Mathf.Clamp01(weather);
            // 약한 흐림부터 구름 사이를 메우지 않도록 강한 날씨 구간에 변화를 모은다.
            return Mathf.Lerp(Coverage, Mathf.Max(Coverage, OvercastCoverage), weight * weight);
        }

        /// <summary>지역 경계와 시간 평활화에 같은 보간 규칙을 적용한다.</summary>
        public static SkySample Lerp(SkySample a, SkySample b, float weight)
        {
            return new SkySample
            {
                Zenith = Color.Lerp(a.Zenith, b.Zenith, weight), Middle = Color.Lerp(a.Middle, b.Middle, weight),
                Horizon = Color.Lerp(a.Horizon, b.Horizon, weight), CloudLight = Color.Lerp(a.CloudLight, b.CloudLight, weight),
                CloudShadow = Color.Lerp(a.CloudShadow, b.CloudShadow, weight), Fog = Color.Lerp(a.Fog, b.Fog, weight),
                Coverage = Mathf.Lerp(a.Coverage, b.Coverage, weight), Softness = Mathf.Lerp(a.Softness, b.Softness, weight),
                OvercastCoverage = Mathf.Lerp(a.OvercastCoverage, b.OvercastCoverage, weight),
                Height = Mathf.Lerp(a.Height, b.Height, weight), Scale = Mathf.Lerp(a.Scale, b.Scale, weight),
                HighCoverage = Mathf.Lerp(a.HighCoverage, b.HighCoverage, weight), Thickness = Mathf.Lerp(a.Thickness, b.Thickness, weight),
                Detail = Mathf.Lerp(a.Detail, b.Detail, weight), Rim = Mathf.Lerp(a.Rim, b.Rim, weight),
                Depth = Mathf.Lerp(a.Depth, b.Depth, weight), Density = Mathf.Lerp(a.Density, b.Density, weight),
                Erosion = Mathf.Lerp(a.Erosion, b.Erosion, weight),
                FormationScale = Mathf.Lerp(a.FormationScale, b.FormationScale, weight),
                EdgeWidth = Mathf.Lerp(a.EdgeWidth, b.EdgeWidth, weight),
                Projection = Mathf.Lerp(a.Projection, b.Projection, weight), Radius = Mathf.Lerp(a.Radius, b.Radius, weight),
                Halo = Mathf.Lerp(a.Halo, b.Halo, weight), LightMultiplier = Mathf.Lerp(a.LightMultiplier, b.LightMultiplier, weight),
                FogDensity = Mathf.Lerp(a.FogDensity, b.FogDensity, weight), FogStart = Mathf.Lerp(a.FogStart, b.FogStart, weight),
                FogEnd = Mathf.Lerp(a.FogEnd, b.FogEnd, weight),
                Stars = Mathf.Lerp(a.Stars, b.Stars, weight), Galaxy = Mathf.Lerp(a.Galaxy, b.Galaxy, weight),
                Moon = Mathf.Lerp(a.Moon, b.Moon, weight), MoonRadius = Mathf.Lerp(a.MoonRadius, b.MoonRadius, weight),
                MoonPhase = Mathf.Lerp(a.MoonPhase, b.MoonPhase, weight)
            };
        }
    }
}
