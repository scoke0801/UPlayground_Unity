using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround.State;
using UPlayGround.Input;

namespace UPlayGround.MovementController
{
    public struct PlayerCharacterInputs
    {
        // 이동 & 회전
        public Vector2 MoveInput;
        public Quaternion CameraRotation;
        
        // 이동 상태 변경
        public InputCondition CrouchInput;
        
        // 일회성 상태 변경 - 이동
        public InputCondition DodgeInput;
        public InputCondition DashInput;
        public InputCondition JumpInput; 
        
        // 일회성 상태 변경 - 공격
        public InputCondition AttackInput;
        public InputCondition HeavyAttackInput;

        // 차지 공격
        public bool  ChargeAttackHeld; // 현재 홀드 중 (임계값 초과)
        public float ChargeHoldTime;   // 누른 총 시간
        
        // 일회성 상태 변경 - 기타
        public InputCondition EquipInput;
        public InputCondition InteractInput;
        
        public List<InputCondition> SkillInput;
        
        public InputCondition GuardInput;

        public void ClearAll()
        {
            MoveInput = Vector2.zero;
            CameraRotation = Quaternion.identity;
            
            ClearInputConditions();
        }
        
        public void ClearInputConditions()
        {
            CrouchInput = InputCondition.None;
            
            DodgeInput = InputCondition.None;
            DashInput  = InputCondition.None;
            JumpInput  = InputCondition.None;
            
            AttackInput      = InputCondition.None;
            HeavyAttackInput = InputCondition.None;
        }
    }
    
    // BeforeCharacterUpdate -> UpdateRotation / UpdateVelocity -> KCC Motor -> AfterCharacterUpdate
    public partial class PlayerMovementController : ActorMovementController
    {
        [Header("Dash Cooldown")]
        [SerializeField] private float _dashCooldown = 1.5f;

        [Header("Jump Additional")]
        public int MaxJumpCount = 2;           // 최대 점프 횟수 (2 = 2단 점프)
        public float DoubleJumpSpeed = 8f;     // 2단 점프 속도 (1단과 다르게 설정 가능)

        [Header("Move Setting")] 
        public float SprintAutoStartDelay = 3f;
        
        private Vector3 _moveInputVector; // 입력값 캐싱
        private Vector3 _lookInputVector;
        private Vector3 _cameraForwardDirection;

        private PlayerCharacterInputs _inputState;

        private float _dashCooldownTimer;

        public Vector3 LookInputVector => _lookInputVector;
        public Vector3 MoveInputVector => _moveInputVector;
        /// <summary> 카메라 평면 정면 방향 — 이동 입력 유무와 관계없이 항상 최신값 유지 </summary>
        public Vector3 CameraForwardDirection => _cameraForwardDirection;
        
        public bool IsDashReady => _dashCooldownTimer <= 0f;

        /// <summary>대시 쿨타임 잔여 시간(초). 0이면 사용 가능. UI 쿨타임 표시용.</summary>
        public float DashCooldownRemaining => Mathf.Max(0f, _dashCooldownTimer);
        /// <summary>대시 쿨타임 전체 길이(초). UI fill 비율 계산용.</summary>
        public float DashCooldownDuration => _dashCooldown;

        /// <summary>
        /// 대시 쿨타임 시작/종료 통지 (remaining, duration). 대시 쿨타임은 이 컨트롤러가 소유하므로
        /// UI는 이 이벤트로 표시 시작/종료 트리거를 받고, 진행 중 잔여 시간은 매 프레임 폴링한다.
        /// </summary>
        public event Action<float, float> OnDashCooldownChanged;

        public void StartDashCooldown()
        {
            _dashCooldownTimer = _dashCooldown;
            OnDashCooldownChanged?.Invoke(_dashCooldown, _dashCooldown);
        }

        protected override void Start() 
        {
            base.Start();
            
            TransitionToState(new PlayerIdleState(this));
        }

        protected override void Update()
        {
            base.Update();
            
            if (_dashCooldownTimer > 0f)
            {
                _dashCooldownTimer -= Time.deltaTime;
                if (_dashCooldownTimer <= 0f)
                {
                    _dashCooldownTimer = 0f;
                    OnDashCooldownChanged?.Invoke(0f, _dashCooldown); // 종료 통지(표시 끄기 트리거)
                }
            }
        }

