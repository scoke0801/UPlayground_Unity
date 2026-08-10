using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UPlayGround.Data.EnumType;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Data.Event
{
    /// <summary>
    /// Controller에 힘 작용
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceAssembly: "Assembly-CSharp")]
    [MotionEventDescriptor("AddForce", "Movement / Time", 0, "대상에게 힘/이동량을 가합니다.", "force", "move", "push", "넉백", "이동")]
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

            if (actor.HasActorType(ActorType.Player))
            {
                HandlePlayerActor(actor as PlayerActor);
            }
            else if (actor.HasActorType(ActorType.Monster))
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
            
            // 이름은 AddForce지만 실제 단위는 delta-v다. 컨트롤러의 모션 전용 정책을 통해
            // 상향 속도 제한과 Dive 상태 차단을 적용한다.
            actor.ActorController.QueueMotionVelocityChange(worldDirection * force);
        }

        private void HandlePlayerActor(PlayerActor actor)
        {
            if (actor == null)
            {
                return;
            }
            
            Vector3 normalizedDir = direction.normalized;
            Vector3 worldDirection = actor.transform.TransformDirection(normalizedDir);
            
            actor.PlayerController.QueueMotionVelocityChange(worldDirection * force);
        }

        public override void OnCompleteEvent(GameObject target)
        {
        }
    }
}
