using UnityEngine;

/// <summary>
/// TPS 액션 RPG의 모든 게임 액터를 위한 기본 클래스
/// </summary>
public abstract class BaseActor : MonoBehaviour
{
    [Header("기본 속성")]
    [SerializeField] protected string actorName = "Unknown";
    [SerializeField] protected bool isActive = true;
    
    [Header("Transform 속성")]
    [SerializeField] protected float moveSpeed = 5f;
    [SerializeField] protected float rotationSpeed = 360f;
    
    [Header("충돌 속성")]
    [SerializeField] protected LayerMask collisionLayers = 1;
    [SerializeField] protected float collisionRadius = 1f;
    
    // 프로퍼티
    public string ActorName { get => actorName; set => actorName = value; }
    public bool IsActive { get => isActive; set => isActive = value; }
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float RotationSpeed { get => rotationSpeed; set => rotationSpeed = value; }
    
    // 이벤트
    public System.Action<BaseActor> OnActorDestroyed;
    public System.Action<BaseActor, bool> OnActiveStateChanged;
    
    // 컴포넌트 참조
    protected Rigidbody actorRigidbody;
    protected Collider actorCollider;
    
    // 초기화
    protected virtual void Awake()
    {
        InitializeComponents();
    }
    
    protected virtual void Start()
    {
        Initialize();
    }
    
    protected virtual void Update()
    {
        if (!isActive) return;
        
        UpdateActor();
    }
    
    /// <summary>
    /// 컴포넌트 참조 초기화
    /// </summary>
    protected virtual void InitializeComponents()
    {
        actorRigidbody = GetComponent<Rigidbody>();
        actorCollider = GetComponent<Collider>();
    }
    
    /// <summary>
    /// 액터 초기화 (상속 클래스에서 구현)
    /// </summary>
    protected abstract void Initialize();
    
    /// <summary>
    /// 매 프레임 업데이트 (상속 클래스에서 구현)
    /// </summary>
    protected abstract void UpdateActor();
    
    /// <summary>
    /// 액터 활성화/비활성화
    /// </summary>
    public virtual void SetActive(bool active)
    {
        if (isActive == active) return;
        
        isActive = active;
        gameObject.SetActive(active);
        OnActiveStateChanged?.Invoke(this, active);
    }
    
    /// <summary>
    /// 액터 이동
    /// </summary>
    public virtual void Move(Vector3 direction)
    {
        if (!isActive || actorRigidbody == null) return;
        
        Vector3 movement = direction.normalized * moveSpeed * Time.deltaTime;
        actorRigidbody.MovePosition(transform.position + movement);
    }
    
    /// <summary>
    /// 액터 회전
    /// </summary>
    public virtual void Rotate(Vector3 targetDirection)
    {
        if (!isActive || targetDirection == Vector3.zero) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, 
            targetRotation, 
            rotationSpeed * Time.deltaTime
        );
    }
    
    /// <summary>
    /// 충돌 검사
    /// </summary>
    public virtual bool CheckCollision(Vector3 position, float radius = -1f)
    {
        float checkRadius = radius < 0 ? collisionRadius : radius;
        return Physics.CheckSphere(position, checkRadius, collisionLayers);
    }
    
    /// <summary>
    /// 거리 계산
    /// </summary>
    public float GetDistanceTo(BaseActor other)
    {
        if (other == null) return float.MaxValue;
        return Vector3.Distance(transform.position, other.transform.position);
    }
    
    /// <summary>
    /// 방향 벡터 계산
    /// </summary>
    public Vector3 GetDirectionTo(BaseActor other)
    {
        if (other == null) return Vector3.zero;
        return (other.transform.position - transform.position).normalized;
    }
    
    /// <summary>
    /// 액터 제거
    /// </summary>
    public virtual void DestroyActor()
    {
        OnActorDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
    
    // 디버그용 기즈모 그리기
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}