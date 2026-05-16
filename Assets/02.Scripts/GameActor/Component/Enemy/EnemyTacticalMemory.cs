using UnityEngine;
using UPlayGround.MovementController;

namespace UPlayGround.Component
{
    /// <summary>
    /// 적 전술 기억 컴포넌트
    /// 전투 이벤트를 기억해 EnemyAIController의 다음 행동 결정에 맥락을 제공한다.
    /// 플레이어의 현재 상태를 관찰하여 반응형 의사결정을 지원한다.
    /// </summary>
    public class EnemyTacticalMemory : MonoBehaviour
    {
        // ── 연속 행동 카운터 ──
        public int ConsecutiveAttackCount { get; private set; }

        // ── 시간 기록 ──
        public float LastHitTime       { get; private set; } = -999f;
        public float LastBlockTime     { get; private set; } = -999f;
        public float LastAttackTime    { get; private set; } = -999f;
        public float LastRetreatTime   { get; private set; } = -999f;

        // ── 플레이어 행동 관찰 ──
        private int   _playerDodgeCount;
        private float _dodgeWindowTimer;
        private int   _playerGuardCount;
        private float _guardWindowTimer;
        private const float DODGE_WINDOW = 5f;
        private const float GUARD_WINDOW = 6f;

        // ── 플레이어 상태 추적 ──
        private Transform _playerTransform;
        private ActorMovementController _playerController;
        private Vector3 _lastPlayerPosition;
        private float _playerIdleTimer;            // 플레이어가 움직이지 않는 시간
        private bool  _isPlayerTracked;

        // ── 전투 리듬 (공격 후 얼마나 빠르게 다음 행동을 할지) ──
        /// <summary> 마지막으로 전투 행동(공격/가드/차지 등)을 완료한 시점 </summary>
        public float LastCombatActionTime { get; private set; } = -999f;

        /// <summary> 현재 전투에서 공격이 적중한 총 횟수 </summary>
        public int TotalHitsLanded { get; private set; }

        /// <summary> 현재 전투에서 공격이 빗나간 총 횟수 </summary>
        public int TotalHitsMissed { get; private set; }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 회피 윈도우
            _dodgeWindowTimer += dt;
            if (_dodgeWindowTimer >= DODGE_WINDOW)
            {
                _dodgeWindowTimer = 0f;
                _playerDodgeCount = 0;
            }

            // 가드 윈도우
            _guardWindowTimer += dt;
            if (_guardWindowTimer >= GUARD_WINDOW)
            {
                _guardWindowTimer = 0f;
                _playerGuardCount = 0;
            }

            // 플레이어 이동 관찰
            UpdatePlayerObservation(dt);
        }

        // ══════════════════════════════════════════
        // 플레이어 관찰
        // ══════════════════════════════════════════

        /// <summary> Detection이 타겟을 잡으면 호출 </summary>
        public void SetPlayerTarget(Transform player)
        {
            if (player == null)
            {
                _isPlayerTracked = false;
                _playerTransform = null;
                _playerController = null;
                return;
            }

            _playerTransform  = player;
            _playerController = player.GetComponent<ActorMovementController>();
            _lastPlayerPosition = player.position;
            _playerIdleTimer = 0f;
            _isPlayerTracked = true;
        }

        private void UpdatePlayerObservation(float dt)
        {
            if (!_isPlayerTracked || _playerTransform == null) return;

            float moved = Vector3.Distance(_playerTransform.position, _lastPlayerPosition);
            _lastPlayerPosition = _playerTransform.position;

            if (moved < 0.05f)
                _playerIdleTimer += dt;
            else
                _playerIdleTimer = 0f;
        }

        /// <summary> 플레이어의 현재 State 이름 </summary>
        public string GetPlayerStateName()
        {
            return _playerController?.CurrentState?.StateName ?? "";
        }

