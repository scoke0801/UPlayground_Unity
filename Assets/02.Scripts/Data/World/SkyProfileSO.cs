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
        [Range(0.01f, 0.4f)] public float cloudSoftness = 0.08f;
        [Min(1)] public float cloudHeight = 1800;
        [Min(1)] public float cloudScale = 5000;
        [Range(0, 1)] public float highCloudCoverage = 0.15f;
        [Range(0, 1)] public float cloudThickness = 0.6f;
        [Range(0, 0.3f)] public float cloudDetail = 0.05f;
        [Range(0, 1)] public float cloudRimStrength = 0.2f;
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
                Projection = sphericalProjection, Radius = planetRadius, Halo = haloStrength,
                LightMultiplier = lightMultiplier, FogDensity = fogDensity,
                FogStart = fogStart, FogEnd = Mathf.Max(fogStart + 1, fogEnd)
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
        public float OvercastCoverage;

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
                Projection = Mathf.Lerp(a.Projection, b.Projection, weight), Radius = Mathf.Lerp(a.Radius, b.Radius, weight),
                Halo = Mathf.Lerp(a.Halo, b.Halo, weight), LightMultiplier = Mathf.Lerp(a.LightMultiplier, b.LightMultiplier, weight),
                FogDensity = Mathf.Lerp(a.FogDensity, b.FogDensity, weight), FogStart = Mathf.Lerp(a.FogStart, b.FogStart, weight),
                FogEnd = Mathf.Lerp(a.FogEnd, b.FogEnd, weight)
            };
        }
    }
}
