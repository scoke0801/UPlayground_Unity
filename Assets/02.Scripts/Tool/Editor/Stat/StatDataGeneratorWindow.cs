#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;
using UPlayGround.Tool.Editor.Combat;

namespace UPlayGround.Tool.Editor.Stat
{
    /// <summary>
    /// ActorStatSO 자동 생성/마이그레이션 툴.
    /// 메뉴: UPlayGround/Stat/Stat Data Generator
    /// </summary>
    public class StatDataGeneratorWindow : EditorWindow
    {
        // ── 탭 ──────────────────────────────────────────────────────
        private enum Tab { Migration, PlayerCharacter, Template, Regenerate, CombatPolicy }
        private Tab _currentTab = Tab.Migration;

        // ── 마이그레이션 상태 ─────────────────────────────────────
        private readonly List<MigrationRow> _migrationRows = new();
        private bool _onlyMissing = true;
        private string _savePath = DefaultSavePath;
        private Vector2 _migrationScroll;
        private bool _allSelected;
        // 재마이그레이션으로 교체된 기존 statData 에셋을 (다른 정의가 참조하지 않으면) 삭제할지 여부
        private bool _deleteReplacedStats = true;

        // ── 템플릿 상태 ───────────────────────────────────────────
        private TemplateKind _templateKind = TemplateKind.NormalMonster;
        private string _templateName = "StatTemplate_Normal";
        private string _templateSavePath = TemplateSavePath;
        private StatTemplateSO _templateToEdit;

        // ── 재생성 상태 ───────────────────────────────────────────
        private StatTemplateSO _regenTemplate;
        private bool _regenOverwrite = true;
        private bool _regenFillDefaults = true;
        private bool _regenCreateMissing = true;
        private string _regenFilter = string.Empty;
        private readonly List<RegenRow> _regenRows = new();
        private Vector2 _regenScroll;
        private bool _regenAllSelected;

        // ── 전투 정책 상태 ───────────────────────────────────────
        private readonly List<PolicyRow> _policyRows = new();
        private Vector2 _policyScroll;
        private bool _policyOnlyRelevant = true;
        private bool _policyAllSelected;

        // ── 플레이어 캐릭터 상태 ──────────────────────────────────
        private readonly Dictionary<CharacterActorType, bool> _playerSelected = new();
        private readonly Dictionary<CharacterActorType, ActorStatSO> _playerExisting = new();
        private string _playerSavePath = PlayerSavePath;
        private string _playerNamePrefix = "ActorStat_Player_";
        private bool _playerOverwrite = false;
        private Vector2 _playerScroll;

        // ── 색상 ──────────────────────────────────────────────────
        private static readonly Color ColorHeader   = new(0.15f, 0.15f, 0.20f);
        private static readonly Color ColorRowEven  = new(0.20f, 0.20f, 0.22f);
        private static readonly Color ColorRowOdd   = new(0.23f, 0.23f, 0.25f);
        private static readonly Color ColorMissing  = new(0.85f, 0.60f, 0.10f);
        private static readonly Color ColorOk       = new(0.30f, 0.75f, 0.40f);

        // ── 상수 ──────────────────────────────────────────────────
        private const string DefaultSavePath = "Assets/10.Datas/Stat/Generated";
        private const string PlayerSavePath  = "Assets/10.Datas/Stat/Player";
        private const string BreakGaugeSavePath = "Assets/10.Datas/Actor/Enemy/BreakGauge/Generated";
        private const string MonsterScalingSavePath = "Assets/10.Datas/Stat/Generated";
        private const string TemplateSavePath = "Assets/10.Datas/Stat/Template";
        private const float ColCheck = 22f;
        private const float ColName  = 200f;
        private const float ColPoise = 90f;
        private const float ColBreak = 110f;
        private const float ColCurrent = 130f;
        private const float ColPlanned = 200f;
        private const float ColGrade = 70f;
        private const float ColPolicy = 210f;
        private const float RowH = 22f;

        // ── 메뉴 ──────────────────────────────────────────────────
        public static void Open()
        {
            var window = GetWindow<StatDataGeneratorWindow>();
            window.titleContent = new GUIContent("Stat Generator", EditorGUIUtility.IconContent("d_PreMatCube").image);
            window.minSize = new Vector2(900f, 460f);
            window.Show();
        }

        // ── 라이프사이클 ──────────────────────────────────────────
        private void OnEnable()
        {
            RefreshMigrationRows();
            RefreshPlayerExisting();
            RefreshRegenRows();
            RefreshPolicyRows();
        }

        private void OnGUI()
        {
            DrawTabs();
            switch (_currentTab)
            {
                case Tab.Migration:       DrawMigrationTab();       break;
                case Tab.PlayerCharacter: DrawPlayerCharacterTab(); break;
                case Tab.Template:        DrawTemplateTab();        break;
                case Tab.Regenerate:      DrawRegenerateTab();      break;
                case Tab.CombatPolicy:    DrawCombatPolicyTab();    break;
            }
        }

        // ── 탭 헤더 ───────────────────────────────────────────────
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawTabButton("Definition 마이그레이션", Tab.Migration);
            DrawTabButton("Player 기본 스탯", Tab.PlayerCharacter);
            DrawTabButton("템플릿 생성", Tab.Template);
            DrawTabButton("스탯 재생성", Tab.Regenerate);
            DrawTabButton("전투 정책", Tab.CombatPolicy);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabButton(string label, Tab tab)
        {
            bool isCurrent = _currentTab == tab;
            var style = isCurrent ? EditorStyles.toolbarButton : EditorStyles.toolbarButton;
            var prevColor = GUI.backgroundColor;
            if (isCurrent) GUI.backgroundColor = new Color(0.4f, 0.6f, 0.9f);
            if (GUILayout.Button(label, style, GUILayout.Width(160)))
                _currentTab = tab;
            GUI.backgroundColor = prevColor;
        }

        // ──────────────────────────────────────────────────────────
        // 마이그레이션 탭
        // ──────────────────────────────────────────────────────────

        private void DrawMigrationTab()
        {
            DrawMigrationToolbar();
            DrawMigrationColumnHeader();
            DrawMigrationRows();
            DrawMigrationFooter();
        }

        private void DrawMigrationToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RefreshMigrationRows();

