using UnityEngine;

/// <summary>
/// 플레이어 캐릭터 클래스
/// 설정 데이터를 ScriptableObject로 관리하여 코드 간결화
/// </summary>
public class Player : Character
{
    [Header("플레이어 데이터")]
    [SerializeField] private PlayerSettingsData playerSettings;
    
    [Header("컴포넌트 참조")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraFollowTarget;
    
    // 레벨 시스템
    private int level;
    private int currentExp;
    private int expToNext;
    
    // 재화
    private int gold;
    
    // 입력
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpInput;
    private bool runInput;
    private bool rollInput;
    
    // 카메라
    private float cameraRotationX = 0f;
    private float cameraRotationY = 0f;
    
    // 물리 처리용 플래그
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
        
        // 데이터 유효성 검사
        if (playerSettings == null)
        {
            Debug.LogError($"{gameObject.name}: PlayerSettingsData가 할당되지 않았습니다!");
        }
    }
    
    protected override void Initialize()
    {
        base.Initialize();
        
        // 플레이어 설정 초기화
        if (playerSettings != null)
        {
            level = playerSettings.startLevel;
            expToNext = CalculateExpToNext();
            gold = playerSettings.startGold;
        }
        
        SetupCamera();
        InitializeInput();
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    protected override void HandleInput()
    {
        // 이동 입력
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        moveInput = new Vector2(horizontal, vertical);
        
        // 마우스 입력 (카메라)
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
        
        // 달리기 설정
        SetRunning(runInput && moveInput.magnitude > 0.1f);
        
        // 점프 플래그
        if (jumpInput)
        {
            shouldJump = true;
        }
        
        // 구르기 플래그
        if (rollInput && moveInput.magnitude > 0.1f)
        {
            rollDirection = transform.forward;
            if (moveInput.magnitude > 0.1f)
            {
                Vector3 inputDirection = playerCamera.transform.forward * moveInput.y + 
                                        playerCamera.transform.right * moveInput.x;
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
        if (playerCamera == null || playerSettings == null) return;
        
        // 마우스 입력으로 카메라 회전
        cameraRotationY += lookInput.x * playerSettings.mouseSensitivity;
        cameraRotationX -= lookInput.y * playerSettings.mouseSensitivity;
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
        }
    }
    
    /// <summary>
    /// 입력 시스템 초기화
    /// </summary>
    private void InitializeInput()
    {
        // InputSystem 액션맵 설정
    }
    
    #region 레벨링 시스템
    
    /// <summary>
    /// 경험치 획득
    /// </summary>
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
    /// 경험치 획득 (호환성 메서드)
    /// </summary>
    public void GainExperience(int experience)
    {
        GainExp(experience);
    }
    
    /// <summary>
    /// 레벨업 처리
    /// </summary>
    private void LevelUp()
    {
        if (playerSettings == null) return;
        
        currentExp -= expToNext;
        level++;
        expToNext = CalculateExpToNext();
        
        // 스탯 증가
        MaxHP += playerSettings.hpIncreasePerLevel;
        MaxMP += playerSettings.mpIncreasePerLevel;
        MaxStamina += playerSettings.staminaIncreasePerLevel;
        
        // 현재 스탯 회복
        CurrentHP = MaxHP;
        CurrentMP = MaxMP;
        CurrentStamina = MaxStamina;
        
        OnLevelUp?.Invoke(level);
        OnExpChanged?.Invoke(currentExp, expToNext);
        
        Debug.Log($"레벨업! 현재 레벨: {level}");
    }
    
    /// <summary>
    /// 다음 레벨 경험치 계산
    /// </summary>
    private int CalculateExpToNext()
    {
        if (playerSettings == null) 
            return 100;
        
        return playerSettings.baseExpRequired + (level - 1) * playerSettings.expIncreasePerLevel;
    }
    
    #endregion
    
    #region 재화 시스템
    
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
    
    #endregion
    
    #region 인벤토리 시스템
    
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
    
    #endregion
    
    #region 설정 조정
    
    /// <summary>
    /// 마우스 감도 설정
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        if (playerSettings != null)
        {
            playerSettings.mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
        }
    }
    
    /// <summary>
    /// 카메라 거리 설정
    /// </summary>
    public void SetCameraDistance(float distance)
    {
        if (playerSettings != null)
        {
            playerSettings.cameraDistance = Mathf.Clamp(distance, 2f, 15f);
        }
    }
    
    #endregion
    
    protected override void Die()
    {
        base.Die();
        Debug.Log("플레이어가 사망했습니다!");
    }
}

/// <summary>
/// 플레이어 인벤토리 시스템
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Header("인벤토리 설정")]
    [SerializeField] private int maxSlots = 30;
    
    private System.Collections.Generic.Dictionary<string, int> items = 
        new System.Collections.Generic.Dictionary<string, int>();
    
    public int MaxSlots => maxSlots;
    public int UsedSlots => items.Count;
    public int AvailableSlots => maxSlots - UsedSlots;
    
    public System.Action<string, int> OnItemAdded;
    public System.Action<string, int> OnItemRemoved;
    public System.Action OnInventoryChanged;
    
    public bool AddItem(string itemID, int quantity)
    {
        if (string.IsNullOrEmpty(itemID) || quantity <= 0)
            return false;
        
        if (items.ContainsKey(itemID))
        {
            items[itemID] += quantity;
        }
        else
        {
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
        
        if (items[itemID] <= 0)
        {
            items.Remove(itemID);
        }
        
        OnItemRemoved?.Invoke(itemID, quantity);
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"아이템 제거: {itemID} x{quantity}");
        return true;
    }
    
    public bool HasItem(string itemID, int quantity = 1)
    {
        if (!items.ContainsKey(itemID))
            return false;
        
        return items[itemID] >= quantity;
    }
    
    public int GetItemQuantity(string itemID)
    {
        return items.ContainsKey(itemID) ? items[itemID] : 0;
    }
    
    public System.Collections.Generic.Dictionary<string, int> GetAllItems()
    {
        return new System.Collections.Generic.Dictionary<string, int>(items);
    }
    
    public void ClearInventory()
    {
        items.Clear();
        OnInventoryChanged?.Invoke();
        Debug.Log("인벤토리를 초기화했습니다.");
    }
}

/// <summary>
/// 플레이어 장비
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    [Header("장비 슬롯")]
    [SerializeField] private Transform weaponSlot;
    [SerializeField] private Transform shieldSlot;
    
    public Transform WeaponSlot => weaponSlot;
    public Transform ShieldSlot => shieldSlot;
}
