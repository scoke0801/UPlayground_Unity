using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation
{
    [Serializable]
    public class Motion
    {
        public string motionName;
        public AnimationClip motionClip;

        // ── 재생 구간 (클립 로컬 시간 기준) ──
        // -1이면 클립 전체 시작/끝을 사용
        public float clipStartTime = -1f;
        public float clipEndTime   = -1f;

        // ── 개별 재생 속도 배율 ──
        public float playbackSpeed = 1f;

        // 타입 안전한 이벤트 리스트
        [SerializeReference]
        public List<MotionEventBase> events = new List<MotionEventBase>();

        // ── 유효성 검사 ──
        public bool IsValid() => motionClip != null;

        /// <summary>클립 내 실제 재생 시작 시간</summary>
        public float ClipStartTime => (clipStartTime >= 0f && motionClip != null)
            ? Mathf.Clamp(clipStartTime, 0f, motionClip.length)
            : 0f;

        /// <summary>클립 내 실제 재생 종료 시간</summary>
        public float ClipEndTime => (clipEndTime >= 0f && motionClip != null)
            ? Mathf.Clamp(clipEndTime, ClipStartTime, motionClip.length)
            : (motionClip != null ? motionClip.length : 0f);

        /// <summary>타임라인 상 이 모션이 차지하는 시간 (재생 구간 / 재생 속도)</summary>
        public float Duration
        {
            get
            {
                if (motionClip == null) return 0f;
                float clipDur = ClipEndTime - ClipStartTime;
                float spd     = playbackSpeed > 0f ? playbackSpeed : 1f;
                return clipDur / spd;
            }
        }
        
        /// <summary>
        /// 특정 시간에 활성화된 이벤트들 반환
        /// </summary>
        public List<MotionEventBase> GetActiveEventsAt(float time)
        {
            var activeEvents = new List<MotionEventBase>();
            if (events == null) return activeEvents;
            
            foreach (var evt in events)
            {
                if (evt != null && evt.IsActiveAt(time))
                    activeEvents.Add(evt);
            }
            
            return activeEvents;
        }
        
        /// <summary>
        /// 특정 타입의 이벤트만 필터링
        /// </summary>
        public List<T> GetEventsByType<T>() where T : MotionEventBase
        {
            var result = new List<T>();
            if (events == null) return result;
            
            foreach (var evt in events)
            {
                if (evt is T typedEvent)
                    result.Add(typedEvent);
            }
            
            return result;
        }
    }
    
    [Serializable]
    public class MotionSet
    {
        public string motionSetName;
        public List<Motion> motions = new List<Motion>();
        
        // 모션 셋 전체 이벤트 (모든 모션에 걸쳐 적용되는 이벤트)
        [SerializeReference]
        public List<MotionEventBase> globalEvents = new List<MotionEventBase>();
        
        // 전체 재생 시간
        public float TotalDuration
        {
            get
            {
                float total = 0f;
                if (motions == null) return total;
                
                foreach (var motion in motions)
                {
                    if (motion != null)
                        total += motion.Duration;
                }
                return total;
            }
        }
        
        // 유효성 검사
        public bool IsValid() => motions != null && motions.Count > 0;
        
        /// <summary>
        /// 전체 타임라인에서 특정 시간에 활성화된 이벤트 반환
        /// </summary>
        public List<MotionEventBase> GetActiveEventsAt(float globalTime)
        {
            var activeEvents = new List<MotionEventBase>();
            
            // 글로벌 이벤트 체크
            if (globalEvents != null)
            {
                foreach (var evt in globalEvents)
                {
                    if (evt != null && evt.IsActiveAt(globalTime))
                        activeEvents.Add(evt);
                }
            }
            
            // 각 모션의 이벤트 체크
            float currentTime = 0f;
            if (motions != null)
            {
                foreach (var motion in motions)
                {
                    if (motion == null) continue;
                    
                    float motionEnd = currentTime + motion.Duration;
                    if (globalTime >= currentTime && globalTime <= motionEnd)
                    {
                        float localTime = globalTime - currentTime;
                        activeEvents.AddRange(motion.GetActiveEventsAt(localTime));
                    }
                    
                    currentTime = motionEnd;
                    if (globalTime < currentTime) break;
                }
            }
            
            return activeEvents;
        }
        
        /// <summary>
        /// 특정 모션의 인덱스와 로컬 타임 계산
        /// </summary>
        public bool GetMotionAtTime(float globalTime, out int motionIndex, out float localTime)
        {
            motionIndex = -1;
            localTime = 0f;
            
            if (motions == null) return false;
            
            float currentTime = 0f;
            for (int i = 0; i < motions.Count; i++)
            {
                var motion = motions[i];
                if (motion == null) continue;
                
                float motionEnd = currentTime + motion.Duration;
                if (globalTime >= currentTime && globalTime <= motionEnd)
                {
                    motionIndex = i;
                    localTime = globalTime - currentTime;
                    return true;
                }
                
                currentTime = motionEnd;
            }
            
            return false;
        }
    }
}