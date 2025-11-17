using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 애니메이션 몽타주 - 단일 AnimationClip + 섹션 + 노티파이
/// AvatarMask를 사용하여 신체 부위별 재생 제어
/// </summary>
[CreateAssetMenu(fileName = "New Animation Montage", menuName = "Animation/Animation Montage")]
public class AnimationMontage : ScriptableObject
{
    [Header("기본 설정")]
    [SerializeField] private AnimationClip animationClip;
    [SerializeField] private AvatarMask avatarMask; // AnimationSlot 대체
    [SerializeField] private bool isLooping = false;
    
    [Header("섹션")]
    [SerializeField] private List<MontageSection> sections = new List<MontageSection>();
    
    [Header("노티파이")]
    [SerializeField] private List<MontageNotify> notifies = new List<MontageNotify>();
    
    public AnimationClip AnimationClip => animationClip;
    public AvatarMask AvatarMask => avatarMask;
    public bool IsLooping => isLooping;
    public List<MontageSection> Sections => sections;
    public List<MontageNotify> Notifies => notifies;
    
    /// <summary>
    /// 섹션 이름으로 찾기
    /// </summary>
    public MontageSection GetSection(string sectionName)
    {
        return sections.Find(s => s.sectionName == sectionName);
    }
    
    /// <summary>
    /// 특정 시간의 노티파이 가져오기
    /// </summary>
    public List<MontageNotify> GetNotifiesAtTime(float normalizedTime)
    {
        return notifies.FindAll(n => Mathf.Approximately(n.normalizedTime, normalizedTime));
    }
}

/// <summary>
/// 몽타주 섹션 - 애니메이션의 특정 구간
/// </summary>
[Serializable]
public class MontageSection
{
    public string sectionName;
    [Range(0f, 1f)] public float startTime; // 정규화된 시간 (0~1)
    [Range(0f, 1f)] public float endTime;
    public string nextSection; // 다음 섹션 이름 (비어있으면 종료)
}

/// <summary>
/// 몽타주 노티파이 - 타임라인 이벤트
/// </summary>
[Serializable]
public class MontageNotify
{
    public string notifyName;
    [Range(0f, 1f)] public float normalizedTime;
    public string eventParameter; // 이벤트 파라미터 (선택)
    
    // 노티파이 트리거 액션
    public Action<string> OnNotifyTriggered;
}
