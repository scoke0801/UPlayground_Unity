using System;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 충돌 판정 활성화 이벤트.
    /// hitPhaseIndex로 현재 히트가 몇 번째 구간인지 Combat에 알린다.
    /// </summary>
    [Serializable]
    public class BeginCollisionEvent : MotionEventBase
    {
        public LayerMask targetLayers = -1;

        [Tooltip("AttackInfoBase.hitPhases의 인덱스. 멀티 히트 시 구간마다 다른 값을 설정한다.")]
        public int hitPhaseIndex = 0;

        public override string GetDisplayName() => "Collision";

        public override string GetShortLabel() => $"Collision [{hitPhaseIndex}]";

        public override void Execute(GameObject target)
        {
            GameActor actor = target.GetComponent<GameActor>();
            if (actor == null) return;

            switch (actor.ActorType)
            {
                case ActorType.Player:
                    HandlePlayerCombat(actor as PlayerActor, true);
                    break;
                case ActorType.Monster:
                    HandleMonsterCombat(actor as MonsterActor, true);
                    break;
            }
        }

        public override void OnCompleteEvent(GameObject target)
        {
            GameActor actor = target.GetComponent<GameActor>();
            if (actor == null) return;

            switch (actor.ActorType)
            {
                case ActorType.Player:
                    HandlePlayerCombat(actor as PlayerActor, false);
                    break;
                case ActorType.Monster:
                    HandleMonsterCombat(actor as MonsterActor, false);
                    break;
            }
        }

        private void HandlePlayerCombat(PlayerActor playerActor, bool isCollisionEnable)
        {
            if (playerActor == null) return;
            PlayerCombat combat = playerActor.GetCombat();
            if (combat == null) return;

            if (isCollisionEnable)
            {
                combat.ClearHitTargets();
                combat.SetHitPhaseIndex(hitPhaseIndex);
            }
            combat.SetEnableCollision(isCollisionEnable);

            //if (isCollisionEnable)
            {
                Debug.Log($"HandlePlayerCombat - {isCollisionEnable}");
            }
        }

        private void HandleMonsterCombat(MonsterActor monsterActor, bool isCollisionEnable)
        {
            if (monsterActor == null) return;
            EnemyCombat combat = monsterActor.Combat;
            if (combat == null) return;

            if (isCollisionEnable)
            {
                combat.ClearHitTargets();
                combat.SetHitPhaseIndex(hitPhaseIndex);
            }
            combat.SetEnableCollision(isCollisionEnable);
        }
    }
}