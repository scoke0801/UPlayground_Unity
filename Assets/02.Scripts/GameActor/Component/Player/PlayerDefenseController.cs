using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>가드 내구도와 방어 성공 후 반격 윈도우의 단일 상태 소유자.</summary>
    public sealed class PlayerDefenseController
    {
        private readonly int _maxGuardCount;
        private readonly float _guardResetDelay;
        private readonly float _perfectGuardCounterWindow;
        private readonly float _parryCounterWindow;
        private readonly float _dodgeCounterWindow;
        private readonly float _assistParryWindow;
        private readonly float _perfectDodgeWindow;

        private int _guardHitCount;
        private float _guardEndTime = -999f;
        private float _perfectGuardCounterEndTime = -999f;
        private float _parryCounterEndTime = -999f;
        private float _dodgeCounterEndTime = -999f;
        private float _assistParryWindowEnd = -999f;
        private GameActor _dodgeCounterTarget;
        private float _perfectDodgeWindowEnd = -999f;

        public PlayerDefenseController(
            int maxGuardCount,
            float guardResetDelay,
            float perfectGuardCounterWindow,
            float parryCounterWindow,
            float dodgeCounterWindow,
            float assistParryWindow,
            float perfectDodgeWindow)
        {
            _maxGuardCount = Mathf.Max(1, maxGuardCount);
            _guardResetDelay = Mathf.Max(0f, guardResetDelay);
            _perfectGuardCounterWindow = Mathf.Max(0f, perfectGuardCounterWindow);
            _parryCounterWindow = Mathf.Max(0f, parryCounterWindow);
            _dodgeCounterWindow = Mathf.Max(0f, dodgeCounterWindow);
            _assistParryWindow = Mathf.Max(0f, assistParryWindow);
            _perfectDodgeWindow = Mathf.Max(0f, perfectDodgeWindow);
        }

        public bool IsGuardBroken { get; private set; }
        public int GuardHitCount => _guardHitCount;
        public int MaxGuardCount => _maxGuardCount;
        public bool IsPerfectGuardCounterAvailable => Time.time <= _perfectGuardCounterEndTime;
        public bool IsParryCounterAvailable => Time.time <= _parryCounterEndTime;
        public bool IsDodgeCounterAvailable => Time.time <= _dodgeCounterEndTime;
        public GameActor DodgeCounterTarget => IsDodgeCounterAvailable ? _dodgeCounterTarget : null;
        public bool IsAssistParryWindow => Time.time <= _assistParryWindowEnd;
        public float AssistParryWindowDuration => _assistParryWindow;
        public bool IsPerfectDodgeWindow => Time.time <= _perfectDodgeWindowEnd;

        public void OpenPerfectDodge() => _perfectDodgeWindowEnd = Time.time + _perfectDodgeWindow;
        public void ClosePerfectDodge() => _perfectDodgeWindowEnd = -999f;

        public void OpenPerfectGuardCounter(float duration = -1f)
            => _perfectGuardCounterEndTime = Time.time + ResolveDuration(duration, _perfectGuardCounterWindow);

        public void ClosePerfectGuardCounter() => _perfectGuardCounterEndTime = -999f;

        public bool ConsumePerfectGuardCounter()
        {
            if (!IsPerfectGuardCounterAvailable) return false;
            ClosePerfectGuardCounter();
            return true;
        }

        public void OpenParryCounter(float duration = -1f)
            => _parryCounterEndTime = Time.time + ResolveDuration(duration, _parryCounterWindow);

        public void CloseParryCounter() => _parryCounterEndTime = -999f;

        public void OpenDodgeCounter(GameActor target, float duration = -1f)
        {
            _dodgeCounterEndTime = Time.time + ResolveDuration(duration, _dodgeCounterWindow);
            _dodgeCounterTarget = target;
        }

        public bool ConsumeDodgeCounter()
        {
            if (!IsDodgeCounterAvailable) return false;
            CloseDodgeCounter();
            return true;
        }

        public void CloseDodgeCounter()
        {
            _dodgeCounterEndTime = -999f;
            _dodgeCounterTarget = null;
        }

        public void OpenAssistParry(float duration = -1f)
            => _assistParryWindowEnd = Time.time + ResolveDuration(duration, _assistParryWindow);

        public void CloseAssistParry() => _assistParryWindowEnd = -999f;

        public bool RegisterGuardHit()
        {
            if (IsGuardBroken) return true;
            _guardHitCount++;
            IsGuardBroken = _guardHitCount >= _maxGuardCount;
            return IsGuardBroken;
        }

        public bool CanGuard() => Time.time - _guardEndTime >= _guardResetDelay;

        public void BeginGuard()
        {
            IsGuardBroken = false;
            _guardHitCount = 0;
        }

        public void ConfirmGuardBreak() => _guardEndTime = Time.time;

        public void Reset()
        {
            _guardHitCount = 0;
            IsGuardBroken = false;
            _guardEndTime = -999f;
            ClosePerfectGuardCounter();
            CloseParryCounter();
            CloseDodgeCounter();
            CloseAssistParry();
            ClosePerfectDodge();
        }

        private static float ResolveDuration(float requested, float fallback)
            => Mathf.Max(0f, requested > 0f ? requested : fallback);
    }
}
