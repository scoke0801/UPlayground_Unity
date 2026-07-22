#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Validation
{
    public sealed class DataValidationHubWindow : EditorWindow
    {
        private readonly List<EditorValidationIssue> _issues = new();
        [SerializeField] private string _filter = "";
        [SerializeField] private string _domainFilter = "전체";
        [SerializeField] private bool _includeInfo = true;
        [SerializeField] private bool _includeWarning = true;
        [SerializeField] private bool _includeError = true;
        [SerializeField] private int _selectedIssueIndex = -1;
        private readonly List<int> _visibleIssueIndices = new();
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private EditorValidationRunResult _lastResult;
        private string[] _domains = { "전체" };

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/유틸/데이터 검증 허브", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.UtilValidation)]
        public static void Open()
        {
            var window = GetWindow<DataValidationHubWindow>("Data Validation");
            window.minSize = new Vector2(840f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            Run(EditorValidationContext.Project());
        }

        private void OnGUI()
        {
            HandleKeyboard();
            DrawToolbar();
            DrawSummary();
            RebuildVisibleIssues();

            if (position.width >= 760f)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawIssueList(GUILayout.Width(Mathf.Clamp(position.width * 0.46f, 360f, 560f)));
                    DrawIssueDetail(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                }
            }
            else
            {
                DrawIssueList(GUILayout.MinHeight(220f));
                DrawIssueDetail(GUILayout.MinHeight(180f));
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("전체 검증", EditorStyles.toolbarButton, GUILayout.Width(78f)))
                    Run(EditorValidationContext.Project());

                using (new EditorGUI.DisabledScope(!HasAssetSelection()))
                {
                    if (GUILayout.Button("선택 검증", EditorStyles.toolbarButton, GUILayout.Width(78f)))
                        Run(EditorValidationContext.Selection());
                }

                if (GUILayout.Button("MD 저장", EditorStyles.toolbarButton, GUILayout.Width(66f)))
                    SaveMarkdownReport();

                if (GUILayout.Button("JSON 저장", EditorStyles.toolbarButton, GUILayout.Width(76f)))
                    SaveJsonReport();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Data 경로 이동", EditorStyles.toolbarButton, GUILayout.Width(102f)))
                    MoveDataAssetsToDataRoot();
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("검색", GUILayout.Width(34f));
                _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField, GUILayout.MinWidth(180f));
                if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                    _filter = "";

                GUILayout.Space(6f);
                int selectedIndex = System.Array.IndexOf(_domains, _domainFilter);
                if (selectedIndex < 0)
                    selectedIndex = 0;
                selectedIndex = EditorGUILayout.Popup(selectedIndex, _domains, EditorStyles.toolbarPopup, GUILayout.Width(150f));
                _domainFilter = _domains[selectedIndex];

                GUILayout.FlexibleSpace();
                _includeError = GUILayout.Toggle(_includeError, $"오류 {_lastResult?.ErrorCount ?? 0}", EditorStyles.toolbarButton, GUILayout.Width(70f));
                _includeWarning = GUILayout.Toggle(_includeWarning, $"경고 {_lastResult?.WarningCount ?? 0}", EditorStyles.toolbarButton, GUILayout.Width(70f));
                _includeInfo = GUILayout.Toggle(_includeInfo, $"정보 {_lastResult?.InfoCount ?? 0}", EditorStyles.toolbarButton, GUILayout.Width(70f));

                if (GUILayout.Button("필터 초기화", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                    ResetFilters();
            }
        }

        private void DrawSummary()
        {
            if (_lastResult == null)
            {
                EditorGUILayout.HelpBox("검증 결과가 없습니다.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(
                    _lastResult.Context.Scope == EditorValidationScope.Project ? "프로젝트 전체" : "선택 범위",
                    EditorStyles.boldLabel,
                    GUILayout.Width(82f));
                GUILayout.Label($"검증기 {_lastResult.ValidatorCount}", GUILayout.Width(70f));
                GUILayout.Label($"소요 {_lastResult.DurationSeconds:F2}s", GUILayout.Width(76f));
                GUILayout.FlexibleSpace();
                GUILayout.Label(
                    _lastResult.ErrorCount > 0 ? "수정이 필요한 오류가 있습니다." :
                    _lastResult.WarningCount > 0 ? "확인이 필요한 경고가 있습니다." :
                    "검증을 통과했습니다.",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawIssueList(params GUILayoutOption[] options)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, options))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("검증 결과", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"{_visibleIssueIndices.Count} / {_issues.Count}", EditorStyles.miniLabel);
                }

                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                if (_visibleIssueIndices.Count == 0)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("현재 필터에 해당하는 이슈가 없습니다.", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    foreach (int issueIndex in _visibleIssueIndices)
                        DrawIssueRow(issueIndex);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawIssueRow(int issueIndex)
        {
            EditorValidationIssue issue = _issues[issueIndex];
            Rect row = GUILayoutUtility.GetRect(0f, 46f, GUILayout.ExpandWidth(true));
            bool selected = issueIndex == _selectedIssueIndex;
            if (selected)
                EditorGUI.DrawRect(row, new Color(0.18f, 0.36f, 0.58f, 0.55f));
            else if (Event.current.type == EventType.Repaint && issueIndex % 2 == 0)
                EditorGUI.DrawRect(row, new Color(1f, 1f, 1f, 0.025f));

            Rect severityRect = new Rect(row.x + 5f, row.y + 7f, 6f, row.height - 14f);
            EditorGUI.DrawRect(severityRect, GetSeverityColor(issue.Severity));

            string title = $"{issue.Domain} · {issue.Field}";
            GUI.Label(new Rect(row.x + 17f, row.y + 4f, row.width - 24f, 18f), title, EditorStyles.boldLabel);
            GUI.Label(
                new Rect(row.x + 17f, row.y + 23f, row.width - 24f, 18f),
                string.IsNullOrWhiteSpace(issue.Message) ? "(메시지 없음)" : issue.Message,
                EditorStyles.miniLabel);

            if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
            {
                _selectedIssueIndex = issueIndex;
                _detailScroll = Vector2.zero;
                if (Event.current.clickCount == 2)
                    SelectIssueAsset(issue, ping: true);
                Event.current.Use();
                Repaint();
            }
        }

        private void DrawIssueDetail(params GUILayoutOption[] options)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, options))
            {
                if (_selectedIssueIndex < 0 || _selectedIssueIndex >= _issues.Count)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField("왼쪽 결과를 선택하면 상세 내용과 작업 버튼을 표시합니다.", EditorStyles.centeredGreyMiniLabel);
                    GUILayout.FlexibleSpace();
                    return;
                }

                EditorValidationIssue issue = _issues[_selectedIssueIndex];
                using (new EditorGUILayout.HorizontalScope())
                {
                    Color previous = GUI.color;
                    GUI.color = GetSeverityColor(issue.Severity);
                    GUILayout.Label(GetSeverityLabel(issue.Severity), EditorStyles.boldLabel, GUILayout.Width(42f));
                    GUI.color = previous;

                    GUILayout.Label($"{issue.Domain} / {issue.Field}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(issue.Asset == null))
                    {
                        if (GUILayout.Button("선택", GUILayout.Width(56f)))
                            SelectIssueAsset(issue, ping: false);
                        if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                            SelectIssueAsset(issue, ping: true);
                    }
                }

                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(issue.Message, issue.ToMessageType());

                if (!string.IsNullOrWhiteSpace(issue.FixHint))
                {
                    EditorGUILayout.LabelField("권장 조치", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(issue.FixHint, EditorStyles.wordWrappedLabel);
                }

                DrawReadOnlyValue("에셋", issue.AssetPath);
                DrawReadOnlyValue("검증기", issue.ValidatorId);
                DrawReadOnlyValue("규칙 ID", issue.RuleId);
                EditorGUILayout.EndScrollView();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("메시지 복사"))
                        CopyIssue(issue, includePath: false);
                    if (GUILayout.Button("전체 정보 복사"))
                        CopyIssue(issue, includePath: true);
                }
            }
        }

        private void Run(EditorValidationContext context)
        {
            _lastResult = EditorValidationRegistry.Run(context);
            _issues.Clear();
            _issues.AddRange(_lastResult.Issues);
            _domains = new[] { "전체" }
                .Concat(_issues
                    .Select(issue => issue.Domain)
                    .Where(domain => !string.IsNullOrWhiteSpace(domain))
                    .Distinct()
                    .OrderBy(domain => domain))
                .ToArray();
            if (!_domains.Contains(_domainFilter))
                _domainFilter = "전체";
            _listScroll = Vector2.zero;
            _selectedIssueIndex = _issues.Count > 0 ? 0 : -1;
            Repaint();
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
            Run(EditorValidationContext.Project());
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
            if (_domainFilter != "전체"
                && !string.Equals(issue.Domain, _domainFilter, System.StringComparison.Ordinal))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(_filter))
                return true;

            string q = _filter.ToLowerInvariant();
            return Contains(issue.Domain, q)
                   || Contains(issue.ValidatorId, q)
                   || Contains(issue.RuleId, q)
                   || Contains(issue.AssetPath, q)
                   || Contains(issue.Field, q)
                   || Contains(issue.Message, q)
                   || Contains(issue.FixHint, q);
        }

        private void SaveMarkdownReport()
        {
            EnsureResult();

            string path = EditorUtility.SaveFilePanel(
                "Data Validation Report 저장",
                Application.dataPath,
                "DataValidationReport.md",
                "md");
            if (string.IsNullOrWhiteSpace(path))
                return;

            EditorValidationReport.WriteMarkdown(path, _lastResult);
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("Markdown 리포트를 저장했습니다."));
        }

        private void SaveJsonReport()
        {
            EnsureResult();

            string path = EditorUtility.SaveFilePanel(
                "Data Validation JSON 저장",
                Application.dataPath,
                "DataValidationReport.json",
                "json");
            if (string.IsNullOrWhiteSpace(path))
                return;

            EditorValidationReport.WriteJson(path, _lastResult);
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("JSON 리포트를 저장했습니다."));
        }

        private void EnsureResult()
        {
            if (_lastResult == null)
                Run(EditorValidationContext.Project());
        }

        private void RebuildVisibleIssues()
        {
            _visibleIssueIndices.Clear();
            for (int i = 0; i < _issues.Count; i++)
            {
                if (ShouldShow(_issues[i]))
                    _visibleIssueIndices.Add(i);
            }

            if (_selectedIssueIndex >= 0 && !_visibleIssueIndices.Contains(_selectedIssueIndex))
                _selectedIssueIndex = _visibleIssueIndices.Count > 0 ? _visibleIssueIndices[0] : -1;
        }

        private void HandleKeyboard()
        {
            Event current = Event.current;
            if (current.type != EventType.KeyDown || EditorGUIUtility.editingTextField)
                return;

            if (current.keyCode is KeyCode.UpArrow or KeyCode.DownArrow)
            {
                RebuildVisibleIssues();
                if (_visibleIssueIndices.Count == 0)
                    return;

                int position = _visibleIssueIndices.IndexOf(_selectedIssueIndex);
                int delta = current.keyCode == KeyCode.DownArrow ? 1 : -1;
                position = position < 0 ? 0 : Mathf.Clamp(position + delta, 0, _visibleIssueIndices.Count - 1);
                _selectedIssueIndex = _visibleIssueIndices[position];
                current.Use();
                Repaint();
            }
            else if (current.keyCode == KeyCode.Return
                     && _selectedIssueIndex >= 0
                     && _selectedIssueIndex < _issues.Count)
            {
                SelectIssueAsset(_issues[_selectedIssueIndex], ping: true);
                current.Use();
            }
        }

        private void ResetFilters()
        {
            _filter = "";
            _domainFilter = "전체";
            _includeError = true;
            _includeWarning = true;
            _includeInfo = true;
        }

        private void SelectIssueAsset(EditorValidationIssue issue, bool ping)
        {
            if (issue.Asset == null)
                return;
            Selection.activeObject = issue.Asset;
            if (ping)
                EditorGUIUtility.PingObject(issue.Asset);
        }

        private void CopyIssue(EditorValidationIssue issue, bool includePath)
        {
            string text = includePath
                ? $"[{issue.Severity}] {issue.Domain} / {issue.Field}\n{issue.Message}\n{issue.FixHint}\n{issue.AssetPath}"
                : issue.Message;
            EditorGUIUtility.systemCopyBuffer = text;
            ShowNotification(new GUIContent("검증 정보를 복사했습니다."));
        }

        private static void DrawReadOnlyValue(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(18f));
        }

        private static Color GetSeverityColor(EditorValidationSeverity severity)
        {
            return severity switch
            {
                EditorValidationSeverity.Error => new Color(0.92f, 0.28f, 0.24f),
                EditorValidationSeverity.Warning => new Color(0.95f, 0.68f, 0.18f),
                _ => new Color(0.28f, 0.62f, 0.92f)
            };
        }

        private static string GetSeverityLabel(EditorValidationSeverity severity)
        {
            return severity switch
            {
                EditorValidationSeverity.Error => "오류",
                EditorValidationSeverity.Warning => "경고",
                _ => "정보"
            };
        }

        private static bool HasAssetSelection()
        {
            return Selection.objects.Any(asset =>
                asset != null && !string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(asset)));
        }

        private static bool Contains(string source, string lowerQuery)
        {
            return !string.IsNullOrEmpty(source) && source.ToLowerInvariant().Contains(lowerQuery);
        }
    }
}
#endif
