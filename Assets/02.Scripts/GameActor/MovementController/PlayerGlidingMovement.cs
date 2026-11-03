using UnityEngine;
using UPlayGround.Data.Actor;

namespace UPlayGround.MovementController
{
    /// <summary>활공의 방향·속도 적분을 KCC 상태와 같은 규칙으로 계산한다.</summary>
    public static class PlayerGlidingMovement
    {
        /// <summary>잡기 전 탈것은 수평 이동을 따라가고, 정점을 지난 뒤에는 탑승 높이를 유지한다.</summary>
        public static Vector3 GetCatchAnchor(Vector3 position, Vector3 up, float verticalSpeed, float gravity)
        {
            float ascentSpeed = Mathf.Max(0f, verticalSpeed);
            return position + up * (ascentSpeed * ascentSpeed / (2f * gravity));
        }

        /// <summary>보간된 본의 위치를 현재 물리 루트로 옮겨 렌더·물리 프레임 차이를 제거한다.</summary>
        public static Vector3 GetPhysicsPosePosition(Vector3 bonePosition, Vector3 renderPosition,
            Quaternion renderRotation, Vector3 physicsPosition, Quaternion physicsRotation)
        {
            return physicsPosition + physicsRotation * Quaternion.Inverse(renderRotation)
                * (bonePosition - renderPosition);
        }

        /// <summary>선회량을 연속적으로 보간해 날개가 입력 변화마다 튀지 않게 한다.</summary>
        public static float SmoothTurn(float current, float target, float blendTime, float deltaTime)
        {
            if (deltaTime <= 0f) return current;
            return Mathf.Lerp(target, current, Mathf.Exp(-deltaTime / blendTime));
        }

        /// <summary>진입과 복귀 임계값을 분리해 경계의 작은 흔들림으로 모션이 재시작되지 않게 한다.</summary>
        public static int SelectTurnPose(int current, float turn, float enterThreshold, float releaseThreshold)
        {
            if (turn >= enterThreshold) return 1;
            if (turn <= -enterThreshold) return -1;
            if (current > 0 && turn > releaseThreshold) return 1;
            if (current < 0 && turn < -releaseThreshold) return -1;
            return 0;
        }

        /// <summary>KCC에 구간 평균 속도를 전달해 프레임률에 따른 도약 높이 차이를 줄인다.</summary>
        public static float IntegrateBallisticStep(ref float verticalSpeed, float gravity, float deltaTime)
        {
            float average = verticalSpeed - gravity * deltaTime * 0.5f;
            verticalSpeed -= gravity * deltaTime;
            return average;
        }

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
            PlayerGlidingSettingsSO settings, float deltaTime, bool isBoosting = false)
        {
            float target = inputMagnitude > 0f
                ? Mathf.Lerp(settings.minimumSpeed, settings.cruiseSpeed, Mathf.Clamp01(inputMagnitude))
                : settings.cruiseSpeed;
            if (isBoosting) target = settings.boostSpeed;
            float acceleration = isBoosting ? settings.boostAcceleration : settings.horizontalAcceleration;
            float rate = target >= speed ? acceleration : settings.horizontalDeceleration;
            return Mathf.MoveTowards(speed, target, rate * deltaTime);
        }
    }
}
