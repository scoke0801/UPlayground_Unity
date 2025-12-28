using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_TurnInPlace", menuName = "UP/FSM/States/Turn In Place")]
    public class TurnInPlaceStateSO : StateSO
    {
        [SerializeField] private LocomotionStateSO locomotionState;
        
        public override void OnEnter(CharacterBrain brain)
        {
            AnimKey turnKey = GetAnimKey(brain);

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

        private AnimKey GetAnimKey(CharacterBrain brain)
        {
            float lastSpeed = brain.GetData<float>("LastSpeed");
            float angle = brain.GetData<float>("TurnAngle");
            float absAngle = Mathf.Abs(angle);
            
            if (lastSpeed > locomotionState.runSpeed)
            {
                // sprint
                if (absAngle > 90)
                    return (angle > 0) ? AnimKey.Sprint_Turn_R180 : AnimKey.Sprint_Turn_L180;
                else if(absAngle > 45)
                    return (angle > 0) ? AnimKey.Sprint_Turn_R90 : AnimKey.Sprint_Turn_L90;
                return (angle > 0) ? AnimKey.Sprint_Turn_R45 : AnimKey.Sprint_Turn_L45;
            }
            else if (lastSpeed > locomotionState.walkSpeed)
            {
                if (absAngle > 90)
                    return (angle > 0) ? AnimKey.Run_Turn_R180 : AnimKey.Run_Turn_L180;
                else if(absAngle > 45)
                    return (angle > 0) ? AnimKey.Run_Turn_R90 : AnimKey.Run_Turn_L90;
                return (angle > 0) ? AnimKey.Run_Turn_R45 : AnimKey.Run_Turn_L45;
            }
            
            if (absAngle > 90)
                return (angle > 0) ? AnimKey.Walk_Turn_R180 : AnimKey.Walk_Turn_L180;
            else if(absAngle > 45)
                return (angle > 0) ? AnimKey.Walk_Turn_R90 : AnimKey.Walk_Turn_L90;
            return (angle > 0) ? AnimKey.Walk_Turn_R45 : AnimKey.Walk_Turn_L45;
        }
    }
}