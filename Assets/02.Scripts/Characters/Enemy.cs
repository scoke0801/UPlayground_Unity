using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Enemy (적) 클래스
/// AI 전투 시스템, 공격 패턴 관리, 드롭 아이템 시스템, 경험치 제공을 포함
/// </summary>
public class Enemy : Character
{
    [Header("적 설정")]
    [SerializeField] private string enemyName = "Enemy";
    [SerializeField] private EnemyType enemyType = EnemyType.Normal;
    [SerializeField] private int level = 1;
    [SerializeField] private int experienceReward = 50;
    
    [Header("AI 설정")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float chaseRange = 15f;
    [SerializeField] private float patrolRange = 5f;
    [SerializeField] private float lostTargetTime = 3f;
    
    [Header("전투 설정")]
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private List<AttackPattern> attackPatterns = new List<AttackPattern>();
    
    [Header("드롭 시스템")]
    [SerializeField] private List<DropItem> dropItems = new List<DropItem>();
    [SerializeField] private int minGoldDrop = 10;
    [SerializeField] private int maxGoldDrop = 50;
    
    // AI 상태
    private EnemyState currentState = EnemyState.Patrol;
    private Transform target;
    private Vector3 startPosition;
    private Vector3 patrolTarget;
    private float lastAttackTime = 0f;
    private float lostTargetTimer = 0f;
    private int currentAttackPatternIndex = 0;
    
    // 전투 관련
    private bool isInCombat = false;
    private float combatTimer = 0f;
    
    // 컴포넌트
    private SphereCollider detectionCollider;
    
    // 프로퍼티
    public string EnemyName => enemyName;
    public EnemyType Type => enemyType;
    public int Level => level;
    public bool IsInCombat => isInCombat;
    public Transform Target => target;
    
    // 이벤트
    public System.Action<Enemy> OnEnemyDied;
    public System.Action<Enemy, Character> OnTargetDetected;
    public System.Action<Enemy, Character> OnTargetLost;
    public System.Action<Enemy, AttackPattern> OnAttackExecuted;
    
    protected override void Initialize()
    {
        base.Initialize();
        
        startPosition = transform.position;
        patrolTarget = GetRandomPatrolPoint();
        
        SetupDetectionCollider();
        InitializeStats();
    }
    
    /// <summary>
    /// 레벨에 따른 스탯 초기화
    /// </summary>
    private void InitializeStats()
    {
        // 레벨에 따른 스탯 증가
        maxHP = 100f + (level - 1) * 20f;
        attackDamage = 20f + (level - 1) * 5f;
        experienceReward = 50 + (level - 1) * 25;
        
        // 현재 체력을 최대치로 설정
        currentHP = maxHP;
    }
    
    /// <summary>
    /// 감지 콜라이더 설정
    /// </summary>
    private void SetupDetectionCollider()
    {
        detectionCollider = gameObject.AddComponent<SphereCollider>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRange;
    }
    
    protected override void UpdateGameLogic()
    {
        base.UpdateGameLogic();
        
        if (!isAlive) return;
        
        UpdateAI();
        UpdateCombat();
    }
    
    /// <summary>
    /// AI 업데이트
    /// </summary>
    private void UpdateAI()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                HandlePatrolState();
                break;
            case EnemyState.Chasing:
                HandleChasingState();
                break;
            case EnemyState.Attacking:
                HandleAttackingState();
                break;
            case EnemyState.Returning:
                HandleReturningState();
                break;
            case EnemyState.Dead:
                // 사망 상태는 별도 처리하지 않음
                break;
        }
    }
    
    /// <summary>
    /// 순찰 상태 처리
    /// </summary>
    private void HandlePatrolState()
    {
        // 목표가 있으면 추적 시작
        if (target != null && Vector3.Distance(transform.position, target.position) <= detectionRange)
        {
            ChangeState(EnemyState.Chasing);
            return;
        }
        
        // 순찰 이동
        Vector3 direction = (patrolTarget - transform.position).normalized;
        direction.y = 0;
        
        if (Vector3.Distance(transform.position, patrolTarget) < 1f)
        {
            patrolTarget = GetRandomPatrolPoint();
        }
        
        SetMovementInput(direction);
        MoveSpeed = walkSpeed;
        
        if (direction != Vector3.zero)
        {
            SetRotationTarget(direction);
        }
    }
    
    /// <summary>
    /// 추적 상태 처리
    /// </summary>
    private void HandleChasingState()
    {
        if (target == null)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // 추적 범위를 벗어났으면 목표 잃음
        if (distanceToTarget > chaseRange)
        {
            lostTargetTimer += Time.deltaTime;
            if (lostTargetTimer >= lostTargetTime)
            {
                LoseTarget();
                ChangeState(EnemyState.Returning);
                return;
            }
        }
        else
        {
            lostTargetTimer = 0f;
        }
        
        // 공격 범위에 들어왔으면 공격
        if (distanceToTarget <= attackRange)
        {
            ChangeState(EnemyState.Attacking);
            return;
        }
        
        // 목표를 향해 이동
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0;
        
        SetMovementInput(direction);
        MoveSpeed = runSpeed;
        
        if (direction != Vector3.zero)
        {
            SetRotationTarget(direction);
        }
    }
    
    /// <summary>
    /// 공격 상태 처리
    /// </summary>
    private void HandleAttackingState()
    {
        if (target == null)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        
        // 공격 범위를 벗어났으면 다시 추적
        if (distanceToTarget > attackRange)
        {
            ChangeState(EnemyState.Chasing);
            return;
        }
        
        // 목표를 바라보기
        Vector3 lookDirection = (target.position - transform.position).normalized;
        lookDirection.y = 0;
        SetRotationTarget(lookDirection);
        
        // 공격 쿨다운 확인
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            ExecuteAttack();
            lastAttackTime = Time.time;
        }
        
        // 이동 정지
        SetMovementInput(Vector3.zero);
    }
    
    /// <summary>
    /// 복귀 상태 처리
    /// </summary>
    private void HandleReturningState()
    {
        Vector3 direction = (startPosition - transform.position).normalized;
        direction.y = 0;
        
        // 시작 지점에 도착했으면 순찰 재개
        if (Vector3.Distance(transform.position, startPosition) < 1f)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }
        
        SetMovementInput(direction);
        MoveSpeed = walkSpeed;
        
        if (direction != Vector3.zero)
        {
            SetRotationTarget(direction);
        }
    }
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    private void ChangeState(EnemyState newState)
    {
        EnemyState previousState = currentState;
        currentState = newState;
        
        // 상태별 초기화
        switch (newState)
        {
            case EnemyState.Patrol:
                isInCombat = false;
                SetAnimationBool("IsRunning", false);
                SetAnimationBool("IsAttacking", false);
                break;
                
            case EnemyState.Chasing:
                isInCombat = true;
                combatTimer = 0f;
                SetAnimationBool("IsRunning", true);
                OnTargetDetected?.Invoke(this, target.GetComponent<Character>());
                break;
                
            case EnemyState.Attacking:
                SetAnimationBool("IsRunning", false);
                SetAnimationBool("IsAttacking", true);
                break;
                
            case EnemyState.Returning:
                isInCombat = false;
                SetAnimationBool("IsRunning", false);
                SetAnimationBool("IsAttacking", false);
                OnTargetLost?.Invoke(this, target?.GetComponent<Character>());
                break;
        }
    }
    
    /// <summary>
    /// 공격 실행
    /// </summary>
    private void ExecuteAttack()
    {
        if (attackPatterns.Count == 0)
        {
            // 기본 공격
            PerformBasicAttack();
            return;
        }
        
        // 패턴 공격 실행
        AttackPattern pattern = attackPatterns[currentAttackPatternIndex];
        StartCoroutine(PerformAttackPattern(pattern));
        
        currentAttackPatternIndex = (currentAttackPatternIndex + 1) % attackPatterns.Count;
    }
    
    /// <summary>
    /// 기본 공격
    /// </summary>
    private void PerformBasicAttack()
    {
        if (target != null)
        {
            Character targetCharacter = target.GetComponent<Character>();
            if (targetCharacter != null)
            {
                targetCharacter.TakeDamage(attackDamage);
                SetAnimationTrigger("Attack");
            }
        }
    }
    
    /// <summary>
    /// 패턴 공격 실행
    /// </summary>
    private IEnumerator PerformAttackPattern(AttackPattern pattern)
    {
        OnAttackExecuted?.Invoke(this, pattern);
        
        for (int i = 0; i < pattern.AttackCount; i++)
        {
            if (target == null) break;
            
            // 공격 애니메이션 재생
            SetAnimationTrigger($"Attack{pattern.AnimationIndex}");
            
            // 공격 딜레이 대기
            yield return new WaitForSeconds(pattern.AttackDelay);
            
            // 데미지 적용
            Character targetCharacter = target.GetComponent<Character>();
            if (targetCharacter != null)
            {
                float damage = attackDamage * pattern.DamageMultiplier;
                targetCharacter.TakeDamage(damage);
                
                // 특수 효과 적용
                if (pattern.StatusEffect != null)
                {
                    targetCharacter.AddStatusEffect(pattern.StatusEffect);
                }
            }
            
            // 연속 공격 간격
            if (i < pattern.AttackCount - 1)
            {
                yield return new WaitForSeconds(pattern.ComboInterval);
            }
        }
    }
    
    /// <summary>
    /// 전투 업데이트
    /// </summary>
    private void UpdateCombat()
    {
        if (isInCombat)
        {
            combatTimer += Time.deltaTime;
        }
    }
    
    /// <summary>
    /// 목표 설정
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        lostTargetTimer = 0f;
    }
    
    /// <summary>
    /// 목표 잃음
    /// </summary>
    private void LoseTarget()
    {
        target = null;
        lostTargetTimer = 0f;
    }
    
    /// <summary>
    /// 랜덤 순찰 지점 생성
    /// </summary>
    private Vector3 GetRandomPatrolPoint()
    {
        Vector2 randomPoint = Random.insideUnitCircle * patrolRange;
        return startPosition + new Vector3(randomPoint.x, 0, randomPoint.y);
    }
    
    /// <summary>
    /// 사망 처리 오버라이드
    /// </summary>
    protected override void Die()
    {
        base.Die();
        
        ChangeState(EnemyState.Dead);
        DropItems();
        GiveExperience();
        
        OnEnemyDied?.Invoke(this);
        
        // 일정 시간 후 오브젝트 제거
        StartCoroutine(DestroyAfterDelay(3f));
    }
    
    /// <summary>
    /// 아이템 드롭
    /// </summary>
    private void DropItems()
    {
        // 골드 드롭
        int goldAmount = Random.Range(minGoldDrop, maxGoldDrop + 1);
        DropGold(goldAmount);
        
        // 아이템 드롭
        foreach (DropItem dropItem in dropItems)
        {
            if (Random.Range(0f, 100f) <= dropItem.DropChance)
            {
                int quantity = Random.Range(dropItem.MinQuantity, dropItem.MaxQuantity + 1);
                DropItem(dropItem.ItemID, quantity);
            }
        }
    }
    
    /// <summary>
    /// 골드 드롭
    /// </summary>
    private void DropGold(int amount)
    {
        // 골드 아이템 생성 로직
        Debug.Log($"{enemyName}이(가) {amount} 골드를 드롭했습니다.");
    }
    
    /// <summary>
    /// 아이템 드롭
    /// </summary>
    private void DropItem(string itemID, int quantity)
    {
        // 아이템 오브젝트 생성 로직
        Debug.Log($"{enemyName}이(가) {itemID}을(를) {quantity}개 드롭했습니다.");
    }
    
    /// <summary>
    /// 경험치 제공
    /// </summary>
    private void GiveExperience()
    {
        if (target != null)
        {
            Player player = target.GetComponent<Player>();
            if (player != null)
            {
                player.GainExperience(experienceReward);
            }
        }
    }
    
    /// <summary>
    /// 일정 시간 후 오브젝트 제거
    /// </summary>
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    // 트리거 이벤트
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && currentState == EnemyState.Patrol)
        {
            SetTarget(other.transform);
            ChangeState(EnemyState.Chasing);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && target == other.transform)
        {
            // 추적 범위를 벗어났을 때는 즉시 목표를 잃지 않고 일정 시간 대기
        }
    }
    
    // 공용 메서드
    public void ForceSetTarget(Transform newTarget)
    {
        SetTarget(newTarget);
        if (currentState == EnemyState.Patrol)
        {
            ChangeState(EnemyState.Chasing);
        }
    }
    
    public void ResetToPatrol()
    {
        LoseTarget();
        ChangeState(EnemyState.Patrol);
    }
}

