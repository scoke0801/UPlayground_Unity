using UnityEngine;
using Animancer;
using System;
using System.Collections.Generic;

/// <summary>
/// AnimationMontage 재생 컴포넌트
/// AvatarMask를 사용하여 신체 부위별 애니메이션 재생 제어
/// </summary>
[RequireComponent(typeof(AnimancerComponent))]
public class MontagePlayer : MonoBehaviour
{
    [SerializeField] private AnimancerComponent animancer;
    
    // 현재 재생 중인 몽타주 정보
    private AnimationMontage _currentMontage;
    private AnimancerState _currentState;
    private MontageSection _currentSection;
    
    // 레이어별 재생 상태 관리 (AvatarMask 기반)
    private Dictionary<AvatarMask, AnimancerLayer> _layerByMask = new Dictionary<AvatarMask, AnimancerLayer>();
    private int _nextLayerIndex = 0;
    
    // 이벤트
    public event Action<string> OnNotifyTriggered;
    public event Action OnMontageComplete;
    
    private void Awake()
    {
        if (animancer == null)
        {
            animancer = GetComponent<AnimancerComponent>();
        }
    }
    
    #region 몽타주 재생
    
    /// <summary>
    /// 몽타주 재생
    /// </summary>
    public AnimancerState Play(AnimationMontage montage, float fadeDuration = 0.25f)
    {
        if (montage == null || montage.AnimationClip == null)
        {
            Debug.LogWarning("[MontagePlayer] 몽타주 또는 애니메이션 클립이 null입니다.");
            return null;
        }
        
        _currentMontage = montage;
        _currentSection = null;
        
        // AvatarMask에 따라 적절한 레이어 선택
        AnimancerLayer targetLayer = GetOrCreateLayer(montage.AvatarMask);
        
        // 애니메이션 재생
        _currentState = targetLayer.Play(montage.AnimationClip, fadeDuration);
        
        // 루프 설정
        _currentState.IsLooping = montage.IsLooping;
        
        // 노티파이 설정
        SetupNotifies();
        
        // 완료 이벤트 설정
        _currentState.Events.OnEnd = OnAnimationEnd;
        
        return _currentState;
    }
    
    /// <summary>
    /// 특정 섹션부터 재생
    /// </summary>
    public AnimancerState PlayFromSection(AnimationMontage montage, string sectionName, float fadeDuration = 0.25f)
    {
        if (montage == null)
        {
            Debug.LogWarning("[MontagePlayer] 몽타주가 null입니다.");
            return null;
        }
        
        MontageSection section = montage.GetSection(sectionName);
        if (section == null)
        {
            Debug.LogWarning($"[MontagePlayer] 섹션을 찾을 수 없습니다: {sectionName}");
            return Play(montage, fadeDuration);
        }
        
        _currentSection = section;
        AnimancerState state = Play(montage, fadeDuration);
        
        if (state != null)
        {
            // 섹션 시작 시간으로 이동
            state.Time = section.startTime * state.Length;
            
            // 섹션 종료 시간 이벤트 설정
            float endTime = section.endTime * state.Length;
            state.Events.Add(endTime, OnSectionEnd);
        }
        
        return state;
    }
    
    /// <summary>
    /// 몽타주 중지
    /// </summary>
    public void Stop(float fadeDuration = 0.25f)
    {
        if (_currentState != null)
        {
            _currentState.Stop(fadeDuration);
            _currentState = null;
            _currentMontage = null;
            _currentSection = null;
        }
    }
    
    #endregion
    
    #region 레이어 관리 (AvatarMask 기반)
    
    /// <summary>
    /// AvatarMask에 맞는 레이어 가져오기 또는 생성
    /// </summary>
    private AnimancerLayer GetOrCreateLayer(AvatarMask mask)
    {
        // AvatarMask가 없으면 기본 레이어 사용
        if (mask == null)
        {
            return animancer.Layers[0];
        }
        
        // 이미 생성된 레이어가 있으면 재사용
        if (_layerByMask.TryGetValue(mask, out AnimancerLayer existingLayer))
        {
            return existingLayer;
        }
        
        // 새 레이어 생성
        AnimancerLayer newLayer = animancer.Layers[_nextLayerIndex];
        newLayer.SetMask(mask);
        
        _layerByMask[mask] = newLayer;
        _nextLayerIndex++;
        
        return newLayer;
    }
    
    /// <summary>
    /// 특정 마스크의 레이어 가져오기
    /// </summary>
    public AnimancerLayer GetLayer(AvatarMask mask)
    {
        if (mask == null)
        {
            return animancer.Layers[0];
        }
        
        return _layerByMask.TryGetValue(mask, out AnimancerLayer layer) ? layer : null;
    }
    
    /// <summary>
    /// 모든 레이어 중지
    /// </summary>
    public void StopAllLayers(float fadeDuration = 0.25f)
    {
        foreach (var layer in animancer.Layers)
        {
            if (layer != null)
            {
                layer.StartFade(0f, fadeDuration);
            }
        }
    }
    
    #endregion
    
    #region 노티파이 및 이벤트
    
    /// <summary>
    /// 노티파이 이벤트 설정
    /// </summary>
    private void SetupNotifies()
    {
        if (_currentMontage == null || _currentState == null)
            return;
        
        foreach (var notify in _currentMontage.Notifies)
        {
            float notifyTime = notify.normalizedTime * _currentState.Length;
            
            _currentState.Events.Add(notifyTime, () =>
            {
                OnNotifyTriggered?.Invoke(notify.notifyName);
                notify.OnNotifyTriggered?.Invoke(notify.eventParameter);
            });
        }
    }
    
    /// <summary>
    /// 섹션 종료 콜백
    /// </summary>
    private void OnSectionEnd()
    {
        if (_currentSection == null || _currentMontage == null)
            return;
        
        // 다음 섹션이 있으면 재생
        if (!string.IsNullOrEmpty(_currentSection.nextSection))
        {
            PlayFromSection(_currentMontage, _currentSection.nextSection, 0f);
        }
        else
        {
            OnMontageComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// 애니메이션 종료 콜백
    /// </summary>
    private void OnAnimationEnd()
    {
        if (!_currentMontage.IsLooping)
        {
            OnMontageComplete?.Invoke();
        }
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 현재 재생 중인 몽타주
    /// </summary>
    public AnimationMontage CurrentMontage => _currentMontage;
    
    /// <summary>
    /// 현재 애니메이션 상태
    /// </summary>
    public AnimancerState CurrentState => _currentState;
    
    /// <summary>
    /// 재생 중인지 확인
    /// </summary>
    public bool IsPlaying => _currentState != null && _currentState.IsPlaying;
    
    #endregion
}
