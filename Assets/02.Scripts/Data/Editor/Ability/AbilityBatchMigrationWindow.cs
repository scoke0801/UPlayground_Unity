using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.Data.Editor.Ability
{
    public sealed class AbilityBatchMigrationWindow : EditorWindow
    {
        private readonly AbilityBatchMigrationOptions _options = new();
        private readonly List<AbilityMigrationPlanEntry> _plan = new();

        private TextField _sourceRoot;
        private TextField _outputRoot;
        private FloatField _abilityCost;
        private FloatField _ultimateCost;
        private FloatField _abilityCooldown;
        private FloatField _ultimateCooldown;
        private Toggle _legacyFallback;
        private Label _summary;
        private ScrollView _results;
        private Button _executeButton;

        private static readonly Color Background = new(0.055f, 0.075f, 0.10f);
        private static readonly Color Panel = new(0.08f, 0.10f, 0.13f);
        private static readonly Color Border = new(0.22f, 0.27f, 0.32f);
        private static readonly Color Accent = new(0.18f, 0.52f, 0.92f);

        [MenuItem(
            "UPlayGround/Ability/기존 데이터 일괄 마이그레이션",
            priority = 20)]
        public static void Open()
        {
            AbilityBatchMigrationWindow window =
                GetWindow<AbilityBatchMigrationWindow>();
            window.titleContent = new GUIContent("Ability Migration");
            window.minSize = new Vector2(900f, 620f);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.backgroundColor = Background;
            rootVisualElement.style.color = new Color(0.88f, 0.9f, 0.94f);

            BuildHeader();
            BuildOptions();
            BuildActions();

            _summary = new Label("미리보기를 실행하면 원본별 변환 계획을 표시합니다.");
            _summary.style.marginLeft = 10f;
            _summary.style.marginTop = 8f;
            _summary.style.marginBottom = 6f;
            _summary.style.unityFontStyleAndWeight = FontStyle.Bold;
            rootVisualElement.Add(_summary);

            _results = new ScrollView(ScrollViewMode.Vertical);
            _results.style.flexGrow = 1f;
            _results.style.marginLeft = 8f;
            _results.style.marginRight = 8f;
            _results.style.marginBottom = 8f;
            _results.style.borderTopColor = Border;
            _results.style.borderBottomColor = Border;
            _results.style.borderLeftColor = Border;
            _results.style.borderRightColor = Border;
            _results.style.borderTopWidth = 1f;
            _results.style.borderBottomWidth = 1f;
            _results.style.borderLeftWidth = 1f;
            _results.style.borderRightWidth = 1f;
            rootVisualElement.Add(_results);
        }

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.style.height = 62f;
            header.style.flexShrink = 0f;
            header.style.paddingLeft = 12f;
            header.style.paddingTop = 8f;
            header.style.backgroundColor = Panel;
            header.style.borderBottomColor = Border;
            header.style.borderBottomWidth = 1f;

            var title = new Label("기존 PlayerAttackDataSO → Gameplay Ability 일괄 마이그레이션");
            title.style.fontSize = 14f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);

            var notice = new Label(
                "미리보기는 읽기 전용입니다. 실행 시에도 원본은 수정하지 않으며, "
                + "기존 출력 경로 또는 Ability ID와 충돌하는 항목은 건너뜁니다.");
            notice.style.marginTop = 5f;
            notice.style.color = new Color(0.65f, 0.72f, 0.82f);
            header.Add(notice);
            rootVisualElement.Add(header);
        }

        private void BuildOptions()
        {
            var box = new VisualElement();
            box.style.marginLeft = 8f;
            box.style.marginRight = 8f;
            box.style.marginTop = 8f;
            box.style.paddingLeft = 10f;
            box.style.paddingRight = 10f;
            box.style.paddingTop = 8f;
            box.style.paddingBottom = 8f;
            box.style.backgroundColor = Panel;

            _sourceRoot = MakeTextField("원본 검색 루트", _options.sourceRoot);
            _outputRoot = MakeTextField("출력 루트", _options.outputRoot);
            box.Add(_sourceRoot);
            box.Add(_outputRoot);

            var values = new VisualElement();
            values.style.flexDirection = FlexDirection.Row;
            values.style.marginTop = 5f;

            _abilityCost = MakeFloatField("Ability 비용", _options.abilityCost);
            _ultimateCost = MakeFloatField("Ultimate 비용", _options.ultimateCost);
            _abilityCooldown =
                MakeFloatField("Ability 쿨다운", _options.abilityCooldown);
            _ultimateCooldown =
                MakeFloatField("Ultimate 쿨다운", _options.ultimateCooldown);

            values.Add(_abilityCost);
            values.Add(_ultimateCost);
            values.Add(_abilityCooldown);
            values.Add(_ultimateCooldown);
            box.Add(values);

            _legacyFallback = new Toggle(
                "정의가 없는 슬롯은 skillAttackList[0/1]로 변환")
            {
                value = _options.includeLegacyFallback,
            };
            _legacyFallback.style.marginTop = 6f;
            box.Add(_legacyFallback);

            var hint = new HelpBox(
                "비용·쿨다운 수치는 PlayerSkillGauge 기본 직렬화 값(0/100, 3/12)을 "
                + "초깃값으로 제공합니다. 캐릭터 프리팹별 값이 다르면 실행 전에 조정하세요.",
                HelpBoxMessageType.Warning);
            hint.style.marginTop = 6f;
            box.Add(hint);
            rootVisualElement.Add(box);
        }

        private void BuildActions()
        {
            var row = new VisualElement();
            row.style.height = 38f;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 8f;
            row.style.paddingRight = 8f;

            var preview = new Button(RefreshPreview)
            {
                text = "읽기 전용 미리보기",
            };
            preview.style.height = 26f;
            preview.style.minWidth = 150f;
            row.Add(preview);

            var selectReady = new Button(() => SetReadySelection(true))
            {
                text = "변환 가능 전체 선택",
            };
            selectReady.style.height = 26f;
            selectReady.style.marginLeft = 5f;
            row.Add(selectReady);

            var clear = new Button(() => SetReadySelection(false))
            {
                text = "선택 해제",
            };
            clear.style.height = 26f;
            clear.style.marginLeft = 5f;
            row.Add(clear);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            row.Add(spacer);

            _executeButton = new Button(ExecuteSelected)
            {
                text = "선택 항목 마이그레이션 실행…",
            };
            _executeButton.style.height = 28f;
            _executeButton.style.minWidth = 210f;
            _executeButton.style.backgroundColor = Accent;
            _executeButton.style.color = Color.white;
            _executeButton.SetEnabled(false);
            row.Add(_executeButton);
            rootVisualElement.Add(row);
        }

        private void RefreshPreview()
        {
            if (!TryReadOptions())
                return;

            try
            {
                _plan.Clear();
                _plan.AddRange(AbilityBatchMigrationService.BuildPlan(_options));
                RebuildResults();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "마이그레이션 미리보기 실패",
                    exception.Message,
                    "확인");
            }
        }

        private void RebuildResults()
        {
            _results.Clear();
            for (int i = 0; i < _plan.Count; i++)
                _results.Add(BuildResultRow(_plan[i]));
            UpdateSummary();
        }

        private VisualElement BuildResultRow(AbilityMigrationPlanEntry entry)
        {
            var row = new VisualElement();
            row.style.minHeight = 66f;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 8f;
            row.style.paddingRight = 8f;
            row.style.paddingTop = 5f;
            row.style.paddingBottom = 5f;
            row.style.borderBottomColor = Border;
            row.style.borderBottomWidth = 1f;

            var selected = new Toggle { value = entry.Selected };
            selected.SetEnabled(entry.Status == AbilityMigrationPlanStatus.Ready);
            selected.RegisterValueChangedCallback(change =>
            {
                entry.Selected = change.newValue;
                UpdateSummary();
            });
            row.Add(selected);

            var icon = new Label(StatusIcon(entry.Status));
            icon.style.width = 24f;
            icon.style.fontSize = 15f;
            icon.style.color = StatusColor(entry.Status);
            row.Add(icon);

            var text = new VisualElement();
            text.style.flexGrow = 1f;
            text.style.flexShrink = 1f;

            var name = new Label(
                $"{entry.Source.name}  ·  {StatusLabel(entry.Status)}");
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            text.Add(name);

            var source = new Label(entry.SourcePath);
            source.style.color = new Color(0.55f, 0.62f, 0.7f);
            source.style.fontSize = 10f;
            text.Add(source);

            var message = new Label(entry.Message);
            message.style.whiteSpace = WhiteSpace.Normal;
            message.style.color = StatusColor(entry.Status);
            text.Add(message);
            row.Add(text);

            var output = new Label(entry.OutputFolder);
            output.style.width = 310f;
            output.style.flexShrink = 0f;
            output.style.whiteSpace = WhiteSpace.Normal;
            output.style.color = new Color(0.62f, 0.68f, 0.76f);
            row.Add(output);

            var ping = new Button(() =>
            {
                Selection.activeObject = entry.Source;
                EditorGUIUtility.PingObject(entry.Source);
            })
            {
                text = "원본",
            };
            ping.style.width = 48f;
            row.Add(ping);
            return row;
        }

        private void UpdateSummary()
        {
            int ready = _plan.Count(
                entry => entry.Status == AbilityMigrationPlanStatus.Ready);
            int selected = _plan.Count(
                entry => entry.Status == AbilityMigrationPlanStatus.Ready
                         && entry.Selected);
            int conflict = _plan.Count(
                entry => entry.Status == AbilityMigrationPlanStatus.Conflict);
            int invalid = _plan.Count(
                entry => entry.Status == AbilityMigrationPlanStatus.InvalidSource);
            int noData = _plan.Count(
                entry => entry.Status == AbilityMigrationPlanStatus.NoConvertibleData);

            _summary.text =
                $"검색 {_plan.Count} · 변환 가능 {ready} · 선택 {selected} · "
                + $"충돌 {conflict} · 원본 오류 {invalid} · 데이터 없음 {noData}";
            _executeButton.SetEnabled(selected > 0);
        }

        private void SetReadySelection(bool selected)
        {
            for (int i = 0; i < _plan.Count; i++)
                if (_plan[i].Status == AbilityMigrationPlanStatus.Ready)
                    _plan[i].Selected = selected;
            RebuildResults();
        }

        private void ExecuteSelected()
        {
            if (!TryReadOptions())
                return;

            int selected = _plan.Count(
                entry => entry.Status == AbilityMigrationPlanStatus.Ready
                         && entry.Selected);
            if (selected == 0)
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Ability 일괄 마이그레이션 실행",
                $"{selected}개 PlayerAttackDataSO를 변환합니다.\n\n"
                + $"출력: {_options.outputRoot}\n"
                + "원본 에셋은 수정하지 않습니다.\n"
                + "기존 출력은 덮어쓰지 않으며 충돌 시 건너뜁니다.\n\n"
                + "생성된 Ability/Set을 실제 캐릭터에 연결하지는 않습니다.",
                "생성 실행",
                "취소");
            if (!confirmed)
                return;

            AbilityBatchMigrationResult result =
                AbilityBatchMigrationService.Execute(_plan, _options);
            Debug.Log(
                $"[AbilityMigration] 완료 소스 {result.ConvertedSources}, "
                + $"Ability {result.CreatedAbilities}, Payload {result.CreatedPayloads}, "
                + $"건너뜀 {result.SkippedSources}, 리포트 {result.ReportPath}");
            EditorUtility.DisplayDialog(
                "Ability 일괄 마이그레이션 완료",
                $"완료 소스: {result.ConvertedSources}\n"
                + $"생성 Ability: {result.CreatedAbilities}\n"
                + $"생성 Payload: {result.CreatedPayloads}\n"
                + $"건너뜀: {result.SkippedSources}\n\n"
                + $"비교 리포트: {result.ReportPath}",
                "확인");
            RefreshPreview();
        }

        private bool TryReadOptions()
        {
            _options.sourceRoot = _sourceRoot.value?.Trim();
            _options.outputRoot = _outputRoot.value?.Trim();
            _options.abilityCost = Mathf.Max(0f, _abilityCost.value);
            _options.ultimateCost = Mathf.Max(0f, _ultimateCost.value);
            _options.abilityCooldown = Mathf.Max(0f, _abilityCooldown.value);
            _options.ultimateCooldown = Mathf.Max(0f, _ultimateCooldown.value);
            _options.includeLegacyFallback = _legacyFallback.value;

            if (string.IsNullOrWhiteSpace(_options.sourceRoot)
                || string.IsNullOrWhiteSpace(_options.outputRoot))
            {
                EditorUtility.DisplayDialog(
                    "입력 필요",
                    "원본 검색 루트와 출력 루트를 입력하세요.",
                    "확인");
                return false;
            }
            return true;
        }

        private static TextField MakeTextField(string label, string value)
        {
            var field = new TextField(label) { value = value };
            field.labelElement.style.minWidth = 120f;
            field.style.marginBottom = 3f;
            return field;
        }

        private static FloatField MakeFloatField(string label, float value)
        {
            var field = new FloatField(label) { value = value };
            field.style.flexGrow = 1f;
            field.style.marginRight = 8f;
            field.labelElement.style.minWidth = 95f;
            return field;
        }

        private static string StatusIcon(AbilityMigrationPlanStatus status) =>
            status switch
            {
                AbilityMigrationPlanStatus.Ready => "✓",
                AbilityMigrationPlanStatus.NoConvertibleData => "○",
                AbilityMigrationPlanStatus.InvalidSource => "!",
                AbilityMigrationPlanStatus.Conflict => "×",
                _ => "?",
            };

        private static string StatusLabel(AbilityMigrationPlanStatus status) =>
            status switch
            {
                AbilityMigrationPlanStatus.Ready => "변환 가능",
                AbilityMigrationPlanStatus.NoConvertibleData => "변환 데이터 없음",
                AbilityMigrationPlanStatus.InvalidSource => "원본 오류",
                AbilityMigrationPlanStatus.Conflict => "충돌",
                _ => status.ToString(),
            };

        private static Color StatusColor(AbilityMigrationPlanStatus status) =>
            status switch
            {
                AbilityMigrationPlanStatus.Ready =>
                    new Color(0.38f, 0.85f, 0.54f),
                AbilityMigrationPlanStatus.NoConvertibleData =>
                    new Color(0.58f, 0.64f, 0.72f),
                AbilityMigrationPlanStatus.InvalidSource =>
                    new Color(1f, 0.68f, 0.2f),
                AbilityMigrationPlanStatus.Conflict =>
                    new Color(1f, 0.35f, 0.35f),
                _ => Color.white,
            };
    }
}