            if (GUILayout.Button("전체 보정", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RepairAllDefinitions();

            if (GUILayout.Button("검증", EditorStyles.toolbarButton, GUILayout.Width(50)))
                ValidateStatDataCoverage(showDialog: true);

            GUILayout.Space(6);

            bool prevMissing = _onlyMissing;
            _onlyMissing = GUILayout.Toggle(_onlyMissing, "누락 항목만", EditorStyles.toolbarButton, GUILayout.Width(90));
            if (prevMissing != _onlyMissing) RefreshMigrationRows();

            GUILayout.Space(12);

            GUILayout.Label("저장 경로", GUILayout.Width(60));
            _savePath = EditorGUILayout.TextField(_savePath, GUILayout.MinWidth(220));
            if (GUILayout.Button("...", EditorStyles.toolbarButton, GUILayout.Width(28)))
                BrowseSavePath(ref _savePath);

            GUILayout.FlexibleSpace();

            GUILayout.Label($"총 {_migrationRows.Count}개", EditorStyles.toolbarButton, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMigrationColumnHeader()
        {
            var rect = GUILayoutUtility.GetRect(0, RowH, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, ColorHeader);

            float x = rect.x;
            // 전체 선택 체크박스
            bool newAllSelected = GUI.Toggle(new Rect(x + 4, rect.y + 3, ColCheck, rect.height), _allSelected, GUIContent.none);
            if (newAllSelected != _allSelected)
            {
                _allSelected = newAllSelected;
                foreach (var row in _migrationRows) row.Selected = _allSelected;
            }
            x += ColCheck;

            DrawHeaderCell("ActorDefinitionSO", ref x, ColName, rect.y, rect.height);
            DrawHeaderCell("PoiseSO",    ref x, ColPoise, rect.y, rect.height);
            DrawHeaderCell("BreakGaugeSO", ref x, ColBreak, rect.y, rect.height);
            DrawHeaderCell("기존 statData", ref x, ColCurrent, rect.y, rect.height);
            DrawHeaderCell("생성 예정", ref x, ColPlanned, rect.y, rect.height);
        }

        private void DrawHeaderCell(string label, ref float x, float w, float y, float h)
        {
            GUI.Label(new Rect(x + 4, y, w, h), label, EditorStyles.boldLabel);
            x += w;
        }

        private void DrawMigrationRows()
        {
            _migrationScroll = EditorGUILayout.BeginScrollView(_migrationScroll);

            for (int i = 0; i < _migrationRows.Count; i++)
            {
                var row = _migrationRows[i];
                var rect = GUILayoutUtility.GetRect(0, RowH, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, i % 2 == 0 ? ColorRowEven : ColorRowOdd);

                float x = rect.x;

                // 체크박스
                row.Selected = GUI.Toggle(new Rect(x + 4, rect.y + 3, ColCheck, rect.height), row.Selected, GUIContent.none);
                x += ColCheck;

                // Definition 이름 (클릭 시 핑)
                if (GUI.Button(new Rect(x + 2, rect.y, ColName - 4, rect.height), row.Definition.name, EditorStyles.label))
                    EditorGUIUtility.PingObject(row.Definition);
                x += ColName;

                // 소스
                DrawSourceLabel(new Rect(x, rect.y, ColPoise, rect.height), row.SourcePoise != null);
                x += ColPoise;

                // 브레이크 게이지 데이터
                DrawSourceLabel(new Rect(x, rect.y, ColBreak, rect.height), row.SourceBreakGauge != null);
                x += ColBreak;

                // 기존 statData
                if (row.ExistingStat != null)
                {
                    var prev = GUI.color;
                    GUI.color = ColorOk;
                    GUI.Label(new Rect(x + 2, rect.y, ColCurrent, rect.height), $"✓ {row.ExistingStat.name}", EditorStyles.miniLabel);
                    GUI.color = prev;
                }
                else
                {
                    var prev = GUI.color;
                    GUI.color = ColorMissing;
                    GUI.Label(new Rect(x + 2, rect.y, ColCurrent, rect.height), "(없음)", EditorStyles.miniLabel);
                    GUI.color = prev;
                }
                x += ColCurrent;

                // 생성 예정 이름
                GUI.Label(new Rect(x + 2, rect.y, ColPlanned, rect.height), row.PlannedAssetName, EditorStyles.miniLabel);
                x += ColPlanned;

                // 단일 생성 버튼
                string buttonLabel = row.ExistingStat == null ? "생성" : row.SourceBreakGauge == null ? "보정" : "재생성";
                if (GUI.Button(new Rect(rect.xMax - 60, rect.y + 1, 56, rect.height - 2), buttonLabel))
                {
                    GenerateSingle(row, buttonLabel == "재생성");
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSourceLabel(Rect rect, bool exists)
        {
            var prev = GUI.color;
            GUI.color = exists ? ColorOk : new Color(0.55f, 0.55f, 0.55f);
            GUI.Label(new Rect(rect.x + 4, rect.y, rect.width, rect.height), exists ? "✓" : "—", EditorStyles.miniLabel);
            GUI.color = prev;
        }

        private void DrawMigrationFooter()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            int selectedCount = _migrationRows.FindAll(r => r.Selected).Count;
            int selectedExisting = _migrationRows.FindAll(r => r.Selected && r.ExistingStat != null).Count;
            using (new EditorGUI.DisabledScope(selectedCount == 0))
            {
                if (GUILayout.Button($"선택 항목 일괄 생성 ({selectedCount}개)", GUILayout.Height(28)))
                    GenerateSelected();
            }

            using (new EditorGUI.DisabledScope(_migrationRows.Count == 0))
            {
                if (GUILayout.Button("누락 항목 모두 생성", GUILayout.Height(28)))
                    GenerateAllMissing();
            }

            EditorGUILayout.EndHorizontal();

            // 이미 등록된 statData까지 등급 템플릿으로 재발급(덮어쓰기)한다.
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(selectedExisting == 0))
            {
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = ColorMissing;
                if (GUILayout.Button($"선택 항목 재마이그레이션 (기존 {selectedExisting}개 덮어쓰기)", GUILayout.Height(24)))
                {
                    string deleteNote = _deleteReplacedStats
                        ? "교체·고아가 된 기존 prefab-local 에셋은 다른 곳이 참조하지 않으면 휴지통으로 이동됩니다(복구 가능).\n"
                        : "기존 에셋은 참조만 끊긴 채 남습니다.\n";
                    bool ok = EditorUtility.DisplayDialog(
                        "재마이그레이션",
                        $"이미 등록된 statData {selectedExisting}개를 등급 템플릿 기준으로 다시 발급해 덮어씁니다.\n" +
                        "수동으로 조정한 스탯 값이 있다면 초기화됩니다.\n" +
                        "(새 ActorStatSO를 중앙 저장 경로에 생성해 재연결합니다.)\n" +
                        deleteNote +
                        "\n계속할까요?",
                        "재마이그레이션", "취소");
                    if (ok)
                    {
                        GenerateSelected(forceRegenerate: true);
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = prevColor;
            }
            _deleteReplacedStats = GUILayout.Toggle(
                _deleteReplacedStats, "교체·고아 에셋 정리", GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            // 과거 실행에서 이미 중앙으로 리포인트된 뒤 남은 prefab-local 고아를 별도로 정리한다.
            if (GUILayout.Button("프리팹 폴더 고아 스탯 정리 (미참조 → 휴지통)", GUILayout.Height(22)))
            {
                SweepColocatedOrphans();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.HelpBox(
                "전체 보정은 statData가 없는 ActorDefinitionSO에 ActorStatSO를 생성해 연결하고, 기존 statData의 누락 StatType을 채웁니다.\n" +
                "등급별 템플릿으로 기본값을 채우고, PoiseSO 값 → MaxPoise/Recovery* 로 초기화됩니다.\n" +
                "몬스터의 breakGaugeData가 비어 있으면 BreakGaugeSO를 생성해 연결합니다.\n" +
                "몬스터의 monsterScaling이 비어 있으면 MonsterScalingSO를 찾아 연결하고, 없으면 기본 Growth 에셋을 생성합니다.\n" +
                "'재마이그레이션'은 이미 등록된 statData도 현재 등급 템플릿으로 다시 발급해 덮어씁니다. ('누락 항목만' 토글을 꺼야 기존 항목이 보입니다.)\n" +
                "'교체·고아 에셋 정리'가 켜져 있으면, 처리한 정의 폴더에서 더 이상 참조되지 않는 prefab-local ActorStatSO를 휴지통으로 이동합니다(공유 에셋은 보존).\n" +
                "'프리팹 폴더 고아 스탯 정리'는 과거 마이그레이션으로 이미 중앙으로 옮겨졌지만 프리팹 Descs 폴더에 남은 미참조 ActorStatSO를 일괄 정리합니다.\n" +
                "※ 03.Prefabs 경로는 현재 git 추적 대상이 아니므로 삭제는 영구 삭제가 아닌 OS 휴지통 이동으로 처리됩니다.",
                MessageType.Info);
        }

        // ── 마이그레이션 동작 ─────────────────────────────────────

        private void RefreshMigrationRows()
        {
            _migrationRows.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (def == null) continue;

                if (_onlyMissing && !HasMissingMigrationData(def)) continue;

                _migrationRows.Add(new MigrationRow
                {
                    Definition = def,
                    SourcePoise = def.poiseData,
                    SourceBreakGauge = def.breakGaugeData,
                    ExistingStat = def.statData,
                    PlannedAssetName = MakePlannedAssetName(def),
                    Selected = def.statData == null,
                });
            }
            _migrationRows.Sort((a, b) => string.Compare(a.Definition.name, b.Definition.name, StringComparison.Ordinal));
        }

        private void GenerateSingle(MigrationRow row, bool regenerateStat)
        {
            EnsureFolder(_savePath);
            EnsureFolder(BreakGaugeSavePath);
            EnsureFolder(MonsterScalingSavePath);
            ActorStatSO so = row.Definition.statData;
            string assetPath = null;

            // Definition에 자동 연결
            var sObj = new SerializedObject(row.Definition);
            bool linkedScaling = EnsureMonsterScalingLinked(row.Definition);
            if (so == null || regenerateStat)
            {
                so = BuildFromDefinition(row);
                assetPath = AssetDatabase.GenerateUniqueAssetPath($"{_savePath}/{row.PlannedAssetName}.asset");
                AssetDatabase.CreateAsset(so, assetPath);
                sObj.FindProperty("statData").objectReferenceValue = so;
                sObj.ApplyModifiedProperties();
            }

            string breakGaugePath = GenerateMissingBreakGauge(row.Definition, so);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // 교체 에셋 삭제는 확인 다이얼로그가 있는 bulk 재마이그레이션(GenerateSelected)에서만 수행한다.
            // 단일 행 "재생성"은 비파괴 동작을 유지(무확인 삭제 방지).
            Debug.Log(string.IsNullOrEmpty(assetPath) && !string.IsNullOrEmpty(breakGaugePath)
                ? $"[StatDataGenerator] 보정 완료: {breakGaugePath} → {row.Definition.name}.breakGaugeData"
                : string.IsNullOrEmpty(breakGaugePath)
                ? $"[StatDataGenerator] 생성 완료: {assetPath} → {row.Definition.name}.statData"
                : $"[StatDataGenerator] 생성 완료: {assetPath} / {breakGaugePath} → {row.Definition.name}");
            if (linkedScaling)
                Debug.Log($"[StatDataGenerator] Growth 연결 완료: {row.Definition.name}.monsterScaling");

            row.ExistingStat = so;
            RefreshMigrationRows();
        }

        private void GenerateSelected(bool forceRegenerate = false)
        {
            EnsureFolder(_savePath);
            EnsureFolder(BreakGaugeSavePath);
            EnsureFolder(MonsterScalingSavePath);
            int created = 0;
            int regenerated = 0;
            int breakCount = 0;
            int scalingLinked = 0;
            var replacedPaths = new List<string>();
            foreach (var row in _migrationRows)
            {
                if (!row.Selected) continue;
                ActorStatSO so = row.Definition.statData;

                var sObj = new SerializedObject(row.Definition);
                if (EnsureMonsterScalingLinked(row.Definition))
                    scalingLinked++;

                // statData가 없으면 신규 생성, forceRegenerate면 이미 등록된 것도 등급 템플릿으로 재발급한다.
                if (so == null || forceRegenerate)
                {
                    ActorStatSO previous = so;
                    so = BuildFromDefinition(row);
                    string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{_savePath}/{row.PlannedAssetName}.asset");
                    AssetDatabase.CreateAsset(so, assetPath);
                    sObj.FindProperty("statData").objectReferenceValue = so;
                    if (previous != null)
                    {
                        // 교체된 기존 에셋 경로를 모아 두었다가 일괄 정리한다.
                        string prevPath = AssetDatabase.GetAssetPath(previous);
                        if (!string.IsNullOrEmpty(prevPath))
                            replacedPaths.Add(prevPath);
                        regenerated++;
                    }
                    else
                    {
                        created++;
                    }
                }

                string breakGaugePath = GenerateMissingBreakGauge(row.Definition, so);
                if (!string.IsNullOrEmpty(breakGaugePath))
                    breakCount++;

                sObj.ApplyModifiedProperties();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int deleted = 0;
            if (_deleteReplacedStats)
            {
                // 이번 실행에서 교체된 에셋 + 처리한 정의 폴더에 남은 prefab-local 고아까지 함께 정리한다.
                // (과거 실행에서 이미 중앙으로 리포인트돼 'previous'로 잡히지 않는 고아를 포착)
                var processed = new List<ActorDefinitionSO>();
                foreach (var row in _migrationRows)
                    if (row.Selected) processed.Add(row.Definition);
                replacedPaths.AddRange(CollectColocatedStatCandidates(processed));
                deleted = CleanupUnreferencedStats(replacedPaths);
            }

            Debug.Log($"[StatDataGenerator] ActorStatSO 신규 {created}개 / 재마이그레이션 {regenerated}개 / Growth 연결 {scalingLinked}개 / 고아 정리 {deleted}개 / BreakGaugeSO {breakCount}개 완료");
            RefreshMigrationRows();
            ValidateStatDataCoverage(showDialog: false);
        }

        /// <summary>
        /// 후보 경로 중 더 이상 어떤 에셋도 직렬화 참조하지 않게 된 ActorStatSO를 휴지통으로 이동한다.
        /// 다른 에셋(정의/스케일링/성장)이 여전히 참조 중인 공유 에셋은 보존한다.
        /// prefab-local 경로(03.Prefabs)는 git 추적 대상이 아니므로 영구 삭제(DeleteAsset) 대신
        /// 복구 가능한 MoveAssetToTrash(OS 휴지통)를 사용한다.
        /// </summary>
        private static int CleanupUnreferencedStats(List<string> candidatePaths, HashSet<string> referencedGuids = null)
        {
            if (candidatePaths == null || candidatePaths.Count == 0)
                return 0;

            referencedGuids ??= CollectReferencedStatGuids();
            int moved = 0;
            var seen = new HashSet<string>();
            foreach (var path in candidatePaths)
            {
                if (string.IsNullOrEmpty(path) || !seen.Add(path))
                    continue;

                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid) || referencedGuids.Contains(guid))
                    continue; // 다른 곳이 여전히 참조 → 공유 에셋이므로 보존

                if (AssetDatabase.MoveAssetToTrash(path))
                {
                    moved++;
                    Debug.Log($"[StatDataGenerator] 미참조 statData 휴지통 이동: {path}");
                }
            }

            if (moved > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return moved;
        }

        /// <summary>
        /// 주어진 정의들과 같은 폴더(하위 폴더 제외)에 위치한 ActorStatSO 에셋 경로를 수집한다.
        /// P09Builder가 프리팹 Descs 폴더에 만든 prefab-local 스탯을 도출하기 위한 구조적 신호로,
        /// 경로를 하드코딩하지 않고 정의 위치에서 동적으로 폴더를 얻는다.
        /// 참조 여부(보존/정리) 판단은 호출부의 CleanupUnreferencedStats가 수행한다.
        /// </summary>
        private static List<string> CollectColocatedStatCandidates(IEnumerable<ActorDefinitionSO> defs)
        {
            var list = new List<string>();
            if (defs == null) return list;

            foreach (var def in defs)
            {
                if (def == null) continue;
                string defPath = AssetDatabase.GetAssetPath(def);
                if (string.IsNullOrEmpty(defPath)) continue;

                string folder = (Path.GetDirectoryName(defPath) ?? string.Empty).Replace('\\', '/');
                if (string.IsNullOrEmpty(folder)) continue;

                foreach (var guid in AssetDatabase.FindAssets("t:ActorStatSO", new[] { folder }))
                {
                    string p = AssetDatabase.GUIDToAssetPath(guid);
                    string pFolder = (Path.GetDirectoryName(p) ?? string.Empty).Replace('\\', '/');
                    if (pFolder == folder) // 같은 폴더만(FindAssets는 하위 폴더까지 검색하므로 필터)
                        list.Add(p);
                }
            }
            return list;
        }

        /// <summary>
        /// 모든 ActorDefinitionSO 폴더를 훑어, 정의와 같은 폴더에 있으나 더 이상 어디서도 참조되지 않는
        /// prefab-local ActorStatSO(= 과거 P09Builder가 만든 대체된 연결의 고아)를 휴지통으로 이동한다.
        /// 과거 실행에서 이미 중앙으로 리포인트돼 재마이그레이션의 'previous'로 잡히지 않는 고아를 정리하는 진입점.
        /// </summary>
        private void SweepColocatedOrphans()
        {
            var allDefs = new List<ActorDefinitionSO>();
            foreach (var guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                var def = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (def != null) allDefs.Add(def);
            }

            var candidates = CollectColocatedStatCandidates(allDefs);
            var referenced = CollectReferencedStatGuids();
            var orphans = new List<string>();
            var seen = new HashSet<string>();
            foreach (var p in candidates)
            {
                if (string.IsNullOrEmpty(p) || !seen.Add(p)) continue;
                string g = AssetDatabase.AssetPathToGUID(p);
                if (string.IsNullOrEmpty(g) || referenced.Contains(g)) continue;
                orphans.Add(p);
            }

            if (orphans.Count == 0)
            {
                EditorUtility.DisplayDialog("고아 스탯 정리", "정리할 미참조 prefab-local ActorStatSO가 없습니다.", "확인");
                return;
            }

            int previewN = Mathf.Min(orphans.Count, 8);
            string preview = string.Join("\n", orphans.GetRange(0, previewN).ConvertAll(Path.GetFileNameWithoutExtension));
            if (orphans.Count > previewN) preview += $"\n... 외 {orphans.Count - previewN}개";

            bool ok = EditorUtility.DisplayDialog(
                "고아 스탯 정리",
                $"ActorDef와 같은 폴더에 있으나 더 이상 어디서도 참조되지 않는 ActorStatSO {orphans.Count}개를 휴지통으로 이동합니다.\n" +
                "이 경로(03.Prefabs)는 git 추적 대상이 아니므로 영구 삭제 대신 OS 휴지통으로 이동합니다(복구 가능).\n\n" +
                preview + "\n\n계속할까요?",
                "휴지통으로 이동", "취소");
            if (!ok) return;

            int moved = CleanupUnreferencedStats(orphans, referenced);
            EditorUtility.DisplayDialog("고아 스탯 정리", $"{moved}개를 휴지통으로 이동했습니다.", "확인");
            RefreshMigrationRows();
        }

        /// <summary>
        /// ActorStatSO를 참조하는 모든 에셋이 현재 가리키는 GUID 집합.
        /// 1) 알려진 중앙 참조 타입(ActorDefinitionSO.statData, MonsterScalingSO.baseStat,
        ///    PartyMemberGrowthSO.baseStat)을 명시적으로 수집하고,
        /// 2) 그 외 프리팹/ScriptableObject가 직렬화 필드로 직접 참조하는 경우까지
        ///    의존성 스캔으로 포착한다(미탐 참조로 인한 오삭제 방지).
        /// </summary>
        private static HashSet<string> CollectReferencedStatGuids()
        {
            var set = new HashSet<string>();

            void Add(ActorStatSO stat)
            {
                if (stat == null) return;
                string path = AssetDatabase.GetAssetPath(stat);
                if (string.IsNullOrEmpty(path)) return;
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid)) set.Add(guid);
            }

            // 1) 알려진 중앙 참조 타입 (빠른 경로)
            foreach (var guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
                Add(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid))?.statData);

            foreach (var guid in AssetDatabase.FindAssets("t:MonsterScalingSO"))
                Add(AssetDatabase.LoadAssetAtPath<MonsterScalingSO>(AssetDatabase.GUIDToAssetPath(guid))?.baseStat);

            foreach (var guid in AssetDatabase.FindAssets("t:PartyMemberGrowthSO"))
                Add(AssetDatabase.LoadAssetAtPath<PartyMemberGrowthSO>(AssetDatabase.GUIDToAssetPath(guid))?.baseStat);

            AddDependencyReferences(set);
            return set;
        }

        /// <summary>
        /// 프리팹/ScriptableObject가 의존성으로 직접 참조하는 ActorStatSO의 GUID를 referenced 집합에 추가한다.
        /// 알려진 중앙 참조 타입 외의 경로(예: 프리팹 컴포넌트 필드의 직접 참조)로 묶인 스탯이
        /// 고아로 오판되어 삭제되는 것을 막기 위한 안전망. (에디터 1회성이라 비용은 허용 범위)
        /// </summary>
        private static void AddDependencyReferences(HashSet<string> referenced)
        {
            // 프로젝트의 모든 ActorStatSO 경로 집합(역참조 교집합 대상)
            var statPaths = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:ActorStatSO"))
                statPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
            if (statPaths.Count == 0)
                return;

            void ScanHolders(string filter)
            {
                foreach (var guid in AssetDatabase.FindAssets(filter))
                {
                    string holderPath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(holderPath))
                        continue;

                    foreach (var dep in AssetDatabase.GetDependencies(holderPath, recursive: false))
                    {
                        if (dep == holderPath || !statPaths.Contains(dep))
                            continue;
                        string depGuid = AssetDatabase.AssetPathToGUID(dep);
                        if (!string.IsNullOrEmpty(depGuid))
                            referenced.Add(depGuid);
                    }
                }
            }

            ScanHolders("t:GameObject");        // 프리팹
            ScanHolders("t:ScriptableObject");  // 그 외 SO 참조
        }

        private void GenerateAllMissing()
        {
            EnsureFolder(_savePath);
            EnsureFolder(BreakGaugeSavePath);
            EnsureFolder(MonsterScalingSavePath);
            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            int count = 0;
            int breakCount = 0;
            int scalingLinked = 0;
            foreach (var guid in guids)
            {
                var def = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (def == null || !HasMissingMigrationData(def)) continue;

                var row = new MigrationRow
                {
                    Definition = def,
                    SourcePoise = def.poiseData,
                    SourceBreakGauge = def.breakGaugeData,
                    PlannedAssetName = MakePlannedAssetName(def),
                };

                ActorStatSO so = def.statData;
                var sObj = new SerializedObject(def);
                if (EnsureMonsterScalingLinked(def))
                    scalingLinked++;

                if (so == null)
                {
                    so = BuildFromDefinition(row);
                    string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{_savePath}/{row.PlannedAssetName}.asset");
                    AssetDatabase.CreateAsset(so, assetPath);
                    sObj.FindProperty("statData").objectReferenceValue = so;
                    count++;
                }

                string breakGaugePath = GenerateMissingBreakGauge(def, so);
                if (!string.IsNullOrEmpty(breakGaugePath))
                    breakCount++;

                sObj.ApplyModifiedProperties();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StatDataGenerator] 누락분 일괄 생성 완료: ActorStatSO {count}개 / Growth 연결 {scalingLinked}개 / BreakGaugeSO {breakCount}개");
            RefreshMigrationRows();
            ValidateStatDataCoverage(showDialog: false);
        }

        private void RepairAllDefinitions()
        {
            EnsureFolder(_savePath);
            EnsureFolder(BreakGaugeSavePath);
            EnsureFolder(MonsterScalingSavePath);

            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            int created = 0;
            int filled = 0;
            int breakCreated = 0;
            int scalingLinked = 0;

            foreach (var guid in guids)
            {
                var def = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (def == null) continue;

                if (EnsureMonsterScalingLinked(def))
                    scalingLinked++;

                if (def.statData == null)
                {
                    var row = new MigrationRow
                    {
                        Definition = def,
                        SourcePoise = def.poiseData,
                        SourceBreakGauge = def.breakGaugeData,
                        PlannedAssetName = MakePlannedAssetName(def),
                    };

                    var so = BuildFromDefinition(row);
                    string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{_savePath}/{row.PlannedAssetName}.asset");
                    AssetDatabase.CreateAsset(so, assetPath);

                    var sObj = new SerializedObject(def);
                    sObj.FindProperty("statData").objectReferenceValue = so;
                    sObj.ApplyModifiedProperties();
                    created++;

                    string breakGaugePath = GenerateMissingBreakGauge(def, so);
                    if (!string.IsNullOrEmpty(breakGaugePath))
                        breakCreated++;
                    continue;
                }

                if (HasMissingStats(def.statData))
                {
                    Undo.RecordObject(def.statData, "Fill Missing Actor Stats");
                    def.statData.EditorFillMissing();
                    EditorUtility.SetDirty(def.statData);
                    filled++;
                }

                string createdBreakGaugePath = GenerateMissingBreakGauge(def, def.statData);
                if (!string.IsNullOrEmpty(createdBreakGaugePath))
                    breakCreated++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StatDataGenerator] 전체 보정 완료: statData 생성 {created}개 / Growth 연결 {scalingLinked}개 / 누락 StatType 채움 {filled}개 / breakGaugeData 생성 {breakCreated}개");
            RefreshMigrationRows();
            ValidateStatDataCoverage(showDialog: true);
        }

        private static ActorStatSO BuildFromDefinition(MigrationRow row)
        {
            // 등급은 definition.grade를 권위로 삼지만, 과거에 빌드된 정의는 grade가 기록되지 않아
            // 기본값(Normal)으로 남아있다. 프리팹의 MonsterActor에 빌드 시점 등급이 보존돼 있으므로
            // 템플릿 적용 전에 프리팹 → 정의로 등급/레벨을 백필해 재마이그레이션 정확도를 보장한다.
            BackfillGradeLevelFromPrefab(row.Definition);

            var so = ScriptableObject.CreateInstance<ActorStatSO>();
            so.EditorFillMissing();

            // MonsterScalingSO가 연결되어 있으면 Growth 기준을 우선 사용한다.
            MonsterScalingSO scaling = row.Definition != null ? row.Definition.monsterScaling : null;
            if (scaling != null)
            {
                Dictionary<StatType, float> values = MonsterStatCalculator.Calculate(scaling, row.Definition);
                foreach (KeyValuePair<StatType, float> pair in values)
                    so.EditorSet(pair.Key, pair.Value);
            }
            else
            {
                // 정의에 작성된 등급에 따라 기본 템플릿 적용
                ApplyGradeTemplate(so, row.Definition != null ? row.Definition.grade : MonsterActorGrade.Normal);
            }

            // PoiseSO → MaxPoise / PoiseRecoveryRate / PoiseRecoveryDelay
            if (row.SourcePoise != null)
            {
                so.EditorSet(StatType.MaxPoise,           row.SourcePoise.maxPoise);
                so.EditorSet(StatType.PoiseRecoveryRate,  row.SourcePoise.recoveryRate);
                so.EditorSet(StatType.PoiseRecoveryDelay, row.SourcePoise.recoveryDelay);
            }

            return so;
        }

        /// <summary>
        /// 정의의 grade/level이 기본값으로 남아있더라도, 연결된 프리팹의 MonsterActor가
        /// 빌드 시점 등급/레벨을 보존하고 있으면 그 값을 정의로 백필한다.
        /// (프리팹이 없거나 MonsterActor가 아니면 아무것도 하지 않는다.)
        /// </summary>
        private static void BackfillGradeLevelFromPrefab(ActorDefinitionSO def)
        {
            if (def == null || def.prefab == null)
                return;

            var actor = def.prefab.GetComponent<MonsterActor>();
            if (actor == null)
                return;

            bool changed = false;
            if (def.grade != actor.Grade)
            {
                def.grade = actor.Grade;
                changed = true;
            }

            int level = Mathf.Max(1, actor.Level);
            if (def.level != level)
            {
                def.level = level;
                changed = true;
            }

            if (changed)
                EditorUtility.SetDirty(def);
        }

        private static string GenerateMissingBreakGauge(ActorDefinitionSO def, ActorStatSO stat)
        {
            if (!NeedsBreakGauge(def))
                return null;

            var data = ScriptableObject.CreateInstance<MonsterBreakGaugeSO>();
            data.name = MakeBreakGaugeAssetName(def);
            data.maxGauge = CalculateBreakGauge(def, stat);
            data.gradePolicy = new MonsterBreakGradePolicy
            {
                weakGaugeMultiplier = 1f,
                normalGaugeMultiplier = 1f,
                eliteGaugeMultiplier = 1f,
                bossGaugeMultiplier = 1f,
            };

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{BreakGaugeSavePath}/{data.name}.asset");
            AssetDatabase.CreateAsset(data, assetPath);

            var sObj = new SerializedObject(def);
            sObj.FindProperty("breakGaugeData").objectReferenceValue = data;
            sObj.ApplyModifiedProperties();
            EditorUtility.SetDirty(def);

            return assetPath;
        }

        private static float CalculateBreakGauge(ActorDefinitionSO def, ActorStatSO stat)
        {
            if (stat != null)
                return Mathf.Max(1f, Mathf.Round(stat.GetBase(StatType.MaxPoise)));

            if (def?.poiseData != null)
                return Mathf.Max(1f, Mathf.Round(def.poiseData.maxPoise));

            return Mathf.Max(1f, ActorStatSO.GetDefault(StatType.MaxPoise));
        }

        private static bool HasMissingMigrationData(ActorDefinitionSO def)
            => def != null && (def.statData == null || NeedsBreakGauge(def) || NeedsMonsterScaling(def));

        private static bool NeedsBreakGauge(ActorDefinitionSO def)
            => IsMonster(def) && def.breakGaugeData == null;

        private static bool NeedsMonsterScaling(ActorDefinitionSO def)
            => IsMonster(def) && def.monsterScaling == null;

        private static bool IsMonster(ActorDefinitionSO def)
            => def != null && (def.actorType & ActorType.Monster) != 0;

        private static bool EnsureMonsterScalingLinked(ActorDefinitionSO def)
        {
            if (!NeedsMonsterScaling(def))
                return false;

            MonsterScalingSO scaling = FindOrCreateMonsterScaling();
            if (scaling == null)
                return false;

            Undo.RecordObject(def, "Link Monster Scaling");
            var so = new SerializedObject(def);
            so.FindProperty("monsterScaling").objectReferenceValue = scaling;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(def);
            return true;
        }

        private static MonsterScalingSO FindOrCreateMonsterScaling()
        {
            string[] guids = AssetDatabase.FindAssets("t:MonsterScalingSO");
            if (guids.Length > 0)
            {
                Array.Sort(guids, (a, b) => string.Compare(
                    AssetDatabase.GUIDToAssetPath(a),
                    AssetDatabase.GUIDToAssetPath(b),
                    StringComparison.Ordinal));
                return AssetDatabase.LoadAssetAtPath<MonsterScalingSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            EnsureFolder(MonsterScalingSavePath);
            var scaling = ScriptableObject.CreateInstance<MonsterScalingSO>();
            scaling.FillDefaults();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{MonsterScalingSavePath}/MonsterScaling_Default.asset");
            AssetDatabase.CreateAsset(scaling, path);
            return scaling;
        }

        // ──────────────────────────────────────────────────────────
        // Player 기본 스탯 탭
        // ──────────────────────────────────────────────────────────

        private void DrawPlayerCharacterTab()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("CharacterActorType별 기본 ActorStatSO 생성", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            DrawPlayerToolbar();
            EditorGUILayout.Space(4);
            DrawPlayerCharacterRows();
            EditorGUILayout.Space(4);
            DrawPlayerFooter();
        }

        private void DrawPlayerToolbar()
        {
            EditorGUILayout.BeginVertical("helpbox");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("저장 경로", GUILayout.Width(70));
            _playerSavePath = EditorGUILayout.TextField(_playerSavePath);
            if (GUILayout.Button("...", GUILayout.Width(28)))
                BrowseSavePath(ref _playerSavePath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("이름 접두", GUILayout.Width(70));
            _playerNamePrefix = EditorGUILayout.TextField(_playerNamePrefix);
            EditorGUILayout.EndHorizontal();

            _playerOverwrite = EditorGUILayout.ToggleLeft(
                "기존 SO가 있으면 값을 덮어쓴다 (체크 해제 시 새 파일로 복제)",
                _playerOverwrite);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("기존 자산 다시 스캔", GUILayout.Height(20)))
                RefreshPlayerExisting();
            if (GUILayout.Button("플레이어블만 선택 (Bokusei/Honoka/LianLian)", GUILayout.Height(20)))
                SelectPlayablesOnly();
            if (GUILayout.Button("전체 선택", GUILayout.Height(20)))
                SetAllPlayerSelection(true);
            if (GUILayout.Button("전체 해제", GUILayout.Height(20)))
                SetAllPlayerSelection(false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawPlayerCharacterRows()
        {
            // 컬럼 헤더
            var headerRect = GUILayoutUtility.GetRect(0, RowH, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(headerRect, ColorHeader);
            float hx = headerRect.x;
            DrawHeaderCell("",          ref hx, ColCheck,   headerRect.y, headerRect.height);
            DrawHeaderCell("Character", ref hx, 100f,       headerRect.y, headerRect.height);
            DrawHeaderCell("프리셋",     ref hx, 110f,       headerRect.y, headerRect.height);
            DrawHeaderCell("HP",        ref hx, 60f,        headerRect.y, headerRect.height);
            DrawHeaderCell("ATK",       ref hx, 60f,        headerRect.y, headerRect.height);
            DrawHeaderCell("MOV",       ref hx, 60f,        headerRect.y, headerRect.height);
            DrawHeaderCell("CRIT%",     ref hx, 60f,        headerRect.y, headerRect.height);
            DrawHeaderCell("Poise",     ref hx, 60f,        headerRect.y, headerRect.height);
            DrawHeaderCell("기존 자산",  ref hx, 200f,       headerRect.y, headerRect.height);

            _playerScroll = EditorGUILayout.BeginScrollView(_playerScroll, GUILayout.MinHeight(180));

            int row = 0;
            foreach (CharacterActorType type in System.Enum.GetValues(typeof(CharacterActorType)))
            {
                if (type == CharacterActorType.None) continue;

                var rect = GUILayoutUtility.GetRect(0, RowH, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, row % 2 == 0 ? ColorRowEven : ColorRowOdd);

                if (!_playerSelected.TryGetValue(type, out bool selected)) selected = false;
                _playerExisting.TryGetValue(type, out var existing);

                float x = rect.x;

                // 체크박스
                bool newSelected = GUI.Toggle(new Rect(x + 4, rect.y + 3, ColCheck, rect.height), selected, GUIContent.none);
                if (newSelected != selected) _playerSelected[type] = newSelected;
                x += ColCheck;

                // 캐릭터 이름
                GUI.Label(new Rect(x + 2, rect.y, 100f, rect.height), type.ToString());
                x += 100f;

                // 프리셋 종류
                string presetName = HasCharacterPreset(type) ? "전용 프리셋" : "기본 (Player)";
                var prevColor = GUI.color;
                GUI.color = HasCharacterPreset(type) ? new Color(0.3f, 0.7f, 0.95f) : new Color(0.7f, 0.7f, 0.7f);
                GUI.Label(new Rect(x + 2, rect.y, 110f, rect.height), presetName, EditorStyles.miniLabel);
                GUI.color = prevColor;
                x += 110f;

                // 미리보기 값
                using (var preview = new ScopedPreview(type))
                {
                    DrawMiniValue(new Rect(x, rect.y, 60f, rect.height), preview.SO.GetBase(StatType.MaxHealth));
                    x += 60f;
                    DrawMiniValue(new Rect(x, rect.y, 60f, rect.height), preview.SO.GetBase(StatType.AttackPower));
                    x += 60f;
                    DrawMiniValue(new Rect(x, rect.y, 60f, rect.height), preview.SO.GetBase(StatType.MoveSpeed));
                    x += 60f;
                    DrawMiniValue(new Rect(x, rect.y, 60f, rect.height), preview.SO.GetBase(StatType.CritRate) * 100f, suffix: "%");
                    x += 60f;
                    DrawMiniValue(new Rect(x, rect.y, 60f, rect.height), preview.SO.GetBase(StatType.MaxPoise));
                    x += 60f;
                }

                // 기존 자산
                if (existing != null)
                {
                    GUI.color = ColorOk;
                    if (GUI.Button(new Rect(x + 2, rect.y, 200f, rect.height), $"✓ {existing.name}", EditorStyles.miniLabel))
                        EditorGUIUtility.PingObject(existing);
                    GUI.color = prevColor;
                }
                else
                {
                    GUI.color = new Color(0.55f, 0.55f, 0.55f);
                    GUI.Label(new Rect(x + 2, rect.y, 200f, rect.height), "(없음)", EditorStyles.miniLabel);
                    GUI.color = prevColor;
                }

                row++;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawMiniValue(Rect rect, float value, string suffix = "")
        {
            string text = string.IsNullOrEmpty(suffix) ? value.ToString("0.##") : $"{value:0.##}{suffix}";
            GUI.Label(rect, text, EditorStyles.miniLabel);
        }

        private void DrawPlayerFooter()
        {
            int selectedCount = 0;
            foreach (var kv in _playerSelected) if (kv.Value) selectedCount++;

            using (new EditorGUI.DisabledScope(selectedCount == 0))
            {
                if (GUILayout.Button($"선택한 캐릭터 ({selectedCount}명) 스탯 SO 생성", GUILayout.Height(28)))
                    GeneratePlayerSelected();
            }

            EditorGUILayout.HelpBox(
                "Bokusei(균형형, 카타나) / Honoka(공격형, 쌍도끼) / LianLian(민첩형, 채찍)는 전용 프리셋이 적용됩니다.\n" +
                "그 외 캐릭터는 기본 PlayerCharacter 프리셋이 적용됩니다.\n" +
                "생성/덮어쓰기된 ActorStatSO는 같은 CharacterActorType의 PartyMemberGrowthSO.baseStat에 자동 연결됩니다.",
                MessageType.Info);
        }

        // ── Player 캐릭터 동작 ────────────────────────────────────

        private void RefreshPlayerExisting()
        {
            _playerExisting.Clear();
            foreach (CharacterActorType type in System.Enum.GetValues(typeof(CharacterActorType)))
            {
                if (type == CharacterActorType.None) continue;
                _playerExisting[type] = FindPlayerStatAsset(type);
            }
        }

        private ActorStatSO FindPlayerStatAsset(CharacterActorType type)
        {
            PartyMemberGrowthSO growth = FindPartyMemberGrowth(type);
            if (growth != null && growth.baseStat != null)
                return growth.baseStat;

            // 이름 접두 + 캐릭터명 패턴으로 검색
            string searchName = $"{_playerNamePrefix}{type}";
            string[] guids = AssetDatabase.FindAssets($"{searchName} t:ActorStatSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ActorStatSO>(path);
                if (so != null && so.name == searchName) return so;
            }
            return null;
        }

        private void SelectPlayablesOnly()
        {
            _playerSelected.Clear();
            foreach (CharacterActorType type in System.Enum.GetValues(typeof(CharacterActorType)))
            {
                if (type == CharacterActorType.None) continue;
                _playerSelected[type] = type == CharacterActorType.Bokusei
                                     || type == CharacterActorType.Honoka
                                     || type == CharacterActorType.LianLian;
            }
        }

        private void SetAllPlayerSelection(bool value)
        {
            _playerSelected.Clear();
            foreach (CharacterActorType type in System.Enum.GetValues(typeof(CharacterActorType)))
            {
                if (type == CharacterActorType.None) continue;
                _playerSelected[type] = value;
            }
        }

        private void GeneratePlayerSelected()
        {
            EnsureFolder(_playerSavePath);
            int created = 0, overwritten = 0, linked = 0;

            foreach (var kv in _playerSelected)
            {
                if (!kv.Value) continue;
                var type = kv.Key;
                string assetName = $"{_playerNamePrefix}{type}";
                _playerExisting.TryGetValue(type, out var existing);
                ActorStatSO targetStat = null;

                if (existing != null && _playerOverwrite)
                {
                    // 기존 SO에 덮어쓰기 (모든 명시값 제거 후 프리셋 재적용)
                    Undo.RecordObject(existing, "Overwrite Player Stat");
                    foreach (StatType st in System.Enum.GetValues(typeof(StatType)))
                        existing.EditorRemove(st);
                    ApplyCharacterPreset(existing, type);
                    EditorUtility.SetDirty(existing);
                    overwritten++;
                    targetStat = existing;
                }
                else
                {
                    var so = ScriptableObject.CreateInstance<ActorStatSO>();
                    so.name = assetName;
                    ApplyCharacterPreset(so, type);
                    string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{_playerSavePath}/{assetName}.asset");
                    AssetDatabase.CreateAsset(so, assetPath);
                    created++;
                    targetStat = so;
                }

                if (targetStat != null && LinkPlayerGrowthBaseStat(type, targetStat))
                    linked++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StatDataGenerator] Player 스탯 생성: 신규 {created}개 / 덮어쓰기 {overwritten}개 / Growth 연결 {linked}개");
            RefreshPlayerExisting();
        }

        private static PartyMemberGrowthSO FindPartyMemberGrowth(CharacterActorType type)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:PartyMemberGrowthSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var growth = AssetDatabase.LoadAssetAtPath<PartyMemberGrowthSO>(path);
                if (growth != null && growth.characterType == type)
                    return growth;
            }

            return null;
        }

        private static bool LinkPlayerGrowthBaseStat(CharacterActorType type, ActorStatSO stat)
        {
            PartyMemberGrowthSO growth = FindPartyMemberGrowth(type);
            if (growth == null || stat == null || growth.baseStat == stat)
                return false;

            Undo.RecordObject(growth, "Link Player Growth Base Stat");
            growth.baseStat = stat;
            EditorUtility.SetDirty(growth);
            return true;
        }

        // ── 캐릭터 프리셋 ─────────────────────────────────────────

        private static bool HasCharacterPreset(CharacterActorType type) =>
            type == CharacterActorType.Bokusei
         || type == CharacterActorType.Honoka
         || type == CharacterActorType.LianLian;

        /// <summary>
        /// 캐릭터별 컨셉을 반영한 기본 스탯 프리셋.
        /// Bokusei: 카타나, 균형형 / Honoka: 쌍도끼, 공격형 / LianLian: 채찍, 민첩형.
        /// 그 외 캐릭터는 기본 PlayerCharacter 프리셋을 사용한다.
        /// </summary>
        private static void ApplyCharacterPreset(ActorStatSO so, CharacterActorType type)
        {
            // 공통 기본값 먼저 적용
            ApplyTemplate(so, TemplateKind.PlayerCharacter);

            switch (type)
            {
                case CharacterActorType.Bokusei: // 균형형 (카타나)
                    so.EditorSet(StatType.MaxHealth,      120f);
                    so.EditorSet(StatType.AttackPower,    1.0f);
                    so.EditorSet(StatType.Defense,        0.05f);
                    so.EditorSet(StatType.CritRate,       0.05f);
                    so.EditorSet(StatType.CritMultiplier, 1.5f);
                    so.EditorSet(StatType.MoveSpeed,      1.0f);
                    so.EditorSet(StatType.DashDistance,   1.0f);
                    so.EditorSet(StatType.MaxPoise,       100f);
                    break;

                case CharacterActorType.Honoka: // 공격형 (쌍도끼)
                    so.EditorSet(StatType.MaxHealth,      110f);
                    so.EditorSet(StatType.AttackPower,    1.2f);
                    so.EditorSet(StatType.Defense,        0.0f);
                    so.EditorSet(StatType.CritRate,       0.05f);
                    so.EditorSet(StatType.CritMultiplier, 1.6f);
                    so.EditorSet(StatType.MoveSpeed,      0.95f);
                    so.EditorSet(StatType.DashDistance,   0.95f);
                    so.EditorSet(StatType.MaxPoise,       110f);
                    break;

                case CharacterActorType.LianLian: // 민첩형 (채찍)
                    so.EditorSet(StatType.MaxHealth,      100f);
                    so.EditorSet(StatType.AttackPower,    0.9f);
                    so.EditorSet(StatType.Defense,        0.0f);
                    so.EditorSet(StatType.CritRate,       0.10f);
                    so.EditorSet(StatType.CritMultiplier, 1.5f);
                    so.EditorSet(StatType.MoveSpeed,      1.15f);
                    so.EditorSet(StatType.DashDistance,   1.2f);
                    so.EditorSet(StatType.MaxPoise,       80f);
                    break;

                // 미정 캐릭터는 PlayerCharacter 폴백만 적용 (위에서 이미 처리됨)
            }
        }

        // ── 미리보기 SO 임시 객체 (using으로 자동 파괴) ──────────
        private struct ScopedPreview : IDisposable
        {
            public ActorStatSO SO;
            public ScopedPreview(CharacterActorType type)
            {
                SO = ScriptableObject.CreateInstance<ActorStatSO>();
                ApplyCharacterPreset(SO, type);
            }
            public void Dispose()
            {
                if (SO != null) UnityEngine.Object.DestroyImmediate(SO);
            }
        }

        // ──────────────────────────────────────────────────────────
        // 템플릿 탭
        // ──────────────────────────────────────────────────────────

        private void DrawTemplateTab()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("재사용 가능한 StatTemplateSO 생성/편집", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "여기서 만든 StatTemplateSO는 '스탯 재생성' 탭에서 선택해 여러 액터의 statData에 일괄 적용할 수 있습니다.\n" +
                "세부 값은 생성 후 Inspector에서 자유롭게 편집하세요. (템플릿에 없는 StatType은 재생성 시 건드리지 않습니다.)",
                MessageType.Info);
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("저장 경로", GUILayout.Width(60));
            _templateSavePath = EditorGUILayout.TextField(_templateSavePath);
            if (GUILayout.Button("...", GUILayout.Width(28)))
                BrowseSavePath(ref _templateSavePath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // ── 프리셋 기반 단일 생성 ──
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("프리셋에서 템플릿 생성", EditorStyles.boldLabel);
            _templateKind = (TemplateKind)EditorGUILayout.EnumPopup("프리셋 종류", _templateKind);
            _templateName = EditorGUILayout.TextField("자산 이름", _templateName);

            DrawTemplateKindPreview(_templateKind);

            if (GUILayout.Button("이 프리셋으로 템플릿 생성", GUILayout.Height(28)))
            {
                var created = CreateTemplateAsset(_templateKind, _templateName, _templateSavePath);
                if (created != null)
                {
                    _templateToEdit = created;
                    Selection.activeObject = created;
                    EditorGUIUtility.PingObject(created);
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            if (GUILayout.Button("기본 프리셋 템플릿 5종 일괄 생성 (Weak/Normal/Elite/Boss/Player)", GUILayout.Height(24)))
                CreateDefaultPresetTemplates(_templateSavePath);

            EditorGUILayout.Space(10);

            // ── 기존 템플릿 편집 진입 ──
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("기존 템플릿 편집", EditorStyles.boldLabel);
            _templateToEdit = (StatTemplateSO)EditorGUILayout.ObjectField("템플릿", _templateToEdit, typeof(StatTemplateSO), false);
            using (new EditorGUI.DisabledScope(_templateToEdit == null))
            {
                if (GUILayout.Button("Inspector에서 열기"))
                {
                    Selection.activeObject = _templateToEdit;
                    EditorGUIUtility.PingObject(_templateToEdit);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawTemplateKindPreview(TemplateKind kind)
        {
            var preview = ScriptableObject.CreateInstance<ActorStatSO>();
            ApplyTemplate(preview, kind);

            EditorGUILayout.LabelField("미리보기", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginVertical("box");
            foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
            {
                if (!preview.TryGetExplicit(type, out float value)) continue;
                EditorGUILayout.LabelField($"  {type}", $"{value:0.##}");
            }
            EditorGUILayout.EndVertical();

            DestroyImmediate(preview);
        }

        private StatTemplateSO CreateTemplateAsset(TemplateKind kind, string assetName, string savePath)
        {
            EnsureFolder(savePath);

            var tmpl = ScriptableObject.CreateInstance<StatTemplateSO>();
            tmpl.description = $"{kind} 프리셋 기반 템플릿";

            // 임시 ActorStatSO에 프리셋을 적용한 뒤 명시된 항목만 템플릿으로 복사.
            var tmp = ScriptableObject.CreateInstance<ActorStatSO>();
            ApplyTemplate(tmp, kind);
            foreach (var entry in tmp.Entries)
                tmpl.EditorSet(entry.statType, entry.baseValue);
            DestroyImmediate(tmp);

            string safeName = string.IsNullOrEmpty(assetName) ? $"StatTemplate_{kind}" : assetName;
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{savePath}/{safeName}.asset");
            AssetDatabase.CreateAsset(tmpl, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StatDataGenerator] 템플릿 생성: {assetPath} ({kind})");
            return tmpl;
        }

        private void CreateDefaultPresetTemplates(string savePath)
        {
            var kinds = new[]
            {
                TemplateKind.WeakMonster,
                TemplateKind.NormalMonster,
                TemplateKind.EliteMonster,
                TemplateKind.Boss,
                TemplateKind.PlayerCharacter,
            };
            int count = 0;
            foreach (var kind in kinds)
            {
                CreateTemplateAsset(kind, $"StatTemplate_{kind}", savePath);
                count++;
            }
            EditorUtility.DisplayDialog("템플릿 생성", $"기본 프리셋 템플릿 {count}종을 생성했습니다.\n{savePath}", "확인");
        }

        // ──────────────────────────────────────────────────────────
        // 스탯 재생성 탭
        // ──────────────────────────────────────────────────────────

        private void DrawRegenerateTab()
        {
            DrawRegenerateToolbar();
            DrawRegenerateRows();
            DrawRegenerateFooter();
        }

        private void DrawRegenerateToolbar()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical("helpbox");

            _regenTemplate = (StatTemplateSO)EditorGUILayout.ObjectField(
                "적용할 템플릿", _regenTemplate, typeof(StatTemplateSO), false);

            if (_regenTemplate != null)
            {
                EditorGUILayout.BeginVertical("box");
                if (_regenTemplate.Entries.Count == 0)
                    EditorGUILayout.LabelField("  (정의된 스탯 없음)", EditorStyles.miniLabel);
                foreach (var entry in _regenTemplate.Entries)
                    EditorGUILayout.LabelField($"  {entry.statType}", $"{entry.baseValue:0.##}");
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginHorizontal();
            _regenOverwrite     = GUILayout.Toggle(_regenOverwrite, "기존 값 덮어쓰기", "Button", GUILayout.Width(130));
            _regenFillDefaults  = GUILayout.Toggle(_regenFillDefaults, "누락 StatType 기본값 채움", "Button", GUILayout.Width(180));
            _regenCreateMissing = GUILayout.Toggle(_regenCreateMissing, "statData 없으면 생성", "Button", GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                _regenOverwrite
                    ? "덮어쓰기 ON: 템플릿에 정의된 StatType은 기존 값을 무시하고 템플릿 값으로 교체합니다(수동 조정값 손실 주의)."
                    : "덮어쓰기 OFF: 대상에 없는(누락된) 항목만 템플릿 값으로 채웁니다. 기존 명시값은 보존됩니다.",
                MessageType.None);

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RefreshRegenRows();
            GUILayout.Label("검색", GUILayout.Width(34));
            _regenFilter = EditorGUILayout.TextField(_regenFilter, GUILayout.MinWidth(160));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"총 {_regenRows.Count}개", EditorStyles.toolbarButton, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            // 컬럼 헤더 + 전체 선택(현재 필터에 보이는 행 대상)
            var rect = GUILayoutUtility.GetRect(0, RowH, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, ColorHeader);
            bool newAll = GUI.Toggle(new Rect(rect.x + 4, rect.y + 3, ColCheck, rect.height), _regenAllSelected, GUIContent.none);
            if (newAll != _regenAllSelected)
            {
                _regenAllSelected = newAll;
                foreach (var row in _regenRows)
                    if (PassesRegenFilter(row)) row.Selected = _regenAllSelected;
            }
            GUI.Label(new Rect(rect.x + ColCheck + 4, rect.y, ColName, rect.height), "ActorDefinitionSO", EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + ColCheck + 4 + ColName, rect.y, ColCurrent, rect.height), "기존 statData", EditorStyles.boldLabel);
        }

        private bool PassesRegenFilter(RegenRow row)
        {
            if (string.IsNullOrEmpty(_regenFilter)) return true;
            return row.Definition != null &&
                   row.Definition.name.IndexOf(_regenFilter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawRegenerateRows()
        {
            _regenScroll = EditorGUILayout.BeginScrollView(_regenScroll);
            int visible = 0;
            for (int i = 0; i < _regenRows.Count; i++)
            {
                var row = _regenRows[i];
                if (!PassesRegenFilter(row)) continue;

                var rect = GUILayoutUtility.GetRect(0, RowH, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, visible % 2 == 0 ? ColorRowEven : ColorRowOdd);
                visible++;

                float x = rect.x;
                row.Selected = GUI.Toggle(new Rect(x + 4, rect.y + 3, ColCheck, rect.height), row.Selected, GUIContent.none);
                x += ColCheck;

                if (GUI.Button(new Rect(x + 2, rect.y, ColName - 4, rect.height), row.Definition.name, EditorStyles.label))
                    EditorGUIUtility.PingObject(row.Definition);
                x += ColName;

                var prev = GUI.color;
                if (row.Definition.statData != null)
                {
                    GUI.color = ColorOk;
                    GUI.Label(new Rect(x + 2, rect.y, ColCurrent, rect.height), $"✓ {row.Definition.statData.name}", EditorStyles.miniLabel);
                }
                else
                {
                    GUI.color = ColorMissing;
                    GUI.Label(new Rect(x + 2, rect.y, ColCurrent, rect.height), "(없음)", EditorStyles.miniLabel);
                }
                GUI.color = prev;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawRegenerateFooter()
        {
            EditorGUILayout.Space(4);

            int selectedCount = 0;
            foreach (var row in _regenRows)
                if (row.Selected && PassesRegenFilter(row)) selectedCount++;

            using (new EditorGUI.DisabledScope(_regenTemplate == null || selectedCount == 0))
            {
                var prevColor = GUI.backgroundColor;
                GUI.backgroundColor = ColorMissing;
                if (GUILayout.Button($"선택 액터 스탯 재생성 ({selectedCount}개)", GUILayout.Height(30)))
                {
                    RegenerateSelected();
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = prevColor;
            }

            if (_regenTemplate == null)
                EditorGUILayout.HelpBox("적용할 템플릿을 먼저 선택하세요.", MessageType.Warning);

            EditorGUILayout.HelpBox(
                "선택한 템플릿을 체크한 액터들의 ActorDefinitionSO.statData에 일괄 적용합니다.\n" +
                "신규 생성된 statData는 저장 경로(마이그레이션 탭의 '저장 경로')에 만들어져 정의에 연결됩니다.",
                MessageType.Info);
        }

        private void RefreshRegenRows()
        {
            _regenRows.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (def == null) continue;
                _regenRows.Add(new RegenRow { Definition = def, Selected = false });
            }
            _regenRows.Sort((a, b) => string.Compare(a.Definition.name, b.Definition.name, System.StringComparison.OrdinalIgnoreCase));
        }

        private void RegenerateSelected()
        {
            if (_regenTemplate == null) return;

            var targets = new List<RegenRow>();
            foreach (var row in _regenRows)
                if (row.Selected && PassesRegenFilter(row)) targets.Add(row);
            if (targets.Count == 0) return;

            string mode = _regenOverwrite ? "덮어쓰기" : "누락만 채움";
            bool ok = EditorUtility.DisplayDialog(
                "스탯 재생성",
                $"템플릿 [{_regenTemplate.name}]을(를) 선택한 {targets.Count}개 액터에 적용합니다.\n" +
                $"적용 방식: {mode}\n" +
                (_regenFillDefaults ? "+ 누락 StatType은 기본값으로 채움\n" : "") +
                (_regenCreateMissing ? "+ statData가 없으면 새로 생성해 연결\n" : "") +
                "\n계속할까요?",
                "재생성", "취소");
            if (!ok) return;

            EnsureFolder(_savePath);
            int applied = 0, created = 0, totalChanged = 0;

            foreach (var row in targets)
            {
                var def = row.Definition;
                var stat = def.statData;

                if (stat == null)
                {
                    if (!_regenCreateMissing) continue;
                    stat = ScriptableObject.CreateInstance<ActorStatSO>();
                    string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{_savePath}/{MakePlannedAssetName(def)}.asset");
                    AssetDatabase.CreateAsset(stat, assetPath);

                    var sObj = new SerializedObject(def);
                    sObj.FindProperty("statData").objectReferenceValue = stat;
                    sObj.ApplyModifiedProperties();
                    created++;
                }
                else
                {
                    Undo.RecordObject(stat, "Regenerate Actor Stat from Template");
                }

                int changed = _regenTemplate.EditorApplyTo(stat, _regenOverwrite);
                if (_regenFillDefaults) stat.EditorFillMissing();
                EditorUtility.SetDirty(stat);
                totalChanged += changed;
                applied++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StatDataGenerator] 재생성 완료: 적용 {applied}개 / 신규 생성 {created}개 / 변경된 StatType {totalChanged}개 (템플릿: {_regenTemplate.name}, {mode})");
            EditorUtility.DisplayDialog("스탯 재생성 완료",
                $"적용 {applied}개\n신규 statData 생성 {created}개\n변경된 StatType {totalChanged}개", "확인");

            RefreshMigrationRows();
        }

        // ── 템플릿 정의 ───────────────────────────────────────────
        private enum TemplateKind
        {
            Empty,
            WeakMonster,
            NormalMonster,
            EliteMonster,
            Boss,
            PlayerCharacter,
        }

        private static void ApplyTemplate(ActorStatSO so, TemplateKind kind)
        {
            switch (kind)
            {
                case TemplateKind.Empty:
                    so.EditorFillMissing();
                    break;

                case TemplateKind.WeakMonster:
                    so.EditorSet(StatType.MaxHealth,          216f);
                    so.EditorSet(StatType.AttackPower,        0.82f);
                    so.EditorSet(StatType.Defense,            0.01f);
                    so.EditorSet(StatType.MaxPoise,           55f);
                    so.EditorSet(StatType.PoiseRecoveryRate,  30f);
                    so.EditorSet(StatType.PoiseRecoveryDelay, 1.7f);
                    so.EditorSet(StatType.MoveSpeed,          1.0f);
                    break;

                case TemplateKind.NormalMonster:
                    so.EditorSet(StatType.MaxHealth,          540f);
                    so.EditorSet(StatType.AttackPower,        1.0f);
                    so.EditorSet(StatType.Defense,            0.0f);
                    so.EditorSet(StatType.MaxPoise,           100f);
                    so.EditorSet(StatType.PoiseRecoveryRate,  30f);
                    so.EditorSet(StatType.PoiseRecoveryDelay, 2.0f);
                    so.EditorSet(StatType.MoveSpeed,          1.0f);
                    break;

                case TemplateKind.EliteMonster:
                    so.EditorSet(StatType.MaxHealth,          1100f);
                    so.EditorSet(StatType.AttackPower,        1.3f);
                    so.EditorSet(StatType.Defense,            0.10f);
                    so.EditorSet(StatType.MaxPoise,           220f);
                    so.EditorSet(StatType.PoiseRecoveryRate,  25f);
                    so.EditorSet(StatType.PoiseRecoveryDelay, 2.5f);
                    so.EditorSet(StatType.MoveSpeed,          1.1f);
                    break;

                case TemplateKind.Boss:
                    so.EditorSet(StatType.MaxHealth,          4500f);
                    so.EditorSet(StatType.AttackPower,        1.5f);
                    so.EditorSet(StatType.Defense,            0.20f);
                    so.EditorSet(StatType.MaxPoise,           700f);
                    so.EditorSet(StatType.PoiseRecoveryRate,  20f);
                    so.EditorSet(StatType.PoiseRecoveryDelay, 3.0f);
                    so.EditorSet(StatType.MoveSpeed,          1.0f);
                    break;

                case TemplateKind.PlayerCharacter:
                    so.EditorSet(StatType.MaxHealth,          120f);
                    so.EditorSet(StatType.AttackPower,        1.0f);
                    so.EditorSet(StatType.Defense,            0.0f);
                    so.EditorSet(StatType.CritRate,           0.05f);
                    so.EditorSet(StatType.CritMultiplier,     1.5f);
                    so.EditorSet(StatType.MaxPoise,           100f);
                    so.EditorSet(StatType.PoiseRecoveryRate,  40f);
                    so.EditorSet(StatType.PoiseRecoveryDelay, 2.0f);
                    so.EditorSet(StatType.MoveSpeed,          1.0f);
                    so.EditorSet(StatType.DashDistance,       1.0f);
                    so.EditorSet(StatType.SkillGaugeRate,     1.0f);
                    so.EditorSet(StatType.InvincibleDuration, 1.0f);
                    break;
            }
        }

        private static void ApplyGradeTemplate(ActorStatSO so, MonsterActorGrade grade)
        {
            switch (grade)
            {
                case MonsterActorGrade.Weak:   ApplyTemplate(so, TemplateKind.WeakMonster);   break;
                case MonsterActorGrade.Normal: ApplyTemplate(so, TemplateKind.NormalMonster); break;
                case MonsterActorGrade.Elite:  ApplyTemplate(so, TemplateKind.EliteMonster);  break;
                case MonsterActorGrade.Boss:   ApplyTemplate(so, TemplateKind.Boss);          break;
            }
        }

        // ──────────────────────────────────────────────────────────
        // 공통 유틸
        // ──────────────────────────────────────────────────────────

        public static void ValidateStatDataCoverageMenu()
            => ValidateStatDataCoverage(showDialog: true);

        private static bool ValidateStatDataCoverage(bool showDialog)
        {
            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            var sb = new StringBuilder();
            int missingStatData = 0;
            int missingEntries = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (def == null) continue;

                if (def.statData == null)
                {
                    missingStatData++;
                    sb.AppendLine($"[statData 없음] {def.name} ({path})");
                    continue;
                }

                foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
                {
                    if (def.statData.TryGetExplicit(type, out _)) continue;
                    missingEntries++;
                    sb.AppendLine($"[StatType 누락] {def.name} → {def.statData.name}.{type}");
                }
            }

            bool ok = missingStatData == 0 && missingEntries == 0;
            if (ok)
            {
                const string message = "[StatDataGenerator] 모든 ActorDefinitionSO에 statData가 있고 모든 StatType이 명시되어 있습니다.";
                Debug.Log(message);
                if (showDialog)
                    EditorUtility.DisplayDialog("Stat Data 검증", "검증 완료: 누락된 statData/StatType이 없습니다.", "확인");
                return true;
            }

            string report = $"[StatDataGenerator] 검증 실패: statData 없음 {missingStatData}개 / StatType 누락 {missingEntries}개\n{sb}";
            Debug.LogError(report);
            if (showDialog)
                EditorUtility.DisplayDialog("Stat Data 검증 실패", report, "확인");
            return false;
        }

        private static bool HasMissingStats(ActorStatSO stat)
        {
            if (stat == null) return true;

            foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
            {
                if (!stat.TryGetExplicit(type, out _))
                    return true;
            }

            return false;
        }

        private static string MakePlannedAssetName(ActorDefinitionSO def)
        {
            string rawName = def != null && !string.IsNullOrEmpty(def.actorId) ? def.actorId : def != null ? def.name : "Unknown";
            string assetName = $"ActorStat_{rawName}";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                assetName = assetName.Replace(invalid, '_');

            return assetName.Replace('/', '_').Replace('\\', '_');
        }

        private static string MakeBreakGaugeAssetName(ActorDefinitionSO def)
        {
            string rawName = def != null && !string.IsNullOrEmpty(def.actorId) ? def.actorId : def != null ? def.name : "Unknown";
            string assetName = $"MonsterBreakGauge_{rawName}";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                assetName = assetName.Replace(invalid, '_');

            return assetName.Replace('/', '_').Replace('\\', '_');
        }

        private void BrowseSavePath(ref string targetPath)
        {
            string abs = EditorUtility.OpenFolderPanel("저장 경로 선택", targetPath, "");
            if (string.IsNullOrEmpty(abs)) return;

            string projectRoot = Application.dataPath.Replace("/Assets", "");
            if (abs.StartsWith(projectRoot))
                targetPath = "Assets" + abs.Substring(projectRoot.Length + "/Assets".Length).Replace("\\", "/");
            else
                EditorUtility.DisplayDialog("경고", "프로젝트 폴더 내부 경로를 선택해야 합니다.", "확인");
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // ── 전투 정책 탭 ──────────────────────────────────────────
        private void DrawCombatPolicyTab()
        {
            DrawCombatPolicyToolbar();
            DrawCombatPolicyColumnHeader();
            DrawCombatPolicyRows();
            DrawCombatPolicyFooter();
        }

        private void DrawCombatPolicyToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70)))
                RefreshPolicyRows();

            if (GUILayout.Button("누락만 자동연결", EditorStyles.toolbarButton, GUILayout.Width(110)))
                AutoLinkMissingPolicies(onlySelected: false);

            if (GUILayout.Button("기본 정책 에셋 생성", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                CombatPolicyAssetGenerator.GenerateDefaultPolicyAssets();
                RefreshPolicyRows();
                GUIUtility.ExitGUI();
            }

            GUILayout.Space(6);

            bool prevRelevant = _policyOnlyRelevant;
            _policyOnlyRelevant = GUILayout.Toggle(_policyOnlyRelevant, "관련 항목만", EditorStyles.toolbarButton, GUILayout.Width(90));
            if (prevRelevant != _policyOnlyRelevant) RefreshPolicyRows();

            GUILayout.FlexibleSpace();

            int missing = _policyRows.FindAll(r => PolicyMissing(r)).Count;
            if (missing > 0)
            {
                var prev = GUI.color;
                GUI.color = ColorMissing;
                GUILayout.Label($"누락 {missing}개", EditorStyles.toolbarButton, GUILayout.Width(70));
                GUI.color = prev;
            }
            GUILayout.Label($"총 {_policyRows.Count}개", EditorStyles.toolbarButton, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCombatPolicyColumnHeader()
        {
            var rect = GUILayoutUtility.GetRect(0, RowH, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, ColorHeader);

            float x = rect.x;
            bool newAll = GUI.Toggle(new Rect(x + 4, rect.y + 3, ColCheck, rect.height), _policyAllSelected, GUIContent.none);
            if (newAll != _policyAllSelected)
            {
                _policyAllSelected = newAll;
                foreach (var row in _policyRows) row.Selected = _policyAllSelected;
            }
            x += ColCheck;

            DrawHeaderCell("ActorDefinitionSO", ref x, ColName, rect.y, rect.height);
            DrawHeaderCell("등급", ref x, ColGrade, rect.y, rect.height);
            DrawHeaderCell("DefensePolicy (플레이어블)", ref x, ColPolicy, rect.y, rect.height);
            DrawHeaderCell("ReactionPolicy (Elite/Boss)", ref x, ColPolicy, rect.y, rect.height);
        }

        private void DrawCombatPolicyRows()
        {
            _policyScroll = EditorGUILayout.BeginScrollView(_policyScroll);

            for (int i = 0; i < _policyRows.Count; i++)
            {
                var row = _policyRows[i];
                if (row.Definition == null) continue;

                var rect = GUILayoutUtility.GetRect(0, RowH, GUILayout.ExpandWidth(true));
                EditorGUI.DrawRect(rect, i % 2 == 0 ? ColorRowEven : ColorRowOdd);

                float x = rect.x;

                row.Selected = GUI.Toggle(new Rect(x + 4, rect.y + 3, ColCheck, rect.height), row.Selected, GUIContent.none);
                x += ColCheck;

                if (GUI.Button(new Rect(x + 2, rect.y, ColName - 4, rect.height), row.Definition.name, EditorStyles.label))
                    EditorGUIUtility.PingObject(row.Definition);
                x += ColName;

                GUI.Label(new Rect(x + 4, rect.y, ColGrade, rect.height), row.Grade.ToString(), EditorStyles.miniLabel);
                x += ColGrade;

                // Defense (플레이어블 캐릭터 대상)
                row.Defense = (CombatDefensePolicySO)DrawPolicyCell(
                    new Rect(x, rect.y, ColPolicy, rect.height),
                    row.Defense, typeof(CombatDefensePolicySO),
                    applicable: row.IsPlayable, isSet: row.Defense != null);
                if (row.Defense != row.Definition.combatDefensePolicy)
                    AssignPolicy(row, defense: row.Defense, reaction: row.Definition.combatReactionPolicy);
                x += ColPolicy;

                // Reaction (Elite/Boss 몬스터 대상)
                row.Reaction = (CombatReactionPolicySO)DrawPolicyCell(
                    new Rect(x, rect.y, ColPolicy, rect.height),
                    row.Reaction, typeof(CombatReactionPolicySO),
                    applicable: row.IsMonster && (row.Grade == MonsterActorGrade.Elite || row.Grade == MonsterActorGrade.Boss),
                    isSet: row.Reaction != null);
                if (row.Reaction != row.Definition.combatReactionPolicy)
                    AssignPolicy(row, defense: row.Definition.combatDefensePolicy, reaction: row.Reaction);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>상태 글리프(✓/⚠/—) + ObjectField를 한 셀에 그린다. 변경된 객체를 반환한다.</summary>
        private UnityEngine.Object DrawPolicyCell(Rect cell, UnityEngine.Object current, Type type, bool applicable, bool isSet)
        {
            var prev = GUI.color;
            GUI.color = !applicable ? new Color(0.5f, 0.5f, 0.5f) : (isSet ? ColorOk : ColorMissing);
            string glyph = !applicable ? "—" : (isSet ? "✓" : "⚠");
            GUI.Label(new Rect(cell.x + 2, cell.y, 16, cell.height), glyph, EditorStyles.miniLabel);
            GUI.color = prev;

            return EditorGUI.ObjectField(
                new Rect(cell.x + 18, cell.y + 1, cell.width - 22, cell.height - 2),
                current, type, false);
        }

        private void DrawCombatPolicyFooter()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            int selected = _policyRows.FindAll(r => r.Selected).Count;
            using (new EditorGUI.DisabledScope(selected == 0))
            {
                if (GUILayout.Button($"선택 항목 자동연결 ({selected}개)", GUILayout.Height(26)))
                    AutoLinkMissingPolicies(onlySelected: true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "DefensePolicy는 플레이어블 캐릭터(characterType≠None), ReactionPolicy는 Elite/Boss 몬스터에 적용됩니다(✓=연결, ⚠=누락, —=해당없음).\n" +
                "'기본 정책 에셋 생성' → '누락만 자동연결' 순으로 채우거나, 각 행에서 직접 지정/해제할 수 있습니다. 정책이 null이면 런타임은 기존 기본 동작을 유지합니다.\n" +
                "주의: 플레이어는 씬에 배치된 PlayerActor의 _definition(고정·스왑 무관) 하나에서만 DefensePolicy를 읽습니다. " +
                "그 정의에 정책이 연결되어 있어야 실제로 적용됩니다(영입 캐릭터별로 분기되지 않음).",
                MessageType.Info);
        }

        private bool PolicyMissing(PolicyRow row)
        {
            if (row.Definition == null) return false;
            bool defMissing = row.IsPlayable && row.Defense == null;
            bool reactMissing = row.IsMonster
                                && (row.Grade == MonsterActorGrade.Elite || row.Grade == MonsterActorGrade.Boss)
                                && row.Reaction == null;
            return defMissing || reactMissing;
        }

        private void RefreshPolicyRows()
        {
            _policyRows.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (def == null) continue;

                bool isPlayer = (def.actorType & ActorType.Player) == ActorType.Player;
                bool isMonster = (def.actorType & ActorType.Monster) == ActorType.Monster;
                bool isPlayable = isPlayer || def.characterType != CharacterActorType.None;

                // Defense는 플레이어블 캐릭터, Reaction은 Elite/Boss 몬스터에 적용된다.
                bool reactionRelevant = isMonster && (def.grade == MonsterActorGrade.Elite || def.grade == MonsterActorGrade.Boss);
                bool relevant = isPlayable || reactionRelevant;
                if (_policyOnlyRelevant && !relevant) continue;

                _policyRows.Add(new PolicyRow
                {
                    Definition = def,
                    Grade = def.grade,
                    IsPlayer = isPlayer,
                    IsMonster = isMonster,
                    IsPlayable = isPlayable,
                    Defense = def.combatDefensePolicy,
                    Reaction = def.combatReactionPolicy,
                });
            }
            _policyRows.Sort((a, b) => string.Compare(a.Definition.name, b.Definition.name, StringComparison.Ordinal));
            _policyAllSelected = false;
        }

        private void AssignPolicy(PolicyRow row, CombatDefensePolicySO defense, CombatReactionPolicySO reaction)
        {
            if (row.Definition == null) return;

            Undo.RecordObject(row.Definition, "Assign Combat Policy");
            row.Definition.combatDefensePolicy = defense;
            row.Definition.combatReactionPolicy = reaction;
            row.Defense = defense;
            row.Reaction = reaction;
            EditorUtility.SetDirty(row.Definition);
            AssetDatabase.SaveAssetIfDirty(row.Definition);
        }

        private void AutoLinkMissingPolicies(bool onlySelected)
        {
            if (!CombatPolicyAssetGenerator.TryLoadDefaultPolicies(
                    out CombatDefensePolicySO defense,
                    out CombatReactionPolicySO eliteReaction,
                    out CombatReactionPolicySO bossReaction))
            {
                EditorUtility.DisplayDialog(
                    "전투 정책",
                    "기본 정책 에셋이 없습니다. 먼저 '기본 정책 에셋 생성'을 실행하세요.",
                    "확인");
                return;
            }

            int linked = 0;
            foreach (var row in _policyRows)
            {
                if (onlySelected && !row.Selected) continue;
                if (row.Definition == null) continue;

                CombatDefensePolicySO newDefense = row.Definition.combatDefensePolicy;
                CombatReactionPolicySO newReaction = row.Definition.combatReactionPolicy;
                bool changed = false;

                if (row.IsPlayable && newDefense == null && defense != null)
                {
                    newDefense = defense;
                    changed = true;
                }

                if (row.IsMonster && newReaction == null)
                {
                    CombatReactionPolicySO graded =
                        CombatPolicyAssetGenerator.ResolveReactionPolicyForGrade(row.Grade, eliteReaction, bossReaction);
                    if (graded != null)
                    {
                        newReaction = graded;
                        changed = true;
                    }
                }

                if (!changed) continue;
                AssignPolicy(row, newDefense, newReaction);
                linked++;
            }

            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent(linked > 0 ? $"{linked}개 연결 완료" : "연결할 누락 항목 없음"));
        }

        // ── 내부 데이터 ───────────────────────────────────────────
        private class MigrationRow
        {
            public ActorDefinitionSO Definition;
            public PoiseSO           SourcePoise;
            public MonsterBreakGaugeSO SourceBreakGauge;
            public ActorStatSO       ExistingStat;
            public string            PlannedAssetName;
            public bool              Selected;
        }

        private class RegenRow
        {
            public ActorDefinitionSO Definition;
            public bool              Selected;
        }

        private class PolicyRow
        {
            public ActorDefinitionSO       Definition;
            public MonsterActorGrade       Grade;
            public bool                    IsPlayer;
            public bool                    IsMonster;
            public bool                    IsPlayable;   // characterType != None 또는 Player 플래그 — DefensePolicy 대상
            public CombatDefensePolicySO   Defense;
            public CombatReactionPolicySO  Reaction;
            public bool                    Selected;
        }
    }
}
#endif
