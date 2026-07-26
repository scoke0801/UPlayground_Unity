using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation
{
    /// <summary>
    /// MotionSet의 Section과 이벤트 시간을 해석한다.
    /// </summary>
    public static class MotionTimelineResolver
    {
        public static bool TryValidateSectionLayout(MotionSet set, out string error)
        {
            if (set == null)
            {
                error = "MotionSet 데이터가 없습니다.";
                return false;
            }
            if (set.schemaVersion != MotionSet.CurrentSchemaVersion)
            {
                error =
                    $"지원하지 않는 MotionSet 스키마입니다: {set.schemaVersion} " +
                    $"(필요: {MotionSet.CurrentSchemaVersion})";
                return false;
            }
            if (set.TotalDuration <= 0f)
            {
                error = "MotionSet 재생 시간이 0입니다.";
                return false;
            }
            if (set.sections == null || set.sections.Count == 0)
            {
                error = "Section이 없습니다.";
                return false;
            }

            var ids = new HashSet<string>();
            var startTimes = new HashSet<float>();
            bool startsAtZero = false;
            foreach (MotionSection section in set.sections)
            {
                if (section == null)
                {
                    error = "null Section이 있습니다.";
                    return false;
                }
                if (string.IsNullOrEmpty(section.id) || !ids.Add(section.id))
                {
                    error = $"Section ID가 없거나 중복됩니다: '{section.displayName}'";
                    return false;
                }
                if (float.IsNaN(section.startTime) ||
                    float.IsInfinity(section.startTime) ||
                    section.startTime < 0f ||
                    section.startTime >= set.TotalDuration)
                {
                    error = $"Section 시작 시간이 범위를 벗어납니다: '{section.displayName}'";
                    return false;
                }
                float canonicalStartTime = Mathf.Round(section.startTime * 10000f) / 10000f;
                if (!startTimes.Add(canonicalStartTime))
                {
                    error = $"같은 시작 시간의 Section이 중복됩니다: {section.startTime:0.####}초";
                    return false;
                }
                if (Mathf.Abs(section.startTime) <= 0.0001f)
                    startsAtZero = true;
            }

            if (!startsAtZero)
            {
                error = "0초에서 시작하는 Section이 없습니다.";
                return false;
            }

            foreach (MotionSection section in set.sections)
            {
                if (!string.IsNullOrEmpty(section.defaultNextId) &&
                    !ids.Contains(section.defaultNextId))
                {
                    error = $"다음 Section을 찾을 수 없습니다: '{section.defaultNextId}'";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public static bool TryGetSection(
            MotionSet set,
            string sectionId,
            out MotionSectionRange range)
        {
            range = default;
            if (set == null || string.IsNullOrEmpty(sectionId) || set.sections == null)
                return false;

            foreach (MotionSection section in set.sections)
            {
                if (section == null || section.id != sectionId)
                    continue;

                float start = Mathf.Clamp(section.startTime, 0f, set.TotalDuration);
                float end = FindNextSectionStart(set, start);
                range = new MotionSectionRange(section, start, end);
                return true;
            }
            return false;
        }

        public static bool TryGetSectionAtTime(
            MotionSet set,
            float time,
            out MotionSectionRange range)
        {
            range = default;
            if (set?.sections == null || set.sections.Count == 0)
                return false;

            MotionSection selected = null;
            float selectedStart = float.MinValue;
            foreach (MotionSection section in set.sections)
            {
                if (section == null || section.startTime > time || section.startTime < selectedStart)
                    continue;
                selected = section;
                selectedStart = section.startTime;
            }

            return selected != null && TryGetSection(set, selected.id, out range);
        }

        public static string ResolveDefaultNextSectionId(MotionSet set, MotionSection section)
        {
            if (set == null || section == null)
                return null;
            if (!string.IsNullOrEmpty(section.defaultNextId))
                return section.defaultNextId;

            MotionSection next = null;
            float nextStart = float.MaxValue;
            if (set.sections != null)
            {
                foreach (MotionSection candidate in set.sections)
                {
                    if (candidate == null ||
                        candidate.startTime <= section.startTime ||
                        candidate.startTime >= nextStart)
                        continue;
                    next = candidate;
                    nextStart = candidate.startTime;
                }
            }
            return next?.id;
        }

        public static bool TryGetEventGlobalRange(
            MotionSet set,
            MotionEventBase motionEvent,
            out float globalStart,
            out float globalEnd)
        {
            globalStart = 0f;
            globalEnd = 0f;
            if (set == null || motionEvent == null)
                return false;

            if (set.globalEvents != null && set.globalEvents.Contains(motionEvent))
                return ResolveEventRange(set, motionEvent, null, 0f, true, out globalStart, out globalEnd);

            if (TryFindEventInMotions(set.motions, motionEvent, out Motion owner, out float ownerOffset))
                return ResolveEventRange(set, motionEvent, owner, ownerOffset, false, out globalStart, out globalEnd);

            if (set.layers != null)
            {
                foreach (MotionLayer layer in set.layers)
                {
                    if (layer == null)
                        continue;
                    if (layer.globalEvents != null && layer.globalEvents.Contains(motionEvent))
                        return ResolveEventRange(set, motionEvent, null, 0f, true, out globalStart, out globalEnd);
                    if (TryFindEventInMotions(layer.motions, motionEvent, out owner, out ownerOffset))
                        return ResolveEventRange(set, motionEvent, owner, ownerOffset, false, out globalStart, out globalEnd);
                }
            }

            return false;
        }

        public static void CollectEventsInRange(
            MotionSet set,
            float startTime,
            float endTime,
            List<MotionEventBase> results)
        {
            if (set == null || results == null)
                return;
            results.Clear();
            CollectEventListInRange(set, set.globalEvents, startTime, endTime, results);
            CollectMotionEventsInRange(set, set.motions, startTime, endTime, results);
            CollectLayerEventsInRange(set, startTime, endTime, results);
            results.Sort((left, right) =>
            {
                TryGetEventGlobalRange(set, left, out float leftStart, out _);
                TryGetEventGlobalRange(set, right, out float rightStart, out _);
                int timeOrder = leftStart.CompareTo(rightStart);
                return timeOrder != 0
                    ? timeOrder
                    : left.executionOrder.CompareTo(right.executionOrder);
            });
        }

        public static void CollectActiveEvents(
            MotionSet set,
            float time,
            List<MotionEventBase> results)
        {
            if (set == null || results == null)
                return;
            results.Clear();
            CollectActiveEventList(set, set.globalEvents, time, results);
            CollectActiveMotionEvents(set, set.motions, time, results);
            if (set.layers != null)
                foreach (MotionLayer layer in set.layers)
                {
                    if (!ShouldVisitLayerEvents(layer))
                        continue;
                    CollectActiveEventList(set, layer.globalEvents, time, results);
                    CollectActiveMotionEvents(set, layer.motions, time, results);
                }
        }

        public static bool TryFindMotion(
            MotionSet set,
            string motionId,
            out Motion motion,
            out float offset)
        {
            offset = 0f;
            if (set == null || string.IsNullOrEmpty(motionId))
            {
                motion = null;
                return false;
            }

            if (TryFindMotionInList(set.motions, motionId, out motion, out offset))
                return true;
            if (set.layers != null)
            {
                foreach (MotionLayer layer in set.layers)
                    if (layer != null &&
                        TryFindMotionInList(layer.motions, motionId, out motion, out offset))
                        return true;
            }

            motion = null;
            offset = 0f;
            return false;
        }

        public static float ResolveSynchronizedTime(
            MotionSet leader,
            MotionLayer follower,
            float leaderTime)
        {
            if (leader == null || follower == null || follower.TotalDuration <= 0f)
                return leaderTime;
            if (follower.sync == null ||
                follower.sync.role != MotionSyncRole.Follower ||
                string.IsNullOrEmpty(follower.sync.groupId) ||
                leader.sync == null ||
                leader.sync.groupId != follower.sync.groupId)
                return leaderTime;

            bool hasPrevious = false;
            bool hasNext = false;
            float previousLeader = 0f;
            float previousFollower = 0f;
            float nextLeader = 0f;
            float nextFollower = 0f;
            float leaderOffset = 0f;
            if (leader.motions != null)
            {
                foreach (Motion motion in leader.motions)
                {
                    if (motion?.markers != null)
                        foreach (MotionMarker marker in motion.markers)
                        {
                            if (marker == null || string.IsNullOrEmpty(marker.id) ||
                                !TryGetMarkerTime(
                                    follower.motions,
                                    marker.id,
                                    out float followerTime))
                                continue;
                            float markerTime =
                                leaderOffset + marker.normalizedTime * motion.Duration;
                            if (markerTime <= leaderTime &&
                                (!hasPrevious || markerTime > previousLeader))
                            {
                                hasPrevious = true;
                                previousLeader = markerTime;
                                previousFollower = followerTime;
                            }
                            if (markerTime > leaderTime &&
                                (!hasNext || markerTime < nextLeader))
                            {
                                hasNext = true;
                                nextLeader = markerTime;
                                nextFollower = followerTime;
                            }
                        }
                    leaderOffset += motion?.Duration ?? 0f;
                }
            }

            if (hasPrevious && hasNext && nextLeader > previousLeader)
                return Mathf.Lerp(
                    previousFollower,
                    nextFollower,
                    Mathf.InverseLerp(previousLeader, nextLeader, leaderTime));
            if (hasPrevious)
                return Mathf.Clamp(
                    previousFollower + (leaderTime - previousLeader),
                    0f,
                    follower.TotalDuration);
            if (hasNext)
                return Mathf.Clamp(
                    nextFollower - (nextLeader - leaderTime),
                    0f,
                    follower.TotalDuration);
            return follower.sync.fallback == MotionSyncFallback.NormalizedTime
                ? Mathf.Clamp01(leaderTime / Mathf.Max(0.0001f, leader.TotalDuration)) *
                  follower.TotalDuration
                : leaderTime;
        }

        public static float EvaluateTimeStretchRate(
            MotionSet set,
            float globalTime,
            float requestedRate)
        {
            requestedRate = Mathf.Max(0.01f, requestedRate);
            MotionTimeStretchSettings settings = set?.timeStretch;
            if (settings == null || !settings.enabled)
                return requestedRate;

            float clamped = Mathf.Clamp(
                requestedRate,
                Mathf.Max(0.01f, settings.minimumRate),
                Mathf.Max(settings.minimumRate, settings.maximumRate));
            if (!settings.protectImpact)
                return clamped;

            float offset = 0f;
            if (set.motions != null)
            {
                foreach (Motion motion in set.motions)
                {
                    if (motion?.markers != null)
                        foreach (MotionMarker marker in motion.markers)
                        {
                            if (marker == null || marker.kind != MotionMarkerKind.Impact)
                                continue;
                            float markerTime = offset + marker.normalizedTime * motion.Duration;
                            if (globalTime >= markerTime - settings.protectionBefore &&
                                globalTime <= markerTime + settings.protectionAfter)
                                return 1f;
                        }
                    offset += motion?.Duration ?? 0f;
                }
            }
            return clamped;
        }

        static bool ResolveEventRange(
            MotionSet set,
            MotionEventBase motionEvent,
            Motion owner,
            float ownerOffset,
            bool globalEvent,
            out float globalStart,
            out float globalEnd)
        {
            MotionEventTimeLink link = motionEvent.timeLink;
            if (!link.enabled)
            {
                float offset = globalEvent ? 0f : ownerOffset;
                globalStart = offset + motionEvent.startTime;
                globalEnd = offset + motionEvent.endTime;
                return true;
            }

            Motion linkedMotion = owner;
            float linkedOffset = ownerOffset;
            if (!string.IsNullOrEmpty(link.linkedMotionId) &&
                !TryFindMotion(set, link.linkedMotionId, out linkedMotion, out linkedOffset))
            {
                globalStart = 0f;
                globalEnd = 0f;
                return false;
            }

            switch (link.mode)
            {
                case MotionEventLinkMode.Absolute:
                    globalStart = link.startValue;
                    globalEnd = link.endValue;
                    return true;

                case MotionEventLinkMode.Relative:
                    globalStart = linkedOffset + link.startValue;
                    globalEnd = linkedOffset + link.endValue;
                    return true;

                case MotionEventLinkMode.Proportional:
                    float duration = linkedMotion != null ? linkedMotion.Duration : set.TotalDuration;
                    globalStart = linkedOffset + Mathf.Clamp01(link.startValue) * duration;
                    globalEnd = linkedOffset + Mathf.Clamp01(link.endValue) * duration;
                    return true;

                case MotionEventLinkMode.Marker:
                    if (!TryFindMarker(linkedMotion, link.markerId, out MotionMarker marker))
                    {
                        globalStart = 0f;
                        globalEnd = 0f;
                        return false;
                    }
                    float markerTime = linkedOffset + marker.normalizedTime * linkedMotion.Duration;
                    globalStart = markerTime + link.startValue;
                    globalEnd = markerTime + link.endValue;
                    return true;

                default:
                    globalStart = 0f;
                    globalEnd = 0f;
                    return false;
            }
        }

        static float FindNextSectionStart(MotionSet set, float currentStart)
        {
            float end = set.TotalDuration;
            foreach (MotionSection candidate in set.sections)
            {
                if (candidate != null &&
                    candidate.startTime > currentStart &&
                    candidate.startTime < end)
                    end = candidate.startTime;
            }
            return end;
        }

        static bool TryFindMotionInList(
            List<Motion> source,
            string motionId,
            out Motion motion,
            out float offset)
        {
            offset = 0f;
            if (source != null)
            {
                foreach (Motion candidate in source)
                {
                    if (candidate != null && candidate.id == motionId)
                    {
                        motion = candidate;
                        return true;
                    }
                    offset += candidate?.Duration ?? 0f;
                }
            }
            motion = null;
            return false;
        }

        static bool TryFindEventInMotions(
            List<Motion> source,
            MotionEventBase motionEvent,
            out Motion owner,
            out float offset)
        {
            offset = 0f;
            if (source != null)
            {
                foreach (Motion motion in source)
                {
                    if (motion?.events != null && motion.events.Contains(motionEvent))
                    {
                        owner = motion;
                        return true;
                    }
                    offset += motion?.Duration ?? 0f;
                }
            }
            owner = null;
            return false;
        }

        static bool TryFindMarker(Motion motion, string markerId, out MotionMarker marker)
        {
            if (motion?.markers != null)
            {
                foreach (MotionMarker candidate in motion.markers)
                {
                    if (candidate != null && candidate.id == markerId)
                    {
                        marker = candidate;
                        return true;
                    }
                }
            }
            marker = null;
            return false;
        }

        static void CollectLayerEventsInRange(
            MotionSet set,
            float startTime,
            float endTime,
            List<MotionEventBase> results)
        {
            if (set.layers == null)
                return;
            foreach (MotionLayer layer in set.layers)
            {
                if (!ShouldVisitLayerEvents(layer))
                    continue;
                CollectEventListInRange(set, layer.globalEvents, startTime, endTime, results);
                CollectMotionEventsInRange(set, layer.motions, startTime, endTime, results);
            }
        }

        static bool ShouldVisitLayerEvents(MotionLayer layer)
        {
            return layer != null &&
                   layer.enabled &&
                   (layer.sync == null ||
                    layer.sync.role != MotionSyncRole.Follower ||
                    layer.sync.triggerFollowerEvents);
        }

        static void CollectMotionEventsInRange(
            MotionSet set,
            List<Motion> motions,
            float startTime,
            float endTime,
            List<MotionEventBase> results)
        {
            if (motions == null)
                return;
            foreach (Motion motion in motions)
                if (motion != null)
                    CollectEventListInRange(
                        set,
                        motion.events,
                        startTime,
                        endTime,
                        results);
        }

        static void CollectEventListInRange(
            MotionSet set,
            List<MotionEventBase> events,
            float startTime,
            float endTime,
            List<MotionEventBase> results)
        {
            if (events == null)
                return;
            foreach (MotionEventBase motionEvent in events)
                if (motionEvent != null &&
                    TryGetEventGlobalRange(set, motionEvent, out float start, out _) &&
                    start >= startTime &&
                    start <= endTime)
                    results.Add(motionEvent);
        }

        static void CollectActiveMotionEvents(
            MotionSet set,
            List<Motion> motions,
            float time,
            List<MotionEventBase> results)
        {
            if (motions == null)
                return;
            foreach (Motion motion in motions)
                if (motion != null)
                    CollectActiveEventList(set, motion.events, time, results);
        }

        static void CollectActiveEventList(
            MotionSet set,
            List<MotionEventBase> events,
            float time,
            List<MotionEventBase> results)
        {
            if (events == null)
                return;
            foreach (MotionEventBase motionEvent in events)
                if (motionEvent != null &&
                    TryGetEventGlobalRange(set, motionEvent, out float start, out float end) &&
                    time >= start &&
                    time <= end)
                    results.Add(motionEvent);
        }

        static bool TryGetMarkerTime(
            List<Motion> motions,
            string markerId,
            out float markerTime)
        {
            float offset = 0f;
            if (motions != null)
                foreach (Motion motion in motions)
                {
                    if (motion?.markers != null)
                        foreach (MotionMarker marker in motion.markers)
                            if (marker != null && marker.id == markerId)
                            {
                                markerTime = offset + marker.normalizedTime * motion.Duration;
                                return true;
                            }
                    offset += motion?.Duration ?? 0f;
                }
            markerTime = 0f;
            return false;
        }
    }
}
