using UnityEngine;
using UPlayGround.Combat;

namespace UPlayGround
{
    public interface IDamageable
    {
        // <summary>
        /// 데미지를 받는다
        /// </summary>
        /// <param name="request">피격 경계에서 고정된 공격 입력</param>
        CombatResult ReceiveHit(in HitRequest request);
        
        /// <summary>
        /// 생존 여부 확인
        /// </summary>
        bool IsAlive();
        
        /// <summary>
        /// 피격 가능한 상태인지 확인 (무적 상태 등)
        /// </summary>
        bool CanTakeDamage();
        
        /// <summary>
        /// 액터의 Transform 반환 (히트 포인트 계산용)
        /// </summary>
        Transform GetTransform();

        /// <summary>
        /// 락온
        /// </summary>
        void LockOn();
        
        /// <summary>
        /// 락온해제
        /// </summary>
        void UnLockOn();

        float GetHealthPercent();

        float GetCurrentHealth();

        void ApplyHealingEffect(float healAmount);
    }
}
