using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Jump", menuName = "UP/FSM/States/Jump")]
    public class JumpStateSO : StateSO
    {
        [SerializeField] private float fadeDuration = 0.1f;
        
        [Header("Transitions")]
        public StateSO FallState; // 상승 완료 후 전환할 상태
        
        public override void OnEnter(CharacterBrain brain)
        {
            ITransition jumpStartAnim = brain.AnimData.GetAnimation(AnimKey.Jump);
            
            if (jumpStartAnim == null) { Debug.LogError($"[{AnimKey.Jump}] 클립이 없습니다!"); return; }
            
            // 1. 애니메이션 재생
            var animState = brain.Animancer.Play(jumpStartAnim, fadeDuration);
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
            // OnExit에서는 아무것도 하지 않음 (필요 시 Reset/Cleanup 로직 추가)
        }
    }
}