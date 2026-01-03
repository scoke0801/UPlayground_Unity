using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Fall", menuName = "UP/FSM/States/Fall")]
    public class FallStateSO : StateSO
    {
        [Header("Transitions")]
        public StateSO LandingState;
        
        public override void OnEnter(CharacterBrain brain)
        {
            Debug.Log($"FallStateSO.OnEnter {brain.IsGrounded()}, " +
                      $"velocity: {brain.Motor.Velocity}");
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

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime, CharacterBrain brain)
        {
            // 공중에서는 특별한 회전 로직이 필요하지 않으므로 비워둡니다.
            // 필요하다면 여기에 공중 회전 로직을 추가할 수 있습니다.
        }
    }
}