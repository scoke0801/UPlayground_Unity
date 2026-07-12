using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.World
{
    /// <summary>
    /// 인게임 시각(GameTimeManager.DayProgress01)에 따라 태양/달 방향광 회전·색·세기,
    /// 주변광, URP Volume 노출을 매 프레임 보간하는 낮밤 조명 컨트롤러.
    ///
    /// 씬에 수동 배치하지 않는다 — WorldLightingManager가 씬 로드 후 동적 생성하고
    /// Bind()로 현재 씬의 Light/Volume 레퍼런스를 주입한다.
    /// 프리팹 없이 AddComponent로 생성될 때는 BuildDefaultProfile()이 기본 연출값을 채운다.
    /// </summary>
    public class WorldLightingController : MonoBehaviour
    {
        [Header("레퍼런스 (Bind로 주입)")]
        [SerializeField] private Light _sunLight;
        [SerializeField] private Light _moonLight;
        [SerializeField] private Volume _globalVolume;
        [SerializeField] private Light _characterFillLight;

        [Header("태양 궤도")]
        [Tooltip("태양 궤도의 방위각(Y 회전). 씬의 지형 방향에 맞춰 조정.")]
        [SerializeField] private float _sunAzimuth = 30f;

        [Header("시간대별 연출 (가로축 = 하루 진행도 0~1, 자정=0 / 정오=0.5)")]
        [SerializeField] private LightingKeyframe[] _lightingProfile;

        // 태양이 이 값 미만이면 라이트를 꺼서 지평선 아래 역광을 막는다.
        private const float MinLightIntensity = 0.005f;
        private const float MoonShadowActivationThreshold = 0.08f;

        private ColorAdjustments _colorAdjustments;
        private bool _volumeInitialized;

        // Volume.profile 접근으로 우리가 만들게 한 런타임 프로필 사본. 파괴 시 함께 정리해 누수를 막는다.
        private VolumeProfile _ownedProfileCopy;

        // 씬 원본 값 복원용
        private Color _originalAmbientColor;
        private float _originalAmbientIntensity;
        private Light _originalSun;
        private bool _originalsCaptured;
        private readonly List<DirectionalLightShadowState> _directionalLightShadowStates = new();
        private int _characterFillCullingMask;

        /// <summary> 현재 씬의 조명 레퍼런스를 주입한다. null 허용(해당 축 제어 생략). </summary>
        public void Bind(Light sunLight, Light moonLight, Volume globalVolume)
        {
            _sunLight = sunLight;
            _moonLight = moonLight;
            _globalVolume = globalVolume;
            _volumeInitialized = false;
            _colorAdjustments = null;

            if (!_originalsCaptured)
            {
                _originalAmbientColor = RenderSettings.ambientLight;
                _originalAmbientIntensity = RenderSettings.ambientIntensity;
                _originalSun = RenderSettings.sun;
                _originalsCaptured = true;
            }

            ConfigureRealtimeShadowOwnership();
            EnsureCharacterFillLight();
        }

        private void Update()
        {
            var time = GameTimeManager.Instance;
            if (time == null) return;

            float t = time.DayProgress01;
            LightingSample sample = EvaluateLighting(t);
            ApplySun(t, sample);
            ApplyMoon(t, sample);
            ApplyAmbient(sample);
            ApplyVolume(sample);
            ApplyShadowOwnership(sample);
            ApplyCharacterFill(sample);
        }

        private void OnDestroy()
        {
            if (_ownedProfileCopy != null)
            {
                Destroy(_ownedProfileCopy);
                _ownedProfileCopy = null;
            }

            if (!_originalsCaptured) return;
            RenderSettings.ambientLight = _originalAmbientColor;
            RenderSettings.ambientIntensity = _originalAmbientIntensity;
            RenderSettings.sun = _originalSun;

            foreach (var state in _directionalLightShadowStates)
            {
                if (state.Light != null)
                    state.Light.shadows = state.OriginalShadows;
            }
            _directionalLightShadowStates.Clear();

            if (_characterFillLight != null)
            {
                Destroy(_characterFillLight.gameObject);
                _characterFillLight = null;
            }
        }

        // ── 축별 적용 ────────────────────────────────────────────────

        private void ApplySun(float t, LightingSample sample)
        {
            if (_sunLight == null) return;

            // t=0 자정(-90도, 지평선 아래) → t=0.5 정오(90도, 머리 위)
            _sunLight.transform.rotation = Quaternion.Euler(t * 360f - 90f, _sunAzimuth, 0f);
            _sunLight.color = sample.SunColor;
            _sunLight.intensity = sample.SunIntensity;
            _sunLight.enabled = sample.SunIntensity > MinLightIntensity;
        }

        private void ConfigureRealtimeShadowOwnership()
        {
            _directionalLightShadowStates.Clear();

            if (_sunLight != null)
            {
                RenderSettings.sun = _sunLight;
                CaptureDirectionalLightShadowState(_sunLight);

                if (_sunLight.shadows == LightShadows.None)
                    _sunLight.shadows = LightShadows.Soft;
            }

            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light == null || light.type != LightType.Directional || light == _sunLight || light == _moonLight)
                    continue;

                CaptureDirectionalLightShadowState(light);
                light.shadows = LightShadows.None;
            }
        }

        private void CaptureDirectionalLightShadowState(Light light)
        {
            foreach (var state in _directionalLightShadowStates)
            {
                if (state.Light == light)
                    return;
            }

            _directionalLightShadowStates.Add(new DirectionalLightShadowState(light, light.shadows));
        }

        private void ApplyMoon(float t, LightingSample sample)
        {
            if (_moonLight == null) return;

            // 달은 태양 반대편 궤도
            _moonLight.transform.rotation = Quaternion.Euler(t * 360f + 90f, _sunAzimuth, 0f);
            _moonLight.color = sample.MoonColor;
            _moonLight.intensity = sample.MoonIntensity;
            _moonLight.enabled = sample.MoonIntensity > MinLightIntensity;
        }

        private void ApplyAmbient(LightingSample sample)
        {
            if (RenderSettings.ambientMode == AmbientMode.Flat)
            {
                RenderSettings.ambientLight = sample.AmbientColor;
            }
            else
            {
                // Skybox/Trilight 모드는 색 대신 강도 배율로 밤 어둡기를 만든다.
                RenderSettings.ambientIntensity = _originalAmbientIntensity * sample.AmbientIntensity;
            }
        }

        private void ApplyVolume(LightingSample sample)
        {
            if (_globalVolume == null) return;

            if (!_volumeInitialized)
            {
                _volumeInitialized = true;

                // Volume.profile 프로퍼티는 sharedProfile의 런타임 사본을 만들어 에셋 오염을 막는다.
                // 다른 시스템이 이미 사본을 만든 경우는 우리 소유가 아니므로 파괴 대상에서 제외한다.
                bool alreadyInstantiated = _globalVolume.HasInstantiatedProfile();
                var profile = _globalVolume.profile;
                if (profile == null) return;
                if (!alreadyInstantiated)
                    _ownedProfileCopy = profile;

                if (!profile.TryGet(out _colorAdjustments))
                    _colorAdjustments = profile.Add<ColorAdjustments>(false);
            }

            if (_colorAdjustments == null) return;
            _colorAdjustments.postExposure.overrideState = true;
            _colorAdjustments.postExposure.value = sample.PostExposure;
        }

        private void ApplyShadowOwnership(LightingSample sample)
        {
            Light shadowLight = _sunLight;
            if (_moonLight != null
                && sample.MoonIntensity > MoonShadowActivationThreshold
                && sample.MoonIntensity > sample.SunIntensity)
            {
                shadowLight = _moonLight;
            }

            foreach (var state in _directionalLightShadowStates)
            {
                if (state.Light == null) continue;
                state.Light.shadows = state.Light == shadowLight ? LightShadows.Soft : LightShadows.None;
            }
        }

        private void EnsureCharacterFillLight()
        {
            _characterFillCullingMask = ResolveCharacterFillCullingMask();
            if (_characterFillCullingMask == 0)
                return;

            if (_characterFillLight != null)
                return;

            var go = new GameObject("World Character Fill Light");
            go.transform.SetParent(transform, worldPositionStays: false);
            _characterFillLight = go.AddComponent<Light>();
            _characterFillLight.type = LightType.Spot;
            _characterFillLight.shadows = LightShadows.None;
            _characterFillLight.renderMode = LightRenderMode.ForcePixel;
            _characterFillLight.cullingMask = ResolveCameraFillCullingMask();
            _characterFillLight.range = 28f;
            _characterFillLight.spotAngle = 46f;
            _characterFillLight.innerSpotAngle = 32f;
            _characterFillLight.intensity = 0f;
        }

        private void ApplyCharacterFill(LightingSample sample)
        {
            if (_characterFillLight == null)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                _characterFillLight.transform.position = mainCamera.transform.position;
                _characterFillLight.transform.rotation = mainCamera.transform.rotation;
            }

            _characterFillLight.color = sample.CharacterFillColor;
            _characterFillLight.intensity = sample.CharacterFillIntensity;
            _characterFillLight.enabled = sample.CharacterFillIntensity > MinLightIntensity;
        }

        private static int ResolveCharacterFillCullingMask()
        {
            int mask = 0;
            AddLayerToMask("Player", ref mask);
            AddLayerToMask("Enemy", ref mask);
            AddLayerToMask("Npc", ref mask);
            return mask;
        }

        private static int ResolveCameraFillCullingMask()
        {
            int mask = ResolveCharacterFillCullingMask();
            AddLayerToMask("Default", ref mask);
            return mask;
        }

        private static void AddLayerToMask(string layerName, ref int mask)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                mask |= 1 << layer;
        }

        private LightingSample EvaluateLighting(float t)
        {
            if (_lightingProfile == null || _lightingProfile.Length == 0)
                BuildDefaultProfile();

            if (_lightingProfile.Length == 1)
                return _lightingProfile[0].ToSample();

            LightingKeyframe previous = _lightingProfile[0];
            LightingKeyframe next = _lightingProfile[0];

            for (int i = 0; i < _lightingProfile.Length; i++)
            {
                var current = _lightingProfile[i];
                var candidateNext = _lightingProfile[(i + 1) % _lightingProfile.Length];
                float currentTime = current.time01;
                float nextTime = candidateNext.time01;
                bool wrapsMidnight = nextTime <= currentTime;

                if ((!wrapsMidnight && t >= currentTime && t <= nextTime)
                    || (wrapsMidnight && (t >= currentTime || t <= nextTime)))
                {
                    previous = current;
                    next = candidateNext;
                    break;
                }
            }

            float span = Mathf.Repeat(next.time01 - previous.time01, 1f);
            if (span <= 0.0001f)
                return next.ToSample();

            float offset = Mathf.Repeat(t - previous.time01, 1f);
            float blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(offset / span));
            return LightingSample.Lerp(previous.ToSample(), next.ToSample(), blend);
        }

        // ── 기본 연출값 ──────────────────────────────────────────────

        /// <summary>
        /// 프리팹 없이 런타임 AddComponent로 생성될 때 호출되는 기본 연출값.
        /// 프리팹/인스펙터로 저작한 값이 있으면 호출하지 않는다.
        /// </summary>
        public void BuildDefaultProfile()
        {
            _lightingProfile = new[]
            {
                new LightingKeyframe(0.00f, 0.00f, 0.60f, 0.90f, -0.05f, 1.35f, new Color(0.34f, 0.38f, 0.55f), new Color(0.55f, 0.62f, 0.85f), new Color(0.40f, 0.44f, 0.58f), new Color(0.48f, 0.54f, 0.76f)),
                new LightingKeyframe(0.22f, 0.08f, 0.48f, 0.92f, -0.03f, 1.35f, new Color(0.72f, 0.54f, 0.42f), new Color(0.55f, 0.62f, 0.85f), new Color(0.44f, 0.46f, 0.56f), new Color(0.62f, 0.58f, 0.52f)),
                new LightingKeyframe(0.28f, 0.65f, 0.04f, 0.88f, -0.03f, 0.75f, new Color(1.00f, 0.62f, 0.42f), new Color(0.42f, 0.46f, 0.65f), new Color(0.48f, 0.43f, 0.42f), new Color(0.82f, 0.62f, 0.48f)),
                new LightingKeyframe(0.38f, 1.05f, 0.00f, 0.95f, 0.00f, 0.25f, new Color(1.00f, 0.92f, 0.82f), new Color(0.42f, 0.46f, 0.65f), new Color(0.55f, 0.56f, 0.60f), new Color(0.86f, 0.80f, 0.72f)),
                new LightingKeyframe(0.50f, 1.25f, 0.00f, 1.00f, 0.00f, 0.18f, new Color(1.00f, 0.98f, 0.94f), new Color(0.42f, 0.46f, 0.65f), new Color(0.58f, 0.58f, 0.60f), new Color(0.90f, 0.86f, 0.80f)),
                new LightingKeyframe(0.72f, 0.95f, 0.00f, 0.92f, 0.00f, 0.30f, new Color(1.00f, 0.88f, 0.72f), new Color(0.42f, 0.46f, 0.65f), new Color(0.55f, 0.54f, 0.56f), new Color(0.86f, 0.76f, 0.64f)),
                new LightingKeyframe(0.80f, 0.32f, 0.30f, 0.85f, -0.03f, 1.15f, new Color(1.00f, 0.50f, 0.30f), new Color(0.55f, 0.62f, 0.85f), new Color(0.50f, 0.44f, 0.48f), new Color(0.72f, 0.52f, 0.46f)),
                new LightingKeyframe(0.87f, 0.00f, 0.55f, 0.90f, -0.05f, 1.35f, new Color(0.34f, 0.38f, 0.55f), new Color(0.55f, 0.62f, 0.85f), new Color(0.40f, 0.44f, 0.58f), new Color(0.48f, 0.54f, 0.76f)),
            };
        }

        [System.Serializable]
        private struct LightingKeyframe
        {
            [Range(0f, 1f)] public float time01;
            [Min(0f)] public float sunIntensity;
            [Min(0f)] public float moonIntensity;
            [Min(0f)] public float ambientIntensity;
            public float postExposure;
            [Min(0f)] public float characterFillIntensity;
            public Color sunColor;
            public Color moonColor;
            public Color ambientColor;
            public Color characterFillColor;

            public LightingKeyframe(
                float time01,
                float sunIntensity,
                float moonIntensity,
                float ambientIntensity,
                float postExposure,
                float characterFillIntensity,
                Color sunColor,
                Color moonColor,
                Color ambientColor,
                Color characterFillColor)
            {
                this.time01 = time01;
                this.sunIntensity = sunIntensity;
                this.moonIntensity = moonIntensity;
                this.ambientIntensity = ambientIntensity;
                this.postExposure = postExposure;
                this.characterFillIntensity = characterFillIntensity;
                this.sunColor = sunColor;
                this.moonColor = moonColor;
                this.ambientColor = ambientColor;
                this.characterFillColor = characterFillColor;
            }

            public LightingSample ToSample()
            {
                return new LightingSample(
                    Mathf.Max(0f, sunIntensity),
                    Mathf.Max(0f, moonIntensity),
                    Mathf.Max(0f, ambientIntensity),
                    postExposure,
                    Mathf.Max(0f, characterFillIntensity),
                    sunColor,
                    moonColor,
                    ambientColor,
                    characterFillColor);
            }
        }

        private readonly struct LightingSample
        {
            public LightingSample(
                float sunIntensity,
                float moonIntensity,
                float ambientIntensity,
                float postExposure,
                float characterFillIntensity,
                Color sunColor,
                Color moonColor,
                Color ambientColor,
                Color characterFillColor)
            {
                SunIntensity = sunIntensity;
                MoonIntensity = moonIntensity;
                AmbientIntensity = ambientIntensity;
                PostExposure = postExposure;
                CharacterFillIntensity = characterFillIntensity;
                SunColor = sunColor;
                MoonColor = moonColor;
                AmbientColor = ambientColor;
                CharacterFillColor = characterFillColor;
            }

            public float SunIntensity { get; }
            public float MoonIntensity { get; }
            public float AmbientIntensity { get; }
            public float PostExposure { get; }
            public float CharacterFillIntensity { get; }
            public Color SunColor { get; }
            public Color MoonColor { get; }
            public Color AmbientColor { get; }
            public Color CharacterFillColor { get; }

            public static LightingSample Lerp(LightingSample a, LightingSample b, float t)
            {
                return new LightingSample(
                    Mathf.Lerp(a.SunIntensity, b.SunIntensity, t),
                    Mathf.Lerp(a.MoonIntensity, b.MoonIntensity, t),
                    Mathf.Lerp(a.AmbientIntensity, b.AmbientIntensity, t),
                    Mathf.Lerp(a.PostExposure, b.PostExposure, t),
                    Mathf.Lerp(a.CharacterFillIntensity, b.CharacterFillIntensity, t),
                    Color.Lerp(a.SunColor, b.SunColor, t),
                    Color.Lerp(a.MoonColor, b.MoonColor, t),
                    Color.Lerp(a.AmbientColor, b.AmbientColor, t),
                    Color.Lerp(a.CharacterFillColor, b.CharacterFillColor, t));
            }
        }

        private readonly struct DirectionalLightShadowState
        {
            public DirectionalLightShadowState(Light light, LightShadows originalShadows)
            {
                Light = light;
                OriginalShadows = originalShadows;
            }

            public Light Light { get; }
            public LightShadows OriginalShadows { get; }
        }
    }
}
