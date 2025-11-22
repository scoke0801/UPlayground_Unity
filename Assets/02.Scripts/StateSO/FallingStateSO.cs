// FallStateSO.cs (신규)
using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Fall", menuName = "FSM/States/Fall")]
    public class FallStateSO : StateSO
    {
        [Header("Settings")]
        
        public string FallLoopAnimKey = "Fall";
        public float AirMoveSpeed = 3f;
        
        [Header("Transitions")]
        public StateSO LandingState; // 착지할 목표 상태 (필수)

        public override void OnEnter(CharacterBrain brain)
        {
            
            ClipTransition fallAnim = brain.AnimData.GetClipTransition(FallLoopAnimKey);
            if (fallAnim.Clip == null) { Debug.LogError($"[{FallLoopAnimKey}] 클립이 없습니다!"); return; }

            // 낙하 애니메이션 재생
            brain.Animancer.Play(fallAnim);
        }

        public override void OnUpdate(CharacterBrain brain)
        {
            // 1. 착지 체크 (가장 높은 우선순위)
            if (brain.IsGrounded())
            {
                // 땅에 닿으면 즉시 착지 상태로 전환
                brain.ChangeState(LandingState);
                return;
            }
            
            // 2. 공중 회전/이동 로직은 OnFixedUpdate에서 처리 (옵션)
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