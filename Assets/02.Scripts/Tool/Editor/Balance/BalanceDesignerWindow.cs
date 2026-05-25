#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.AI.BehaviorTree.Editor;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Balance
{
    public sealed class BalanceDesignerWindow : EditorWindow
    {
        private const float ListWidth = 260f;
        private const float RowHeight = 24f;

        private ActorDatabase _database;
        private BalanceScenarioAsset _scenario;
        private ActorDefinitionSO _selectedActor;
        private Vector2 _actorListScroll;
        private Vector2 _resultScroll;
        private Vector2 _detailScroll;
        private string _searchFilter = "";
        private ActorType _actorTypeFilter = ActorType.Monster;
        private readonly List<BalanceScenarioResult> _results = new();
        private BalanceScenarioResult _selectedResult;

        private float _targetDuration = 30f;
        private float _assumedDistance = 2.5f;
        private int _monsterLevel = 1;
        private float _playerAttackPower = 1f;
        private float _manualPlayerDps = 18f;
        private float _playerAttackInterval = 1.2f;
        private float _minAttackOpportunities = 1f;

        [MenuItem("UPlayGround/Gameplay/Balance/Balance Designer", priority = 20)]
        public static void Open()
        {
            var window = GetWindow<BalanceDesignerWindow>();
            window.titleContent = new GUIContent("Balance Designer");
            window.minSize = new Vector2(980f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            TryAutoLoadDatabase();
            BindSelection();
        }

        private void OnSelectionChange()
        {
            BindSelection();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawScenarioPanel();

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            DrawActorList();
            DrawMainPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                _database = (ActorDatabase)EditorGUILayout.ObjectField(_database, typeof(ActorDatabase), false, GUILayout.Width(220f));
                if (EditorGUI.EndChangeCheck())
                {
                    _results.Clear();
                    _selectedResult = null;
                }

                _scenario = (BalanceScenarioAsset)EditorGUILayout.ObjectField(_scenario, typeof(BalanceScenarioAsset), false, GUILayout.Width(220f));

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(_selectedActor == null))
                {
                    if (GUILayout.Button("Analyze Selected", EditorStyles.toolbarButton, GUILayout.Width(112f)))
                        AnalyzeSelected();
                }

                using (new EditorGUI.DisabledScope(_database == null))
                {
                    if (GUILayout.Button("Analyze Database", EditorStyles.toolbarButton, GUILayout.Width(118f)))
                        AnalyzeDatabase();

                    if (GUILayout.Button("Generate Missing All", EditorStyles.toolbarButton, GUILayout.Width(134f)))
                        GenerateMissingForDatabase();
                }

                using (new EditorGUI.DisabledScope(_results.Count == 0))
                {
                    if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(86f)))
                        ExportCsv();
                }
            }
        }

        private void DrawScenarioPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_scenario != null)
                {
                    EditorGUILayout.LabelField(
                        $"Scenario: {_scenario.name} / Player {_scenario.playerCharacter} / Target {_scenario.targetDuration:F1}s / Distance {_scenario.assumedDistance:F1}",
                        EditorStyles.boldLabel);
                    return;
                }

                EditorGUILayout.LabelField("임시 분석 조건", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _targetDuration = EditorGUILayout.FloatField("기준 시간", Mathf.Max(1f, _targetDuration));
                    _assumedDistance = EditorGUILayout.FloatField("기준 거리", Mathf.Max(0f, _assumedDistance));
                    _monsterLevel = EditorGUILayout.IntField("몬스터 레벨", Mathf.Max(1, _monsterLevel));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _playerAttackPower = EditorGUILayout.FloatField("플레이어 공격력", Mathf.Max(0f, _playerAttackPower));
                    _manualPlayerDps = EditorGUILayout.FloatField("기준 공격 DPS", Mathf.Max(0f, _manualPlayerDps));
                    _playerAttackInterval = EditorGUILayout.FloatField("공격 간격", Mathf.Max(0.05f, _playerAttackInterval));
                    _minAttackOpportunities = EditorGUILayout.FloatField("최소 공격 기회", Mathf.Max(0f, _minAttackOpportunities));
                }
            }
        }

        private void DrawActorList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(ListWidth), GUILayout.ExpandHeight(true)))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("검색", GUILayout.Width(34f));
                    _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
                    if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                        _searchFilter = "";
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("타입", GUILayout.Width(34f));
                    _actorTypeFilter = (ActorType)EditorGUILayout.EnumFlagsField(_actorTypeFilter, EditorStyles.toolbarPopup);
                }

                if (_database == null)
                {
                    EditorGUILayout.HelpBox("ActorDatabase를 선택하세요.", MessageType.Info);
                    return;
                }

                _actorListScroll = EditorGUILayout.BeginScrollView(_actorListScroll);
                IReadOnlyList<ActorDefinitionSO> actors = _database.All;
                for (int i = 0; i < actors.Count; i++)
                {
                    ActorDefinitionSO actor = actors[i];
                    if (!ShouldShowActor(actor))
                        continue;

                    DrawActorListItem(actor);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawActorListItem(ActorDefinitionSO actor)
        {
            Rect rect = GUILayoutUtility.GetRect(ListWidth - 8f, 42f);
            bool selected = _selectedActor == actor;
            if (selected)
                EditorGUI.DrawRect(rect, new Color(0.18f, 0.36f, 0.58f));

            string title = string.IsNullOrWhiteSpace(actor.displayName) ? actor.actorId : actor.displayName;
            GUI.Label(new Rect(rect.x + 8f, rect.y + 4f, rect.width - 16f, 18f), title, selected ? EditorStyles.whiteBoldLabel : EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 22f, rect.width - 16f, 16f), $"{actor.actorId}  Lv.{actor.level}  {actor.grade}", selected ? EditorStyles.whiteMiniLabel : EditorStyles.miniLabel);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selectedActor = actor;
                _selectedResult = FindResult(actor);
                Selection.activeObject = actor;
                Event.current.Use();
            }
        }

        private void DrawMainPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                DrawSelectedSummary();
                DrawResultTable();
                DrawDetailPanel();
            }
        }

        private void DrawSelectedSummary()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_selectedActor == null)
                {
                    EditorGUILayout.LabelField("좌측에서 ActorDefinitionSO를 선택하세요.", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{_selectedActor.displayName} [{_selectedActor.actorId}]", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Inspector", GUILayout.Width(86f)))
                        Selection.activeObject = _selectedActor;

                    using (new EditorGUI.DisabledScope(_selectedActor.attackData == null))
                    {
                        if (GUILayout.Button("Attack Generator", GUILayout.Width(128f)))
                            UPlayGround.Editor.AttackDataFromMotionSetWindow.Open(_selectedActor.attackData);
                    }

                    using (new EditorGUI.DisabledScope(_selectedActor.behaviorData == null || _selectedActor.behaviorData.behaviorTree == null))
                    {
                        if (GUILayout.Button("Open BT", GUILayout.Width(86f)))
                            BehaviorTreeEditorWindow.Open(_selectedActor.behaviorData.behaviorTree);
                    }

                    using (new EditorGUI.DisabledScope(!BalanceDataAutoGenerator.HasMissingData(_selectedActor)))
                    {
                        if (GUILayout.Button("Generate Missing", GUILayout.Width(128f)))
                            GenerateMissingForSelected();
                    }
                }

                string statSummary = _selectedActor.statData != null
                    ? $"HP {_selectedActor.statData.GetBase(StatType.MaxHealth):F0} / ATK {_selectedActor.statData.GetBase(StatType.AttackPower):F2} / DEF {_selectedActor.statData.GetBase(StatType.Defense):F2}"
                    : "statData 없음";
                string attackSummary = _selectedActor.attackData != null
                    ? $"Skills {_selectedActor.attackData.skills?.Count ?? 0} / GlobalCD {_selectedActor.attackData.globalCooldown:F2}"
                    : "attackData 없음";

                EditorGUILayout.LabelField($"{statSummary}  |  {attackSummary}", EditorStyles.miniLabel);
            }
        }

        private void DrawResultTable()
        {
            Rect header = GUILayoutUtility.GetRect(0f, 24f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(header, new Color(0.14f, 0.14f, 0.16f));
            DrawResultCells(header, "Actor", "Status", "플레이어 생존", "몬스터 처치", "플레이어 DPS", "적 DPS", "Strong%", "공격 기회", "Summary", true);

            _resultScroll = EditorGUILayout.BeginScrollView(_resultScroll, GUILayout.MinHeight(160f), GUILayout.MaxHeight(240f));
            for (int i = 0; i < _results.Count; i++)
                DrawResultRow(_results[i]);
            EditorGUILayout.EndScrollView();
        }

        private void DrawResultRow(BalanceScenarioResult result)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
            if (_selectedResult == result)
                EditorGUI.DrawRect(rect, new Color(0.16f, 0.32f, 0.48f));
            else if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, GetStatusColor(result.Status, 0.18f));

            DrawResultCells(
                rect,
                result.Actor != null ? result.Actor.actorId : "(null)",
                result.Status.ToString(),
                BalanceCombatEstimator.FormatTime(result.PlayerTimeToDeath),
                BalanceCombatEstimator.FormatTime(result.MonsterTimeToDeath),
                result.PlayerExpectedDps.ToString("F1"),
                result.EnemyExpectedDps.ToString("F1"),
                $"{result.StrongAttackChance * 100f:F0}%",
                result.EnemyAttackOpportunities.ToString("F1"),
                result.Summary,
                false);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selectedResult = result;
                _selectedActor = result.Actor;
                if (_selectedActor != null)
                    Selection.activeObject = _selectedActor;
                Event.current.Use();
            }
        }

        private void DrawResultCells(
            Rect rect,
            string actor,
            string status,
            string playerTtd,
            string monsterTtd,
            string playerDps,
            string enemyDps,
            string strongChance,
            string opportunities,
            string summary,
            bool header)
        {
            GUIStyle style = header ? EditorStyles.boldLabel : EditorStyles.label;
            float x = rect.x + 6f;
            Label(new Rect(x, rect.y + 3f, 160f, 18f), actor, style); x += 164f;
            Label(new Rect(x, rect.y + 3f, 82f, 18f), status, style); x += 86f;
            Label(new Rect(x, rect.y + 3f, 92f, 18f), playerTtd, style); x += 96f;
            Label(new Rect(x, rect.y + 3f, 92f, 18f), monsterTtd, style); x += 96f;
            Label(new Rect(x, rect.y + 3f, 82f, 18f), playerDps, style); x += 86f;
            Label(new Rect(x, rect.y + 3f, 64f, 18f), enemyDps, style); x += 68f;
            Label(new Rect(x, rect.y + 3f, 64f, 18f), strongChance, style); x += 68f;
            Label(new Rect(x, rect.y + 3f, 82f, 18f), opportunities, style); x += 86f;
            Label(new Rect(x, rect.y + 3f, rect.xMax - x - 6f, 18f), summary, style);
        }

        private static void Label(Rect rect, string text, GUIStyle style)
            => GUI.Label(rect, text ?? string.Empty, style);

        private void DrawDetailPanel()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("Detail", EditorStyles.boldLabel);
                if (_selectedResult == null)
                {
                    EditorGUILayout.LabelField("분석 결과를 선택하세요.", EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                DrawCombatSummary(_selectedResult);
                DrawMessages(_selectedResult);
                DrawSkillBreakdown(_selectedResult);
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawCombatSummary(BalanceScenarioResult result)
        {
            EditorGUILayout.LabelField("전투 시간 요약", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"몬스터 HP {result.MonsterHealth:F0} / 플레이어 공격력 {result.PlayerAttackPower:F2} / 플레이어 예상 DPS {result.PlayerExpectedDps:F1}",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                $"몬스터 처치 예상 시간 {BalanceCombatEstimator.FormatTime(result.MonsterTimeToDeath)} / 플레이어 생존 예상 시간 {BalanceCombatEstimator.FormatTime(result.PlayerTimeToDeath)}",
                EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                $"공격 확률 Basic {result.BasicAttackChance * 100f:F0}% / Heavy {result.HeavyAttackChance * 100f:F0}% / Skill {result.SkillAttackChance * 100f:F0}% / 강한 공격 {result.StrongAttackChance * 100f:F0}%",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(6f);
        }

        private void DrawMessages(BalanceScenarioResult result)
        {
            EditorGUILayout.LabelField("검증 메시지", EditorStyles.boldLabel);
            if (result.Messages.Count == 0)
            {
                EditorGUILayout.LabelField("문제 없음", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < result.Messages.Count; i++)
            {
                BalanceValidationMessage message = result.Messages[i];
                MessageType type = message.Level switch
                {
                    BalanceValidationLevel.Error => MessageType.Error,
                    BalanceValidationLevel.Warning => MessageType.Warning,
                    _ => MessageType.Info,
                };
                EditorGUILayout.HelpBox(message.Message, type);
            }
        }

        private void DrawSkillBreakdown(BalanceScenarioResult result)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("공격 기여도", EditorStyles.boldLabel);
            if (result.SkillBreakdowns.Count == 0)
            {
                EditorGUILayout.LabelField("사용 가능한 공격 없음", EditorStyles.miniLabel);
                return;
            }

            Rect header = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(header, new Color(0.13f, 0.13f, 0.15f));
            DrawSkillCells(header, "AnimKey", "Category", "Chance", "Damage", "CD", "DPS", "Hits", true);

            for (int i = 0; i < result.SkillBreakdowns.Count; i++)
            {
                BalanceSkillBreakdown skill = result.SkillBreakdowns[i];
                Rect row = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
                if (i % 2 == 1)
                    EditorGUI.DrawRect(row, new Color(0f, 0f, 0f, 0.12f));
                DrawSkillCells(
                    row,
                    skill.Name,
                    skill.Category,
                    $"{skill.SelectionChance * 100f:F0}%",
                    skill.Damage.ToString("F1"),
                    skill.Cooldown.ToString("F1"),
                    skill.DpsContribution.ToString("F1"),
                    skill.HitPhaseCount.ToString(),
                    false);
            }
        }

        private void DrawSkillCells(Rect rect, string name, string category, string chance, string damage, string cooldown, string dps, string hits, bool header)
        {
            GUIStyle style = header ? EditorStyles.boldLabel : EditorStyles.label;
            float x = rect.x + 6f;
            Label(new Rect(x, rect.y + 3f, 150f, 16f), name, style); x += 154f;
            Label(new Rect(x, rect.y + 3f, 80f, 16f), category, style); x += 84f;
            Label(new Rect(x, rect.y + 3f, 70f, 16f), chance, style); x += 74f;
            Label(new Rect(x, rect.y + 3f, 70f, 16f), damage, style); x += 74f;
            Label(new Rect(x, rect.y + 3f, 54f, 16f), cooldown, style); x += 58f;
            Label(new Rect(x, rect.y + 3f, 58f, 16f), dps, style); x += 62f;
            Label(new Rect(x, rect.y + 3f, 44f, 16f), hits, style);
        }

        private void AnalyzeSelected()
        {
            if (_selectedActor == null)
                return;

            BalanceScenarioResult result = BalanceCombatEstimator.Analyze(_selectedActor, _scenario, CreateFallbackInput());
            int index = _results.FindIndex(x => x.Actor == _selectedActor);
            if (index >= 0)
                _results[index] = result;
            else
                _results.Add(result);
            _selectedResult = result;
        }

        private void AnalyzeDatabase()
        {
            _results.Clear();
            _selectedResult = null;

            if (_database == null)
                return;

            IReadOnlyList<ActorDefinitionSO> actors = _database.All;
            for (int i = 0; i < actors.Count; i++)
            {
                ActorDefinitionSO actor = actors[i];
                if (actor == null)
                    continue;
                if (_actorTypeFilter != ActorType.None && (actor.actorType & _actorTypeFilter) == 0)
                    continue;

                _results.Add(BalanceCombatEstimator.Analyze(actor, _scenario, CreateFallbackInput()));
            }

            _selectedResult = FindResult(_selectedActor);
        }

        private void GenerateMissingForSelected()
        {
            if (_selectedActor == null)
                return;

            BalanceDataAutoGenerator.GenerationSummary summary = BalanceDataAutoGenerator.GenerateMissing(_selectedActor);
            AnalyzeSelected();

            if (summary.CreatedAny)
                EditorUtility.DisplayDialog("누락 데이터 생성", BuildGenerationMessage(summary), "확인");
            else
                EditorUtility.DisplayDialog("누락 데이터 생성", "생성할 누락 데이터가 없습니다.", "확인");
        }

        private void GenerateMissingForDatabase()
        {
            if (_database == null)
                return;

            if (!EditorUtility.DisplayDialog(
                    "누락 데이터 일괄 생성",
                    "현재 필터와 관계없이 ActorDatabase의 모든 몬스터 누락 데이터를 생성하고 연결합니다.\n계속하시겠습니까?",
                    "생성",
                    "취소"))
            {
                return;
            }

            int createdCount = 0;
            IReadOnlyList<ActorDefinitionSO> actors = _database.All;
            for (int i = 0; i < actors.Count; i++)
            {
                ActorDefinitionSO actor = actors[i];
                if (actor == null || !BalanceDataAutoGenerator.HasMissingData(actor))
                    continue;

                BalanceDataAutoGenerator.GenerationSummary summary = BalanceDataAutoGenerator.GenerateMissing(actor);
                createdCount += summary.CreatedCount;
            }

            AnalyzeDatabase();
            EditorUtility.DisplayDialog("누락 데이터 일괄 생성", $"생성된 에셋: {createdCount}개", "확인");
        }

        private static string BuildGenerationMessage(BalanceDataAutoGenerator.GenerationSummary summary)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"생성된 에셋: {summary.CreatedCount}개");
            if (!string.IsNullOrEmpty(summary.StatDataPath))
                builder.AppendLine(summary.StatDataPath);
            if (!string.IsNullOrEmpty(summary.AttackDataPath))
                builder.AppendLine(summary.AttackDataPath);
            if (summary.GeneratedAttackSkillCount > 0)
                builder.AppendLine($"공격 데이터: Motion 기반 {summary.GeneratedAttackSkillCount}개 스킬 생성");
            if (!string.IsNullOrEmpty(summary.MotionSetSource))
                builder.AppendLine($"MotionSet: {summary.MotionSetSource}");
            if (!string.IsNullOrEmpty(summary.BehaviorDataPath))
                builder.AppendLine(summary.BehaviorDataPath);
            if (!string.IsNullOrEmpty(summary.BehaviorTreePath))
                builder.AppendLine(summary.BehaviorTreePath);
            return builder.ToString();
        }

        private BalanceScenarioInput CreateFallbackInput()
        {
            return new BalanceScenarioInput(
                Mathf.Max(1f, _targetDuration),
                Mathf.Max(0f, _assumedDistance),
                Mathf.Max(1, _monsterLevel),
                Mathf.Max(0f, _playerAttackPower),
                Mathf.Max(0f, _manualPlayerDps),
                Mathf.Max(0.05f, _playerAttackInterval),
                Mathf.Max(0f, _minAttackOpportunities));
        }

        private void ExportCsv()
        {
            string path = EditorUtility.SaveFilePanel("Balance 결과 CSV 저장", Application.dataPath, "BalanceDesignerResults.csv", "csv");
            if (string.IsNullOrWhiteSpace(path))
                return;

            var builder = new StringBuilder();
            builder.AppendLine("actorId,displayName,level,grade,status,targetDuration,playerSurvivalSeconds,monsterKillSeconds,monsterHp,playerAttackPower,playerDps,enemyDps,basicChance,heavyChance,skillChance,strongChance,attackOpportunities,availableSkills,summary");
            for (int i = 0; i < _results.Count; i++)
            {
                BalanceScenarioResult r = _results[i];
                ActorDefinitionSO actor = r.Actor;
                builder.Append(Escape(actor != null ? actor.actorId : ""));
                builder.Append(',');
                builder.Append(Escape(actor != null ? actor.displayName : ""));
                builder.Append(',');
                builder.Append(r.MonsterLevel);
                builder.Append(',');
                builder.Append(actor != null ? actor.grade.ToString() : "");
                builder.Append(',');
                builder.Append(r.Status);
                builder.Append(',');
                builder.Append(r.TargetDuration.ToString("F2"));
                builder.Append(',');
                builder.Append(r.PlayerTimeToDeath.ToString("F2"));
                builder.Append(',');
                builder.Append(r.MonsterTimeToDeath.ToString("F2"));
                builder.Append(',');
                builder.Append(r.MonsterHealth.ToString("F2"));
                builder.Append(',');
                builder.Append(r.PlayerAttackPower.ToString("F2"));
                builder.Append(',');
                builder.Append(r.PlayerExpectedDps.ToString("F2"));
                builder.Append(',');
                builder.Append(r.EnemyExpectedDps.ToString("F2"));
                builder.Append(',');
                builder.Append(r.BasicAttackChance.ToString("F4"));
                builder.Append(',');
                builder.Append(r.HeavyAttackChance.ToString("F4"));
                builder.Append(',');
                builder.Append(r.SkillAttackChance.ToString("F4"));
                builder.Append(',');
                builder.Append(r.StrongAttackChance.ToString("F4"));
                builder.Append(',');
                builder.Append(r.EnemyAttackOpportunities.ToString("F2"));
                builder.Append(',');
                builder.Append(r.AvailableSkillCount.ToString("F0"));
                builder.Append(',');
                builder.Append(Escape(r.Summary));
                builder.AppendLine();
            }

            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Export CSV", $"저장 완료\n{path}", "확인");
        }

        private static string Escape(string value)
        {
            value ??= string.Empty;
            if (!value.Contains(",") && !value.Contains("\"") && !value.Contains("\n"))
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private bool ShouldShowActor(ActorDefinitionSO actor)
        {
            if (actor == null)
                return false;

            if (_actorTypeFilter != ActorType.None && (actor.actorType & _actorTypeFilter) == 0)
                return false;

            if (string.IsNullOrWhiteSpace(_searchFilter))
                return true;

            return Contains(actor.actorId, _searchFilter) || Contains(actor.displayName, _searchFilter);
        }

        private static bool Contains(string source, string filter)
            => !string.IsNullOrEmpty(source) && source.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;

        private BalanceScenarioResult FindResult(ActorDefinitionSO actor)
        {
            if (actor == null)
                return null;

            for (int i = 0; i < _results.Count; i++)
                if (_results[i].Actor == actor)
                    return _results[i];
            return null;
        }

        private void TryAutoLoadDatabase()
        {
            if (_database != null)
                return;

            string[] guids = AssetDatabase.FindAssets("t:ActorDatabase");
            if (guids.Length > 0)
                _database = AssetDatabase.LoadAssetAtPath<ActorDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private void BindSelection()
        {
            if (Selection.activeObject is ActorDefinitionSO actor)
            {
                _selectedActor = actor;
                _selectedResult = FindResult(actor);
            }
            else if (Selection.activeObject is ActorDatabase database)
            {
                _database = database;
            }
            else if (Selection.activeObject is BalanceScenarioAsset scenario)
            {
                _scenario = scenario;
            }
        }

        private static Color GetStatusColor(BalanceCheckStatus status, float alpha)
        {
            Color color = status switch
            {
                BalanceCheckStatus.InvalidData => new Color(0.75f, 0.18f, 0.18f),
                BalanceCheckStatus.TooEasy => new Color(0.86f, 0.58f, 0.18f),
                BalanceCheckStatus.TooLethal => new Color(0.82f, 0.16f, 0.16f),
                BalanceCheckStatus.Stalled => new Color(0.36f, 0.36f, 0.42f),
                BalanceCheckStatus.Stable => new Color(0.16f, 0.56f, 0.28f),
                _ => Color.gray,
            };
            color.a = alpha;
            return color;
        }
    }
}
#endif
