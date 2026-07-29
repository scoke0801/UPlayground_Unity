using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Data.Sound;

namespace UPlayGround.Data.Event
{
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor("HealSkill", "Combat", 0, "회복 스킬 판정을 실행합니다.", "heal", "recovery", "힐", "회복")]
    public class HealSkillEvent : MotionEventBase
    {
        public string vfxPrefabKey;
        public string vfxAuraPrefabKey;
        public string soundKey = GameSoundKey.Heal;
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

                ActorSvc.Objects?.ShowFX(vfxPrefabKey,     vfxPosition, duration: vfxLifeTime);
                ActorSvc.Objects?.ShowFX(vfxAuraPrefabKey, vfxPosition, duration: vfxLifeTime);

                float healthBefore = skillTarget.GetCurrentHealth();

            // 힐 실행 — 대상의 GameplayEffectSpec 경로에서 Attribute와 플로터를 갱신한다.
            skillTarget.ApplyHealingEffect(actor.Combat.CurrentSkill.baseInfo.damage);

                if (skillTarget.GetCurrentHealth() > healthBefore && !string.IsNullOrWhiteSpace(soundKey))
                    Svc.Sound?.PlaySfx(soundKey, vfxPosition);
            }
        }

        public override void OnCompleteEvent(GameObject target) { }
    }
}
