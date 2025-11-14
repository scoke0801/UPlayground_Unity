using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// MotionSetPlayer 사용 예시
/// 다양한 사용 시나리오를 보여줍니다
/// </summary>
public class MotionSetPlayerExample : MonoBehaviour
{
    [Header("컴포넌트")]
    [SerializeField] private MotionSetPlayer motionSetPlayer;
    
    [Header("MotionSet 에셋")]
    [SerializeField] private MotionSet locomotionSet;        // Blend 모드 (Linear)
    [SerializeField] private MotionSet combatComboSet;       // Sequential 모드
    [SerializeField] private MotionSet directionalSet;       // Directional 모드
    [SerializeField] private MotionSet idleVariationsSet;    // Random 모드
    [SerializeField] private MotionSet strafeSet;            // Blend 모드 (Cartesian)
    
    [Header("테스트 설정")]
    [SerializeField] private float maxSpeed = 10f;
    
    private Vector2 moveInput;
    private bool isInCombat = false;
    
    private void Start()
    {
        // MotionSetPlayer 이벤트 구독
        motionSetPlayer.OnMotionSetStarted += HandleMotionSetStarted;
        motionSetPlayer.OnMotionSetEnded += HandleMotionSetEnded;
        motionSetPlayer.OnMotionChanged += HandleMotionChanged;
        motionSetPlayer.OnMotionEnded += HandleMotionEnded;
        
        // 기본 Locomotion 재생
        if (locomotionSet != null)
        {
            motionSetPlayer.Play(locomotionSet);
        }
    }
    
    private void Update()
    {
        // 입력 처리
        HandleInput();
        
        // 현재 재생 중인 MotionSet에 따라 업데이트
        UpdateCurrentMotionSet();
    }
    
    // ============================================================
    // 입력 처리
    // ============================================================
    
    private void HandleInput()
    {
        // WASD 이동 입력
        moveInput = new Vector2(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical")
        );
        
        // 1키: Locomotion (Linear Blend)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayLocomotion();
        }
        
