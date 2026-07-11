using UnityEngine;

namespace UPlayGround.Input
{
    /// <summary>
    /// 입력 관련 유틸리티 함수
    /// </summary>
    public static class InputUtility
    {
        /// <summary>
        /// 카메라 기준 이동 방향 계산
        /// </summary>
        public static Vector3 GetCameraRelativeMovement(Vector2 inputDirection, Camera camera)
        {
            if (camera == null)
                return Vector3.zero;

            Transform cameraTransform = camera.transform;
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            // Y축 성분 제거 (평면 이동만)
            forward.y = 0;
            right.y = 0;

            forward.Normalize();
            right.Normalize();

            // 카메라 기준 이동 방향 계산
            return (forward * inputDirection.y + right * inputDirection.x).normalized;
        }

        /// <summary>
        /// 이동 방향으로 캐릭터 회전
        /// </summary>
        public static Quaternion GetRotationTowardsMovement(Vector3 movementDirection, float rotationSpeed, float deltaTime)
        {
            if (movementDirection.sqrMagnitude < 0.01f)
                return Quaternion.identity;

            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            return Quaternion.Slerp(Quaternion.identity, targetRotation, rotationSpeed * deltaTime);
        }

        /// <summary>
        /// 데드존 적용
        /// </summary>
        public static Vector2 ApplyDeadzone(Vector2 input, float deadzone = 0.15f)
        {
            float magnitude = input.magnitude;

            if (magnitude < deadzone)
                return Vector2.zero;

            // 데드존 이후 값을 재매핑 (0-1 범위)
            float adjustedMagnitude = (magnitude - deadzone) / (1f - deadzone);
            return input.normalized * Mathf.Min(adjustedMagnitude, 1f);
        }

        /// <summary>
        /// 입력 스무딩
        /// </summary>
        public static Vector2 SmoothInput(Vector2 currentInput, Vector2 targetInput, float smoothTime, ref Vector2 velocity, float deltaTime)
        {
            float x = Mathf.SmoothDamp(currentInput.x, targetInput.x, ref velocity.x, smoothTime, Mathf.Infinity, deltaTime);
            float y = Mathf.SmoothDamp(currentInput.y, targetInput.y, ref velocity.y, smoothTime, Mathf.Infinity, deltaTime);
            return new Vector2(x, y);
        }

        /// <summary>
        /// 8방향 스냅 (명조/원신 스타일)
        /// </summary>
        public static Vector2 SnapToEightDirections(Vector2 input)
        {
            if (input.sqrMagnitude < 0.01f)
                return Vector2.zero;

            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;

            // 8방향으로 스냅 (45도 간격)
            float snappedAngle = Mathf.Round(angle / 45f) * 45f;

            float rad = snappedAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        /// <summary>
        /// 더블 탭 감지
        /// </summary>
        public class DoubleTapDetector
        {
            private float _lastTapTime;
            private readonly float _doubleTapWindow;

            public DoubleTapDetector(float doubleTapWindow = 0.3f)
            {
                _doubleTapWindow = doubleTapWindow;
            }

            public bool CheckDoubleTap(bool inputPressed)
            {
                if (!inputPressed)
                    return false;

                float currentTime = Time.time;
                bool isDoubleTap = (currentTime - _lastTapTime) < _doubleTapWindow;
                _lastTapTime = currentTime;

                return isDoubleTap;
            }

            public void Reset()
            {
                _lastTapTime = 0f;
            }
        }

        /// <summary>
        /// 홀드 시간 감지
        /// </summary>
        public class HoldDetector
        {
            private float _holdStartTime;
            private bool _isHolding;
            private readonly float _holdThreshold;

            public bool IsHolding => _isHolding;
            public float HoldDuration => _isHolding ? Time.time - _holdStartTime : 0f;

            public HoldDetector(float holdThreshold = 0.5f)
            {
                _holdThreshold = holdThreshold;
            }

            public void Update(bool inputHeld)
            {
                if (inputHeld && !_isHolding)
                {
                    _holdStartTime = Time.time;
                }

                _isHolding = inputHeld && (Time.time - _holdStartTime >= _holdThreshold);
            }

            public void Reset()
            {
                _isHolding = false;
                _holdStartTime = 0f;
            }
        }
    }
}