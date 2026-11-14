using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data.Combat;

namespace UPlayGround.Components
{
    /// <summary>가드 내구도와 방어 성공 후 반격 윈도우의 단일 상태 소유자.</summary>
    public sealed class PlayerDefenseController
    {
        private readonly PlayerActor _owner;
        private readonly int _maxGuardCount;
        private readonly float _guardResetDelay;
        private readonly float _perfectGuardCounterWindow;
        private readonly float _parryCounterWindow;
        private readonly float _dodgeCounterWindow;
        private readonly float _assistParryWindow;
        private readonly float _perfectGuardWindow;
        private readonly float _perfectDodgeWindow;

        private int _guardHitCount;
        private float _guardEndTime = -999f;
        private float _perfectGuardCounterEndTime = -999f;
        private float _parryCounterEndTime = -999f;
        private float _dodgeCounterEndTime = -999f;
        private float _assistParryWindowEnd = -999f;
        private GameActor _dodgeCounterTarget;
        private float _perfectDodgeWindowEnd = -999f;
        private bool _projectileGuardHeld;
        private ulong _projectileInputId;
        private ulong _consumedProjectileInputId;
        private ulong _projectileGuardActionId;
        private ulong _rewardedProjectileActionId;
        private float _projectileGuardStart = float.NegativeInfinity;
        private float _projectileGuardEnd = float.NegativeInfinity;
        private static ulong _nextAssistActionId;
        public ulong AssistActionId { get; private set; }

        public ulong ProjectileGuardActionId => _projectileGuardActionId;

        /// <summary>입력 해제 뒤의 실제 새 누름만 가드 진입에서 소비한다.</summary>
        public void SetProjectileGuardInput(bool held)
        {
            if (held && !_projectileGuardHeld) _projectileInputId++;
            _projectileGuardHeld = held;
        }

        public bool IsProjectileGuardWindow(float contactTime)
            => !IsGuardBroken && _projectileGuardActionId != 0
               && contactTime >= _projectileGuardStart && contactTime <= _projectileGuardEnd;

        public bool ConsumeProjectileReward(ulong actionId)
        {
            if (actionId == 0 || actionId == _rewardedProjectileActionId) return false;
            _rewardedProjectileActionId = actionId;
            return true;
        }

        public PlayerDefenseController(
            PlayerActor owner,
            int maxGuardCount,
            float guardResetDelay,
            float perfectGuardCounterWindow,
            float parryCounterWindow,
            float dodgeCounterWindow,
            float assistParryWindow,
            float perfectGuardWindow,
            float perfectDodgeWindow)
        {
            _owner = owner;
            _maxGuardCount = Mathf.Max(1, maxGuardCount);
            _guardResetDelay = Mathf.Max(0f, guardResetDelay);
            _perfectGuardCounterWindow = Mathf.Max(0f, perfectGuardCounterWindow);
            _parryCounterWindow = Mathf.Max(0f, parryCounterWindow);
            _dodgeCounterWindow = Mathf.Max(0f, dodgeCounterWindow);
            _assistParryWindow = Mathf.Max(0f, assistParryWindow);
            _perfectGuardWindow = Mathf.Max(0f, perfectGuardWindow);
            _perfectDodgeWindow = Mathf.Max(0f, perfectDodgeWindow);
        }

        public bool IsGuardBroken { get; private set; }
        public int GuardHitCount => _guardHitCount;
        public int MaxGuardCount => DefensePolicy?.ResolveMaxGuardCount(_maxGuardCount) ?? _maxGuardCount;
        private float Now => _owner != null ? _owner.ActorTime : Time.time;
        private CombatDefensePolicySO DefensePolicy => _owner?.Definition?.EffectiveCombatDefensePolicy;

        public bool IsPerfectGuardCounterAvailable => Now <= _perfectGuardCounterEndTime;
        public bool IsParryCounterAvailable => Now <= _parryCounterEndTime;
        public bool IsDodgeCounterAvailable => Now <= _dodgeCounterEndTime;
        public GameActor DodgeCounterTarget => IsDodgeCounterAvailable ? _dodgeCounterTarget : null;
        public bool IsAssistParryWindow => Now <= _assistParryWindowEnd;
        public float AssistParryWindowDuration =>
            DefensePolicy?.ResolveAssistParryWindow(_assistParryWindow) ?? _assistParryWindow;
        public float PerfectGuardWindowDuration =>
            DefensePolicy?.ResolvePerfectGuardWindow(_perfectGuardWindow) ?? _perfectGuardWindow;
        public bool IsPerfectDodgeWindow => Now <= _perfectDodgeWindowEnd;

