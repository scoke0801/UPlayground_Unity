using UnityEngine;
using Game.Input;

namespace Game.FSM
{
    /// <summary>
    /// 플레이어 입력 처리 및 캐릭터 제어
    /// </summary>
    public class PlayerBrain : CharacterBrain
    {
        [Header("Input")]
        [SerializeField] private InputReader inputReader;

        [Header("Input Buffer")]
        [SerializeField] private float bufferTime = 0.15f;
        [SerializeField] private int maxBufferSize = 10;

        private InputBuffer _inputBuffer;
        private Camera _cachedCamera;

        protected override void Awake()
        {
            base.Awake();

            _cachedCamera = Camera.main;
            _inputBuffer = new InputBuffer(bufferTime, maxBufferSize);

            // InputReader 초기화
            if (inputReader == null)
            {
                inputReader = GetComponent<InputReader>();
                if (inputReader == null)
                {
                    inputReader = gameObject.AddComponent<InputReader>();
                    Debug.Log("[PlayerBrain] InputReader 컴포넌트를 자동 추가했습니다.");
                }
            }

            SubscribeToInputEvents();
        }

        private void SubscribeToInputEvents()
        {
            if (inputReader == null) return;

            // 이벤트 구독
            inputReader.OnJumpEvent += () => _inputBuffer.AddInput("Jump");
            inputReader.OnRollEvent += () => _inputBuffer.AddInput("Roll");
            inputReader.OnAttackEvent += () => _inputBuffer.AddInput("Attack");
            inputReader.OnHeavyAttackEvent += () => _inputBuffer.AddInput("HeavyAttack");
            inputReader.OnInteractEvent += () => _inputBuffer.AddInput("Interact");
        }

        protected override void HandleInput()
        {
            if (inputReader == null) return;

            // 이동 입력 처리 (카메라 기준)
            ProcessMovementInput();

            // 버퍼된 입력 처리
            ProcessBufferedInputs();
        }

        /// <summary>
        /// 이동 입력 처리 (카메라 기준)
        /// </summary>
        private void ProcessMovementInput()
        {
            Vector2 moveInput = inputReader.MoveInput;

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
        }

        /// <summary>
        /// 버퍼된 입력 처리
        /// </summary>
        private void ProcessBufferedInputs()
        {
            // 점프
            if (_inputBuffer.HasInput("Jump"))
            {
                SetJumpInput(true);
                _inputBuffer.ConsumeInput("Jump");
            }

            // 회피
            if (_inputBuffer.HasInput("Roll"))
            {
                SetDodgeInput(true);
                _inputBuffer.ConsumeInput("Roll");
            }

            // 공격
            if (_inputBuffer.HasInput("Attack"))
            {
                SetAttackInput(AttackInputType.Light);
                _inputBuffer.ConsumeInput("Attack");
            }

            // 강공격
            if (_inputBuffer.HasInput("HeavyAttack"))
            {
                SetAttackInput(AttackInputType.Heavy);
                _inputBuffer.ConsumeInput("HeavyAttack");
            }

            // 상호작용
            if (_inputBuffer.HasInput("Interact"))
            {
                // 상호작용 로직 처리
                HandleInteract();
                _inputBuffer.ConsumeInput("Interact");
            }
        }

        /// <summary>
        /// 상호작용 처리
        /// </summary>
        private void HandleInteract()
        {
            // TODO: 상호작용 로직 구현
            Debug.Log("[PlayerBrain] 상호작용 입력!");
        }

        /// <summary>
        /// 입력 버퍼 클리어
        /// </summary>
        public void ClearInputBuffer()
        {
            _inputBuffer.Clear();
        }

        /// <summary>
        /// 입력 버퍼 디버그 출력
        /// </summary>
        [ContextMenu("Debug Input Buffer")]
        public void DebugInputBuffer()
        {
            _inputBuffer.DebugPrint();
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제는 InputReader에서 처리
        }
    }
}
