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
    
    // 이동 관련 변수
    protected Vector3 movementInput;
    protected Vector3 rotationTarget;
    
    protected virtual void Awake()
    {
        InitializeComponents();
    }
    
    protected virtual void Start()
    {
        Initialize();
    }
    
    // 입력 처리 및 일반 게임 로직 (60fps 기준)
    protected virtual void Update()
    {
        if (!isActive) return;
        HandleInput();
        UpdateGameLogic();
    }
    
    // 물리 기반 이동 처리 (50fps 고정)
    protected virtual void FixedUpdate()
    {
        if (!isActive) return;
        HandlePhysicsMovement();
    }
    
    // 카메라 및 후처리 로직 (모든 Update 후 실행)
    protected virtual void LateUpdate()
    {
        if (!isActive) return;
        HandleLateUpdate();
    }
    
    protected virtual void InitializeComponents()
    {
        actorRigidbody = GetComponent<Rigidbody>();
        actorCollider = GetComponent<Collider>();
    }
    
    protected abstract void Initialize();
    
    // 입력 처리 (상속 클래스에서 구현)
    protected virtual void HandleInput() { }
    
    // 일반 게임 로직 (상속 클래스에서 구현)
    protected virtual void UpdateGameLogic() { }
    
    // 물리 기반 이동 처리
    protected virtual void HandlePhysicsMovement()
    {
        if (actorRigidbody == null) return;
        
        // 이동 처리
        if (movementInput != Vector3.zero)
        {
            Vector3 movement = movementInput.normalized * moveSpeed * Time.fixedDeltaTime;
            actorRigidbody.MovePosition(transform.position + movement);
        }
        
        // 회전 처리
        if (rotationTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rotationTarget);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRotation, 
                rotationSpeed * Time.fixedDeltaTime
            );
        }
    }
    
    // 후처리 로직 (상속 클래스에서 구현)
    protected virtual void HandleLateUpdate() { }
    
    // 이동 입력 설정
    public virtual void SetMovementInput(Vector3 direction)
    {
        movementInput = direction;
    }
    
    // 회전 타겟 설정
    public virtual void SetRotationTarget(Vector3 targetDirection)
    {
        rotationTarget = targetDirection;
    }
    
    // 기존 메서드들 유지...
    public virtual void SetActive(bool active)
    {
        if (isActive == active) return;
        
        isActive = active;
        gameObject.SetActive(active);
        OnActiveStateChanged?.Invoke(this, active);
    }
    
    public virtual bool CheckCollision(Vector3 position, float radius = -1f)
    {
        float checkRadius = radius < 0 ? collisionRadius : radius;
        return Physics.CheckSphere(position, checkRadius, collisionLayers);
    }
    
    public float GetDistanceTo(BaseActor other)
    {
        if (other == null) return float.MaxValue;
        return Vector3.Distance(transform.position, other.transform.position);
    }
    
    public Vector3 GetDirectionTo(BaseActor other)
    {
        if (other == null) return Vector3.zero;
        return (other.transform.position - transform.position).normalized;
    }
    
    public virtual void DestroyActor()
    {
        OnActorDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}