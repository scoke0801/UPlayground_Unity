#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor.Combat
{
    public sealed class CombatHitboxSetupWindow : EditorWindow
    {
        [SerializeField] private GameObject _target;
        [SerializeField] private CombatHitboxSetupProfileSO _profile;
        [SerializeField] private CombatHitboxSetupMode _mode = CombatHitboxSetupMode.WeaponAutoFit;
        [SerializeField] private bool _useAutomaticMode = true;
        [SerializeField] private bool _forceRefit;
        [SerializeField] private bool _showAdvanced;
        [SerializeField] private Vector2 _scroll;

        private readonly List<CombatHitboxSetupResult> _results = new();

        [MenuItem("UPlayGround/Combat/HitBox Setup")]
        [MenuItem("UPlayGround/Generator Tool/Combat HitBox Setup")]
        private static void Open()
        {
            GetWindow<CombatHitboxSetupWindow>("Combat HitBox Setup");
        }

        private void OnEnable()
        {
            Selection.selectionChanged += HandleSelectionChanged;
            if (_target == null)
                _target = Selection.activeGameObject;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= HandleSelectionChanged;
        }

        private void HandleSelectionChanged()
        {
            if (Selection.activeGameObject != null)
                _target = Selection.activeGameObject;
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("부착형 Combat HitBox 자동 설정", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Hierarchy 또는 Project에서 루트를 선택하면 하위 계층을 분석해 생성 방식을 자동 결정합니다.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _target = (GameObject)EditorGUILayout.ObjectField(
                "대상 루트",
                _target,
                typeof(GameObject),
                true);
            if (EditorGUI.EndChangeCheck())
            {
                _results.Clear();
                if (_target != null)
                    Selection.activeGameObject = _target;
            }

            CombatHitboxTargetAnalysis analysis = CombatHitboxAutoFitter.Analyze(_target);
            DrawAnalysis(analysis);

            _profile = (CombatHitboxSetupProfileSO)EditorGUILayout.ObjectField(
                "생성 프로필",
                _profile,
                typeof(CombatHitboxSetupProfileSO),
                false);

            using (new EditorGUI.DisabledScope(_target == null))
            {
                GUI.backgroundColor = new Color(0.35f, 0.8f, 0.45f);
                if (GUILayout.Button("하위 계층 분석 후 HitBox 자동 생성", GUILayout.Height(34f)))
                    ExecuteSingle(_target, ResolveMode(analysis));
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_target == null))
                {
                    if (GUILayout.Button("검증"))
                        ExecuteSingle(_target, CombatHitboxSetupMode.ValidateOnly);
                    if (GUILayout.Button("기존 항목 Refit"))
                        ExecuteSingle(_target, CombatHitboxSetupMode.RefitExisting);
                    if (GUILayout.Button("생성 항목 제거"))
                        ExecuteSingle(_target, CombatHitboxSetupMode.RemoveGenerated);
                }
            }

            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "고급 설정 및 다중 선택", true);
            if (_showAdvanced)
                DrawAdvanced(analysis);

            EditorGUILayout.Space(8);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (CombatHitboxSetupResult result in _results)
            {
                EditorGUILayout.LabelField(
                    $"{result.Target}  생성 {result.Created} / 갱신 {result.Updated} / 건너뜀 {result.Skipped}",
                    EditorStyles.boldLabel);
                foreach (string message in result.Messages)
                    EditorGUILayout.LabelField($"  • {message}", EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space(4);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawAnalysis(CombatHitboxTargetAnalysis analysis)
        {
            MessageType type = _target == null
                ? MessageType.Warning
                : analysis.RendererCount == 0 && analysis.SuggestedMode == CombatHitboxSetupMode.WeaponAutoFit
                    ? MessageType.Warning
                    : MessageType.Info;
            EditorGUILayout.HelpBox(
                _target == null
                    ? "Player/Model_Bokusei/Weapon/Katana처럼 HitBox를 붙일 루트를 선택하세요."
                    : $"{analysis.Summary}\n자동 모드: {GetModeLabel(analysis.SuggestedMode)}",
                type);
        }

        private void DrawAdvanced(CombatHitboxTargetAnalysis analysis)
        {
            EditorGUI.indentLevel++;
            _useAutomaticMode = EditorGUILayout.ToggleLeft("계층 분석으로 모드 자동 선택", _useAutomaticMode);
            using (new EditorGUI.DisabledScope(_useAutomaticMode))
                _mode = (CombatHitboxSetupMode)EditorGUILayout.EnumPopup("수동 모드", _mode);
            _forceRefit = EditorGUILayout.ToggleLeft("수동 수정 마커도 강제 Refit", _forceRefit);

            GameObject[] selectedTargets = GetSelectedTargets();
            EditorGUILayout.LabelField($"현재 다중 선택 대상: {selectedTargets.Length}개");
            using (new EditorGUI.DisabledScope(selectedTargets.Length == 0))
            {
                if (GUILayout.Button("선택 대상 전체 자동 생성"))
                    Execute(selectedTargets, automaticMode: true);
            }
            EditorGUI.indentLevel--;
        }

        private CombatHitboxSetupMode ResolveMode(CombatHitboxTargetAnalysis analysis)
            => _useAutomaticMode ? analysis.SuggestedMode : _mode;

        private void ExecuteSingle(GameObject target, CombatHitboxSetupMode mode)
        {
            _results.Clear();
            ExecuteTarget(target, mode);
            FinishExecution();
        }

        private void Execute(GameObject[] targets, bool automaticMode)
        {
            _results.Clear();
            foreach (GameObject target in targets)
            {
                CombatHitboxSetupMode mode = automaticMode
                    ? CombatHitboxAutoFitter.Analyze(target).SuggestedMode
                    : _mode;
                ExecuteTarget(target, mode);
            }
            FinishExecution();
        }

        private void FinishExecution()
        {
            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();
        }

        private void ExecuteTarget(GameObject target, CombatHitboxSetupMode mode)
        {
            if (target == null)
                return;

            string path = AssetDatabase.GetAssetPath(target);
            if (IsModelAsset(path))
            {
                _results.Add(new CombatHitboxSetupResult(
                    target.name, 0, 0, 1, new[] { "FBX 원본 수정 차단: Prefab Variant 또는 별도 Prefab에서 실행하세요." }));
                return;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(target))
                ApplyToPrefab(path, mode);
            else
                _results.Add(CombatHitboxAutoFitter.Apply(target, mode, _profile, _forceRefit));
        }

        private void ApplyToPrefab(string path, CombatHitboxSetupMode mode)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CombatHitboxSetupResult result =
                    CombatHitboxAutoFitter.Apply(contents, mode, _profile, _forceRefit);
                _results.Add(result);
                if (mode != CombatHitboxSetupMode.ValidateOnly)
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static GameObject[] GetSelectedTargets()
        {
            var targets = new List<GameObject>();
            foreach (UnityEngine.Object selected in Selection.objects)
            {
                if (selected is GameObject gameObject)
                {
                    targets.Add(gameObject);
                    continue;
                }

                string path = AssetDatabase.GetAssetPath(selected);
                if (!AssetDatabase.IsValidFolder(path))
                    continue;
                targets.AddRange(AssetDatabase.FindAssets("t:Prefab", new[] { path })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                    .Where(prefab => prefab != null));
            }
            return targets.Distinct().ToArray();
        }

        private static bool IsModelAsset(string path)
            => !string.IsNullOrWhiteSpace(path)
               && string.Equals(Path.GetExtension(path), ".fbx", System.StringComparison.OrdinalIgnoreCase);

        private static string GetModeLabel(CombatHitboxSetupMode mode)
            => mode switch
            {
                CombatHitboxSetupMode.WeaponAutoFit => "무기 Renderer Bounds",
                CombatHitboxSetupMode.HumanoidBodySetup => "Humanoid 본",
                CombatHitboxSetupMode.GenericBodySetup => "Generic 본 이름",
                _ => mode.ToString(),
            };
    }
}
#endif
