using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.Enum;
using UPlayGround.Data.Event;

namespace UPlayGround
{
    public interface IDamageable
    {
        // <summary>
        /// 데미지를 받는다
        /// </summary>
        /// <param name="attackData">공격 정보</param>
        void TakeDamage(AttackData attackData);
        
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
    }
}