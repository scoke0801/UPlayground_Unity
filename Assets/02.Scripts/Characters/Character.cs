using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 모든 캐릭터(플레이어, NPC, 적)의 기본 클래스
/// </summary>
public abstract class Character : BaseActor
{
    [Header("캐릭터 스탯")]
    [SerializeField] protected float maxHP = 100f;
    [SerializeField] protected float maxMP = 50f;
    [SerializeField] protected float maxStamina = 100f;
    
    [Header("재생률")]
    [SerializeField] protected float hpRegenRate = 5f;
    [SerializeField] protected float mpRegenRate = 10f;
    [SerializeField] protected float staminaRegenRate = 20f;
    
    [Header("이동 속성")]
    [SerializeField] protected float walkSpeed = 3f;
    [SerializeField] protected float runSpeed = 6f;
    [SerializeField] protected float jumpPower = 5f;
    [SerializeField] protected float rollDistance = 3f;
    
    // 현재 스탯
    protected float currentHP;
    protected float currentMP;
    protected float currentStamina;
    
    // 상태
    protected bool isAlive = true;
    protected bool isRunning = false;
    protected bool isGrounded = true;
    protected bool isRolling = false;
    
    // 상태 이상
    protected List<StatusEffect> statusEffects = new List<StatusEffect>();
    
    // 프로퍼티
    public float CurrentHP { get => currentHP; protected set => currentHP = Mathf.Clamp(value, 0, maxHP); }
    public float CurrentMP { get => currentMP; protected set => currentMP = Mathf.Clamp(value, 0, maxMP); }
    public float CurrentStamina { get => currentStamina; protected set => currentStamina = Mathf.Clamp(value, 0, maxStamina); }
    public float MaxHP { get => maxHP; protected set => maxHP = value; }
    public float MaxMP { get => maxMP; protected set => maxMP = value; }
    public float MaxStamina { get => maxStamina; protected set => maxStamina = value; }
    
    public bool IsAlive => isAlive;
    public bool IsRunning => isRunning;
    public bool IsGrounded => isGrounded;
    public bool IsRolling => isRolling;
    
    // 이벤트
    public System.Action<Character, float, float> OnHPChanged;
    public System.Action<Character, float, float> OnMPChanged;
    public System.Action<Character> OnCharacterDied;
    public System.Action<Character, StatusEffect> OnStatusEffectAdded;
    
    // 컴포넌트
    protected Animator characterAnimator;
    
    protected override void InitializeComponents()
    {
        base.InitializeComponents();
        characterAnimator = GetComponent<Animator>();
    }
    
    protected override void Initialize()
    {
        currentHP = maxHP;
        currentMP = maxMP;
        currentStamina = maxStamina;
        
        moveSpeed = walkSpeed;
    }
    
    // Update: 일반 게임 로직
    protected override void UpdateGameLogic()
    {
        if (!isAlive) return;
        
        UpdateRegeneration();
        UpdateStatusEffects();
    }
    
    // FixedUpdate: 물리 기반 처리
    protected override void HandlePhysicsMovement()
    {
        if (!isAlive) return;
        
        UpdateGroundCheck();
        base.HandlePhysicsMovement();
    }
    
