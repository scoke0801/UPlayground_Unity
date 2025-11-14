using System;
using System.Collections.Generic;
using UnityEngine;
using Animancer;

namespace Animation
{
    /// <summary>
    /// 언리얼 스타일의 애니메이션 몽타쥬 시스템
    /// 여러 애니메이션을 섹션으로 나누어 제어 가능
    /// </summary>
    [CreateAssetMenu(fileName = "New Animation Montage", menuName = "Animation/Montage")]
    public class AnimationMontage : ScriptableObject
    {
        [Header("Montage Settings")]
        [SerializeField] private string montageName;
        [SerializeField] private string slotName = "DefaultSlot";
        [SerializeField] private List<MontageSection> sections = new List<MontageSection>();
        
        public string MontageName => montageName;
        public string SlotName => slotName;
        public IReadOnlyList<MontageSection> Sections => sections;
        
        /// <summary>
        /// 섹션 이름으로 섹션 찾기
        /// </summary>
        public MontageSection GetSection(string sectionName)
        {
            return sections.Find(s => s.SectionName == sectionName);
        }
        
        /// <summary>
        /// 인덱스로 섹션 가져오기
        /// </summary>
        public MontageSection GetSection(int index)
        {
            return index >= 0 && index < sections.Count ? sections[index] : null;
        }
        
        /// <summary>
        /// 첫 번째 섹션 가져오기
        /// </summary>
        public MontageSection GetFirstSection()
        {
            return sections.Count > 0 ? sections[0] : null;
        }
    }
    
    /// <summary>
    /// 몽타쥬 섹션 - 애니메이션의 특정 구간을 정의
    /// </summary>
    [Serializable]
    public class MontageSection
    {
        [SerializeField] private string sectionName = "Default";
        [SerializeField] private AnimationClip clip;
        [SerializeField] private float fadeInDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.25f;
        [SerializeField] private float playRate = 1f;
        [SerializeField] private bool loop = false;
        [SerializeField] private string nextSection = ""; // 다음 섹션 이름 (비어있으면 자동)
        [SerializeField] private List<MontageNotify> notifies = new List<MontageNotify>();
        
        public string SectionName => sectionName;
        public AnimationClip Clip => clip;
        public float FadeInDuration => fadeInDuration;
        public float FadeOutDuration => fadeOutDuration;
        public float PlayRate => playRate;
        public bool Loop => loop;
        public string NextSection => nextSection;
        public IReadOnlyList<MontageNotify> Notifies => notifies;
    }
    
    /// <summary>
    /// 몽타쥬 노티파이 - 특정 시점에 이벤트 발생
    /// </summary>
    [Serializable]
    public class MontageNotify
    {
        [SerializeField] private string notifyName;
        [SerializeField] private float triggerTime; // 정규화된 시간 (0~1)
        
        public string NotifyName => notifyName;
        public float TriggerTime => triggerTime;
    }
}
