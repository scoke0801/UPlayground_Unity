using UnityEngine;
using Animancer;
using System.Collections.Generic;

/// <summary>
/// 캐릭터 애니메이션을 제어하는 런타임 컨트롤러
/// AnimancerComponent와 AnimationDataAsset을 사용
/// </summary>
[RequireComponent(typeof(AnimancerComponent))]
public class CharacterAnimationController : MonoBehaviour
{
    [Header("필수 컴포넌트")]
    [SerializeField] 
    [Tooltip("Animancer 컴포넌트 (자동 할당)")]
    private AnimancerComponent animancer;
    
    [SerializeField] 
    [Tooltip("애니메이션 데이터 에셋")]
    private AnimationDataAsset animationData;
    
    [Header("현재 상태")]
    [SerializeField]
    [Tooltip("현재 재생 중인 애니메이션 이름")]
    private string currentAnimation;
    
    // 현재 재생 중인 상태
    private AnimancerState currentState;
    
    // 이벤트 핸들러 딕셔너리
    private Dictionary<string, System.Action> eventHandlers = new Dictionary<string, System.Action>();

    #region Unity 생명주기
    private void Awake()
    {
        // Animancer 컴포넌트 자동 할당
        if (animancer == null)
        {
            animancer = GetComponent<AnimancerComponent>();
        }
        
        // 이벤트 핸들러 등록
        RegisterEventHandlers();
    }

    private void Start()
    {
        // 데이터 검증
        if (animationData == null)
        {
            Debug.LogError("AnimationDataAsset이 할당되지 않았습니다!");
            return;
        }
        
        // 기본 애니메이션 재생 (있는 경우)
        if (animationData.clips.Count > 0)
        {
            PlayAnimation(animationData.clips[0].clipName);
        }
    }
    #endregion

    #region 애니메이션 재생
    /// <summary>
    /// 애니메이션 재생
    /// </summary>
    public void PlayAnimation(string clipName, float? customFadeDuration = null)
    {
        var clipData = animationData.GetClipData(clipName);
        if (clipData == null || clipData.clip == null)
        {
            Debug.LogWarning($"클립을 찾을 수 없습니다: {clipName}");
            return;
        }
        
        // 페이드 시간 결정
        float fadeDuration = customFadeDuration ?? clipData.fadeInDuration;
        
        // 애니메이션 재생
        currentState = animancer.Play(clipData.clip, fadeDuration);
        currentState.Speed = clipData.defaultSpeed;
        currentAnimation = clipName;
        
        // 이벤트 설정
        SetupEvents(clipName, currentState);
        
        Debug.Log($"애니메이션 재생: {clipName}");
    }
    
    /// <summary>
    /// 트랜지션을 사용한 애니메이션 전환
    /// </summary>
    public void TransitionTo(string targetClipName)
    {
        if (string.IsNullOrEmpty(currentAnimation))
        {
            PlayAnimation(targetClipName);
            return;
        }
        
        // 트랜지션 데이터 찾기
        var transition = animationData.GetTransition(currentAnimation, targetClipName);
        
        if (transition != null)
        {
            // 트랜지션 데이터로 재생
            var clipData = animationData.GetClipData(targetClipName);
            if (clipData != null)
            {
                currentState = animancer.Play(clipData.clip, transition.fadeDuration, transition.fadeMode);
                currentState.Speed = clipData.defaultSpeed;
                currentAnimation = targetClipName;
                
                SetupEvents(targetClipName, currentState);
                
                Debug.Log($"트랜지션: {currentAnimation} → {targetClipName}");
            }
        }
        else
        {
            // 기본 재생
            PlayAnimation(targetClipName);
        }
    }
    
    /// <summary>
    /// 레이어에 애니메이션 재생
    /// </summary>
    public void PlayOnLayer(string layerName, string clipName)
    {
        var layerData = animationData.GetLayer(layerName);
        var clipData = animationData.GetClipData(clipName);
        
        if (layerData == null || clipData == null)
        {
            Debug.LogWarning($"레이어 또는 클립을 찾을 수 없습니다: {layerName}, {clipName}");
            return;
        }
        
        // 레이어 가져오기 또는 생성
        AnimancerLayer layer = animancer.Layers[layerData.layerIndex];
        
        // 레이어 설정
        if (layerData.avatarMask != null)
        {
            layer.Mask = layerData.avatarMask;
        }
        
        layer.Weight = layerData.weight;
        layer.IsAdditive = layerData.isAdditive;
        
        // 애니메이션 재생
        var state = layer.Play(clipData.clip);
        state.Speed = clipData.defaultSpeed;
        
        Debug.Log($"레이어 {layerName}에서 {clipName} 재생");
    }
    #endregion

