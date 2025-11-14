using UnityEngine;
using Animancer;
using Animation;
using System;

/// <summary>
/// MotionSet을 재생하는 플레이어
/// </summary>
[RequireComponent(typeof(AnimancerComponent))]
public class MotionSetPlayer : MonoBehaviour
{
    [SerializeField] private AnimancerComponent animancer;
    [SerializeField] private MontagePlayer montagePlayer;
    
    private MotionSet currentMotionSet;
    private int sequentialIndex = 0;
    private MotionData currentMotion;
    
    // Blend 모드용 Mixer State
    private LinearMixerState linearMixer;
    private CartesianMixerState cartesianMixer;
    private DirectionalMixerState directionalMixer;
    
    // 이벤트
    public event Action<MotionSet> OnMotionSetStarted;
    public event Action<MotionSet> OnMotionSetEnded;
    public event Action<MotionData> OnMotionChanged;
    public event Action<MotionData> OnMotionEnded;
    
    private void Awake()
    {
        if (animancer == null)
            animancer = GetComponent<AnimancerComponent>();
    }
    
    /// <summary>
    /// MotionSet 재생
    /// </summary>
    public void Play(MotionSet motionSet)
    {
        if (motionSet == null)
        {
            Debug.LogWarning("[MotionSetPlayer] MotionSet is null!");
            return;
        }
        
        currentMotionSet = motionSet;
        sequentialIndex = 0;
        
        OnMotionSetStarted?.Invoke(motionSet);
        
        switch (motionSet.playMode)
        {
            case MotionPlayMode.Single:
                PlaySingle(motionSet);
                break;
                
            case MotionPlayMode.Sequential:
                PlaySequential(motionSet, 0);
                break;
                
            case MotionPlayMode.Blend:
                SetupBlendMode(motionSet);
                break;
                
            case MotionPlayMode.Directional:
                // Directional은 PlayByDirection 메서드로 별도 제어
                Debug.Log("[MotionSetPlayer] Directional mode ready. Call PlayByDirection() to play.");
                break;
                
            case MotionPlayMode.Random:
                PlayRandom(motionSet);
                break;
        }
    }
    
    /// <summary>
    /// 현재 MotionSet 정지
    /// </summary>
    public void Stop(float fadeDuration = 0.25f)
    {
        if (linearMixer != null)
        {
            linearMixer.Stop();
            linearMixer = null;
        }
        
        if (cartesianMixer != null)
        {
            cartesianMixer.Stop();
            cartesianMixer = null;
        }
        
        if (directionalMixer != null)
        {
            directionalMixer.Stop();
            directionalMixer = null;
        }
        
        if (montagePlayer != null && montagePlayer.IsPlaying)
        {
            montagePlayer.StopMontage(fadeDuration);
        }
        
        var motionSet = currentMotionSet;
        currentMotionSet = null;
        currentMotion = null;
        
        if (motionSet != null)
            OnMotionSetEnded?.Invoke(motionSet);
    }
    
    /// <summary>
    /// 단일 재생
    /// </summary>
    private void PlaySingle(MotionSet motionSet)
    {
        if (motionSet.motions.Count == 0) return;
        
        var motion = motionSet.motions[0];
        PlayMotionData(motion);
    }
    
    /// <summary>
    /// 순차 재생
    /// </summary>
    public void PlaySequential(MotionSet motionSet, int index)
    {
        var motion = motionSet.GetMotionByIndex(index);
        if (motion == null)
        {
            Debug.LogWarning($"[MotionSetPlayer] Motion at index {index} not found!");
            return;
        }
        
        sequentialIndex = index;
        PlayMotionData(motion);
    }
    
    /// <summary>
    /// 다음 콤보 재생
    /// </summary>
    public void PlayNextSequential()
    {
        if (currentMotionSet == null) return;
        if (currentMotionSet.playMode != MotionPlayMode.Sequential) return;
        
        sequentialIndex = (sequentialIndex + 1) % currentMotionSet.motions.Count;
        PlaySequential(currentMotionSet, sequentialIndex);
    }
    
    /// <summary>
    /// 이전 콤보로 돌아가기
    /// </summary>
    public void PlayPreviousSequential()
    {
        if (currentMotionSet == null) return;
        if (currentMotionSet.playMode != MotionPlayMode.Sequential) return;
        
        sequentialIndex--;
        if (sequentialIndex < 0)
            sequentialIndex = currentMotionSet.motions.Count - 1;
        
        PlaySequential(currentMotionSet, sequentialIndex);
    }
    
    /// <summary>
    /// 랜덤 재생
    /// </summary>
    private void PlayRandom(MotionSet motionSet)
    {
        var motion = motionSet.GetRandomMotion();
        if (motion != null)
            PlayMotionData(motion);
    }
    
