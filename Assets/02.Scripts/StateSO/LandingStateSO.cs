using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Landing", menuName = "UP/FSM/States/Landing")]
    public class LandingStateSO : StateSO,  IMovementState
    {
        private Vector3 _enteredVelocity;
        private AnimancerState _animancerState;
        public override void OnEnter(CharacterBrain brain)
        {          
            Debug.Log("LandingStateSO.OnEnter");
            ITransition landAnim = brain.AnimData.GetAnimation(AnimKey.Land);
            if (landAnim == null) { Debug.LogError($"[{AnimKey.Land}] 클립이 없습니다!"); return; }
            
            _enteredVelocity = brain.Motor.Velocity;
            // 착지 애니메이션 재생
            _animancerState = brain.Animancer.Play(landAnim);
            _animancerState.OwnedEvents.OnEnd = () => brain.ChangeState(brain.DefaultState);
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
            // 회전 불가능
        }
    }
}