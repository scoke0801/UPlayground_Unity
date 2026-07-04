#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 무기 본(Extra Bone) 애니메이션 베이크 에디터 창 (단일/수동).
/// 메뉴: UPlayGround / Animation / Weapon Bone Bake
///
/// ■ 배경
///   · Humanoid 리타겟팅은 휴먼 본만 근육값으로 변환하며, 무기 본 같은 extra bone 곡선은
///     "Animator 루트 기준 전체 경로가 일치하는 트랜스폼"에만 raw curve로 재생된다.
///   · Frank 팩 클립의 무기 본 경로(root/pelvis/.../hand_r/R_Hand_Weapon/Weapon_Sword)는
///     프로젝트 캐릭터(Armature/Hips/.../Hand_R/...)와 경로가 달라 곡선이 무시된다.
///
/// ■ 동작 (실제 베이크 로직은 WeaponBoneBakeCore 에 있음 — 파이프라인 창과 공유)
///   · 소스 리그(Frank_Katana_Skin)와 대상 리그(Bokusei)에 같은 클립을 동시에 샘플링
///   · 매 프레임 소스 무기 본의 월드 포즈를 대상 부모 본(Hand_R) 로컬 공간으로 변환
///   · 변환된 포즈를 대상 경로(.../Hand_R/R_Hand_Weapon) 곡선으로 클립 사본에 기록
///   · 같은 경로에 기존 베이크 결과가 있으면 CopySerialized로 덮어써 GUID 보존
///
/// ■ 사용 절차
///   1. 소스/대상 모델과 본 매핑 확인 (기본값: Katana → Bokusei)
///   2. 베이크할 클립 등록 (FBX 선택 후 "선택에서 클립 추가" 버튼 사용 가능)
///   3. Bake 실행 → 출력 폴더에 {클립이름}{접미사}.anim 생성
///   4. MotionSet의 클립 참조를 베이크된 클립으로 교체 (일괄 교체는 파이프라인 창 사용)
///   5. 무기 소켓 ParentConstraint의 source를 R_Hand_Weapon으로 전환
/// </summary>
public class WeaponBoneBakeEditorWindow : EditorWindow
{
    private const string DefaultSourceModelPath =
        "Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Meshes/Frank_Katana_Skin.FBX";
    private const string DefaultTargetModelPath =
        "Assets/ExternalAssets/Character/ROKO SHOP/Bokusei/00_FBX/Bokusei.fbx";
    private const string DefaultOutputFolder = "Assets/07.Animations/WeaponBaked";

    [SerializeField] private GameObject _sourceModel;
    [SerializeField] private GameObject _targetModel;
    [SerializeField] private List<WeaponBoneBakeCore.BoneMapping> _mappings = new() { new WeaponBoneBakeCore.BoneMapping() };
    [SerializeField] private List<AnimationClip> _clips = new();
    [SerializeField] private string _outputFolder = DefaultOutputFolder;
    [SerializeField] private string _suffix = "_WeaponBaked";
    [SerializeField] private float _sampleRate = 0f; // 0 = 클립 frameRate 사용
    [SerializeField] private float _positionScale = 1f;

    private Vector2 _scroll;
    private bool _showHelp;

