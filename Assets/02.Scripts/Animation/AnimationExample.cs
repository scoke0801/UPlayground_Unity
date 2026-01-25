using UnityEngine;

/// <summary>
/// Animancer 캐릭터 애니메이션 컨트롤러 사용 예제
/// 이 스크립트를 캐릭터에 추가하여 기본적인 애니메이션 제어를 테스트할 수 있습니다.
/// </summary>
[RequireComponent(typeof(CharacterAnimationController))]
public class AnimationExample : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [Tooltip("Idle 애니메이션 이름")]
    public string idleAnimation = "Idle";
    
    [Tooltip("Walk 애니메이션 이름")]
    public string walkAnimation = "Walk";
    
    [Tooltip("Run 애니메이션 이름")]
    public string runAnimation = "Run";
    
    [Tooltip("Jump 애니메이션 이름")]
    public string jumpAnimation = "Jump";
    
    [Header("이동 설정")]
    [Tooltip("걷기 속도")]
    public float walkSpeed = 2f;
    
    [Tooltip("달리기 속도")]
    public float runSpeed = 5f;
    
    private CharacterAnimationController animController;
    private CharacterController characterController;
    private string currentAnimation;
    private float currentSpeed;
    
    void Start()
    {
        // 컴포넌트 가져오기
        animController = GetComponent<CharacterAnimationController>();
        characterController = GetComponent<CharacterController>();
        
        // 시작 애니메이션
        PlayAnimation(idleAnimation);
    }
    
    void Update()
    {
        HandleInput();
        HandleMovement();
    }
    
    /// <summary>
    /// 입력 처리
    /// </summary>
    private void HandleInput()
    {
        // 이동 입력
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        
        // 애니메이션 전환
        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            // 이동 중
            if (isRunning)
            {
                PlayAnimation(runAnimation);
                currentSpeed = runSpeed;
            }
            else
            {
                PlayAnimation(walkAnimation);
                currentSpeed = walkSpeed;
            }
        }
        else
        {
            // 정지 상태
            PlayAnimation(idleAnimation);
            currentSpeed = 0f;
        }
        
        // 점프 (스페이스바)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayAnimation(jumpAnimation);
        }
        
        // 애니메이션 속도 조절 테스트 (1~5 키)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            animController.SetSpeed(0.5f);
            Debug.Log("애니메이션 속도: 0.5x");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            animController.SetSpeed(1f);
            Debug.Log("애니메이션 속도: 1x");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            animController.SetSpeed(1.5f);
            Debug.Log("애니메이션 속도: 1.5x");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            animController.SetSpeed(2f);
            Debug.Log("애니메이션 속도: 2x");
        }
        
        // 일시정지/재개 (P 키)
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (animController.IsPlaying())
            {
                animController.Pause();
                Debug.Log("애니메이션 일시정지");
            }
            else
            {
                animController.Resume();
                Debug.Log("애니메이션 재개");
            }
        }
    }
    
    /// <summary>
    /// 캐릭터 이동 처리
    /// </summary>
    private void HandleMovement()
    {
        if (characterController == null || currentSpeed == 0f)
            return;
        
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, 0f, vertical);
        movement = transform.TransformDirection(movement);
        movement *= currentSpeed;
        
        // 중력 적용
        movement.y = -9.81f;
        
        characterController.Move(movement * Time.deltaTime);
        
        // 캐릭터 회전
        if (movement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(movement.x, 0f, movement.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }
    
    /// <summary>
    /// 애니메이션 재생 (중복 방지)
    /// </summary>
    private void PlayAnimation(string animationName)
    {
        if (currentAnimation != animationName)
        {
            animController.TransitionTo(animationName);
            currentAnimation = animationName;
        }
    }
    
    /// <summary>
    /// 디버그 정보 표시
    /// </summary>
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== 애니메이션 컨트롤 ===");
        GUILayout.Label($"현재 애니메이션: {currentAnimation}");
        GUILayout.Label($"재생 속도: {currentSpeed:F1}");
        GUILayout.Label($"진행도: {animController.GetNormalizedTime():F2}");
        GUILayout.Label("");
        GUILayout.Label("조작법:");
        GUILayout.Label("WASD - 이동");
        GUILayout.Label("Shift - 달리기");
        GUILayout.Label("Space - 점프");
        GUILayout.Label("1~4 - 속도 조절");
        GUILayout.Label("P - 일시정지/재개");
        GUILayout.EndArea();
    }
}
