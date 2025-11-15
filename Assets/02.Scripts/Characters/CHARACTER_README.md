# 캐릭터 시스템 가이드

TPS 액션 RPG의 캐릭터 시스템 완벽 가이드입니다.

---

## 📋 목차

1. [시스템 개요](#-시스템-개요)
2. [계층 구조](#-계층-구조)
3. [BaseActor 클래스](#-baseactor-클래스)
4. [Character 클래스](#-character-클래스)
5. [Player 클래스](#-player-클래스)
6. [NPC 클래스](#-npc-클래스)
7. [Enemy 클래스](#-enemy-클래스)
8. [데이터 구조](#-데이터-구조)
9. [사용 예제](#-사용-예제)
10. [확장 가이드](#-확장-가이드)

---

## 🎯 시스템 개요

캐릭터 시스템은 **데이터 중심 설계**를 기반으로 하며, 모든 캐릭터는 계층적 구조를 통해 기능을 상속받습니다.

### 핵심 원칙

- ✅ **데이터와 로직 분리** - ScriptableObject로 설정 관리
- ✅ **재사용성** - 공통 기능은 상위 클래스에서 구현
- ✅ **확장성** - 새로운 캐릭터 타입 추가 용이
- ✅ **명확성** - 각 클래스의 역할이 명확하게 정의됨

---

## 🏗️ 계층 구조

```
BaseActor (모든 액터의 기본)
    ↓
Character (캐릭터 공통 기능)
    ├─→ Player (플레이어 캐릭터)
    ├─→ NPC (비플레이어 캐릭터)
    └─→ Enemy (적 캐릭터)
```

### 각 레벨의 역할

| 클래스 | 역할 | 주요 기능 |
|--------|------|----------|
| **BaseActor** | 기본 액터 | 이동, 회전, 충돌, 생명주기 |
| **Character** | 캐릭터 | HP/MP/스태미나, 점프, 구르기, 상태 이상 |
| **Player** | 플레이어 | 입력 처리, 카메라, 인벤토리, 레벨 |
| **NPC** | NPC | AI, 대화, 퀘스트, 상점 |
| **Enemy** | 적 | AI 전투, 공격 패턴, 드롭 |

---

## 🔷 BaseActor 클래스

모든 게임 액터의 최상위 클래스입니다.

### 주요 기능

- ✅ Transform 관리 (위치, 회전, 크기)
- ✅ 이동 및 회전 처리
- ✅ 충돌 검사
- ✅ Unity 생명주기 관리

### 핵심 속성

```csharp
[Header("기본 속성")]
protected string actorName = "Unknown";
protected bool isActive = true;

[Header("Transform 속성")]
protected float moveSpeed = 5f;
protected float rotationSpeed = 360f;

[Header("충돌 속성")]
protected LayerMask collisionLayers = 1;
protected float collisionRadius = 1f;
```

### 주요 메서드

#### 이동 제어

```csharp
// 이동 방향 설정
public virtual void SetMovementInput(Vector3 direction)

// 회전 타겟 설정
public virtual void SetRotationTarget(Vector3 targetDirection)
```

#### 충돌 및 거리 계산

```csharp
// 충돌 체크
public virtual bool CheckCollision(Vector3 position, float radius = -1f)

// 다른 액터와의 거리
public float GetDistanceTo(BaseActor other)

// 다른 액터로의 방향
public Vector3 GetDirectionTo(BaseActor other)
```

#### 활성화 제어

```csharp
// 액터 활성화/비활성화
public virtual void SetActive(bool active)

// 액터 제거
public virtual void DestroyActor()
```

### Unity 생명주기

```csharp
protected virtual void Awake()          // 컴포넌트 초기화
protected virtual void Start()          // 게임 로직 초기화
protected virtual void Update()         // 입력 처리, 일반 로직 (60fps)
protected virtual void FixedUpdate()    // 물리 이동 처리 (50fps)
protected virtual void LateUpdate()     // 카메라, 후처리 (Update 이후)
```

### 이벤트

```csharp
public System.Action<BaseActor> OnActorDestroyed;
public System.Action<BaseActor, bool> OnActiveStateChanged;
```

### 사용 예제

```csharp
public class CustomActor : BaseActor
{
    protected override void Initialize()
    {
        actorName = "My Custom Actor";
        moveSpeed = 10f;
    }
    
    protected override void HandleInput()
    {
        // 입력 처리
        Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        SetMovementInput(movement);
    }
}
```

---

## 💪 Character 클래스

모든 캐릭터(플레이어, NPC, 적)의 기본 클래스입니다.

### 주요 기능

- ✅ **스탯 시스템** - HP, MP, 스태미나 관리
- ✅ **액션 시스템** - 점프, 구르기, 달리기
- ✅ **재생 시스템** - 자동 회복
- ✅ **상태 이상** - 버프/디버프 효과
- ✅ **애니메이션** - Animator 연동

### 스탯 구조

```csharp
[Header("캐릭터 데이터")]
[SerializeField] protected CharacterStatsData statsData;
[SerializeField] protected MovementSettingsData movementData;

// 현재 스탯 (런타임)
protected float currentHP;
protected float currentMP;
protected float currentStamina;

// 프로퍼티 - 자동으로 0~Max 범위 제한
public float CurrentHP { get; protected set; }
public float CurrentMP { get; protected set; }
public float CurrentStamina { get; protected set; }

// 최대 스탯 (상속 클래스에서 수정 가능)
public virtual float MaxHP { get; protected set; }
public virtual float MaxMP { get; protected set; }
public virtual float MaxStamina { get; protected set; }
```

### 상태 프로퍼티

```csharp
public bool IsAlive => isAlive;         // 생존 여부
public bool IsRunning => isRunning;     // 달리기 중
public bool IsGrounded => isGrounded;   // 지면 접촉
public bool IsRolling => isRolling;     // 구르기 중
```

### 핵심 메서드

#### 전투 관련

```csharp
// 데미지 받기
public virtual void TakeDamage(float damage)
{
    if (!isAlive) return;
    
    CurrentHP -= damage;
    OnHPChanged?.Invoke(this, currentHP, MaxHP);
    
    if (currentHP <= 0)
        Die();
}

// 체력 회복
public virtual void Heal(float amount)
{
    CurrentHP += amount;
    OnHPChanged?.Invoke(this, currentHP, MaxHP);
}

// 마나 사용
public virtual bool UseMP(float amount)
{
    if (currentMP < amount) return false;
    
    CurrentMP -= amount;
    OnMPChanged?.Invoke(this, currentMP, MaxMP);
    return true;
}

// 스태미나 사용
public virtual bool UseStamina(float amount)
{
    if (currentStamina < amount) return false;
    
    CurrentStamina -= amount;
    return true;
}
```

#### 액션 시스템

```csharp
// 점프 (FixedUpdate에서 호출 권장)
public virtual void Jump()
{
    if (!isGrounded || !UseStamina(statsData.jumpStaminaCost)) 
        return;
    
    actorRigidbody.AddForce(Vector3.up * movementData.jumpPower, ForceMode.Impulse);
    SetAnimationTrigger("Jump");
}

// 구르기
public virtual void Roll(Vector3 direction)
{
    if (!isGrounded || isRolling || !UseStamina(statsData.rollStaminaCost)) 
        return;
    
    StartCoroutine(PerformRoll(direction));
}

// 달리기 설정
public virtual void SetRunning(bool running)
{
    if (running && currentStamina <= 0)
        running = false;
    
    isRunning = running;
    moveSpeed = running ? movementData.runSpeed : movementData.walkSpeed;
    
    if (running)
        UseStamina(statsData.runStaminaCostPerSecond * Time.deltaTime);
    
    SetAnimationBool("IsRunning", running);
}
```

#### 상태 이상 시스템

```csharp
// 상태 이상 추가
public virtual void AddStatusEffect(StatusEffect effect)
{
    statusEffects.Add(effect);
    effect.OnApply(this);
    OnStatusEffectAdded?.Invoke(this, effect);
}
```

### 자동 회복 시스템

```csharp
protected virtual void UpdateRegeneration()
{
    if (currentHP < MaxHP)
        CurrentHP += statsData.hpRegenRate * Time.deltaTime;
    
    if (currentMP < MaxMP)
        CurrentMP += statsData.mpRegenRate * Time.deltaTime;
    
    if (currentStamina < MaxStamina && !isRunning)
        CurrentStamina += statsData.staminaRegenRate * Time.deltaTime;
}
```

### 이벤트

```csharp
public System.Action<Character, float, float> OnHPChanged;
public System.Action<Character, float, float> OnMPChanged;
public System.Action<Character> OnCharacterDied;
public System.Action<Character, StatusEffect> OnStatusEffectAdded;
```

### 애니메이션 제어

```csharp
protected virtual void SetAnimationTrigger(string triggerName)
{
    if (characterAnimator != null)
        characterAnimator.SetTrigger(triggerName);
}

protected virtual void SetAnimationBool(string paramName, bool value)
{
    if (characterAnimator != null)
        characterAnimator.SetBool(paramName, value);
}

protected virtual void SetAnimationFloat(string paramName, float value)
{
    if (characterAnimator != null)
        characterAnimator.SetFloat(paramName, value);
}
```

### 사용 예제

```csharp
public class Warrior : Character
{
    protected override void Initialize()
    {
        base.Initialize();
        Debug.Log($"전사 생성: {actorName}");
    }
    
    public void SpecialAttack()
    {
        if (UseMP(20f))
        {
            Debug.Log("특수 공격 시전!");
            SetAnimationTrigger("SpecialAttack");
        }
    }
}
```

---

## 🎮 Player 클래스

플레이어 캐릭터를 위한 클래스입니다.

### 주요 기능

- ✅ **입력 처리** - 키보드/마우스/게임패드
- ✅ **TPS 카메라** - 3인칭 카메라 컨트롤
- ✅ **인벤토리 시스템**
- ✅ **레벨/경험치 시스템**
- ✅ **스킬 시스템**

### 핵심 속성

```csharp
[Header("플레이어 설정")]
[SerializeField] private PlayerSettingsData playerSettings;

[Header("카메라")]
[SerializeField] private Transform cameraTransform;
[SerializeField] private Transform cameraPivot;

[Header("레벨 시스템")]
private int currentLevel = 1;
private float currentExperience = 0f;
private float experienceToNextLevel = 100f;

// 카메라 제어
private float currentCameraDistance;
private float targetCameraDistance;
private float currentVerticalAngle = 0f;
private Vector2 mouseInput;
```

### 입력 처리

```csharp
protected override void HandleInput()
{
    if (!isAlive) return;
    
    // 이동 입력
    float horizontal = Input.GetAxis("Horizontal");
    float vertical = Input.GetAxis("Vertical");
    Vector2 moveInput = new Vector2(horizontal, vertical);
    
    // 카메라 입력
    float mouseX = Input.GetAxis("Mouse X");
    float mouseY = Input.GetAxis("Mouse Y");
    mouseInput = new Vector2(mouseX, mouseY);
    
    // 점프
    if (Input.GetButtonDown("Jump"))
        Jump();
    
    // 구르기
    if (Input.GetKeyDown(KeyCode.LeftShift))
        Roll(GetMovementDirection());
    
    // 달리기
    bool isRunPressed = Input.GetKey(KeyCode.LeftControl);
    SetRunning(isRunPressed);
}
```

### 카메라 시스템

#### 카메라 설정

```csharp
private void SetupCamera()
{
    if (cameraTransform == null)
    {
        GameObject cameraObj = new GameObject("PlayerCamera");
        cameraTransform = cameraObj.transform;
        
        Camera cam = cameraObj.AddComponent<Camera>();
        cameraObj.AddComponent<AudioListener>();
    }
    
    currentCameraDistance = playerSettings.cameraDistance;
    targetCameraDistance = currentCameraDistance;
}
```

#### 카메라 업데이트 (LateUpdate)

```csharp
protected override void HandleLateUpdate()
{
    if (!isAlive) return;
    
    UpdateCameraRotation();
    UpdateCameraZoom();
    UpdateCameraPosition();
}
```

#### 카메라 회전

```csharp
private void UpdateCameraRotation()
{
    // 수평 회전 (플레이어 회전)
    transform.Rotate(Vector3.up, mouseInput.x * playerSettings.mouseSensitivity);
    
    // 수직 회전 (카메라 피벗)
    float yRotation = playerSettings.invertYAxis ? mouseInput.y : -mouseInput.y;
    currentVerticalAngle += yRotation * playerSettings.mouseSensitivity;
    
    // 각도 제한
    currentVerticalAngle = Mathf.Clamp(
        currentVerticalAngle,
        playerSettings.cameraMinVerticalAngle,
        playerSettings.cameraMaxVerticalAngle
    );
}
```

#### 카메라 줌

```csharp
private void UpdateCameraZoom()
{
    float scrollInput = Input.GetAxis("Mouse ScrollWheel");
    
    if (Mathf.Abs(scrollInput) > 0.01f)
    {
        targetCameraDistance -= scrollInput * playerSettings.zoomSpeed;
        targetCameraDistance = Mathf.Clamp(
            targetCameraDistance,
            playerSettings.cameraMinDistance,
            playerSettings.cameraMaxDistance
        );
    }
    
    // 부드러운 줌
    currentCameraDistance = Mathf.Lerp(
        currentCameraDistance,
        targetCameraDistance,
        Time.deltaTime * playerSettings.zoomSmoothing
    );
}
```

### 레벨 시스템

```csharp
// 경험치 획득
public void GainExperience(float amount)
{
    currentExperience += amount;
    
    while (currentExperience >= experienceToNextLevel)
    {
        LevelUp();
    }
    
    OnExperienceGained?.Invoke(this, currentExperience, experienceToNextLevel);
}

// 레벨업
private void LevelUp()
{
    currentLevel++;
    currentExperience -= experienceToNextLevel;
    experienceToNextLevel *= 1.5f;
    
    // 스탯 증가
    MaxHP += 10f;
    MaxMP += 5f;
    
    // 체력/마나 완전 회복
    CurrentHP = MaxHP;
    CurrentMP = MaxMP;
    
    OnLevelUp?.Invoke(this, currentLevel);
}
```

### 이벤트

```csharp
public System.Action<Player, float, float> OnExperienceGained;
public System.Action<Player, int> OnLevelUp;
```

### 사용 예제

```csharp
// 플레이어 생성 및 설정
Player player = playerObject.GetComponent<Player>();

// 이벤트 구독
player.OnHPChanged += (character, current, max) => {
    Debug.Log($"HP: {current}/{max}");
};

player.OnLevelUp += (p, level) => {
    Debug.Log($"레벨업! 현재 레벨: {level}");
};

// 경험치 지급
player.GainExperience(50f);
```

---

## 🤝 NPC 클래스

비플레이어 캐릭터를 위한 클래스입니다.

### 주요 기능

- ✅ **AI 행동 패턴** - Idle, Walking, Interacting
- ✅ **대화 시스템**
- ✅ **퀘스트 시스템**
- ✅ **상점 시스템**
- ✅ **자동 순찰**

### NPC 타입

```csharp
public enum NPCType
{
    Normal,      // 일반 NPC (대화만)
    QuestGiver,  // 퀘스트 제공자
    Merchant     // 상인
}
```

### AI 상태

```csharp
public enum NPCState
{
    Idle,        // 대기
    Walking,     // 걷기
    Interacting  // 상호작용 중
}
```

### 핵심 속성

```csharp
[Header("NPC 데이터")]
[SerializeField] private NPCSettingsData npcSettings;

[Header("NPC 설정")]
[SerializeField] private string npcName = "NPC";

// AI 상태
private NPCState currentState = NPCState.Idle;
private Vector3 startPosition;
private Vector3 currentTarget;
private float stateTimer = 0f;

// 퀘스트 시스템 (런타임)
private List<Quest> runtimeAvailableQuests;
private List<Quest> completableQuests;

// 상점 시스템 (런타임)
private List<ShopItem> runtimeShopItems;
```

### AI 시스템

#### 상태 업데이트

```csharp
private void UpdateAI()
{
    stateTimer += Time.deltaTime;
    
    switch (currentState)
    {
        case NPCState.Idle:
            HandleIdleState();
            break;
        case NPCState.Walking:
            HandleWalkingState();
            break;
        case NPCState.Interacting:
            HandleInteractingState();
            break;
    }
}
```

#### Idle 상태

```csharp
private void HandleIdleState()
{
    // 일정 시간 대기 후 걷기 시작
    float waitTime = Random.Range(npcSettings.idleTimeMin, npcSettings.idleTimeMax);
    
    if (stateTimer >= waitTime)
    {
        SetRandomTarget();
        ChangeState(NPCState.Walking);
    }
}
```

#### Walking 상태

```csharp
private void HandleWalkingState()
{
    Vector3 direction = (currentTarget - transform.position).normalized;
    direction.y = 0;
    
    // 목표 지점 도달 시
    if (Vector3.Distance(transform.position, currentTarget) < 0.5f)
    {
        ChangeState(NPCState.Idle);
        return;
    }
    
    // 이동
    SetMovementInput(direction);
    SetRotationTarget(direction);
}
```

#### 순찰 시스템

```csharp
private void SetRandomTarget()
{
    // 시작 지점 주변 랜덤 위치
    Vector2 randomPoint = Random.insideUnitCircle * npcSettings.patrolRange;
    currentTarget = startPosition + new Vector3(randomPoint.x, 0, randomPoint.y);
}
```

### 상호작용 시스템

#### 기본 상호작용

```csharp
public void Interact()
{
    if (!CanInteract) return;
    
    ChangeState(NPCState.Interacting);
    
    switch (npcSettings.npcType)
    {
        case NPCType.Normal:
            StartDialogue();
            break;
        case NPCType.QuestGiver:
            HandleQuestInteraction();
            break;
        case NPCType.Merchant:
            OpenShop();
            break;
    }
}
```

#### 대화 시스템

```csharp
private void StartDialogue()
{
    if (npcSettings.dialogues == null || npcSettings.dialogues.Count == 0)
    {
        OnDialogue?.Invoke(this, "안녕하세요!");
        return;
    }
    
    string dialogue = npcSettings.dialogues[currentDialogueIndex];
    OnDialogue?.Invoke(this, dialogue);
    
    // 다음 대화로 순환
    currentDialogueIndex = (currentDialogueIndex + 1) % npcSettings.dialogues.Count;
}
```

#### 퀘스트 시스템

```csharp
private void HandleQuestInteraction()
{
    // 완료 가능한 퀘스트 우선
    if (completableQuests.Count > 0)
    {
        Quest quest = completableQuests[0];
        CompleteQuest(quest);
        return;
    }
    
    // 제공 가능한 퀘스트
    if (runtimeAvailableQuests.Count > 0)
    {
        Quest quest = runtimeAvailableQuests[0];
        OfferQuest(quest);
        return;
    }
    
    // 퀘스트 없으면 일반 대화
    StartDialogue();
}

// 퀘스트 제공
private void OfferQuest(Quest quest)
{
    OnQuestOffered?.Invoke(this, quest);
    runtimeAvailableQuests.Remove(quest);
}

// 퀘스트 완료
private void CompleteQuest(Quest quest)
{
    OnQuestCompleted?.Invoke(this, quest);
    completableQuests.Remove(quest);
}
```

#### 상점 시스템

```csharp
private void OpenShop()
{
    OnShopOpened?.Invoke(this);
}

// 아이템 구매
public bool BuyItem(ShopItem item, Player buyer)
{
    // 구현 필요
    return false;
}

// 아이템 판매
public bool SellItem(string itemID, int quantity, Player seller)
{
    // 구현 필요
    return false;
}
```

### 이벤트

```csharp
public System.Action<NPC, string> OnDialogue;
public System.Action<NPC, Quest> OnQuestOffered;
public System.Action<NPC, Quest> OnQuestCompleted;
public System.Action<NPC> OnShopOpened;
```

### 사용 예제

```csharp
// NPC 설정
NPC npc = npcObject.GetComponent<NPC>();

// 이벤트 구독
npc.OnDialogue += (npc, dialogue) => {
    Debug.Log($"{npc.NPCName}: {dialogue}");
};

npc.OnQuestOffered += (npc, quest) => {
    Debug.Log($"퀘스트 제공: {quest.QuestName}");
};

// 플레이어가 상호작용
if (Input.GetKeyDown(KeyCode.E) && npc.CanInteract)
{
    npc.Interact();
}
```

---

## ⚔️ Enemy 클래스

적 캐릭터를 위한 클래스입니다.

### 주요 기능

- ✅ **AI 전투 시스템**
- ✅ **공격 패턴 관리**
- ✅ **드롭 아이템**
- ✅ **경험치 제공**
- ✅ **순찰 및 추적**

### 구현 예제

```csharp
public class Enemy : Character
{
    [Header("적 설정")]
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 2f;
    
    [Header("보상")]
    [SerializeField] private int experienceReward = 50;
    [SerializeField] private List<string> dropItems;
    
    private Transform target;
    private EnemyState currentState = EnemyState.Idle;
    private float attackTimer = 0f;
    
    protected override void Initialize()
    {
        base.Initialize();
        FindTarget();
    }
    
    protected override void UpdateGameLogic()
    {
        base.UpdateGameLogic();
        
        if (!isAlive) return;
        
        UpdateAI();
    }
    
    private void UpdateAI()
    {
        if (target == null)
        {
            FindTarget();
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // 상태 전환
        if (distanceToTarget > detectionRange)
        {
            ChangeState(EnemyState.Idle);
        }
        else if (distanceToTarget > attackRange)
        {
            ChangeState(EnemyState.Chasing);
        }
        else
        {
            ChangeState(EnemyState.Attacking);
        }
        
        // 상태별 행동
        switch (currentState)
        {
            case EnemyState.Idle:
                Patrol();
                break;
            case EnemyState.Chasing:
                ChaseTarget();
                break;
            case EnemyState.Attacking:
                AttackTarget();
                break;
        }
    }
    
    private void ChaseTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0;
        
        SetMovementInput(direction);
        SetRotationTarget(direction);
    }
    
    private void AttackTarget()
    {
        SetMovementInput(Vector3.zero);
        
        // 타겟을 향해 회전
        Vector3 lookDirection = (target.position - transform.position).normalized;
        lookDirection.y = 0;
        SetRotationTarget(lookDirection);
        
        // 공격 쿨다운 체크
        attackTimer += Time.deltaTime;
        if (attackTimer >= attackCooldown)
        {
            PerformAttack();
            attackTimer = 0f;
        }
    }
    
    private void PerformAttack()
    {
        SetAnimationTrigger("Attack");
        
        // 타겟에게 데미지
        Character targetCharacter = target.GetComponent<Character>();
        if (targetCharacter != null)
        {
            targetCharacter.TakeDamage(10f);
        }
    }
    
    protected override void Die()
    {
        base.Die();
        
        // 경험치 지급
        Player player = target?.GetComponent<Player>();
        if (player != null)
        {
            player.GainExperience(experienceReward);
        }
        
        // 아이템 드롭
        DropItems();
        
        // 일정 시간 후 제거
        Destroy(gameObject, 3f);
    }
    
    private void DropItems()
    {
        foreach (string itemID in dropItems)
        {
            // 아이템 드롭 구현
            Debug.Log($"아이템 드롭: {itemID}");
        }
    }
}

public enum EnemyState
{
    Idle,
    Chasing,
    Attacking
}

public enum EnemyType
{
    Melee,      // 근접
    Ranged,     // 원거리
    Boss        // 보스
}
```

---

## 📊 데이터 구조

### CharacterStatsData (ScriptableObject)

캐릭터의 기본 스탯을 정의합니다.

```csharp
[CreateAssetMenu(fileName = "New Character Stats", menuName = "RPG/Character/Stats Data")]
public class CharacterStatsData : ScriptableObject
{
    [Header("체력")]
    public float maxHP = 100f;
    public float hpRegenRate = 5f;
    
    [Header("마나")]
    public float maxMP = 50f;
    public float mpRegenRate = 10f;
    
    [Header("스태미나")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 20f;
    
    [Header("스태미나 소모")]
    public float jumpStaminaCost = 20f;
    public float rollStaminaCost = 30f;
    public float runStaminaCostPerSecond = 10f;
}
```

**생성 방법:**
```
Project 우클릭 → Create → RPG → Character → Stats Data
```

**사용 예제:**
```csharp
[SerializeField] private CharacterStatsData warriorStats;
[SerializeField] private CharacterStatsData mageStats;

// Character 클래스에 할당
character.statsData = warriorStats;
```

### MovementSettingsData (ScriptableObject)

이동 및 물리 설정을 정의합니다.

```csharp
[CreateAssetMenu(fileName = "MovementSettings", menuName = "RPG/Character/Movement Settings")]
public class MovementSettingsData : ScriptableObject
{
    [Header("이동 속도")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    
    [Header("점프")]
    public float jumpPower = 5f;
    
    [Header("구르기")]
    public float rollDistance = 3f;
    public float rollDuration = 0.5f;
    
    [Header("지면 체크")]
    public float groundCheckDistance = 0.1f;
    
    [Header("회전")]
    public float rotationSpeed = 720f;
}
```

### PlayerSettingsData (ScriptableObject)

플레이어 전용 설정입니다.

```csharp
[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Game/Player Settings")]
public class PlayerSettingsData : ScriptableObject
{
    [Header("입력 설정")]
    [Range(0.01f, 0.5f)]
    public float deadzone = 0.1f;
    
    [Header("마우스 감도")]
    [Range(1f, 100f)]
    public float mouseSensitivity = 5f;
    public bool invertYAxis = false;
    
    [Header("카메라 회전 설정")]
    [Range(-90f, 0f)]
    public float cameraMinVerticalAngle = -40f;
    [Range(0f, 90f)]
    public float cameraMaxVerticalAngle = 80f;
    [Range(0.01f, 0.2f)]
    public float cameraRotationSmoothing = 0.05f;
    
    [Header("카메라 거리 설정")]
    [Range(1f, 10f)]
    public float cameraDistance = 5f;
    [Range(0.5f, 3f)]
    public float cameraMinDistance = 1.5f;
    [Range(5f, 15f)]
    public float cameraMaxDistance = 10f;
    [Range(1f, 20f)]
    public float zoomSpeed = 2f;
    
    [Header("카메라 충돌 설정")]
    public LayerMask cameraCollisionLayers = -1;
    [Range(0.1f, 1f)]
    public float cameraCollisionRadius = 0.2f;
}
```

### NPCSettingsData (ScriptableObject)

NPC 전용 설정입니다.

```csharp
[CreateAssetMenu(fileName = "NPCSettings", menuName = "RPG/Character/NPC Settings")]
public class NPCSettingsData : ScriptableObject
{
    [Header("NPC 타입")]
    public NPCType npcType = NPCType.Normal;
    
    [Header("AI 설정")]
    public float patrolRange = 5f;
    public float npcWalkSpeed = 2f;
    public float idleTimeMin = 2f;
    public float idleTimeMax = 5f;
    
    [Header("상호작용")]
    public float interactionRange = 2f;
    public List<string> dialogues;
    
    [Header("퀘스트 (QuestGiver 전용)")]
    public List<Quest> availableQuests;
    
    [Header("상점 (Merchant 전용)")]
    public List<ShopItem> shopItems;
    [Range(0f, 100f)]
    public float shopBuyBackPercentage = 50f;
}
```

---

## 💡 사용 예제

### 예제 1: 기본 캐릭터 생성

```csharp
// 1. ScriptableObject 데이터 생성
// Assets → Create → RPG → Character → Stats Data

// 2. 캐릭터 GameObject 생성
GameObject characterObj = new GameObject("MyCharacter");

// 3. Character 컴포넌트 추가
Character character = characterObj.AddComponent<Character>();

// 4. 데이터 할당 (인스펙터 또는 코드)
character.statsData = Resources.Load<CharacterStatsData>("CharacterStats/Warrior");
character.movementData = Resources.Load<MovementSettingsData>("Movement/Default");

// 5. 이벤트 구독
character.OnHPChanged += (c, current, max) => {
    Debug.Log($"HP: {current}/{max}");
};
```

### 예제 2: 플레이어 설정

```csharp
public class GameSetup : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private PlayerSettingsData playerSettings;
    
    void Start()
    {
        // 플레이어 생성
        GameObject playerObj = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        Player player = playerObj.GetComponent<Player>();
        
        // 설정 할당
        player.playerSettings = playerSettings;
        
        // UI 업데이트를 위한 이벤트 구독
        player.OnHPChanged += UpdateHealthBar;
        player.OnLevelUp += ShowLevelUpEffect;
    }
    
    void UpdateHealthBar(Character character, float current, float max)
    {
        // UI 업데이트
        float fillAmount = current / max;
        // healthBar.fillAmount = fillAmount;
    }
    
    void ShowLevelUpEffect(Player player, int level)
    {
        Debug.Log($"축하합니다! 레벨 {level}에 도달했습니다!");
        // 레벨업 이펙트 재생
    }
}
```

### 예제 3: NPC 생성 및 상호작용

```csharp
public class NPCManager : MonoBehaviour
{
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private NPCSettingsData merchantSettings;
    
    void Start()
    {
        // 상인 NPC 생성
        GameObject npcObj = Instantiate(npcPrefab, new Vector3(5, 0, 0), Quaternion.identity);
        NPC npc = npcObj.GetComponent<NPC>();
        
        // 설정 할당
        npc.npcSettings = merchantSettings;
        
        // 이벤트 구독
        npc.OnDialogue += HandleDialogue;
        npc.OnShopOpened += OpenShopUI;
        npc.OnQuestOffered += ShowQuestUI;
    }
    
    void HandleDialogue(NPC npc, string dialogue)
    {
        Debug.Log($"{npc.NPCName}: {dialogue}");
        // UI에 대화 표시
    }
    
    void OpenShopUI(NPC npc)
    {
        Debug.Log("상점 UI 열기");
        List<ShopItem> items = npc.GetShopItems();
        // 상점 UI 표시
    }
    
    void ShowQuestUI(NPC npc, Quest quest)
    {
        Debug.Log($"퀘스트: {quest.QuestName}");
        // 퀘스트 UI 표시
    }
}
```

### 예제 4: 적 생성 및 전투

```csharp
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private CharacterStatsData enemyStats;
    
    void SpawnEnemy(Vector3 position)
    {
        GameObject enemyObj = Instantiate(enemyPrefab, position, Quaternion.identity);
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        
        // 스탯 할당
        enemy.statsData = enemyStats;
        
        // 이벤트 구독
        enemy.OnCharacterDied += HandleEnemyDeath;
    }
    
    void HandleEnemyDeath(Character character)
    {
        Enemy enemy = character as Enemy;
        Debug.Log($"{enemy.ActorName} 처치!");
        
        // 킬 카운트 증가, 보상 지급 등
    }
}
```

---

## 🔧 확장 가이드

### 새로운 캐릭터 타입 추가

#### 1단계: 클래스 생성

```csharp
public class Mage : Character
{
    [Header("마법사 전용")]
    [SerializeField] private float spellPower = 50f;
    [SerializeField] private float castSpeed = 1f;
    
    private bool isCasting = false;
    
    protected override void Initialize()
    {
        base.Initialize();
        Debug.Log("마법사 초기화");
    }
    
    public void CastSpell(string spellName)
    {
        if (isCasting || !UseMP(30f))
            return;
        
        StartCoroutine(PerformCastSpell(spellName));
    }
    
    private IEnumerator PerformCastSpell(string spellName)
    {
        isCasting = true;
        SetAnimationTrigger("Cast");
        
        yield return new WaitForSeconds(1f / castSpeed);
        
        Debug.Log($"{spellName} 시전!");
        // 마법 효과 적용
        
        isCasting = false;
    }
}
```

#### 2단계: 데이터 생성

```csharp
[CreateAssetMenu(fileName = "MageStats", menuName = "RPG/Character/Mage Stats")]
public class MageStatsData : CharacterStatsData
{
    [Header("마법사 전용 스탯")]
    public float spellPower = 50f;
    public float castSpeed = 1f;
    public float manaCostReduction = 0f; // 0~1 (0% ~ 100%)
}
```

### 커스텀 상태 이상 효과

```csharp
public class BurnEffect : StatusEffect
{
    private float damagePerSecond;
    
    public BurnEffect(float duration, float damagePerSecond) 
        : base("화상", duration)
    {
        this.damagePerSecond = damagePerSecond;
    }
    
    public override void OnApply(Character character)
    {
        Debug.Log($"{character.ActorName}이(가) 화상을 입었습니다!");
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        // 매 초 데미지
        Character character = GetAffectedCharacter();
        if (character != null)
        {
            character.TakeDamage(damagePerSecond * deltaTime);
        }
    }
    
    public override void OnRemove(Character character)
    {
        Debug.Log($"{character.ActorName}의 화상이 치료되었습니다.");
    }
}

// 사용 예제
character.AddStatusEffect(new BurnEffect(5f, 10f)); // 5초간 초당 10 데미지
```

### 스탯 시스템 확장

```csharp
public class ExtendedCharacter : Character
{
    // 추가 스탯
    protected float attack = 10f;
    protected float defense = 5f;
    protected float criticalChance = 0.1f; // 10%
    
    public float Attack 
    { 
        get => attack; 
        set => attack = Mathf.Max(0, value); 
    }
    
    public float Defense 
    { 
        get => defense; 
        set => defense = Mathf.Max(0, value); 
    }
    
    // 데미지 계산 오버라이드
    public override void TakeDamage(float damage)
    {
        // 방어력 적용
        float reducedDamage = Mathf.Max(1, damage - defense);
        base.TakeDamage(reducedDamage);
    }
    
    // 크리티컬 계산
    public float CalculateDamage()
    {
        float damage = attack;
        
        // 크리티컬 판정
        if (Random.value < criticalChance)
        {
            damage *= 2f;
            Debug.Log("크리티컬 히트!");
        }
        
        return damage;
    }
}
```

---

## ⚠️ 주의사항

### 1. 데이터 할당 확인

```csharp
protected override void InitializeComponents()
{
    base.InitializeComponents();
    
    // 반드시 데이터가 할당되었는지 확인
    if (statsData == null)
    {
        Debug.LogError($"{gameObject.name}: CharacterStatsData가 할당되지 않았습니다!");
    }
}
```

### 2. 물리 처리 구분

```csharp
// ❌ 잘못된 예: Update에서 물리 처리
void Update()
{
    Jump(); // Rigidbody 사용 - FixedUpdate에서 해야 함
}

// ✅ 올바른 예: 입력은 Update, 물리는 FixedUpdate
void Update()
{
    if (Input.GetButtonDown("Jump"))
    {
        shouldJump = true; // 플래그만 설정
    }
}

void FixedUpdate()
{
    if (shouldJump)
    {
        Jump(); // 실제 물리 처리
        shouldJump = false;
    }
}
```

### 3. 이벤트 구독 해제

```csharp
void OnEnable()
{
    player.OnHPChanged += HandleHPChanged;
}

void OnDisable()
{
    // 반드시 구독 해제!
    if (player != null)
    {
        player.OnHPChanged -= HandleHPChanged;
    }
}
```

### 4. Null 체크

```csharp
// ❌ 위험한 코드
character.TakeDamage(10f);

// ✅ 안전한 코드
if (character != null && character.IsAlive)
{
    character.TakeDamage(10f);
}
```

---

## 🎓 베스트 프랙티스

### 1. 데이터 중심 설계

```csharp
// ❌ 나쁜 예: 하드코딩
public class BadCharacter : Character
{
    private float maxHP = 100f;
    private float walkSpeed = 3f;
}

// ✅ 좋은 예: 데이터 기반
public class GoodCharacter : Character
{
    [SerializeField] private CharacterStatsData statsData;
    // statsData에서 모든 값 가져오기
}
```

### 2. 확장 가능한 구조

```csharp
// virtual 키워드로 오버라이드 가능하게
public virtual void TakeDamage(float damage)
{
    // 기본 구현
}

// 상속 클래스에서 커스터마이징
public override void TakeDamage(float damage)
{
    // 커스텀 로직
    base.TakeDamage(damage * defensiveBonus);
}
```

### 3. 이벤트 기반 통신

```csharp
// ❌ 직접 참조
public class BadUI : MonoBehaviour
{
    void Update()
    {
        healthBar.value = player.CurrentHP; // 매 프레임 체크
    }
}

// ✅ 이벤트 기반
public class GoodUI : MonoBehaviour
{
    void Start()
    {
        player.OnHPChanged += UpdateHealthBar; // 변경 시에만 업데이트
    }
    
    void UpdateHealthBar(Character c, float current, float max)
    {
        healthBar.value = current / max;
    }
}
```

---

**캐릭터 시스템을 활용한 즐거운 개발 되세요! 🎮**
