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
    /// 메뉴: UPlayGround/Gameplay/Balance/Monster Stat Generator
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

        [MenuItem("UPlayGround/Gameplay/Balance/Monster Stat Generator", priority = 22)]
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
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            IReadOnlyList<ActorDefinitionSO> actors = _database.All;
            for (int i = 0; i < actors.Count; i++)
            {
                ActorDefinitionSO actor = actors[i];
                if (actor == null || (actor.actorType & ActorType.Monster) == 0)
                    continue;
                if (!PassesFilter(actor))
                    continue;

                DrawRow(actor, i);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(ActorDefinitionSO actor, int index)
        {
            Rect row = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            bool missing = actor.statData == null;

            if (Event.current.type == EventType.Repaint)
            {
                if (missing)
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

            Dictionary<StatType, float> planned = MonsterStatCalculator.Calculate(_scaling, actor.grade, actor.level, _difficultyOverride);
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
                EditorGUIUtility.PingObject(actor);
                Selection.activeObject = actor;
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
            LabelStyled(ref x, header, 150f, "현재(HP/ATK/DEF)");
            LabelStyled(ref x, header, 220f, "예정(레벨×등급×난이도)");
        }

        /// <summary>statData가 없는 몬스터만 새로 생성한다(기존 값 보호).</summary>
        private void GenerateMissing()
        {
            if (_scaling == null || _database == null)
                return;

            EnsureFolder(StatSavePath);
            int created = 0;
            IReadOnlyList<ActorDefinitionSO> actors = _database.All;

            for (int i = 0; i < actors.Count; i++)
            {
                ActorDefinitionSO actor = actors[i];
                if (actor == null || (actor.actorType & ActorType.Monster) == 0)
                    continue;
                if (actor.statData != null) // 누락만 생성 — 기존 값 보호
                    continue;

                CreateAndAssignStat(actor);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Monster Stat Generator", $"생성된 statData: {created}개\n(기존 statData가 있는 몬스터는 건너뜀)", "확인");
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

            EnsureFolder(StatSavePath);
            for (int i = 0; i < targets.Count; i++)
            {
                ActorDefinitionSO actor = targets[i];
                if (actor.statData != null)
                {
                    // 기존 에셋 제자리 덮어쓰기 — 다른 곳에서 참조 중인 statData 링크를 유지한다.
                    Undo.RecordObject(actor.statData, "Re-level Monster Stat");
                    WriteStatValues(actor.statData, actor);
                    EditorUtility.SetDirty(actor.statData);
                }
                else
                {
                    CreateAndAssignStat(actor);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Monster Stat Generator", $"재생성 완료: {targets.Count}개 (덮어쓰기 {overwriteCount}개)", "확인");
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

        private void CreateAndAssignStat(ActorDefinitionSO actor)
        {
            var stat = ScriptableObject.CreateInstance<ActorStatSO>();
            WriteStatValues(stat, actor);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{StatSavePath}/ActorStat_{SafeName(actor)}.asset");
            AssetDatabase.CreateAsset(stat, path);

            Undo.RecordObject(actor, "Generate Monster Stat");
            var so = new SerializedObject(actor);
            so.FindProperty("statData").objectReferenceValue = stat;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(actor);
        }

        private void WriteStatValues(ActorStatSO stat, ActorDefinitionSO actor)
        {
            Dictionary<StatType, float> values = MonsterStatCalculator.Calculate(_scaling, actor.grade, actor.level, _difficultyOverride);
            foreach (KeyValuePair<StatType, float> pair in values)
                stat.EditorSet(pair.Key, pair.Value);
        }

        private void CreateScalingAsset()
        {
            EnsureFolder(StatSavePath);
            var scaling = ScriptableObject.CreateInstance<MonsterScalingSO>();
            scaling.FillDefaults();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{StatSavePath}/MonsterScaling_Default.asset");
            AssetDatabase.CreateAsset(scaling, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _scaling = scaling;
            EditorGUIUtility.PingObject(scaling);
        }

        // ── helpers ────────────────────────────────────────────────
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

        private static string SafeName(ActorDefinitionSO actor)
        {
            string raw = !string.IsNullOrWhiteSpace(actor.actorId) ? actor.actorId : actor.name;
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                raw = raw.Replace(invalid, '_');
            return raw.Replace('/', '_').Replace('\\', '_').Replace(' ', '_');
        }

        private static T FindFirst<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
