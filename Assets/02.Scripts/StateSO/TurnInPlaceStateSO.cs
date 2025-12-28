using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_TurnInPlace", menuName = "UP/FSM/States/Turn In Place")]
    public class TurnInPlaceStateSO : StateSO
    {
        [Header("Animation")]
        [SerializeField] private float fadeDuration = 0.25f;
        
        [SerializeField] private LocomotionStateSO locomotionState;

        private CharacterBrain _cachedBrain;
        private AnimancerState _animState;
        

        public override void OnEnter(CharacterBrain brain)
        {
            brain.Animancer.Animator.applyRootMotion = true;
            
            AnimKey turnKey = GetAnimKey(brain);
            var anim = brain.AnimData.GetAnimation(turnKey);
            _animState = brain.Animancer.Play(anim, fadeDuration);
            
            _cachedBrain = brain;
            
            if (_animState.Events(brain, out AnimancerEvent.Sequence events))
            {
                events.OnEnd = OnTransitionToLocomotion;
            }
        }
        
        public override void OnExit(CharacterBrain brain)
        {
            base.OnExit(brain);
            
            brain.Animancer.Animator.applyRootMotion = false;
        }

        private void OnTransitionToLocomotion()
        {
            _cachedBrain.Animancer.Animator.applyRootMotion = false;
            // 회전 없이 바로 상태 전환
            _cachedBrain.ChangeState(_cachedBrain.DefaultState);
        }

        private AnimKey GetAnimKey(CharacterBrain brain)
        {
            float lastSpeed = brain.GetData<float>("LastSpeed");
            float angle = brain.GetData<float>("TurnAngle");
            float absAngle = Mathf.Abs(angle);
            
            if (lastSpeed > locomotionState.runSpeed)
            {
                if (absAngle > 90)
                    return AnimKey.Sprint_Turn_180;
                else if(absAngle > 45)
                    return (angle > 0) ? AnimKey.Sprint_Turn_R90 : AnimKey.Sprint_Turn_L90;
                return (angle > 0) ? AnimKey.Sprint_Turn_R45 : AnimKey.Sprint_Turn_L45;
            }
            else if (lastSpeed > locomotionState.walkSpeed)
            {
                if (absAngle > 90)
                    return AnimKey.Run_Turn_180;
                else if(absAngle > 45)
                    return (angle > 0) ? AnimKey.Run_Turn_R90 : AnimKey.Run_Turn_L90;
                return (angle > 0) ? AnimKey.Run_Turn_R45 : AnimKey.Run_Turn_L45;
            }
            
            if (absAngle > 90)
                return AnimKey.Walk_Turn_180;
            else if(absAngle > 45)
                return (angle > 0) ? AnimKey.Walk_Turn_R90 : AnimKey.Walk_Turn_L90;
            return (angle > 0) ? AnimKey.Walk_Turn_R45 : AnimKey.Walk_Turn_L45;
        }
    }
}