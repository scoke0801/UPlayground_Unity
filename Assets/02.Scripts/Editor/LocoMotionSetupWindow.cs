using System.Collections.Generic;
using System.IO;
using System.Linq;
using AYellowpaper.SerializedCollections;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// Stop / TurnInPlace FBX 클립을 폴더에서 자동으로 탐색하여
    /// MotionSetAsset을 일괄 생성하고 ActorAnimationMotionSet에 등록하는 에디터 창.
    /// </summary>
    public class LocoMotionSetupWindow : EditorWindow
    {
        // ── 파일명 패턴 → Motion Slot 매핑 ──────────────────────────────────────────
        // _InPlace 접미사는 스캔 시 자동으로 제거한 뒤 비교한다.
        private static readonly Dictionary<string, GameplayTag> PatternToKey = new()
        {
            // ── Base — Walk Slow (8방향) ──────────────────────────────────────
            { "Walk_Slow_F",        MotionTags.Walk_Slow       },
            { "Walk_Slow_B",        MotionTags.Walk_Slow_B     },
            { "Walk_Slow_B_L45",    MotionTags.Walk_Slow_B_L45 },
            { "Walk_Slow_B_R45",    MotionTags.Walk_Slow_B_R45 },
            { "Walk_Slow_F_L45",    MotionTags.Walk_Slow_F_L45 },
            { "Walk_Slow_F_R45",    MotionTags.Walk_Slow_F_R45 },
            { "Walk_Slow_F_L90_A",  MotionTags.Walk_Slow_F_L90 },  // A 버전 우선
            { "Walk_Slow_F_R90_A",  MotionTags.Walk_Slow_F_R90 },
            // ── Base — Walk (8방향) ───────────────────────────────────────────
            { "Walk_F",             MotionTags.Walk            },
            { "Walk_B",             MotionTags.Walk_B          },
            { "Walk_B_L45",         MotionTags.Walk_B_L45      },
            { "Walk_B_R45",         MotionTags.Walk_B_R45      },
            { "Walk_F_L45",         MotionTags.Walk_F_L45      },
            { "Walk_F_R45",         MotionTags.Walk_F_R45      },
            { "Walk_F_L90_A",       MotionTags.Walk_F_L90      },
            { "Walk_F_R90_A",       MotionTags.Walk_F_R90      },
            // ── Base — Run (8방향) ────────────────────────────────────────────
            { "Run_F",              MotionTags.Run             },
            { "Run_B",              MotionTags.Run_B           },
            { "Run_B_L45",          MotionTags.Run_B_L45       },
            { "Run_B_R45",          MotionTags.Run_B_R45       },
            { "Run_F_L45",          MotionTags.Run_F_L45       },
            { "Run_F_R45",          MotionTags.Run_F_R45       },
            { "Run_F_L90_A",        MotionTags.Run_F_L90       },
            { "Run_F_R90_A",        MotionTags.Run_F_R90       },
            // ── Stop — Run ────────────────────────────────────────────────────
            { "Run_F_To_Idle",     MotionTags.Move_Stop_Running     },
            { "Run_F_L45_To_Idle", MotionTags.Move_Stop_Running_L45 },
            { "Run_F_R45_To_Idle", MotionTags.Move_Stop_Running_R45 },
            // ── Stop — Walk ───────────────────────────────────────────────────
            { "Walk_F_To_Idle",     MotionTags.Move_Stop_Walking     },
            { "Walk_F_L45_To_Idle", MotionTags.Move_Stop_Walking_L45 },
            { "Walk_F_R45_To_Idle", MotionTags.Move_Stop_Walking_R45 },
            // ── Stop — Sprint ─────────────────────────────────────────────────
            { "Sprint_F_To_Idle",     MotionTags.Move_Stop_Sprinting     },
            { "Sprint_F_L45_To_Idle", MotionTags.Move_Stop_Sprinting_L45 },
            { "Sprint_F_R45_To_Idle", MotionTags.Move_Stop_Sprinting_R45 },
            // ── TurnInPlace — Stand Idle ──────────────────────────────────────
            { "Stand_Idle_Turn_L45", MotionTags.Stand_Idle_Turn_L45 },
            { "Stand_Idle_Turn_R45", MotionTags.Stand_Idle_Turn_R45 },
            { "Stand_Idle_Turn_L90", MotionTags.Stand_Idle_Turn_L90 },
            { "Stand_Idle_Turn_R90", MotionTags.Stand_Idle_Turn_R90 },
            { "Stand_Idle_Turn_180", MotionTags.Stand_Idle_Turn_180 },
            // ── Turn — Run (이동 중 방향 전환) ────────────────────────────────
            { "Run_F_Turn_L45", MotionTags.Run_Turn_L45 },
            { "Run_F_Turn_R45", MotionTags.Run_Turn_R45 },
            { "Run_F_Turn_L90", MotionTags.Run_Turn_L90 },
            { "Run_F_Turn_R90", MotionTags.Run_Turn_R90 },
            { "Run_F_Turn_180", MotionTags.Run_Turn_180 },
            // ── Turn — Walk ───────────────────────────────────────────────────
            { "Walk_F_Turn_L45", MotionTags.Walk_Turn_L45 },
            { "Walk_F_Turn_R45", MotionTags.Walk_Turn_R45 },
            { "Walk_F_Turn_L90", MotionTags.Walk_Turn_L90 },
            { "Walk_F_Turn_R90", MotionTags.Walk_Turn_R90 },
            { "Walk_F_Turn_180", MotionTags.Walk_Turn_180 },
            // ── Turn — Sprint ─────────────────────────────────────────────────
            { "Sprint_F_Turn_L45", MotionTags.Sprint_Turn_L45 },
            { "Sprint_F_Turn_R45", MotionTags.Sprint_Turn_R45 },
            { "Sprint_F_Turn_L90", MotionTags.Sprint_Turn_L90 },
            { "Sprint_F_Turn_R90", MotionTags.Sprint_Turn_R90 },
            { "Sprint_F_Turn_180", MotionTags.Sprint_Turn_180 },
        };

        // ── 내부 데이터 ─────────────────────────────────────────────────────────
        private class MappingEntry
        {
            public GameplayTag    MotionSlot;
            public string         FbxPath;       // Assets/ 상대경로
            public bool           IsInPlace;
            public AnimationClip  Clip;
            public bool           Selected  = true;
            public bool           Exists;         // 대상 SO에 이미 등록됐는지
        }

        // ── 상태 ────────────────────────────────────────────────────────────────
        private string                        _scanFolder    = "";
        // 직접 지정 모드
        private ActorAnimationMotionSet       _targetSO;
        // PlayerActor 모드 — WeaponType.NoWeapon 아래 SO를 자동 추출
        private PlayerActorAnimationMotionSet _playerActorSO;
        private WeaponType                    _weaponType    = WeaponType.NoWeapon;

        private string _outputFolder  = "Assets/10.Datas/Motion/Locomotion";
        private string _filePrefix    = "MS_";
        private bool   _preferInPlace = true;
        private bool   _overwrite     = false;

        private List<MappingEntry> _entries  = new();
        private Vector2            _scroll;
        private bool               _scanned;
        private string             _statusMsg = "";

        // ── 스타일 캐시 ─────────────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _rowEvenStyle;
        private GUIStyle _rowOddStyle;

        // ────────────────────────────────────────────────────────────────────────

        public static void Open()
        {
            var w = GetWindow<LocoMotionSetupWindow>("Locomotion Setup");
            w.minSize = new Vector2(820, 520);
            w.Show();
        }

        private void OnEnable() => _scanned = false;

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.Space(8);
            DrawHeader();
            EditorGUILayout.Space(4);
            DrawSettings();
            EditorGUILayout.Space(6);
            DrawScanButton();

            if (_scanned)
            {
                EditorGUILayout.Space(6);
                DrawResultTable();
                EditorGUILayout.Space(4);
                DrawGenerateButton();
            }

            if (!string.IsNullOrEmpty(_statusMsg))
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(_statusMsg, MessageType.Info);
            }
        }

        // ── UI 그리기 ──────────────────────────────────────────────────────────

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Locomotion Motion Setup", _headerStyle);
            EditorGUILayout.LabelField(
                "지정 폴더를 재귀 탐색하여 Stop / TurnInPlace 클립을 찾고, MotionSetAsset을 자동 생성합니다.",
                EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawSettings()
        {
            using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

            EditorGUILayout.LabelField("설정", EditorStyles.boldLabel);

            // 스캔 폴더
            using (new EditorGUILayout.HorizontalScope())
            {
                _scanFolder = EditorGUILayout.TextField("스캔 폴더", _scanFolder);
                if (GUILayout.Button("…", GUILayout.Width(26)))
                {
                    string picked = EditorUtility.OpenFolderPanel("스캔할 폴더 선택", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(picked))
                        _scanFolder = ToProjectRelative(picked);
                }
            }

            // ── 타겟 모드 ─────────────────────────────────────────────────────
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("등록 대상", EditorStyles.boldLabel);

            // PlayerActorAnimationMotionSet 모드 (권장 — HasMotion과 동일 경로)
            _playerActorSO = (PlayerActorAnimationMotionSet)EditorGUILayout.ObjectField(
                new GUIContent("PlayerActor MotionSet", "PlayerActorAnimationMotionSet을 드래그하면 WeaponType에 맞는 ActorAnimationMotionSet을 자동으로 찾습니다."),
                _playerActorSO, typeof(PlayerActorAnimationMotionSet), false);

            if (_playerActorSO != null)
            {
                _weaponType = (WeaponType)EditorGUILayout.EnumPopup(
                    new GUIContent("Weapon Type", "NoWeapon = 공통 로코모션 (HasMotion이 체크하는 경로)"),
                    _weaponType);

                var resolved = _playerActorSO.GetActorAnimationMotionSet(_weaponType);
                if (resolved != null)
                {
                    GUI.enabled = false;
                    EditorGUILayout.ObjectField("  └ 해석된 SO", resolved, typeof(ActorAnimationMotionSet), false);
                    GUI.enabled = true;
                    _targetSO = resolved;
                }
                else
                {
                    EditorGUILayout.HelpBox($"WeaponType.{_weaponType} 에 연결된 ActorAnimationMotionSet이 없습니다.\n PlayerActorAnimationMotionSet 인스펙터에서 먼저 등록하세요.", MessageType.Warning);
                    _targetSO = null;
                }
            }
            else
            {
                // 직접 지정 모드 (fallback)
                _targetSO = (ActorAnimationMotionSet)EditorGUILayout.ObjectField(
                    new GUIContent("ActorAnimationMotionSet", "PlayerActorAnimationMotionSet이 없을 때 직접 지정합니다."),
                    _targetSO, typeof(ActorAnimationMotionSet), false);
            }

            // 출력 폴더
            using (new EditorGUILayout.HorizontalScope())
            {
                _outputFolder = EditorGUILayout.TextField("MotionSetAsset 저장 폴더", _outputFolder);
                if (GUILayout.Button("…", GUILayout.Width(26)))
                {
                    string picked = EditorUtility.OpenFolderPanel("저장 폴더 선택", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(picked))
                        _outputFolder = ToProjectRelative(picked);
                }
            }

            _filePrefix = EditorGUILayout.TextField(
                new GUIContent("파일명 접두사", "저장될 MotionSetAsset 파일명 앞에 붙는 문자열.\n예) 'MS_' → MS_Move_Stop_Running.asset"),
                _filePrefix);

            EditorGUILayout.Space(2);
            _preferInPlace = EditorGUILayout.Toggle(
                new GUIContent("InPlace 버전 우선", "같은 이름의 일반/InPlace 클립이 모두 존재할 때 InPlace를 선택합니다."),
                _preferInPlace);
            _overwrite = EditorGUILayout.Toggle(
                new GUIContent("기존 항목 덮어쓰기", "대상 SO에 이미 등록된 Motion Slot도 재생성합니다."),
                _overwrite);
        }

        private void DrawScanButton()
        {
            GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
            if (GUILayout.Button("폴더 스캔", GUILayout.Height(28)))
                Scan();
            GUI.backgroundColor = Color.white;
        }

        private void DrawResultTable()
        {
            EditorGUILayout.LabelField($"스캔 결과  —  총 {_entries.Count}개 매핑 발견", EditorStyles.boldLabel);

            if (_entries.Count == 0)
            {
                EditorGUILayout.HelpBox("매핑 가능한 클립을 찾지 못했습니다.\n폴더 경로와 파일명 규칙을 확인하세요.", MessageType.Warning);
                return;
            }

            // 컬럼 헤더
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Space(4);
                EditorGUILayout.LabelField("✓",        GUILayout.Width(18));
                EditorGUILayout.LabelField("Motion Slot",  GUILayout.Width(220));
                EditorGUILayout.LabelField("클립",      GUILayout.Width(170));
                EditorGUILayout.LabelField("InPlace",  GUILayout.Width(55));
                EditorGUILayout.LabelField("FBX 경로",  GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField("상태",      GUILayout.Width(48));
            }

            // 스크롤 영역
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(260));
            for (int i = 0; i < _entries.Count; i++)
            {
                var e   = _entries[i];
                var bg  = e.Exists
                    ? new Color(1f, 0.92f, 0.4f, 0.25f)   // 노란 — 기존 존재
                    : new Color(0.5f, 1f, 0.6f, 0.18f);   // 초록 — 신규

                Rect row = EditorGUILayout.BeginHorizontal(i % 2 == 0 ? _rowEvenStyle : _rowOddStyle);
                EditorGUI.DrawRect(row, bg);

                GUILayout.Space(4);
                e.Selected = EditorGUILayout.Toggle(e.Selected, GUILayout.Width(18));
                EditorGUILayout.LabelField(e.MotionSlot.ToString(), GUILayout.Width(220));
                EditorGUILayout.ObjectField(e.Clip, typeof(AnimationClip), false, GUILayout.Width(170));
                EditorGUILayout.LabelField(e.IsInPlace ? "✔" : "", GUILayout.Width(55));
                EditorGUILayout.LabelField(e.FbxPath, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

                var statusColor = e.Exists ? new Color(1f, 0.7f, 0f) : new Color(0.2f, 0.8f, 0.2f);
                GUI.contentColor = statusColor;
                EditorGUILayout.LabelField(e.Exists ? "기존" : "신규", GUILayout.Width(48));
                GUI.contentColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            // 전체/신규 선택 버튼
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전체 선택",  GUILayout.Width(80))) _entries.ForEach(e => e.Selected = true);
                if (GUILayout.Button("전체 해제",  GUILayout.Width(80))) _entries.ForEach(e => e.Selected = false);
                if (GUILayout.Button("신규만 선택", GUILayout.Width(90))) _entries.ForEach(e => e.Selected = !e.Exists);
                GUILayout.FlexibleSpace();
                int sel = _entries.Count(e => e.Selected);
                EditorGUILayout.LabelField($"선택: {sel} / {_entries.Count}", GUILayout.Width(100));
            }
        }

        private void DrawGenerateButton()
        {
            if (_targetSO == null)
            {
                EditorGUILayout.HelpBox("생성하려면 대상 ActorAnimationMotionSet을 지정하세요.", MessageType.Error);
                return;
            }

            int count = _entries.Count(e => e.Selected);
            GUI.backgroundColor = new Color(0.4f, 0.85f, 0.4f);
            if (GUILayout.Button($"선택된 {count}개  MotionSetAsset 생성 / 업데이트", GUILayout.Height(32)))
                Generate();
            GUI.backgroundColor = Color.white;
        }

        // ── 로직 ───────────────────────────────────────────────────────────────

        private void Scan()
        {
            _entries.Clear();
            _scanned  = true;
            _statusMsg = "";

            if (!AssetDatabase.IsValidFolder(_scanFolder))
            {
                _statusMsg = $"폴더를 찾을 수 없습니다: {_scanFolder}";
                return;
            }

            // Motion Slot별 후보 수집 (InPlace 선호 여부 반영)
            var candidates = new Dictionary<GameplayTag, (string path, bool inPlace, AnimationClip clip)>();

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { _scanFolder });
            foreach (string guid in guids)
            {
                string path     = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                bool isInPlace  = fileName.EndsWith("_InPlace");
                string baseName = isInPlace ? fileName[..^"_InPlace".Length] : fileName;

                if (!PatternToKey.TryGetValue(baseName, out GameplayTag motionSlot))
                    continue;

                AnimationClip clip = ExtractClip(path);
                if (clip == null)
                    continue;

                if (!candidates.TryGetValue(motionSlot, out var existing))
                {
                    candidates[motionSlot] = (path, isInPlace, clip);
                }
                else
                {
                    // 선호 버전이 아직 없는 경우만 교체
                    bool wantInPlace     = _preferInPlace;
                    bool gotWanted       = wantInPlace ? existing.inPlace : !existing.inPlace;
                    bool candidateWanted = wantInPlace ? isInPlace : !isInPlace;
                    if (candidateWanted && !gotWanted)
                        candidates[motionSlot] = (path, isInPlace, clip);
                }
            }

            // MappingEntry 변환
            foreach (var kv in candidates)
            {
                bool exists = _targetSO != null && _targetSO.motionSlots.ContainsKey(kv.Key);
                _entries.Add(new MappingEntry
                {
                    MotionSlot = kv.Key,
                    FbxPath   = kv.Value.path,
                    IsInPlace = kv.Value.inPlace,
                    Clip      = kv.Value.clip,
                    Exists    = exists,
                    Selected  = _overwrite || !exists,
                });
            }

            _entries.Sort((a, b) => string.CompareOrdinal(a.MotionSlot.TagName, b.MotionSlot.TagName));
            _statusMsg = $"스캔 완료: {candidates.Count}개 매핑 발견 (전체 FBX {guids.Length}개 검사)";
            Repaint();
        }

        private void Generate()
        {
            if (_targetSO == null) return;

            EnsureFolderExists(_outputFolder);

            var selected = _entries.Where(e => e.Selected && e.Clip != null).ToList();
            if (selected.Count == 0) return;

            string prefix  = _filePrefix ?? "";
            int created = 0, updated = 0;

            // 각 항목의 저장 경로 미리 계산
            var pathMap = selected.ToDictionary(
                e => e,
                e => $"{_outputFolder}/{prefix}{e.MotionSlot}.asset");

            // ── Phase 1: MotionSetAsset 파일 생성/업데이트 ────────────────────
            // StartAssetEditing으로 묶어 CreateAsset 중간에 발생하는
            // 개별 import를 막고, Stop 시점에 일괄 import하도록 한다.
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var entry in selected)
                {
                    string assetPath = pathMap[entry];
                    MotionSetAsset msa = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(assetPath);
                    bool isNew         = msa == null;

                    if (isNew)
                    {
                        msa           = CreateInstance<MotionSetAsset>();
                        msa.motionSet = new MotionSet { motionSetName = $"{prefix}{entry.MotionSlot}" };
                    }

                    if (msa.motionSet.motions.Count == 0)
                        msa.motionSet.motions.Add(new Motion());

                    var motion           = msa.motionSet.motions[0];
                    motion.motionClip    = entry.Clip;
                    motion.motionName    = entry.Clip.name;
                    motion.playbackSpeed = 1f;
                    motion.clipStartTime = -1f;
                    motion.clipEndTime   = -1f;

                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(msa, assetPath);
                        created++;
                    }
                    else
                    {
                        EditorUtility.SetDirty(msa);
                        updated++;
                    }
                }
            }
            finally
            {
                // Stop 시점에 일괄 import — 이후 LoadAssetAtPath가 확정된 참조를 반환
                AssetDatabase.StopAssetEditing();
            }

            // ── Phase 2: import 완료된 에셋을 디스크에서 다시 로드하여 등록 ──
            // CreateInstance로 만든 객체는 import 후 무효화될 수 있으므로
            // 반드시 경로 기반으로 재로드한 참조를 딕셔너리에 넣는다.
            Undo.RecordObject(_targetSO, "Locomotion Setup: Register Motion Slots");

            foreach (var entry in selected)
            {
                string assetPath = pathMap[entry];
                var msa = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(assetPath);
                if (msa == null)
                {
                    Debug.LogWarning($"[LocoSetup] 로드 실패: {assetPath}");
                    continue;
                }

                if (_targetSO.motionSlots.ContainsKey(entry.MotionSlot))
                    _targetSO.motionSlots[entry.MotionSlot] = msa;
                else
                    _targetSO.motionSlots.Add(entry.MotionSlot, msa);
            }
            
            SyncSerializedDictionary(_targetSO.motionSlots);
            EditorUtility.SetDirty(_targetSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _statusMsg = $"완료 — 생성: {created}개 / 업데이트: {updated}개";

            // 결과 반영 (Exists 플래그 갱신)
            Scan();
            Repaint();
        }

        // ── 유틸 ───────────────────────────────────────────────────────────────

        /// <summary>FBX(Model) 에셋 경로에서 첫 번째 AnimationClip을 추출한다.</summary>
        private static AnimationClip ExtractClip(string path)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }
            return null;
        }

        /// <summary>절대 경로를 Assets/ 상대경로로 변환한다.</summary>
        private static string ToProjectRelative(string absolutePath)
        {
            string full = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string data = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            return full.StartsWith(data)
                ? "Assets" + full[data.Length..]
                : absolutePath;
        }

        /// <summary>중간 폴더를 포함해 경로를 재귀적으로 생성한다.</summary>
        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/') ?? "Assets";
            EnsureFolderExists(parent);

            string folderName = Path.GetFileName(folderPath);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };

            _rowEvenStyle = new GUIStyle(GUIStyle.none);
            _rowOddStyle  = new GUIStyle(GUIStyle.none);
        }
        
        private void SyncSerializedDictionary<TKey, TValue>(SerializedDictionary<TKey, TValue> dict)
        {
            if (dict == null) return;

            // 리플렉션으로 internal 필드인 _serializedList를 찾아옴
            var field = typeof(SerializedDictionary<TKey, TValue>).GetField("_serializedList", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                var list = (List<SerializedKeyValuePair<TKey, TValue>>)field.GetValue(dict);
                list.Clear();
                foreach (var kvp in dict)
                {
                    list.Add(new SerializedKeyValuePair<TKey, TValue> { Key = kvp.Key, Value = kvp.Value });
                }
            }
        }
    }
}
