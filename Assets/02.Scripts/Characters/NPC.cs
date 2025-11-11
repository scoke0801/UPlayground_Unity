using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// NPC (비플레이어 캐릭터) 클래스
/// AI 행동 패턴, 대화 시스템, 퀘스트 제공/완료, 상점 기능을 포함
/// </summary>
public class NPC : Character
{
    [Header("NPC 설정")]
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private NPCType npcType = NPCType.Normal;
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private float patrolRange = 5f;
    [SerializeField] private float npcWalkSpeed = 2f;
    
    [Header("대화 시스템")]
    [SerializeField] private List<string> dialogues = new List<string>();
    [SerializeField] private int currentDialogueIndex = 0;
    
    [Header("퀘스트 시스템")]
    [SerializeField] private List<Quest> availableQuests = new List<Quest>();
    [SerializeField] private List<Quest> completableQuests = new List<Quest>();
    
    [Header("상점 시스템 (상인용)")]
    [SerializeField] private List<ShopItem> shopItems = new List<ShopItem>();
    [SerializeField] private int shopBuyBackPercentage = 50;
    
    // AI 상태
    private NPCState currentState = NPCState.Idle;
    private Vector3 startPosition;
    private Vector3 currentTarget;
    private float stateTimer = 0f;
    private Transform player;
    
    // 컴포넌트
    private SphereCollider interactionCollider;
    
    // 프로퍼티
    public string NPCName => npcName;
    public NPCType Type => npcType;
    public bool CanInteract { get; private set; } = false;
    
    // 이벤트
    public System.Action<NPC, string> OnDialogue;
    public System.Action<NPC, Quest> OnQuestOffered;
    public System.Action<NPC, Quest> OnQuestCompleted;
    public System.Action<NPC> OnShopOpened;
    
    protected override void Initialize()
    {
        base.Initialize();
        
        startPosition = transform.position;
        currentTarget = startPosition;
        player = FindFirstObjectByType<Player>()?.transform;
        
        SetupInteractionCollider();
        SetRandomTarget();
    }
    
    /// <summary>
    /// 상호작용 콜라이더 설정
    /// </summary>
    private void SetupInteractionCollider()
    {
        interactionCollider = gameObject.AddComponent<SphereCollider>();
        interactionCollider.isTrigger = true;
        interactionCollider.radius = interactionRange;
    }
    
    protected override void UpdateGameLogic()
    {
        base.UpdateGameLogic();
        
        if (!isAlive) return;
        
        UpdateAI();
        UpdateInteraction();
    }
    
    /// <summary>
    /// AI 행동 패턴 업데이트
    /// </summary>
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
    
    /// <summary>
    /// 대기 상태 처리
    /// </summary>
    private void HandleIdleState()
    {
        if (stateTimer >= Random.Range(2f, 5f))
        {
            SetRandomTarget();
            ChangeState(NPCState.Walking);
        }
    }
    
    /// <summary>
    /// 걷기 상태 처리
    /// </summary>
    private void HandleWalkingState()
    {
        Vector3 direction = (currentTarget - transform.position).normalized;
        direction.y = 0; // Y축 이동 제한
        
        if (Vector3.Distance(transform.position, currentTarget) < 0.5f)
        {
            ChangeState(NPCState.Idle);
            return;
        }
        
        // 이동
        SetMovementInput(direction);
        
        // 이동 방향으로 회전
        if (direction != Vector3.zero)
        {
            SetRotationTarget(direction);
        }
    }
    
    /// <summary>
    /// 상호작용 상태 처리
    /// </summary>
    private void HandleInteractingState()
    {
        // 플레이어가 멀어지면 원래 상태로 복귀
        if (player != null && Vector3.Distance(transform.position, player.position) > interactionRange)
        {
            ChangeState(NPCState.Idle);
        }
    }
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    private void ChangeState(NPCState newState)
    {
        currentState = newState;
        stateTimer = 0f;
        
        switch (newState)
        {
            case NPCState.Idle:
                SetMovementInput(Vector3.zero);
                break;
            case NPCState.Walking:
                MoveSpeed = npcWalkSpeed;
                break;
            case NPCState.Interacting:
                SetMovementInput(Vector3.zero);
                if (player != null)
                {
                    // 플레이어를 바라보기
                    Vector3 lookDirection = (player.position - transform.position).normalized;
                    lookDirection.y = 0;
                    SetRotationTarget(lookDirection);
                }
                break;
        }
    }
    
    /// <summary>
    /// 랜덤 목표 지점 설정
    /// </summary>
    private void SetRandomTarget()
    {
        Vector2 randomPoint = Random.insideUnitCircle * patrolRange;
        currentTarget = startPosition + new Vector3(randomPoint.x, 0, randomPoint.y);
    }
    
