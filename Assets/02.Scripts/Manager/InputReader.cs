using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input
{
    /// <summary>
    /// 입력 시스템의 추상화 레이어
    /// InputManager와 캐릭터 사이의 중간 다리 역할
    /// </summary>
    public class InputReader : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float mouseSensitivity = 1f;
        [SerializeField] private bool invertY = false;

        // 입력 값
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool RunPressed { get; private set; }
        public bool RollPressed { get; private set; }
        public bool AttackPressed { get; private set; }
        public bool HeavyAttackPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        
        // 스킬 입력
        public bool Skill1Pressed { get; private set; }
        public bool Skill2Pressed { get; private set; }
        public bool Skill3Pressed { get; private set; }
        public bool Skill4Pressed { get; private set; }
        
        public bool Skill1Held { get; private set; }
        public bool Skill2Held { get; private set; }
        public bool Skill3Held { get; private set; }
        public bool Skill4Held { get; private set; }
        
        public void OnSkill1(InputAction.CallbackContext context) => HandleSkillInput(context, 1);
        public void OnSkill2(InputAction.CallbackContext context) => HandleSkillInput(context, 2);
        public void OnSkill3(InputAction.CallbackContext context) => HandleSkillInput(context, 3);
        public void OnSkill4(InputAction.CallbackContext context) => HandleSkillInput(context, 4);
        
        // 이벤트
        public event Action OnJumpEvent;
        public event Action OnRollEvent;
        public event Action OnAttackEvent;
        public event Action OnHeavyAttackEvent;
        public event Action OnInteractEvent;
        
        // 스킬 이벤트
        public event Action<int> OnSkillPressed;  // 스킬 번호 전달 (1-4)
        public event Action<int> OnSkillReleased; // 차징 스킬용

        // Input Actions
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _runAction;
        private InputAction _rollAction;
        private InputAction _attackAction;
        private InputAction _heavyAttackAction;
        private InputAction _interactAction;
        
        // Skill Actions
        private InputAction _skill1Action;
        private InputAction _skill2Action;
        private InputAction _skill3Action;
        private InputAction _skill4Action;

        private void Start()
        {
            InitializeInputActions();
        }

        private void OnEnable()
        {
            EnableGameplayInput();
        }

        private void OnDisable()
        {
            DisableGameplayInput();
        }

        private void InitializeInputActions()
        {
            if (InputManager.Instance == null)
            {
                Debug.LogError("[InputReader] InputManager가 없습니다!");
                return;
            }

            _moveAction = InputManager.Instance.MoveAction;
            _lookAction = InputManager.Instance.LookAction;
            _jumpAction = InputManager.Instance.JumpAction;
            _runAction = InputManager.Instance.RunAction;
            _rollAction = InputManager.Instance.RollAction;
            _attackAction = InputManager.Instance.AttackAction;
            _heavyAttackAction = InputManager.Instance.HeavyAttackAction;
            _interactAction = InputManager.Instance.InteractAction;
            
            // 스킬 액션 초기화
            _skill1Action = InputManager.Instance.Skill1Action;
            _skill2Action = InputManager.Instance.Skill2Action;
            _skill3Action = InputManager.Instance.Skill3Action;
            _skill4Action = InputManager.Instance.Skill4Action;

            // 이벤트 구독
            if (_jumpAction != null)
            {
                _jumpAction.performed += OnJumpPerformed;
                _jumpAction.canceled += OnJumpCanceled;
            }

            if (_rollAction != null)
                _rollAction.performed += OnRollPerformed;

            if (_attackAction != null)
                _attackAction.performed += OnAttackPerformed;

            if (_heavyAttackAction != null)
                _heavyAttackAction.performed += OnHeavyAttackPerformed;

            if (_interactAction != null)
                _interactAction.performed += OnInteractPerformed;
                
            // 스킬 이벤트 구독
            if (_skill1Action != null)
            {
                _skill1Action.performed += ctx => OnSkillPerformed(1);
                _skill1Action.canceled += ctx => OnSkillCanceled(1);
            }
            
            if (_skill2Action != null)
            {
                _skill2Action.performed += ctx => OnSkillPerformed(2);
                _skill2Action.canceled += ctx => OnSkillCanceled(2);
            }
            
            if (_skill3Action != null)
            {
                _skill3Action.performed += ctx => OnSkillPerformed(3);
                _skill3Action.canceled += ctx => OnSkillCanceled(3);
            }
            
            if (_skill4Action != null)
            {
                _skill4Action.performed += ctx => OnSkillPerformed(4);
                _skill4Action.canceled += ctx => OnSkillCanceled(4);
            }
        }

        private void Update()
        {
            UpdateInputValues();
        }

        private void UpdateInputValues()
        {
            // Move
            if (_moveAction != null)
                MoveInput = _moveAction.ReadValue<Vector2>();

            // Look (마우스 감도 적용)
            if (_lookAction != null)
            {
                Vector2 rawLook = _lookAction.ReadValue<Vector2>();
                LookInput = new Vector2(
                    rawLook.x * mouseSensitivity,
                    rawLook.y * mouseSensitivity * (invertY ? -1f : 1f)
                );
            }

            // Run (홀드 체크)
            if (_runAction != null)
                RunPressed = _runAction.IsPressed();

            // Jump (홀드 체크)
            if (_jumpAction != null)
                JumpHeld = _jumpAction.IsPressed();
                
            // 스킬 홀드 상태 체크 (차징 스킬용)
            if (_skill1Action != null)
                Skill1Held = _skill1Action.IsPressed();
            if (_skill2Action != null)
                Skill2Held = _skill2Action.IsPressed();
            if (_skill3Action != null)
                Skill3Held = _skill3Action.IsPressed();
            if (_skill4Action != null)
                Skill4Held = _skill4Action.IsPressed();
        }

        #region 입력 이벤트 콜백

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            JumpPressed = true;
            OnJumpEvent?.Invoke();
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            JumpPressed = false;
        }

        private void OnRollPerformed(InputAction.CallbackContext context)
        {
            RollPressed = true;
            OnRollEvent?.Invoke();
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            AttackPressed = true;
            OnAttackEvent?.Invoke();
        }

        private void OnHeavyAttackPerformed(InputAction.CallbackContext context)
        {
            HeavyAttackPressed = true;
            OnHeavyAttackEvent?.Invoke();
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            InteractPressed = true;
            OnInteractEvent?.Invoke();
        }
        
        private void HandleSkillInput(InputAction.CallbackContext context, int skillIndex)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                OnSkillPressed?.Invoke(skillIndex);
            }
            else if (context.phase == InputActionPhase.Canceled)
            {
                OnSkillReleased?.Invoke(skillIndex);
            }
        }

        private void OnSkillPerformed(int skillIndex)
        {
            switch (skillIndex)
            {
                case 1: Skill1Pressed = true; break;
                case 2: Skill2Pressed = true; break;
                case 3: Skill3Pressed = true; break;
                case 4: Skill4Pressed = true; break;
            }
            OnSkillPressed?.Invoke(skillIndex);
        }
        
        private void OnSkillCanceled(int skillIndex)
        {
            switch (skillIndex)
            {
                case 1: Skill1Pressed = false; break;
                case 2: Skill2Pressed = false; break;
                case 3: Skill3Pressed = false; break;
                case 4: Skill4Pressed = false; break;
            }
            OnSkillReleased?.Invoke(skillIndex);
        }

        #endregion

        #region 입력 소비

        /// <summary>
        /// 점프 입력 소비
        /// </summary>
        public void ConsumeJumpInput()
        {
            JumpPressed = false;
        }

        /// <summary>
        /// 회피 입력 소비
        /// </summary>
        public void ConsumeRollInput()
        {
            RollPressed = false;
        }

        /// <summary>
        /// 공격 입력 소비
        /// </summary>
        public void ConsumeAttackInput()
        {
            AttackPressed = false;
        }

        /// <summary>
        /// 강공격 입력 소비
        /// </summary>
        public void ConsumeHeavyAttackInput()
        {
            HeavyAttackPressed = false;
        }

        /// <summary>
        /// 상호작용 입력 소비
        /// </summary>
        public void ConsumeInteractInput()
        {
            InteractPressed = false;
        }
        
        /// <summary>
        /// 스킬 입력 소비
        /// </summary>
        public void ConsumeSkillInput(int skillIndex)
        {
            switch (skillIndex)
            {
                case 1: Skill1Pressed = false; break;
                case 2: Skill2Pressed = false; break;
                case 3: Skill3Pressed = false; break;
                case 4: Skill4Pressed = false; break;
            }
        }

        /// <summary>
        /// 모든 입력 소비
        /// </summary>
        public void ConsumeAllInputs()
        {
            JumpPressed = false;
            RollPressed = false;
            AttackPressed = false;
            HeavyAttackPressed = false;
            InteractPressed = false;
            
            Skill1Pressed = false;
            Skill2Pressed = false;
            Skill3Pressed = false;
            Skill4Pressed = false;
        }

        #endregion

        #region 입력 제어

        public void EnableGameplayInput()
        {
            if (InputManager.Instance != null)
                InputManager.Instance.SwitchToGameplay();
        }

        public void DisableGameplayInput()
        {
            ConsumeAllInputs();
        }

        public void SetMouseSensitivity(float sensitivity)
        {
            mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
        }

        public void SetInvertY(bool invert)
        {
            invertY = invert;
        }

        #endregion

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (_jumpAction != null)
            {
                _jumpAction.performed -= OnJumpPerformed;
                _jumpAction.canceled -= OnJumpCanceled;
            }

            if (_rollAction != null)
                _rollAction.performed -= OnRollPerformed;

            if (_attackAction != null)
                _attackAction.performed -= OnAttackPerformed;

            if (_heavyAttackAction != null)
                _heavyAttackAction.performed -= OnHeavyAttackPerformed;

            if (_interactAction != null)
                _interactAction.performed -= OnInteractPerformed;
        }
    }
}