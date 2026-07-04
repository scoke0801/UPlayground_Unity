#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 무기 본 애니메이션 "일괄 변환 파이프라인" 창.
/// 메뉴: UPlayGround / Animation / Weapon Bone Bake Pipeline
///
/// 베이크 핵심 로직은 WeaponBoneBakeCore(단일 창과 공유)에 있고, 이 창은 4단계 오케스트레이션만 담당한다.
///
///   1단계) 소스 수집·필터 : 폴더/선택에서 클립 수집 → 무기 본 곡선이 실제로 있는 클립만 표시
///   2단계) 일괄 베이크     : 코어 재사용 + FBX 기반 파일명 + GUID 보존 덮어쓰기 + 매핑 영속화(JSON)
///   3단계) 참조 교체       : MotionSet 등 SO의 클립 참조를 드라이런 후 교체(Undo/롤백)
///   4단계) 검증 리포트     : 클립별 키수·이탈량, 교체 후 잔존 원본 참조 목록
/// </summary>
public class WeaponBoneBakePipelineWindow : EditorWindow
{
    private const string DefaultSourceModelPath =
        "Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Meshes/Frank_Katana_Skin.FBX";
    private const string DefaultTargetModelPath =
        "Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/00_FBX/Bokusei.fbx";
    private const string DefaultOutputFolder = "Assets/07.Animations/WeaponBaked";
    private const string DefaultSourceFolder =
        "Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Katana";
    private const string DefaultMotionSetFolder = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet";

    // ── 수집 항목 ──
    private class CollectItem
    {
        public AnimationClip clip;
        public bool hasWeaponCurves;
        public bool include;
    }

    // ── 리그/베이크 설정 ──
    [SerializeField] private GameObject _sourceModel;
    [SerializeField] private GameObject _targetModel;
    [SerializeField] private List<WeaponBoneBakeCore.BoneMapping> _mappings = new() { new WeaponBoneBakeCore.BoneMapping() };
    [SerializeField] private string _outputFolder = DefaultOutputFolder;
    [SerializeField] private string _suffix = "_WeaponBaked";
    [SerializeField] private float _sampleRate = 0f;
    [SerializeField] private float _positionScale = 1f;
    [SerializeField] private bool _useFbxNameForGenericClips = true;

    // ── 1단계 ──
    [SerializeField] private string _sourceFolder = DefaultSourceFolder;
    private readonly List<CollectItem> _collected = new();

    // ── 2단계 ──
    private readonly List<WeaponBoneBakeCore.ClipReport> _reports = new();

    // ── 3단계 ──
    [SerializeField] private string _scanFolder = DefaultMotionSetFolder;
    [SerializeField] private string _scanTypeFilter = "t:ScriptableObject";
    private List<WeaponBakeReferenceReplacer.ReplacementEntry> _replacements = new();
    private readonly List<WeaponBakeReferenceReplacer.AppliedRecord> _rollback = new();
    private List<string> _stillReferencing = new();

    // ── 폴드아웃/스크롤 ──
    [SerializeField] private bool _foldRig = true;
    [SerializeField] private bool _foldCollect = true;
    [SerializeField] private bool _foldBake = true;
    [SerializeField] private bool _foldReplace = true;
    [SerializeField] private bool _foldReport = true;
    private Vector2 _scroll;

    [MenuItem("UPlayGround/Animation/Weapon Bone Bake Pipeline")]
    private static void Open()
    {
        var window = GetWindow<WeaponBoneBakePipelineWindow>("Weapon Bake Pipeline");
        window.minSize = new Vector2(560f, 560f);
    }

