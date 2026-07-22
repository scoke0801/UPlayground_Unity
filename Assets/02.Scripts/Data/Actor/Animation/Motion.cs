using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation
{
    public enum MotionLayerBlendMode
    {
        Override,
        Additive,
    }

    [Serializable]
    public class MotionLayer
    {
        public string layerName = "Animation Layer";
        public bool enabled = true;
        [Min(1)] public int animancerLayerIndex = 1;
        public AvatarMask avatarMask;
        public MotionLayerBlendMode blendMode = MotionLayerBlendMode.Override;
        [Range(0f, 1f)] public float weight = 1f;
        public bool holdLastFrame = true;
        public List<Motion> motions = new List<Motion>();

        [SerializeReference]
        public List<MotionEventBase> globalEvents = new List<MotionEventBase>();

        public float TotalDuration
        {
            get
            {
                float total = 0f;
                if (motions == null) return total;
                foreach (Motion motion in motions)
                    if (motion != null)
                        total += motion.Duration;
                return total;
            }
        }

        public bool IsValid()
        {
            if (!enabled || motions == null)
                return false;
            foreach (Motion motion in motions)
                if (motion != null && motion.IsValid())
                    return true;
            return false;
        }

        public bool GetMotionAtTime(float time, out int motionIndex, out float localTime)
        {
            motionIndex = -1;
            localTime = 0f;
            if (motions == null)
                return false;

            float current = 0f;
            for (int i = 0; i < motions.Count; i++)
            {
                Motion motion = motions[i];
                if (motion == null || !motion.IsValid())
                    continue;
                float end = current + motion.Duration;
                if (time >= current && time <= end)
                {
                    motionIndex = i;
                    localTime = time - current;
                    return true;
                }
                current = end;
            }
            return false;
        }

        public List<MotionEventBase> GetEventsInRange(float startTime, float endTime)
        {
            var results = new List<MotionEventBase>();
            if (globalEvents != null)
            {
                foreach (MotionEventBase evt in globalEvents)
                    if (evt != null && evt.startTime >= startTime && evt.startTime <= endTime)
                        results.Add(evt);
            }

            float offset = 0f;
            if (motions == null)
                return results;
            foreach (Motion motion in motions)
            {
                if (motion?.events != null)
                {
                    foreach (MotionEventBase evt in motion.events)
                        if (evt != null &&
                            offset + evt.startTime >= startTime &&
                            offset + evt.startTime <= endTime)
                            results.Add(evt);
                }
                offset += motion?.Duration ?? 0f;
            }
            return results;
        }

        public List<MotionEventBase> GetActiveEventsAt(float time)
        {
            var results = new List<MotionEventBase>();
            if (globalEvents != null)
                foreach (MotionEventBase evt in globalEvents)
                    if (evt != null && evt.IsActiveAt(time))
                        results.Add(evt);

            float offset = 0f;
            if (motions == null)
                return results;
            foreach (Motion motion in motions)
            {
                if (motion != null && time >= offset && time <= offset + motion.Duration)
                    results.AddRange(motion.GetActiveEventsAt(time - offset));
                offset += motion?.Duration ?? 0f;
            }
            return results;
        }

        public bool TryGetEventGlobalStart(MotionEventBase evt, out float globalStart)
        {
            globalStart = 0f;
            if (evt == null)
                return false;
            if (globalEvents != null && globalEvents.Contains(evt))
            {
                globalStart = evt.startTime;
                return true;
            }

            float offset = 0f;
            if (motions == null)
                return false;
            foreach (Motion motion in motions)
            {
                if (motion?.events != null && motion.events.Contains(evt))
                {
                    globalStart = offset + evt.startTime;
                    return true;
                }
                offset += motion?.Duration ?? 0f;
            }
            return false;
        }
    }

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

        // Base 타임라인(motions)을 재생할 Animancer 레이어 인덱스.
        // 0 = 레이어0(디렉터, 기존과 동일). 1 이상이면 Base 타임라인 전체를 해당 레이어에
        // 오버레이로 재생한다. 마스크(액터 _upperBodyMask)가 그 레이어에 있으면 마스크된 부위만
        // 움직이고, L0는 다른 MotionSet(예: 하체 로코모션)이 동시에 사용할 수 있다.
        // 병렬 재생 레이어(layers)와는 별개다: 이건 Base 시퀀스 자체를 옮기는 것이지,
        // Base와 동시에 도는 추가 트랙이 아니다.
        [Min(0)] public int baseLayerIndex = 0;

        // 모션 셋 전체 이벤트 (모든 모션에 걸쳐 적용되는 이벤트)
        [SerializeReference]
        public List<MotionEventBase> globalEvents = new List<MotionEventBase>();

        // Base 모션 시퀀스와 같은 시간축에서 병렬 재생되는 Animancer 레이어.
        // 비어 있으면 기존 단일 레이어 MotionSet과 완전히 동일하게 동작한다.
        public List<MotionLayer> layers = new List<MotionLayer>();
        
        // 전체 재생 시간
        public float TotalDuration
        {
            get
            {
                float total = 0f;
                if (motions != null)
                    foreach (var motion in motions)
                        if (motion != null)
                            total += motion.Duration;
                if (layers != null)
                {
                    foreach (MotionLayer layer in layers)
                        if (layer != null && layer.enabled)
                            total = Mathf.Max(total, layer.TotalDuration);
                }
                return total;
            }
        }
        
        // 유효성 검사
        public bool IsValid()
        {
            if (motions != null)
                foreach (Motion motion in motions)
                    if (motion != null && motion.IsValid())
                        return true;
            if (layers != null)
                foreach (MotionLayer layer in layers)
                    if (layer != null && layer.IsValid())
                        return true;
            return false;
        }

        public bool HasPlaybackLayers
        {
            get
            {
                if (layers == null) return false;
                foreach (MotionLayer layer in layers)
                    if (layer != null && layer.IsValid())
                        return true;
                return false;
            }
        }
        
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
                    if (evt != null && evt.IsActiveAtGlobal(globalTime))
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

            if (layers != null)
            {
                foreach (MotionLayer layer in layers)
                {
                    if (layer == null || !layer.enabled)
                        continue;
                    activeEvents.AddRange(layer.GetActiveEventsAt(globalTime));
                }
            }

            return activeEvents;
        }

        /// <summary>
        /// 특정 시간 범위 [start, end] 내에서 시작되는 모든 이벤트 반환 (글로벌 타임라인 기준)
        /// </summary>
        public List<MotionEventBase> GetEventsInRange(float startGlobalTime, float endGlobalTime)
        {
            var results = new List<MotionEventBase>();

            // 글로벌 이벤트 체크
            if (globalEvents != null)
            {
                foreach (var evt in globalEvents)
                {
                    if (evt == null) continue;
                    float absStart = evt.startTime; // globalEvents는 오프셋 0
                    if (absStart >= startGlobalTime && absStart <= endGlobalTime)
                        results.Add(evt);
                }
            }

            // 각 모션의 이벤트 체크
            float accumulated = 0f;
            if (motions != null)
            {
                foreach (var motion in motions)
                {
                    if (motion == null) continue;
                    float motionEnd = accumulated + motion.Duration;

                    // 검색 범위가 이 모션 구간과 겹치는지 확인
                    if (endGlobalTime > accumulated && startGlobalTime < motionEnd)
                    {
                        float localRangeStart = Mathf.Max(0f, startGlobalTime - accumulated);
                        float localRangeEnd = endGlobalTime - accumulated;

                        foreach (var evt in motion.events)
                        {
                            if (evt != null && evt.startTime >= localRangeStart && evt.startTime <= localRangeEnd)
                                results.Add(evt);
                        }
                    }
                    accumulated = motionEnd;
                    if (startGlobalTime > accumulated) continue;
                }
            }

            if (layers != null)
            {
                foreach (MotionLayer layer in layers)
                    if (layer != null && layer.enabled)
                        results.AddRange(layer.GetEventsInRange(startGlobalTime, endGlobalTime));
            }
            return results;
        }

        /// <summary>
        /// 이벤트의 글로벌 시작 시각을 발화 검출(GetEventsInRange)과 동일한 Duration 누적 방식으로 즉석 계산한다.
        /// 캐시된 globalStartTimeOffset 대신 이걸 쓰면 포즈시간(_currentTime)과 항상 같은 기준으로 정렬된다.
        /// </summary>
        public bool TryGetEventGlobalStart(MotionEventBase evt, out float globalStart)
        {
            globalStart = 0f;
            if (evt == null) return false;

            if (globalEvents != null && globalEvents.Contains(evt))
            {
                globalStart = evt.startTime; // globalEvents는 오프셋 0
                return true;
            }

            float accumulated = 0f;
            if (motions != null)
            {
                foreach (var motion in motions)
                {
                    if (motion == null) continue;
                    if (motion.events != null && motion.events.Contains(evt))
                    {
                        globalStart = accumulated + evt.startTime;
                        return true;
                    }
                    accumulated += motion.Duration;
                }
            }

            if (layers != null)
            {
                foreach (MotionLayer layer in layers)
                    if (layer != null && layer.TryGetEventGlobalStart(evt, out globalStart))
                        return true;
            }

            return false;
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
