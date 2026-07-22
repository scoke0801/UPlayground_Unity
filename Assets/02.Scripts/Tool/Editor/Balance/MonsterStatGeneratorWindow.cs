#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// MonsterScalingSO 커브로 ActorDatabase의 몬스터 statData를 (레벨 × 등급 × 난이도) 일괄 산출하는 창.
    /// - Generate Missing (All): statData가 없는 몬스터만 신규 생성(기존 값 보호).
    /// - Apply Selected: 체크한 몬스터를 재생성. 기존 statData는 제자리 덮어쓰기(확인 다이얼로그 + Undo).
    ///   기존 로스터를 레벨/난이도에 맞춰 재조정할 때 사용. 보스 등은 체크 해제로 보호한다.
    /// 메뉴: UPlayGround/게임플레이/밸런스/몬스터 스탯 생성기
    /// </summary>
    public sealed class MonsterStatGeneratorWindow : EditorWindow
    {
        private const string StatSavePath = "Assets/10.Datas/Stat/Generated";

        private MonsterScalingSO _scaling;
        private ActorDatabase _database;
        private float _difficultyOverride;
        private Vector2 _scroll;
        private string _filter = "";
        private readonly Dictionary<ActorDefinitionSO, bool> _selected = new();

        // 방향키 네비게이션: 그려지는 행과 동일한 순서/필터의 몬스터 목록 + 행 커서.
        private readonly List<ActorDefinitionSO> _visibleMonsters = new();
        private ActorDefinitionSO _cursorActor;
        private float _tableViewportHeight;
        private bool _ensureCursorVisible;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/밸런스/몬스터 스탯 생성기", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayBalance + 2)]
        public static void Open()
        {
            var window = GetWindow<MonsterStatGeneratorWindow>();
            window.titleContent = new GUIContent("Monster Stat Generator");
            window.minSize = new Vector2(940f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_database == null)
                _database = FindFirst<ActorDatabase>();
            if (_scaling == null)
                _scaling = FindFirst<MonsterScalingSO>();
        }

        private void OnGUI()
        {
            HandleTableNavigation();
            DrawToolbar();
            DrawSettings();
            DrawTable();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    Repaint();

                using (new EditorGUI.DisabledScope(_scaling != null))
                {
                    if (GUILayout.Button("Create Scaling Asset", EditorStyles.toolbarButton, GUILayout.Width(150f)))
                        CreateScalingAsset();
                }

                using (new EditorGUI.DisabledScope(_scaling == null))
                {
                    if (GUILayout.Button("액션 기준 프리셋 적용", EditorStyles.toolbarButton, GUILayout.Width(140f)))
                        ApplyActionCombatScalingPreset();
                }

                GUILayout.FlexibleSpace();

                bool canGenerate = _scaling != null && _database != null;
                using (new EditorGUI.DisabledScope(!canGenerate))
                {
                    if (GUILayout.Button("전체선택", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                        SetAllSelected(true);
                    if (GUILayout.Button("해제", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                        SetAllSelected(false);
                    if (GUILayout.Button("Generate Missing (All)", EditorStyles.toolbarButton, GUILayout.Width(160f)))
                        GenerateMissing();
                    if (GUILayout.Button("Apply Selected (덮어쓰기)", EditorStyles.toolbarButton, GUILayout.Width(180f)))
                        ApplySelected();
                }
            }
        }

        private void DrawSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _scaling = (MonsterScalingSO)EditorGUILayout.ObjectField("Monster Scaling", _scaling, typeof(MonsterScalingSO), false);
                _database = (ActorDatabase)EditorGUILayout.ObjectField("Actor Database", _database, typeof(ActorDatabase), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _difficultyOverride = EditorGUILayout.FloatField(
                        new GUIContent("난이도 오버라이드", "0이면 SO의 difficultyMultiplier 사용. 0보다 크면 프리뷰/생성에 이 값을 적용."),
                        Mathf.Max(0f, _difficultyOverride));
                    GUILayout.Label("검색", GUILayout.Width(34f));
                    _filter = EditorGUILayout.TextField(_filter);
                }

                if (_scaling == null)
                    EditorGUILayout.HelpBox("MonsterScalingSO가 없습니다. 'Create Scaling Asset'으로 기본 커브를 생성하세요.", MessageType.Info);
                else
                    EditorGUILayout.LabelField(
                        $"등급 배율 {_scaling.gradeScalings.Count}개 / 성장 규칙 {_scaling.growthRules.Count}개 / 난이도 {_scaling.difficultyMultiplier:F2}",
                        EditorStyles.miniLabel);
            }
        }

        private void DrawTable()
        {
            if (_database == null)
            {
                EditorGUILayout.HelpBox("ActorDatabase를 지정하세요.", MessageType.Info);
                return;
            }

            DrawHeader();

            RebuildVisibleMonsters();
            if (_ensureCursorVisible)
                ApplyEnsureCursorVisible();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _visibleMonsters.Count; i++)
                DrawRow(_visibleMonsters[i], i);
            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint)
                _tableViewportHeight = GUILayoutUtility.GetLastRect().height;
        }

        // _database.All에서 몬스터 + 검색 필터를 통과한 행을 그려지는 순서 그대로 모은다.
        private void RebuildVisibleMonsters()
        {
            _visibleMonsters.Clear();
            if (_database == null)
                return;

            IReadOnlyList<ActorDefinitionSO> actors = _database.All;
            for (int i = 0; i < actors.Count; i++)
            {
                ActorDefinitionSO actor = actors[i];
                if (actor == null || (actor.actorType & ActorType.Monster) == 0)
                    continue;
                if (!PassesFilter(actor))
                    continue;

                _visibleMonsters.Add(actor);
            }
        }

        // 위/아래 방향키로 표시 목록의 이전/다음 몬스터로 커서를 이동한다(텍스트 편집 중에는 무시).
        private void HandleTableNavigation()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown || _database == null)
                return;
            if (e.keyCode != KeyCode.UpArrow && e.keyCode != KeyCode.DownArrow)
                return;
            if (EditorGUIUtility.editingTextField)
                return;

            RebuildVisibleMonsters();
            if (_visibleMonsters.Count == 0)
                return;

            int index = _visibleMonsters.IndexOf(_cursorActor);
            int next = index < 0
                ? 0
                : Mathf.Clamp(index + (e.keyCode == KeyCode.DownArrow ? 1 : -1), 0, _visibleMonsters.Count - 1);

            SelectCursor(_visibleMonsters[next]);
            _ensureCursorVisible = true;
            e.Use();
            Repaint();
        }

        // 클릭과 동일하게 커서 이동 시 인스펙터 선택 + 핑을 맞춘다.
        private void SelectCursor(ActorDefinitionSO actor)
        {
            _cursorActor = actor;
            EditorGUIUtility.PingObject(actor);
            Selection.activeObject = actor;
        }

        // 커서 행이 스크롤 영역 밖이면 보이도록 스크롤을 보정한다(행 높이 22 고정).
        private void ApplyEnsureCursorVisible()
        {
            _ensureCursorVisible = false;
            int index = _visibleMonsters.IndexOf(_cursorActor);
            if (index < 0 || _tableViewportHeight <= 0f)
                return;

            const float rowHeight = 22f;
            float top = index * rowHeight;
            float bottom = top + rowHeight;
            if (top < _scroll.y)
                _scroll.y = top;
            else if (bottom > _scroll.y + _tableViewportHeight)
                _scroll.y = bottom - _tableViewportHeight;
        }

        private void DrawRow(ActorDefinitionSO actor, int index)
        {
            Rect row = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            bool missing = actor.statData == null;

            if (Event.current.type == EventType.Repaint)
            {
                if (actor == _cursorActor)
                    EditorGUI.DrawRect(row, new Color(0.16f, 0.32f, 0.48f, 0.85f));
                else if (missing)
                    EditorGUI.DrawRect(row, new Color(0.85f, 0.60f, 0.10f, 0.16f));
                else if (index % 2 == 1)
                    EditorGUI.DrawRect(row, new Color(0f, 0f, 0f, 0.10f));
            }

            float x = row.x + 4f;

            // 모든 몬스터 선택 가능 — 체크한 액터는 기존 statData가 있어도 덮어쓴다(보스는 체크 해제로 보호).
            _selected.TryGetValue(actor, out bool sel);
            bool newSel = GUI.Toggle(new Rect(x, row.y + 3f, 18f, 16f), sel, GUIContent.none);
            if (newSel != sel)
                _selected[actor] = newSel;
            x += 22f;

            Label(ref x, row, 150f, actor.actorId);
            Label(ref x, row, 40f, $"Lv{actor.level}");
            Label(ref x, row, 60f, actor.grade.ToString());
            Label(ref x, row, 64f, missing ? "없음" : "있음");
            Label(ref x, row, 72f, actor.monsterScaling != null ? "연결" : "누락");
            Label(ref x, row, 58f, MonsterStatCalculator.GetHumanoidWeaponProfileName(actor));

            Dictionary<StatType, float> planned = MonsterStatCalculator.Calculate(ResolveScaling(actor), actor, _difficultyOverride);
            float pHp = planned[StatType.MaxHealth];
            float pAtk = planned[StatType.AttackPower];
            float pDef = planned[StatType.Defense];
            float pPoise = planned[StatType.MaxPoise];

            string current = missing
                ? "-"
                : $"{actor.statData.GetBase(StatType.MaxHealth):F0}/{actor.statData.GetBase(StatType.AttackPower):F2}/{actor.statData.GetBase(StatType.Defense):F2}";
            Label(ref x, row, 150f, $"현재 {current}");
            Label(ref x, row, 220f, $"예정 HP{pHp:F0} ATK{pAtk:F2} DEF{pDef:F2} Poise{pPoise:F0}");

            if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
            {
                SelectCursor(actor);
                Event.current.Use();
            }
        }

        private void DrawHeader()
        {
            Rect header = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(header, new Color(0.13f, 0.13f, 0.15f));
            float x = header.x + 4f;
            x += 22f; // checkbox column
            LabelStyled(ref x, header, 150f, "actorId");
            LabelStyled(ref x, header, 40f, "Lv");
            LabelStyled(ref x, header, 60f, "Grade");
            LabelStyled(ref x, header, 64f, "statData");
            LabelStyled(ref x, header, 72f, "Growth");
            LabelStyled(ref x, header, 58f, "유형");
            LabelStyled(ref x, header, 150f, "현재(HP/ATK/DEF)");
            LabelStyled(ref x, header, 220f, "예정(레벨×등급×난이도)");
        }

        /// <summary>statData가 없는 몬스터만 새로 생성한다(기존 값 보호).</summary>
        private void GenerateMissing()
        {
            if (_scaling == null || _database == null)
                return;

            int created = 0;
            int linked = 0;
            IReadOnlyList<ActorDefinitionSO> actors = _database.All;

            for (int i = 0; i < actors.Count; i++)
            {
                ActorDefinitionSO actor = actors[i];
                if (actor == null || (actor.actorType & ActorType.Monster) == 0)
                    continue;
                if (actor.statData != null) // 누락만 생성 — 기존 값 보호
                    continue;

                MonsterStatBakeService.Result result = MonsterStatBakeService.Bake(actor, new MonsterStatBakeService.Options
                {
                    PreferredScaling = _scaling,
                    StatSavePath = StatSavePath,
                    DifficultyOverride = _difficultyOverride,
                    CreateMissingStat = true,
                    ForceRegenerate = false,
                    LinkMissingScaling = true,
                    RecordUndo = true,
                    UndoLabel = "Generate Missing Monster Stat",
                });
                if (result.CreatedStat)
                    created++;
                if (result.LinkedScaling)
                    linked++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Monster Stat Generator", $"생성된 statData: {created}개\nGrowth 연결: {linked}개\n(기존 statData가 있는 몬스터는 건너뜀)", "확인");
        }

        /// <summary>체크한 몬스터를 재생성한다. 기존 statData가 있으면 제자리 덮어쓰기(Undo 가능).</summary>
        private void ApplySelected()
        {
            if (_scaling == null || _database == null)
                return;

            var targets = new List<ActorDefinitionSO>();
            int overwriteCount = 0;
            IReadOnlyList<ActorDefinitionSO> actors = _database.All;
            for (int i = 0; i < actors.Count; i++)
            {
                ActorDefinitionSO actor = actors[i];
                if (actor == null || (actor.actorType & ActorType.Monster) == 0)
                    continue;
                if (!(_selected.TryGetValue(actor, out bool sel) && sel))
                    continue;

                targets.Add(actor);
                if (actor.statData != null)
                    overwriteCount++;
            }

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("Monster Stat Generator", "선택된 몬스터가 없습니다.", "확인");
                return;
            }

            bool proceed = EditorUtility.DisplayDialog(
                "선택 몬스터 재생성",
                $"선택 {targets.Count}개를 커브로 재생성합니다.\n그중 {overwriteCount}개는 기존 statData를 덮어씁니다.\n공유 statData 에셋이면 같은 에셋을 참조하는 다른 몬스터도 함께 변경됩니다.\n(Undo로 되돌릴 수 있습니다)\n계속하시겠습니까?",
                "재생성",
                "취소");
            if (!proceed)
                return;

            int linked = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                ActorDefinitionSO actor = targets[i];
                MonsterStatBakeService.Result result = MonsterStatBakeService.Bake(actor, new MonsterStatBakeService.Options
                {
                    PreferredScaling = _scaling,
                    StatSavePath = StatSavePath,
                    DifficultyOverride = _difficultyOverride,
                    CreateMissingStat = true,
                    ForceRegenerate = true,
                    ReplaceExistingStatAsset = false,
                    LinkMissingScaling = true,
                    RecordUndo = true,
                    UndoLabel = "Re-level Monster Stat",
                });
                if (result.LinkedScaling)
                    linked++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Monster Stat Generator", $"재생성 완료: {targets.Count}개 (덮어쓰기 {overwriteCount}개)\nGrowth 연결: {linked}개", "확인");
        }

        private void SetAllSelected(bool value)
        {
            if (_database == null)
                return;

            IReadOnlyList<ActorDefinitionSO> actors = _database.All;
            for (int i = 0; i < actors.Count; i++)
            {
                ActorDefinitionSO actor = actors[i];
                if (actor == null || (actor.actorType & ActorType.Monster) == 0)
                    continue;
                if (!PassesFilter(actor)) // 필터에 보이는 행만 토글
                    continue;
                _selected[actor] = value;
            }
        }

        private void CreateScalingAsset()
        {
            var scaling = MonsterStatBakeService.FindOrCreateScaling(StatSavePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _scaling = scaling;
            EditorGUIUtility.PingObject(scaling);
        }

        private void ApplyActionCombatScalingPreset()
        {
            if (_scaling == null)
                return;

            bool proceed = EditorUtility.DisplayDialog(
                "Monster Scaling 프리셋 적용",
                "현재 MonsterScalingSO에 액션 전투 기준 프리셋을 적용합니다.\n" +
                "Weak/Normal/Elite/Boss HP와 성장률이 낮아지고, 생성기 미리보기/재생성 결과가 바뀝니다.\n" +
                "기존 몬스터 statData는 Apply Selected를 실행하기 전까지 변경되지 않습니다.\n" +
                "(Undo로 되돌릴 수 있습니다)\n계속하시겠습니까?",
                "적용",
                "취소");
            if (!proceed)
                return;

            Undo.RecordObject(_scaling, "Apply Action Combat Monster Scaling Preset");
            _scaling.ApplyActionCombatDefaults();
            EditorUtility.SetDirty(_scaling);
            AssetDatabase.SaveAssetIfDirty(_scaling);
            Repaint();
        }

        // ── helpers ────────────────────────────────────────────────
        private MonsterScalingSO ResolveScaling(ActorDefinitionSO actor)
            => actor != null && actor.monsterScaling != null ? actor.monsterScaling : _scaling;

        private bool PassesFilter(ActorDefinitionSO actor)
        {
            if (string.IsNullOrWhiteSpace(_filter))
                return true;
            return Contains(actor.actorId, _filter) || Contains(actor.displayName, _filter);
        }

        private static bool Contains(string s, string f)
            => !string.IsNullOrEmpty(s) && s.IndexOf(f, System.StringComparison.OrdinalIgnoreCase) >= 0;

        private static void Label(ref float x, Rect row, float width, string text)
        {
            GUI.Label(new Rect(x, row.y + 3f, width, 16f), text, EditorStyles.label);
            x += width + 4f;
        }

        private static void LabelStyled(ref float x, Rect row, float width, string text)
        {
            GUI.Label(new Rect(x, row.y + 3f, width, 16f), text, EditorStyles.boldLabel);
            x += width + 4f;
        }

        private static T FindFirst<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }
    }
}
#endif
