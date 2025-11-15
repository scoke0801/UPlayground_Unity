using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 캐릭터 클래스 - Input System 버전
/// </summary>
public class Player : Character
{
    [Header("플레이어 데이터")]
    [SerializeField] private PlayerSettingsData playerSettings;
    
    [Header("컴포넌트 참조")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraFollowTarget;
    
    // 입력
    private Vector2 moveInput;
    private Vector2 lookInput;
    
    // 카메라
    private float cameraRotationX = 0f;
    private float cameraRotationY = 0f;
    
    // 물리 처리용 플래그
    private bool shouldJump;
    private bool shouldRoll;
    private Vector3 rollDirection;
    
    // Input Manager 참조
    private InputManager inputManager;
    
    // 이벤트
    public System.Action<int> OnLevelUp;
    public System.Action<int, int> OnExpChanged;
    
    protected override void InitializeComponents()
    {
        base.InitializeComponents();
       
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        if (cameraFollowTarget == null)
            cameraFollowTarget = transform;
        
        // 데이터 유효성 검사
        if (playerSettings == null)
        {
            Debug.LogError($"{gameObject.name}: PlayerSettingsData가 할당되지 않았습니다!");
        }
        
        // InputManager 참조
        inputManager = InputManager.Instance;
        if (inputManager == null)
        {
            Debug.LogError($"{gameObject.name}: InputManager를 찾을 수 없습니다!");
        }
    }
    
    protected override void Initialize()
    {
        base.Initialize();
        
        SetupCamera();
        SubscribeToInputEvents();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    /// <summary>
    /// 입력 이벤트 구독
    /// </summary>
    private void SubscribeToInputEvents()
    {
        if (inputManager == null) return;
        
        // 버튼 액션 이벤트 구독
        if (inputManager.JumpAction != null)
            inputManager.JumpAction.performed += OnJumpPerformed;
        
        if (inputManager.RollAction != null)
            inputManager.RollAction.performed += OnRollPerformed;
        
        if (inputManager.AttackAction != null)
            inputManager.AttackAction.performed += OnAttackPerformed;
        
        if (inputManager.InteractAction != null)
            inputManager.InteractAction.performed += OnInteractPerformed;
        
        Debug.Log("[Player] 입력 이벤트 구독 완료");
    }
    
    /// <summary>
    /// 입력 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromInputEvents()
    {
        if (inputManager == null) return;
        
        if (inputManager.JumpAction != null)
            inputManager.JumpAction.performed -= OnJumpPerformed;
        
        if (inputManager.RollAction != null)
            inputManager.RollAction.performed -= OnRollPerformed;
        
        if (inputManager.AttackAction != null)
            inputManager.AttackAction.performed -= OnAttackPerformed;
        
        if (inputManager.InteractAction != null)
            inputManager.InteractAction.performed -= OnInteractPerformed;
        
        Debug.Log("[Player] 입력 이벤트 구독 해제");
    }
    
    protected override void HandleInput()
    {
        if (inputManager == null) return;
        
        // 게임플레이 모드일 때만 입력 처리
        if (inputManager.CurrentMode != InputMode.Gameplay) 
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            return;
        }
        
        // 이동 입력 (연속)
        if (inputManager.MoveAction != null)
            moveInput = inputManager.MoveAction.ReadValue<Vector2>();
        
        // 시점 입력 (연속)
        if (inputManager.LookAction != null)
            lookInput = inputManager.LookAction.ReadValue<Vector2>();
    }
    
    protected override void UpdateGameLogic()
    {
        base.UpdateGameLogic();
        ProcessMovementInput();
        UpdateAnimations();
    }
    
    protected override void HandlePhysicsMovement()
    {
        base.HandlePhysicsMovement();
        
        // 점프 처리
        if (shouldJump)
        {
            Jump();
            shouldJump = false;
        }
        
        // 구르기 처리
        if (shouldRoll)
        {
            Roll(rollDirection);
            shouldRoll = false;
        }
    }
    
    protected override void HandleLateUpdate()
    {
        UpdateCameraRotation();
        UpdateCameraPosition();
    }
    
    #region 입력 이벤트 핸들러
    
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (inputManager.CurrentMode != InputMode.Gameplay) return;
        shouldJump = true;
        Debug.Log("[Player] 점프 입력");
    }
    