    #region 애니메이션 제어
    /// <summary>
    /// 현재 애니메이션 일시정지
    /// </summary>
    public void Pause()
    {
        if (currentState != null)
        {
            currentState.Speed = 0;
        }
    }
    
    /// <summary>
    /// 현재 애니메이션 재개
    /// </summary>
    public void Resume()
    {
        if (currentState != null)
        {
            var clipData = animationData.GetClipData(currentAnimation);
            currentState.Speed = clipData?.defaultSpeed ?? 1f;
        }
    }
    
    /// <summary>
    /// 현재 애니메이션 정지
    /// </summary>
    public void Stop()
    {
        animancer.Stop();
        currentState = null;
        currentAnimation = null;
    }
    
    /// <summary>
    /// 재생 속도 설정
    /// </summary>
    public void SetSpeed(float speed)
    {
        if (currentState != null)
        {
            currentState.Speed = speed;
        }
    }
    
    /// <summary>
    /// 특정 시간으로 이동
    /// </summary>
    public void SetTime(float time)
    {
        if (currentState != null)
        {
            currentState.Time = time;
        }
    }
    
    /// <summary>
    /// 정규화된 시간으로 이동 (0~1)
    /// </summary>
    public void SetNormalizedTime(float normalizedTime)
    {
        if (currentState != null)
        {
            currentState.NormalizedTime = normalizedTime;
        }
    }
    #endregion

    #region 이벤트 시스템
    /// <summary>
    /// 이벤트 핸들러 등록
    /// </summary>
    private void RegisterEventHandlers()
    {
        // 여기에 커스텀 이벤트 핸들러를 등록
        // 예: eventHandlers["FootStep"] = OnFootStep;
        eventHandlers["FootStep"] = OnFootStep;
        eventHandlers["AttackHit"] = OnAttackHit;
        eventHandlers["Jump"] = OnJump;
    }
    
    /// <summary>
    /// 애니메이션 이벤트 설정
    /// </summary>
    private void SetupEvents(string clipName, AnimancerState state)
    {
        var events = animationData.GetEventsForClip(clipName);
        
        foreach (var eventData in events)
        {
            // Animancer 이벤트 추가
            state.Events(this).Add(
                eventData.normalizedTime,
                () => TriggerEvent(eventData.eventName)
            );
        }
    }
    
    /// <summary>
    /// 이벤트 트리거
    /// </summary>
    private void TriggerEvent(string eventName)
    {
        if (eventHandlers.TryGetValue(eventName, out var handler))
        {
            handler?.Invoke();
        }
        else
        {
            Debug.Log($"이벤트 발생: {eventName}");
        }
    }
    
    // 이벤트 핸들러 예시들
    private void OnFootStep()
    {
        Debug.Log("발소리 재생");
        // 발소리 사운드 재생 등
    }
    
    private void OnAttackHit()
    {
        Debug.Log("공격 히트");
        // 히트 이펙트, 데미지 처리 등
    }
    
    private void OnJump()
    {
        Debug.Log("점프");
        // 점프 사운드, 파티클 등
    }
    #endregion

    #region 유틸리티
    /// <summary>
    /// 현재 재생 중인 애니메이션 이름 가져오기
    /// </summary>
    public string GetCurrentAnimation()
    {
        return currentAnimation;
    }
    
    /// <summary>
    /// 현재 애니메이션이 재생 중인지 확인
    /// </summary>
    public bool IsPlaying()
    {
        return currentState != null && currentState.IsPlaying;
    }
    
    /// <summary>
    /// 현재 애니메이션 진행도 가져오기 (0~1)
    /// </summary>
    public float GetNormalizedTime()
    {
        return currentState?.NormalizedTime ?? 0f;
    }
    
    /// <summary>
    /// 모든 레이어의 가중치 설정
    /// </summary>
    public void SetLayerWeight(int layerIndex, float weight)
    {
        if (layerIndex < animancer.Layers.Count)
        {
            animancer.Layers[layerIndex].Weight = weight;
        }
    }
    #endregion

    #region 에디터 전용
#if UNITY_EDITOR
    private void OnValidate()
    {
        // Animancer 컴포넌트 자동 할당
        if (animancer == null)
        {
            animancer = GetComponent<AnimancerComponent>();
        }
    }
#endif
    #endregion
}
