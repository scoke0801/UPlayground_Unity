using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_TurnInPlace", menuName = "UP/FSM/States/Turn In Place")]
    public class TurnInPlaceStateSO : StateSO
    {
        [Header("Animation")] [SerializeField] private float fadeDuration = 0.25f;

        [SerializeField] private LocomotionStateSO locomotionState;

        private CharacterBrain _cachedBrain;
        private AnimancerState _animState;


        public override void OnEnter(CharacterBrain brain)
        {
            brain.Animancer.Animator.applyRootMotion = true;

            AnimKey turnKey = GetAnimKey(brain);
            var anim = brain.AnimData.GetAnimation(turnKey);
            _animState = brain.Animancer.Play(anim, fadeDuration);

            Debug.Log($"TurnInPlaceStateSO: state:{turnKey}");
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
            bool isRight = angle > 0;

            // 속도 등급 결정 (0: Walk, 1: Run, 2: Sprint)
            int speedLevel = 0;
            if (lastSpeed >= locomotionState.runSpeed) speedLevel = 2;
            else if (lastSpeed >= locomotionState.walkSpeed) speedLevel = 1;

            return speedLevel switch
            {
                2 => GetSprintTurnKey(absAngle, isRight),
                1 => GetRunTurnKey(absAngle, isRight),
                _ => GetWalkTurnKey(absAngle, isRight)
            };
        }

        // Sprint 구간 선택 logic
        private AnimKey GetSprintTurnKey(float absAngle, bool isRight) => absAngle switch
        {
            > 90f => AnimKey.Sprint_Turn_180,
            > 45f => isRight ? AnimKey.Sprint_Turn_R90 : AnimKey.Sprint_Turn_L90,
            _ => isRight ? AnimKey.Sprint_Turn_R45 : AnimKey.Sprint_Turn_L45
        };

        // Run 구간 선택 logic
        private AnimKey GetRunTurnKey(float absAngle, bool isRight) => absAngle switch
        {
            > 90f => AnimKey.Run_Turn_180,
            > 45f => isRight ? AnimKey.Run_Turn_R90 : AnimKey.Run_Turn_L90,
            _ => isRight ? AnimKey.Run_Turn_R45 : AnimKey.Run_Turn_L45
        };

        // Walk 구간 선택 logic
        private AnimKey GetWalkTurnKey(float absAngle, bool isRight) => absAngle switch
        {
            > 135f => AnimKey.Walk_Turn_180,
            > 75f => isRight ? AnimKey.Walk_Turn_R90 : AnimKey.Walk_Turn_L90,
            _ => isRight ? AnimKey.Walk_Turn_R45 : AnimKey.Walk_Turn_L45
        };
    }
}