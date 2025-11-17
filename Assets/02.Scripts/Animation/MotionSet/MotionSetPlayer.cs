using UnityEngine;
using Animancer;
using System.Collections.Generic;

/// <summary>
/// MotionSet 재생 컴포넌트
/// AvatarMask를 사용하여 신체 부위별 모션 세트 재생 제어
/// </summary>
[RequireComponent(typeof(AnimancerComponent))]
public class MotionSetPlayer : MonoBehaviour
{
    [SerializeField] private AnimancerComponent animancer;
    [SerializeField] private MontagePlayer montagePlayer;
    
    // 현재 재생 중인 모션 세트 정보
    private MotionSet _currentMotionSet;
    private int _currentSequentialIndex = 0;
    private MotionData _currentMotion;
    
    // 레이어별 재생 상태 관리
    private Dictionary<AvatarMask, AnimancerLayer> _layerByMask = new Dictionary<AvatarMask, AnimancerLayer>();
    private Dictionary<AvatarMask, MixerState<Vector2>> _mixerByMask = new Dictionary<AvatarMask, MixerState<Vector2>>();
    private int _nextLayerIndex = 0;
    
    private void Awake()
    {
        if (animancer == null)
        {
            animancer = GetComponent<AnimancerComponent>();
        }
        
        if (montagePlayer == null)
        {
            montagePlayer = GetComponent<MontagePlayer>();
        }
    }
    
    #region 모션 세트 재생
    
    /// <summary>
    /// MotionSet 재생
    /// </summary>
    public void Play(MotionSet motionSet, float fadeDuration = 0.25f)
    {
        if (motionSet == null || motionSet.Motions.Count == 0)
        {
            Debug.LogWarning("[MotionSetPlayer] MotionSet이 비어있거나 null입니다.");
            return;
        }
        
        _currentMotionSet = motionSet;
        _currentSequentialIndex = 0;
        
        switch (motionSet.PlayMode)
        {
            case MotionPlayMode.Single:
                PlaySingle(motionSet, fadeDuration);
                break;
                
            case MotionPlayMode.Sequential:
                PlaySequential(motionSet, 0, fadeDuration);
                break;
                
            case MotionPlayMode.Blend:
                SetupBlendMotionSet(motionSet);
                break;
                
            case MotionPlayMode.Directional:
                // Directional은 방향 입력 시 재생
                break;
                
            case MotionPlayMode.Random:
                PlayRandom(motionSet, fadeDuration);
                break;
        }
    }
    
    /// <summary>
    /// 단일 모션 재생
    /// </summary>
    private void PlaySingle(MotionSet motionSet, float fadeDuration)
    {
        MotionData motion = motionSet.GetMotionByIndex(0);
        if (motion != null && motion.HasValidAnimation)
        {
            PlayMotion(motion, motionSet.AvatarMask, fadeDuration);
        }
    }
    
    /// <summary>
    /// 순차 모션 재생
    /// </summary>
    private void PlaySequential(MotionSet motionSet, int index, float fadeDuration)
    {
        if (index >= motionSet.Motions.Count)
        {
            _currentSequentialIndex = 0;
            return;
        }
        
        MotionData motion = motionSet.GetMotionByIndex(index);
        if (motion != null && motion.HasValidAnimation)
        {
            var state = PlayMotion(motion, motionSet.AvatarMask, fadeDuration);
            
            // 다음 모션으로 자동 전환 설정
            if (state != null && !motion.loopable)
            {
                state.Events(this).OnEnd ??= () =>
                {
                    _currentSequentialIndex++;
                    if (_currentSequentialIndex < motionSet.Motions.Count)
                    {
                        PlaySequential(motionSet, _currentSequentialIndex, fadeDuration);
                    }
                };
            }
        }
    }
    
    /// <summary>
    /// 랜덤 모션 재생
    /// </summary>
    private void PlayRandom(MotionSet motionSet, float fadeDuration)
    {
        MotionData motion = motionSet.GetRandomMotion();
        if (motion != null && motion.HasValidAnimation)
        {
            PlayMotion(motion, motionSet.AvatarMask, fadeDuration);
        }
    }
    
    /// <summary>
    /// 개별 모션 재생
    /// </summary>
    private AnimancerState PlayMotion(MotionData motion, AvatarMask mask, float fadeDuration)
    {
        _currentMotion = motion;
        
        // Montage가 있으면 MontagePlayer 사용
        if (motion.montage != null)
        {
            if (montagePlayer != null)
            {
                return montagePlayer.Play(motion.montage, fadeDuration);
            }
            else
            {
                Debug.LogWarning("[MotionSetPlayer] MontagePlayer가 없어 Montage를 재생할 수 없습니다.");
                return null;
            }
        }
        
        // 일반 Clip 재생
        if (motion.clip != null)
        {
            AnimancerLayer layer = GetOrCreateLayer(mask);
            var state = layer.Play(motion.clip, fadeDuration);
            // IsLooping은 Clip 자체 설정을 따름
            return state;
        }
        
        return null;
    }
    
    #endregion
    
    #region 블렌딩
    
    /// <summary>
    /// 블렌딩 모션 세트 설정
    /// </summary>
    private void SetupBlendMotionSet(MotionSet motionSet)
    {
        AnimancerLayer layer = GetOrCreateLayer(motionSet.AvatarMask);
        
        switch (motionSet.BlendType)
        {
            case MotionBlendType.Linear:
                SetupLinearBlend(motionSet, layer);
                break;
                
            case MotionBlendType.Cartesian:
            case MotionBlendType.Directional:
                SetupCartesianBlend(motionSet, layer);
                break;
        }
    }
    
