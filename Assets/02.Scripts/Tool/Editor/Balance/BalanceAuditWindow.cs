#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 밸런스 데이터 안전망 창.
    /// 1) 스냅샷 diff — 베이스라인 JSON과 현재 에셋 수치를 비교해 의도치 않은 변경을 표시.
    /// 2) 일괄 검증 — 전체 몬스터 ActorDefinitionSO에 BalanceActorDataValidator + Estimator를 실행.
    /// 생성기(스탯/공격 데이터) 실행 전 베이스라인 저장 → 실행 후 비교가 권장 워크플로.
    /// </summary>
    public sealed class BalanceAuditWindow : EditorWindow
    {
        private enum Tab
        {
            SnapshotDiff,
            BatchValidation,
        }

        private Tab _tab = Tab.SnapshotDiff;
        private Vector2 _diffScroll;
        private Vector2 _validationScroll;

        // 스냅샷 diff 상태
        private List<BalanceSnapshotService.DiffEntry> _diffs;
        private string _baselineCreatedAt;
        private string _diffSummary;
        private float _minRelativeChangeFilter;

        // 일괄 검증 상태
        private BalanceScenarioAsset _scenario;
        private readonly List<ValidationRow> _validationRows = new();
        private int _totalErrors;
        private int _totalWarnings;
        private bool _onlyProblems = true;

        private sealed class ValidationRow
        {
            public ActorDefinitionSO Actor;
            public BalanceScenarioResult Result;
            public int ErrorCount;
            public int WarningCount;
            public bool Expanded;
        }

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/밸런스/밸런스 점검 (스냅샷·검증)", priority = UPlaygroundMenuPriority.GameplayBalance + 1)]
        public static void Open()
        {
            var window = GetWindow<BalanceAuditWindow>();
            window.titleContent = new GUIContent("Balance Audit");
            window.minSize = new Vector2(760f, 480f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_scenario == null)
                _scenario = FindFirstScenario();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Toggle(_tab == Tab.SnapshotDiff, "스냅샷 Diff", EditorStyles.toolbarButton))
                    _tab = Tab.SnapshotDiff;
                if (GUILayout.Toggle(_tab == Tab.BatchValidation, "일괄 검증", EditorStyles.toolbarButton))
                    _tab = Tab.BatchValidation;
                GUILayout.FlexibleSpace();
            }

            if (_tab == Tab.SnapshotDiff)
                DrawSnapshotTab();
            else
                DrawValidationTab();
        }

        #region Snapshot Diff Tab

        private void DrawSnapshotTab()
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("베이스라인 저장 (현재 상태)", GUILayout.Height(26f)))
                    SaveBaseline();

                using (new EditorGUI.DisabledScope(!BalanceSnapshotService.HasBaseline))
                {
                    if (GUILayout.Button("베이스라인과 비교", GUILayout.Height(26f)))
                        RunDiff();
                }

                if (GUILayout.Button("폴더 열기", GUILayout.Width(80f), GUILayout.Height(26f)))
                {
                    System.IO.Directory.CreateDirectory(BalanceSnapshotService.SnapshotDirectory);
                    EditorUtility.RevealInFinder(BalanceSnapshotService.SnapshotDirectory);
                }
            }

            string baselineInfo = BalanceSnapshotService.HasBaseline
                ? $"베이스라인: {(_baselineCreatedAt ?? "(비교 시 표시)")} — {BalanceSnapshotService.BaselinePath}"
                : "베이스라인 없음 — 생성기 실행 전 [베이스라인 저장]을 먼저 누르세요.";
            EditorGUILayout.LabelField(baselineInfo, EditorStyles.miniLabel);

            if (_diffs == null)
            {
                EditorGUILayout.HelpBox("워크플로: ① 베이스라인 저장 → ② 생성기 실행/수치 편집 → ③ 베이스라인과 비교 → 의도한 변경만 있는지 확인.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(_diffSummary, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("최소 변화율 필터", GUILayout.Width(96f));
                _minRelativeChangeFilter = EditorGUILayout.Slider(_minRelativeChangeFilter, 0f, 1f, GUILayout.Width(180f));
            }

            _diffScroll = EditorGUILayout.BeginScrollView(_diffScroll);
            int shown = 0;
            for (int i = 0; i < _diffs.Count; i++)
            {
                BalanceSnapshotService.DiffEntry diff = _diffs[i];
                if (diff.Kind == BalanceSnapshotService.DiffKind.ValueChanged && diff.RelativeChange < _minRelativeChangeFilter)
                    continue;

                shown++;
                Rect rect = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(rect, GetDiffColor(diff));
                GUI.Label(new Rect(rect.x + 6f, rect.y + 2f, rect.width - 12f, 16f), diff.ToString(), EditorStyles.label);
            }

            if (shown == 0)
                EditorGUILayout.LabelField(_diffs.Count == 0 ? "변경 없음 — 베이스라인과 동일합니다." : "필터에 걸리는 항목이 없습니다.", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("리포트 콘솔 출력"))
                    LogDiffReport();
                if (GUILayout.Button("현재 상태를 새 베이스라인으로 승인"))
                    SaveBaseline();
            }
        }

        private void SaveBaseline()
        {
            BalanceSnapshotService.Snapshot snapshot = BalanceSnapshotService.Capture();
            BalanceSnapshotService.SaveBaseline(snapshot);
            _baselineCreatedAt = snapshot.createdAt;
            _diffs = null;
            Debug.Log($"[BalanceAudit] 베이스라인 저장 완료 — 액터 {snapshot.actors.Count}개, 플레이어 공격 데이터 {snapshot.playerAttacks.Count}개\n{BalanceSnapshotService.BaselinePath}");
        }

        private void RunDiff()
        {
            BalanceSnapshotService.Snapshot baseline = BalanceSnapshotService.LoadBaseline();
            if (baseline == null)
            {
                _diffSummary = "베이스라인 로드 실패";
                _diffs = new List<BalanceSnapshotService.DiffEntry>();
                return;
            }

            _baselineCreatedAt = baseline.createdAt;
            BalanceSnapshotService.Snapshot current = BalanceSnapshotService.Capture();
            _diffs = BalanceSnapshotService.Diff(baseline, current);

            int changed = 0, added = 0, removed = 0;
            for (int i = 0; i < _diffs.Count; i++)
            {
                switch (_diffs[i].Kind)
                {
                    case BalanceSnapshotService.DiffKind.ValueChanged: changed++; break;
                    case BalanceSnapshotService.DiffKind.Added: added++; break;
                    default: removed++; break;
                }
            }
            _diffSummary = $"변경 {changed} / 추가 {added} / 제거 {removed} (베이스라인 {baseline.createdAt})";
        }

        private void LogDiffReport()
        {
            if (_diffs == null)
                return;

            var sb = new StringBuilder();
            sb.AppendLine($"[BalanceAudit] 스냅샷 diff 리포트 — {_diffSummary}");
            for (int i = 0; i < _diffs.Count; i++)
                sb.AppendLine(_diffs[i].ToString());
            Debug.Log(sb.ToString());
        }

        private static Color GetDiffColor(BalanceSnapshotService.DiffEntry diff)
        {
            if (diff.Kind != BalanceSnapshotService.DiffKind.ValueChanged)
                return new Color(0.35f, 0.25f, 0.1f, 0.45f);
            if (diff.RelativeChange >= 0.5f)
                return new Color(0.5f, 0.12f, 0.12f, 0.5f);
            if (diff.RelativeChange >= 0.15f)
                return new Color(0.45f, 0.38f, 0.1f, 0.45f);
            return new Color(0.2f, 0.2f, 0.22f, 0.35f);
        }

        #endregion

        #region Batch Validation Tab

        private void DrawValidationTab()
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                _scenario = (BalanceScenarioAsset)EditorGUILayout.ObjectField("시나리오", _scenario, typeof(BalanceScenarioAsset), false);
                if (GUILayout.Button("전체 몬스터 검증 실행", GUILayout.Width(160f), GUILayout.Height(20f)))
                    RunBatchValidation();
            }

            if (_validationRows.Count == 0)
            {
                EditorGUILayout.HelpBox("전체 몬스터 ActorDefinitionSO에 대해 데이터 검증 + 밸런스 추정을 일괄 실행합니다. 커밋 전 점검 용도.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                string headline = _totalErrors > 0
                    ? $"오류 {_totalErrors}건 — 데이터 보정 필요"
                    : $"오류 없음 (경고 {_totalWarnings}건)";
                EditorGUILayout.LabelField(headline, _totalErrors > 0 ? EditorStyles.boldLabel : EditorStyles.label);
                GUILayout.FlexibleSpace();
                _onlyProblems = GUILayout.Toggle(_onlyProblems, "문제 있는 액터만", EditorStyles.miniButton, GUILayout.Width(120f));
            }

            _validationScroll = EditorGUILayout.BeginScrollView(_validationScroll);
            for (int i = 0; i < _validationRows.Count; i++)
            {
                ValidationRow row = _validationRows[i];
                if (_onlyProblems && row.ErrorCount == 0 && row.WarningCount == 0)
                    continue;

                DrawValidationRow(row);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawValidationRow(ValidationRow row)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string status = row.Result != null ? row.Result.Status.ToString() : "-";
                    string score = row.Result != null ? row.Result.BalanceScore.ToString("F0") : "-";
                    string label = $"{row.Actor.actorId}  |  오류 {row.ErrorCount} / 경고 {row.WarningCount}  |  {status} (Score {score})";
                    row.Expanded = EditorGUILayout.Foldout(row.Expanded, label, true);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("선택", GUILayout.Width(44f)))
                        Selection.activeObject = row.Actor;
                }

                if (!row.Expanded || row.Result == null)
                    return;

                for (int i = 0; i < row.Result.Messages.Count; i++)
                {
                    BalanceValidationMessage message = row.Result.Messages[i];
                    MessageType type = message.Level switch
                    {
                        BalanceValidationLevel.Error => MessageType.Error,
                        BalanceValidationLevel.Warning => MessageType.Warning,
                        _ => MessageType.Info,
                    };
                    EditorGUILayout.HelpBox(message.Message, type);
                }

                if (!string.IsNullOrEmpty(row.Result.RecommendedAction))
                    EditorGUILayout.LabelField($"권장: {row.Result.RecommendedAction}", EditorStyles.miniLabel);
            }
        }

        private void RunBatchValidation()
        {
            _validationRows.Clear();
            _totalErrors = 0;
            _totalWarnings = 0;

            float assumedDistance = _scenario != null ? _scenario.assumedDistance : 2.5f;
            var fallbackInput = new BalanceScenarioInput(
                _scenario != null ? _scenario.targetDuration : 30f,
                assumedDistance,
                1,
                _scenario != null ? _scenario.manualPlayerAttackPower : 1f,
                _scenario != null ? _scenario.manualPlayerDps : 45f,
                _scenario != null ? _scenario.playerAttackInterval : 1.2f,
                _scenario != null ? _scenario.minAttackOpportunities : 1f);

            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (actor == null || (actor.actorType & ActorType.Monster) == 0)
                    continue;

                BalanceScenarioResult result = BalanceCombatEstimator.Analyze(actor, _scenario, fallbackInput);
                var row = new ValidationRow { Actor = actor, Result = result };
                for (int m = 0; m < result.Messages.Count; m++)
                {
                    if (result.Messages[m].Level == BalanceValidationLevel.Error) row.ErrorCount++;
                    else if (result.Messages[m].Level == BalanceValidationLevel.Warning) row.WarningCount++;
                }

                _totalErrors += row.ErrorCount;
                _totalWarnings += row.WarningCount;
                _validationRows.Add(row);
            }

            _validationRows.Sort((a, b) =>
            {
                int byError = b.ErrorCount.CompareTo(a.ErrorCount);
                if (byError != 0) return byError;
                int byWarning = b.WarningCount.CompareTo(a.WarningCount);
                return byWarning != 0 ? byWarning : string.CompareOrdinal(a.Actor.actorId, b.Actor.actorId);
            });

            Debug.Log($"[BalanceAudit] 일괄 검증 완료 — 몬스터 {_validationRows.Count}개, 오류 {_totalErrors}건, 경고 {_totalWarnings}건");
        }

        private static BalanceScenarioAsset FindFirstScenario()
        {
            string[] guids = AssetDatabase.FindAssets("t:BalanceScenarioAsset");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<BalanceScenarioAsset>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        #endregion
    }
}
#endif
