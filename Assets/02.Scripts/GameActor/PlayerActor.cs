using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.Enum;
using UPlayGround.GameActor.MovementController;
using UPlayground.Input;
using UPlayGround.InputDefine;

namespace UPlayGround.GameActor
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PlayerActor : Base.GameActor
    {
        protected PlayerMovementController PlayerMovementController;
        private Camera _camera;
        
        #region Mono
        protected override void Awake()
        {
            base.Awake();
            
            _camera = Camera.main;
            PlayerMovementController = MovementController as PlayerMovementController;

            RegisterInputEvents();
        }

        private void OnDestroy()
        {
            UnRegisterInputEvents();
        }
        
        private void Update()
        {
            if (MovementController == null) return;

            // CameraManager를 통해 현재 메인 카메라의 회전값을 가져옴
            Quaternion cameraRotation = Quaternion.identity;
            if (_camera != null)
            {
                cameraRotation = _camera.transform.rotation;
            }

            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs
            {
                // Build the CharacterInputs struct
                MoveInput = _currentMoveInput,
                CameraRotation = cameraRotation,
                CrouchInput = _crouchInputCondition,
                
                JumpInput = _jumpInputCondition,
                DodgeInput =  _dodgeInputCondition,
                
                AttackInput =  _attackInputCondition,
                HeavyAttackInput =  _heavyInputCondition,
            };

            // 이동 입력과 카메라 회전값을 함께 전달
            PlayerMovementController.SetInputs(characterInputs);
            
            // 전달 후 요청 초기화 (한 프레임만 유효)
            // [TODO] 어느정도 입력 버퍼 시간이 필요하다면... 바로 초기화를 하지 않아야한다.
            _jumpInputCondition = InputCondition.None;
            _dodgeInputCondition = InputCondition.None;
            _attackInputCondition = InputCondition.None;
            _heavyInputCondition = InputCondition.None;
        }
        #endregion
    }

    // Input 처리
    public partial class PlayerActor : Base.GameActor
    {
        private Vector2 _currentMoveInput;
        private InputCondition _jumpInputCondition;
        private InputCondition _crouchInputCondition;
        private InputCondition _dodgeInputCondition;
        
        private InputCondition _attackInputCondition;
        private InputCondition _heavyInputCondition;
        
        private void RegisterInputEvents()
        {
            if (InputManager.Instance)
            {
                InputLayer layer = InputLayer.Level_0;
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Move,
                    OnInputMove, OnInputMove, OnInputMove, null, OnMoveCanceled, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Jump,
                    null, OnInputPerformedJump, null, null, null, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Walk,
                    null, OnInputPerformedWalk, null, null, null, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Sprint,
                    null, OnInputPerformedSprint, null, null, null, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Crouching,
                    null, OnInputPerformedCrouching, null, null, null, layer);
                                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dodge,
                    null, OnInputPerformedDodge, null, null, null, layer);
                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Attack,
                    null, OnInputPerformedAttack, null, null, null, layer);
                                
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.HeavyAttack,
                    null, OnInputPerformedHeavyAttack, null, null, null, layer);
            }
        }
        private void UnRegisterInputEvents()
        {   
            if (InputManager.Instance)
            {
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Move,
                    OnInputMove, OnInputMove, OnInputMove);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Jump,
                    null, OnInputPerformedJump, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Walk,
                    null, OnInputPerformedWalk, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Sprint,
                    null, OnInputPerformedSprint, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Crouching,
                    null, OnInputPerformedCrouching, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Dodge,
                    null, OnInputPerformedDodge, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Attack,
                    null, OnInputPerformedAttack, null);
                                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.HeavyAttack,
                    null, OnInputPerformedHeavyAttack, null);
            }
        }
        
        #region InputCallback
        private void OnInputMove(InputAction.CallbackContext obj)
        {
            _currentMoveInput = obj.ReadValue<Vector2>();
        }
        
        private void OnMoveCanceled()
        {
            _currentMoveInput = Vector2.zero;
            PlayerMovementController.ClearInputAll();
        }
        
        private void OnInputPerformedJump(InputAction.CallbackContext obj)
        {
            _jumpInputCondition = InputCondition.Pressed;
        }
        
        private void OnInputPerformedCrouching(InputAction.CallbackContext obj)
        {
            _crouchInputCondition = (_crouchInputCondition == InputCondition.Pressed)
                ? InputCondition.None : InputCondition.Pressed;
        }
        
        private void OnInputPerformedDodge(InputAction.CallbackContext obj)
        {
            _dodgeInputCondition = InputCondition.Pressed;
        }
        
        private void OnInputPerformedWalk(InputAction.CallbackContext obj)
        {
            MoveAnimType = MoveAnimType == BaseMoveAnimType.Walk ? BaseMoveAnimType.Run : BaseMoveAnimType.Walk;
        }
        
        private void OnInputPerformedSprint(InputAction.CallbackContext obj)
        {
            MoveAnimType = MoveAnimType == BaseMoveAnimType.Sprint ? BaseMoveAnimType.Run : BaseMoveAnimType.Sprint;
        }
        
        private void OnInputPerformedHeavyAttack(InputAction.CallbackContext obj)
        {
            _attackInputCondition = InputCondition.Pressed;
        }

        private void OnInputPerformedAttack(InputAction.CallbackContext obj)
        {
            _heavyInputCondition = InputCondition.Pressed;
        }
        #endregion

        public void ClearCrouchInput()
        {
            _crouchInputCondition = InputCondition.None;
            PlayerMovementController.ClearCrouchInput();
        }
    }
}