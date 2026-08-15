using UnityEngine;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 연출용 스무스 회전 전환 (SetRotationSmooth → 매 프레임 보간).
    /// SmoothStep 또는 커스텀 AnimationCurve 보간 지원.
    /// </summary>
    public class CameraRotationTransition
    {
        public bool IsActive { get; private set; }
        public bool UnlockOnComplete { get; private set; }

        private float _startYaw, _startPitch;
        private float _targetYaw, _targetPitch;
        private float _elapsed, _duration;
        private AnimationCurve _curve;

        public void Start(float fromYaw, float fromPitch, float toYaw, float toPitch,
                          float duration,
                          AnimationCurve curve = null, bool unlockOnComplete = false)
        {
            if (duration <= 0f)
            {
                IsActive = false;
                return;
            }

            _startYaw = fromYaw;
            _startPitch = fromPitch;
            _targetYaw = toYaw;
            _targetPitch = toPitch;
            _elapsed = 0f;
            _duration = duration;
            _curve = curve;
            IsActive = true;
            UnlockOnComplete = unlockOnComplete;
        }

        public void Cancel()
        {
            IsActive = false;
            UnlockOnComplete = false;
        }

        /// <summary>
        /// 매 프레임 호출. 보간된 Yaw/Pitch를 out으로 반환.
        /// 전환 완료 시 IsActive = false가 된다.
        /// </summary>
        public void Update(float deltaTime, ref float yaw, ref float pitch)
        {
            if (!IsActive) return;

            _elapsed += deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            float smoothT = (_curve != null && _curve.length > 0)
                ? _curve.Evaluate(t)
                : Mathf.SmoothStep(0f, 1f, t);

            yaw = Mathf.LerpAngle(_startYaw, _targetYaw, smoothT);
            pitch = Mathf.Lerp(_startPitch, _targetPitch, smoothT);

            if (t >= 1f)
            {
                IsActive = false;
            }
        }
    }
}