        public void OpenPerfectDodge()
        {
            float duration = DefensePolicy?.ResolvePerfectDodgeWindow(_perfectDodgeWindow)
                             ?? _perfectDodgeWindow;
            _perfectDodgeWindowEnd = Now + duration;
        }
        public void ClosePerfectDodge() => _perfectDodgeWindowEnd = -999f;

        public void OpenPerfectGuardCounter(float duration = -1f)
            => _perfectGuardCounterEndTime = Now + ResolveDuration(duration, _perfectGuardCounterWindow);

        public void ClosePerfectGuardCounter() => _perfectGuardCounterEndTime = -999f;

        public bool ConsumePerfectGuardCounter()
        {
            if (!IsPerfectGuardCounterAvailable) return false;
            ClosePerfectGuardCounter();
            return true;
        }

        public void OpenParryCounter(float duration = -1f)
            => _parryCounterEndTime = Now + ResolveDuration(duration, _parryCounterWindow);

        public void CloseParryCounter() => _parryCounterEndTime = -999f;

        public void OpenDodgeCounter(GameActor target, float duration = -1f)
        {
            _dodgeCounterEndTime = Now + ResolveDuration(duration, _dodgeCounterWindow);
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
        {
            AssistActionId = ++_nextAssistActionId;
            _assistParryWindowEnd = Now + ResolveDuration(duration, AssistParryWindowDuration);
        }

        public void CloseAssistParry() => _assistParryWindowEnd = -999f;

        /// <summary>가드 피격을 적용하기 전에 최종 방어 결과를 부수효과 없이 계산한다.</summary>
        public DefenseOutcome PreviewGuardOutcome(bool isPerfectGuardWindow)
        {
            if (IsGuardBroken || _guardHitCount + 1 >= MaxGuardCount)
                return DefenseOutcome.GuardBreak;

            return isPerfectGuardWindow
                ? DefenseOutcome.PerfectGuard
                : DefenseOutcome.Block;
        }

        /// <summary>파이프라인이 확정한 가드 결과를 내구도 상태에 한 번만 반영한다.</summary>
        public void CommitGuardOutcome(DefenseOutcome outcome)
        {
            if (outcome is not (DefenseOutcome.Block
                or DefenseOutcome.PerfectGuard
                or DefenseOutcome.GuardBreak))
            {
                return;
            }

            _guardHitCount++;
            IsGuardBroken = outcome == DefenseOutcome.GuardBreak
                            || _guardHitCount >= MaxGuardCount;
        }

        public bool CanGuard()
        {
            float resetDelay = DefensePolicy?.ResolveGuardResetDelay(_guardResetDelay)
                               ?? _guardResetDelay;
            return Now - _guardEndTime >= resetDelay;
        }

        public void BeginGuard()
        {
            IsGuardBroken = false;
            _guardHitCount = 0;
            BeginProjectileGuard(Time.time, DefensePolicy);
        }

        /// <summary>실제 Guard 진입의 전역 시각에서 새 누름을 소비하고 투사체 창을 연다.</summary>
        public void BeginProjectileGuard(float now, CombatDefensePolicySO policy)
        {
            _projectileGuardEnd = float.NegativeInfinity;
            if (!_projectileGuardHeld || _projectileInputId == _consumedProjectileInputId) return;
            _consumedProjectileInputId = _projectileInputId;
            if (policy == null || now - _projectileGuardStart < policy.projectileParryRearmInterval) return;
            _projectileGuardActionId = _projectileInputId;
            _projectileGuardStart = now;
            _projectileGuardEnd = _projectileGuardStart + policy.projectilePerfectGuardWindow;
        }

        public void ConfirmGuardBreak()
        {
            _guardEndTime = Now;
            _guardHitCount = 0;
            IsGuardBroken = false;
            ClosePerfectGuardCounter();
            CloseParryCounter();
            CloseDodgeCounter();
            CloseAssistParry();
            ClosePerfectDodge();
        }

        public void Reset()
        {
            _projectileGuardHeld = false;
            _projectileInputId = _consumedProjectileInputId = _projectileGuardActionId = _rewardedProjectileActionId = 0;
            _projectileGuardStart = _projectileGuardEnd = float.NegativeInfinity;
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