    /// <summary>
    /// 지면 체크 (FixedUpdate에서 실행)
    /// </summary>
    protected virtual void UpdateGroundCheck()
    {
        float groundCheckDistance = 0.1f;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f);
    }
    
    /// <summary>
    /// 체력/마나/스태미나 자동 회복
    /// </summary>
    protected virtual void UpdateRegeneration()
    {
        if (currentHP < maxHP)
            CurrentHP += hpRegenRate * Time.deltaTime;
        
        if (currentMP < maxMP)
            CurrentMP += mpRegenRate * Time.deltaTime;
        
        if (currentStamina < maxStamina && !isRunning)
            CurrentStamina += staminaRegenRate * Time.deltaTime;
    }
    
    /// <summary>
    /// 상태 이상 효과 업데이트
    /// </summary>
    protected virtual void UpdateStatusEffects()
    {
        for (int i = statusEffects.Count - 1; i >= 0; i--)
        {
            statusEffects[i].Update(Time.deltaTime);
            if (statusEffects[i].IsExpired())
            {
                statusEffects[i].OnRemove(this);
                statusEffects.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// 데미지 받기
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        if (!isAlive) return;
        
        CurrentHP -= damage;
        OnHPChanged?.Invoke(this, currentHP, maxHP);
        
        if (currentHP <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 체력 회복
    /// </summary>
    public virtual void Heal(float amount)
    {
        CurrentHP += amount;
        OnHPChanged?.Invoke(this, currentHP, maxHP);
    }
    
    /// <summary>
    /// 마나 사용
    /// </summary>
    public virtual bool UseMP(float amount)
    {
        if (currentMP < amount) return false;
        
        CurrentMP -= amount;
        OnMPChanged?.Invoke(this, currentMP, maxMP);
        return true;
    }
    
    /// <summary>
    /// 스태미나 사용
    /// </summary>
    public virtual bool UseStamina(float amount)
    {
        if (currentStamina < amount) return false;
        
        CurrentStamina -= amount;
        return true;
    }
    
    /// <summary>
    /// 점프 (FixedUpdate에서 호출됨)
    /// </summary>
    public virtual void Jump()
    {
        if (!isGrounded || !UseStamina(20f)) return;
        
        if (actorRigidbody != null)
        {
            actorRigidbody.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        }
        
        SetAnimationTrigger("Jump");
    }
    
    /// <summary>
    /// 구르기 (FixedUpdate에서 호출됨)
    /// </summary>
    public virtual void Roll(Vector3 direction)
    {
        if (!isGrounded || isRolling || !UseStamina(30f)) return;
        
        StartCoroutine(PerformRoll(direction));
    }
    
    /// <summary>
    /// 구르기 실행
    /// </summary>
    protected virtual System.Collections.IEnumerator PerformRoll(Vector3 direction)
    {
        isRolling = true;
        SetAnimationTrigger("Roll");
        
        float rollTime = 0.5f;
        float timer = 0f;
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + direction.normalized * rollDistance;
        
        while (timer < rollTime)
        {
            timer += Time.deltaTime;
            float progress = timer / rollTime;
            transform.position = Vector3.Lerp(startPos, targetPos, progress);
            yield return null;
        }
        
        isRolling = false;
    }
    
    /// <summary>
    /// 달리기 설정
    /// </summary>
    public virtual void SetRunning(bool running)
    {
        if (isRolling) return;
        
        if (running && currentStamina <= 0)
        {
            running = false;
        }
        
        isRunning = running;
        moveSpeed = running ? runSpeed : walkSpeed;
        
        if (running)
        {
            UseStamina(10f * Time.deltaTime);
        }
        
        SetAnimationBool("IsRunning", running);
    }
    
    /// <summary>
    /// 상태 이상 추가
    /// </summary>
    public virtual void AddStatusEffect(StatusEffect effect)
    {
        statusEffects.Add(effect);
        effect.OnApply(this);
        OnStatusEffectAdded?.Invoke(this, effect);
    }
    
    /// <summary>
    /// 사망 처리
    /// </summary>
    protected virtual void Die()
    {
        isAlive = false;
        SetAnimationTrigger("Die");
        OnCharacterDied?.Invoke(this);
    }
    
    /// <summary>
    /// 애니메이션 파라미터 설정
    /// </summary>
    protected virtual void SetAnimationTrigger(string triggerName)
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger(triggerName);
        }
    }
    
    protected virtual void SetAnimationBool(string paramName, bool value)
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetBool(paramName, value);
        }
    }
    
    protected virtual void SetAnimationFloat(string paramName, float value)
    {
        if (characterAnimator != null)
        {
            characterAnimator.SetFloat(paramName, value);
        }
    }
}

/// <summary>
/// 상태 이상 효과 기본 클래스
/// </summary>
[System.Serializable]
public abstract class StatusEffect
{
    [SerializeField] protected string effectName;
    [SerializeField] protected float duration;
    [SerializeField] protected float remainingTime;
    
    public string EffectName => effectName;
    public float Duration => duration;
    public float RemainingTime => remainingTime;
    
    public StatusEffect(string name, float duration)
    {
        this.effectName = name;
        this.duration = duration;
        this.remainingTime = duration;
    }
    
    public virtual void OnApply(Character character) { }
    public virtual void Update(float deltaTime) 
    { 
        remainingTime -= deltaTime; 
    }
    public virtual void OnRemove(Character character) { }
    public virtual bool IsExpired() => remainingTime <= 0f;
}