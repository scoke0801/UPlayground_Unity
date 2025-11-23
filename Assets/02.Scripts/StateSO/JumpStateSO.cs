using UnityEngine;
using Animancer;

namespace Game.FSM
{
    [CreateAssetMenu(fileName = "State_Jump", menuName = "FSM/States/Jump")]
    public class JumpStateSO : StateSO
    {
        [Header("Settings")]
        public string JumpStartKey = "JumpStart";
        
        public float JumpForce = 7f;
        public float AirMoveSpeed = 3f; 
        
        [SerializeField] private float fadeDuration = 0.1f;
        
        [Header("Transitions")]
        public StateSO FallState; // 상승 완료 후 전환할 상태
        
        public override void OnEnter(CharacterBrain brain)
        {
            ClipTransition jumpStartAnim = brain.AnimData.GetClipTransition(JumpStartKey);
            
            if (jumpStartAnim.Clip == null) { Debug.LogError($"[{JumpStartKey}] 클립이 없습니다!"); return; }
            
            // 1. 애니메이션 재생
            var animState = brain.Animancer.Play(jumpStartAnim, fadeDuration);
            
            // 2. 물리 힘 적용
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
            // 1. 상승 종료 체크
            if (false == brain.IsGrounded() && brain.Rb.linearVelocity.y <= 0 && FallState != null)
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