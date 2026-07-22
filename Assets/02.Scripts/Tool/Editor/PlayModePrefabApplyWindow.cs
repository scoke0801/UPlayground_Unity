#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Tool.Editor
{
    /// <summary>
    /// PlayMode에서 조정한 프리팹 인스턴스 값을 원본 프리팹 에셋에 즉시 적용하는 보조 창.
    /// 런타임에 스크립트가 바꾼 직렬화 필드도 최대한 잡기 위해 적용 직전에 계층 전체를 Prefab override로 기록한다.
    /// </summary>
    public sealed class PlayModePrefabApplyWindow : EditorWindow
    {
        private readonly List<PrefabTarget> _targets = new();
        private Vector2 _scroll;
        private bool _includeChildPrefabInstances = true;
        private bool _recordHierarchyBeforeApply = true;
        private double _lastRefreshTime;

        private const double RefreshInterval = 0.25;

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/유틸/PlayMode 변경값 프리팹 적용", priority = UPlaygroundMenuPriority.Util)]
        public static void Open()
        {
            var window = GetWindow<PlayModePrefabApplyWindow>("PlayMode Prefab Apply");
            window.minSize = new Vector2(520f, 360f);
            window.Show();
        }

        [MenuItem("GameObject/UPlayGround/PlayMode 변경값 프리팹 적용", priority = 49)]
        private static void ApplySelectionFromGameObjectMenu()
        {
            ApplySelection(true, true);
        }

        [MenuItem("GameObject/UPlayGround/PlayMode 변경값 프리팹 적용", true)]
        private static bool CanApplySelectionFromGameObjectMenu()
        {
            return Application.isPlaying && Selection.gameObjects.Length > 0;
        }

        private void OnEnable()
        {
            Selection.selectionChanged += Refresh;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            Refresh();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= Refresh;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup - _lastRefreshTime < RefreshInterval)
                return;

            _lastRefreshTime = EditorApplication.timeSinceStartup;
            Refresh();
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "PlayMode에서만 사용합니다. PlayMode 중 씬의 프리팹 인스턴스를 선택한 뒤 적용하세요.",
                    MessageType.Info);
                return;
            }

            if (_targets.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "선택된 프리팹 인스턴스가 없습니다. Hierarchy에서 원본 프리팹에 반영할 오브젝트를 선택하세요.",
                    MessageType.Warning);
                return;
            }

            DrawApplyPanel();
            DrawTargetList();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("새로 고침", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    Refresh();

                GUILayout.FlexibleSpace();

                _includeChildPrefabInstances = GUILayout.Toggle(
                    _includeChildPrefabInstances,
                    "하위 프리팹 포함",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(105f));

                _recordHierarchyBeforeApply = GUILayout.Toggle(
                    _recordHierarchyBeforeApply,
                    "계층 값 기록",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(90f));
            }
        }

        private void DrawApplyPanel()
        {
            EditorGUILayout.HelpBox(
                "선택 대상의 현재 PlayMode 값을 원본 프리팹 에셋에 저장합니다. 실행 후 되돌리려면 즉시 Undo 또는 버전 관리 diff를 확인하세요.",
                MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_targets.Count == 0))
                {
                    if (GUILayout.Button("선택 프리팹 모두 적용", GUILayout.Height(30f)))
                        ApplyTargets(_targets, _recordHierarchyBeforeApply);
                }

                if (GUILayout.Button("Project에서 프리팹 보기", GUILayout.Width(150f), GUILayout.Height(30f)))
                    PingFirstPrefabAsset();
            }
        }

        private void DrawTargetList()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"적용 대상 {_targets.Count}개", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (PrefabTarget target in _targets)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(target.Root, typeof(GameObject), true);
                        using (new EditorGUI.DisabledScope(target.Root == null))
                        {
                            if (GUILayout.Button("적용", GUILayout.Width(58f)))
                                ApplyTargets(new[] { target }, _recordHierarchyBeforeApply);
                        }
                    }

                    EditorGUILayout.LabelField("Prefab", target.AssetPath, EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void Refresh()
        {
            _targets.Clear();
            CollectSelectionTargets(_targets, _includeChildPrefabInstances);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Refresh();
            Repaint();
        }

        private void PingFirstPrefabAsset()
        {
            if (_targets.Count == 0)
                return;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(_targets[0].AssetPath);
            if (asset == null)
                return;

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void ApplySelection(bool includeChildPrefabInstances, bool recordHierarchyBeforeApply)
        {
            var targets = new List<PrefabTarget>();
            CollectSelectionTargets(targets, includeChildPrefabInstances);
            ApplyTargets(targets, recordHierarchyBeforeApply);
        }

        private static void ApplyTargets(IReadOnlyList<PrefabTarget> targets, bool recordHierarchyBeforeApply)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[PlayModePrefabApply] PlayMode에서만 적용할 수 있습니다.");
                return;
            }

            int applied = 0;
            foreach (PrefabTarget target in targets)
            {
                if (target.Root == null || string.IsNullOrEmpty(target.AssetPath))
                    continue;

                if (recordHierarchyBeforeApply)
                    RecordHierarchyPrefabModifications(target.Root);

                PrefabUtility.ApplyPrefabInstance(target.Root, InteractionMode.UserAction);
                applied++;
                Debug.Log($"[PlayModePrefabApply] 프리팹 적용 완료: {target.Root.name} -> {target.AssetPath}");
            }

            if (applied > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void CollectSelectionTargets(List<PrefabTarget> targets, bool includeChildPrefabInstances)
        {
            var seen = new HashSet<int>();

            foreach (GameObject selected in Selection.gameObjects)
            {
                if (selected == null)
                    continue;

                AddPrefabRoot(selected, targets, seen);

                if (!includeChildPrefabInstances)
                    continue;

                foreach (Transform child in selected.GetComponentsInChildren<Transform>(true))
                    AddPrefabRoot(child.gameObject, targets, seen);
            }
        }

        private static void AddPrefabRoot(GameObject candidate, List<PrefabTarget> targets, HashSet<int> seen)
        {
            if (candidate == null || !PrefabUtility.IsPartOfPrefabInstance(candidate))
                return;

            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(candidate);
            if (root == null)
                return;

            int instanceId = root.GetInstanceID();
            if (!seen.Add(instanceId))
                return;

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            if (string.IsNullOrEmpty(assetPath))
                return;

            targets.Add(new PrefabTarget(root, assetPath));
        }

        private static void RecordHierarchyPrefabModifications(GameObject root)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(transform.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(transform);

                foreach (UnityEngine.Component component in transform.GetComponents<UnityEngine.Component>())
                {
                    if (component == null)
                        continue;

                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            }
        }

        private readonly struct PrefabTarget
        {
            public readonly GameObject Root;
            public readonly string AssetPath;

            public PrefabTarget(GameObject root, string assetPath)
            {
                Root = root;
                AssetPath = assetPath;
            }
        }
    }
}
#endif
