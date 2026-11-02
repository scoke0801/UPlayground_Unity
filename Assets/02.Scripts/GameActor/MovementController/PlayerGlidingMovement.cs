using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.MovementController
{
    /// <summary>활공의 방향·속도 적분을 KCC 상태와 같은 규칙으로 계산한다.</summary>
    public static class PlayerGlidingMovement
    {
        /// <summary>무입력 방향을 유지하고 스틱 크기에 비례해 방향을 바꾼다.</summary>
        public static Vector3 RotateHeading(Vector3 heading, Vector3 input, Vector3 up,
            float turnSpeed, float deltaTime)
        {
            Vector3 planarInput = Vector3.ProjectOnPlane(input, up);
            float magnitude = Mathf.Clamp01(planarInput.magnitude);
            if (magnitude <= 0f || deltaTime <= 0f)
                return heading;

            float angle = Vector3.SignedAngle(heading, planarInput, up);
            float step = Mathf.Clamp(angle, -turnSpeed * magnitude * deltaTime,
                turnSpeed * magnitude * deltaTime);
            return (Quaternion.AngleAxis(step, up) * heading).normalized;
        }

        /// <summary>목표 하강 속도에 수렴시키며 양의 상승 속도를 허용하지 않는다.</summary>
        public static float IntegrateDescent(float verticalSpeed, float descentSpeed,
            float blendTime, float deltaTime)
        {
            if (deltaTime <= 0f) return verticalSpeed;
            float decay = Mathf.Exp(-deltaTime / blendTime);
            return -descentSpeed + (Mathf.Min(verticalSpeed, 0f) + descentSpeed) * decay;
        }

        /// <summary>아날로그 입력 크기를 목표 속도에도 보존한다.</summary>
        public static float IntegrateSpeed(float speed, float inputMagnitude,
            PlayerGlidingSettingsSO settings, float deltaTime)
        {
            float target = inputMagnitude > 0f
                ? Mathf.Lerp(settings.minimumSpeed, settings.cruiseSpeed, Mathf.Clamp01(inputMagnitude))
                : settings.cruiseSpeed;
            float rate = target >= speed ? settings.horizontalAcceleration : settings.horizontalDeceleration;
            return Mathf.MoveTowards(speed, target, rate * deltaTime);
        }
    }
}
