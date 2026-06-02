#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Stat
{
    /// <summary>
    /// ActorStatSO 자동 생성/마이그레이션 툴.
    /// 메뉴: UPlayGround/Stat/Stat Data Generator
    /// </summary>
    public class StatDataGeneratorWindow : EditorWindow
    {
        // ── 탭 ──────────────────────────────────────────────────────
        private enum Tab { Migration, PlayerCharacter, Template }
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
        private string _templateName = "ActorStat_New";
        private int _templateCount = 1;
        private string _templateSavePath = DefaultSavePath;

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
        private const float ColCheck = 22f;
        private const float ColName  = 200f;
        private const float ColPoise = 90f;
        private const float ColBreak = 110f;
        private const float ColCurrent = 130f;
        private const float ColPlanned = 200f;
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
        }

        private void OnGUI()
        {
            DrawTabs();
            switch (_currentTab)
            {
                case Tab.Migration:       DrawMigrationTab();       break;
                case Tab.PlayerCharacter: DrawPlayerCharacterTab(); break;
                case Tab.Template:        DrawTemplateTab();        break;
            }
        }

        // ── 탭 헤더 ───────────────────────────────────────────────
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawTabButton("Definition 마이그레이션", Tab.Migration);
            DrawTabButton("Player 기본 스탯", Tab.PlayerCharacter);
            DrawTabButton("템플릿 생성", Tab.Template);
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
                        ? "교체된 기존 에셋은 다른 정의가 참조하지 않으면 삭제됩니다.\n"
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
                _deleteReplacedStats, "교체된 기존 에셋 삭제", GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "전체 보정은 statData가 없는 ActorDefinitionSO에 ActorStatSO를 생성해 연결하고, 기존 statData의 누락 StatType을 채웁니다.\n" +
                "등급별 템플릿으로 기본값을 채우고, PoiseSO 값 → MaxPoise/Recovery* 로 초기화됩니다.\n" +
                "몬스터의 breakGaugeData가 비어 있으면 BreakGaugeSO를 생성해 연결합니다.\n" +
                "'재마이그레이션'은 이미 등록된 statData도 현재 등급 템플릿으로 다시 발급해 덮어씁니다. ('누락 항목만' 토글을 꺼야 기존 항목이 보입니다.)\n" +
                "'교체된 기존 에셋 삭제'가 켜져 있으면, 더 이상 어떤 정의도 참조하지 않게 된 기존 statData 에셋을 자동 정리합니다(공유 에셋은 보존).",
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
            ActorStatSO so = row.Definition.statData;
            string assetPath = null;

            // Definition에 자동 연결
            var sObj = new SerializedObject(row.Definition);
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

            row.ExistingStat = so;
            RefreshMigrationRows();
        }

        private void GenerateSelected(bool forceRegenerate = false)
        {
            EnsureFolder(_savePath);
            EnsureFolder(BreakGaugeSavePath);
            int created = 0;
            int regenerated = 0;
            int breakCount = 0;
            var replacedPaths = new List<string>();
            foreach (var row in _migrationRows)
            {
                if (!row.Selected) continue;
                ActorStatSO so = row.Definition.statData;

                var sObj = new SerializedObject(row.Definition);
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

            int deleted = _deleteReplacedStats ? CleanupUnreferencedStats(replacedPaths) : 0;

            Debug.Log($"[StatDataGenerator] ActorStatSO 신규 {created}개 / 재마이그레이션 {regenerated}개 / 기존 에셋 정리 {deleted}개 / BreakGaugeSO {breakCount}개 완료");
            RefreshMigrationRows();
            ValidateStatDataCoverage(showDialog: false);
        }

        /// <summary>
        /// 교체로 더 이상 어떤 에셋도 직렬화 참조하지 않게 된 ActorStatSO 에셋을 삭제한다.
        /// 다른 에셋(정의/스케일링/성장)이 여전히 참조 중인 공유 에셋은 보존한다.
        /// </summary>
        private static int CleanupUnreferencedStats(List<string> candidatePaths)
        {
            if (candidatePaths == null || candidatePaths.Count == 0)
                return 0;

            var referencedGuids = CollectReferencedStatGuids();
            int deleted = 0;
            var seen = new HashSet<string>();
            foreach (var path in candidatePaths)
            {
                if (string.IsNullOrEmpty(path) || !seen.Add(path))
                    continue;

                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid) || referencedGuids.Contains(guid))
                    continue; // 다른 정의가 여전히 참조 → 공유 에셋이므로 보존

                if (AssetDatabase.DeleteAsset(path))
                {
                    deleted++;
                    Debug.Log($"[StatDataGenerator] 미참조 기존 statData 삭제: {path}");
                }
            }

            if (deleted > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return deleted;
        }

        /// <summary>
        /// ActorStatSO를 직렬화 참조하는 모든 에셋(ActorDefinitionSO.statData,
        /// MonsterScalingSO.baseStat, PartyMemberGrowthSO.baseStat)이 현재 가리키는 GUID 집합.
        /// 새로운 ActorStatSO 참조 타입을 추가하면 여기도 함께 갱신할 것.
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

            foreach (var guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
                Add(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid))?.statData);

            foreach (var guid in AssetDatabase.FindAssets("t:MonsterScalingSO"))
                Add(AssetDatabase.LoadAssetAtPath<MonsterScalingSO>(AssetDatabase.GUIDToAssetPath(guid))?.baseStat);

            foreach (var guid in AssetDatabase.FindAssets("t:PartyMemberGrowthSO"))
                Add(AssetDatabase.LoadAssetAtPath<PartyMemberGrowthSO>(AssetDatabase.GUIDToAssetPath(guid))?.baseStat);

            return set;
        }

        private void GenerateAllMissing()
        {
            EnsureFolder(_savePath);
            EnsureFolder(BreakGaugeSavePath);
            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            int count = 0;
            int breakCount = 0;
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
            Debug.Log($"[StatDataGenerator] 누락분 일괄 생성 완료: ActorStatSO {count}개 / BreakGaugeSO {breakCount}개");
            RefreshMigrationRows();
            ValidateStatDataCoverage(showDialog: false);
        }

        private void RepairAllDefinitions()
        {
            EnsureFolder(_savePath);
            EnsureFolder(BreakGaugeSavePath);

            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            int created = 0;
            int filled = 0;
            int breakCreated = 0;

            foreach (var guid in guids)
            {
                var def = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (def == null) continue;

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
            Debug.Log($"[StatDataGenerator] 전체 보정 완료: statData 생성 {created}개 / 누락 StatType 채움 {filled}개 / breakGaugeData 생성 {breakCreated}개");
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

            // 정의에 작성된 등급에 따라 기본 템플릿 적용
            ApplyGradeTemplate(so, row.Definition != null ? row.Definition.grade : MonsterActorGrade.Normal);

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
            => def != null && (def.statData == null || NeedsBreakGauge(def));

        private static bool NeedsBreakGauge(ActorDefinitionSO def)
            => IsMonster(def) && def.breakGaugeData == null;

        private static bool IsMonster(ActorDefinitionSO def)
            => def != null && (def.actorType & ActorType.Monster) != 0;

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
                "그 외 캐릭터는 기본 PlayerCharacter 프리셋이 적용됩니다.",
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
            int created = 0, overwritten = 0;

            foreach (var kv in _playerSelected)
            {
                if (!kv.Value) continue;
                var type = kv.Key;
                string assetName = $"{_playerNamePrefix}{type}";
                _playerExisting.TryGetValue(type, out var existing);

                if (existing != null && _playerOverwrite)
                {
                    // 기존 SO에 덮어쓰기 (모든 명시값 제거 후 프리셋 재적용)
                    Undo.RecordObject(existing, "Overwrite Player Stat");
                    foreach (StatType st in System.Enum.GetValues(typeof(StatType)))
                        existing.EditorRemove(st);
                    ApplyCharacterPreset(existing, type);
                    EditorUtility.SetDirty(existing);
                    overwritten++;
                }
                else
                {
                    var so = ScriptableObject.CreateInstance<ActorStatSO>();
                    ApplyCharacterPreset(so, type);
                    string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{_playerSavePath}/{assetName}.asset");
                    AssetDatabase.CreateAsset(so, assetPath);
                    created++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StatDataGenerator] Player 스탯 생성: 신규 {created}개 / 덮어쓰기 {overwritten}개");
            RefreshPlayerExisting();
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
            EditorGUILayout.LabelField("템플릿 기반 ActorStatSO 일괄 생성", EditorStyles.boldLabel);
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical("helpbox");
            _templateKind  = (TemplateKind)EditorGUILayout.EnumPopup("템플릿 종류", _templateKind);
            _templateName  = EditorGUILayout.TextField("자산 이름 (접두)", _templateName);
            _templateCount = Mathf.Max(1, EditorGUILayout.IntField("생성 개수", _templateCount));

            EditorGUILayout.BeginHorizontal();
            _templateSavePath = EditorGUILayout.TextField("저장 경로", _templateSavePath);
            if (GUILayout.Button("...", GUILayout.Width(28)))
                BrowseSavePath(ref _templateSavePath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            DrawTemplatePreview();

            EditorGUILayout.Space(8);
            if (GUILayout.Button($"{_templateCount}개 생성", GUILayout.Height(32)))
                GenerateFromTemplate();
        }

        private void DrawTemplatePreview()
        {
            EditorGUILayout.LabelField("미리보기 (스탯 값)", EditorStyles.boldLabel);
            var preview = ScriptableObject.CreateInstance<ActorStatSO>();
            ApplyTemplate(preview, _templateKind);

            EditorGUILayout.BeginVertical("helpbox");
            foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
            {
                bool isExplicit = preview.TryGetExplicit(type, out float value);
                if (!isExplicit) continue;
                EditorGUILayout.LabelField($"  {type}", $"{value:0.##}");
            }
            EditorGUILayout.EndVertical();

            DestroyImmediate(preview);
        }

        private void GenerateFromTemplate()
        {
            EnsureFolder(_templateSavePath);
            for (int i = 0; i < _templateCount; i++)
            {
                var so = ScriptableObject.CreateInstance<ActorStatSO>();
                ApplyTemplate(so, _templateKind);

                string baseName = _templateCount == 1 ? _templateName : $"{_templateName}_{i + 1:D2}";
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{_templateSavePath}/{baseName}.asset");
                AssetDatabase.CreateAsset(so, assetPath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[StatDataGenerator] 템플릿 [{_templateKind}] 기반 {_templateCount}개 생성 완료");
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
                    so.EditorSet(StatType.MaxHealth,          50f);
                    so.EditorSet(StatType.AttackPower,        0.8f);
                    so.EditorSet(StatType.Defense,            0.0f);
                    so.EditorSet(StatType.MaxPoise,           30f);
                    so.EditorSet(StatType.PoiseRecoveryRate,  30f);
                    so.EditorSet(StatType.PoiseRecoveryDelay, 1.5f);
                    so.EditorSet(StatType.MoveSpeed,          1.0f);
                    break;

                case TemplateKind.NormalMonster:
                    so.EditorSet(StatType.MaxHealth,          80f);
                    so.EditorSet(StatType.AttackPower,        1.0f);
                    so.EditorSet(StatType.Defense,            0.0f);
                    so.EditorSet(StatType.MaxPoise,           50f);
                    so.EditorSet(StatType.PoiseRecoveryRate,  30f);
                    so.EditorSet(StatType.PoiseRecoveryDelay, 2.0f);
                    so.EditorSet(StatType.MoveSpeed,          1.0f);
                    break;

                case TemplateKind.EliteMonster:
                    so.EditorSet(StatType.MaxHealth,          150f);
                    so.EditorSet(StatType.AttackPower,        1.3f);
                    so.EditorSet(StatType.Defense,            0.10f);
                    so.EditorSet(StatType.MaxPoise,           120f);
                    so.EditorSet(StatType.PoiseRecoveryRate,  25f);
                    so.EditorSet(StatType.PoiseRecoveryDelay, 2.5f);
                    so.EditorSet(StatType.MoveSpeed,          1.1f);
                    break;

                case TemplateKind.Boss:
                    so.EditorSet(StatType.MaxHealth,          600f);
                    so.EditorSet(StatType.AttackPower,        1.5f);
                    so.EditorSet(StatType.Defense,            0.20f);
                    so.EditorSet(StatType.MaxPoise,           250f);
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
    }
}
#endif
