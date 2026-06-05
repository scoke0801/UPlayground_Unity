#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Tool.Editor.Combat;

namespace UPlayGround.Tool.Editor.Validation
{
    public sealed class DataValidationHubWindow : EditorWindow
    {
        private readonly List<EditorValidationIssue> _issues = new();
        private Vector2 _scroll;
        private string _filter = "";
        private bool _includeInfo = true;
        private bool _includeWarning = true;
        private bool _includeError = true;

        [MenuItem("UPlayGround/유틸/데이터 검증 허브", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.UtilValidation)]
        public static void Open()
        {
            var window = GetWindow<DataValidationHubWindow>("Data Validation");
            window.minSize = new Vector2(840f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            RunAll();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSummary();
            DrawIssues();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("전체 검증", EditorStyles.toolbarButton, GUILayout.Width(78f)))
                    RunAll();

                if (GUILayout.Button("리포트 저장", EditorStyles.toolbarButton, GUILayout.Width(86f)))
                    SaveMarkdownReport();

                if (GUILayout.Button("Data 경로 이동", EditorStyles.toolbarButton, GUILayout.Width(102f)))
                    MoveDataAssetsToDataRoot();

                GUILayout.Space(6f);
                GUILayout.Label("검색", GUILayout.Width(34f));
                _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField, GUILayout.MinWidth(180f));
                if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                    _filter = "";

                GUILayout.FlexibleSpace();
                _includeError = GUILayout.Toggle(_includeError, "Error", EditorStyles.toolbarButton, GUILayout.Width(58f));
                _includeWarning = GUILayout.Toggle(_includeWarning, "Warning", EditorStyles.toolbarButton, GUILayout.Width(76f));
                _includeInfo = GUILayout.Toggle(_includeInfo, "Info", EditorStyles.toolbarButton, GUILayout.Width(52f));
            }
        }

        private void DrawSummary()
        {
            int errors = 0;
            int warnings = 0;
            int infos = 0;
            foreach (EditorValidationIssue issue in _issues)
            {
                switch (issue.Severity)
                {
                    case EditorValidationSeverity.Error: errors++; break;
                    case EditorValidationSeverity.Warning: warnings++; break;
                    case EditorValidationSeverity.Info: infos++; break;
                }
            }

            EditorGUILayout.HelpBox(
                $"검증 결과: Error {errors} / Warning {warnings} / Info {infos} / Total {_issues.Count}",
                errors > 0 ? MessageType.Error : warnings > 0 ? MessageType.Warning : MessageType.Info);
        }

        private void DrawIssues()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            int visible = 0;
            foreach (EditorValidationIssue issue in _issues)
            {
                if (!ShouldShow(issue))
                    continue;

                visible++;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{issue.Severity} | {issue.Domain} | {issue.Field}", EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        if (issue.Asset != null && GUILayout.Button("Ping", GUILayout.Width(52f)))
                            EditorGUIUtility.PingObject(issue.Asset);
                        if (issue.Asset != null && GUILayout.Button("Select", GUILayout.Width(58f)))
                            Selection.activeObject = issue.Asset;
                    }

                    EditorGUILayout.HelpBox(issue.Message, issue.ToMessageType());
                    if (!string.IsNullOrWhiteSpace(issue.FixHint))
                        EditorGUILayout.LabelField(issue.FixHint, EditorStyles.wordWrappedMiniLabel);
                    if (!string.IsNullOrWhiteSpace(issue.AssetPath))
                        EditorGUILayout.SelectableLabel(issue.AssetPath, EditorStyles.miniLabel, GUILayout.Height(16f));
                }
            }

            if (visible == 0)
                EditorGUILayout.LabelField("표시할 이슈가 없습니다.", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndScrollView();
        }

        private void RunAll()
        {
            _issues.Clear();
            _issues.AddRange(DataPathValidator.ValidateAll());
            _issues.AddRange(ActorDataValidator.ValidateAll());
            _issues.AddRange(GeneralDataValidator.ValidateAll());

            foreach (CombatValidationIssue issue in CombatDataValidator.ValidateAll())
            {
                _issues.Add(new EditorValidationIssue(
                    ConvertSeverity(issue.Severity),
                    "Combat",
                    issue.AssetPath,
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(issue.AssetPath),
                    issue.Context,
                    issue.Message));
            }

            _issues.Sort(CompareIssue);
        }

        private void MoveDataAssetsToDataRoot()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Data 경로 이동",
                "Assets/10.Datas 밖에 있는 UPlayGround 데이터 에셋을 같은 타입의 기존 Data 폴더 위치로 이동합니다.\n\nGUID는 유지되지만, 현재 에셋 이동 작업을 실행할까요?",
                "이동",
                "취소");
            if (!confirm)
                return;

            DataPathMoveResult result = DataPathValidator.MoveAssetsToDataRoot();
            RunAll();
            EditorUtility.DisplayDialog(
                "Data 경로 이동 완료",
                $"이동: {result.Moved}\n기준 경로 없음: {result.Skipped}\n실패: {result.Failed}",
                "확인");
        }

        private bool ShouldShow(EditorValidationIssue issue)
        {
            if (issue.Severity == EditorValidationSeverity.Error && !_includeError)
                return false;
            if (issue.Severity == EditorValidationSeverity.Warning && !_includeWarning)
                return false;
            if (issue.Severity == EditorValidationSeverity.Info && !_includeInfo)
                return false;

            if (string.IsNullOrWhiteSpace(_filter))
                return true;

            string q = _filter.ToLowerInvariant();
            return Contains(issue.Domain, q)
                   || Contains(issue.AssetPath, q)
                   || Contains(issue.Field, q)
                   || Contains(issue.Message, q)
                   || Contains(issue.FixHint, q);
        }

        private void SaveMarkdownReport()
        {
            if (_issues.Count == 0)
                RunAll();

            string path = EditorUtility.SaveFilePanel(
                "Data Validation Report 저장",
                Application.dataPath,
                "DataValidationReport.md",
                "md");
            if (string.IsNullOrWhiteSpace(path))
                return;

            var builder = new StringBuilder();
            builder.AppendLine("# Data Validation Report");
            builder.AppendLine();
            builder.AppendLine($"- Issues: {_issues.Count}");
            builder.AppendLine();
            builder.AppendLine("| Severity | Domain | Field | Message | Asset | Fix Hint |");
            builder.AppendLine("|----------|--------|-------|---------|-------|----------|");

            foreach (EditorValidationIssue issue in _issues)
            {
                builder.Append("| ")
                    .Append(issue.Severity)
                    .Append(" | ")
                    .Append(Escape(issue.Domain))
                    .Append(" | ")
                    .Append(Escape(issue.Field))
                    .Append(" | ")
                    .Append(Escape(issue.Message))
                    .Append(" | ")
                    .Append(Escape(issue.AssetPath))
                    .Append(" | ")
                    .Append(Escape(issue.FixHint))
                    .AppendLine(" |");
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();
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

        private static int CompareIssue(EditorValidationIssue a, EditorValidationIssue b)
        {
            int severity = b.Severity.CompareTo(a.Severity);
            if (severity != 0)
                return severity;

            int domain = string.Compare(a.Domain, b.Domain, System.StringComparison.Ordinal);
            if (domain != 0)
                return domain;

            return string.Compare(a.AssetPath, b.AssetPath, System.StringComparison.Ordinal);
        }

        private static bool Contains(string source, string lowerQuery)
        {
            return !string.IsNullOrEmpty(source) && source.ToLowerInvariant().Contains(lowerQuery);
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
