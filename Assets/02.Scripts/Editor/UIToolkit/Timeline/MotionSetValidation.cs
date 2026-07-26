using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor.UIToolkit.Timeline
{
    internal enum MotionValidationSeverity
    {
        Info,
        Warning,
        Error,
    }

    internal readonly struct MotionValidationIssue
    {
        public readonly MotionValidationSeverity severity;
        public readonly string code;
        public readonly string message;

        public MotionValidationIssue(
            MotionValidationSeverity severity,
            string code,
            string message)
        {
            this.severity = severity;
            this.code = code;
            this.message = message;
        }
    }

    /// <summary>
    /// 에셋을 저장하지 않고 MotionSet의 구조 오류를 검사한다.
    /// ID 보정은 사용자가 명시적으로 요청했을 때만 Undo와 함께 적용한다.
    /// </summary>
    internal static class MotionSetValidation
    {
        public static void Collect(MotionSet set, List<MotionValidationIssue> results)
        {
            results.Clear();
            if (set == null)
            {
                Add(results, MotionValidationSeverity.Error, "SET_NULL", "MotionSet 데이터가 없습니다.");
                return;
            }

            var motionIds = new HashSet<string>();
            ValidateMotions(set.motions, "Base", motionIds, results);
            if (set.layers != null)
            {
                var layerIds = new HashSet<string>();
                for (int i = 0; i < set.layers.Count; i++)
                {
                    MotionLayer layer = set.layers[i];
                    if (layer == null)
                    {
                        Add(results, MotionValidationSeverity.Error, "LAYER_NULL", $"레이어 {i}가 null입니다.");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(layer.channelId) && !layerIds.Add(layer.channelId))
                        Add(results, MotionValidationSeverity.Error, "CHANNEL_DUPLICATE",
                            $"채널 ID '{layer.channelId}'가 중복됩니다.");
                    ValidateMotions(layer.motions, $"Layer {i}", motionIds, results);
                    ValidateEvents(set, layer.globalEvents, $"Layer {i}/Global", results);
                }
            }

            ValidateEvents(set, set.globalEvents, "Global", results);
            ValidateMotionEvents(set, set.motions, "Base", results);
            if (set.layers != null)
                for (int i = 0; i < set.layers.Count; i++)
                    ValidateMotionEvents(set, set.layers[i]?.motions, $"Layer {i}", results);

            ValidateSections(set, results);
            ValidateCurves(set, motionIds, results);
        }

        public static int RepairStableIds(MotionSet set, UnityEngine.Object undoTarget)
        {
            if (set == null)
                return 0;

            if (undoTarget != null)
                Undo.RegisterCompleteObjectUndo(undoTarget, "Repair MotionSet Stable IDs");

            int repaired = 0;
            var used = new HashSet<string>();
            repaired += RepairMotionIds(set.motions, used);
            if (set.layers != null)
                foreach (MotionLayer layer in set.layers)
                    repaired += RepairMotionIds(layer?.motions, used);

            used.Clear();
            if (set.sections != null)
            {
                foreach (MotionSection section in set.sections)
                {
                    if (section == null)
                        continue;
                    if (!string.IsNullOrEmpty(section.id) && used.Add(section.id))
                        continue;
                    section.id = NewId("section", used);
                    repaired++;
                }
            }

            used.Clear();
            repaired += RepairMarkerIds(set.motions, used);
            if (set.layers != null)
                foreach (MotionLayer layer in set.layers)
                    repaired += RepairMarkerIds(layer?.motions, used);

            if (set.schemaVersion < MotionSet.CurrentSchemaVersion)
            {
                set.schemaVersion = MotionSet.CurrentSchemaVersion;
                repaired++;
            }

            if (repaired > 0 && undoTarget != null)
                EditorUtility.SetDirty(undoTarget);
            return repaired;
        }

        static void ValidateMotions(
            List<Motion> motions,
            string scope,
            HashSet<string> ids,
            List<MotionValidationIssue> results)
        {
            if (motions == null)
                return;
            for (int i = 0; i < motions.Count; i++)
            {
                Motion motion = motions[i];
                if (motion == null)
                {
                    Add(results, MotionValidationSeverity.Error, "MOTION_NULL",
                        $"{scope} 모션 {i}가 null입니다.");
                    continue;
                }
                if (motion.motionClip == null)
                    Add(results, MotionValidationSeverity.Error, "CLIP_MISSING",
                        $"{scope} 모션 {i}의 AnimationClip이 없습니다.");
                if (string.IsNullOrEmpty(motion.id))
                    Add(results, MotionValidationSeverity.Warning, "MOTION_ID_MISSING",
                        $"{scope} 모션 {i}에 안정 ID가 없습니다.");
                else if (!ids.Add(motion.id))
                    Add(results, MotionValidationSeverity.Error, "MOTION_ID_DUPLICATE",
                        $"모션 ID '{motion.id}'가 중복됩니다.");
                if (motion.playbackSpeed <= 0f)
                    Add(results, MotionValidationSeverity.Error, "PLAYBACK_SPEED",
                        $"{scope} 모션 {i}의 재생 속도는 0보다 커야 합니다.");
                if (motion.motionClip != null && motion.ClipEndTime <= motion.ClipStartTime)
                    Add(results, MotionValidationSeverity.Error, "CLIP_RANGE",
                        $"{scope} 모션 {i}의 클립 구간이 비어 있습니다.");

                var markerIds = new HashSet<string>();
                if (motion.markers == null)
                    continue;
                foreach (MotionMarker marker in motion.markers)
                {
                    if (marker == null)
                        continue;
                    if (string.IsNullOrEmpty(marker.id))
                        Add(results, MotionValidationSeverity.Warning, "MARKER_ID_MISSING",
                            $"{scope} 모션 {i}의 마커에 안정 ID가 없습니다.");
                    else if (!markerIds.Add(marker.id))
                        Add(results, MotionValidationSeverity.Error, "MARKER_ID_DUPLICATE",
                            $"{scope} 모션 {i}의 마커 ID '{marker.id}'가 중복됩니다.");
                }
            }
        }

        static void ValidateMotionEvents(
            MotionSet set,
            List<Motion> motions,
            string scope,
            List<MotionValidationIssue> results)
        {
            if (motions == null)
                return;
            for (int i = 0; i < motions.Count; i++)
                ValidateEvents(set, motions[i]?.events, $"{scope}/Motion {i}", results);
        }

        static void ValidateEvents(
            MotionSet set,
            List<MotionEventBase> events,
            string scope,
            List<MotionValidationIssue> results)
        {
            if (events == null)
                return;
            for (int i = 0; i < events.Count; i++)
            {
                MotionEventBase motionEvent = events[i];
                if (motionEvent == null)
                {
                    Add(results, MotionValidationSeverity.Error, "EVENT_NULL",
                        $"{scope} 이벤트 {i}의 managed reference가 없습니다.");
                    continue;
                }
                if (!MotionTimelineResolver.TryGetEventGlobalRange(
                        set, motionEvent, out float start, out float end))
                {
                    Add(results, MotionValidationSeverity.Error, "EVENT_LINK_BROKEN",
                        $"{scope} 이벤트 {i}의 시간 링크를 해석할 수 없습니다.");
                    continue;
                }
                if (start < 0f || end < start || end > set.TotalDuration + 0.001f)
                    Add(results, MotionValidationSeverity.Error, "EVENT_RANGE",
                        $"{scope} 이벤트 {i}의 범위({start:0.###}~{end:0.###})가 유효하지 않습니다.");
            }
        }

        static void ValidateSections(MotionSet set, List<MotionValidationIssue> results)
        {
            if (set.sections == null)
                return;
            var ids = new HashSet<string>();
            float previous = -1f;
            foreach (MotionSection section in set.sections)
            {
                if (section == null)
                {
                    Add(results, MotionValidationSeverity.Error, "SECTION_NULL", "Section이 null입니다.");
                    continue;
                }
                if (string.IsNullOrEmpty(section.id))
                    Add(results, MotionValidationSeverity.Warning, "SECTION_ID_MISSING",
                        $"Section '{section.displayName}'에 안정 ID가 없습니다.");
                else if (!ids.Add(section.id))
                    Add(results, MotionValidationSeverity.Error, "SECTION_ID_DUPLICATE",
                        $"Section ID '{section.id}'가 중복됩니다.");
                if (section.startTime < 0f || section.startTime >= set.TotalDuration)
                    Add(results, MotionValidationSeverity.Error, "SECTION_RANGE",
                        $"Section '{section.displayName}' 시작 시간이 범위를 벗어났습니다.");
                if (section.startTime < previous)
                    Add(results, MotionValidationSeverity.Warning, "SECTION_ORDER",
                        "Section 목록이 시작 시간 순서가 아닙니다.");
                previous = section.startTime;
            }

            foreach (MotionSection section in set.sections)
            {
                if (section == null || string.IsNullOrEmpty(section.defaultNextId))
                    continue;
                if (!ids.Contains(section.defaultNextId))
                    Add(results, MotionValidationSeverity.Error, "SECTION_NEXT_BROKEN",
                        $"Section '{section.displayName}'의 다음 Section을 찾을 수 없습니다.");
            }
        }

        static void ValidateCurves(
            MotionSet set,
            HashSet<string> motionIds,
            List<MotionValidationIssue> results)
        {
            if (set.curves == null)
                return;
            foreach (MotionCurveTrack track in set.curves)
            {
                if (track == null)
                {
                    Add(results, MotionValidationSeverity.Error, "CURVE_NULL", "Curve Track이 null입니다.");
                    continue;
                }
                if (track.curve == null)
                    Add(results, MotionValidationSeverity.Error, "CURVE_MISSING",
                        $"Curve '{track.displayName}'에 AnimationCurve가 없습니다.");
                if (!string.IsNullOrEmpty(track.targetId) &&
                    track.channel != MotionCurveChannel.LayerWeight &&
                    !motionIds.Contains(track.targetId))
                    Add(results, MotionValidationSeverity.Warning, "CURVE_TARGET",
                        $"Curve '{track.displayName}'의 대상 '{track.targetId}'을 찾을 수 없습니다.");
            }
        }

        static int RepairMotionIds(List<Motion> motions, HashSet<string> used)
        {
            int count = 0;
            if (motions == null)
                return count;
            foreach (Motion motion in motions)
            {
                if (motion == null)
                    continue;
                if (!string.IsNullOrEmpty(motion.id) && used.Add(motion.id))
                    continue;
                motion.id = NewId("motion", used);
                count++;
            }
            return count;
        }

        static int RepairMarkerIds(List<Motion> motions, HashSet<string> used)
        {
            int count = 0;
            if (motions == null)
                return count;
            foreach (Motion motion in motions)
            {
                if (motion?.markers == null)
                    continue;
                foreach (MotionMarker marker in motion.markers)
                {
                    if (marker == null)
                        continue;
                    if (!string.IsNullOrEmpty(marker.id) && used.Add(marker.id))
                        continue;
                    marker.id = NewId("marker", used);
                    count++;
                }
            }
            return count;
        }

        static string NewId(string prefix, HashSet<string> used)
        {
            string id;
            do id = $"{prefix}_{Guid.NewGuid():N}";
            while (!used.Add(id));
            return id;
        }

        static void Add(
            List<MotionValidationIssue> results,
            MotionValidationSeverity severity,
            string code,
            string message)
        {
            results.Add(new MotionValidationIssue(severity, code, message));
        }
    }
}
