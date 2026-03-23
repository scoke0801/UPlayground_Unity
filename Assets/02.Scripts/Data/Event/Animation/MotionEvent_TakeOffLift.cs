using System;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 이륙 리프트 이벤트.
    /// Fly_Start 애니메이션에서 발이 지면을 떠나는 프레임에 배치한다.
    /// EnemyTakeOffState.OnLiftOff()를 호출해 KCC GroundSolving을 비활성화한다.
    /// </summary>
    [Serializable]
    public class TakeOffLiftEvent : MotionEventBase
    {
        public override string GetDisplayName() => "TakeOff Lift";
        public override string GetShortLabel()  => "TakeOffLift";

        public override void Execute(GameObject target)
        {
            var takeOffState = target.GetComponent<ActorMovementController>()
                                     ?.CurrentState as EnemyTakeOffState;
            takeOffState?.OnLiftOff();
        }

        public override void OnCompleteEvent(GameObject target) { }
    }
}
