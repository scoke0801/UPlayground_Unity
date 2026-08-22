using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Gameplay.Encounter.Editor
{
    /// <summary>영입 조우의 진행 불능 구성을 씬 저작 시점에 검증한다.</summary>
    [CustomEditor(typeof(RecruitmentEncounterAnchor))]
    public sealed class RecruitmentEncounterAnchorEditor : UnityEditor.Editor
    {
        private readonly List<RecruitmentEncounterAuthoringIssue> _issues = new();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            bool changed = serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6f);
            if (GUILayout.Button("영입 조우 저작 창에서 열기"))
            {
                RecruitmentEncounterAuthoringWindow.OpenForAnchor(
                    (RecruitmentEncounterAnchor)target);
            }

            if (GUILayout.Button("참가자를 지면에 맞추기"))
                AlignParticipantsToGround((RecruitmentEncounterAnchor)target);

            RecruitmentEncounterAuthoringValidator.ValidateAnchor(
                (RecruitmentEncounterAnchor)target,
                _issues,
                includeProjectIdScan: changed);
            for (int i = 0; i < _issues.Count; i++)
                DrawIssue(_issues[i]);
        }

        /// <summary>
        /// 참가자 배치 높이를 실제 지면으로 맞춰 씬 데이터를 고친다.
        /// 런타임 보정은 안전망일 뿐이고, 저작 위치가 지면 아래면 씬 뷰의 대치 구도를 확인할 수 없다.
        /// </summary>
        private static void AlignParticipantsToGround(RecruitmentEncounterAnchor anchor)
        {
            SerializedProperty participants = new SerializedObject(anchor).FindProperty("_participants");
            if (participants == null || !participants.isArray)
                return;

            int aligned = 0;
            for (int i = 0; i < participants.arraySize; i++)
            {
                var participant = participants
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue as RecruitmentEncounterParticipant;
                if (participant == null)
                    continue;

                Transform participantTransform = participant.transform;
                Vector3 position = participantTransform.position;
                if (!RecruitmentEncounterAuthoringValidator.TryMeasureGroundOffset(
                        participantTransform,
                        out float offset)
                    || Mathf.Abs(offset) <= RecruitmentEncounterAuthoringValidator.GroundPlacementTolerance)
                {
                    continue;
                }

                Undo.RecordObject(participantTransform, "영입 조우 참가자 지면 정렬");
                position.y -= offset;
                participantTransform.position = position;
                PrefabUtility.RecordPrefabInstancePropertyModifications(participantTransform);
                EditorUtility.SetDirty(participantTransform);
                aligned++;
            }

            Debug.Log(
                aligned > 0
                    ? $"[RecruitmentEncounter] 참가자 {aligned}명을 지면에 맞췄습니다. 씬을 저장하세요."
                    : "[RecruitmentEncounter] 지면에서 벗어난 참가자가 없습니다.",
                anchor);
        }

        private static void DrawIssue(RecruitmentEncounterAuthoringIssue issue)
        {
            MessageType messageType = issue.Severity switch
            {
                RecruitmentEncounterIssueSeverity.Error => MessageType.Error,
                RecruitmentEncounterIssueSeverity.Warning => MessageType.Warning,
                _ => MessageType.Info,
            };
            EditorGUILayout.HelpBox(issue.Message, messageType);

            if (issue.Context == null || issue.Context == Selection.activeObject)
                return;
            if (GUILayout.Button($"문제 대상 선택: {issue.Context.name}"))
            {
                Selection.activeObject = issue.Context;
                EditorGUIUtility.PingObject(issue.Context);
            }
        }
    }
}