/// <summary>
/// 적 타입 열거형
/// </summary>
public enum EnemyType
{
    Normal,     // 일반 적
    Elite,      // 엘리트 적
    Boss,       // 보스
    Minion      // 미니언
}

/// <summary>
/// 적 AI 상태
/// </summary>
public enum EnemyState
{
    Patrol,     // 순찰
    Chasing,    // 추적
    Attacking,  // 공격
    Returning,  // 복귀
    Dead        // 사망
}

/// <summary>
/// 공격 패턴 데이터
/// </summary>
[System.Serializable]
public class AttackPattern
{
    [SerializeField] private string patternName;
    [SerializeField] private int attackCount = 1;
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float attackDelay = 0.5f;
    [SerializeField] private float comboInterval = 0.3f;
    [SerializeField] private int animationIndex = 1;
    [SerializeField] private StatusEffect statusEffect;
    
    public string PatternName => patternName;
    public int AttackCount => attackCount;
    public float DamageMultiplier => damageMultiplier;
    public float AttackDelay => attackDelay;
    public float ComboInterval => comboInterval;
    public int AnimationIndex => animationIndex;
    public StatusEffect StatusEffect => statusEffect;
    
    public AttackPattern(string name, int count = 1, float multiplier = 1f, float delay = 0.5f)
    {
        patternName = name;
        attackCount = count;
        damageMultiplier = multiplier;
        attackDelay = delay;
        comboInterval = 0.3f;
        animationIndex = 1;
    }
}

/// <summary>
/// 드롭 아이템 데이터
/// </summary>
[System.Serializable]
public class DropItem
{
    [SerializeField] private string itemID;
    [SerializeField] private string itemName;
    [SerializeField] private float dropChance = 50f; // 퍼센트
    [SerializeField] private int minQuantity = 1;
    [SerializeField] private int maxQuantity = 1;
    
    public string ItemID => itemID;
    public string ItemName => itemName;
    public float DropChance => dropChance;
    public int MinQuantity => minQuantity;
    public int MaxQuantity => maxQuantity;
    
    public DropItem(string id, string name, float chance, int min = 1, int max = 1)
    {
        itemID = id;
        itemName = name;
        dropChance = chance;
        minQuantity = min;
        maxQuantity = max;
    }
}
