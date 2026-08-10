using System;
using UnityEngine;

namespace UPlayGround.MovementController
{
    /// <summary>
    /// 수직 Launch가 기존 상승/하강 속도와 결합되는 정책.
    /// 피격 Launch는 Replace를 사용해 점프·다중 피격과의 무제한 합산을 막고,
    /// 모션 이벤트의 보조 상승은 AtLeast를 사용해 더 강한 기존 탄도를 보존한다.
    /// Additive는 명시적으로 누적이 필요한 특수 동작에만 사용한다.
    /// </summary>
    public enum VerticalLaunchVelocityPolicy
    {
        Replace,
        AtLeast,
        Additive,
    }

    /// <summary>
    /// 한 KCC 스텝에 들어온 수직 Launch 요청을 호출 순서와 무관하게 합성한다.
    /// Replace/AtLeast 요청은 각각 가장 강한 값 하나만 남기므로 같은 피격이
    /// 여러 경로에서 들어와도 수직 속도가 요청 횟수만큼 폭증하지 않는다.
    /// </summary>
    public struct PendingVerticalLaunch
    {
        private bool _hasReplacement;
        private float _replacementSpeed;
        private bool _hasMinimum;
        private float _minimumSpeed;
        private float _additiveSpeed;

        public bool HasValue =>
            _hasReplacement || _hasMinimum || _additiveSpeed > 0f;

        public void Enqueue(
            float upwardSpeed,
            VerticalLaunchVelocityPolicy policy)
        {
            float speed = Mathf.Max(0f, upwardSpeed);
            if (speed <= 0f)
                return;

            switch (policy)
            {
                case VerticalLaunchVelocityPolicy.Replace:
                    _replacementSpeed = _hasReplacement
                        ? Mathf.Max(_replacementSpeed, speed)
                        : speed;
                    _hasReplacement = true;
                    break;
                case VerticalLaunchVelocityPolicy.AtLeast:
                    _minimumSpeed = _hasMinimum
                        ? Mathf.Max(_minimumSpeed, speed)
                        : speed;
                    _hasMinimum = true;
                    break;
                case VerticalLaunchVelocityPolicy.Additive:
                    _additiveSpeed += speed;
                    break;
            }
        }

        public float Resolve(float currentUpwardSpeed)
        {
            float resolved = _hasReplacement
                ? _replacementSpeed
                : currentUpwardSpeed;
            if (_hasMinimum)
                resolved = Mathf.Max(resolved, _minimumSpeed);
            return resolved + _additiveSpeed;
        }

        public void Clear() => this = default;
    }

    public static class ExternalVelocityPolicy
    {
        /// <summary>
        /// 저작 모션의 상향 속도를 컨트롤러 한계 안으로 제한한다.
        /// 상태가 상향 이동을 금지하면 0을 반환한다.
        /// </summary>
        public static float ClampAuthoredUpwardSpeed(
            float requestedSpeed,
            float maximumSpeed,
            bool allowsUpwardVelocity)
        {
            if (!allowsUpwardVelocity)
                return 0f;
            return Mathf.Clamp(requestedSpeed, 0f, Mathf.Max(0f, maximumSpeed));
        }
    }

    /// <summary>
    /// 충돌 해결이 끝난 실제 속도에서 지정 방향 성분만 감쇠한다.
    /// 벽 충돌로 해당 성분이 이미 사라졌다면 반대 방향 속도를 만들지 않는다.
    /// </summary>
    [Serializable]
    public struct DirectionalVelocityDamper
    {
        public Vector3 Direction { get; private set; }
        public float RemainingSpeed { get; private set; }
        public float Drag { get; private set; }
        private bool _hasStarted;

        public bool IsActive => RemainingSpeed > 0.01f;

        public DirectionalVelocityDamper(Vector3 velocity, float drag)
        {
            float speed = velocity.magnitude;
            Direction = speed > 0.0001f ? velocity / speed : Vector3.zero;
            RemainingSpeed = speed;
            Drag = Mathf.Max(0f, drag);
            _hasStarted = false;
        }

        public void Apply(ref Vector3 velocity, float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f || Direction.sqrMagnitude < 0.999f)
                return;

            // AddPlanarKnockback이 합산되는 최초 스텝에는 요청한 delta-v를 그대로 보존한다.
            if (!_hasStarted)
            {
                _hasStarted = true;
                return;
            }

            float decayRatio = 1f - Mathf.Exp(-Drag * deltaTime);
            float requestedRemoval = RemainingSpeed * decayRatio;
            float availableSpeed = Mathf.Max(0f, Vector3.Dot(velocity, Direction));
            velocity -= Direction * Mathf.Min(availableSpeed, requestedRemoval);
            RemainingSpeed *= 1f - decayRatio;
        }
    }
}