    private void OnEnable()
    {
        if (_sourceModel == null)
            _sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSourceModelPath);
        if (_targetModel == null)
            _targetModel = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultTargetModelPath);
    }

    // ── GUI ──────────────────────────────────────────────────

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawRigSection();
        EditorGUILayout.Space(6f);
        DrawCollectSection();
        EditorGUILayout.Space(6f);
        DrawBakeSection();
        EditorGUILayout.Space(6f);
        DrawReplaceSection();
        EditorGUILayout.Space(6f);
        DrawReportSection();

        EditorGUILayout.EndScrollView();
    }

    // ── 리그/베이크 설정 ──

    private void DrawRigSection()
    {
        _foldRig = EditorGUILayout.BeginFoldoutHeaderGroup(_foldRig, "리그 · 베이크 설정");
        if (_foldRig)
        {
            _sourceModel = (GameObject)EditorGUILayout.ObjectField("소스 모델 (Frank)", _sourceModel, typeof(GameObject), false);
            _targetModel = (GameObject)EditorGUILayout.ObjectField("대상 모델 (플레이어)", _targetModel, typeof(GameObject), false);

            EditorGUILayout.LabelField("본 매핑 (소스 본 이름 → 대상 곡선 경로)", EditorStyles.boldLabel);
            for (int i = 0; i < _mappings.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
                _mappings[i].sourceBoneName = EditorGUILayout.TextField("소스 본 이름", _mappings[i].sourceBoneName);
                _mappings[i].targetBonePath = EditorGUILayout.TextField("대상 본 경로", _mappings[i].targetBonePath);
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("−", GUILayout.Width(24f), GUILayout.Height(38f)))
                {
                    _mappings.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("매핑 추가"))
                _mappings.Add(new WeaponBoneBakeCore.BoneMapping());

            _outputFolder = EditorGUILayout.TextField("출력 폴더", _outputFolder);
            _suffix = EditorGUILayout.TextField("파일 접미사", _suffix);
            _sampleRate = EditorGUILayout.FloatField(new GUIContent("샘플레이트 (0=클립 fps)"), _sampleRate);
            _positionScale = EditorGUILayout.FloatField(new GUIContent("위치 스케일", "리그 체격 차이 보정용 위치 배율"), _positionScale);
            _useFbxNameForGenericClips = EditorGUILayout.ToggleLeft(
                new GUIContent("Generic 클립명은 FBX 파일명 사용", "'Take 001' 같은 이름을 FBX 파일명으로 대체"),
                _useFbxNameForGenericClips);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ── 1단계: 수집·필터 ──

    private void DrawCollectSection()
    {
        _foldCollect = EditorGUILayout.BeginFoldoutHeaderGroup(_foldCollect, "1단계 · 소스 수집 · 필터");
        if (_foldCollect)
        {
            _sourceFolder = EditorGUILayout.TextField("소스 폴더", _sourceFolder);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("폴더에서 수집"))
                CollectFromFolder(_sourceFolder);
            if (GUILayout.Button("선택에서 수집"))
                CollectFromSelection();
            if (GUILayout.Button("비우기"))
                _collected.Clear();
            EditorGUILayout.EndHorizontal();

            int withCurves = 0;
            foreach (CollectItem c in _collected)
                if (c.hasWeaponCurves) withCurves++;

            EditorGUILayout.LabelField(
                $"수집 {_collected.Count}개 · 무기 본 곡선 보유 {withCurves}개 · 제외 {_collected.Count - withCurves}개",
                EditorStyles.miniBoldLabel);

            for (int i = 0; i < _collected.Count; i++)
            {
                CollectItem item = _collected[i];
                EditorGUILayout.BeginHorizontal();

                using (new EditorGUI.DisabledScope(!item.hasWeaponCurves))
                    item.include = EditorGUILayout.Toggle(item.include, GUILayout.Width(18f));

                EditorGUILayout.ObjectField(item.clip, typeof(AnimationClip), false);

                if (item.hasWeaponCurves)
                    EditorGUILayout.LabelField("무기 본 곡선 O", GUILayout.Width(110f));
                else
                    EditorGUILayout.LabelField("곡선 없음 — 원본 사용", EditorStyles.miniLabel, GUILayout.Width(150f));

                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void CollectFromFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            EditorUtility.DisplayDialog("수집 실패", $"유효한 폴더가 아닙니다:\n{folder}", "확인");
            return;
        }

        // 폴더 내 모든 에셋을 훑어 FBX/anim의 하위 AnimationClip을 수집.
        string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folder });
        var seen = new HashSet<AnimationClip>();
        foreach (CollectItem c in _collected)
            seen.Add(c.clip);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".fbx" && ext != ".anim") continue;

            foreach (Object obj in AssetDatabase.LoadAllAssetsAtPath(path))
                TryAddClip(obj as AnimationClip, seen);
        }
    }

    private void CollectFromSelection()
    {
        var seen = new HashSet<AnimationClip>();
        foreach (CollectItem c in _collected)
            seen.Add(c.clip);

        foreach (Object obj in Selection.GetFiltered(typeof(AnimationClip), SelectionMode.DeepAssets))
            TryAddClip(obj as AnimationClip, seen);
    }

    private void TryAddClip(AnimationClip clip, HashSet<AnimationClip> seen)
    {
        if (clip == null || clip.name.StartsWith("__preview__") || seen.Contains(clip))
            return;

        seen.Add(clip);
        bool has = WeaponBoneBakeCore.HasWeaponBoneCurves(clip, _mappings);
        _collected.Add(new CollectItem { clip = clip, hasWeaponCurves = has, include = has });
    }

    // ── 2단계: 일괄 베이크 ──

    private void DrawBakeSection()
    {
        _foldBake = EditorGUILayout.BeginFoldoutHeaderGroup(_foldBake, "2단계 · 일괄 베이크");
        if (_foldBake)
        {
            int targetCount = 0;
            foreach (CollectItem c in _collected)
                if (c.include && c.hasWeaponCurves) targetCount++;

            EditorGUILayout.HelpBox(
                "무기 본 곡선이 있는 선택 클립만 베이크합니다. 기존 산출물은 GUID를 보존해 덮어쓰며,\n" +
                "소스→베이크 매핑은 출력 폴더의 WeaponBakeMap.json 에 기록됩니다.",
                MessageType.None);

            using (new EditorGUI.DisabledScope(!CanBake(targetCount)))
            {
                if (GUILayout.Button($"베이크 실행 ({targetCount}개)", GUILayout.Height(30f)))
                    RunBake();
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private bool CanBake(int targetCount)
    {
        return _sourceModel != null && _targetModel != null && _mappings.Count > 0 && targetCount > 0;
    }

    private void RunBake()
    {
        var clips = new List<AnimationClip>();
        foreach (CollectItem c in _collected)
            if (c.include && c.hasWeaponCurves && c.clip != null)
                clips.Add(c.clip);

        if (clips.Count == 0)
            return;

        WeaponBakeMap map = WeaponBakeMap.Load(_outputFolder);

        var opt = new WeaponBoneBakeCore.BakeOptions
        {
            sourceModel = _sourceModel,
            targetModel = _targetModel,
            mappings = _mappings,
            outputFolder = _outputFolder,
            suffix = _suffix,
            sampleRate = _sampleRate,
            positionScale = _positionScale,
            useFbxNameForGenericClips = _useFbxNameForGenericClips,
            map = map
        };

        _reports.Clear();
        try
        {
            List<WeaponBoneBakeCore.ClipReport> result = WeaponBoneBakeCore.BakeClips(
                clips, opt,
                (progress, label) => EditorUtility.DisplayProgressBar("Weapon Bake Pipeline", label, progress));
            _reports.AddRange(result);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        LogReportSummary();
    }

    // ── 3단계: 참조 교체 ──

    private void DrawReplaceSection()
    {
        _foldReplace = EditorGUILayout.BeginFoldoutHeaderGroup(_foldReplace, "3단계 · MotionSet 참조 교체");
        if (_foldReplace)
        {
            _scanFolder = EditorGUILayout.TextField("스캔 폴더", _scanFolder);
            _scanTypeFilter = EditorGUILayout.TextField(
                new GUIContent("타입 필터", "AssetDatabase.FindAssets 필터. 예: t:ScriptableObject"), _scanTypeFilter);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("드라이런 스캔"))
                ScanReplacements();
            using (new EditorGUI.DisabledScope(!HasApplicable()))
            {
                if (GUILayout.Button("교체 실행"))
                    ApplyReplacements();
            }
            using (new EditorGUI.DisabledScope(_rollback.Count == 0))
            {
                if (GUILayout.Button($"롤백 ({_rollback.Count})"))
                    RollbackReplacements();
            }
            EditorGUILayout.EndHorizontal();

            if (_replacements.Count == 0)
            {
                EditorGUILayout.LabelField("드라이런 결과 없음 (스캔을 먼저 실행)", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"교체 후보 {_replacements.Count}건", EditorStyles.miniBoldLabel);
                foreach (WeaponBakeReferenceReplacer.ReplacementEntry e in _replacements)
                {
                    EditorGUILayout.BeginHorizontal();
                    e.apply = EditorGUILayout.Toggle(e.apply, GUILayout.Width(18f));
                    EditorGUILayout.LabelField(
                        $"{Path.GetFileNameWithoutExtension(e.ownerPath)} · {e.propertyPath}",
                        GUILayout.MinWidth(180f));
                    string from = e.fromClip != null ? e.fromClip.name : "(null)";
                    string to = e.toClip != null ? e.toClip.name : "(null)";
                    EditorGUILayout.LabelField($"{from} → {to}", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private bool HasApplicable()
    {
        foreach (WeaponBakeReferenceReplacer.ReplacementEntry e in _replacements)
            if (e.apply) return true;
        return false;
    }

    private Dictionary<AnimationClip, AnimationClip> BuildSourceToBaked()
    {
        // 우선 방금 베이크한 리포트에서 대응표를 만들고, 없으면 영속화된 매핑에서 로드.
        var map = new Dictionary<AnimationClip, AnimationClip>();
        foreach (WeaponBoneBakeCore.ClipReport r in _reports)
        {
            if (r.sourceClip != null && r.bakedClip != null && !map.ContainsKey(r.sourceClip))
                map.Add(r.sourceClip, r.bakedClip);
        }

        if (map.Count == 0)
        {
            WeaponBakeMap persisted = WeaponBakeMap.Load(_outputFolder);
            map = persisted.BuildSourceToBaked();
        }

        return map;
    }

    private string[] ScanFolders()
    {
        return AssetDatabase.IsValidFolder(_scanFolder) ? new[] { _scanFolder } : null;
    }

    private void ScanReplacements()
    {
        Dictionary<AnimationClip, AnimationClip> map = BuildSourceToBaked();
        if (map.Count == 0)
        {
            EditorUtility.DisplayDialog("스캔 불가",
                "원본→베이크 대응표가 비어 있습니다. 먼저 2단계 베이크를 실행하거나 WeaponBakeMap.json이 있어야 합니다.",
                "확인");
            _replacements = new List<WeaponBakeReferenceReplacer.ReplacementEntry>();
            return;
        }

        _replacements = WeaponBakeReferenceReplacer.Scan(map, ScanFolders(), _scanTypeFilter);
        Debug.Log($"[WeaponBakePipeline] 드라이런: 교체 후보 {_replacements.Count}건 (스캔 폴더: {_scanFolder})");
    }

    private void ApplyReplacements()
    {
        int applied = WeaponBakeReferenceReplacer.Apply(_replacements, _rollback);
        Debug.Log($"[WeaponBakePipeline] 참조 교체 완료: {applied}건 (롤백 스택 {_rollback.Count})");
        RefreshStillReferencing();
        // 교체된 항목은 목록에서 제거
        _replacements.RemoveAll(e => e.apply);
    }

    private void RollbackReplacements()
    {
        int restored = WeaponBakeReferenceReplacer.Rollback(_rollback);
        Debug.Log($"[WeaponBakePipeline] 롤백 완료: {restored}건");
        _rollback.Clear();
        RefreshStillReferencing();
    }

    // ── 4단계: 검증 리포트 ──

    private void DrawReportSection()
    {
        _foldReport = EditorGUILayout.BeginFoldoutHeaderGroup(_foldReport, "4단계 · 검증 리포트");
        if (_foldReport)
        {
            if (GUILayout.Button("잔존 원본 참조 재검사"))
                RefreshStillReferencing();

            if (_reports.Count == 0)
            {
                EditorGUILayout.LabelField("베이크 리포트 없음", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"베이크 클립 {_reports.Count}개", EditorStyles.boldLabel);
                foreach (WeaponBoneBakeCore.ClipReport r in _reports)
                {
                    string name = r.sourceClip != null ? r.sourceClip.name : "(null)";
                    string line =
                        $"{name} · 키 {r.keyCount} · 위치이탈 {r.maxPositionDeviation:F3}m · 회전이탈 {r.maxRotationDeviation:F1}°";
                    if (r.IsMotionNegligible)
                        EditorGUILayout.HelpBox(line + "\n⚠ 무기 본 모션 미미", MessageType.Warning);
                    else
                        EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("교체 후 잔존 원본 참조", EditorStyles.boldLabel);
            if (_stillReferencing == null || _stillReferencing.Count == 0)
            {
                EditorGUILayout.LabelField("없음 (또는 미검사)", EditorStyles.miniLabel);
            }
            else
            {
                foreach (string path in _stillReferencing)
                    EditorGUILayout.LabelField("• " + path, EditorStyles.miniLabel);
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private void RefreshStillReferencing()
    {
        Dictionary<AnimationClip, AnimationClip> map = BuildSourceToBaked();
        var sources = new HashSet<AnimationClip>(map.Keys);
        _stillReferencing = WeaponBakeReferenceReplacer.FindStillReferencing(sources, ScanFolders(), _scanTypeFilter);
    }

    private void LogReportSummary()
    {
        int negligible = 0;
        foreach (WeaponBoneBakeCore.ClipReport r in _reports)
            if (r.IsMotionNegligible) negligible++;

        Debug.Log(
            $"[WeaponBakePipeline] 베이크 완료: {_reports.Count}개 클립" +
            (negligible > 0 ? $" (무기 본 모션 미미 {negligible}개 — 리포트 확인)" : string.Empty));
    }
}
#endif
