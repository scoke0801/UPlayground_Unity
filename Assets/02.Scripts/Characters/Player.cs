using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 캐릭터 클래스
/// </summary>
public class Player : Character
{
    [Header("플레이어 설정")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraFollowTarget;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float cameraDistance = 5f;
    [SerializeField] private float cameraHeight = 2f;
    
    [Header("레벨 시스템")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int expToNext = 100;
    
    // 입력 관련
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpInput;
    private bool runInput;
    private bool rollInput;
    
    // 카메라 관련
    private float cameraRotationX = 0f;
    private float cameraRotationY = 0f;
    
    // 이동 관련 (FixedUpdate에서 사용)
    private Vector3 calculatedMoveDirection;
    private bool shouldJump;
    private bool shouldRoll;
    private Vector3 rollDirection;
    
    // 인벤토리 및 장비
    private PlayerInventory inventory;
    private PlayerEquipment equipment;
    
    // 프로퍼티
    public int Level => level;
    public int CurrentExp => currentExp;
    public int ExpToNext => expToNext;
    public PlayerInventory Inventory => inventory;
    public PlayerEquipment Equipment => equipment;
    
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
        
        inventory = GetComponent<PlayerInventory>();
        equipment = GetComponent<PlayerEquipment>();
        
        if (inventory == null)
            inventory = gameObject.AddComponent<PlayerInventory>();
        
        if (equipment == null)
            equipment = gameObject.AddComponent<PlayerEquipment>();
    }
    
    protected override void Initialize()
    {
        base.Initialize();
        SetupCamera();
        InitializeInput();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    // Update: 입력 처리 및 일반 게임 로직
    protected override void HandleInput()
    {
        // 이동 입력
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        moveInput = new Vector2(horizontal, vertical);
        
        // 마우스 입력 (카메라용)
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        lookInput = new Vector2(mouseX, mouseY);
        
        // 액션 입력
        jumpInput = Input.GetButtonDown("Jump");
        runInput = Input.GetKey(KeyCode.LeftShift);
        rollInput = Input.GetKeyDown(KeyCode.LeftControl);
    }
    
    protected override void UpdateGameLogic()
    {
        ProcessMovementInput();
        UpdateAnimations();
    }
    
    // FixedUpdate: 물리 기반 이동 처리
    protected override void HandlePhysicsMovement()
    {
        // 기본 이동 처리
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
    
    // LateUpdate: 카메라 업데이트
    protected override void HandleLateUpdate()
    {
        UpdateCameraRotation();
        UpdateCameraPosition();
    }
    
    /// <summary>
    /// 이동 입력을 물리 이동으로 변환
    /// </summary>
    private void ProcessMovementInput()
    {
        if (moveInput.magnitude > 0.1f)
        {
            // 카메라 기준 이동 방향 계산
            Vector3 cameraForward = playerCamera.transform.forward;
            Vector3 cameraRight = playerCamera.transform.right;
            
            cameraForward.y = 0;
            cameraRight.y = 0;
            
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
            
            // FixedUpdate에서 사용할 이동 방향 설정
            SetMovementInput(moveDirection);
            
            // 캐릭터 회전 방향 설정
            if (moveDirection != Vector3.zero)
            {
                SetRotationTarget(moveDirection);
            }
        }
        else
        {
            SetMovementInput(Vector3.zero);
        }
        
        // 달리기 설정
        SetRunning(runInput && moveInput.magnitude > 0.1f);
        
        // 점프 처리 (FixedUpdate에서 실행하도록 플래그 설정)
        if (jumpInput)
        {
            shouldJump = true;
        }
        
        // 구르기 처리 (FixedUpdate에서 실행하도록 플래그 설정)
        if (rollInput && moveInput.magnitude > 0.1f)
        {
            rollDirection = transform.forward;
            if (moveInput.magnitude > 0.1f)
            {
                Vector3 inputDirection = playerCamera.transform.forward * moveInput.y + playerCamera.transform.right * moveInput.x;
                inputDirection.y = 0;
                rollDirection = inputDirection.normalized;
            }
            shouldRoll = true;
        }
    }
    
    /// <summary>
    /// 카메라 회전 계산
    /// </summary>
    private void UpdateCameraRotation()
    {
        if (playerCamera == null) return;
        
        // 마우스 입력으로 카메라 회전
        cameraRotationY += lookInput.x * mouseSensitivity;
        cameraRotationX -= lookInput.y * mouseSensitivity;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -80f, 80f);
    }
    
    /// <summary>
    /// 카메라 위치 업데이트 (LateUpdate에서 실행)
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (playerCamera == null || cameraFollowTarget == null) return;
        
        // 카메라 위치 및 회전 계산
        Quaternion cameraRotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0);
        Vector3 cameraOffset = cameraRotation * Vector3.back * cameraDistance + Vector3.up * cameraHeight;
        Vector3 targetPosition = cameraFollowTarget.position + cameraOffset;
        
        // 카메라 이동 (부드럽게)
        playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position, 
            targetPosition, 
            Time.deltaTime * 10f
        );
        
        playerCamera.transform.LookAt(
            cameraFollowTarget.position + Vector3.up * cameraHeight * 0.5f
        );
    }
    
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
        if (playerCamera != null)
        {
            Vector3 cameraPos = cameraFollowTarget.position - cameraFollowTarget.forward * cameraDistance + Vector3.up * cameraHeight;
            playerCamera.transform.position = cameraPos;
            playerCamera.transform.LookAt(cameraFollowTarget);
        }
    }
    
    /// <summary>
    /// 입력 시스템 초기화
    /// </summary>
    private void InitializeInput()
    {
        // InputSystem 액션맵 설정은 여기서 구현
    }
    
    // 기존 메서드들...
    public void GainExp(int exp)
    {
        currentExp += exp;
        OnExpChanged?.Invoke(currentExp, expToNext);
        
        while (currentExp >= expToNext)
        {
            LevelUp();
        }
    }
    
    private void LevelUp()
    {
        currentExp -= expToNext;
        level++;
        expToNext = CalculateExpToNext();
        
        maxHP += 20f;
        maxMP += 10f;
        maxStamina += 15f;
        
        CurrentHP = maxHP;
        CurrentMP = maxMP;
        CurrentStamina = maxStamina;
        
        OnLevelUp?.Invoke(level);
        OnExpChanged?.Invoke(currentExp, expToNext);
        
        Debug.Log($"레벨업! 현재 레벨: {level}");
    }
    
    private int CalculateExpToNext()
    {
        return 100 + (level - 1) * 50;
    }
    
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
    }
    
    public void SetCameraDistance(float distance)
    {
        cameraDistance = Mathf.Clamp(distance, 2f, 15f);
    }
    
    protected override void Die()
    {
        base.Die();
        Debug.Log("플레이어가 사망했습니다!");
    }
    
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            mouseSensitivity = Mathf.Clamp(mouseSensitivity, 0.1f, 10f);
            cameraDistance = Mathf.Clamp(cameraDistance, 2f, 15f);
        }
    }
}

/// <summary>
/// 플레이어 인벤토리 (간단한 구현)
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int maxSlots = 30;
    
    // 실제 프로젝트에서는 아이템 시스템과 연동
    public int MaxSlots => maxSlots;
}

/// <summary>
/// 플레이어 장비 (간단한 구현)
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    [Header("장비 슬롯")]
    [SerializeField] private Transform weaponSlot;
    [SerializeField] private Transform shieldSlot;
    
    // 실제 프로젝트에서는 장비 아이템 시스템과 연동
    public Transform WeaponSlot => weaponSlot;
    public Transform ShieldSlot => shieldSlot;
}