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
    public System.Action<int, int> OnExpChanged; // (currentExp, expToNext)
    
    protected override void InitializeComponents()
    {
        base.InitializeComponents();
        
        // 카메라 설정
        if (playerCamera == null)
            playerCamera = Camera.main;
        
        if (cameraFollowTarget == null)
            cameraFollowTarget = transform;
        
        // 컴포넌트 초기화
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
        
        // 플레이어 전용 초기화
        SetupCamera();
        InitializeInput();
        
        // 커서 잠금
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    protected override void UpdateCharacter()
    {
        HandleInput();
        UpdateMovement();
        UpdateCamera();
        UpdateAnimations();
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
        // 실제 프로젝트에서는 InputActionAsset을 사용
    }
    
    /// <summary>
    /// 입력 처리
    /// </summary>
    private void HandleInput()
    {
        // 이동 입력
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        moveInput = new Vector2(horizontal, vertical);
        
        // 마우스 입력
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        lookInput = new Vector2(mouseX, mouseY);
        
        // 액션 입력
        jumpInput = Input.GetButtonDown("Jump");
        runInput = Input.GetKey(KeyCode.LeftShift);
        rollInput = Input.GetKeyDown(KeyCode.LeftControl);
    }
    
    /// <summary>
    /// 이동 처리
    /// </summary>
    private void UpdateMovement()
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
            
            // 캐릭터 이동
            Move(moveDirection);
            
            // 캐릭터 회전
            if (moveDirection != Vector3.zero)
            {
                Rotate(moveDirection);
            }
        }
        
        // 달리기 설정
        SetRunning(runInput && moveInput.magnitude > 0.1f);
        
        // 점프
        if (jumpInput)
        {
            Jump();
        }
        
        // 구르기
        if (rollInput && moveInput.magnitude > 0.1f)
        {
            Vector3 rollDirection = transform.forward;
            if (moveInput.magnitude > 0.1f)
            {
                Vector3 inputDirection = playerCamera.transform.forward * moveInput.y + playerCamera.transform.right * moveInput.x;
                inputDirection.y = 0;
                rollDirection = inputDirection.normalized;
            }
            Roll(rollDirection);
        }
    }
    
    /// <summary>
    /// 카메라 업데이트
    /// </summary>
    private void UpdateCamera()
    {
        if (playerCamera == null) return;
        
        // 마우스 입력으로 카메라 회전
        cameraRotationY += lookInput.x * mouseSensitivity;
        cameraRotationX -= lookInput.y * mouseSensitivity;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -80f, 80f);
        
        // 카메라 위치 및 회전 계산
        Quaternion cameraRotation = Quaternion.Euler(cameraRotationX, cameraRotationY, 0);
        Vector3 cameraOffset = cameraRotation * Vector3.back * cameraDistance + Vector3.up * cameraHeight;
        Vector3 targetPosition = cameraFollowTarget.position + cameraOffset;
        
        // 카메라 이동 (부드럽게)
        playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, targetPosition, Time.deltaTime * 10f);
        playerCamera.transform.LookAt(cameraFollowTarget.position + Vector3.up * cameraHeight * 0.5f);
    }
    
    /// <summary>
    /// 애니메이션 업데이트
    /// </summary>
    private void UpdateAnimations()
    {
        if (characterAnimator == null) return;
        
        // 이동 속도 애니메이션
        float moveSpeed = moveInput.magnitude;
        SetAnimationFloat("MoveSpeed", moveSpeed);
        SetAnimationBool("IsGrounded", isGrounded);
    }
    
    /// <summary>
    /// 경험치 획득
    /// </summary>
    public void GainExp(int exp)
    {
        currentExp += exp;
        OnExpChanged?.Invoke(currentExp, expToNext);
        
        // 레벨업 체크
        while (currentExp >= expToNext)
        {
            LevelUp();
        }
    }
    
    /// <summary>
    /// 레벨업
    /// </summary>
    private void LevelUp()
    {
        currentExp -= expToNext;
        level++;
        expToNext = CalculateExpToNext();
        
        // 스탯 증가
        maxHP += 20f;
        maxMP += 10f;
        maxStamina += 15f;
        
        // 체력 완전 회복
        CurrentHP = maxHP;
        CurrentMP = maxMP;
        CurrentStamina = maxStamina;
        
        OnLevelUp?.Invoke(level);
        OnExpChanged?.Invoke(currentExp, expToNext);
        
        Debug.Log($"레벨업! 현재 레벨: {level}");
    }
    
    /// <summary>
    /// 다음 레벨까지 필요한 경험치 계산
    /// </summary>
    private int CalculateExpToNext()
    {
        return 100 + (level - 1) * 50; // 레벨이 오를수록 더 많은 경험치 필요
    }
    
    /// <summary>
    /// 마우스 감도 설정
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
    }
    
    /// <summary>
    /// 카메라 거리 설정
    /// </summary>
    public void SetCameraDistance(float distance)
    {
        cameraDistance = Mathf.Clamp(distance, 2f, 15f);
    }
    
    protected override void Die()
    {
        base.Die();
        
        // 플레이어 사망 처리
        Debug.Log("플레이어가 사망했습니다!");
        
        // 게임 오버 UI 표시 등의 로직
        // GameManager.Instance.ShowGameOver();
    }
    
    private void OnValidate()
    {
        // 인스펙터에서 값 변경 시 실시간 적용
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