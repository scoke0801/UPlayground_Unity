using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Fall", menuName = "UP/FSM/States/Fall")]
    public class FallStateSO : StateSO
    {
        [Header("Settings")]
        public float AirMoveSpeed = 3f;
        
        [Header("Transitions")]
        public StateSO LandingState;
        
        public override void OnEnter(CharacterBrain brain)
        {
            ITransition fallAnim = brain.AnimData.GetAnimation(AnimKey.Fall);
            if (fallAnim == null) { Debug.LogError($"[{AnimKey.Fall}] 클립이 없습니다!"); return; }

            // 낙하 애니메이션 재생
            brain.Animancer.Play(fallAnim);
        }

        public override void OnUpdate(CharacterBrain brain)
        {
            // 1. 착지 체크
            if (brain.IsGrounded())
            {
                // 땅에 닿으면 즉시 착지 상태로 전환
                brain.ChangeState(LandingState);
                return;
            }
        }
        
        public override void OnFixedUpdate(CharacterBrain brain)
        {
            // JumpStateSO의 OnFixedUpdate 로직과 동일
            if (brain.InputDirection.sqrMagnitude > 0.01f)
            {
                // ... 공중 이동 로직 (생략, 기존 JumpStateSO 참고)
            }
        }
    }
}