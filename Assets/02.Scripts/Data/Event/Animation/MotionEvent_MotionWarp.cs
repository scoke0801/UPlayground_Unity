using System;
using UnityEngine;
using UPlayGround.Component;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// Motion Warp 활성 구간 이벤트.
    /// startTime ~ endTime 구간 동안 IsMotionWarping = true.
    /// Execute 시 이벤트 구간 길이(endTime - startTime)를 Combat에 전달해
    /// AttackState가 정확한 남은 시간 기반으로 속력을 역산한다.
    /// 플레이어(PlayerCombat)와 몬스터(EnemyCombat) 모두 지원.
    /// </summary>
    [Serializable]
    public class MotionEvent_MotionWarp : MotionEventBase
    {
        public override string GetDisplayName() => "Motion Warp";
        public override string GetShortLabel()  => "Warp";

        public override void Execute(GameObject target)
        {
            float warpDuration = endTime - startTime;

            var playerCombat = target.GetComponent<PlayerCombat>()
                            ?? target.GetComponentInChildren<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.BeginMotionWarp(warpDuration);
                return;
            }

            var enemyCombat = target.GetComponent<EnemyCombat>()
                           ?? target.GetComponentInChildren<EnemyCombat>();
            enemyCombat?.BeginMotionWarp(warpDuration);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            var playerCombat = target.GetComponent<PlayerCombat>()
                            ?? target.GetComponentInChildren<PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.EndMotionWarp();
                return;
            }

            var enemyCombat = target.GetComponent<EnemyCombat>()
                           ?? target.GetComponentInChildren<EnemyCombat>();
            enemyCombat?.EndMotionWarp();
        }
    }
}
