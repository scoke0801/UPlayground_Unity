using UnityEngine;

/// <summary>
/// 간단한 캐릭터 컨트롤러 - 애니메이션 테스트용
/// WASD 이동과 애니메이션을 연동하여 기능 확인
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CharacterAnimationController))]
public class SimpleCharacterController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float rotationSpeed = 10f;
    
    private CharacterController characterController;
    private CharacterAnimationController animationController;
    
    private Vector3 velocity;
    private bool isGrounded;
    private bool isRunning = false;
    
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animationController = GetComponent<CharacterAnimationController>();
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleActions();
        ApplyGravity();
    }

    /// <summary>
    /// 이동 처리
    /// </summary>
    void HandleMovement()
    {
        // 입력 받기
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        
        if (direction.magnitude >= 0.1f)
        {
            // 달리기 모드 확인
            isRunning = Input.GetKey(KeyCode.LeftShift);
            
            // 이동 속도 설정
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            
            // 캐릭터 회전
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, 
                Quaternion.AngleAxis(targetAngle, Vector3.up), 
                rotationSpeed * Time.deltaTime);
            
            // 이동
            Vector3 moveDirection = direction * currentSpeed;
            characterController.Move(moveDirection * Time.deltaTime);
            
            // 애니메이션 업데이트
            //animationController.PlayMovement(currentSpeed);
        }
        else
        {
            // 정지 상태
            animationController.PlayIdle();
        }
    }

    /// <summary>
    /// 점프 처리
    /// </summary>
    void HandleJump()
    {
        // 지면 체크
        isGrounded = characterController.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 지면에 붙어있도록
        }
        
        // 점프 입력
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animationController.PlayJump();
        }
    }

    /// <summary>
    /// 액션 처리 (공격)
    /// </summary>
    void HandleActions()
    {
        if (Input.GetMouseButtonDown(0)) // 좌클릭
        {
            animationController.PlayAttack();
        }
    }

    /// <summary>
    /// 중력 적용
    /// </summary>
    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// 현재 이동 속도 반환
    /// </summary>
    public float GetCurrentSpeed()
    {
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        return horizontalVelocity.magnitude;
    }

    /// <summary>
    /// 지면에 있는지 확인
    /// </summary>
    public bool IsGrounded()
    {
        return isGrounded;
    }

    /// <summary>
    /// 달리고 있는지 확인
    /// </summary>
    public bool IsRunning()
    {
        return isRunning;
    }

    /// <summary>
    /// 디버그 정보 표시
    /// </summary>
    void OnGUI()
    {
        if (Application.isEditor)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("=== 캐릭터 상태 ===");
            GUILayout.Label($"애니메이션: {animationController.GetCurrentState()}");
            GUILayout.Label($"이동 속도: {GetCurrentSpeed():F1}");
            GUILayout.Label($"지면 여부: {isGrounded}");
            GUILayout.Label($"달리기: {isRunning}");
            GUILayout.Label("");
            GUILayout.Label("=== 조작법 ===");
            GUILayout.Label("WASD: 이동");
            GUILayout.Label("Shift: 달리기");
            GUILayout.Label("Space: 점프");
            GUILayout.Label("좌클릭: 공격");
            GUILayout.Label("");
            GUILayout.Label("=== 테스트 키 ===");
            GUILayout.Label("1: Idle, 2: Walk, 3: Run");
            GUILayout.EndArea();
        }
    }
}