    /// <summary>
    /// 블렌드 모드 설정
    /// </summary>
    private void SetupBlendMode(MotionSet motionSet)
    {
        switch (motionSet.blendType)
        {
            case MotionBlendType.Linear:
                SetupLinearMixer(motionSet);
                break;
                
            case MotionBlendType.Cartesian:
                SetupCartesianMixer(motionSet);
                break;
                
            case MotionBlendType.Directional:
                SetupDirectionalMixer(motionSet);
                break;
                
            default:
                Debug.LogWarning($"[MotionSetPlayer] Blend mode but blendType is None!");
                break;
        }
    }
    
    /// <summary>
    /// Linear Mixer 설정 (1D 블렌딩)
    /// </summary>
    private void SetupLinearMixer(MotionSet motionSet)
    {
        linearMixer = new LinearMixerState();
        int layerIndex = motionSet.GetLayerIndex();
        
        foreach (var motion in motionSet.motions)
        {
            if (!motion.IsValid()) continue;
            
            if (motion.sourceType == MotionSourceType.Clip && motion.clip != null)
            {
                linearMixer.Add(motion.clip, motion.threshold);
            }
            else if (motion.sourceType == MotionSourceType.Montage && motion.montage != null)
            {
                // Montage의 첫 섹션 클립 사용
                var firstSection = motion.montage.GetFirstSection();
                if (firstSection?.Clip != null)
                {
                    linearMixer.Add(firstSection.Clip, motion.threshold);
                }
            }
        }
        
        if (linearMixer.ChildCount > 0)
        {
            animancer.Layers[layerIndex].Play(linearMixer);
            Debug.Log($"[MotionSetPlayer] Linear Mixer setup with {linearMixer.ChildCount} motions on layer {layerIndex}");
        }
        else
        {
            Debug.LogWarning("[MotionSetPlayer] No valid clips found for Linear Mixer!");
        }
    }
    
    /// <summary>
    /// Cartesian Mixer 설정 (2D 블렌딩)
    /// </summary>
    private void SetupCartesianMixer(MotionSet motionSet)
    {
        cartesianMixer = new CartesianMixerState();
        int layerIndex = motionSet.GetLayerIndex();
        
        foreach (var motion in motionSet.motions)
        {
            if (!motion.IsValid()) continue;
            
            Vector2 position = motion.GetPosition2D();
            
            if (motion.sourceType == MotionSourceType.Clip && motion.clip != null)
            {
                cartesianMixer.Add(motion.clip, position);
            }
            else if (motion.sourceType == MotionSourceType.Montage && motion.montage != null)
            {
                var firstSection = motion.montage.GetFirstSection();
                if (firstSection?.Clip != null)
                {
                    cartesianMixer.Add(firstSection.Clip, position);
                }
            }
        }
        
        if (cartesianMixer.ChildCount > 0)
        {
            animancer.Layers[layerIndex].Play(cartesianMixer);
            Debug.Log($"[MotionSetPlayer] Cartesian Mixer setup with {cartesianMixer.ChildCount} motions on layer {layerIndex}");
        }
        else
        {
            Debug.LogWarning("[MotionSetPlayer] No valid clips found for Cartesian Mixer!");
        }
    }
    
    /// <summary>
    /// Directional Mixer 설정 (2D 방향 블렌딩)
    /// </summary>
    private void SetupDirectionalMixer(MotionSet motionSet)
    {
        directionalMixer = new DirectionalMixerState();
        int layerIndex = motionSet.GetLayerIndex();
        
        foreach (var motion in motionSet.motions)
        {
            if (!motion.IsValid()) continue;
            if (motion.directionAngle < 0) continue; // 방향 설정 안된 모션 제외
            
            Vector2 direction = motion.GetDirectionVector();
            
            if (motion.sourceType == MotionSourceType.Clip && motion.clip != null)
            {
                directionalMixer.Add(motion.clip, direction);
            }
            else if (motion.sourceType == MotionSourceType.Montage && motion.montage != null)
            {
                var firstSection = motion.montage.GetFirstSection();
                if (firstSection?.Clip != null)
                {
                    directionalMixer.Add(firstSection.Clip, direction);
                }
            }
        }
        
        if (directionalMixer.ChildCount > 0)
        {
            animancer.Layers[layerIndex].Play(directionalMixer);
            Debug.Log($"[MotionSetPlayer] Directional Mixer setup with {directionalMixer.ChildCount} motions on layer {layerIndex}");
        }
        else
        {
            Debug.LogWarning("[MotionSetPlayer] No valid clips found for Directional Mixer!");
        }
    }
    
    /// <summary>
    /// 블렌드 파라미터 업데이트 (1D - Linear용)
    /// </summary>
    public void UpdateBlendParameter(float parameter)
    {
        if (linearMixer != null)
        {
            linearMixer.Parameter = parameter;
        }
        else
        {
            Debug.LogWarning("[MotionSetPlayer] Linear Mixer is not active!");
        }
    }
    
    /// <summary>
    /// 블렌드 파라미터 업데이트 (2D - Cartesian/Directional용)
    /// </summary>
    public void UpdateBlendParameter(Vector2 parameter)
    {
        if (cartesianMixer != null)
        {
            cartesianMixer.Parameter = parameter;
        }
        else if (directionalMixer != null)
        {
            directionalMixer.Parameter = parameter;
        }
        else
        {
            Debug.LogWarning("[MotionSetPlayer] 2D Mixer (Cartesian/Directional) is not active!");
        }
    }
    
