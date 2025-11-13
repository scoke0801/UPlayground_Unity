using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Animation
{
    /// <summary>
    /// 언리얼 엔진의 Animation Montage와 유사한 시스템
    /// 여러 애니메이션 섹션을 조합하고 동적으로 제어
    /// </summary>
    [CreateAssetMenu(fileName = "NewMontage", menuName = "Animation/Animation Montage")]
    public class AnimationMontage : ScriptableObject
    {
        [Header("Montage Settings")]
        [SerializeField] private string montageName;
        [SerializeField] private string defaultSlot = "DefaultSlot";
        [SerializeField] private float blendInTime = 0.25f;
        [SerializeField] private float blendOutTime = 0.25f;
        
        [Header("Sections")]
        [SerializeField] private List<MontageSection> sections = new List<MontageSection>();
        
        [Header("Events")]
        [SerializeField] private List<MontageEvent> events = new List<MontageEvent>();

        public string MontageName => montageName;
        public string DefaultSlot => defaultSlot;
        public float BlendInTime => blendInTime;
        public float BlendOutTime => blendOutTime;
        public List<MontageSection> Sections => sections;
        public List<MontageEvent> Events => events;

        /// <summary>
        /// 특정 섹션 찾기
        /// </summary>
        public MontageSection GetSection(string sectionName)
        {
            return sections.Find(s => s.SectionName == sectionName);
        }

        /// <summary>
        /// 전체 Montage 재생 시간
        /// </summary>
        public float GetTotalDuration()
        {
            float totalTime = 0f;
            foreach (var section in sections)
            {
                totalTime += section.GetDuration();
            }
            return totalTime;
        }

        /// <summary>
        /// 특정 시간에 발생해야 하는 이벤트 찾기
        /// </summary>
        public List<MontageEvent> GetEventsAtTime(float time)
        {
            return events.FindAll(e => Mathf.Approximately(e.TriggerTime, time));
        }
    }

    /// <summary>
    /// Montage 섹션 - 애니메이션의 특정 부분을 정의
    /// </summary>
    [Serializable]
    public class MontageSection
    {
        [SerializeField] private string sectionName;
        [SerializeField] private AnimationClip clip;
        [SerializeField] private float startTime;
        [SerializeField] private float playbackSpeed = 1f;
        [SerializeField] private bool loop = false;
        [SerializeField] private int loopCount = 1; // -1은 무한 반복
        [SerializeField] private string nextSection; // 다음 섹션으로 자동 전환

        public string SectionName => sectionName;
        public AnimationClip Clip => clip;
        public float StartTime => startTime;
        public float PlaybackSpeed => playbackSpeed;
        public bool Loop => loop;
        public int LoopCount => loopCount;
        public string NextSection => nextSection;

        /// <summary>
        /// 섹션의 재생 시간
        /// </summary>
        public float GetDuration()
        {
            if (clip == null) return 0f;
            return clip.length / playbackSpeed;
        }
    }

    /// <summary>
    /// Montage 이벤트 - 특정 시간에 발생하는 이벤트
    /// </summary>
    [Serializable]
    public class MontageEvent
    {
        [SerializeField] private string eventName;
        [SerializeField] private float triggerTime;
        [SerializeField] private string eventParameter;

        public string EventName => eventName;
        public float TriggerTime => triggerTime;
        public string EventParameter => eventParameter;
    }

    /// <summary>
    /// Montage 재생 완료 델리게이트
    /// </summary>
    public delegate void MontageEndedDelegate(AnimationMontage montage);
    
    /// <summary>
    /// Montage 이벤트 델리게이트
    /// </summary>
    public delegate void MontageEventDelegate(string eventName, string parameter);
}
