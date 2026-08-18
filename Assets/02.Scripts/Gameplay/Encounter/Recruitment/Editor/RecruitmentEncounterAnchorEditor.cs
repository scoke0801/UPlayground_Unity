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

            RecruitmentEncounterAuthoringValidator.ValidateAnchor(
                (RecruitmentEncounterAnchor)target,
                _issues,
                includeProjectIdScan: changed);
            for (int i = 0; i < _issues.Count; i++)
                DrawIssue(_issues[i]);
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
