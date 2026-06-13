#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.AI.BehaviorTree.Editor;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
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

        // 방향키 네비게이션: 그려지는 행과 동일한 순서/필터의 액터 목록을 공유한다.
        private readonly List<ActorDefinitionSO> _visibleActors = new();
        private float _actorListViewportHeight;
        private bool _ensureSelectedVisible;

        private float _targetDuration = 30f;
        private float _assumedDistance = 2.5f;
        private int _monsterLevel = 1;
        private float _playerAttackPower = 1f;
        private float _manualPlayerDps = 18f;
        private float _playerAttackInterval = 1.2f;
        private float _minAttackOpportunities = 1f;

        [MenuItem("UPlayGround/게임플레이/밸런스/밸런스 디자이너", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayBalance)]
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
            HandleListNavigation();
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

                if (GUILayout.Button("텔레메트리 로드", EditorStyles.toolbarButton, GUILayout.Width(104f)))
                {
                    CombatTelemetryImporter.Reload();
                    Repaint();
                }

                if (GUILayout.Button("Scenario ← Player", EditorStyles.toolbarButton, GUILayout.Width(124f)))
                    GenerateScenarioForActivePlayer();

                if (GUILayout.Button("Scenario ← Party", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                    GenerateScenarioForAllPlayers();

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

                RebuildVisibleActors();
                if (_ensureSelectedVisible)
                    ApplyEnsureSelectedVisible();

                _actorListScroll = EditorGUILayout.BeginScrollView(_actorListScroll);
                for (int i = 0; i < _visibleActors.Count; i++)
                    DrawActorListItem(_visibleActors[i]);
                EditorGUILayout.EndScrollView();

                if (Event.current.type == EventType.Repaint)
                    _actorListViewportHeight = GUILayoutUtility.GetLastRect().height;
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
                SelectActor(actor);
                Event.current.Use();
            }
        }

        private void SelectActor(ActorDefinitionSO actor)
        {
            _selectedActor = actor;
            _selectedResult = FindResult(actor);
            Selection.activeObject = actor;
        }

        // _database.All을 필터링해 화면에 표시되는 액터를 그려지는 순서 그대로 모은다.
        private void RebuildVisibleActors()
        {
            _visibleActors.Clear();
            if (_database == null)
                return;

            IReadOnlyList<ActorDefinitionSO> actors = _database.All;
            for (int i = 0; i < actors.Count; i++)
            {
                if (ShouldShowActor(actors[i]))
                    _visibleActors.Add(actors[i]);
            }
        }

        // 위/아래 방향키로 표시 목록의 이전/다음 액터를 선택한다(텍스트 편집 중에는 무시).
        private void HandleListNavigation()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown || _database == null)
                return;
            if (e.keyCode != KeyCode.UpArrow && e.keyCode != KeyCode.DownArrow)
                return;
            if (EditorGUIUtility.editingTextField)
                return;

            RebuildVisibleActors();
            if (_visibleActors.Count == 0)
                return;

            int index = _visibleActors.IndexOf(_selectedActor);
            int next = index < 0
                ? 0
                : Mathf.Clamp(index + (e.keyCode == KeyCode.DownArrow ? 1 : -1), 0, _visibleActors.Count - 1);

            SelectActor(_visibleActors[next]);
            _ensureSelectedVisible = true;
            e.Use();
            Repaint();
        }

        // 선택 항목이 스크롤 영역 밖이면 보이도록 스크롤을 보정한다(행 높이 42 고정).
        private void ApplyEnsureSelectedVisible()
        {
            _ensureSelectedVisible = false;
            int index = _visibleActors.IndexOf(_selectedActor);
            if (index < 0 || _actorListViewportHeight <= 0f)
                return;

            const float itemHeight = 42f;
            float top = index * itemHeight;
            float bottom = top + itemHeight;
            if (top < _actorListScroll.y)
                _actorListScroll.y = top;
            else if (bottom > _actorListScroll.y + _actorListViewportHeight)
                _actorListScroll.y = bottom - _actorListViewportHeight;
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
            DrawResultCells(header, "Actor", "Score", "Status", "플레이어 생존", "몬스터 처치", "플레이어 DPS", "적 DPS", "Strong%", "권장 액션", true);

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

            string monsterTtd = BalanceCombatEstimator.FormatTime(result.MonsterTimeToDeath);
            if (result.Actor != null
                && CombatTelemetryImporter.TryGet(result.Actor.actorId, out CombatTelemetryImporter.ActorTelemetry telemetry)
                && telemetry.KillCount > 0)
            {
                monsterTtd += $" (실측 {telemetry.MedianKillTime:F1}s)";
            }

            DrawResultCells(
                rect,
                result.Actor != null ? result.Actor.actorId : "(null)",
                result.BalanceScore.ToString("F0"),
                result.Status.ToString(),
                BalanceCombatEstimator.FormatTime(result.PlayerTimeToDeath),
                monsterTtd,
                result.PlayerExpectedDps.ToString("F1"),
                result.EnemyExpectedDps.ToString("F1"),
                $"{result.StrongAttackChance * 100f:F0}%",
                result.RecommendedAction,
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
            string score,
            string status,
            string playerTtd,
            string monsterTtd,
            string playerDps,
            string enemyDps,
            string strongChance,
            string recommendedAction,
            bool header)
        {
            GUIStyle style = header ? EditorStyles.boldLabel : EditorStyles.label;
            float x = rect.x + 6f;
            Label(new Rect(x, rect.y + 3f, 160f, 18f), actor, style); x += 164f;
            Label(new Rect(x, rect.y + 3f, 48f, 18f), score, style); x += 52f;
            Label(new Rect(x, rect.y + 3f, 82f, 18f), status, style); x += 86f;
            Label(new Rect(x, rect.y + 3f, 92f, 18f), playerTtd, style); x += 96f;
            Label(new Rect(x, rect.y + 3f, 92f, 18f), monsterTtd, style); x += 96f;
            Label(new Rect(x, rect.y + 3f, 82f, 18f), playerDps, style); x += 86f;
            Label(new Rect(x, rect.y + 3f, 64f, 18f), enemyDps, style); x += 68f;
            Label(new Rect(x, rect.y + 3f, 64f, 18f), strongChance, style); x += 68f;
            Label(new Rect(x, rect.y + 3f, rect.xMax - x - 6f, 18f), recommendedAction, style);
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
                BalanceTelemetrySection.Draw(_selectedResult, _scenario);
                BalanceSimulationSection.Draw(_selectedResult, _scenario, CreateFallbackInput());
                DrawMessages(_selectedResult);
                DrawSkillBreakdown(_selectedResult);
                DrawTargetRecommendation(_selectedResult);
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
                $"품질 점수 {result.BalanceScore:F0}/100 / 생존 목표비 {result.PlayerSurvivalRatio:F2}x / 처치 목표비 {result.MonsterKillRatio:F2}x / 권장 액션: {result.RecommendedAction}",
                EditorStyles.miniLabel);
            if (result.MonsterBreakGauge > 0f)
            {
                string breakTime = result.PlayerExpectedBreakDps > 0f
                    ? $"{result.EstimatedTimeToBreak:F1}s"
                    : "-";
                EditorGUILayout.LabelField(
                    $"브레이크 게이지 {result.MonsterBreakGauge:F0} / 플레이어 Break {result.PlayerExpectedBreakDps:F1}/s / 예상 브레이크 {breakTime} / 노출 가동률 {result.BreakExposedUptime * 100f:F0}%",
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    $"브레이크 포함 실효 DPS {result.PlayerEffectiveDpsWithBreak:F1} / 브레이크 포함 처치 예상 {BalanceCombatEstimator.FormatTime(result.MonsterTimeToDeathWithBreak)}",
                    EditorStyles.miniLabel);
            }
            EditorGUILayout.LabelField(
                $"공격 확률 Basic {result.BasicAttackChance * 100f:F0}% / Heavy {result.HeavyAttackChance * 100f:F0}% / Skill {result.SkillAttackChance * 100f:F0}% / 강한 공격 {result.StrongAttackChance * 100f:F0}%",
                EditorStyles.miniLabel);
            string netPoise = result.NetPoisePressure > 0f
                ? $"순 압박 +{result.NetPoisePressure:F1}/s (회복 초과 → 지속 경직 위험)"
                : $"순 압박 {result.NetPoisePressure:F1}/s (회복으로 상쇄)";
            EditorGUILayout.LabelField(
                $"경직 압박 {result.EnemyPoiseDps:F1} poise/s vs 회복 {result.PlayerPoiseRecoveryRate:F0}/s → {netPoise}",
                EditorStyles.miniLabel);
            string topShare = string.IsNullOrEmpty(result.TopAttackName)
                ? "-"
                : $"{result.TopAttackName} {result.TopAttackDpsShare * 100f:F0}%";
            EditorGUILayout.LabelField(
                $"해금 공격 {result.UnlockedSkillCount} / 잠김 {result.LockedSkillCount} / 사용 가능 {result.AvailableSkillCount:F0} | 최대 기여 공격 {topShare}",
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

            EditorGUILayout.LabelField("DPS 기여도 내림차순 정렬. DPS% 35% 초과는 빨강, 강공격 Danger Ring 누락은 주황으로 표시.", EditorStyles.centeredGreyMiniLabel);

            Rect header = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(header, new Color(0.13f, 0.13f, 0.15f));
            DrawSkillCells(header, "AnimKey", "Category", "Chance", "Damage", "Poise", "CD", "DPS", "DPS%", "DR/Tele", true);

            for (int i = 0; i < result.SkillBreakdowns.Count; i++)
            {
                BalanceSkillBreakdown skill = result.SkillBreakdowns[i];
                Rect row = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
                if (skill.DpsShare > 0.35f)
                    EditorGUI.DrawRect(row, new Color(0.82f, 0.16f, 0.16f, 0.22f));
                else if (skill.IsStrong && !skill.UseDangerRing)
                    EditorGUI.DrawRect(row, new Color(0.86f, 0.58f, 0.18f, 0.22f));
                else if (i % 2 == 1)
                    EditorGUI.DrawRect(row, new Color(0f, 0f, 0f, 0.12f));

                string ringLabel = skill.UseDangerRing
                    ? (skill.DangerRingDuration > 0f ? $"DR {skill.DangerRingDuration:F1}s" : "DR auto")
                    : "-";
                string flags = $"{ringLabel}{(skill.UseTelegraph ? " +T" : "")}";

                DrawSkillCells(
                    row,
                    skill.Name,
                    skill.Category,
                    $"{skill.SelectionChance * 100f:F0}%",
                    skill.Damage.ToString("F1"),
                    skill.PoiseDamage.ToString("F0"),
                    skill.Cooldown.ToString("F1"),
                    skill.DpsContribution.ToString("F1"),
                    $"{skill.DpsShare * 100f:F0}%",
                    flags,
                    false);
            }
        }

        private void DrawSkillCells(Rect rect, string name, string category, string chance, string damage, string poise, string cooldown, string dps, string share, string flags, bool header)
        {
            GUIStyle style = header ? EditorStyles.boldLabel : EditorStyles.label;
            float x = rect.x + 6f;
            Label(new Rect(x, rect.y + 3f, 140f, 16f), name, style); x += 144f;
            Label(new Rect(x, rect.y + 3f, 72f, 16f), category, style); x += 76f;
            Label(new Rect(x, rect.y + 3f, 58f, 16f), chance, style); x += 62f;
            Label(new Rect(x, rect.y + 3f, 64f, 16f), damage, style); x += 68f;
            Label(new Rect(x, rect.y + 3f, 50f, 16f), poise, style); x += 54f;
            Label(new Rect(x, rect.y + 3f, 44f, 16f), cooldown, style); x += 48f;
            Label(new Rect(x, rect.y + 3f, 54f, 16f), dps, style); x += 58f;
            Label(new Rect(x, rect.y + 3f, 50f, 16f), share, style); x += 54f;
            Label(new Rect(x, rect.y + 3f, 90f, 16f), flags, style);
        }

        private void DrawTargetRecommendation(BalanceScenarioResult result)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("권장 보정 — 목표 전투시간 역산 (Phase 2)", EditorStyles.boldLabel);

            if (result.Actor == null || result.Status == BalanceCheckStatus.InvalidData)
            {
                EditorGUILayout.LabelField("유효한 분석 결과가 필요합니다.", EditorStyles.miniLabel);
                return;
            }

            BalanceTargetSolver.Recommendation rec = BalanceTargetSolver.Solve(result);

            EditorGUILayout.LabelField(
                $"목표 시간 {rec.TargetKillTime:F0}s 기준 (양측 모두 이 시간만큼 버티도록 역산)",
                EditorStyles.miniLabel);

            // HP 권장
            using (new EditorGUILayout.HorizontalScope())
            {
                string hpText = rec.CanSolveHealth
                    ? $"HP  현재 {rec.CurrentHealth:F0}  →  권장 {rec.RecommendedHealth:F0}  (플레이어 DPS {result.PlayerExpectedDps:F1} × {rec.TargetKillTime:F0}s)"
                    : "HP  플레이어 DPS가 0이라 역산 불가";
                EditorGUILayout.LabelField(hpText, EditorStyles.miniLabel);

                using (new EditorGUI.DisabledScope(!rec.CanSolveHealth || result.Actor.statData == null))
                {
                    if (GUILayout.Button("Apply HP", GUILayout.Width(90f)) &&
                        ConfirmApply($"몬스터 HP를 {rec.CurrentHealth:F0} → {rec.RecommendedHealth:F0}으로 변경합니다.\n(Undo 가능)"))
                    {
                        if (BalanceTargetSolver.ApplyHealth(result.Actor, rec.RecommendedHealth))
                            AnalyzeSelected();
                    }
                }
            }

            // 피해 배율 권장
            using (new EditorGUILayout.HorizontalScope())
            {
                string dmgText = rec.CanSolveDamage
                    ? $"피해 배율 ×{rec.RecommendedDamageScale:F2}  (목표 적 DPS {result.PlayerHealth / rec.TargetSurvivalTime:F1} / 현재 {rec.CurrentEnemyDps:F1})"
                    : "피해 배율  적 DPS 또는 플레이어 HP가 0이라 역산 불가";
                EditorGUILayout.LabelField(dmgText, EditorStyles.miniLabel);

                bool meaningfulScale = rec.CanSolveDamage && !Mathf.Approximately(rec.RecommendedDamageScale, 1f) && rec.RecommendedDamageScale > 0f;
                using (new EditorGUI.DisabledScope(!meaningfulScale || result.Actor.attackData == null))
                {
                    if (GUILayout.Button("Apply Damage", GUILayout.Width(110f)) &&
                        ConfirmApply($"모든 공격 HitPhase 피해에 ×{rec.RecommendedDamageScale:F2}를 곱합니다.\n(Undo 가능)"))
                    {
                        if (BalanceTargetSolver.ApplyDamageScale(result.Actor, rec.RecommendedDamageScale))
                            AnalyzeSelected();
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "HP 권장은 처치 시간을, 피해 배율은 플레이어 생존 시간을 목표 시간에 맞춥니다. 적용 전 값과 비교 후 수동 확정하세요.",
                MessageType.Info);
        }

        private static bool ConfirmApply(string message)
            => EditorUtility.DisplayDialog(
                "권장 보정 적용",
                $"{message}\n공유 데이터 에셋이면 같은 에셋을 참조하는 다른 몬스터도 함께 변경됩니다.",
                "적용",
                "취소");

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

            BalanceDataAutoGenerator.GenerationSummary summary = BalanceDataAutoGenerator.GenerateMissing(_selectedActor, _scenario, CreateFallbackInput());
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

                BalanceDataAutoGenerator.GenerationSummary summary = BalanceDataAutoGenerator.GenerateMissing(actor, _scenario, CreateFallbackInput());
                createdCount += summary.CreatedCount;
            }

            AnalyzeDatabase();
            EditorUtility.DisplayDialog("누락 데이터 일괄 생성", $"생성된 에셋: {createdCount}개", "확인");
        }

        private void GenerateScenarioForActivePlayer()
        {
            PartyConfigSO config = BalanceScenarioGenerator.FindPartyConfig();
            if (config == null && !EditorUtility.DisplayDialog(
                    "시나리오 생성",
                    "PartyConfigSO를 찾지 못했습니다. 기본 캐릭터(Bokusei)·레벨 1 가정으로 생성할까요?",
                    "생성",
                    "취소"))
            {
                return;
            }

            BalanceScenarioGenerator.ScenarioGenResult result =
                BalanceScenarioGenerator.GenerateForActiveCharacter(config);

            // 생성/갱신된 시나리오를 현재 분석 대상으로 자동 연결한다.
            _scenario = result.Asset;
            EditorGUIUtility.PingObject(result.Asset);
            if (_database != null)
                AnalyzeDatabase();

            string verb = result.Created ? "생성" : "갱신";
            EditorUtility.DisplayDialog(
                "시나리오 생성",
                $"현재 조작 캐릭터 시나리오 {verb} 완료\n\n" +
                $"캐릭터: {result.Character} (Lv.{result.Level})\n" +
                $"{result.Note}\n{result.Path}",
                "확인");
        }

        private void GenerateScenarioForAllPlayers()
        {
            PartyConfigSO config = BalanceScenarioGenerator.FindPartyConfig();
            if (config == null)
            {
                EditorUtility.DisplayDialog("시나리오 일괄 생성", "PartyConfigSO를 찾지 못했습니다.", "확인");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "시나리오 일괄 생성",
                    "PartyConfig.growthData의 모든 파티 캐릭터에 대해 시나리오를 생성/갱신합니다.\n" +
                    "(인카운터/방어 가정값은 보존하고 플레이어 데이터만 새로고침)\n계속하시겠습니까?",
                    "생성",
                    "취소"))
            {
                return;
            }

            List<BalanceScenarioGenerator.ScenarioGenResult> results =
                BalanceScenarioGenerator.GenerateForAllPartyMembers(config);

            var builder = new StringBuilder();
            int created = 0;
            int updated = 0;
            for (int i = 0; i < results.Count; i++)
            {
                BalanceScenarioGenerator.ScenarioGenResult r = results[i];
                if (r.Created) created++; else updated++;
                builder.AppendLine($"· {r.Character} (Lv.{r.Level}) {(r.Created ? "신규" : "갱신")} — {r.Note}");
            }

            CharacterActorType active = BalanceScenarioGenerator.ResolveActiveCharacter(config);
            BalanceScenarioGenerator.ScenarioGenResult activeResult =
                results.Find(r => r.Character == active);
            if (activeResult != null)
                _scenario = activeResult.Asset;
            if (_database != null)
                AnalyzeDatabase();

            EditorUtility.DisplayDialog(
                "시나리오 일괄 생성",
                $"총 {results.Count}개 (신규 {created} / 갱신 {updated})\n\n{builder}",
                "확인");
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
            builder.AppendLine("actorId,displayName,level,grade,status,balanceScore,playerSurvivalRatio,monsterKillRatio,recommendedAction,targetDuration,playerSurvivalSeconds,monsterKillSeconds,monsterKillSecondsWithBreak,monsterHp,playerAttackPower,playerDps,playerEffectiveDpsWithBreak,playerBreakDps,monsterBreakGauge,estimatedTimeToBreak,breaksPerFight,breakExposedUptime,enemyDps,enemyPoiseDps,playerPoiseRecovery,netPoisePressure,basicChance,heavyChance,skillChance,strongChance,topAttack,topAttackShare,dangerRingMissing,attackOpportunities,unlockedSkills,lockedSkills,availableSkills,summary");
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
                builder.Append(r.BalanceScore.ToString("F0"));
                builder.Append(',');
                builder.Append(r.PlayerSurvivalRatio.ToString("F4"));
                builder.Append(',');
                builder.Append(r.MonsterKillRatio.ToString("F4"));
                builder.Append(',');
                builder.Append(Escape(r.RecommendedAction));
                builder.Append(',');
                builder.Append(r.TargetDuration.ToString("F2"));
                builder.Append(',');
                builder.Append(r.PlayerTimeToDeath.ToString("F2"));
                builder.Append(',');
                builder.Append(r.MonsterTimeToDeath.ToString("F2"));
                builder.Append(',');
                builder.Append(r.MonsterTimeToDeathWithBreak.ToString("F2"));
                builder.Append(',');
                builder.Append(r.MonsterHealth.ToString("F2"));
                builder.Append(',');
                builder.Append(r.PlayerAttackPower.ToString("F2"));
                builder.Append(',');
                builder.Append(r.PlayerExpectedDps.ToString("F2"));
                builder.Append(',');
                builder.Append(r.PlayerEffectiveDpsWithBreak.ToString("F2"));
                builder.Append(',');
                builder.Append(r.PlayerExpectedBreakDps.ToString("F2"));
                builder.Append(',');
                builder.Append(r.MonsterBreakGauge.ToString("F2"));
                builder.Append(',');
                builder.Append(r.EstimatedTimeToBreak.ToString("F2"));
                builder.Append(',');
                builder.Append(r.EstimatedBreaksPerFight.ToString("F2"));
                builder.Append(',');
                builder.Append(r.BreakExposedUptime.ToString("F4"));
                builder.Append(',');
                builder.Append(r.EnemyExpectedDps.ToString("F2"));
                builder.Append(',');
                builder.Append(r.EnemyPoiseDps.ToString("F2"));
                builder.Append(',');
                builder.Append(r.PlayerPoiseRecoveryRate.ToString("F2"));
                builder.Append(',');
                builder.Append(r.NetPoisePressure.ToString("F2"));
                builder.Append(',');
                builder.Append(r.BasicAttackChance.ToString("F4"));
                builder.Append(',');
                builder.Append(r.HeavyAttackChance.ToString("F4"));
                builder.Append(',');
                builder.Append(r.SkillAttackChance.ToString("F4"));
                builder.Append(',');
                builder.Append(r.StrongAttackChance.ToString("F4"));
                builder.Append(',');
                builder.Append(Escape(r.TopAttackName));
                builder.Append(',');
                builder.Append(r.TopAttackDpsShare.ToString("F4"));
                builder.Append(',');
                builder.Append(CountDangerRingMissing(r));
                builder.Append(',');
                builder.Append(r.EnemyAttackOpportunities.ToString("F2"));
                builder.Append(',');
                builder.Append(r.UnlockedSkillCount);
                builder.Append(',');
                builder.Append(r.LockedSkillCount);
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

        private static int CountDangerRingMissing(BalanceScenarioResult result)
        {
            int count = 0;
            for (int i = 0; i < result.SkillBreakdowns.Count; i++)
            {
                BalanceSkillBreakdown skill = result.SkillBreakdowns[i];
                if (skill.IsStrong && !skill.UseDangerRing)
                    count++;
            }
            return count;
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
