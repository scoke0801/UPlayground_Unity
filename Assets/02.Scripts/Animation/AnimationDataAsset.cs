using UnityEngine;
using Animancer;
using System.Collections.Generic;

/// <summary>
/// Animancer 애니메이션 데이터를 저장하는 ScriptableObject
/// 생성: Assets > Create > Animancer > Animation Data
/// </summary>
[CreateAssetMenu(fileName = "AnimationData", menuName = "Animancer/Animation Data")]
public class AnimationDataAsset : ScriptableObject
{
    [System.Serializable]
    public class ClipData
    {
        [Tooltip("애니메이션 클립")]
        public AnimationClip clip;
        
        [Tooltip("클립 식별 이름")]
        public string clipName;
        
        [Tooltip("기본 재생 속도")]
        [Range(0.1f, 3f)]
        public float defaultSpeed = 1f;
        
        [Tooltip("루프 재생 여부")]
        public bool isLooping = true;
        
        [Tooltip("페이드 인 시간")]
        [Range(0f, 1f)]
        public float fadeInDuration = 0.25f;
    }
    
    [System.Serializable]
    public class TransitionData
    {
        [Tooltip("시작 상태 이름")]
        public string fromState;
        
        [Tooltip("목표 상태 이름")]
        public string toState;
        
        [Tooltip("페이드 지속 시간")]
        [Range(0.1f, 2f)]
        public float fadeDuration = 0.3f;
        
        [Tooltip("페이드 모드")]
        public FadeMode fadeMode = FadeMode.FixedSpeed;
    }
    
    [System.Serializable]
    public class EventData
    {
        [Tooltip("이벤트가 발생할 클립 이름")]
        public string clipName;
        
        [Tooltip("정규화된 시간 (0~1)")]
        [Range(0f, 1f)]
        public float normalizedTime;
        
        [Tooltip("이벤트 이름")]
        public string eventName;
        
        [Tooltip("호출할 메서드 이름")]
        public string methodName;
    }
    
    [System.Serializable]
    public class LayerData
    {
        [Tooltip("레이어 이름")]
        public string layerName;
        
        [Tooltip("레이어 인덱스")]
        public int layerIndex;
        
        [Tooltip("가산 블렌딩 사용")]
        public bool isAdditive = false;
        
        [Tooltip("아바타 마스크")]
        public AvatarMask avatarMask;
        
        [Tooltip("레이어 가중치")]
        [Range(0f, 1f)]
        public float weight = 1f;
    }
    
    [Header("애니메이션 클립")]
    [Tooltip("사용 가능한 모든 애니메이션 클립")]
    public List<ClipData> clips = new List<ClipData>();
    
    [Header("트랜지션")]
    [Tooltip("상태 간 전환 규칙")]
    public List<TransitionData> transitions = new List<TransitionData>();
    
    [Header("이벤트")]
    [Tooltip("애니메이션 이벤트")]
    public List<EventData> events = new List<EventData>();
    
    [Header("레이어")]
    [Tooltip("애니메이션 레이어 설정")]
    public List<LayerData> layers = new List<LayerData>();
    
    /// <summary>
    /// 이름으로 클립 데이터 찾기
    /// </summary>
    public ClipData GetClipData(string clipName)
    {
        return clips.Find(c => c.clipName == clipName);
    }
    
    /// <summary>
    /// 트랜지션 데이터 찾기
    /// </summary>
    public TransitionData GetTransition(string fromState, string toState)
    {
        return transitions.Find(t => t.fromState == fromState && t.toState == toState);
    }
    
    /// <summary>
    /// 특정 클립의 이벤트 목록 가져오기
    /// </summary>
    public List<EventData> GetEventsForClip(string clipName)
    {
        return events.FindAll(e => e.clipName == clipName);
    }
    
    /// <summary>
    /// 레이어 데이터 찾기
    /// </summary>
    public LayerData GetLayer(string layerName)
    {
        return layers.Find(l => l.layerName == layerName);
    }
    
    /// <summary>
    /// 데이터 검증
    /// </summary>
    public void ValidateData()
    {
        // 중복 클립 이름 검사
        HashSet<string> clipNames = new HashSet<string>();
        foreach (var clip in clips)
        {
            if (!clipNames.Add(clip.clipName))
            {
                Debug.LogWarning($"중복된 클립 이름: {clip.clipName}");
            }
        }
        
        // 트랜지션 유효성 검사
        foreach (var transition in transitions)
        {
            if (GetClipData(transition.fromState) == null)
            {
                Debug.LogWarning($"존재하지 않는 상태: {transition.fromState}");
            }
            if (GetClipData(transition.toState) == null)
            {
                Debug.LogWarning($"존재하지 않는 상태: {transition.toState}");
            }
        }
        
        Debug.Log("데이터 검증 완료");
    }
}
