#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Editor.Authoring;
using UPlayGround.Tool.Editor.Validation;

namespace UPlayGround.Editor.Authoring
{
    /// <summary>
    /// 프로젝트 전역 검증 프레임워크를 데이터 저작 허브의 느슨한 공급자 경계에 연결합니다.
    /// </summary>
    [InitializeOnLoad]
    internal static class ProjectValidationBridgeRegistration
    {
        static ProjectValidationBridgeRegistration()
        {
            ValidationBridge.Register("project-validation", RunProjectValidation);
        }

        private static IEnumerable<DataAuthoringValidationResult> RunProjectValidation()
        {
            EditorValidationRunResult run = EditorValidationRegistry.Run(EditorValidationContext.Project());
            foreach (EditorValidationIssue issue in run.Issues)
            {
                Object context = issue.Asset;
                if (context == null && !string.IsNullOrWhiteSpace(issue.AssetPath))
                    context = AssetDatabase.LoadAssetAtPath<Object>(issue.AssetPath);

                string label = context != null
                    ? context.name
                    : !string.IsNullOrWhiteSpace(issue.AssetPath) ? issue.AssetPath : issue.Field;
                yield return new DataAuthoringValidationResult(
                    null,
                    string.IsNullOrWhiteSpace(issue.Domain) ? "프로젝트" : issue.Domain,
                    issue.AssetPath,
                    label,
                    new DataAuthoringIssue(ConvertSeverity(issue.Severity), issue.Message, context));
            }
        }

        private static DataAuthoringIssueSeverity ConvertSeverity(EditorValidationSeverity severity)
        {
            return severity switch
            {
                EditorValidationSeverity.Error => DataAuthoringIssueSeverity.Error,
                EditorValidationSeverity.Warning => DataAuthoringIssueSeverity.Warning,
                _ => DataAuthoringIssueSeverity.Info
            };
        }
    }
}
#endif
