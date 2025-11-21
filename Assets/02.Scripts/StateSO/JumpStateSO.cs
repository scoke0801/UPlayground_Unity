using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Jump", menuName = "FSM/States/Jump")]
    public class JumpStateSO : StateSO
    {
        [Header("Settings")]
        public ClipTransition JumpStartAnim; // 점프 시작 모션
        public ClipTransition JumpLoopAnim;  // 공중 체공 모션 (없으면 Start만 사용)
        public float JumpForce = 7f;
        public float AirMoveSpeed = 3f; // 공중 이동 속도 (지상보다 느리게)

        public override void OnEnter(CharacterBrain brain)
        {
            // 1. 애니메이션 재생
            brain.Animancer.Play(JumpStartAnim);

            // 2. 물리 힘 적용 (기존 Y 속도 초기화 후 적용)
            Vector3 velocity = brain.Rb.linearVelocity;
            velocity.y = 0; 
            brain.Rb.linearVelocity = velocity;
            
            brain.Rb.AddForce(Vector3.up * JumpForce, ForceMode.Impulse);
        }

        public override void OnFixedUpdate(CharacterBrain brain)
        {
            // 3. 공중 이동 제어 (약간의 이동 허용)
            if (brain.InputDirection.sqrMagnitude > 0.01f)
            {
                Vector3 airMove = brain.InputDirection * AirMoveSpeed;
                // Y축 속도는 유지하고 X, Z만 변경
                brain.Rb.linearVelocity = new Vector3(airMove.x, brain.Rb.linearVelocity.y, airMove.z);

                // 공중 회전
                Quaternion targetRot = Quaternion.LookRotation(brain.InputDirection);
                brain.transform.rotation = Quaternion.Slerp(brain.transform.rotation, targetRot, Time.fixedDeltaTime * 5f);
            }
        }

        public override void OnUpdate(CharacterBrain brain)
        {
            // 4. 착지 체크 (점프 직후 바로 체크되는 것 방지 위해 y속도 체크)
            if (brain.Rb.linearVelocity.y < 0.1f && brain.IsGrounded())
            {
                // 땅에 닿으면 기본 상태로 복귀
                brain.ChangeState(brain.DefaultState);
            }
        }
    }
}