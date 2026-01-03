using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Jump", menuName = "UP/FSM/States/Jump")]
    public class JumpStateSO : StateSO, IMovementState
    {
        [SerializeField] private float fadeDuration = 0.1f;
        [SerializeField] private float JumpUpSpeed = 10f;
        
        [Header("Transitions")]
        public StateSO FallState; // 상승 완료 후 전환할 상태

        private bool _jumpApplied = false;
        
        public override void OnEnter(CharacterBrain brain)
        {
            Debug.Log("JumpStateSO.OnEnter");
            ITransition jumpStartAnim = brain.AnimData.GetAnimation(AnimKey.Jump);
            
            if (jumpStartAnim == null) { Debug.LogError($"[{AnimKey.Jump}] 클립이 없습니다!"); return; }
            
            // 1. 애니메이션 재생
            var animState = brain.Animancer.Play(jumpStartAnim, fadeDuration);
            brain.SetData("JumpApplied", false);

            _jumpApplied = false;
        }
        
        public override void OnFixedUpdate(CharacterBrain brain)
        {
        }

        public override void OnUpdate(CharacterBrain brain)
        {
            // 1. 상승 종료 체크
            if (false == brain.IsGrounded() && brain.Motor.Velocity.y <= 0 && FallState != null)
            {
                // 상승이 끝나고 하강하기 시작하면, FallState로 즉시 전환
                brain.ChangeState(FallState);
                return;
            }
        }
        
        public override void OnExit(CharacterBrain brain)
        {
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime, CharacterBrain brain)
        {
            if (_jumpApplied == false)
            {
                brain.Motor.ForceUnground();
                currentVelocity += brain.Motor.CharacterUp * JumpUpSpeed - Vector3.Project(currentVelocity, brain.Motor.CharacterUp);
                _jumpApplied = true;
            }
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime, CharacterBrain brain)
        {
            // 점프 중에는 특별한 회전 로직이 필요하지 않으므로 비워둡니다.
        }
    }
}