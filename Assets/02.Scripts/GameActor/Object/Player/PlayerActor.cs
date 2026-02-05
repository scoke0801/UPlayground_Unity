using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UPlayGround.Data.Enum;
using UPlayGround.Animation;
using UPlayGround.Component;
using UPlayGround.MovementController;
using UPlayGround.Input;
using UPlayGround.InputDefine;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 
    /// </summary>
    public partial class PlayerActor : GameActor
    {
        protected PlayerMovementController PlayerMovementController;
        private Camera _camera;
        private PlayerActorAnimator _playerActorAnimator;
        
        public override ActorAnimator Animator => _playerActorAnimator;
        #region Mono
        protected override void Awake()
        {
            base.Awake();
            
            _camera = Camera.main;
            PlayerMovementController = MovementController as PlayerMovementController;

            _playerActorAnimator = _animator as PlayerActorAnimator;
            
            InitComponents();
            
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
                
                EquipInput = _equipInputCondition,
                
                SkillInput =  new List<InputCondition>()
                {
                    _skillInputCondition[0],
                    _skillInputCondition[1],
                    _skillInputCondition[2],
                    _skillInputCondition[3],
                },
            };

            // 이동 입력과 카메라 회전값을 함께 전달
            PlayerMovementController.SetInputs(characterInputs);
            
            // 전달 후 요청 초기화 (한 프레임만 유효)
            // [TODO] 어느정도 입력 버퍼 시간이 필요하다면... 바로 초기화를 하지 않아야한다.
            //_jumpInputCondition = InputCondition.None;
            _dodgeInputCondition = InputCondition.None;
            _attackInputCondition = InputCondition.None;
            _heavyInputCondition = InputCondition.None;
            _equipInputCondition = InputCondition.None;

            for (int i = 0; i < _skillInputCondition.Count; ++i)
            {
                _skillInputCondition[i] = InputCondition.None;
            }
        }
        #endregion
    }

    // Input 처리
    public partial class PlayerActor : GameActor
    {
        private Vector2 _currentMoveInput;
        private InputCondition _jumpInputCondition;
        private InputCondition _crouchInputCondition;
        private InputCondition _dodgeInputCondition;
        
        private InputCondition _attackInputCondition;
        private InputCondition _heavyInputCondition;
        
        private InputCondition _equipInputCondition;
        private List<InputCondition> _skillInputCondition = new List<InputCondition> 
        { 
            InputCondition.None,
            InputCondition.None,
            InputCondition.None,
            InputCondition.None 
        };
        
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

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_1,
                    null, OnInputPerformedSkill_1, null, null, null, layer);
              
                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_2,
                    null, OnInputPerformedSkill_2, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_3,
                    null, OnInputPerformedSkill_3, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_4,
                    null, OnInputPerformedSkill_4, null, null, null, layer);

                InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Equip,
                    null, OnInputPerformedEquipWeapon, null, null, null, layer);
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
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_1,
                    null, OnInputPerformedSkill_1, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_2,
                    null, OnInputPerformedSkill_2, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_3,
                    null, OnInputPerformedSkill_3, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Skill_4,
                    null, OnInputPerformedSkill_4, null);
                
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.Equip,
                    null, OnInputPerformedEquipWeapon, null);
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
            _heavyInputCondition = InputCondition.Pressed;
        }

        private void OnInputPerformedAttack(InputAction.CallbackContext obj)
        {
            _attackInputCondition = InputCondition.Pressed;
        }
        
        private void OnInputPerformedEquipWeapon(InputAction.CallbackContext obj)
        {
            _equipInputCondition = InputCondition.Pressed;
        }
        
        private void OnInputPerformedSkill_1(InputAction.CallbackContext obj)
        {
            _skillInputCondition[0] = InputCondition.Pressed;
        }
        private void OnInputPerformedSkill_2(InputAction.CallbackContext obj)
        {
            _skillInputCondition[1] = InputCondition.Pressed;
        }
        private void OnInputPerformedSkill_3(InputAction.CallbackContext obj)
        {
            _skillInputCondition[2] = InputCondition.Pressed;
        }
        private void OnInputPerformedSkill_4(InputAction.CallbackContext obj)
        {
            _skillInputCondition[3] = InputCondition.Pressed;
        }
        #endregion

        public void ClearCrouchInput()
        {
            _crouchInputCondition = InputCondition.None;
            PlayerMovementController.ClearCrouchInput();
        }

        public void ClearJumpInput()
        {
            _jumpInputCondition = InputCondition.None;
            PlayerMovementController.ClearJumpInput();
        }
    }

    // Component
    public partial class PlayerActor : GameActor
    {
        // 추가 컴포넌트
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private PlayerCombat _combat;
        
        public PlayerEquipment GetPlayerEquipment() { return _equipment; }
        public PlayerCombat GetCombat() { return _combat; }

        public bool IsEquippedRightWeapon => _equipment.IsMainWeaponEquipped;
        public bool IsEquippedLeftWeapon => _equipment.IsSubWeaponEquipped;

        private void InitComponents()
        {
            // _equipment.SetLeftWeaponType(WeaponType.Shield);
            // _equipment.SetRightWeaponType(WeaponType.Sword);
            //
            // StartCoroutine(EquipWeapon());
        }
    }
    
}