using System;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// Controller에 힘 작용
    /// </summary>
    [Serializable]
    public class AddForceEvent : MotionEventBase
    {
        public Vector3 direction;   // normalized로 사용
        public float force;
        
        public override string GetDisplayName() => "AddForce";

        public override string GetShortLabel() => $"AddForce";

        public override void Execute(GameObject target)
        {
            GameActor actor = target.GetComponent<GameActor>();
            if(actor == null)
            {
                return;
            }

            if (actor.ActorType == ActorType.Player)
            {
                HandlePlayerActor(actor as PlayerActor);
            }
            else if (actor.ActorType == ActorType.Monster)
            {
                HandleMonsterActor(actor as MonsterActor);
            }
        }

        private void HandleMonsterActor(MonsterActor actor)
        {
            if (actor == null)
            {
                return;
            }
            
            Vector3 normalizedDir = direction.normalized;
            
            // 캐릭터의 현재 회전값을 기준으로 로컬 공간의 벡터를 월드 공간의 벡터로 변환
            Vector3 worldDirection = actor.transform.TransformDirection(normalizedDir);
            
            // 변환된 월드 방향에 힘(force)을 곱하여 적용
            actor.ActorController.AddVelocity(worldDirection * force);
        }

        private void HandlePlayerActor(PlayerActor actor)
        {
            if (actor == null)
            {
                return;
            }
            
            Vector3 normalizedDir = direction.normalized;
            Vector3 worldDirection = actor.transform.TransformDirection(normalizedDir);
            
            actor.PlayerController.AddVelocity(worldDirection * force);
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}