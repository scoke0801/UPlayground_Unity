#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor
{
    /// <summary>
    /// PlayMode에서 맞춘 씬 Transform 값을 지정한 프리팹 에셋의 같은 경로에 복사한다.
    /// 기준 루트와 스캔 루트를 분리해, 특정 하위 오브젝트 아래의 대상만 골라 반영할 수 있다.
    /// </summary>
    public sealed class RuntimeTransformPrefabSyncWindow : EditorWindow
    {
        private const string DefaultPlayerPrefabPath = "Assets/03.Prefabs/Actor/Player/Player.prefab";

        private GameObject _pathRoot;
        private GameObject _scanRoot;
        private GameObject _targetPrefab;
        private string _containerName = "InteractionObject";
        private Vector2 _detectedScroll;
        private Vector2 _applyScroll;
        private readonly List<ToolTransformInfo> _detectedTransforms = new();
        private readonly List<ToolTransformInfo> _applyTargets = new();

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/유틸/Runtime Transform 프리팹 반영", priority = UPlaygroundMenuPriority.Util + 1)]
        public static void Open()
        {
            var window = GetWindow<RuntimeTransformPrefabSyncWindow>("Runtime Transform Sync");
            window.minSize = new Vector2(620f, 440f);
            window.ResolveDefaultsFromSelection();
            window.Show();
        }

        [MenuItem("GameObject/UPlayGround/Runtime Transform 프리팹 반영", priority = 50)]
        private static void OpenFromGameObjectMenu()
        {
            Open();
        }

        [MenuItem("GameObject/UPlayGround/Runtime Transform 프리팹 반영", true)]
        private static bool CanOpenFromGameObjectMenu()
        {
            return Selection.activeGameObject != null;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            if (_targetPrefab == null)
                _targetPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPlayerPrefabPath);
            ResolveDefaultsFromSelection();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawRoots();
            DrawTransformLists();
            DrawApplyButton();
        }

        private void DrawHeader()
        {
            EditorGUILayout.HelpBox(
                "스캔 루트에서 지정 컨테이너 하위 Transform을 감지하고, 씬 기준 루트부터 계산한 경로로 대상 프리팹 에셋에 저장합니다.",
                MessageType.Info);
        }

        private void DrawRoots()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _pathRoot = (GameObject)EditorGUILayout.ObjectField(
                        "씬 기준 루트",
                        _pathRoot,
                        typeof(GameObject),
                        true);

                    if (GUILayout.Button("선택 루트", GUILayout.Width(80f)))
                        UseSelectionAsPathRoot();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _scanRoot = (GameObject)EditorGUILayout.ObjectField(
                        "스캔 루트",
                        _scanRoot,
                        typeof(GameObject),
                        true);

                    if (GUILayout.Button("선택 사용", GUILayout.Width(80f)))
                        UseSelectionAsScanRoot();
                }

                _targetPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "대상 프리팹",
                    _targetPrefab,
                    typeof(GameObject),
                    false);

                _containerName = EditorGUILayout.TextField("컨테이너 이름", _containerName);
                EditorGUILayout.LabelField(
                    string.IsNullOrWhiteSpace(_containerName)
                        ? "컨테이너 이름이 비어 있으면 스캔 루트의 직계 자식을 감지합니다."
                        : "스캔 루트 하위에서 같은 이름의 컨테이너를 찾고, 그 직계 자식을 감지합니다.",
                    EditorStyles.miniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("새로 고침", GUILayout.Width(90f)))
                        RefreshDetectedTransforms();

                    using (new EditorGUI.DisabledScope(_detectedTransforms.Count == 0))
                    {
                        if (GUILayout.Button("감지 항목 모두 +", GUILayout.Width(110f)))
                            AddAllDetectedTargets();
                    }

                    using (new EditorGUI.DisabledScope(_applyTargets.Count == 0))
                    {
                        if (GUILayout.Button("적용 목록 비우기", GUILayout.Width(110f)))
                            _applyTargets.Clear();
                    }

                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(_targetPrefab == null))
                    {
                        if (GUILayout.Button("Project에서 프리팹 보기", GUILayout.Width(150f)))
                            PingPrefab();
                    }
                }
            }
        }

        private void DrawTransformLists()
        {
            RefreshDetectedTransformsIfNeeded();

            EditorGUILayout.LabelField($"감지된 Transform {_detectedTransforms.Count}개", EditorStyles.boldLabel);
            _detectedScroll = EditorGUILayout.BeginScrollView(_detectedScroll, GUILayout.MinHeight(120f));

            if (_detectedTransforms.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "스캔 루트 하위에서 적용할 Transform을 찾지 못했습니다.",
                    MessageType.Warning);
            }

            foreach (var info in _detectedTransforms)
                DrawDetectedTransform(info);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"적용 대상 {_applyTargets.Count}개", EditorStyles.boldLabel);
            _applyScroll = EditorGUILayout.BeginScrollView(_applyScroll, GUILayout.MinHeight(120f));

            if (_applyTargets.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "적용 대상이 없습니다. 감지된 항목에서 + 버튼으로 추가하세요.",
                    MessageType.Info);
            }

            for (int i = _applyTargets.Count - 1; i >= 0; i--)
                DrawApplyTarget(i);

            EditorGUILayout.EndScrollView();
        }

        private void DrawDetectedTransform(ToolTransformInfo info)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(info.Transform, typeof(Transform), true);
                    using (new EditorGUI.DisabledScope(ContainsPath(_applyTargets, info.RelativePath)))
                    {
                        if (GUILayout.Button("+", GUILayout.Width(28f)))
                            AddApplyTarget(info);
                    }
                }

                DrawTransformInfo(info);
            }
        }

        private void DrawApplyTarget(int index)
        {
            var info = _applyTargets[index];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.ObjectField(info.Transform, typeof(Transform), true);
                    if (GUILayout.Button("-", GUILayout.Width(28f)))
                    {
                        _applyTargets.RemoveAt(index);
                        return;
                    }
                }

                DrawTransformInfo(info);
            }
        }

        private static void DrawTransformInfo(ToolTransformInfo info)
        {
            EditorGUILayout.LabelField("Path", info.RelativePath, EditorStyles.miniLabel);
            EditorGUILayout.Vector3Field("Local Position", info.Transform.localPosition);
            EditorGUILayout.Vector3Field("Local Rotation", info.Transform.localEulerAngles);
            EditorGUILayout.Vector3Field("Local Scale", info.Transform.localScale);
        }

        private void DrawApplyButton()
        {
            if (_pathRoot != null && _scanRoot != null && !IsTransformUnderRoot(_pathRoot.transform, _scanRoot.transform))
            {
                EditorGUILayout.HelpBox(
                    "스캔 루트가 씬 기준 루트 하위에 없습니다. 프리팹 경로를 계산할 수 없습니다.",
                    MessageType.Error);
            }

            using (new EditorGUI.DisabledScope(!CanApply()))
            {
                if (GUILayout.Button("감지된 Runtime Transform 값을 대상 프리팹에 반영", GUILayout.Height(34f)))
                    ApplyToPrefab();
            }
        }

        private void OnSelectionChanged()
        {
            ResolveDefaultsFromSelection();
            Repaint();
        }

        private void ResolveDefaultsFromSelection()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected != null)
            {
                SetPathRoot(FindDefaultPathRoot(selected));
                SetScanRoot(selected);
            }

            if (_targetPrefab == null)
            {
                _targetPrefab = ResolvePrefabFromSelection(selected)
                                ?? AssetDatabase.LoadAssetAtPath<GameObject>(DefaultPlayerPrefabPath);
            }

            RefreshDetectedTransforms();
        }

        private void UseSelectionAsPathRoot()
        {
            if (Selection.activeGameObject != null)
                SetPathRoot(Selection.activeGameObject);

            GameObject selectedPrefab = ResolvePrefabFromSelection(Selection.activeGameObject);
            if (selectedPrefab != null)
                _targetPrefab = selectedPrefab;

            RefreshDetectedTransforms();
        }

        private void UseSelectionAsScanRoot()
        {
            if (Selection.activeGameObject != null)
                SetScanRoot(Selection.activeGameObject);

            RefreshDetectedTransforms();
        }

        private void SetPathRoot(GameObject pathRoot)
        {
            if (_pathRoot == pathRoot)
                return;

            _pathRoot = pathRoot;
            _applyTargets.Clear();
        }

        private void SetScanRoot(GameObject scanRoot)
        {
            if (_scanRoot == scanRoot)
                return;

            _scanRoot = scanRoot;
            _applyTargets.Clear();
        }

        private void RefreshDetectedTransformsIfNeeded()
        {
            if (_detectedTransforms.Count == 0 && _pathRoot != null && _scanRoot != null)
                RefreshDetectedTransforms();
        }

        private void RefreshDetectedTransforms()
        {
            _detectedTransforms.Clear();

            if (_pathRoot == null || _scanRoot == null)
                return;

            if (!IsTransformUnderRoot(_pathRoot.transform, _scanRoot.transform))
                return;

            if (string.IsNullOrWhiteSpace(_containerName))
            {
                AddDirectChildren(_scanRoot.transform);
                return;
            }

            foreach (Transform transform in _scanRoot.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == _containerName)
                    AddDirectChildren(transform);
            }
        }

        private void AddDirectChildren(Transform container)
        {
            foreach (Transform child in container)
            {
                if (child == null)
                    continue;

                string path = GetRelativePath(_pathRoot.transform, child);
                if (string.IsNullOrEmpty(path))
                    continue;

                _detectedTransforms.Add(new ToolTransformInfo(child, path));
            }
        }

        private bool CanApply()
        {
            return _pathRoot != null
                   && _scanRoot != null
                   && _targetPrefab != null
                   && _applyTargets.Count > 0
                   && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_targetPrefab));
        }

        private void ApplyToPrefab()
        {
            string prefabPath = AssetDatabase.GetAssetPath(_targetPrefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("[RuntimeTransformSync] 대상 프리팹 경로가 없습니다.");
                return;
            }

            PruneInvalidApplyTargets();
            if (_applyTargets.Count == 0)
            {
                Debug.LogWarning("[RuntimeTransformSync] 반영할 Transform 대상이 없습니다.");
                return;
            }

            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                int applied = 0;
                int missing = 0;

                foreach (var info in _applyTargets)
                {
                    Transform target = FindByRelativePath(prefabRoot.transform, info.RelativePath);
                    if (target == null)
                    {
                        missing++;
                        Debug.LogWarning($"[RuntimeTransformSync] 프리팹에서 경로를 찾지 못했습니다: {info.RelativePath}");
                        continue;
                    }

                    target.localPosition = info.Transform.localPosition;
                    target.localRotation = info.Transform.localRotation;
                    target.localScale = info.Transform.localScale;
                    EditorUtility.SetDirty(target);
                    applied++;
                }

                bool success;
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath, out success);
                if (!success)
                {
                    Debug.LogError($"[RuntimeTransformSync] 프리팹 저장 실패: {prefabPath}");
                    return;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[RuntimeTransformSync] 완료: 적용 {applied}개, 누락 {missing}개 -> {prefabPath}");
            }
            finally
            {
                if (prefabRoot != null)
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private void PingPrefab()
        {
            if (_targetPrefab == null)
                return;

            Selection.activeObject = _targetPrefab;
            EditorGUIUtility.PingObject(_targetPrefab);
        }

        private void AddAllDetectedTargets()
        {
            for (int i = 0; i < _detectedTransforms.Count; i++)
                AddApplyTarget(_detectedTransforms[i]);
        }

        private void AddApplyTarget(ToolTransformInfo info)
        {
            if (info.Transform == null || string.IsNullOrEmpty(info.RelativePath))
                return;

            if (ContainsPath(_applyTargets, info.RelativePath))
                return;

            _applyTargets.Add(info);
        }

        private void PruneInvalidApplyTargets()
        {
            for (int i = _applyTargets.Count - 1; i >= 0; i--)
            {
                if (_applyTargets[i].Transform == null || string.IsNullOrEmpty(_applyTargets[i].RelativePath))
                    _applyTargets.RemoveAt(i);
            }
        }

        private static bool ContainsPath(List<ToolTransformInfo> list, string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].RelativePath == path)
                    return true;
            }

            return false;
        }

        private static GameObject FindDefaultPathRoot(GameObject selected)
        {
            if (selected == null)
                return null;

            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(selected);
            if (prefabRoot != null)
                return prefabRoot;

            return selected.transform.root != null ? selected.transform.root.gameObject : selected;
        }

        private static GameObject ResolvePrefabFromSelection(GameObject selected)
        {
            if (selected == null)
                return null;

            GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(selected);
            if (prefabRoot == null)
                return null;

            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static bool IsTransformUnderRoot(Transform root, Transform target)
        {
            if (root == null || target == null)
                return false;

            Transform current = target;
            while (current != null)
            {
                if (current == root)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null || target == root)
                return string.Empty;

            var names = new Stack<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return current == root ? string.Join("/", names) : string.Empty;
        }

        private static Transform FindByRelativePath(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
                return null;

            Transform current = root;
            string[] parts = path.Split('/');
            for (int i = 0; i < parts.Length; i++)
            {
                current = FindDirectChild(current, parts[i]);
                if (current == null)
                    return null;
            }

            return current;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        private readonly struct ToolTransformInfo
        {
            public readonly Transform Transform;
            public readonly string RelativePath;

            public ToolTransformInfo(Transform transform, string relativePath)
            {
                Transform = transform;
                RelativePath = relativePath;
            }
        }
    }
}
#endif
