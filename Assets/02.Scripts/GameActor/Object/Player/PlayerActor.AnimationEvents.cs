using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.InputSystem;
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
using UPlayGround.Manager.Handler;
using UPlayGround.Manager.Combat;
using UPlayGround.Combat;
using UPlayGround.State;
using UPlayGround.UI;
using Random = UnityEngine.Random;
using UPlayGround.AI.CombatDecision;

namespace UPlayGround
{
    // 애니메이션 이벤트 리시버
    public partial class PlayerActor : GameActor, IDamageable
    {
        /// <summary>
        /// 채광/벌목/채집 1회 타격량. 채집력(GatheringPower) 스탯을 단일 소스로 사용하며 최소 1을 보장한다.
        /// 레거시 애니메이션 이벤트(Hit)와 MotionEvent_Interaction 타임라인이 공유하는 유일한 계산식.
        /// </summary>
        public static int CalcGatheringHitAmount(ActorStatContainer stats)
        {
            float power = stats != null
                ? stats.GetFinalStat(StatType.GatheringPower)
                : ActorStatSO.GetDefault(StatType.GatheringPower);

            return Mathf.Max(1, Mathf.RoundToInt(power));
        }

        public void Hit()
        {
            IInteractable target = GameObjectMgr?.InteractionHandler?.CurrentClosestInteractable;
            if (target == null) return;

            int hitAmount = CalcGatheringHitAmount(Stats);
            target.OnAnimationEvent(InteractionAnimEvent.OnHit, new PlayerInteractionEvent { value = hitAmount });

            GameActor actor = target.GetActor();
            if (actor == null) return;

            Vector3 pos = actor.transform.position;
            var col = actor.GetComponent<Collider>();
            if (col != null) pos.y += col.bounds.extents.y * 0.5f;
            GameObjectMgr.ShowFX(FXKeyType.InteractionObjectHitFX, pos);
        }

        public void CatchFish()
        {
            IInteractable target = GameObjectMgr?.InteractionHandler?.CurrentClosestInteractable;
            if (target == null) return;

            target.OnAnimationEvent(InteractionAnimEvent.CatchFish, new PlayerInteractionEvent { value = 0 });

            GameActor actor = target.GetActor();
            if (actor == null) return;
            GameObjectMgr.ShowFX(FXKeyType.InteractionObjectHitFX, actor.transform.position);
        }
    }
}
