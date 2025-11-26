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

        // 이벤트
        public event Action OnJumpEvent;
        public event Action OnRollEvent;
        public event Action OnAttackEvent;
        public event Action OnHeavyAttackEvent;
        public event Action OnInteractEvent;

        // Input Actions
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _runAction;
        private InputAction _rollAction;
        private InputAction _attackAction;
        private InputAction _heavyAttackAction;
        private InputAction _interactAction;

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
        /// 모든 입력 소비
        /// </summary>
        public void ConsumeAllInputs()
        {
            JumpPressed = false;
            RollPressed = false;
            AttackPressed = false;
            HeavyAttackPressed = false;
            InteractPressed = false;
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