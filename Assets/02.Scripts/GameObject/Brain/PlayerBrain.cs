using System;
using UnityEngine;
using Game.Input;
using Game.Skills;
using Game.Data;
using UnityEngine.Serialization;

namespace Game.FSM
{    
    /// <summary>
    /// 플레이어 입력 처리 및 캐릭터 제어
    /// </summary>
    public class PlayerBrain : CharacterBrain
    {
        [FormerlySerializedAs("inputReader")]
        [Header("Input")]
        [SerializeField] private PlayerInputReader playerInputReader;

        [Header("Input Buffer")]
        [SerializeField] private float bufferTime = 0.15f;
        [SerializeField] private int maxBufferSize = 10;
        
        // 참조 추가
        private SkillSystem _skillSystem;

        private InputBuffer _inputBuffer;
        private Camera _cachedCamera;

        // state
        public bool IsOnInteraction { get; private set; }

        protected void OnDisable()
        {
        }

        protected override void Awake()
        {
            base.Awake();

            _cachedCamera = Camera.main;
            _inputBuffer = new InputBuffer(bufferTime, maxBufferSize);
            
            // SkillSystem 컴포넌트 가져오기
            _skillSystem = GetComponent<SkillSystem>();
            if (_skillSystem == null) Debug.LogWarning("[PlayerBrain] SkillSystem이 없습니다.");

            // InputReader 초기화 로직 (기존 유지)
            if (playerInputReader == null)
            {
                playerInputReader = GetComponent<PlayerInputReader>();
                if (playerInputReader == null)
                {
                    playerInputReader = gameObject.AddComponent<PlayerInputReader>();
                }
            }

            SubscribeToInputEvents();
            SubscribeToEvent();
        }

        private void SubscribeToEvent()
        {
            GameObjectManager.Instance.OnInteractionOn += OnInterfaction;
            GameObjectManager.Instance.OnInteractionOut += OnInteractionOut;
        }

        private void SubscribeToInputEvents()
        {
            if (playerInputReader == null) return;

            // 기본 이동/액션
            playerInputReader.OnJumpEvent += () => _inputBuffer.AddInput("Jump");
            playerInputReader.OnRollEvent += () => _inputBuffer.AddInput("Roll");
            playerInputReader.OnAttackEvent += () => _inputBuffer.AddInput("Attack");
            
            // 스킬 입력 구독
            playerInputReader.OnSkillPressed += (index) => _inputBuffer.AddInput($"Skill{index}");
            
            // 차징 스킬용
            playerInputReader.OnSkillReleased += (index) => _inputBuffer.AddInput($"Skill{index}Released");
            
            playerInputReader.OnInteractEvent += () => _inputBuffer.AddInput("Interact");

            playerInputReader.OnSprintPerformEvent += OnSprintPerformed;
            playerInputReader.OnSprintCancelEvent += OnSprintCanceled;
        }
        
        protected override void HandleInput()
        {
            if (playerInputReader == null) return;
            ProcessMovementInput();
            ProcessBufferedInputs();
        }
        
        /// <summary>
        /// 이동 입력 처리 (카메라 기준)
        /// </summary>
        private void ProcessMovementInput()
        {
            Vector2 moveInput = playerInputReader.MoveInput;
            if (_cachedCamera == null)
            {
                _cachedCamera = Camera.main;
                if (_cachedCamera == null)
                {
                    SetInputDirection(Vector3.zero);
                    return;
                }
            }

            // 카메라 기준 방향 보정
            Transform cameraTransform = _cachedCamera.transform;
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            // Y축 성분 제거 (평면 이동만)
            forward.y = 0;
            right.y = 0;

            forward.Normalize();
            right.Normalize();

            // 카메라 기준 이동 방향 계산
            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            SetInputDirection(moveDirection);
            if (_cachedCamera == null) _cachedCamera = Camera.main;
            // ... 방향 계산 로직 ...
            // SetInputDirection(moveDirection);
        }

        /// <summary>
        /// 버퍼된 입력 처리
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

            if (_inputBuffer.HasInput("Sprint"))
            {
                SetSprintInput(true);
            }
            
            // 스킬 입력 처리
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

            if (_inputBuffer.HasInput("Interact"))
            {
                SetInteractionInput(true);
                _inputBuffer.ConsumeInput("Interact");
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
            if (_skillSystem.TryUseSkill(slotIndex, out SkillJsonData jsonData))
            {
                // Json 데이터에서 ExecutionState 경로 가져오기
                if (jsonData != null && !string.IsNullOrEmpty(jsonData.executionStatePath))
                {
                    // Resources에서 StateSO 로드
                    StateSO executionState = Resources.Load<StateSO>(jsonData.executionStatePath);
                    
                    if (executionState != null)
                    {
                        // 블랙보드에 스킬 인덱스 저장 (SkillActionStateSO에서 사용)
                        SetData("CurrentSkillIndex", slotIndex);
                        SetData("ChargeRatio", 0f); // Instant 스킬은 차징 없음
                        
                        ChangeState(executionState);
                        _inputBuffer.ConsumeInput(inputKey);
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerBrain] ExecutionState를 로드할 수 없습니다: {jsonData.executionStatePath}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[PlayerBrain] 슬롯 {slotIndex} 스킬에 State가 할당되지 않았습니다.");
                }
            }
            else
            {
                // 실패 (쿨타임 등): 입력 소비
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
        
        private void OnInterfaction()
        {
            IsOnInteraction = true;
            // 별도 상태로 변경해도 괜찮을 것 같다.
        }

        private void OnInteractionOut()
        {
            IsOnInteraction = false;
        }
        
        private void OnSprintPerformed()
        {
            SetSprintInput(true);
        }

        private void OnSprintCanceled()
        {
            SetSprintInput(false);
        }
    }
}
