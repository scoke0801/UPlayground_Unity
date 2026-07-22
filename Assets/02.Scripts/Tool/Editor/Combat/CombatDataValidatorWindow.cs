#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Combat
{
    public class CombatDataValidatorWindow : EditorWindow
    {
        private readonly List<CombatValidationIssue> _issues = new();
        private Vector2 _scroll;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/전투/도구/데이터 검증기", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayCombatTools)]
        public static void Open()
        {
            GetWindow<CombatDataValidatorWindow>("Combat Validator");
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate All", GUILayout.Width(120)))
                    RunValidation();
                if (GUILayout.Button("Save Markdown", GUILayout.Width(130)))
                    SaveMarkdownReport();
                if (GUILayout.Button("Generate Policies", GUILayout.Width(140)))
                    CombatPolicyAssetGenerator.GenerateDefaultPolicyAssets();

                GUILayout.Label($"Issues: {_issues.Count}");
            }

            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (CombatValidationIssue issue in _issues)
            {
                MessageType messageType = issue.Severity switch
                {
                    CombatValidationSeverity.Error => MessageType.Error,
                    CombatValidationSeverity.Warning => MessageType.Warning,
                    _ => MessageType.Info,
                };

                EditorGUILayout.HelpBox(
                    $"{issue.Severity} | {issue.Context}\n{issue.Message}\n{issue.AssetPath}",
                    messageType);
            }
            EditorGUILayout.EndScrollView();
        }

        private void RunValidation()
        {
            _issues.Clear();
            _issues.AddRange(CombatDataValidator.ValidateAll());
        }

        private void SaveMarkdownReport()
        {
            if (_issues.Count == 0)
                RunValidation();

            string path = EditorUtility.SaveFilePanel(
                "Save Combat Validation Report",
                Application.dataPath,
                "CombatValidationReport.md",
                "md");
            if (string.IsNullOrWhiteSpace(path))
                return;

            var builder = new StringBuilder();
            builder.AppendLine("# Combat Validation Report");
            builder.AppendLine();
            builder.AppendLine($"- Issues: {_issues.Count}");
            builder.AppendLine();
            builder.AppendLine("| Severity | Context | Message | Asset |");
            builder.AppendLine("|----------|---------|---------|-------|");
            foreach (CombatValidationIssue issue in _issues)
            {
                builder.Append("| ")
                    .Append(issue.Severity)
                    .Append(" | ")
                    .Append(EscapeMarkdown(issue.Context))
                    .Append(" | ")
                    .Append(EscapeMarkdown(issue.Message))
                    .Append(" | ")
                    .Append(EscapeMarkdown(issue.AssetPath))
                    .AppendLine(" |");
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        private static string EscapeMarkdown(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
#endif
