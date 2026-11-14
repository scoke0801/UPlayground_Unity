using UnityEngine;
using UPlayGround.State;
using UPlayGround.Combat;

namespace UPlayGround.Components
{
    public partial class EnemyTacticalMemory
    {
        private GameActor _targetActor;
        private EnemyAIContext _selfContext;
        private GameActor _selfActor;
        public bool IsTargetCastingOrCharging { get; private set; }
        public bool IsTargetBehind { get; private set; }
        public bool IsPlayerLowHealth { get; private set; }
        public float LastGuardBrokenTime { get; private set; } = float.NegativeInfinity;

        private void Awake()
        {
            _selfContext = GetComponent<EnemyAIContext>();
            _selfActor = GetComponent<GameActor>();
        }

        private void ClearTacticalRead()
        {
            IsTargetCastingOrCharging = false;
            IsTargetBehind = false;
            IsPlayerLowHealth = false;
        }

        private void UpdateTacticalRead()
        {
            ClearTacticalRead();
            if (!_isPlayerTracked || _playerTransform == null) return;
            var data = _selfContext != null ? _selfContext.BehaviorData : null;
            var state = _playerController != null ? _playerController.CurrentState : null;
            IsTargetCastingOrCharging = state != null && (state.StateTags & ActorStateTag.CastingOrCharging) != 0;
            if (data == null) return;
            IsTargetBehind = IsWithinRearArc(transform.forward, _playerTransform.position - transform.position, data.rearAngle);
            IsPlayerLowHealth = _targetActor is IDamageable target && target.GetHealthPercent() <= data.playerLowHealthThreshold;
        }

        /// <summary>높이를 제외한 후방 원뿔 안에 타겟이 있는지 판정한다.</summary>
        public static bool IsWithinRearArc(Vector3 forward, Vector3 toTarget, float fullAngle)
        {
            forward.y = 0f;
            toTarget.y = 0f;
            if (forward.sqrMagnitude <= Mathf.Epsilon || toTarget.sqrMagnitude <= Mathf.Epsilon) return false;
            return Vector3.Dot(-forward.normalized, toTarget.normalized)
                >= Mathf.Cos(Mathf.Clamp(fullAngle, 0f, 360f) * 0.5f * Mathf.Deg2Rad) - 0.000001f;
        }

        /// <summary>실제 가드 파괴를 처리하는 실행 경로에서만 기록한다.</summary>
        public void NotifyGuardBroken() => LastGuardBrokenTime = Time.time;

        private void ObserveGuardBreak(CombatResult result)
        {
            if (_selfActor != null && result.Victim == _selfActor && result.DefenseOutcome == DefenseOutcome.GuardBreak)
                NotifyGuardBroken();
        }

        public bool WasGuardBrokenRecently()
        {
            var data = _selfContext != null ? _selfContext.BehaviorData : null;
            return data != null && IsGuardBreakMemoryActive(LastGuardBrokenTime, Time.time, data.guardBrokenMemoryDuration);
        }

        public static bool IsGuardBreakMemoryActive(float lastTime, float now, float duration)
            => duration > 0f && now >= lastTime && now - lastTime < duration;
    }
}