    private void OnRollPerformed(InputAction.CallbackContext context)
    {
        if (inputManager.CurrentMode != InputMode.Gameplay) return;
        
        if (moveInput.magnitude > 0.1f)
        {
            // 입력 방향으로 구르기
            Vector3 inputDirection = playerCamera.transform.forward * moveInput.y + 
                                    playerCamera.transform.right * moveInput.x;
            inputDirection.y = 0;
            rollDirection = inputDirection.normalized;
            shouldRoll = true;
        }
        else
        {
            // 현재 바라보는 방향으로 구르기
            rollDirection = transform.forward;
            shouldRoll = true;
        }
        
        Debug.Log("[Player] 구르기 입력");
    }
    
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (inputManager.CurrentMode != InputMode.Gameplay) return;
        PerformAttack();
    }
    
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (inputManager.CurrentMode != InputMode.Gameplay) return;
        TryInteract();
    }
    
    #endregion
    
    #region 이동 및 카메라
    
    /// <summary>
    /// 이동 입력을 물리 이동으로 변환
    /// </summary>
    private void ProcessMovementInput()
    {
        if (inputManager == null || playerSettings == null) return;
        
        Debug.Log($"moveInput: {moveInput}");
        if (moveInput.magnitude > playerSettings.deadzone)
        {
            // 카메라 기준 이동 방향 계산
            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraRight = playerCamera.transform.right;
            
            cameraForward.y = 0;
            cameraRight.y = 0;
            
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
            
            SetMovementInput(moveDirection);
            
            if (moveDirection != Vector3.zero)
            {
                SetRotationTarget(moveDirection);
            }
        }
        else
        {
            SetMovementInput(Vector3.zero);
        }
        
        // 달리기 설정 (Hold 방식)
        bool isRunning = false;
        if (inputManager.RunAction != null)
        {
            isRunning = inputManager.RunAction.IsPressed() && moveInput.magnitude > playerSettings.deadzone;
        }
        SetRunning(isRunning);
    }
    
    /// <summary>
    /// 카메라 회전 계산
    /// </summary>
    private void UpdateCameraRotation()
    {
        if (playerCamera == null || playerSettings == null) return;
        if (inputManager == null || inputManager.CurrentMode != InputMode.Gameplay) return;
        
        // 마우스 입력으로 카메라 회전 (Time.deltaTime 적용)
        float sensitivity = playerSettings.mouseSensitivity * Time.deltaTime;
    
        // 수평 회전 (Y축)
        cameraRotationY += lookInput.x * sensitivity;
    
        // 수직 회전 (X축)
        float yInput = playerSettings.invertYAxis ? lookInput.y : -lookInput.y;
        cameraRotationX += yInput * sensitivity;
    
        // 수직 회전 각도 제한
        cameraRotationX = Mathf.Clamp(cameraRotationX, 
            playerSettings.cameraMinVerticalAngle, 
            playerSettings.cameraMaxVerticalAngle);
    }
    
    /// <summary>
    /// 카메라 위치 업데이트 (LateUpdate에서 실행)
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (playerCamera == null || cameraFollowTarget == null || playerSettings == null) return;
        
        // 카메라 위치 및 회전 계산
        Quaternion cameraRotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0);
        Vector3 cameraOffset = cameraRotation * Vector3.back * playerSettings.cameraDistance + 
                              Vector3.up * playerSettings.cameraHeight;
        Vector3 targetPosition = cameraFollowTarget.position + cameraOffset;
        
        // 카메라 이동 (부드럽게)
        playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position, 
            targetPosition, 
            Time.deltaTime * playerSettings.cameraSmoothness
        );
        
        playerCamera.transform.LookAt(
            cameraFollowTarget.position + Vector3.up * playerSettings.cameraHeight * 0.5f
        );
    }
    
    #endregion
    
    #region 액션
    
    /// <summary>
    /// 공격 실행
    /// </summary>
    private void PerformAttack()
    {
        Debug.Log("[Player] 공격!");
        SetAnimationTrigger("Attack");
        
        // TODO: 공격 로직 구현
        // - 무기 스윙
        // - 데미지 판정
        // - 이펙트 재생
    }
    
    /// <summary>
    /// 상호작용 시도
    /// </summary>
    private void TryInteract()
    {
        // NPC와의 상호작용 체크
        Collider[] colliders = Physics.OverlapSphere(transform.position, 2f);
        
        foreach (var col in colliders)
        {
            NPC npc = col.GetComponent<NPC>();
            if (npc != null && npc.CanInteract)
            {
                npc.Interact();
                Debug.Log($"[Player] {npc.NPCName}와 상호작용!");
                return;
            }
        }
        
        Debug.Log("[Player] 상호작용 가능한 대상이 없습니다.");
    }
    
    #endregion
    
    /// <summary>
    /// 애니메이션 업데이트
    /// </summary>
    private void UpdateAnimations()
    {
        if (characterAnimator == null) return;
        
        float moveSpeed = moveInput.magnitude;
        SetAnimationFloat("MoveSpeed", moveSpeed);
        SetAnimationBool("IsGrounded", isGrounded);
    }
    
    /// <summary>
    /// 카메라 초기 설정
    /// </summary>
    private void SetupCamera()
    {
        if (playerCamera != null && cameraFollowTarget != null && playerSettings != null)
        {
            Vector3 cameraPos = cameraFollowTarget.position - 
                               cameraFollowTarget.forward * playerSettings.cameraDistance + 
                               Vector3.up * playerSettings.cameraHeight;
            playerCamera.transform.position = cameraPos;
            playerCamera.transform.LookAt(cameraFollowTarget);
            
            Debug.Log("[Player] 카메라 초기 설정 완료");
        }
    }
    
    protected override void Die()
    {
        base.Die();
        
        // 입력 비활성화
        if (inputManager != null)
        {
            inputManager.DisableAllInput();
        }
        
        Debug.Log("[Player] 플레이어가 사망했습니다!");
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromInputEvents();
    }
}