    [MenuItem("UPlayGround/Animation/Weapon Bone Bake")]
    private static void Open()
    {
        var window = GetWindow<WeaponBoneBakeEditorWindow>("Weapon Bone Bake");
        window.minSize = new Vector2(480f, 400f);
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

        _showHelp = EditorGUILayout.ToggleLeft("도움말 표시", _showHelp);
        if (_showHelp)
        {
            EditorGUILayout.HelpBox(
                "Humanoid extra bone 곡선은 전체 경로가 일치해야만 재생됩니다.\n" +
                "이 툴은 소스 리그의 무기 본 궤적을 대상 캐릭터 경로의 곡선으로 변환한\n" +
                "클립 사본(.anim)을 생성합니다. 재베이크 시 GUID가 보존되므로\n" +
                "MotionSet 참조를 교체한 뒤에도 안심하고 다시 실행할 수 있습니다.\n\n" +
                "여러 FBX/폴더를 한 번에 변환하고 MotionSet 참조까지 교체하려면\n" +
                "UPlayGround / Animation / Weapon Bone Bake Pipeline 창을 사용하세요.",
                MessageType.Info);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("리그", EditorStyles.boldLabel);
        _sourceModel = (GameObject)EditorGUILayout.ObjectField("소스 모델 (Frank)", _sourceModel, typeof(GameObject), false);
        _targetModel = (GameObject)EditorGUILayout.ObjectField("대상 모델 (플레이어)", _targetModel, typeof(GameObject), false);

        EditorGUILayout.Space(4f);
        DrawMappings();

        EditorGUILayout.Space(4f);
        DrawClips();

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("출력", EditorStyles.boldLabel);
        _outputFolder = EditorGUILayout.TextField("출력 폴더", _outputFolder);
        _suffix = EditorGUILayout.TextField("파일 접미사", _suffix);
        _sampleRate = EditorGUILayout.FloatField(new GUIContent("샘플레이트 (0=클립 fps)"), _sampleRate);
        _positionScale = EditorGUILayout.FloatField(new GUIContent("위치 스케일", "리그 체격 차이 보정용 위치 배율"), _positionScale);

        EditorGUILayout.Space(8f);
        using (new EditorGUI.DisabledScope(!CanBake()))
        {
            if (GUILayout.Button($"Bake ({_clips.Count}개 클립)", GUILayout.Height(32f)))
                BakeAll();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawMappings()
    {
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
            EditorGUILayout.Space(2f);
        }

        if (GUILayout.Button("매핑 추가"))
            _mappings.Add(new WeaponBoneBakeCore.BoneMapping());
    }

    private void DrawClips()
    {
        EditorGUILayout.LabelField($"클립 ({_clips.Count})", EditorStyles.boldLabel);
        for (int i = 0; i < _clips.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _clips[i] = (AnimationClip)EditorGUILayout.ObjectField(_clips[i], typeof(AnimationClip), false);
            if (GUILayout.Button("−", GUILayout.Width(24f)))
            {
                _clips.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("클립 추가"))
            _clips.Add(null);
        if (GUILayout.Button("선택에서 클립 추가"))
            AddClipsFromSelection();
        if (GUILayout.Button("전체 비우기"))
            _clips.Clear();
        EditorGUILayout.EndHorizontal();
    }

    private void AddClipsFromSelection()
    {
        // FBX를 선택하면 하위 클립까지 수집. 프리뷰 클립(__preview__)은 제외.
        UnityEngine.Object[] found = Selection.GetFiltered(typeof(AnimationClip), SelectionMode.DeepAssets);
        foreach (UnityEngine.Object obj in found)
        {
            var clip = obj as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview__"))
                continue;
            if (!_clips.Contains(clip))
                _clips.Add(clip);
        }
    }

    private bool CanBake()
    {
        if (_sourceModel == null || _targetModel == null || _mappings.Count == 0)
            return false;

        for (int i = 0; i < _clips.Count; i++)
        {
            if (_clips[i] != null)
                return true;
        }

        return false;
    }

    // ── 베이크 (코어에 위임) ───────────────────────────────────

    private void BakeAll()
    {
        var opt = new WeaponBoneBakeCore.BakeOptions
        {
            sourceModel = _sourceModel,
            targetModel = _targetModel,
            mappings = _mappings,
            outputFolder = _outputFolder,
            suffix = _suffix,
            sampleRate = _sampleRate,
            positionScale = _positionScale,
            // 단일 창은 기존 동작(클립 이름 그대로) 유지 — generic 이름 치환/매핑 영속화는 파이프라인 담당
            useFbxNameForGenericClips = false,
            map = null
        };

        try
        {
            WeaponBoneBakeCore.BakeClips(
                _clips, opt,
                (progress, label) => EditorUtility.DisplayProgressBar("Weapon Bone Bake", label, progress));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
#endif
