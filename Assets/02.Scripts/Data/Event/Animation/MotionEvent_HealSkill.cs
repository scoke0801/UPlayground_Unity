using System;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Data.Event
{
    [Serializable]
    public class HealSkillEvent : MotionEventBase
    {
        public string vfxPrefabKey;
        public string vfxAuraPrefabKey;
        [FormerlySerializedAs("vfxPlayTime")] public float vfxLifeTime = 0f;
        
        public override string GetDisplayName() => "HealSkill";
        public override string GetShortLabel()  => "HealSkill:";

        public override void Execute(GameObject target)
        {
            var actor = target.GetComponent<GameActor>();
            if (actor == null) return;

            if (actor.HasActorType(ActorType.Monster))
            {
                HandleMonsterHeal(actor as MonsterActor);
                return;
            }

            if (actor.HasActorType(ActorType.Player))
            {
                HandlePlayerHeal(actor as PlayerActor);
            }
        }

        private void HandlePlayerHeal(PlayerActor actor)
        {
        }

        private void HandleMonsterHeal(MonsterActor actor)
        {
            if (actor == null || actor.Combat == null) return;

            foreach (var skillTarget in actor.Combat.SkillTargetList)
            {
                if (skillTarget == null) continue;

                // VFX 위치 계산 — GameActor가 null이어도 터지지 않도록 방어
                Transform targetTransform = skillTarget.GetTransform();
                Vector3 vfxPosition = targetTransform.position;

                var targetActor = targetTransform.GetComponent<GameActor>();
                if (targetActor != null && targetActor.HasSocket(ActorSocketType.Center))
                    vfxPosition = targetActor.GetSocket(ActorSocketType.Center).position;

                GameObjectManager.Instance.ShowFX(vfxPrefabKey,     vfxPosition, duration: vfxLifeTime);
                GameObjectManager.Instance.ShowFX(vfxAuraPrefabKey, vfxPosition, duration: vfxLifeTime);

                // 힐 실행 — MonsterActor.Heal() 내부에서 플로터 출력
                skillTarget.Heal(actor.Combat.CurrentSkill.baseInfo.damage);
            }
        }

        public override void OnCompleteEvent(GameObject target) { }
    }
}
