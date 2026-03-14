using System;
using System.Numerics;
using UnityEngine;
using UnityEngine.Serialization;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using Plane = System.Numerics.Plane;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// 투사체 발사 이벤트
    /// </summary>
    [Serializable]
    public class HealSkillEvent : MotionEventBase
    {
        public string vfxPrefabKey;
        public string vfxAuraPrefabKey;
        [FormerlySerializedAs("vfxPlayTime")] public float vfxLifeTime = 0f;
        
        public override string GetDisplayName() => "HealSKill";

        public override string GetShortLabel()
        {
            return "HealSKill:";
        }

        public override void Execute(GameObject target)
        {
            // [TODO]actor로 부터 대상 정보를 가져와서 힐 처리
            // 대상 위치에 힐 이펙트도 붙여주고..
            var actor = target.GetComponent<GameActor>();
            if (actor == null)
            {
                return;
            }

            if (actor.HasActorType(ActorType.Monster))
            {
                HandleMonsterHeal(actor as MonsterActor);
                return;
            }

            if (actor.HasActorType(ActorType.Player))
            {
                HandlePlayerHeal(actor as PlayerActor);
                return;
            }
        }

        private void HandlePlayerHeal(PlayerActor actor)
        {
        }

        private void HandleMonsterHeal(MonsterActor actor)
        {
            if (actor == null || actor.Combat == null)
            {
                return;
            }
            
            foreach(var skillTarget in actor.Combat.SkillTargetList)
            {
                if (skillTarget == null)
                {
                    continue;
                }

                var targetActor = skillTarget.GetTransform().GetComponent<GameActor>();
                Vector3 vfxPosition = skillTarget.GetTransform().position;

                if (targetActor.HasSocket(ActorSocketType.Center))
                {
                    vfxPosition = targetActor.GetSocket(ActorSocketType.Center).position;
                }
                
                GameObjectManager.Instance.ShowFX(vfxPrefabKey, vfxPosition, duration: vfxLifeTime);
                GameObjectManager.Instance.ShowFX(vfxAuraPrefabKey, vfxPosition, duration: vfxLifeTime);

                skillTarget.Heal(actor.Combat.CurrentSkill.baseInfo.damage);
            }
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }

}