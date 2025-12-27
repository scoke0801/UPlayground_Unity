using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_TurnInPlace", menuName = "UP/FSM/States/Turn In Place")]
    public class TurnInPlaceStateSO : StateSO
    {
        public override void OnEnter(CharacterBrain brain)
        {
            float angle = brain.GetData<float>("TurnAngle");
            AnimKey turnKey;

            // 각도와 방향에 따른 애니메이션 매칭
            if (Mathf.Abs(angle) > 135f)
                turnKey = (angle > 0) ? AnimKey.Move_Turn_R180 : AnimKey.Move_Turn_L180;
            else
                turnKey = (angle > 0) ? AnimKey.Move_Turn_R90 : AnimKey.Move_Turn_L90;

            var anim = brain.AnimData.GetAnimation(turnKey);
            var state = brain.Animancer.Play(anim, 0.1f);
            
            // 애니메이션 도중 캐릭터가 입력 방향을 향하도록 부드럽게 보정
            if (brain.InputDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(brain.InputDirection);
                brain.Rb.MoveRotation(Quaternion.Slerp(brain.transform.rotation, targetRot, Time.fixedDeltaTime * 10f));
            }
            
            if (state.Events(brain, out AnimancerEvent.Sequence events))
            {
                brain.ChangeState(brain.DefaultState);
                events.OnEnd = () => brain.ChangeState(brain.DefaultState);
            }
        }

        public override void OnFixedUpdate(CharacterBrain brain)
        {
            // 애니메이션 도중 캐릭터가 입력 방향을 향하도록 부드럽게 보정
            if (brain.InputDirection.sqrMagnitude > 0.01f)
            {
                //Quaternion targetRot = Quaternion.LookRotation(brain.InputDirection);
                //brain.Rb.MoveRotation(Quaternion.Slerp(brain.transform.rotation, targetRot, Time.fixedDeltaTime * 10f));
            }
        }
    }
}