#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Tool.Editor.Combat;

namespace UPlayGround.Tool.Editor.Validation
{
    public enum EditorValidationScope
    {
        Project,
        Selection
    }

    public sealed class EditorValidationContext
    {
        private readonly string[] _assetPaths;

        public EditorValidationScope Scope { get; }
        public IReadOnlyList<string> AssetPaths => _assetPaths;

        private EditorValidationContext(EditorValidationScope scope, IEnumerable<string> assetPaths)
        {
            Scope = scope;
            _assetPaths = assetPaths?
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(NormalizePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();
        }

        public static EditorValidationContext Project()
        {
            return new EditorValidationContext(EditorValidationScope.Project, null);
        }

        public static EditorValidationContext Selection()
        {
            return new EditorValidationContext(
                EditorValidationScope.Selection,
                UnityEditor.Selection.objects.Select(AssetDatabase.GetAssetPath));
        }

        public bool Includes(EditorValidationIssue issue)
        {
            if (Scope == EditorValidationScope.Project)
                return true;
            if (_assetPaths.Length == 0 || string.IsNullOrWhiteSpace(issue.AssetPath))
                return false;

            string issuePath = NormalizePath(issue.AssetPath);
            foreach (string selectedPath in _assetPaths)
            {
                if (string.Equals(issuePath, selectedPath, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (AssetDatabase.IsValidFolder(selectedPath)
                    && issuePath.StartsWith(selectedPath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').TrimEnd('/');
        }
    }

    public interface IEditorDataValidator
    {
        string Id { get; }
        string DisplayName { get; }
        int Order { get; }
        IEnumerable<EditorValidationIssue> Validate(EditorValidationContext context);
    }

    public sealed class EditorValidationRunResult
    {
        public EditorValidationContext Context { get; }
        public IReadOnlyList<EditorValidationIssue> Issues { get; }
        public int ValidatorCount { get; }
        public double DurationSeconds { get; }

        public int ErrorCount => Issues.Count(issue => issue.Severity == EditorValidationSeverity.Error);
        public int WarningCount => Issues.Count(issue => issue.Severity == EditorValidationSeverity.Warning);
        public int InfoCount => Issues.Count(issue => issue.Severity == EditorValidationSeverity.Info);

        public EditorValidationRunResult(
            EditorValidationContext context,
            IReadOnlyList<EditorValidationIssue> issues,
            int validatorCount,
            double durationSeconds)
        {
            Context = context;
            Issues = issues;
            ValidatorCount = validatorCount;
            DurationSeconds = durationSeconds;
        }
    }

    public static class EditorValidationRegistry
    {
        private static List<IEditorDataValidator> s_validators;

        public static IReadOnlyList<IEditorDataValidator> Validators
            => s_validators ??= DiscoverValidators();

        public static EditorValidationRunResult Run(EditorValidationContext context)
        {
            context ??= EditorValidationContext.Project();
            var stopwatch = Stopwatch.StartNew();
            var issues = new List<EditorValidationIssue>();

            foreach (IEditorDataValidator validator in Validators)
            {
                try
                {
                    IEnumerable<EditorValidationIssue> validatorIssues = validator.Validate(context);
                    if (validatorIssues == null)
                        continue;

                    foreach (EditorValidationIssue issue in validatorIssues)
                    {
                        EditorValidationIssue normalized = string.IsNullOrWhiteSpace(issue.ValidatorId)
                            ? issue.WithValidator(validator.Id)
                            : issue;
                        if (context.Includes(normalized))
                            issues.Add(normalized);
                    }
                }
                catch (Exception exception)
                {
                    issues.Add(new EditorValidationIssue(
                        EditorValidationSeverity.Error,
                        "Validation",
                        string.Empty,
                        null,
                        validator.Id,
                        $"{validator.DisplayName} 실행 중 예외가 발생했습니다: {exception.Message}",
                        "Console의 예외 로그를 확인하세요.",
                        "validator.exception",
                        validator.Id));
                    UnityEngine.Debug.LogException(exception);
                }
            }

            issues.Sort(CompareIssue);
            stopwatch.Stop();
            return new EditorValidationRunResult(context, issues, Validators.Count, stopwatch.Elapsed.TotalSeconds);
        }

        public static void Reload()
        {
            s_validators = null;
        }

        private static List<IEditorDataValidator> DiscoverValidators()
        {
            var validators = new List<IEditorDataValidator>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IEditorDataValidator>())
            {
                if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
                    continue;

                try
                {
                    if (Activator.CreateInstance(type) is IEditorDataValidator validator)
                        validators.Add(validator);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogError($"[DataValidation] 검증기 생성 실패: {type.FullName}\n{exception}");
                }
            }

            return validators
                .OrderBy(validator => validator.Order)
                .ThenBy(validator => validator.DisplayName, StringComparer.Ordinal)
                .ToList();
        }

        private static int CompareIssue(EditorValidationIssue a, EditorValidationIssue b)
        {
            int severity = b.Severity.CompareTo(a.Severity);
            if (severity != 0)
                return severity;

            int domain = string.Compare(a.Domain, b.Domain, StringComparison.Ordinal);
            if (domain != 0)
                return domain;

            int path = string.Compare(a.AssetPath, b.AssetPath, StringComparison.Ordinal);
            if (path != 0)
                return path;

            return string.Compare(a.Field, b.Field, StringComparison.Ordinal);
        }
    }

    internal sealed class DataPathEditorValidator : IEditorDataValidator
    {
        public string Id => "data-path";
        public string DisplayName => "데이터 경로";
        public int Order => 100;
        public IEnumerable<EditorValidationIssue> Validate(EditorValidationContext context)
            => DataPathValidator.ValidateAll();
    }

    internal sealed class ActorEditorValidator : IEditorDataValidator
    {
        public string Id => "actor";
        public string DisplayName => "액터 데이터";
        public int Order => 200;
        public IEnumerable<EditorValidationIssue> Validate(EditorValidationContext context)
            => ActorDataValidator.ValidateAll();
    }

    internal sealed class GeneralEditorValidator : IEditorDataValidator
    {
        public string Id => "general";
        public string DisplayName => "일반 데이터";
        public int Order => 300;
        public IEnumerable<EditorValidationIssue> Validate(EditorValidationContext context)
            => GeneralDataValidator.ValidateAll();
    }

    internal sealed class CombatEditorValidator : IEditorDataValidator
    {
        public string Id => "combat";
        public string DisplayName => "전투 데이터";
        public int Order => 400;

        public IEnumerable<EditorValidationIssue> Validate(EditorValidationContext context)
        {
            foreach (CombatValidationIssue issue in CombatDataValidator.ValidateAll())
            {
                yield return new EditorValidationIssue(
                    ConvertSeverity(issue.Severity),
                    "Combat",
                    issue.AssetPath,
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(issue.AssetPath),
                    issue.Context,
                    issue.Message);
            }
        }

        private static EditorValidationSeverity ConvertSeverity(CombatValidationSeverity severity)
        {
            return severity switch
            {
                CombatValidationSeverity.Error => EditorValidationSeverity.Error,
                CombatValidationSeverity.Warning => EditorValidationSeverity.Warning,
                _ => EditorValidationSeverity.Info
            };
        }
    }
}
#endif
