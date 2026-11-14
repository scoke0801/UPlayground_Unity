#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Data.Editor
{
    /// <summary>수작업과 전투 이벤트를 보존하고 생성한 표현 이벤트만 원자적으로 교체한다.</summary>
    public static class WeaponPresentationGenerator
    {
        /// <summary>생성 전에 전체 후보를 검증하므로 실패하면 원본 에셋을 수정하지 않는다.</summary>
        public static List<UltimateTimelineEvent> Build(WeaponPresentationProfileSO profile, float motionDuration)
        {
            if (profile == null)
                throw new ArgumentException("프로필이 없습니다.");
            if (!profile.IsValid(out string error))
                throw new ArgumentException(error);
            if (float.IsNaN(motionDuration) || float.IsInfinity(motionDuration) || motionDuration <= 0f)
                throw new ArgumentException("확정한 모션 길이가 필요합니다.");

            var result = new List<UltimateTimelineEvent>();
            foreach (WeaponPresentationBeat beat in profile.beats)
            {
                foreach (UltimateTimelineEvent template in beat.eventTemplates)
                {
                    UltimateTimelineEvent copy = UltimateEventClipboard.Clone(template);
                    copy.startTime += beat.normalizedTime * motionDuration;
                    if (copy.EndTime > motionDuration + 0.0001f)
                        throw new ArgumentException("생성 이벤트가 모션 끝을 초과합니다.");
                    if (copy is UltimateFaceStripEvent face && !face.useAccentOverride)
                    {
                        face.useAccentOverride = true;
                        face.accentOverride = profile.accentColor;
                    }
                    result.Add(copy);
                }
            }
            return result;
        }

        /// <summary>생성 ID가 없는 최초 적용에서도 기존 목록은 모두 보존한다.</summary>
        public static void Apply(UltimateSequenceAsset asset, WeaponPresentationProfileSO profile, float motionDuration)
        {
            if (asset == null || UnityEditor.SerializationUtility.HasManagedReferencesWithMissingTypes(asset))
                throw new ArgumentException("시퀀스가 없거나 managed reference가 누락됐습니다.");
            List<UltimateTimelineEvent> generated = Build(profile, motionDuration);
            var candidate = new List<UltimateTimelineEvent>();
            if (asset.events != null)
            {
                foreach (UltimateTimelineEvent item in asset.events)
                {
                    if (item == null)
                        throw new ArgumentException("기존 이벤트 누락을 먼저 복구해야 합니다.");
                    bool owned = !string.IsNullOrEmpty(asset.generatedPresentationId)
                        && item.presentationGenerationId == asset.generatedPresentationId;
                    if (owned && !WeaponPresentationProfileSO.IsPresentationEvent(item))
                        throw new ArgumentException("생성 목록에 전투 이벤트가 있습니다. 원본을 보존합니다.");
                    if (!owned)
                        candidate.Add(item);
                }
            }
            string generation = Guid.NewGuid().ToString("N");
            foreach (UltimateTimelineEvent item in generated)
            {
                item.presentationGenerationId = generation;
                candidate.Add(item);
            }
            // 같은 시각의 기존 이벤트 실행 순서를 바꾸지 않는다. 런타임은 시간으로 발화한다.
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("무기 연출 템플릿 적용");
            try
            {
                Undo.RecordObject(asset, "무기 연출 템플릿 적용");
                asset.events = candidate;
                asset.weaponProfile = profile;
                asset.generatedPresentationId = generation;
                EditorUtility.SetDirty(asset);
                Undo.CollapseUndoOperations(group);
                // Test Runner는 시작 시 현재 그룹까지 되돌린다. 저작 그룹을 닫아 후속 작업과 분리한다.
                Undo.IncrementCurrentGroup();
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }
    }
}
#endif