        /// <summary> 플레이어가 공격 모션 중인가 </summary>
        public bool IsPlayerAttacking()
        {
            string s = GetPlayerStateName();
            return s is "Attack" or "DashAttack" or "JumpAttack" or "FinishAttack" or "HeavyAttack";
        }

        /// <summary> 플레이어가 가드 중인가 </summary>
        public bool IsPlayerGuarding()
        {
            return GetPlayerStateName() == "Guard";
        }

        /// <summary> 플레이어가 피격 경직 중인가 </summary>
        public bool IsPlayerStaggered()
        {
            return GetPlayerStateName() == "Hit";
        }

        /// <summary> 플레이어가 가만히 서 있는가 (일정 시간 이상 이동 없음) </summary>
        public bool IsPlayerIdle(float threshold = 0.8f)
        {
            return _playerIdleTimer >= threshold;
        }

        /// <summary> 플레이어가 회복 동작 중인가 (Idle + 일정 시간 무행동) </summary>
        public bool IsPlayerRecovering()
        {
            string s = GetPlayerStateName();
            return s == "Idle" && _playerIdleTimer >= 1.2f;
        }

        // ══════════════════════════════════════════
        // 외부 알림
        // ══════════════════════════════════════════

        public void NotifyAttackLanded()
        {
            ConsecutiveAttackCount++;
            TotalHitsLanded++;
            LastAttackTime = Time.time;
            LastCombatActionTime = Time.time;
        }

        public void NotifyAttackMissed()
        {
            _playerDodgeCount++;
            _dodgeWindowTimer = 0f;
            TotalHitsMissed++;
            LastAttackTime = Time.time;
            LastCombatActionTime = Time.time;
        }

        public void NotifyTookDamage()
        {
            LastHitTime = Time.time;
            ConsecutiveAttackCount = 0;
        }

        public void NotifyBlocked()
        {
            LastBlockTime = Time.time;
            LastCombatActionTime = Time.time;
        }

        public void NotifyPlayerGuarded()
        {
            _playerGuardCount++;
            _guardWindowTimer = 0f;
        }

        public void NotifyRetreated()
        {
            LastRetreatTime = Time.time;
            LastCombatActionTime = Time.time;
        }

        public void NotifyCombatAction()
        {
            LastCombatActionTime = Time.time;
        }

        public void ResetAttackCount() => ConsecutiveAttackCount = 0;

        /// <summary> 전투 시작 시 통계 리셋 </summary>
        public void ResetCombatStats()
        {
            TotalHitsLanded = 0;
            TotalHitsMissed = 0;
            ConsecutiveAttackCount = 0;
        }

        // ══════════════════════════════════════════
        // 상태 질의
        // ══════════════════════════════════════════

        public bool WasHitRecently(float hitWindow = 2f)
            => Time.time - LastHitTime < hitWindow;

        public bool DidBlockRecently(float blockWindow = 1.5f)
            => Time.time - LastBlockTime < blockWindow;

        public bool IsPlayerDodgingFrequently(int threshold = 2)
            => _playerDodgeCount >= threshold;

        public bool IsPlayerGuardingFrequently(int threshold = 2)
            => _playerGuardCount >= threshold;

        public bool IsOverAttacking(int limit = 3)
            => ConsecutiveAttackCount >= limit;

        /// <summary> 마지막 전투 행동 이후 경과 시간 </summary>
        public float TimeSinceLastCombatAction()
            => Time.time - LastCombatActionTime;

        /// <summary> 마지막 후퇴 이후 경과 시간 </summary>
        public float TimeSinceLastRetreat()
            => Time.time - LastRetreatTime;

        /// <summary> 적중률 (0~1). 데이터 부족 시 0.5 반환 </summary>
        public float GetHitAccuracy()
        {
            int total = TotalHitsLanded + TotalHitsMissed;
            return total > 0 ? (float)TotalHitsLanded / total : 0.5f;
        }
    }
}
