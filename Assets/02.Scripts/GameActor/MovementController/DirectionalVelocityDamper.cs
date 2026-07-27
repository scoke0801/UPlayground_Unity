using System;
using UnityEngine;

namespace UPlayGround.MovementController
{
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

            // AddImpulse가 합산되는 최초 스텝에는 요청한 delta-v를 그대로 보존한다.
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
