using UnityEngine;
using Game.Input;
using Game.Skills; // SkillSystem 네임스페이스 추가

namespace Game.FSM
{
    public class PlayerBrain : CharacterBrain
    {
        [Header("Input")]
        [SerializeField] private InputReader inputReader;

        [Header("Input Buffer")]
        [SerializeField] private float bufferTime = 0.15f;
        [SerializeField] private int maxBufferSize = 10;
        
        // 참조 추가
        private SkillSystem _skillSystem;

        private InputBuffer _inputBuffer;
        private Camera _cachedCamera;

        protected override void Awake()
        {
            base.Awake();

            _cachedCamera = Camera.main;
            _inputBuffer = new InputBuffer(bufferTime, maxBufferSize);
            
            // SkillSystem 컴포넌트 가져오기
            _skillSystem = GetComponent<SkillSystem>();
            if (_skillSystem == null) Debug.LogWarning("[PlayerBrain] SkillSystem이 없습니다.");

            // InputReader 초기화 로직 (기존 유지)
            if (inputReader == null)
            {
                inputReader = GetComponent<InputReader>();
                if (inputReader == null)
                {
                    inputReader = gameObject.AddComponent<InputReader>();
                }
            }

            SubscribeToInputEvents();
        }

        private void SubscribeToInputEvents()
        {
            if (inputReader == null) return;

            // 기본 이동/액션
            inputReader.OnJumpEvent += () => _inputBuffer.AddInput("Jump");
            inputReader.OnRollEvent += () => _inputBuffer.AddInput("Roll");
            inputReader.OnAttackEvent += () => _inputBuffer.AddInput("Attack");
            
            // 스킬 입력 구독
            inputReader.OnSkillPressed += (index) => _inputBuffer.AddInput($"Skill{index}");
            
            // 차징 스킬용
            inputReader.OnSkillReleased += (index) => _inputBuffer.AddInput($"Skill{index}Released");
        }

        protected override void HandleInput()
        {
            if (inputReader == null) return;
            ProcessMovementInput();
            ProcessBufferedInputs();
        }

        private void ProcessMovementInput()
        {
            // (기존 코드와 동일하여 생략)
            // ...
            Vector2 moveInput = inputReader.MoveInput;
            if (_cachedCamera == null) _cachedCamera = Camera.main;
            // ... 방향 계산 로직 ...
            // SetInputDirection(moveDirection);
        }

        /// <summary>
        /// 버퍼된 입력 처리 (핵심 수정 파트)
        /// </summary>
        private void ProcessBufferedInputs()
        {
            // 1. 회피 (최우선 순위 예시)
            if (_inputBuffer.HasInput("Roll"))
            {
                // 조건 체크 필요 시 추가 (예: 스테미너)
                SetDodgeInput(true); // DodgeState로의 전환은 StateTransition 조건으로 처리됨
                _inputBuffer.ConsumeInput("Roll");
                return; // 회피가 발동되면 다른 행동 무시
            }
            
            // 2. 스킬 입력 처리
            for (int i = 1; i <= 4; i++)
            {
                string key = $"Skill{i}";
                if (_inputBuffer.HasInput(key))
                {
                    if (TryProcessSkillInput(key, i))
                    {
                        return; // 스킬 사용 성공 시 즉시 리턴 (우선순위 처리)
                    }
                }
            }
            // 스킬 입력 처리 - Release
            for (int i = 1; i <= 4; i++)
            {
                string releaseKey = $"Skill{i}Released";
                if (_inputBuffer.HasInput(releaseKey))
                {
                    if (TryProcessSkillRelease(releaseKey, i))
                    {
                        // 해제 로직이 상태를 전환했다면 리턴
                        return;
                    }
                }
            }

            // 3. 점프
            if (_inputBuffer.HasInput("Jump"))
            {
                if (IsGrounded()) 
                {
                    SetJumpInput(true);
                    _inputBuffer.ConsumeInput("Jump");
                }
            }

            // 4. 기본 공격
            if (_inputBuffer.HasInput("Attack"))
            {
                SetAttackInput(AttackInputType.Light);
                _inputBuffer.ConsumeInput("Attack");
            }
        }
        
        /// <summary>
        /// 스킬 입력 처리 헬퍼 메서드
        /// </summary>
        private bool TryProcessSkillInput(string inputKey, int slotIndex)
        {
            if (!_inputBuffer.HasInput(inputKey)) return false;
            if (_skillSystem == null) return false;

            // SkillSystem에게 사용 가능 여부 확인 및 데이터 요청
            if (_skillSystem.TryUseSkill(slotIndex, out SkillData data))
            {
                // 성공 시: 해당 스킬의 State로 즉시 전환
                if (data != null && data.ExecutionState != null)
                {
                    ChangeState(data.ExecutionState);
                    _inputBuffer.ConsumeInput(inputKey);
                    return true;
                }
                else
                {
                    Debug.LogWarning($"슬롯 {slotIndex} 스킬에 State가 할당되지 않았습니다.");
                }
            }
            else
            {
                // 실패 (쿨타임 등): 입력을 소비할지 말지는 기획 의도에 따름
                // 여기서는 연타 방지를 위해 입력을 소비해버림
                _inputBuffer.ConsumeInput(inputKey);
            }
            return false;
        }
        
        /// <summary>
        /// 스킬 Released 입력 처리 헬퍼 메서드 (상태 내 로직)
        /// 이 함수는 현재 FSM State에게 해제 신호를 전달합니다.
        /// </summary>
        private bool TryProcessSkillRelease(string inputKey, int slotIndex)
        {
            // 1. 버퍼 소비
            _inputBuffer.ConsumeInput(inputKey);
            
            // 2. 현재 상태에 해제 신호 전달
            // 차징 또는 채널링 상태에서 이 블랙보드 데이터를 체크하고, 
            // 데이터가 True일 경우 스킬을 발동시키거나 종료합니다.
            SetData($"Skill{slotIndex}Released", true); 
            
            // 이 시점에서는 상태 전환이 일어나지 않으므로 false를 반환합니다.
            // 상태 전환은 CurrentState의 OnUpdate(예: SkillChargeStateSO)에서 일어납니다.
            return false; 
        }
    }
}