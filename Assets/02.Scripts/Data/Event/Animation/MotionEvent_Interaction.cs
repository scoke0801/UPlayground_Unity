using System;
using UPlayGround.Data.EnumType;
using UnityEngine;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 플레이어 인터렉션 모션 타임라인에서 현재 상호작용 대상에 처리 타이밍을 전달한다.
    /// 채광/벌목/채집은 OnHit, 낚시는 CatchFish를 사용한다.
    /// 타임라인은 발화 타이밍만 담당하고, 타격량은 플레이어 스탯(GatheringPower)에서만 나온다.
    /// </summary>
    [Serializable]
    public class InteractionEvent : MotionEventBase
    {
        public InteractionAnimEvent interactionEvent = InteractionAnimEvent.OnHit;
        public bool showHitFx = true;

        public override string GetDisplayName() => "Interaction";

        public override string GetShortLabel() => interactionEvent switch
        {
            InteractionAnimEvent.OnHit => "Interact Hit",
            InteractionAnimEvent.CatchFish => "Catch Fish",
            _ => "Interaction"
        };

        public override void Execute(GameObject target)
        {
            IInteractable interactable = GameObjectManager.Instance?.InteractionHandler?.CurrentClosestInteractable;
            if (interactable == null || interactable.IsInteracting() == false)
                return;

            interactable.OnAnimationEvent(
                interactionEvent,
                new PlayerInteractionEvent { value = CalcHitAmount() });

            if (showHitFx)
                ShowInteractionFx(interactable);
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }

        /// <summary>
        /// 타격량 = 플레이어 채집력(GatheringPower). 계산식은 PlayerActor와 공유한다.
        /// </summary>
        private static int CalcHitAmount()
            => PlayerActor.CalcGatheringHitAmount(GameObjectManager.Instance?.Player?.Stats);

        private static void ShowInteractionFx(IInteractable interactable)
        {
            GameActor actor = interactable.GetActor();
            if (actor == null)
                return;

            Vector3 position = actor.transform.position;
            var collider = actor.GetComponent<Collider>();
            if (collider != null)
                position.y += collider.bounds.extents.y * 0.5f;

            GameObjectManager.Instance?.ShowFX(FXKeyType.InteractionObjectHitFX, position);
        }
    }
}
