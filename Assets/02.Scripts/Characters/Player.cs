using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 캐릭터 클래스 - Input System 버전
/// TPS 카메라 각도 제한, 충돌 처리, 줌 기능 포함
/// </summary>
public class Player : Character
{
    [Header("플레이어 데이터")]
    [SerializeField] private PlayerSettingsData playerSettings;
    
    [Header("컴포넌트 참조")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraFollowTarget;
    
    [Header("카메라 충돌 설정")]
    [SerializeField] private LayerMask cameraCollisionLayers = -1; // 모든 레이어와 충돌 체크
    [SerializeField] private float cameraCollisionRadius = 0.2f;   // 카메라 충돌 검사 반경
    [SerializeField] private float cameraCollisionBuffer = 0.2f;   // 벽과의 여유 공간
    
    // 입력
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float zoomInput;
    
    // 카메라 회전
    private float cameraRotationX = 0f;  // 수직 회전 (Pitch)
    private float cameraRotationY = 0f;  // 수평 회전 (Yaw)
    
    // 카메라 거리 (줌)
    private float currentCameraDistance;  // 현재 카메라 거리
    private float targetCameraDistance;   // 목표 카메라 거리 (줌 적용)
    
    // 카메라 스무딩
    private Vector2 currentLookVelocity;
    private Vector2 smoothedLookInput;
    
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
        
        // 카메라 거리 초기화
        if (playerSettings != null)
        {
            currentCameraDistance = playerSettings.cameraDistance;
            targetCameraDistance = playerSettings.cameraDistance;
        }
        
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
            zoomInput = 0f;
            return;
        }
        
        // 이동 입력 (연속)
        if (inputManager.MoveAction != null)
            moveInput = inputManager.MoveAction.ReadValue<Vector2>();
        
        // 시점 입력 (연속)
        if (inputManager.LookAction != null)
            lookInput = inputManager.LookAction.ReadValue<Vector2>();
        
