using System;
using Interaction.Enum;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 플레이어 인터렉션 모션 타임라인에서 현재 상호작용 대상에 처리 타이밍을 전달한다.
    /// 채광/벌목/채집은 OnHit, 낚시는 CatchFish를 사용한다.
    /// </summary>
    [Serializable]
    public class InteractionEvent : MotionEventBase
    {
        public InteractionAnimEvent interactionEvent = InteractionAnimEvent.OnHit;
        public int value = 1;
        public bool showHitFx = true;

        public override string GetDisplayName() => "Interaction";

        public override string GetShortLabel() => interactionEvent switch
        {
            InteractionAnimEvent.OnHit => $"Interact Hit ({value})",
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
                new PlayerInteractionEvent { value = value });

            if (showHitFx)
                ShowInteractionFx(interactable);
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }

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