    /// <summary>
    /// 1D 선형 블렌딩 설정
    /// </summary>
    private void SetupLinearBlend(MotionSet motionSet, AnimancerLayer layer)
    {
        var mixer = new LinearMixerState();
        
        // 모션 추가
        foreach (var motion in motionSet.Motions)
        {
            if (motion.HasValidAnimation)
            {
                var clip = motion.GetClip();
                mixer.Add(clip, motion.threshold);
            }
        }
        
        layer.Play(mixer);
    }
    
    /// <summary>
    /// 2D 블렌딩 설정
    /// </summary>
    private void SetupCartesianBlend(MotionSet motionSet, AnimancerLayer layer)
    {
        // 방향성 블렌딩인 경우 DirectionalMixerState 사용
        if (motionSet.BlendType == MotionBlendType.Directional)
        {
            var mixer = new DirectionalMixerState();
            
            foreach (var motion in motionSet.Motions)
            {
                if (motion.HasValidAnimation)
                {
                    var clip = motion.GetClip();
                    float rad = motion.directionAngle * Mathf.Deg2Rad;
                    Vector2 position = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                    mixer.Add(clip, position);
                }
            }
            
            layer.Play(mixer);
            _mixerByMask[motionSet.AvatarMask] = mixer;
        }
        else // Cartesian
        {
            var mixer = new CartesianMixerState();
            
            foreach (var motion in motionSet.Motions)
            {
                if (motion.HasValidAnimation)
                {
                    var clip = motion.GetClip();
                    Vector2 position = new Vector2(motion.threshold, 0);
                    mixer.Add(clip, position);
                }
            }
            
            layer.Play(mixer);
            _mixerByMask[motionSet.AvatarMask] = mixer;
        }
    }
    
    /// <summary>
    /// 블렌딩 파라미터 업데이트 (1D)
    /// </summary>
    public void UpdateBlendParameter(float parameter)
    {
        if (_currentMotionSet == null || _currentMotionSet.PlayMode != MotionPlayMode.Blend)
            return;
        
        AnimancerLayer layer = GetLayer(_currentMotionSet.AvatarMask);
        if (layer != null && layer.CurrentState is LinearMixerState mixer)
        {
            mixer.Parameter = parameter;
        }
    }
    
    /// <summary>
    /// 블렌딩 파라미터 업데이트 (2D)
    /// </summary>
    public void UpdateBlendParameter(Vector2 parameter)
    {
        if (_currentMotionSet == null || _currentMotionSet.PlayMode != MotionPlayMode.Blend)
            return;
        
        if (_mixerByMask.TryGetValue(_currentMotionSet.AvatarMask, out var mixer))
        {
            mixer.Parameter = parameter;
        }
    }
    
    #endregion
    
    #region 방향성 재생
    
    /// <summary>
    /// 방향으로 모션 재생
    /// </summary>
    public void PlayByDirection(Vector2 direction, float fadeDuration = 0.25f)
    {
        if (_currentMotionSet == null || _currentMotionSet.PlayMode != MotionPlayMode.Directional)
        {
            Debug.LogWarning("[MotionSetPlayer] Directional 모드가 아닙니다.");
            return;
        }
        
        MotionData motion = _currentMotionSet.GetMotionByDirection(direction);
        if (motion != null && motion.HasValidAnimation)
        {
            PlayMotion(motion, _currentMotionSet.AvatarMask, fadeDuration);
        }
    }
    
    #endregion
    
    #region 순차 재생 제어
    
    /// <summary>
    /// 다음 순차 모션 재생
    /// </summary>
    public void PlayNextSequential(float fadeDuration = 0.25f)
    {
        if (_currentMotionSet == null || _currentMotionSet.PlayMode != MotionPlayMode.Sequential)
        {
            Debug.LogWarning("[MotionSetPlayer] Sequential 모드가 아닙니다.");
            return;
        }
        
        _currentSequentialIndex++;
        if (_currentSequentialIndex >= _currentMotionSet.Motions.Count)
        {
            _currentSequentialIndex = 0;
        }
        
        PlaySequential(_currentMotionSet, _currentSequentialIndex, fadeDuration);
    }
    
    /// <summary>
    /// 특정 인덱스의 모션 재생
    /// </summary>
    public void PlaySequentialAtIndex(int index, float fadeDuration = 0.25f)
    {
        if (_currentMotionSet == null || _currentMotionSet.PlayMode != MotionPlayMode.Sequential)
        {
            Debug.LogWarning("[MotionSetPlayer] Sequential 모드가 아닙니다.");
            return;
        }
        
        _currentSequentialIndex = Mathf.Clamp(index, 0, _currentMotionSet.Motions.Count - 1);
        PlaySequential(_currentMotionSet, _currentSequentialIndex, fadeDuration);
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
        newLayer.Mask = mask;  // SetMask 대신 Mask 속성 사용
        
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
    public void StopAll(float fadeDuration = 0.25f)
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
    
    #region 유틸리티
    
    /// <summary>
    /// 현재 모션 세트
    /// </summary>
    public MotionSet CurrentMotionSet => _currentMotionSet;
    
    /// <summary>
    /// 현재 모션
    /// </summary>
    public MotionData CurrentMotion => _currentMotion;
    
    /// <summary>
    /// 현재 순차 인덱스
    /// </summary>
    public int CurrentSequentialIndex => _currentSequentialIndex;
    
    #endregion
}
