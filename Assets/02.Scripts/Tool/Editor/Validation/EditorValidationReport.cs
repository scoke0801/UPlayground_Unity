#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Validation
{
    public static class EditorValidationReport
    {
        [Serializable]
        private sealed class JsonReport
        {
            public string generatedAt;
            public string scope;
            public int validatorCount;
            public double durationSeconds;
            public int errorCount;
            public int warningCount;
            public int infoCount;
            public List<JsonIssue> issues = new();
        }

        [Serializable]
        private sealed class JsonIssue
        {
            public string severity;
            public string validatorId;
            public string ruleId;
            public string domain;
            public string assetPath;
            public string field;
            public string message;
            public string fixHint;
        }

        public static void WriteMarkdown(string path, EditorValidationRunResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Data Validation Report");
            builder.AppendLine();
            builder.AppendLine($"- Generated: {DateTime.Now:O}");
            builder.AppendLine($"- Scope: {result.Context.Scope}");
            builder.AppendLine($"- Validators: {result.ValidatorCount}");
            builder.AppendLine($"- Duration: {result.DurationSeconds:F2}s");
            builder.AppendLine($"- Error: {result.ErrorCount}");
            builder.AppendLine($"- Warning: {result.WarningCount}");
            builder.AppendLine($"- Info: {result.InfoCount}");
            builder.AppendLine();
            builder.AppendLine("| Severity | Validator | Rule | Domain | Field | Message | Asset | Fix Hint |");
            builder.AppendLine("|----------|-----------|------|--------|-------|---------|-------|----------|");

            foreach (EditorValidationIssue issue in result.Issues)
            {
                builder.Append("| ")
                    .Append(issue.Severity).Append(" | ")
                    .Append(Escape(issue.ValidatorId)).Append(" | ")
                    .Append(Escape(issue.RuleId)).Append(" | ")
                    .Append(Escape(issue.Domain)).Append(" | ")
                    .Append(Escape(issue.Field)).Append(" | ")
                    .Append(Escape(issue.Message)).Append(" | ")
                    .Append(Escape(issue.AssetPath)).Append(" | ")
                    .Append(Escape(issue.FixHint)).AppendLine(" |");
            }

            EnsureParentDirectory(path);
            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        }

        public static void WriteJson(string path, EditorValidationRunResult result)
        {
            var report = new JsonReport
            {
                generatedAt = DateTime.Now.ToString("O"),
                scope = result.Context.Scope.ToString(),
                validatorCount = result.ValidatorCount,
                durationSeconds = result.DurationSeconds,
                errorCount = result.ErrorCount,
                warningCount = result.WarningCount,
                infoCount = result.InfoCount
            };

            foreach (EditorValidationIssue issue in result.Issues)
            {
                report.issues.Add(new JsonIssue
                {
                    severity = issue.Severity.ToString(),
                    validatorId = issue.ValidatorId,
                    ruleId = issue.RuleId,
                    domain = issue.Domain,
                    assetPath = issue.AssetPath,
                    field = issue.Field,
                    message = issue.Message,
                    fixHint = issue.FixHint
                });
            }

            EnsureParentDirectory(path);
            File.WriteAllText(path, JsonUtility.ToJson(report, true), Encoding.UTF8);
        }

        public static void RunFromCommandLine()
        {
            EditorValidationRunResult result = EditorValidationRegistry.Run(EditorValidationContext.Project());
            string path = GetCommandLineValue("-validationReport");
            if (string.IsNullOrWhiteSpace(path))
                path = Path.GetFullPath("Library/UPlaygroundValidationReport.json");

            WriteJson(path, result);
            Debug.Log(
                $"[DataValidation] 완료: Error {result.ErrorCount}, Warning {result.WarningCount}, " +
                $"Info {result.InfoCount}, Report {path}");
            EditorApplication.Exit(result.ErrorCount > 0 ? 1 : 0);
        }

        private static string GetCommandLineValue(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return null;
        }

        private static void EnsureParentDirectory(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
#endif
