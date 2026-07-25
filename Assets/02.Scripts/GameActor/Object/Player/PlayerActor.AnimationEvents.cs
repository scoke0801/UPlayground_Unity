using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Ability.Core;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;
using UPlayGround.Data.Stat;
using UPlayGround.MovementController;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.Combat;
using UPlayGround.State;
using UPlayGround.UI;
using Random = UnityEngine.Random;
using UPlayGround.AI.CombatDecision;
using UPlayGround.Gameplay.Ability;

namespace UPlayGround
{
    // 애니메이션 이벤트 리시버
    public partial class PlayerActor : GameActor, IDamageable
    {
        /// <summary>
        /// 채광/벌목/채집 1회 타격량. 채집력(GatheringPower) 스탯을 단일 소스로 사용하며 최소 1을 보장한다.
        /// 애니메이션 Hit 이벤트와 MotionEvent_Interaction 타임라인이 공유하는 유일한 계산식.
        /// </summary>
        public static int CalcGatheringHitAmount(AbilitySystemComponent abilitySystem)
        {
            float power = abilitySystem != null
                          && abilitySystem.TryGetAttribute(
                              global::UPlayGround.Data.Stat.Attributes.Life.GatheringPower,
                              current: true,
                              out float value)
                ? value
                : UPlayGroundAttributeDefaults.Get(
                    global::UPlayGround.Data.Stat.Attributes.Life.GatheringPower);

            return Mathf.Max(1, Mathf.RoundToInt(power));
        }

        public void Hit()
        {
            IInteractable target = GameObjectMgr?.InteractionHandler?.CurrentClosestInteractable;
            if (target == null) return;

            int hitAmount = CalcGatheringHitAmount(AbilitySystem);
            target.OnAnimationEvent(InteractionAnimEvent.OnHit, new PlayerInteractionEvent { value = hitAmount });

            Transform interactionTransform = target.GetInteractionTransform();
            if (interactionTransform == null) return;

            Vector3 pos = interactionTransform.position;
            var col = interactionTransform.GetComponent<Collider>();
            if (col != null) pos.y += col.bounds.extents.y * 0.5f;
            GameObjectMgr.ShowFX(FXKeyType.InteractionObjectHitFX, pos);
        }

        public void CatchFish()
        {
            IInteractable target = GameObjectMgr?.InteractionHandler?.CurrentClosestInteractable;
            if (target == null) return;

            target.OnAnimationEvent(InteractionAnimEvent.CatchFish, new PlayerInteractionEvent { value = 0 });

            Transform interactionTransform = target.GetInteractionTransform();
            if (interactionTransform == null) return;
            GameObjectMgr.ShowFX(FXKeyType.InteractionObjectHitFX, interactionTransform.position);
        }
    }
}
