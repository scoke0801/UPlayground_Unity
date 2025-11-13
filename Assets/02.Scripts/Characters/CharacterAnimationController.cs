using UnityEngine;
using Animancer;
using System.Collections;
using System;

/// <summary>
/// [DEPRECATED] 간단한 테스트용 애니메이션 컨트롤러
/// 
/// 새로운 프로젝트에서는 AdvancedCharacterAnimationController를 사용하세요.
/// 이 클래스는 Animation Sequence와 Animation Montage를 지원하는
/// 언리얼 스타일의 애니메이션 시스템을 제공합니다.
/// 
/// 참고: Assets/02.Scripts/Core/AnimationSystemGuide.md
/// </summary>
[System.Obsolete("Use AdvancedCharacterAnimationController instead")]
[RequireComponent(typeof(AnimancerComponent))]
public class CharacterAnimationController : MonoBehaviour
{
    [Header("기본 애니메이션")]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip walkClip;
    [SerializeField] private AnimationClip runClip;
    
    [Header("액션 애니메이션")]
    [SerializeField] private AnimationClip jumpClip;
    [SerializeField] private AnimationClip attackClip;
    
    [Header("설정")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float transitionDuration = 0.3f;

    private AnimancerComponent animancer;
    private AnimancerState currentState;
    
    // 현재 애니메이션 상태
    public enum AnimationState
    {
        Idle,
        Walking,
        Running,
        Jumping,
        Attacking
    }
    
    private AnimationState currentAnimationState = AnimationState.Idle;
    
    void Awake()
    {
        animancer = GetComponent<AnimancerComponent>();
    }

    void Start()
    {
        // 기본 상태로 Idle 애니메이션 재생
        PlayIdle();
    }

    /// <summary>
    /// Idle 애니메이션 재생
    /// </summary>
    public void PlayIdle()
    {
        if (idleClip != null && currentAnimationState != AnimationState.Idle)
        {
            currentState = animancer.Play(idleClip, transitionDuration);
            currentAnimationState = AnimationState.Idle;
            Debug.Log("애니메이션: Idle 상태");
        }
    }

    /// <summary>
    /// 이동 속도에 따른 애니메이션 재생 (Walk/Run)
    /// </summary>
    /// <param name="moveSpeed">이동 속도</param>
    /// <param name="isRunning">달리기 여부</param>
    public void PlayMovement(float moveSpeed, bool isRunning)
    {
        if (moveSpeed <= 0.1f)
        {
            PlayIdle();
            return;
        }
        
        if (isRunning)
        {
            PlayRun();
        }
        else
        {
            PlayWalk();
        }
    }

    /// <summary>
    /// Walk 애니메이션 재생
    /// </summary>
    public void PlayWalk()
    {
        if (walkClip != null && currentAnimationState != AnimationState.Walking)
        {
            currentState = animancer.Play(walkClip, transitionDuration);
            currentAnimationState = AnimationState.Walking;
            Debug.Log("애니메이션: Walk 상태");
        }
    }

    /// <summary>
    /// Run 애니메이션 재생
    /// </summary>
    public void PlayRun()
    {
        if (runClip != null && currentAnimationState != AnimationState.Running)
        {
            currentState = animancer.Play(runClip, transitionDuration);
            currentAnimationState = AnimationState.Running;
            Debug.Log("애니메이션: Run 상태");
        }
    }

    /// <summary>
    /// Jump 애니메이션 재생 (원샷 애니메이션)
    /// </summary>
    public void PlayJump()
    {
        if (jumpClip != null)
        {
            var jumpState = animancer.Play(jumpClip, transitionDuration);
            currentAnimationState = AnimationState.Jumping;
            
            // 점프 애니메이션이 끝나면 Idle로 돌아가기 (코루틴 사용)
            StartCoroutine(WaitForAnimationEnd(jumpState, () => PlayIdle()));
            
            Debug.Log("애니메이션: Jump 실행");
        }
    }

    /// <summary>
    /// Attack 애니메이션 재생 (원샷 애니메이션)
    /// </summary>
    public void PlayAttack()
    {
        if (attackClip != null)
        {
            var attackState = animancer.Play(attackClip, transitionDuration);
            currentAnimationState = AnimationState.Attacking;
            
            // 공격 애니메이션이 끝나면 Idle로 돌아가기 (코루틴 사용)
            StartCoroutine(WaitForAnimationEnd(attackState, () => PlayIdle()));
            
            Debug.Log("애니메이션: Attack 실행");
        }
    }

    /// <summary>
    /// 현재 재생 중인 애니메이션의 진행률 (0~1)
    /// </summary>
    public float GetCurrentAnimationProgress()
    {
        if (currentState != null)
        {
            return currentState.NormalizedTime;
        }
        return 0f;
    }

    /// <summary>
    /// 현재 애니메이션 상태 반환
    /// </summary>
    public AnimationState GetCurrentState()
    {
        return currentAnimationState;
    }

    /// <summary>
    /// 애니메이션이 재생 중인지 확인
    /// </summary>
    public bool IsPlaying()
    {
        return currentState != null && currentState.IsPlaying;
    }

    /// <summary>
    /// 애니메이션 속도 조정
    /// </summary>
    /// <param name="speed">재생 속도 (1.0 = 기본 속도)</param>
    public void SetAnimationSpeed(float speed)
    {
        if (currentState != null)
        {
            currentState.Speed = speed;
        }
    }

    /// <summary>
    /// 애니메이션이 끝날 때까지 기다리고 콜백 실행
    /// </summary>
    /// <param name="state">기다릴 애니메이션 상태</param>
    /// <param name="onComplete">완료 시 실행할 액션</param>
    private System.Collections.IEnumerator WaitForAnimationEnd(AnimancerState state, System.Action onComplete)
    {
        if (state == null) yield break;
        
        // 애니메이션이 끝날 때까지 대기
        while (state.IsPlaying && state.NormalizedTime < 1f)
        {
            yield return null;
        }
        
        // 콜백 실행
        onComplete?.Invoke();
    }
    
    // 테스트용 키보드 입력 (디버그 목적)
    void Update()
    {
        #if UNITY_EDITOR
        TestInput();
        #endif
    }
    
    /// <summary>
    /// 테스트용 입력 처리 (에디터에서만 작동)
    /// </summary>
    void TestInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayIdle();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayWalk();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayRun();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayJump();
        }
        else if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            PlayAttack();
        }
    }
}
