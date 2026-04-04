using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// Stop / TurnInPlace FBX 클립을 폴더에서 자동으로 탐색하여
    /// MotionSetAsset을 일괄 생성하고 ActorAnimationMotionSet에 등록하는 에디터 창.
    /// </summary>
    public class LocoMotionSetupWindow : EditorWindow
    {
        // ── 파일명 패턴 → AnimKey 매핑 ──────────────────────────────────────────
        // _InPlace 접미사는 스캔 시 자동으로 제거한 뒤 비교한다.
        private static readonly Dictionary<string, AnimKey> PatternToKey = new()
        {
            // Stop — Run
            { "Run_F_To_Idle",     AnimKey.Move_Stop_Running     },
            { "Run_F_L45_To_Idle", AnimKey.Move_Stop_Running_L45 },
            { "Run_F_R45_To_Idle", AnimKey.Move_Stop_Running_R45 },
            // Stop — Walk
            { "Walk_F_To_Idle",     AnimKey.Move_Stop_Walking     },
            { "Walk_F_L45_To_Idle", AnimKey.Move_Stop_Walking_L45 },
            { "Walk_F_R45_To_Idle", AnimKey.Move_Stop_Walking_R45 },
            // Stop — Sprint
            { "Sprint_F_To_Idle",     AnimKey.Move_Stop_Sprinting     },
            { "Sprint_F_L45_To_Idle", AnimKey.Move_Stop_Sprinting_L45 },
            { "Sprint_F_R45_To_Idle", AnimKey.Move_Stop_Sprinting_R45 },
            // TurnInPlace — Stand Idle
            { "Stand_Idle_Turn_L45", AnimKey.Stand_Idle_Turn_L45 },
            { "Stand_Idle_Turn_R45", AnimKey.Stand_Idle_Turn_R45 },
            { "Stand_Idle_Turn_L90", AnimKey.Stand_Idle_Turn_L90 },
            { "Stand_Idle_Turn_R90", AnimKey.Stand_Idle_Turn_R90 },
            { "Stand_Idle_Turn_180", AnimKey.Stand_Idle_Turn_180 },
        };

        // ── 내부 데이터 ─────────────────────────────────────────────────────────
        private class MappingEntry
        {
            public AnimKey        AnimKey;
            public string         FbxPath;       // Assets/ 상대경로
            public bool           IsInPlace;
            public AnimationClip  Clip;
            public bool           Selected  = true;
            public bool           Exists;         // 대상 SO에 이미 등록됐는지
        }

        // ── 상태 ────────────────────────────────────────────────────────────────
        private string                   _scanFolder    = "";
        private ActorAnimationMotionSet  _targetSO;
        private string                   _outputFolder  = "Assets/10.Datas/Motion/Locomotion";
        private string                   _filePrefix    = "MS_";
        private bool                     _preferInPlace = true;
        private bool                     _overwrite     = false;

        private List<MappingEntry> _entries  = new();
        private Vector2            _scroll;
        private bool               _scanned;
        private string             _statusMsg = "";

        // ── 스타일 캐시 ─────────────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _rowEvenStyle;
        private GUIStyle _rowOddStyle;

        // ────────────────────────────────────────────────────────────────────────

        [MenuItem("UPlayGround/Locomotion Motion Setup")]
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

            // 대상 ActorAnimationMotionSet
            _targetSO = (ActorAnimationMotionSet)EditorGUILayout.ObjectField(
                "대상 ActorAnimationMotionSet", _targetSO, typeof(ActorAnimationMotionSet), false);

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
                new GUIContent("기존 항목 덮어쓰기", "대상 SO에 이미 등록된 AnimKey도 재생성합니다."),
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
                EditorGUILayout.LabelField("AnimKey",  GUILayout.Width(220));
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
                EditorGUILayout.LabelField(e.AnimKey.ToString(), GUILayout.Width(220));
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

            // AnimKey별 후보 수집 (InPlace 선호 여부 반영)
            var candidates = new Dictionary<AnimKey, (string path, bool inPlace, AnimationClip clip)>();

            string[] guids = AssetDatabase.FindAssets("t:Model", new[] { _scanFolder });
            foreach (string guid in guids)
            {
                string path     = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                bool isInPlace  = fileName.EndsWith("_InPlace");
                string baseName = isInPlace ? fileName[..^"_InPlace".Length] : fileName;

                if (!PatternToKey.TryGetValue(baseName, out AnimKey animKey))
                    continue;

                AnimationClip clip = ExtractClip(path);
                if (clip == null)
                    continue;

                if (!candidates.TryGetValue(animKey, out var existing))
                {
                    candidates[animKey] = (path, isInPlace, clip);
                }
                else
                {
                    // 선호 버전이 아직 없는 경우만 교체
                    bool wantInPlace     = _preferInPlace;
                    bool gotWanted       = wantInPlace ? existing.inPlace : !existing.inPlace;
                    bool candidateWanted = wantInPlace ? isInPlace : !isInPlace;
                    if (candidateWanted && !gotWanted)
                        candidates[animKey] = (path, isInPlace, clip);
                }
            }

            // MappingEntry 변환
            foreach (var kv in candidates)
            {
                bool exists = _targetSO != null && _targetSO.motionSets.ContainsKey(kv.Key);
                _entries.Add(new MappingEntry
                {
                    AnimKey   = kv.Key,
                    FbxPath   = kv.Value.path,
                    IsInPlace = kv.Value.inPlace,
                    Clip      = kv.Value.clip,
                    Exists    = exists,
                    Selected  = _overwrite || !exists,
                });
            }

            _entries.Sort((a, b) => a.AnimKey.CompareTo(b.AnimKey));
            _statusMsg = $"스캔 완료: {candidates.Count}개 매핑 발견 (전체 FBX {guids.Length}개 검사)";
            Repaint();
        }

        private void Generate()
        {
            if (_targetSO == null) return;

            EnsureFolderExists(_outputFolder);

            var selected = _entries.Where(e => e.Selected && e.Clip != null).ToList();
            if (selected.Count == 0) return;

            string prefix = _filePrefix ?? "";
            int created = 0, updated = 0;

            // ── Phase 1: MotionSetAsset 생성/업데이트 ──────────────────────────
            // AssetDatabase.CreateAsset 호출이 끝난 뒤에 딕셔너리를 건드려야
            // SerializedDictionary의 dirty 처리가 올바르게 된다.
            var toRegister = new List<(AnimKey key, MotionSetAsset msa)>();

            foreach (var entry in selected)
            {
                string assetPath = $"{_outputFolder}/{prefix}{entry.AnimKey}.asset";

                MotionSetAsset msa = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(assetPath);
                bool isNew         = msa == null;

                if (isNew)
                {
                    msa           = CreateInstance<MotionSetAsset>();
                    msa.motionSet = new MotionSet { motionSetName = $"{prefix}{entry.AnimKey}" };
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

                toRegister.Add((entry.AnimKey, msa));
            }

            // Phase 1 확정: 에셋 디스크에 기록
            AssetDatabase.SaveAssets();

            // ── Phase 2: ActorAnimationMotionSet 딕셔너리 일괄 등록 ────────────
            // 에셋 생성이 모두 끝난 뒤 한 번에 RecordObject → 딕셔너리 수정 → SetDirty
            Undo.RecordObject(_targetSO, "Locomotion Setup: Register AnimKeys");

            foreach (var (key, msa) in toRegister)
            {
                if (_targetSO.motionSets.ContainsKey(key))
                    _targetSO.motionSets[key] = msa;
                else
                    _targetSO.motionSets.Add(key, msa);
            }

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
    }
}
