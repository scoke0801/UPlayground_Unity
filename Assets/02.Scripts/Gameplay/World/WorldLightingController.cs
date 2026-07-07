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

        [Header("태양 궤도")]
        [Tooltip("태양 궤도의 방위각(Y 회전). 씬의 지형 방향에 맞춰 조정.")]
        [SerializeField] private float _sunAzimuth = 30f;

        [Header("시간대별 연출 (가로축 = 하루 진행도 0~1, 자정=0 / 정오=0.5)")]
        [SerializeField] private Gradient _sunColorByTime;
        [SerializeField] private AnimationCurve _sunIntensityByTime;
        [SerializeField] private AnimationCurve _moonIntensityByTime;
        [SerializeField] private Gradient _ambientColorByTime;
        [Tooltip("Skybox 주변광 모드일 때 곱해지는 ambientIntensity 배율.")]
        [SerializeField] private AnimationCurve _ambientIntensityByTime;
        [Tooltip("URP ColorAdjustments.postExposure 값. 밤에 음수로 어둡게.")]
        [SerializeField] private AnimationCurve _exposureByTime;

        // 태양이 이 값 미만이면 라이트를 꺼서 지평선 아래 역광을 막는다.
        private const float MinLightIntensity = 0.005f;

        private ColorAdjustments _colorAdjustments;
        private bool _volumeInitialized;

        // Volume.profile 접근으로 우리가 만들게 한 런타임 프로필 사본. 파괴 시 함께 정리해 누수를 막는다.
        private VolumeProfile _ownedProfileCopy;

        // 씬 원본 값 복원용
        private Color _originalAmbientColor;
        private float _originalAmbientIntensity;
        private bool _originalsCaptured;

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
                _originalsCaptured = true;
            }
        }

        private void Update()
        {
            var time = GameTimeManager.Instance;
            if (time == null) return;

            float t = time.DayProgress01;
            ApplySun(t);
            ApplyMoon(t);
            ApplyAmbient(t);
            ApplyVolume(t);
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
        }

        // ── 축별 적용 ────────────────────────────────────────────────

        private void ApplySun(float t)
        {
            if (_sunLight == null) return;

            // t=0 자정(-90도, 지평선 아래) → t=0.5 정오(90도, 머리 위)
            _sunLight.transform.rotation = Quaternion.Euler(t * 360f - 90f, _sunAzimuth, 0f);

            if (_sunColorByTime != null)
                _sunLight.color = _sunColorByTime.Evaluate(t);

            float intensity = _sunIntensityByTime != null ? Mathf.Max(0f, _sunIntensityByTime.Evaluate(t)) : 1f;
            _sunLight.intensity = intensity;
            _sunLight.enabled = intensity > MinLightIntensity;
        }

        private void ApplyMoon(float t)
        {
            if (_moonLight == null) return;

            // 달은 태양 반대편 궤도
            _moonLight.transform.rotation = Quaternion.Euler(t * 360f + 90f, _sunAzimuth, 0f);

            float intensity = _moonIntensityByTime != null ? Mathf.Max(0f, _moonIntensityByTime.Evaluate(t)) : 0f;
            _moonLight.intensity = intensity;
            _moonLight.enabled = intensity > MinLightIntensity;
        }

        private void ApplyAmbient(float t)
        {
            if (RenderSettings.ambientMode == AmbientMode.Flat)
            {
                if (_ambientColorByTime != null)
                    RenderSettings.ambientLight = _ambientColorByTime.Evaluate(t);
            }
            else if (_ambientIntensityByTime != null)
            {
                // Skybox/Trilight 모드는 색 대신 강도 배율로 밤 어둡기를 만든다.
                RenderSettings.ambientIntensity =
                    _originalAmbientIntensity * Mathf.Max(0f, _ambientIntensityByTime.Evaluate(t));
            }
        }

        private void ApplyVolume(float t)
        {
            if (_globalVolume == null || _exposureByTime == null) return;

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
            _colorAdjustments.postExposure.value = _exposureByTime.Evaluate(t);
        }

        // ── 기본 연출값 ──────────────────────────────────────────────

        /// <summary>
        /// 프리팹 없이 런타임 AddComponent로 생성될 때 호출되는 기본 연출값.
        /// 프리팹/인스펙터로 저작한 값이 있으면 호출하지 않는다.
        /// </summary>
        public void BuildDefaultProfile()
        {
            _sunColorByTime = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.30f, 0.34f, 0.50f), 0.00f), // 자정
                    new GradientColorKey(new Color(0.30f, 0.34f, 0.50f), 0.21f), // 새벽 직전
                    new GradientColorKey(new Color(1.00f, 0.58f, 0.35f), 0.27f), // 일출
                    new GradientColorKey(new Color(1.00f, 0.96f, 0.88f), 0.38f), // 오전
                    new GradientColorKey(new Color(1.00f, 0.98f, 0.94f), 0.50f), // 정오
                    new GradientColorKey(new Color(1.00f, 0.96f, 0.88f), 0.70f), // 오후
                    new GradientColorKey(new Color(1.00f, 0.48f, 0.28f), 0.79f), // 일몰
                    new GradientColorKey(new Color(0.30f, 0.34f, 0.50f), 0.87f), // 밤
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                },
            };

            _sunIntensityByTime = new AnimationCurve(
                new Keyframe(0.00f, 0f),
                new Keyframe(0.22f, 0f),
                new Keyframe(0.30f, 0.9f),
                new Keyframe(0.50f, 1.3f),
                new Keyframe(0.72f, 1.0f),
                new Keyframe(0.82f, 0.15f),
                new Keyframe(0.87f, 0f),
                new Keyframe(1.00f, 0f));

            _moonIntensityByTime = new AnimationCurve(
                new Keyframe(0.00f, 0.25f),
                new Keyframe(0.18f, 0.22f),
                new Keyframe(0.25f, 0f),
                new Keyframe(0.83f, 0f),
                new Keyframe(0.90f, 0.20f),
                new Keyframe(1.00f, 0.25f));

            _ambientColorByTime = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.10f, 0.12f, 0.20f), 0.00f),
                    new GradientColorKey(new Color(0.10f, 0.12f, 0.20f), 0.21f),
                    new GradientColorKey(new Color(0.42f, 0.38f, 0.40f), 0.30f),
                    new GradientColorKey(new Color(0.55f, 0.56f, 0.60f), 0.45f),
                    new GradientColorKey(new Color(0.55f, 0.56f, 0.60f), 0.70f),
                    new GradientColorKey(new Color(0.40f, 0.30f, 0.30f), 0.80f),
                    new GradientColorKey(new Color(0.10f, 0.12f, 0.20f), 0.88f),
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                },
            };

            _ambientIntensityByTime = new AnimationCurve(
                new Keyframe(0.00f, 0.25f),
                new Keyframe(0.22f, 0.25f),
                new Keyframe(0.32f, 0.8f),
                new Keyframe(0.50f, 1.0f),
                new Keyframe(0.72f, 0.9f),
                new Keyframe(0.85f, 0.3f),
                new Keyframe(0.90f, 0.25f),
                new Keyframe(1.00f, 0.25f));

            _exposureByTime = new AnimationCurve(
                new Keyframe(0.00f, -1.2f),
                new Keyframe(0.22f, -1.2f),
                new Keyframe(0.30f, -0.35f),
                new Keyframe(0.42f, 0f),
                new Keyframe(0.70f, 0f),
                new Keyframe(0.80f, -0.45f),
                new Keyframe(0.88f, -1.2f),
                new Keyframe(1.00f, -1.2f));
        }
    }
}
