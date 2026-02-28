using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// 적 전술 기억 컴포넌트
    /// 전투 이벤트를 기억해 EnemyBrain의 다음 행동 결정에 맥락을 제공한다.
    /// </summary>
    public class EnemyTacticalMemory : MonoBehaviour
    {
        // ---- 연속 행동 카운터 ----
        public int ConsecutiveAttackCount { get; private set; }

        // ---- 시간 기록 ----
        public float LastHitTime    { get; private set; } = -999f;
        public float LastBlockTime  { get; private set; } = -999f;

        // ---- 플레이어 행동 관찰 ----
        private int   _playerDodgeCount;
        private float _dodgeWindowTimer;
        private const float DODGE_WINDOW = 5f;

        private void Update()
        {
            _dodgeWindowTimer += Time.deltaTime;
            if (_dodgeWindowTimer >= DODGE_WINDOW)
            {
                _dodgeWindowTimer = 0f;
                _playerDodgeCount = 0;
            }
        }

        // ---- 외부 알림 ----

        public void NotifyAttackLanded()
        {
            ConsecutiveAttackCount++;
        }

        /// <summary> 공격이 빗나갔을 때 (플레이어 회피 성공) </summary>
        public void NotifyAttackMissed()
        {
            _playerDodgeCount++;
            _dodgeWindowTimer = 0f;
        }

        public void NotifyTookDamage()
        {
            LastHitTime = Time.time;
            ConsecutiveAttackCount = 0;
        }

        public void NotifyBlocked()
        {
            LastBlockTime = Time.time;
        }

        public void ResetAttackCount() => ConsecutiveAttackCount = 0;

        // ---- 상태 질의 ----

        public bool WasHitRecently(float hitWindow = 2f)
            => Time.time - LastHitTime < hitWindow;

        public bool DidBlockRecently(float blockWindow = 1.5f)
            => Time.time - LastBlockTime < blockWindow;

        public bool IsPlayerDodgingFrequently(int threshold = 2)
            => _playerDodgeCount >= threshold;

        public bool IsOverAttacking(int limit = 3)
            => ConsecutiveAttackCount >= limit;
    }
}