        public void ClearInputAll()
        {
            _inputState.ClearAll();
        }

        public void ClearInputConditions()
        {
            _inputState.ClearInputConditions();
        }
        
        // PlayerActor에서 호출하여 입력 전달
        public void SetInputs(PlayerCharacterInputs input)
        {
            if (Motor == null)
                return;
            
            _inputState = input;
            
            // 1. 기본적인 이동 입력 벡터 (X, Z)
            Vector3 rawMoveInput = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);

            // 2. 카메라가 바라보는 방향을 지면(CharacterUp)에 투영하여 기준 방향 설정
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(input.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(input.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            
            // 3. 카메라 기준의 회전값 생성
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            // 4. 입력 벡터를 카메라 회전에 맞춰 변환 (카메라 앞방향이 캐릭터의 이동 앞방향이 됨)
            _moveInputVector = cameraPlanarRotation * rawMoveInput;

            // 5. 카메라 정면 방향은 항상 갱신 (TurnInPlace 등 Idle 상태에서도 참조)
            _cameraForwardDirection = cameraPlanarDirection;

            // 6. 캐릭터가 바라볼 방향 설정 (이동 중일 때만 업데이트)
            if (_moveInputVector.sqrMagnitude > 0f)
            {
                _lookInputVector = _moveInputVector.normalized;
            }
        }
        
        /// <summary>
        /// 호출 시점: 모터가 주변의 물리적 장애물을 감지하고 충돌 계산을 시작하기 직전, 매 충돌 후보마다 호출됩니다.
        /// 역할: 특정 콜라이더와 충돌할지 말지를 결정하는 **'통행권 체크'**입니다.
        /// </summary>
        public override bool IsColliderValidForCollisions(Collider coll)
        {
            if (IgnoredColliders.Contains(coll))
            {
                return false;
            }
            return base.IsColliderValidForCollisions(coll);
        }
    }

    public partial class PlayerMovementController : ActorMovementController
    {
        public bool HasMoveInput()
        {
            return _moveInputVector.sqrMagnitude > 0;
        }

        public bool HasDodgeInput()
        {   
            return _inputState.DodgeInput == InputCondition.Pressed;
        }
        
        public bool HasDashInput()
        {
            return _inputState.DashInput == InputCondition.Pressed;
        }
        public bool HasJumpInput()
        {
            return _inputState.JumpInput == InputCondition.Pressed;
        }

        public bool HasCrouchInput()
        {
            return _inputState.CrouchInput == InputCondition.Pressed;
        }

        public void ClearCrouchInput()
        {
            _inputState.CrouchInput = InputCondition.None;
        }
        
        public void ClearJumpInput()
        {
            _inputState.JumpInput = InputCondition.None;
        }
        
        public bool HasAttackInput()
        {
            return _inputState.AttackInput == InputCondition.Pressed;
        }

        public bool HasHeavyAttackInput()
        {
            return _inputState.HeavyAttackInput == InputCondition.Pressed;
        }

        public bool  IsChargeAttackHeld() => _inputState.ChargeAttackHeld;
        public float GetChargeHoldTime()  => _inputState.ChargeHoldTime;

        public bool HasEquipInput()
        {
            return _inputState.EquipInput == InputCondition.Pressed;
        }

        public bool HasGuardInput()
        {
            return _inputState.GuardInput == InputCondition.Pressed;
        }
        
        public bool HasInteractInput()
        {
            return _inputState.InteractInput == InputCondition.Pressed;
        }

        public bool HasSkillInput(int index)
        {
            if (_inputState.SkillInput == null || _inputState.SkillInput.Count <= index)
                return false;
            
            return _inputState.SkillInput[index] == InputCondition.Pressed;
        }

    }

    public partial class PlayerMovementController : ActorMovementController
    {
        private void OnLanded()
        {
            Debug.Log("Landed");
        }

        private void OnLeaveStableGround()
        {
            Debug.Log("Left ground");
        }
    }
}