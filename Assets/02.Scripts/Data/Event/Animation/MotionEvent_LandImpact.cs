using System;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 착지 충격 판정 이벤트.
    /// Fly_Landing 애니메이션의 발 접지 프레임에 배치한다.
    /// EnemyLandState.OnLandImpact()를 호출해 범위 내 플레이어에게 데미지 + 넉백을 적용한다.
    /// </summary>
    [Serializable]
    public class LandImpactEvent : MotionEventBase
    {
        public override string GetDisplayName() => "Land Impact";
        public override string GetShortLabel()  => "LandImpact";

        public override void Execute(GameObject target)
        {
            // 착지 충격은 OnEnter 시점이 아닌 발 접지 프레임(startTime)에 1회 실행
            var landState = target.GetComponent<ActorMovementController>()
                                  ?.CurrentState as EnemyLandState;
            landState?.OnLandImpact();
        }

        public override void OnCompleteEvent(GameObject target) { }
    }
}