    /// <summary>
    /// 상호작용 가능 여부 업데이트
    /// </summary>
    private void UpdateInteraction()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        CanInteract = distanceToPlayer <= interactionRange;
    }
    
    /// <summary>
    /// 플레이어와 상호작용
    /// </summary>
    public void Interact()
    {
        if (!CanInteract) return;
        
        ChangeState(NPCState.Interacting);
        
        switch (npcType)
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
    
    /// <summary>
    /// 대화 시작
    /// </summary>
    private void StartDialogue()
    {
        if (dialogues.Count == 0)
        {
            OnDialogue?.Invoke(this, "안녕하세요!");
            return;
        }
        
        string dialogue = dialogues[currentDialogueIndex];
        OnDialogue?.Invoke(this, dialogue);
        
        // 다음 대화로 진행
        currentDialogueIndex = (currentDialogueIndex + 1) % dialogues.Count;
    }
    
    /// <summary>
    /// 퀘스트 상호작용 처리
    /// </summary>
    private void HandleQuestInteraction()
    {
        // 완료 가능한 퀘스트 먼저 처리
        if (completableQuests.Count > 0)
        {
            Quest quest = completableQuests[0];
            CompleteQuest(quest);
            return;
        }
        
        // 제공 가능한 퀘스트 처리
        if (availableQuests.Count > 0)
        {
            Quest quest = availableQuests[0];
            OfferQuest(quest);
            return;
        }
        
        // 퀘스트가 없으면 일반 대화
        StartDialogue();
    }
    
    /// <summary>
    /// 퀘스트 제공
    /// </summary>
    private void OfferQuest(Quest quest)
    {
        OnQuestOffered?.Invoke(this, quest);
        availableQuests.Remove(quest);
    }
    
    /// <summary>
    /// 퀘스트 완료
    /// </summary>
    private void CompleteQuest(Quest quest)
    {
        OnQuestCompleted?.Invoke(this, quest);
        completableQuests.Remove(quest);
        
        // 보상 지급 로직
        if (player != null)
        {
            Player playerComponent = player.GetComponent<Player>();
            if (playerComponent != null)
            {
                playerComponent.GainExperience(quest.ExperienceReward);
                // 골드 보상 등 추가 가능
            }
        }
    }
    
    /// <summary>
    /// 상점 열기
    /// </summary>
    private void OpenShop()
    {
        OnShopOpened?.Invoke(this);
    }
    
    /// <summary>
    /// 아이템 구매
    /// </summary>
    public bool BuyItem(ShopItem item, Player buyer)
    {
        // 구매 로직 구현
        if (buyer.GetGold() >= item.Price)
        {
            buyer.SpendGold(item.Price);
            buyer.AddItemToInventory(item.ItemID, item.Quantity);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 아이템 판매
    /// </summary>
    public bool SellItem(string itemID, int quantity, Player seller)
    {
        // 판매 로직 구현
        if (seller.HasItem(itemID, quantity))
        {
            seller.RemoveItemFromInventory(itemID, quantity);
            
            // 판매가 계산 (상점에서 설정한 비율로)
            int sellPrice = GetItemSellPrice(itemID) * quantity;
            seller.GainGold(sellPrice);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 아이템 판매가 계산
    /// </summary>
    private int GetItemSellPrice(string itemID)
    {
        ShopItem shopItem = shopItems.Find(item => item.ItemID == itemID);
        if (shopItem != null)
        {
            return Mathf.RoundToInt(shopItem.Price * (shopBuyBackPercentage / 100f));
        }
        return 10; // 기본 판매가
    }
    
    /// <summary>
    /// 퀘스트를 완료 가능 목록에 추가
    /// </summary>
    public void AddCompletableQuest(Quest quest)
    {
        if (!completableQuests.Contains(quest))
        {
            completableQuests.Add(quest);
        }
    }
    
    // 트리거 이벤트
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CanInteract = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CanInteract = false;
            if (currentState == NPCState.Interacting)
            {
                ChangeState(NPCState.Idle);
            }
        }
    }
    
    // 유틸리티 메서드
    public List<ShopItem> GetShopItems() => shopItems;
    public List<Quest> GetAvailableQuests() => availableQuests;
    public List<Quest> GetCompletableQuests() => completableQuests;
}

/// <summary>
/// NPC 타입 열거형
/// </summary>
public enum NPCType
{
    Normal,      // 일반 NPC
    QuestGiver,  // 퀘스트 제공자
    Merchant     // 상인
}

/// <summary>
/// NPC AI 상태
/// </summary>
public enum NPCState
{
    Idle,        // 대기
    Walking,     // 걷기
    Interacting  // 상호작용 중
}

/// <summary>
/// 퀘스트 데이터
/// </summary>
[System.Serializable]
public class Quest
{
    [SerializeField] private string questID;
    [SerializeField] private string questName;
    [SerializeField] private string description;
    [SerializeField] private int experienceReward;
    [SerializeField] private int goldReward;
    [SerializeField] private List<string> itemRewards;
    
    public string QuestID => questID;
    public string QuestName => questName;
    public string Description => description;
    public int ExperienceReward => experienceReward;
    public int GoldReward => goldReward;
    public List<string> ItemRewards => itemRewards;
    
    public Quest(string id, string name, string desc, int exp, int gold)
    {
        questID = id;
        questName = name;
        description = desc;
        experienceReward = exp;
        goldReward = gold;
        itemRewards = new List<string>();
    }
}

/// <summary>
/// 상점 아이템 데이터
/// </summary>
[System.Serializable]
public class ShopItem
{
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;
    [SerializeField] private int price;
    [SerializeField] private int quantity;
    [SerializeField] private bool isUnlimited;
    
    public string ItemID => itemID;
    public string ItemName => itemName;
    public int Price => price;
    public int Quantity => quantity;
    public bool IsUnlimited => isUnlimited;
    
    public ShopItem(string id, string name, int price, int quantity = 1, bool unlimited = false)
    {
        this.itemID = id;
        this.itemName = name;
        this.price = price;
        this.quantity = quantity;
        this.isUnlimited = unlimited;
    }
}
