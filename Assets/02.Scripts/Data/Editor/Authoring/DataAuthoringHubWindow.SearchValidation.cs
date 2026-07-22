#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.Data.Editor.Authoring
{
    public sealed partial class DataAuthoringHubWindow
    {
        private const int MaxGlobalSearchResults = 500;
        private const string SpreadsheetMenuPath = "UPlayGround/SO 스프레드시트";
        private const string ValidationHubMenuPath = "UPlayGround/유틸/데이터 검증 허브";

        private readonly List<DataAuthoringValidationResult> _validationResults =
            new List<DataAuthoringValidationResult>();
        private ToolbarSearchField _globalSearchField;
        private ToolbarButton _validationButton;
        private bool _hasValidationRun;

        private void ShowGlobalSearch(string query)
        {
            if (_domainHost == null)
                return;

            string normalized = query?.Trim() ?? string.Empty;
            if (normalized.Length == 0)
            {
                RestoreActivePanel();
                return;
            }

            var results = new List<DataAuthoringSearchEntry>();
            foreach (IDataDomainPanel panel in _panels)
            {
                try
                {
                    results.AddRange(panel.Search(normalized));
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            DataAuthoringSearchEntry[] ordered = results
                .OrderBy(result => result.Panel.DisplayName, StringComparer.CurrentCulture)
                .ThenBy(result => result.Label, StringComparer.CurrentCulture)
                .Take(MaxGlobalSearchResults)
                .ToArray();

            _domainHost.Clear();
            var root = BuildResultRoot(
                $"전역 검색 · '{normalized}'",
                $"{ordered.Length:N0}개 결과" + (results.Count > MaxGlobalSearchResults ? $" · 상위 {MaxGlobalSearchResults}개만 표시" : string.Empty));
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            scroll.style.paddingLeft = 10f;
            scroll.style.paddingRight = 10f;
            if (ordered.Length == 0)
            {
                scroll.Add(BuildEmptyMessage("일치하는 데이터가 없습니다."));
            }
            else
            {
                foreach (DataAuthoringSearchEntry result in ordered)
                    scroll.Add(BuildSearchRow(result));
            }
            root.Add(scroll);
            _domainHost.Add(root);
        }

        private VisualElement BuildSearchRow(DataAuthoringSearchEntry result)
        {
            var row = new Button(() => OpenSearchResult(result));
            row.style.height = 50f;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.style.marginBottom = 4f;
            row.style.backgroundColor = DataAuthoringTheme.SurfaceRaised;
            DataAuthoringTheme.SetBorder(row);
            DataAuthoringTheme.Round(row, 4f);

            var icon = new Image { scaleMode = ScaleMode.ScaleToFit };
            if (result.Icon != null)
                icon.sprite = result.Icon;
            else
                icon.image = result.Panel.Icon;
            icon.style.width = 32f;
            icon.style.height = 32f;
            icon.style.marginRight = 9f;
            row.Add(icon);

            var domain = new Label(result.Panel.DisplayName);
            domain.style.width = 72f;
            domain.style.fontSize = 10f;
            domain.style.color = DataAuthoringTheme.Accent;
            row.Add(domain);

            var label = new Label(result.Label);
            label.style.flexGrow = 1f;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            row.Add(label);

            var key = new Label(result.Key);
            key.style.fontSize = 10f;
            key.style.color = DataAuthoringTheme.Muted;
            row.Add(key);
            return row;
        }

        private void OpenSearchResult(DataAuthoringSearchEntry result)
        {
            _globalSearchField?.SetValueWithoutNotify(string.Empty);
            SelectPanel(result.Panel);
            result.Panel.SelectSearchEntry(result);
        }

        private void RunAndShowValidation()
        {
            _validationResults.Clear();
            _validationResults.AddRange(ValidationBridge.Collect(_panels));
            _hasValidationRun = true;
            UpdateValidationButton();
            ShowValidationResults();
        }

        private void ShowValidationResults()
        {
            if (_domainHost == null)
                return;

            int errors = _validationResults.Count(result => result.Issue.Severity == DataAuthoringIssueSeverity.Error);
            int warnings = _validationResults.Count(result => result.Issue.Severity == DataAuthoringIssueSeverity.Warning);
            int infos = _validationResults.Count(result => result.Issue.Severity == DataAuthoringIssueSeverity.Info);

            _domainHost.Clear();
            var root = BuildResultRoot("데이터 검증", $"오류 {errors:N0} · 경고 {warnings:N0} · 정보 {infos:N0}");
            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.marginLeft = 10f;
            actions.style.marginRight = 10f;
            actions.style.marginBottom = 6f;
            var rerun = new Button(RunAndShowValidation) { text = "다시 검증" };
            DataAuthoringTheme.StyleButton(rerun, true);
            actions.Add(rerun);
            var fullHub = new Button(() => EditorApplication.ExecuteMenuItem(ValidationHubMenuPath)) { text = "전체 검증 허브 열기" };
            DataAuthoringTheme.StyleButton(fullHub);
            actions.Add(fullHub);
            root.Add(actions);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1f;
            scroll.style.paddingLeft = 10f;
            scroll.style.paddingRight = 10f;
            if (_validationResults.Count == 0)
            {
                scroll.Add(BuildEmptyMessage("발견된 데이터 이슈가 없습니다."));
            }
            else
            {
                foreach (DataAuthoringValidationResult result in _validationResults)
                    scroll.Add(BuildValidationRow(result));
            }
            root.Add(scroll);
            _domainHost.Add(root);
        }

        private VisualElement BuildValidationRow(DataAuthoringValidationResult result)
        {
            var row = new Button(() => OpenValidationResult(result));
            row.style.minHeight = 46f;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.unityTextAlign = TextAnchor.MiddleLeft;
            row.style.marginBottom = 4f;
            row.style.backgroundColor = DataAuthoringTheme.SurfaceRaised;
            DataAuthoringTheme.SetBorder(row);
            DataAuthoringTheme.Round(row, 4f);

            var severity = new Label(SeveritySymbol(result.Issue.Severity));
            severity.style.width = 26f;
            severity.style.fontSize = 15f;
            severity.style.color = SeverityColor(result.Issue.Severity);
            row.Add(severity);

            var domain = new Label(result.Domain);
            domain.style.width = 86f;
            domain.style.fontSize = 10f;
            row.Add(domain);

            var text = new VisualElement();
            text.style.flexGrow = 1f;
            var label = new Label(string.IsNullOrWhiteSpace(result.Label) ? result.Key : result.Label);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            text.Add(label);
            var message = new Label(result.Issue.Message);
            message.style.fontSize = 10f;
            message.style.whiteSpace = WhiteSpace.Normal;
            text.Add(message);
            row.Add(text);
            row.SetEnabled(result.Panel != null || result.Issue.Context != null);
            return row;
        }

        private void OpenValidationResult(DataAuthoringValidationResult result)
        {
            IDataDomainPanel panel = result.Panel;
            if (panel == null && result.Issue.Context != null)
                panel = _panels.FirstOrDefault(candidate => candidate.OwnsAsset(result.Issue.Context));

            if (panel != null)
            {
                SelectPanel(panel);
                if (result.Value != null)
                {
                    panel.SelectSearchEntry(new DataAuthoringSearchEntry(
                        panel,
                        result.Key,
                        result.Label,
                        result.Value,
                        context: result.Issue.Context));
                }
                else if (result.Issue.Context != null)
                    panel.SelectAsset(result.Issue.Context);
                return;
            }

            if (result.Issue.Context != null)
            {
                Selection.activeObject = result.Issue.Context;
                EditorGUIUtility.PingObject(result.Issue.Context);
            }
        }

        private void RestoreActivePanel()
        {
            if (_domainHost == null)
                return;
            _domainHost.Clear();
            if (_activePanel != null)
                _domainHost.Add(_activePanel.Root);
            else
                ShowEmptyHub();
        }

        private void InvalidateValidation()
        {
            _validationResults.Clear();
            _hasValidationRun = false;
            UpdateValidationButton();
        }

        private void UpdateValidationButton()
        {
            if (_validationButton == null)
                return;
            _validationButton.text = _hasValidationRun ? $"검증 {_validationResults.Count:N0}" : "검증 실행";
            if (_hasValidationRun)
            {
                int errors = _validationResults.Count(result => result.Issue.Severity == DataAuthoringIssueSeverity.Error);
                int warnings = _validationResults.Count(result => result.Issue.Severity == DataAuthoringIssueSeverity.Warning);
                _validationButton.text = _validationResults.Count == 0
                    ? "✓ 검증 완료"
                    : $"검증 {_validationResults.Count:N0}  ● {errors:N0}  ▲ {warnings:N0}";
            }

            Color statusColor = !_hasValidationRun || _validationResults.Count == 0
                ? (_hasValidationRun ? DataAuthoringTheme.Success : DataAuthoringTheme.Muted)
                : SeverityColor(_validationResults.Max(result => result.Issue.Severity));
            _validationButton.style.color = statusColor;
            _validationButton.style.borderLeftColor = statusColor;
            _validationButton.style.borderRightColor = statusColor;
            _validationButton.style.borderTopColor = statusColor;
            _validationButton.style.borderBottomColor = statusColor;
            _navigationList?.Rebuild();
        }

        private static VisualElement BuildResultRoot(string title, string summary)
        {
            var root = new VisualElement();
            root.style.flexGrow = 1f;
            root.style.backgroundColor = DataAuthoringTheme.Surface;
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.minHeight = 58f;
            header.style.paddingLeft = 16f;
            header.style.paddingRight = 16f;
            header.style.paddingTop = 10f;
            header.style.paddingBottom = 10f;
            header.style.backgroundColor = DataAuthoringTheme.SurfaceRaised;
            header.style.borderBottomWidth = 1f;
            header.style.borderBottomColor = DataAuthoringTheme.Border;
            var heading = new Label(title);
            heading.style.fontSize = 16f;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.flexGrow = 1f;
            header.Add(heading);
            var count = new Label(summary);
            count.style.fontSize = 10f;
            count.style.color = DataAuthoringTheme.Muted;
            header.Add(count);
            root.Add(header);
            return root;
        }

        private static VisualElement BuildEmptyMessage(string message)
        {
            var label = new Label(message);
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginTop = 28f;
            label.style.color = DataAuthoringTheme.Muted;
            return label;
        }

        private static void OpenSpreadsheet()
        {
            if (!EditorApplication.ExecuteMenuItem(SpreadsheetMenuPath))
                EditorUtility.DisplayDialog("대량 편집 열기 실패", SpreadsheetMenuPath, "확인");
        }

        private static string SeveritySymbol(DataAuthoringIssueSeverity severity) => severity switch
        {
            DataAuthoringIssueSeverity.Error => "●",
            DataAuthoringIssueSeverity.Warning => "▲",
            _ => "ℹ"
        };

        private static Color SeverityColor(DataAuthoringIssueSeverity severity) => severity switch
        {
            DataAuthoringIssueSeverity.Error => new Color(1f, 0.35f, 0.3f),
            DataAuthoringIssueSeverity.Warning => new Color(1f, 0.7f, 0.22f),
            _ => new Color(0.4f, 0.72f, 1f)
        };
    }
}
#endif
