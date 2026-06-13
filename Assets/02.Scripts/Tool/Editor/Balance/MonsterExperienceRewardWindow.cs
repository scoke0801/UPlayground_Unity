#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// PLAYER_GROWTH_LEVELING_DESIGN 기반 몬스터 EXP 보상 일괄 발급 창.
    /// ActorDefinitionSO.level/grade와 기준 플레이어 레벨 차이를 사용해 expReward를 계산한다.
    /// </summary>
    public sealed class MonsterExperienceRewardWindow : EditorWindow
    {
        private const float RowHeight = 22f;

        private ActorDatabase _database;
        private LevelCurveSO _levelCurve;
        private int _playerLevel = 1;
        private float _sameLevelNormalRewardRatio = 0.18f;
        private float _levelGapStep = 0.12f;
        private float _minLevelGapMultiplier = 0.25f;
        private float _maxLevelGapMultiplier = 2.5f;
        private float _weakMultiplier = 0.6f;
        private float _normalMultiplier = 1f;
        private float _eliteMultiplier = 2.75f;
        private float _bossMultiplier = 10f;
        private long _minReward = 1;
        private bool _overwriteGrowthCurve;
        private string _filter = "";
        private Vector2 _scroll;
        private ActorDefinitionSO _cursorActor;
        private float _tableViewportHeight;
        private bool _ensureCursorVisible;

        private readonly Dictionary<ActorDefinitionSO, bool> _selected = new();
        private readonly List<ActorDefinitionSO> _allMonsters = new();
        private readonly List<ActorDefinitionSO> _visibleMonsters = new();

        [MenuItem("UPlayGround/게임플레이/밸런스/몬스터 경험치 발급기", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayBalance + 4)]
        public static void Open()
        {
            var window = GetWindow<MonsterExperienceRewardWindow>();
            window.titleContent = new GUIContent("Monster EXP Reward");
            window.minSize = new Vector2(980f, 580f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_database == null)
                _database = FindFirst<ActorDatabase>();
            if (_levelCurve == null)
                _levelCurve = FindFirst<LevelCurveSO>();
            ReloadMonsterCache();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is ActorDefinitionSO actor && IsMonster(actor))
            {
                _cursorActor = actor;
            }
            else if (Selection.activeObject is ActorDatabase database)
            {
                _database = database;
                ReloadMonsterCache();
            }
            else if (Selection.activeObject is LevelCurveSO curve)
            {
                _levelCurve = curve;
            }

            Repaint();
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
                {
                    ReloadMonsterCache();
                    Repaint();
                }

                if (GUILayout.Button("LevelCurve 생성/찾기", EditorStyles.toolbarButton, GUILayout.Width(136f)))
                    CreateOrFindLevelCurve();

                using (new EditorGUI.DisabledScope(_levelCurve == null))
                {
                    if (GUILayout.Button("Growth 곡선 연결", EditorStyles.toolbarButton, GUILayout.Width(118f)))
                        LinkGrowthCurves();
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(_allMonsters.Count == 0))
                {
                    if (GUILayout.Button("전체선택", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                        SetVisibleSelected(true);
                    if (GUILayout.Button("해제", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                        SetVisibleSelected(false);
                    if (GUILayout.Button("Apply Selected", EditorStyles.toolbarButton, GUILayout.Width(112f)))
                        ApplySelected();
                    if (GUILayout.Button("Apply All Monsters", EditorStyles.toolbarButton, GUILayout.Width(132f)))
                        ApplyAll();
                }
            }
        }

        private void DrawSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                _database = (ActorDatabase)EditorGUILayout.ObjectField("Actor Database", _database, typeof(ActorDatabase), false);
                if (EditorGUI.EndChangeCheck())
                    ReloadMonsterCache();

                _levelCurve = (LevelCurveSO)EditorGUILayout.ObjectField("Level Curve", _levelCurve, typeof(LevelCurveSO), false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _playerLevel = EditorGUILayout.IntField(
                        new GUIContent("기준 플레이어 레벨", "몬스터 레벨 - 플레이어 레벨 차이 계산의 기준."),
                        Mathf.Max(1, _playerLevel));
                    _sameLevelNormalRewardRatio = EditorGUILayout.FloatField(
                        new GUIContent("동레벨 Normal 보상비율", "동레벨 Normal 몬스터 1마리가 다음 레벨 요구 EXP의 몇 %를 주는지."),
                        Mathf.Max(0f, _sameLevelNormalRewardRatio));
                    _levelGapStep = EditorGUILayout.FloatField(
                        new GUIContent("레벨차 1당 보정", "몬스터 레벨이 플레이어보다 1 높거나 낮을 때 곱해지는 증감폭."),
                        Mathf.Max(0f, _levelGapStep));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _minLevelGapMultiplier = EditorGUILayout.FloatField("최소 레벨차 배율", Mathf.Max(0f, _minLevelGapMultiplier));
                    _maxLevelGapMultiplier = EditorGUILayout.FloatField("최대 레벨차 배율", Mathf.Max(_minLevelGapMultiplier, _maxLevelGapMultiplier));
                    _minReward = EditorGUILayout.LongField("최소 보상", System.Math.Max(0, _minReward));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _weakMultiplier = EditorGUILayout.FloatField("Weak", Mathf.Max(0f, _weakMultiplier));
                    _normalMultiplier = EditorGUILayout.FloatField("Normal", Mathf.Max(0f, _normalMultiplier));
                    _eliteMultiplier = EditorGUILayout.FloatField("Elite", Mathf.Max(0f, _eliteMultiplier));
                    _bossMultiplier = EditorGUILayout.FloatField("Boss", Mathf.Max(0f, _bossMultiplier));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("검색", GUILayout.Width(34f));
                    _filter = EditorGUILayout.TextField(_filter);
                    _overwriteGrowthCurve = EditorGUILayout.ToggleLeft(
                        new GUIContent("기존 PartyMemberGrowthSO.levelCurve도 덮어쓰기", "꺼두면 levelCurve가 비어 있는 성장 데이터만 연결한다."),
                        _overwriteGrowthCurve,
                        GUILayout.Width(260f));
                }

                long required = GetOptions().LevelCurve != null
                    ? GetOptions().LevelCurve.GetRequiredExp(_playerLevel)
                    : FallbackRequiredExp(_playerLevel);
                EditorGUILayout.LabelField(
                    $"공식: round(RequiredExp(Lv.{_playerLevel}) {required} x 보상비율 x 등급배율 x clamp(1 + (몬스터Lv-플레이어Lv) x 레벨차보정))",
                    EditorStyles.miniLabel);

                if (_levelCurve == null)
                    EditorGUILayout.HelpBox("LevelCurveSO가 없으면 PartyManager 폴백과 동일한 100 * level^1.5 기준으로 미리보기합니다. 버튼으로 기본 에셋을 생성해 Growth 데이터에 연결하세요.", MessageType.Info);
            }
        }

        private void DrawTable()
        {
            RebuildVisibleMonsters();
            DrawHeader();

            if (_visibleMonsters.Count == 0)
            {
                EditorGUILayout.HelpBox(_database == null
                    ? "ActorDatabase가 없으면 AssetDatabase 전체 검색으로 몬스터 ActorDefinitionSO를 찾습니다. 검색 결과가 없습니다."
                    : "표시할 몬스터가 없습니다.",
                    MessageType.Info);
                return;
            }

            if (_ensureCursorVisible)
                ApplyEnsureCursorVisible();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _visibleMonsters.Count; i++)
                DrawRow(_visibleMonsters[i], i);
            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.Repaint)
                _tableViewportHeight = GUILayoutUtility.GetLastRect().height;
        }

        private void DrawHeader()
        {
            Rect header = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(header, new Color(0.13f, 0.13f, 0.15f));
            float x = header.x + 4f;
            x += 22f;
            LabelStyled(ref x, header, 170f, "actorId");
            LabelStyled(ref x, header, 44f, "Lv");
            LabelStyled(ref x, header, 64f, "Grade");
            LabelStyled(ref x, header, 74f, "현재 EXP");
            LabelStyled(ref x, header, 82f, "예정 EXP");
            LabelStyled(ref x, header, 82f, "레벨차배율");
            LabelStyled(ref x, header, 72f, "등급배율");
            LabelStyled(ref x, header, 180f, "표시명");
        }

        private void DrawRow(ActorDefinitionSO actor, int index)
        {
            Rect row = GUILayoutUtility.GetRect(0f, RowHeight, GUILayout.ExpandWidth(true));
            MonsterExperienceRewardService.Preview preview =
                MonsterExperienceRewardService.Calculate(actor, GetOptions());
            bool changed = actor.expReward != preview.Reward;

            if (Event.current.type == EventType.Repaint)
            {
                if (actor == _cursorActor)
                    EditorGUI.DrawRect(row, new Color(0.16f, 0.32f, 0.48f, 0.85f));
                else if (changed)
                    EditorGUI.DrawRect(row, new Color(0.85f, 0.60f, 0.10f, 0.14f));
                else if (index % 2 == 1)
                    EditorGUI.DrawRect(row, new Color(0f, 0f, 0f, 0.10f));
            }

            float x = row.x + 4f;
            _selected.TryGetValue(actor, out bool selected);
            bool newSelected = GUI.Toggle(new Rect(x, row.y + 3f, 18f, 16f), selected, GUIContent.none);
            if (newSelected != selected)
                _selected[actor] = newSelected;
            x += 22f;

            Label(ref x, row, 170f, actor.actorId);
            Label(ref x, row, 44f, actor.level.ToString());
            Label(ref x, row, 64f, actor.grade.ToString());
            Label(ref x, row, 74f, actor.expReward.ToString());
            Label(ref x, row, 82f, preview.Reward.ToString());
            Label(ref x, row, 82f, preview.LevelGapMultiplier.ToString("F2"));
            Label(ref x, row, 72f, preview.GradeMultiplier.ToString("F2"));
            Label(ref x, row, 180f, string.IsNullOrWhiteSpace(actor.displayName) ? actor.name : actor.displayName);

            if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
            {
                SelectCursor(actor);
                Event.current.Use();
            }
        }

        private void ApplySelected()
        {
            var targets = new List<ActorDefinitionSO>();
            for (int i = 0; i < _visibleMonsters.Count; i++)
            {
                ActorDefinitionSO actor = _visibleMonsters[i];
                if (_selected.TryGetValue(actor, out bool selected) && selected)
                    targets.Add(actor);
            }

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("몬스터 경험치 발급기", "선택된 몬스터가 없습니다.", "확인");
                return;
            }

            if (!ConfirmApply(targets.Count, "선택 몬스터"))
                return;

            MonsterExperienceRewardService.ApplyResult result =
                MonsterExperienceRewardService.ApplyAll(targets, GetOptions());
            SaveAndReport(result);
        }

        private void ApplyAll()
        {
            List<ActorDefinitionSO> targets = MonsterExperienceRewardService.LoadMonsterDefinitions(_database);
            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("몬스터 경험치 발급기", "몬스터 ActorDefinitionSO를 찾지 못했습니다.", "확인");
                return;
            }

            if (!ConfirmApply(targets.Count, "전체 몬스터"))
                return;

            MonsterExperienceRewardService.ApplyResult result =
                MonsterExperienceRewardService.ApplyAll(targets, GetOptions());
            SaveAndReport(result);
        }

        private void LinkGrowthCurves()
        {
            if (_levelCurve == null)
                return;

            int changed = MonsterExperienceRewardService.LinkLevelCurveToGrowthAssets(_levelCurve, _overwriteGrowthCurve);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Growth 곡선 연결", $"PartyMemberGrowthSO 갱신: {changed}개", "확인");
        }

        private void CreateOrFindLevelCurve()
        {
            _levelCurve = MonsterExperienceRewardService.FindOrCreateLevelCurve();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(_levelCurve);
        }

        private void SaveAndReport(MonsterExperienceRewardService.ApplyResult result)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "몬스터 경험치 발급기",
                $"스캔: {result.Scanned}개\n변경: {result.Changed}개\n건너뜀: {result.Skipped}개",
                "확인");
        }

        private bool ConfirmApply(int count, string label)
            => EditorUtility.DisplayDialog(
                "경험치 보상 적용",
                $"{label} {count}개의 ActorDefinitionSO.expReward를 현재 공식으로 갱신합니다.\n" +
                "Undo로 되돌릴 수 있습니다.\n계속하시겠습니까?",
                "적용",
                "취소");

        private void RebuildVisibleMonsters()
        {
            _visibleMonsters.Clear();
            for (int i = 0; i < _allMonsters.Count; i++)
            {
                ActorDefinitionSO actor = _allMonsters[i];
                if (actor != null && PassesFilter(actor))
                    _visibleMonsters.Add(actor);
            }
        }

        private void ReloadMonsterCache()
        {
            _allMonsters.Clear();
            _allMonsters.AddRange(MonsterExperienceRewardService.LoadMonsterDefinitions(_database));
            _selected.Clear();

            if (_cursorActor != null && !_allMonsters.Contains(_cursorActor))
                _cursorActor = null;

            RebuildVisibleMonsters();
        }

        private void SetVisibleSelected(bool value)
        {
            RebuildVisibleMonsters();
            for (int i = 0; i < _visibleMonsters.Count; i++)
                _selected[_visibleMonsters[i]] = value;
        }

        private void HandleTableNavigation()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown)
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

        private void SelectCursor(ActorDefinitionSO actor)
        {
            _cursorActor = actor;
            EditorGUIUtility.PingObject(actor);
            Selection.activeObject = actor;
        }

        private void ApplyEnsureCursorVisible()
        {
            _ensureCursorVisible = false;
            int index = _visibleMonsters.IndexOf(_cursorActor);
            if (index < 0 || _tableViewportHeight <= 0f)
                return;

            float top = index * RowHeight;
            float bottom = top + RowHeight;
            if (top < _scroll.y)
                _scroll.y = top;
            else if (bottom > _scroll.y + _tableViewportHeight)
                _scroll.y = bottom - _tableViewportHeight;
        }

        private MonsterExperienceRewardService.Options GetOptions()
        {
            return new MonsterExperienceRewardService.Options
            {
                LevelCurve = _levelCurve,
                PlayerLevel = _playerLevel,
                SameLevelNormalRewardRatio = _sameLevelNormalRewardRatio,
                LevelGapStep = _levelGapStep,
                MinLevelGapMultiplier = _minLevelGapMultiplier,
                MaxLevelGapMultiplier = _maxLevelGapMultiplier,
                WeakMultiplier = _weakMultiplier,
                NormalMultiplier = _normalMultiplier,
                EliteMultiplier = _eliteMultiplier,
                BossMultiplier = _bossMultiplier,
                MinReward = _minReward,
            };
        }

        private bool PassesFilter(ActorDefinitionSO actor)
        {
            if (string.IsNullOrWhiteSpace(_filter))
                return true;
            return Contains(actor.actorId, _filter)
                   || Contains(actor.displayName, _filter)
                   || Contains(actor.name, _filter)
                   || actor.grade.ToString().IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMonster(ActorDefinitionSO actor)
            => actor != null && (actor.actorType & ActorType.Monster) != 0;

        private static bool Contains(string source, string filter)
            => !string.IsNullOrEmpty(source) && source.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;

        private static long FallbackRequiredExp(int level)
        {
            double required = 100.0 * System.Math.Pow(System.Math.Max(1, level), 1.5);
            return (long)System.Math.Max(1.0, System.Math.Round(required, System.MidpointRounding.AwayFromZero));
        }

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
            return guids != null && guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }
    }
}
#endif
