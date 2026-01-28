using System.Collections.Generic;
using JetBrains.Annotations;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround.GameActor.State;
using UPlayGround.Input;

namespace UPlayGround.GameActor.MovementController
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
        public InputCondition JumpInput; 
        
        // 일회성 상태 변경 - 공격
        public InputCondition AttackInput;
        public InputCondition HeavyAttackInput;
        
        // 일회성 상태 변경 - 기타
        public InputCondition EquipInput;
        
        public List<InputCondition> SkillInput;

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
            JumpInput  = InputCondition.None;
            
            AttackInput      = InputCondition.None;
            HeavyAttackInput = InputCondition.None;
        }
    }
    
    // BeforeCharacterUpdate -> UpdateRotation / UpdateVelocity -> KCC Motor -> AfterCharacterUpdate
    public partial class PlayerMovementController : ActorMovementController
    {
        private Vector3 _moveInputVector; // 입력값 캐싱
        private Vector3 _lookInputVector;
        
        private PlayerCharacterInputs _inputState;
        
        public Vector3 LookInputVector => _lookInputVector;
        public Vector3 MoveInputVector => _moveInputVector;
        
        protected override void Start()
        {
            base.Start();
            
            TransitionToState(new PlayerIdleState(this));
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
            
            // 5. 캐릭터가 바라볼 방향 설정 (이동 중일 때만 업데이트하거나 카메라 정면 유지)
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
        
        public bool HasAttackInput()
        {
            return _inputState.AttackInput == InputCondition.Pressed;
        }

        public bool HasHeavyAttackInput()
        {
            return _inputState.HeavyAttackInput == InputCondition.Pressed;
        }

        public bool HasEquipInput()
        {
            return _inputState.EquipInput == InputCondition.Pressed;
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