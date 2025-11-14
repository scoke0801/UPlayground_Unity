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
    
    [Header("재화 시스템")]
    [SerializeField] private int gold = 0;
    
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
    public int Gold => gold;
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
    
    /// <summary>
    /// 경험치 획득 (NPC/Enemy 시스템용 호환 메서드)
    /// </summary>
    public void GainExperience(int experience)
    {
        GainExp(experience);
    }
    
    /// <summary>
    /// 골드 획득
    /// </summary>
    public void GainGold(int amount)
    {
        gold += amount;
        Debug.Log($"골드 {amount}개를 획득했습니다. 총 골드: {gold}");
    }
    
    /// <summary>
    /// 골드 사용
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            Debug.Log($"골드 {amount}개를 사용했습니다. 남은 골드: {gold}");
            return true;
        }
        
        Debug.Log("골드가 부족합니다.");
        return false;
    }
    
    /// <summary>
    /// 현재 골드 반환
    /// </summary>
    public int GetGold()
    {
        return gold;
    }
    
    /// <summary>
    /// 인벤토리에 아이템 추가
    /// </summary>
    public bool AddItemToInventory(string itemID, int quantity)
    {
        return inventory.AddItem(itemID, quantity);
    }
    
    /// <summary>
    /// 인벤토리에서 아이템 제거
    /// </summary>
    public bool RemoveItemFromInventory(string itemID, int quantity)
    {
        return inventory.RemoveItem(itemID, quantity);
    }
    
    /// <summary>
    /// 아이템 보유 여부 확인
    /// </summary>
    public bool HasItem(string itemID, int quantity)
    {
        return inventory.HasItem(itemID, quantity);
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
/// 플레이어 인벤토리 시스템
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("인벤토리 설정")]
    [SerializeField] private int maxSlots = 30;
    
    // 아이템 저장용 딕셔너리 (아이템 ID -> 수량)
    private System.Collections.Generic.Dictionary<string, int> items = 
        new System.Collections.Generic.Dictionary<string, int>();
    
    // 프로퍼티
    public int MaxSlots => maxSlots;
    public int UsedSlots => items.Count;
    public int AvailableSlots => maxSlots - UsedSlots;
    
    // 이벤트
    public System.Action<string, int> OnItemAdded;
    public System.Action<string, int> OnItemRemoved;
    public System.Action OnInventoryChanged;
    
    /// <summary>
    /// 아이템 추가
    /// </summary>
    public bool AddItem(string itemID, int quantity)
    {
        if (string.IsNullOrEmpty(itemID) || quantity <= 0)
            return false;
        
        // 기존 아이템이 있으면 수량 증가
        if (items.ContainsKey(itemID))
        {
            items[itemID] += quantity;
        }
        else
        {
            // 새 아이템 추가 (슬롯 확인)
            if (UsedSlots >= maxSlots)
            {
                Debug.Log("인벤토리가 가득 찼습니다.");
                return false;
            }
            
            items[itemID] = quantity;
        }
        
        OnItemAdded?.Invoke(itemID, quantity);
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"아이템 추가: {itemID} x{quantity}");
        return true;
    }
    
    /// <summary>
    /// 아이템 제거
    /// </summary>
    public bool RemoveItem(string itemID, int quantity)
    {
        if (string.IsNullOrEmpty(itemID) || quantity <= 0)
            return false;
        
        if (!items.ContainsKey(itemID))
        {
            Debug.Log($"아이템이 없습니다: {itemID}");
            return false;
        }
        
        int currentQuantity = items[itemID];
        if (currentQuantity < quantity)
        {
            Debug.Log($"아이템이 부족합니다: {itemID} (보유: {currentQuantity}, 필요: {quantity})");
            return false;
        }
        
        items[itemID] -= quantity;
        
        // 수량이 0이 되면 아이템 제거
        if (items[itemID] <= 0)
        {
            items.Remove(itemID);
        }
        
        OnItemRemoved?.Invoke(itemID, quantity);
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"아이템 제거: {itemID} x{quantity}");
        return true;
    }
    
    /// <summary>
    /// 아이템 보유 여부 확인
    /// </summary>
    public bool HasItem(string itemID, int quantity = 1)
    {
        if (!items.ContainsKey(itemID))
            return false;
        
        return items[itemID] >= quantity;
    }
    
    /// <summary>
    /// 아이템 수량 반환
    /// </summary>
    public int GetItemQuantity(string itemID)
    {
        return items.ContainsKey(itemID) ? items[itemID] : 0;
    }
    
    /// <summary>
    /// 모든 아이템 정보 반환
    /// </summary>
    public System.Collections.Generic.Dictionary<string, int> GetAllItems()
    {
        return new System.Collections.Generic.Dictionary<string, int>(items);
    }
    
    /// <summary>
    /// 인벤토리 초기화
    /// </summary>
    public void ClearInventory()
    {
        items.Clear();
        OnInventoryChanged?.Invoke();
        Debug.Log("인벤토리를 초기화했습니다.");
    }
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