        // 줌 입력 (마우스 휠)
        zoomInput = Input.GetAxis("Mouse ScrollWheel");
    }
    
    protected override void UpdateGameLogic()
    {
        base.UpdateGameLogic();
        ProcessMovementInput();
        ProcessZoomInput();
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
    /// 줌 입력 처리 (마우스 휠)
    /// </summary>
    private void ProcessZoomInput()
    {
        if (playerSettings == null) return;
        if (Mathf.Abs(zoomInput) < 0.01f) return;
        
        // 줌 인/아웃
        targetCameraDistance -= zoomInput * playerSettings.zoomSpeed;
        targetCameraDistance = Mathf.Clamp(
            targetCameraDistance, 
            playerSettings.cameraMinDistance, 
            playerSettings.cameraMaxDistance
        );
    }
    
    /// <summary>
    /// 카메라 회전 계산 (스무딩 + 각도 제한)
    /// </summary>
    private void UpdateCameraRotation()
    {
        if (playerCamera == null || playerSettings == null) return;
        if (inputManager == null || inputManager.CurrentMode != InputMode.Gameplay) return;
        
        // 마우스 입력 스무딩
        float smoothTime = playerSettings.cameraRotationSmoothing;
        smoothedLookInput.x = Mathf.SmoothDamp(
            smoothedLookInput.x, 
            lookInput.x, 
            ref currentLookVelocity.x, 
            smoothTime
        );
        smoothedLookInput.y = Mathf.SmoothDamp(
            smoothedLookInput.y, 
            lookInput.y, 
            ref currentLookVelocity.y, 
            smoothTime
        );
        
        // 마우스 입력으로 카메라 회전 (Time.deltaTime 적용)
        float sensitivity = playerSettings.mouseSensitivity * Time.deltaTime;
        
        // 수평 회전 (Y축 - Yaw) - 제한 없음 (360도)
        cameraRotationY += smoothedLookInput.x * sensitivity;
        
        // 수직 회전 (X축 - Pitch) - 각도 제한 적용
        float yInput = playerSettings.invertYAxis ? smoothedLookInput.y : -smoothedLookInput.y;
        cameraRotationX += yInput * sensitivity;
        
        // TPS 카메라 수직 각도 제한
        // 일반적으로 -40° (아래) ~ 80° (위) 정도
        cameraRotationX = Mathf.Clamp(
            cameraRotationX, 
            playerSettings.cameraMinVerticalAngle,  // 예: -40
            playerSettings.cameraMaxVerticalAngle   // 예: 80
        );
    }
    
    /// <summary>
    /// 카메라 위치 업데이트 (LateUpdate에서 실행)
    /// 충돌 처리 + 정수리 방지 포함
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (playerCamera == null || cameraFollowTarget == null || playerSettings == null) return;
        
        // 1. 카메라 거리 부드럽게 보간 (줌 적용)
        currentCameraDistance = Mathf.Lerp(
            currentCameraDistance, 
            targetCameraDistance, 
            Time.deltaTime * playerSettings.zoomSmoothing
        );
        
        // 2. 목표 카메라 회전 계산
        Quaternion cameraRotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0);
        
        // 3. 이상적인 카메라 위치 계산 (충돌 없을 때)
        Vector3 desiredCameraOffset = cameraRotation * Vector3.back * currentCameraDistance;
        Vector3 desiredCameraPosition = cameraFollowTarget.position + 
                                       Vector3.up * playerSettings.cameraHeight + 
                                       desiredCameraOffset;
        
        // 4. 카메라 충돌 처리
        Vector3 finalCameraPosition = HandleCameraCollision(
            cameraFollowTarget.position + Vector3.up * playerSettings.cameraHeight,
            desiredCameraPosition
        );
        
        // 5. 정수리 방지 - 최소 수평 거리 보장
        finalCameraPosition = EnforceMinimumHorizontalDistance(finalCameraPosition);
        
        // 6. 카메라 이동 (부드럽게)
        playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position, 
            finalCameraPosition, 
            Time.deltaTime * playerSettings.cameraSmoothness
        );
        
        // 7. 카메라가 타겟을 바라보도록 설정
        Vector3 lookAtTarget = cameraFollowTarget.position + 
                              Vector3.up * playerSettings.cameraLookAtHeight;
        playerCamera.transform.LookAt(lookAtTarget);
    }
    
    /// <summary>
    /// 카메라 충돌 처리 (벽 투과 방지)
    /// </summary>
    private Vector3 HandleCameraCollision(Vector3 from, Vector3 to)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        
        // SphereCast로 카메라 경로에 장애물이 있는지 확인
        if (Physics.SphereCast(
            from, 
            cameraCollisionRadius, 
            direction.normalized, 
            out RaycastHit hit, 
            distance, 
            cameraCollisionLayers))
        {
            // 충돌 지점에서 버퍼만큼 떨어진 위치로 카메라 이동
            float safeDistance = Mathf.Max(hit.distance - cameraCollisionBuffer, 0.1f);
            return from + direction.normalized * safeDistance;
        }
        
        // 충돌 없으면 원래 위치
        return to;
    }
    
    /// <summary>
    /// 카메라가 플레이어 정수리로 가지 않도록 최소 수평 거리 보장
    /// </summary>
    private Vector3 EnforceMinimumHorizontalDistance(Vector3 cameraPosition)
    {
        if (playerSettings == null || cameraFollowTarget == null) return cameraPosition;
        
        // 플레이어의 중심 위치 (Y축 포함)
        Vector3 playerCenter = cameraFollowTarget.position + Vector3.up * playerSettings.cameraHeight;
        
        // XZ 평면에서의 거리 계산 (수평 거리)
        Vector3 horizontalOffset = cameraPosition - playerCenter;
        horizontalOffset.y = 0; // Y축 제거하여 수평 거리만 계산
        
        float currentHorizontalDistance = horizontalOffset.magnitude;
        
        // 최소 수평 거리보다 작으면 강제로 밀어냄
        if (currentHorizontalDistance < playerSettings.minHorizontalDistance)
        {
            // 현재 방향 유지하면서 최소 거리까지 밀어냄
            if (currentHorizontalDistance > 0.01f)
            {
                // 방향 벡터를 정규화하고 최소 거리만큼 확장
                Vector3 pushDirection = horizontalOffset.normalized;
                Vector3 adjustedHorizontalOffset = pushDirection * playerSettings.minHorizontalDistance;
                
                // 새 카메라 위치 = 플레이어 중심 + 조정된 수평 오프셋 + 원래 높이 차이
                float heightDifference = cameraPosition.y - playerCenter.y;
                return playerCenter + adjustedHorizontalOffset + Vector3.up * heightDifference;
            }
            else
            {
                // 수평 거리가 거의 0이면 (정확히 정수리) 뒤쪽으로 밀어냄
                Vector3 fallbackDirection = -cameraFollowTarget.forward;
                Vector3 fallbackOffset = fallbackDirection * playerSettings.minHorizontalDistance;
                
                float heightDifference = cameraPosition.y - playerCenter.y;
                return playerCenter + fallbackOffset + Vector3.up * heightDifference;
            }
        }
        
        // 최소 거리 이상이면 그대로 반환
        return cameraPosition;
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
            // 초기 카메라 회전 설정 (플레이어 뒤쪽)
            cameraRotationY = transform.eulerAngles.y;
            cameraRotationX = 15f; // 약간 위에서 내려다보는 각도
            
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
    
    #region 디버그 (Gizmos)
    
    private void OnDrawGizmosSelected()
    {
        if (cameraFollowTarget == null || playerSettings == null) return;
        
        Vector3 centerPoint = cameraFollowTarget.position + Vector3.up * playerSettings.cameraHeight;
        
        // 최소 수평 거리 원 표시 (정수리 방지 영역)
        Gizmos.color = Color.yellow;
        DrawCircle(centerPoint, playerSettings.minHorizontalDistance, 32);
        
        // 카메라 충돌 검사 구체 표시
        Gizmos.color = Color.cyan;
        if (playerCamera != null)
        {
            Vector3 to = playerCamera.transform.position;
            Gizmos.DrawLine(centerPoint, to);
            Gizmos.DrawWireSphere(to, cameraCollisionRadius);
            
            // 현재 수평 거리 표시
            Vector3 horizontalOffset = to - centerPoint;
            horizontalOffset.y = 0;
            float currentHorizontalDist = horizontalOffset.magnitude;
            
            // 최소 거리보다 작으면 빨간색
            Gizmos.color = currentHorizontalDist < playerSettings.minHorizontalDistance 
                ? Color.red 
                : Color.green;
            Gizmos.DrawLine(
                new Vector3(centerPoint.x, to.y, centerPoint.z), 
                to
            );
        }
    }
    
    /// <summary>
    /// Gizmo용 원 그리기 헬퍼 메서드
    /// </summary>
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius, 
                0, 
                Mathf.Sin(angle) * radius
            );
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    #endregion
}