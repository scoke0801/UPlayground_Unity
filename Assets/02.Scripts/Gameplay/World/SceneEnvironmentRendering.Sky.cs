using System;
using UnityEngine;
using UPlayGround.Data.World;
using UPlayGround.Manager;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.Gameplay.World
{
    public sealed partial class SceneEnvironmentRendering
    {
        [Header("지역 하늘 (비우면 기존 재질 유지)")]
        [SerializeField] private SkyProfileSO _defaultSkyProfile;
        [SerializeField] private SkyRegionBinding[] _skyRegions = Array.Empty<SkyRegionBinding>();
        [SerializeField] private EnvironmentController _environment;
        [SerializeField] private Transform _skyObserver;
        [SerializeField, Min(0)] private float _skyTransitionSeconds = 4;
        [SerializeField, Min(1)] private float _skyTeleportDistance = 150;
        [Tooltip("기본 게임 속도(1분/실제 초)에서의 바람 이동 m/분. 지역과 무관한 공통 위상이다.")]
        [SerializeField, Min(0)] private float _weatherCloudSpeed = 12;
        [SerializeField] private bool _applyRegionalFog = true;
        [Tooltip("수면이 사용하는 Realtime / Via Scripting 프로브. 비우면 반사 갱신 비용이 없다.")]
        [SerializeField] private ReflectionProbe _skyReflectionProbe;
        [SerializeField, Min(0.2f)] private float _reflectionUpdateSeconds = 2;
        private int _reflectionRenderId = -1;
        private float _nextReflectionTime;
        private SkyRegionBinding[] _orderedRegions;
        private SkySample _skySample;
        private bool _hasSkySample, _hasFogSnapshot;
        private Color _previousFogColor;
        private float _previousFogDensity, _previousFogStart, _previousFogEnd;
        private int _skyFrame = -1;
        private Vector3 _previousObserver;

        private static readonly int ZenithId = Shader.PropertyToID("_ZenithColor");
        private static readonly int MiddleId = Shader.PropertyToID("_MiddleColor");
        private static readonly int HorizonId = Shader.PropertyToID("_HorizonColor");
        private static readonly int GroundId = Shader.PropertyToID("_GroundColor");
        private static readonly int CloudLightId = Shader.PropertyToID("_CloudLightColor");
        private static readonly int CloudShadowId = Shader.PropertyToID("_CloudShadowColor");
        private static readonly int CoverageId = Shader.PropertyToID("_CloudCoverage");
        private static readonly int SoftnessId = Shader.PropertyToID("_CloudSoftness");
        private static readonly int HeightId = Shader.PropertyToID("_CloudHeight");
        private static readonly int ScaleId = Shader.PropertyToID("_CloudScale");
        private static readonly int HighCoverageId = Shader.PropertyToID("_HighCloudCoverage");
        private static readonly int ThicknessId = Shader.PropertyToID("_CloudThickness");
        private static readonly int ProjectionId = Shader.PropertyToID("_SphericalProjection");
        private static readonly int RadiusId = Shader.PropertyToID("_PlanetRadius");
        private static readonly int HaloId = Shader.PropertyToID("_HaloStrength");
        private static readonly int ClockId = Shader.PropertyToID("_CloudClock");
        private static readonly int UseClockId = Shader.PropertyToID("_UseCloudClock");
        private static readonly int WindOffsetId = Shader.PropertyToID("_CloudWindOffset");
        private static readonly int DetailId = Shader.PropertyToID("_CloudDetail");
        private static readonly int RimId = Shader.PropertyToID("_CloudRimStrength");
        private static readonly int HighSpeedId = Shader.PropertyToID("_HighCloudSpeed");
        private static readonly int FlowMultiplierId = Shader.PropertyToID("_CloudFlowMultiplier");
        private static readonly int MotionScaleId = Shader.PropertyToID("_CloudMotionScale");
        private static readonly int StarsId = Shader.PropertyToID("_StarIntensity");
        private static readonly int GalaxyId = Shader.PropertyToID("_GalaxyIntensity");
        private static readonly int MoonId = Shader.PropertyToID("_MoonIntensity");
        private static readonly int MoonRadiusId = Shader.PropertyToID("_MoonRadius");
        private static readonly int MoonPhaseId = Shader.PropertyToID("_MoonPhase");
        private static readonly int CelestialRotationId = Shader.PropertyToID("_CelestialRotation");
        private static readonly int LightningId = Shader.PropertyToID("_Lightning");
        private static readonly int RegionalPaletteId = Shader.PropertyToID("_UseRegionalPalette");
        private static readonly int DepthId = Shader.PropertyToID("_CloudDepth");
        private static readonly int DensityId = Shader.PropertyToID("_CloudDensity");
        private static readonly int ErosionId = Shader.PropertyToID("_CloudErosion");
        private static readonly int FormationScaleId = Shader.PropertyToID("_CloudFormationScale");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_CloudEdgeWidth");
        private static readonly int RainCloudId = Shader.PropertyToID("_RainCloudIntensity");

        /// <summary>날씨 보정과 중복 곱하지 않을 지역 조명 배율을 공급한다.</summary>
        public float RegionalLightMultiplier => _hasSkySample ? _skySample.LightMultiplier : 1;

        /// <summary>환경과 하늘이 같은 실제 플레이 카메라를 관측점으로 사용한다.</summary>
        public void BindSkyObserver(Transform observer) => _skyObserver = observer;

        private void InitializeRegionalSky()
        {
            _orderedRegions = _skyRegions == null ? Array.Empty<SkyRegionBinding>() : (SkyRegionBinding[])_skyRegions.Clone();
            // 같은 우선순위는 저작 배열 순서를 유지해 재입장 때 결과가 뒤집히지 않는다.
            for (int i = 1; i < _orderedRegions.Length; i++)
            {
                var region = _orderedRegions[i];
                int index = i - 1;
                while (index >= 0 && ComparePriority(_orderedRegions[index], region) > 0)
                {
                    _orderedRegions[index + 1] = _orderedRegions[index];
                    index--;
                }
                _orderedRegions[index + 1] = region;
            }
            _hasSkySample = false;
            _skyFrame = -1;
            _reflectionRenderId = -1;
            _nextReflectionTime = 0;
            if (_runtimeSky != null && _runtimeSky.HasProperty(RegionalPaletteId))
                _runtimeSky.SetFloat(RegionalPaletteId, _defaultSkyProfile != null ? 1 : 0);
            if (_defaultSkyProfile == null) return;
            ConfigureCloudMotion();
            _hasFogSnapshot = _applyRegionalFog;
            _previousFogColor = RenderSettings.fogColor;
            _previousFogDensity = RenderSettings.fogDensity;
            _previousFogStart = RenderSettings.fogStartDistance;
            _previousFogEnd = RenderSettings.fogEndDistance;
            ApplyRegionalSky();
        }

        private void ConfigureCloudMotion()
        {
            if (_runtimeSky == null) return;
            // 지역마다 누적 시각에 다른 속도·크기를 곱하면 오래된 저장일수록 위상이 크게 밀린다.
            // 운송 좌표계는 진입 때 기본 프로필로 고정하고 지역은 공간 투영과 미술만 보간한다.
            _runtimeSky.SetFloat(FlowMultiplierId, Mathf.Max(0, _defaultSkyProfile.cloudFlowMultiplier));
            _runtimeSky.SetFloat(MotionScaleId, Mathf.Max(1, _defaultSkyProfile.cloudScale));
            _runtimeSky.SetFloat(HighSpeedId, _defaultSkyProfile.highCloudSpeed);
        }

        private static int ComparePriority(SkyRegionBinding a, SkyRegionBinding b) =>
            (a != null ? a.priority : int.MinValue).CompareTo(b != null ? b.priority : int.MinValue);

        private void ApplyRegionalSky()
        {
            if (_defaultSkyProfile == null || _runtimeSky == null || _skyFrame == Time.frameCount) return;
            _skyFrame = Time.frameCount;
            Transform observer = _skyObserver;
            if (observer == null && Services.TryGet<ICameraViewService>(out var view))
            {
                var camera = view.GetMainCamera();
                if (camera != null) observer = camera.transform;
            }
            Vector3 position = observer != null ? observer.position : transform.position;
            bool hasTime = Services.TryGet<IGameTimeService>(out var time);
            float dayProgress = hasTime ? time.MinuteOfDay / WorldTimeSettingsSO.MinutesPerDay : 0.5f;
            double totalMinutes = hasTime ? (double)time.CurrentDay * WorldTimeSettingsSO.MinutesPerDay + time.MinuteOfDay : 0;
            SkySample target = EvaluateRegionalSky(dayProgress, position);
            UpdateSkySample(target, position, Time.deltaTime);
            ApplySkySample(_skySample);
            _runtimeSky.SetFloat(UseClockId, 1);
            // 저장된 세계 시각을 사용해 지역 진입과 로드에서 위상을 다시 시작하지 않는다.
            _runtimeSky.SetFloat(ClockId, (float)totalMinutes);
            Vector2 wind = Vector2.zero;
            if (_environment != null && _environment.IsReady)
                _environment.TryGetCloudDisplacement(totalMinutes, out wind);
            wind *= _weatherCloudSpeed;
            _runtimeSky.SetVector(WindOffsetId, new Vector4(wind.x, wind.y, 0, 0));
            RefreshSkyReflection();
        }

        private void ApplyCelestialState()
        {
            if (_runtimeSky == null || !_runtimeSky.HasProperty(CelestialRotationId)) return;
            float progress = Services.TryGet<IGameTimeService>(out var time)
                ? time.MinuteOfDay / WorldTimeSettingsSO.MinutesPerDay : 0.5f;
            float angle = progress * Mathf.PI * 2;
            _runtimeSky.SetVector(CelestialRotationId, new Vector4(Mathf.Sin(angle), Mathf.Cos(angle), 0, 0));
            Vector3 strike = _environment != null ? _environment.LightningDirection : Vector3.up;
            float flash = _environment != null ? _environment.LightningIntensity : 0;
            _runtimeSky.SetVector(LightningId, new Vector4(strike.x, strike.y, strike.z, flash));
        }

        private SkySample EvaluateRegionalSky(float dayProgress, Vector3 position)
        {
            SkySample target = _defaultSkyProfile.Evaluate(dayProgress);
            for (int i = 0; i < _orderedRegions.Length; i++)
            {
                var region = _orderedRegions[i];
                if (region == null || region.profile == null || region.gameObject.scene != gameObject.scene) continue;
                float weight = region.EvaluateWeight(position);
                if (weight > 0) target = SkySample.Lerp(target, region.profile.Evaluate(dayProgress), weight);
            }
            return target;
        }

        private void UpdateSkySample(SkySample target, Vector3 position, float deltaTime)
        {
            bool snap = !_hasSkySample || (position - _previousObserver).sqrMagnitude > _skyTeleportDistance * _skyTeleportDistance;
            float blend = snap || _skyTransitionSeconds <= 0 ? 1 : 1 - Mathf.Exp(-deltaTime / _skyTransitionSeconds);
            _skySample = SkySample.Lerp(_skySample, target, blend);
            _hasSkySample = true;
            _previousObserver = position;
        }

        private void RefreshSkyReflection()
        {
            if (_skyReflectionProbe == null || !_skyReflectionProbe.isActiveAndEnabled
                || _skyReflectionProbe.mode != UnityEngine.Rendering.ReflectionProbeMode.Realtime
                || _skyReflectionProbe.refreshMode != UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting
                || Time.time < _nextReflectionTime) return;
            if (_reflectionRenderId >= 0 && !_skyReflectionProbe.IsFinishedRendering(_reflectionRenderId)) return;
            _reflectionRenderId = _skyReflectionProbe.RenderProbe();
            _nextReflectionTime = Time.time + _reflectionUpdateSeconds;
        }

        private void ApplySkySample(SkySample sample)
        {
            float weather = _environment != null && _environment.IsReady ? _environment.CloudCoverage : 0;
            float cloud = sample.EvaluateCloudCoverage(weather);
            float rainCloud = _environment != null && _environment.IsReady ? _environment.RainCloudIntensity : 0;
            // 강수 예고가 있는 동안 적운 사이의 낮은 층운도 덮어 파란 틈이 남지 않게 한다.
            cloud = Mathf.Lerp(cloud, Mathf.Max(cloud, weather), rainCloud);
            _runtimeSky.SetFloat(RainCloudId, rainCloud);
            float daylight = _boundSun != null ? Mathf.SmoothStep(0, 1, -_boundSun.transform.forward.y) : 1;
            _runtimeSky.SetColor(ZenithId, sample.Zenith);
            _runtimeSky.SetColor(MiddleId, sample.Middle);
            _runtimeSky.SetColor(HorizonId, sample.Horizon);
            _runtimeSky.SetColor(GroundId, sample.Fog);
            _runtimeSky.SetColor(CloudLightId, Color.Lerp(sample.CloudLight, sample.CloudShadow, weather * 0.5f));
            _runtimeSky.SetColor(CloudShadowId, sample.CloudShadow);
            _runtimeSky.SetFloat(CoverageId, cloud);
            _runtimeSky.SetFloat(SoftnessId, sample.Softness);
            _runtimeSky.SetFloat(HeightId, sample.Height);
            _runtimeSky.SetFloat(ScaleId, sample.Scale);
            _runtimeSky.SetFloat(HighCoverageId, sample.HighCoverage);
            _runtimeSky.SetFloat(ThicknessId, Mathf.Lerp(sample.Thickness, 1, weather));
            _runtimeSky.SetFloat(DetailId, sample.Detail);
            _runtimeSky.SetFloat(RimId, sample.Rim * (1 - rainCloud * 0.7f));
            _runtimeSky.SetFloat(DepthId, Mathf.Max(1, sample.Depth));
            _runtimeSky.SetFloat(DensityId, sample.Density);
            _runtimeSky.SetFloat(ErosionId, sample.Erosion);
            _runtimeSky.SetFloat(FormationScaleId, Mathf.Max(1000, sample.FormationScale));
            _runtimeSky.SetFloat(EdgeWidthId, Mathf.Max(1, sample.EdgeWidth));
            _runtimeSky.SetFloat(ProjectionId, sample.Projection);
            _runtimeSky.SetFloat(RadiusId, sample.Radius);
            _runtimeSky.SetFloat(HaloId, sample.Halo * (1 - weather) * daylight);
            _runtimeSky.SetFloat(StarsId, sample.Stars);
            _runtimeSky.SetFloat(GalaxyId, sample.Galaxy);
            _runtimeSky.SetFloat(MoonId, sample.Moon);
            _runtimeSky.SetFloat(MoonRadiusId, sample.MoonRadius);
            _runtimeSky.SetFloat(MoonPhaseId, sample.MoonPhase);
            if (!_hasFogSnapshot) return;
            // FogMode와 활성 여부는 씬의 기존 계약을 보존한다.
            RenderSettings.fogColor = sample.Fog;
            RenderSettings.fogDensity = sample.FogDensity;
            RenderSettings.fogStartDistance = sample.FogStart;
            RenderSettings.fogEndDistance = sample.FogEnd;
        }

        private void RestoreRegionalSky()
        {
            if (_hasFogSnapshot && RenderSettings.skybox == _runtimeSky)
            {
                RenderSettings.fogColor = _previousFogColor;
                RenderSettings.fogDensity = _previousFogDensity;
                RenderSettings.fogStartDistance = _previousFogStart;
                RenderSettings.fogEndDistance = _previousFogEnd;
            }
            _hasFogSnapshot = false;
            _hasSkySample = false;
            _skyFrame = -1;
        }
    }
}
