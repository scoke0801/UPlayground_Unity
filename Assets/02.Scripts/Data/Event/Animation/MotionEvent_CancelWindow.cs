using System;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 캔슬(인터럽트) 허용 구간 이벤트.
    /// 공격 모션 타임라인에서 "이 구간 동안 다른 행동(회피/대시/공격타입 등)으로 끊을 수 있다"를 저작한다.
    /// 시작(Execute)에 윈도우를 열고 끝(OnCompleteEvent)에 닫는다 — ComboWindowEvent와 동일 패턴.
    ///
    /// "무엇을 캔슬"은 공격 데이터의 전역 interruptActions, "언제 캔슬"은 본 이벤트 구간이 결정한다.
    /// 이벤트가 하나도 없는 공격은 기존 폴백(히트박스 콜리전 비활성 = 캔슬 가능)으로 동작한다(무회귀).
    /// </summary>
    [Serializable]
    public class CancelWindowEvent : MotionEventBase
    {
        [Tooltip("None이면 공격의 전역 interruptActions를 그대로 허용한다.\n지정하면 이 구간에서는 전역 마스크와의 교집합으로 좁힌다(예: 선딜 후반은 Dodge만).")]
        public PlayerInterruptAction maskOverride = PlayerInterruptAction.None;

        public override string GetDisplayName() => "CancelWindow";

        public override string GetShortLabel()
            => maskOverride == PlayerInterruptAction.None ? "CancelWindow" : $"CancelWindow [{maskOverride}]";

        public override void Execute(GameObject target) => Handle(target, true);

        public override void OnCompleteEvent(GameObject target) => Handle(target, false);

        private void Handle(GameObject target, bool open)
        {
            GameActor actor = target.GetComponent<GameActor>();
            if (actor == null || !actor.HasActorType(ActorType.Player))
                return;

            PlayerCombat combat = (actor as PlayerActor)?.GetCombat();
            if (combat == null)
                return;

            if (open)
                combat.OpenCancelWindow(maskOverride);
            else
                combat.CloseCancelWindow(maskOverride);
        }
    }
}
