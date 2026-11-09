using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>고속 이동 유지 시간과 블러 진입·복귀를 카메라 렌더링과 분리해 계산한다.</summary>
    public sealed class SpeedRadialBlur
    {
        private float _heldTime;
        private bool _isFast;
        public float Strength { get; private set; }

        /// <summary>월드 이동을 원근 투영해 정면·측면·후면 비행에 맞는 화면 흐름을 계산한다.</summary>
        public static Vector3 CalculateFlow(Quaternion cameraRotation, float verticalFieldOfView,
            float aspect, Vector3 velocity)
        {
            if (velocity.sqrMagnitude < 0.0001f) return Vector3.forward;
            Vector3 local = Quaternion.Inverse(cameraRotation) * velocity.normalized;
            float focalLength = 0.5f / Mathf.Tan(Mathf.Clamp(verticalFieldOfView, 1f, 179f) * Mathf.Deg2Rad * 0.5f);
            return new Vector3(-local.x * focalLength / Mathf.Max(0.01f, aspect),
                -local.y * focalLength, local.z);
        }

        /// <summary>이동 불가·연출 전환에서는 즉시 해제하고 일반 감속에서는 빠르게 복귀한다.</summary>
        public void Advance(CameraSettings settings, CameraMotionContext motion, bool canShow, float deltaTime)
        {
            if (!canShow || settings == null || !settings.enableSpeedRadialBlur || !motion.IsAvailable
                || !motion.IsGliding || motion.IsGrounded)
            {
                Reset();
                return;
            }
            if (deltaTime <= 0f) return;
            float speed = motion.SpeedFeedback;
            // 진입·복귀 기준을 나눠 속도 경계의 작은 흔들림이 유지 시간을 계속 초기화하지 않게 한다.
            float threshold = _isFast ? Mathf.Min(settings.speedRadialBlurReleaseThreshold,
                settings.speedRadialBlurSpeedThreshold) : settings.speedRadialBlurSpeedThreshold;
            _isFast = speed > threshold;
            _heldTime = _isFast ? _heldTime + deltaTime : 0f;
            float target = _isFast && _heldTime >= settings.speedRadialBlurDelay
                ? Mathf.Pow(speed, Mathf.Max(0.01f, settings.speedRadialBlurResponseExponent))
                    * settings.glidingRadialBlurStrength
                : 0f;
            float blend = target > Strength ? settings.speedRadialBlurFadeIn : settings.speedRadialBlurFadeOut;
            // 임계 시간을 넘은 프레임에서도 실제 지연 이후의 시간만 진입 보간에 사용한다.
            float blendDelta = target > 0f
                ? Mathf.Min(deltaTime, Mathf.Max(0f, _heldTime - settings.speedRadialBlurDelay)) : deltaTime;
            Strength = Mathf.Lerp(target, Strength, Mathf.Exp(-blendDelta / Mathf.Max(0.01f, blend)));
            if (Strength < 0.00001f) Strength = 0f;
        }

        /// <summary>재진입 시 유지 시간과 이전 강도를 이어받지 않는다.</summary>
        public void Reset()
        {
            _heldTime = 0f;
            _isFast = false;
            Strength = 0f;
        }
    }

    /// <summary>카메라 모듈이 선택한 단일 플레이 카메라의 블러 값만 렌더 모듈에 전달한다.</summary>
    public static class SpeedRadialBlurState
    {
        public static Camera TargetCamera { get; private set; }
        public static float Strength { get; private set; }
        public static Vector2 Radii { get; private set; }
        public static Vector3 Flow { get; private set; }
        public static float MaxRadius { get; private set; }

        /// <summary>다른 카메라와 씬 뷰에 고속 이동 효과가 번지지 않도록 대상을 지정한다.</summary>
        public static void Set(Camera camera, float strength, float innerRadius, float outerRadius,
            Vector3? flow = null, float maxRadius = 0.065f)
        {
            TargetCamera = camera;
            Strength = Mathf.Clamp(strength, 0f, 0.1f);
            Radii = new Vector2(innerRadius, Mathf.Max(innerRadius + 0.01f, outerRadius));
            Flow = flow ?? Vector3.forward;
            MaxRadius = Mathf.Clamp(maxRadius, 0f, 0.15f);
        }

        /// <summary>씬 전환·종료·도메인 재초기화에서 이전 카메라를 해제한다.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            TargetCamera = null;
            Strength = 0f;
            Radii = Vector2.zero;
            Flow = Vector3.forward;
            MaxRadius = 0f;
        }
    }
}
