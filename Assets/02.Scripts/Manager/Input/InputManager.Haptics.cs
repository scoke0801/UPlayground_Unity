using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 전투 진동 수명주기. 연타 요청은 남은 시간 동안 강한 축을 보존하고 종료 시점을 연장한다.
    /// scaled time을 쓰지 않아 히트스톱 중에도 짧고 예측 가능한 촉감을 유지한다.
    /// </summary>
    public partial class InputManager
    {
        private const float MaxCombatHapticDuration = 0.35f;

        private Gamepad _hapticGamepad;
        private float _hapticLowFrequency;
        private float _hapticHighFrequency;
        private float _hapticEndTime = -1f;

        public void PlayCombatHaptic(float lowFrequency, float highFrequency, float duration)
        {
            var settings = Svc.Settings?.Data;
            if (settings != null && !settings.combatVibration)
            {
                StopHaptics();
                return;
            }

            if (_activeDevice != ActiveInputDevice.Gamepad)
                return;

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null || !gamepad.added)
                return;

            float intensity = settings != null
                ? Mathf.Clamp01(settings.combatVibrationIntensity)
                : 1f;
            float low = Mathf.Clamp01(lowFrequency) * intensity;
            float high = Mathf.Clamp01(highFrequency) * intensity;
            float clampedDuration = Mathf.Clamp(duration, 0f, MaxCombatHapticDuration);
            if (clampedDuration <= 0f || low <= 0f && high <= 0f)
                return;

            if (_hapticGamepad != gamepad)
                StopHaptics();

            _hapticGamepad = gamepad;
            _hapticLowFrequency = Mathf.Max(_hapticLowFrequency, low);
            _hapticHighFrequency = Mathf.Max(_hapticHighFrequency, high);
            _hapticEndTime = Mathf.Max(
                _hapticEndTime,
                Time.unscaledTime + clampedDuration);
            gamepad.SetMotorSpeeds(_hapticLowFrequency, _hapticHighFrequency);
        }

        public void StopHaptics()
        {
            if (_hapticGamepad != null)
            {
                try
                {
                    _hapticGamepad.SetMotorSpeeds(0f, 0f);
                }
                catch
                {
                    // 장치 제거 프레임에는 Input System 백엔드가 이미 해제됐을 수 있다.
                }
            }

            _hapticGamepad = null;
            _hapticLowFrequency = 0f;
            _hapticHighFrequency = 0f;
            _hapticEndTime = -1f;
        }

        private void TickHaptics()
        {
            var settings = Svc.Settings?.Data;
            if (settings != null && !settings.combatVibration)
            {
                StopHaptics();
                return;
            }

            if (_hapticGamepad == null)
                return;

            if (!_hapticGamepad.added || Time.unscaledTime >= _hapticEndTime)
                StopHaptics();
        }
    }
}
