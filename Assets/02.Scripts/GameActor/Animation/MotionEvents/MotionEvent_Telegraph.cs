using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 몬스터 공격 범위 텔레그래프 이벤트.
    /// EnemyAttackInfo.useTelegraph가 켜진 현재 스킬의 히트 페이즈 범위를 표시한다.
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    public class TelegraphEvent : MotionEventBase
    {
        [Tooltip("AttackInfoBase.hitPhases의 인덱스. 실제 Collision 이벤트와 같은 값을 사용한다.")]
        public int hitPhaseIndex = 0;

        [Tooltip("true면 이벤트 시작 시점 위치에 고정한다. false면 공격자/워프 이동을 따라간다.")]
        public bool lockPositionOnStart = false;

        public override string GetDisplayName() => "Telegraph";

        public override string GetShortLabel() => $"Telegraph [{hitPhaseIndex}]";

        public override void Execute(GameObject target)
        {
            GameActor actor = target.GetComponent<GameActor>();
            if (actor == null || !actor.HasActorType(ActorType.Monster)) return;

            MonsterActor monsterActor = actor as MonsterActor;
            monsterActor?.Combat?.BeginTelegraph(hitPhaseIndex, lockPositionOnStart);
        }

        public override void OnCompleteEvent(GameObject target)
        {
            GameActor actor = target.GetComponent<GameActor>();
            if (actor == null || !actor.HasActorType(ActorType.Monster)) return;

            MonsterActor monsterActor = actor as MonsterActor;
            monsterActor?.Combat?.ClearTelegraphs();
        }
    }
}
