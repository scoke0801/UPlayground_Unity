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
        public ClipTransition JumpEndAnim;   // 착지 모션
        public float JumpForce = 7f;
        public float AirMoveSpeed = 3f; 

        // 현재 점프 단계
        private enum JumpPhase { Start, Loop, End }
        private JumpPhase _currentPhase;
        
        // [추가] 착지 검사를 지연시키기 위한 변수 (버그 방지)
        private float _timeElapsed;
        private const float _landingCheckDelay = 0.05f; // 초기 0.05초 동안 착지 검사 무시 (FixedUpdate 2~3회)

        public override void OnEnter(CharacterBrain brain)
        {
            // 1. 점프 시작 애니메이션 재생 및 단계 설정
            var animState = brain.Animancer.Play(JumpStartAnim);
            _currentPhase = JumpPhase.Start; 
            _timeElapsed = 0f; // [추가] 시간 초기화
            
            // Start 애니메이션이 끝나면 Loop로 자동으로 전환되도록 이벤트 설정
            if (JumpLoopAnim != null)
            {
                if (animState.Events(brain, out AnimancerEvent.Sequence events))
                {
                    events.OnEnd = () => 
                    {
                        if (_currentPhase == JumpPhase.Start)
                        {
                            Debug.Log("Play Jump Loop (Animation End Event)");
                            brain.Animancer.Play(JumpLoopAnim, 0.2f);
                            _currentPhase = JumpPhase.Loop;
                        }
                    };
                }
            }

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
            _timeElapsed += Time.deltaTime; // [추가] 시간 누적

            // 1. 공중 체공 애니메이션 전환 (Start -> Loop)
            // Start 애니메이션의 OnEnd 이벤트와 이 로직 중 먼저 발동하는 쪽이 Loop로 전환함
            if (_currentPhase == JumpPhase.Start && !brain.IsGrounded() && brain.Rb.linearVelocity.y < -0.1f) // 하강 명확화
            {
                // 점프 후 하강하기 시작하면 Loop 애니메이션으로 전환
                if (JumpLoopAnim != null)
                {
                    Debug.Log("Play Jump Loop (Falling Check)");
                    brain.Animancer.Play(JumpLoopAnim, 0.2f);
                    _currentPhase = JumpPhase.Loop;
                }
            }

            // 2. 착지 체크 및 착지 애니메이션 재생 (Loop/Fall -> End)
            
            // [추가] 초기 몇 프레임 동안은 착지 검사를 무시
            if (_timeElapsed < _landingCheckDelay)
            {
                return;
            }

            if (brain.IsGrounded()) 
            {
                // 이전 버그 수정 로직 제거 (이제 지연 로직이 대신 안정화 역할 수행)
                // if (_currentPhase == JumpPhase.Start && brain.Rb.linearVelocity.y > 0.1f) { return; }
                
                // 착지 애니메이션이 정의되어 있고, 현재 End 상태가 아니라면 착지 시작
                if (_currentPhase != JumpPhase.End && JumpEndAnim != null)
                {
                    Debug.Log("Play Jump End");
                    // 착지 애니메이션 재생
                    var animState = brain.Animancer.Play(JumpEndAnim);
                    _currentPhase = JumpPhase.End;

                    // 애니메이션 끝나면 기본 상태(DefaultState)로 복귀
                    if (animState.Events(brain, out AnimancerEvent.Sequence events))
                    {
                        events.OnEnd = () => 
                        {
                            brain.ChangeState(brain.DefaultState);
                        };
                    }
                }
                else if (_currentPhase != JumpPhase.End && JumpEndAnim == null) 
                {
                    // JumpEndAnim이 정의되지 않은 경우, 즉시 기본 상태로 복귀
                    brain.ChangeState(brain.DefaultState);
                }
            }
        }
    }
}