    /// <summary>
    /// 방향에 따라 재생 (Directional 모드 - Mixer 미사용 버전)
    /// </summary>
    public void PlayByDirection(Vector2 direction)
    {
        if (currentMotionSet == null)
        {
            Debug.LogWarning("[MotionSetPlayer] No MotionSet is loaded!");
            return;
        }
        
        if (currentMotionSet.playMode != MotionPlayMode.Directional)
        {
            Debug.LogWarning("[MotionSetPlayer] MotionSet is not in Directional mode!");
            return;
        }
        
        var motion = currentMotionSet.GetMotionByDirection(direction);
        if (motion != null)
            PlayMotionData(motion);
    }
    
    /// <summary>
    /// 개별 MotionData 재생
    /// </summary>
    private void PlayMotionData(MotionData motion)
    {
        if (motion == null || !motion.IsValid())
        {
            Debug.LogWarning("[MotionSetPlayer] Invalid motion data!");
            return;
        }
        
        currentMotion = motion;
        int layerIndex = currentMotionSet?.GetLayerIndex() ?? 0;
        
        OnMotionChanged?.Invoke(motion);
        
        // Montage로 재생
        if (motion.sourceType == MotionSourceType.Montage && motion.montage != null)
        {
            if (montagePlayer != null)
            {
                montagePlayer.PlayMontage(motion.montage);
                
                // Montage 종료 이벤트 구독
                montagePlayer.OnMontageEnded -= HandleMontageEnded;
                montagePlayer.OnMontageEnded += HandleMontageEnded;
            }
            else
            {
                Debug.LogWarning("[MotionSetPlayer] MontagePlayer is not assigned!");
            }
        }
        // Clip으로 재생
        else if (motion.sourceType == MotionSourceType.Clip && motion.clip != null)
        {
            var state = animancer.Layers[layerIndex].Play(motion.clip);
            
            // 루프 설정 - Animancer v8에서는 Events로 제어
            if (!motion.loopable)
            {
                // 루프하지 않으면 종료 이벤트 설정
                state.Events(this).OnEnd = () => HandleMotionEnded(motion);
            }
            else
            {
                // 루프하면 OnEnd를 null로 (또는 설정하지 않음)
                if (state.Events(this, out var events))
                {
                    events.OnEnd = null;
                }
            }
            
            Debug.Log($"[MotionSetPlayer] Playing clip '{motion.motionName}' on layer {layerIndex}");
        }
    }
    
    /// <summary>
    /// Montage 종료 핸들러
    /// </summary>
    private void HandleMontageEnded(AnimationMontage montage)
    {
        if (currentMotion != null && currentMotion.montage == montage)
        {
            HandleMotionEnded(currentMotion);
        }
    }
    
    /// <summary>
    /// 모션 종료 핸들러
    /// </summary>
    private void HandleMotionEnded(MotionData motion)
    {
        OnMotionEnded?.Invoke(motion);
        
        // Sequential 모드에서 자동으로 다음 모션 재생
        if (currentMotionSet != null && currentMotionSet.playMode == MotionPlayMode.Sequential)
        {
            int nextIndex = sequentialIndex + 1;
            
            if (nextIndex < currentMotionSet.motions.Count)
            {
                PlaySequential(currentMotionSet, nextIndex);
            }
            else
            {
                // 마지막 모션이면 MotionSet 종료
                var motionSet = currentMotionSet;
                currentMotionSet = null;
                currentMotion = null;
                OnMotionSetEnded?.Invoke(motionSet);
            }
        }
    }
    
    /// <summary>
    /// 재생 속도 변경
    /// </summary>
    public void SetSpeed(float speed)
    {
        if (linearMixer != null)
            linearMixer.Speed = speed;
        else if (cartesianMixer != null)
            cartesianMixer.Speed = speed;
        else if (directionalMixer != null)
            directionalMixer.Speed = speed;
        else if (montagePlayer != null)
            montagePlayer.SetPlayRate(speed);
    }
    
    /// <summary>
    /// 현재 재생 상태
    /// </summary>
    public bool IsPlaying
    {
        get
        {
            if (linearMixer != null && linearMixer.IsPlaying) return true;
            if (cartesianMixer != null && cartesianMixer.IsPlaying) return true;
            if (directionalMixer != null && directionalMixer.IsPlaying) return true;
            if (montagePlayer != null && montagePlayer.IsPlaying) return true;
            return false;
        }
    }
    
    /// <summary>
    /// 현재 MotionSet
    /// </summary>
    public MotionSet CurrentMotionSet => currentMotionSet;
    
    /// <summary>
    /// 현재 재생 중인 모션
    /// </summary>
    public MotionData CurrentMotion => currentMotion;
    
    /// <summary>
    /// 현재 Sequential 인덱스
    /// </summary>
    public int CurrentSequentialIndex => sequentialIndex;
}
