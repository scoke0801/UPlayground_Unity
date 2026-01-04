using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Move_Stop", menuName = "UP/FSM/States/Move Stop")]
    public class StopStateSO : StateSO,  IMovementState
    {
        [SerializeField] private float fadeDuration = 0.1f;
        [SerializeField] private LocomotionStateSO locomotionState;

        private Vector3 _enteredVelocity;
        private AnimancerState _animancerState;
        
        public override void OnEnter(CharacterBrain brain)
        {
            float lastSpeed = brain.GetData<float>("LastSpeed");
            AnimKey stopKey;
            
            _enteredVelocity = brain.Motor.Velocity;

            // 속도 구간별 적절한 Stop 애니메이션 선택
            if (lastSpeed > locomotionState.runSpeed) stopKey = AnimKey.Move_Stop_Sprinting;
            else if (lastSpeed > locomotionState.walkSpeed) stopKey = AnimKey.Move_Stop_Running;
            else stopKey = AnimKey.Move_Stop_Walking;

            var anim = brain.AnimData.GetAnimation(stopKey);
            _animancerState = brain.Animancer.Play(anim, fadeDuration);
            
            Debug.Log($"StopStateSO: state:{stopKey}, anim: {anim}");
            // 애니메이션이 끝나면 다시 Locomotion(Idle)으로 복귀
            if (_animancerState.Events(brain, out AnimancerEvent.Sequence events))
            {
                //events.Add(0.85f, () => brain.ChangeState(brain.DefaultState));
                events.OnEnd = () => brain.ChangeState(brain.DefaultState);
            }
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime, CharacterBrain brain)
        {
            if (_animancerState != null)
            {
                // state.NormalizedTime은 0(시작)에서 1(끝)까지 증가함
                // 커브를 조절하고 싶다면 AnimationCurve를 serialize하여 사용할 수도 있음
                float progress = Mathf.Clamp01(_animancerState.NormalizedTime);
                
                // 시작 속도에서 0으로 점진적 보간
                currentVelocity = Vector3.Lerp(_enteredVelocity, Vector3.zero, progress);
            }
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime, CharacterBrain brain)
        {
            locomotionState.UpdateRotation(ref currentRotation, deltaTime, brain);
        }
    }
}