        // 2키: Combat Combo (Sequential)
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayCombatCombo();
        }
        
        // 3키: Directional Movement
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayDirectional();
        }
        
        // 4키: Idle Variations (Random)
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PlayIdleVariation();
        }
        
        // 5키: Strafe (Cartesian Blend)
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            PlayStrafe();
        }
        
        // Space: 다음 콤보 (Sequential 모드일 때)
        if (Input.GetKeyDown(KeyCode.Space) && isInCombat)
        {
            motionSetPlayer.PlayNextSequential();
        }
    }
    
    // ============================================================
    // MotionSet 재생 메서드들
    // ============================================================
    
    /// <summary>
    /// 예시 1: Locomotion (Linear Blend)
    /// 속도에 따라 Idle → Walk → Run → Sprint 블렌딩
    /// </summary>
    public void PlayLocomotion()
    {
        if (locomotionSet == null)
        {
            Debug.LogWarning("Locomotion MotionSet이 할당되지 않았습니다!");
            return;
        }
        
        motionSetPlayer.Play(locomotionSet);
        isInCombat = false;
        
        Debug.Log("🏃 Locomotion 모드 활성화 - WASD로 이동하세요");
    }
    
    /// <summary>
    /// 예시 2: Combat Combo (Sequential)
    /// Space 키를 눌러 다음 콤보 재생
    /// </summary>
    public void PlayCombatCombo()
    {
        if (combatComboSet == null)
        {
            Debug.LogWarning("Combat Combo MotionSet이 할당되지 않았습니다!");
            return;
        }
        
        motionSetPlayer.Play(combatComboSet);
        isInCombat = true;
        
        Debug.Log("⚔️ Combat Combo 모드 활성화 - Space로 다음 콤보 실행");
    }
    
    /// <summary>
    /// 예시 3: Directional Movement
    /// 8방향 입력에 따라 적절한 애니메이션 재생
    /// </summary>
    public void PlayDirectional()
    {
        if (directionalSet == null)
        {
            Debug.LogWarning("Directional MotionSet이 할당되지 않았습니다!");
            return;
        }
        
        motionSetPlayer.Play(directionalSet);
        isInCombat = false;
        
        Debug.Log("🧭 Directional 모드 활성화 - WASD로 방향 전환");
    }
    
    /// <summary>
    /// 예시 4: Idle Variations (Random)
    /// 랜덤으로 Idle 애니메이션 재생
    /// </summary>
    public void PlayIdleVariation()
    {
        if (idleVariationsSet == null)
        {
            Debug.LogWarning("Idle Variations MotionSet이 할당되지 않았습니다!");
            return;
        }
        
        motionSetPlayer.Play(idleVariationsSet);
        isInCombat = false;
        
        Debug.Log("😴 랜덤 Idle 애니메이션 재생");
    }
    
    /// <summary>
    /// 예시 5: Strafe (Cartesian Blend)
    /// 2D 입력에 따라 블렌딩 (전후좌우 이동)
    /// </summary>
    public void PlayStrafe()
    {
        if (strafeSet == null)
        {
            Debug.LogWarning("Strafe MotionSet이 할당되지 않았습니다!");
            return;
        }
        
        motionSetPlayer.Play(strafeSet);
        isInCombat = false;
        
        Debug.Log("🎯 Strafe 모드 활성화 - WASD로 자유 이동");
    }
    
    // ============================================================
    // 업데이트 로직
    // ============================================================
    
    private void UpdateCurrentMotionSet()
    {
        if (motionSetPlayer.CurrentMotionSet == null) return;
        
        // 현재 MotionSet의 재생 모드에 따라 업데이트
        switch (motionSetPlayer.CurrentMotionSet.playMode)
        {
            case MotionPlayMode.Blend:
                UpdateBlendMode();
                break;
                
            case MotionPlayMode.Directional:
                UpdateDirectionalMode();
                break;
        }
    }
    
    /// <summary>
    /// Blend 모드 업데이트
    /// </summary>
    private void UpdateBlendMode()
    {
        var blendType = motionSetPlayer.CurrentMotionSet.blendType;
        
        if (blendType == MotionBlendType.Linear)
        {
            // Linear: 이동 속도 계산
            float currentSpeed = moveInput.magnitude * maxSpeed;
            motionSetPlayer.UpdateBlendParameter(currentSpeed);
        }
        else if (blendType == MotionBlendType.Cartesian)
        {
            // Cartesian: 2D 입력 그대로 사용
            motionSetPlayer.UpdateBlendParameter(moveInput * maxSpeed);
        }
        else if (blendType == MotionBlendType.Directional)
        {
            // Directional Blend: 입력 방향 사용
            if (moveInput.magnitude > 0.1f)
            {
                motionSetPlayer.UpdateBlendParameter(moveInput.normalized);
            }
        }
    }
    
    /// <summary>
    /// Directional 모드 업데이트
    /// </summary>
    private void UpdateDirectionalMode()
    {
        // 입력이 있을 때만 방향 애니메이션 재생
        if (moveInput.magnitude > 0.1f)
        {
            motionSetPlayer.PlayByDirection(moveInput.normalized);
        }
    }
    
    // ============================================================
    // 이벤트 핸들러
    // ============================================================
    
    private void HandleMotionSetStarted(MotionSet motionSet)
    {
        Debug.Log($"✅ MotionSet 시작: {motionSet.motionSetName}");
    }
    
    private void HandleMotionSetEnded(MotionSet motionSet)
    {
        Debug.Log($"⏹️ MotionSet 종료: {motionSet.motionSetName}");
        
        // Combat이 끝나면 자동으로 Locomotion으로 돌아가기
        if (motionSet == combatComboSet && locomotionSet != null)
        {
            PlayLocomotion();
        }
    }
    
    private void HandleMotionChanged(MotionData motion)
    {
        Debug.Log($"🎬 모션 변경: {motion.motionName}");
    }
    
    private void HandleMotionEnded(MotionData motion)
    {
        Debug.Log($"✔️ 모션 종료: {motion.motionName}");
    }
    
    // ============================================================
    // 유틸리티
    // ============================================================
    
    /// <summary>
    /// 재생 속도 변경 (디버그용)
    /// </summary>
    [ContextMenu("속도 x0.5")]
    private void SetSpeedHalf()
    {
        motionSetPlayer.SetSpeed(0.5f);
    }
    
    [ContextMenu("속도 x1.0")]
    private void SetSpeedNormal()
    {
        motionSetPlayer.SetSpeed(1f);
    }
    
    [ContextMenu("속도 x2.0")]
    private void SetSpeedDouble()
    {
        motionSetPlayer.SetSpeed(2f);
    }
    
    [ContextMenu("현재 상태 출력")]
    private void PrintCurrentState()
    {
        if (motionSetPlayer.CurrentMotionSet != null)
        {
            Debug.Log($"=== 현재 상태 ===");
            Debug.Log($"MotionSet: {motionSetPlayer.CurrentMotionSet.motionSetName}");
            Debug.Log($"재생 모드: {motionSetPlayer.CurrentMotionSet.playMode}");
            Debug.Log($"재생 중: {motionSetPlayer.IsPlaying}");
            
            if (motionSetPlayer.CurrentMotion != null)
            {
                Debug.Log($"현재 모션: {motionSetPlayer.CurrentMotion.motionName}");
            }
            
            if (motionSetPlayer.CurrentMotionSet.playMode == MotionPlayMode.Sequential)
            {
                Debug.Log($"Sequential 인덱스: {motionSetPlayer.CurrentSequentialIndex}");
            }
        }
        else
        {
            Debug.Log("재생 중인 MotionSet이 없습니다.");
        }
    }
    
    private void OnGUI()
    {
        // 간단한 UI 가이드
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== MotionSet 테스트 ===");
        GUILayout.Label("1: Locomotion (Linear Blend)");
        GUILayout.Label("2: Combat Combo (Sequential)");
        GUILayout.Label("3: Directional (8방향)");
        GUILayout.Label("4: Idle Variations (Random)");
        GUILayout.Label("5: Strafe (Cartesian Blend)");
        GUILayout.Label("");
        GUILayout.Label("WASD: 이동/방향");
        GUILayout.Label("Space: 다음 콤보 (Combat 모드)");
        GUILayout.EndArea();
    }
}
