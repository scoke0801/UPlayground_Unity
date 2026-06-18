#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UPlayGround.Debugging;

namespace UPlayGround.Debugging.Editor
{
    public class DebugGizmoWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("UPlayGround/Debug/Debug Gizmo Window")]
        public static void Open()
        {
            var window = GetWindow<DebugGizmoWindow>();
            window.titleContent = new GUIContent("Debug Gizmo", EditorGUIUtility.IconContent("d_DebuggerEnabled").image);
            window.minSize = new Vector2(320f, 260f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            DrawHeader();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode에서 DebugGizmoManager를 사용할 수 있습니다.", MessageType.Info);
                return;
            }

            // Instance 접근은 매니저가 없을 때 싱글톤을 새로 생성하는 부작용이 있으므로,
            // 창을 여는 행위만으로 매니저가 만들어지지 않도록 조회만 수행한다.
            DebugGizmoManager manager = FindFirstObjectByType<DebugGizmoManager>();
            if (manager == null)
            {
                EditorGUILayout.HelpBox("DebugGizmoManager를 찾을 수 없습니다.", MessageType.Warning);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawGlobalOptions(manager);
            EditorGUILayout.Space(8);
            DrawCategories(manager);
            EditorGUILayout.Space(8);
            DrawContentTypes(manager);
            EditorGUILayout.Space(8);
            DrawFocus(manager);
            EditorGUILayout.Space(8);
            DrawProviders(manager);
            EditorGUILayout.Space(8);
            DrawRecorder(manager);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawHeader()
        {
            Rect rect = GUILayoutUtility.GetRect(0, 34, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.2f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 20f), "Debug Gizmo", EditorStyles.boldLabel);
        }

        private static void DrawGlobalOptions(DebugGizmoManager manager)
        {
            EditorGUILayout.LabelField("Global", EditorStyles.boldLabel);
            manager.SetEnabled(EditorGUILayout.Toggle("Enabled", manager.Enabled));
            manager.SetDrawLabels(EditorGUILayout.Toggle("Draw Labels", manager.DrawLabels));
            manager.SetDrawOnlyFocus(EditorGUILayout.Toggle("Draw Only Focus", manager.DrawOnlyFocus));
            manager.SetMaxDrawDistance(EditorGUILayout.FloatField("Max Draw Distance", manager.MaxDrawDistance));
        }

        private static void DrawCategories(DebugGizmoManager manager)
        {
            EditorGUILayout.LabelField("Categories", EditorStyles.boldLabel);
            DrawCategoryToggle(manager, DebugGizmoCategory.Combat);
            DrawCategoryToggle(manager, DebugGizmoCategory.AI);
            DrawCategoryToggle(manager, DebugGizmoCategory.Movement);
            DrawCategoryToggle(manager, DebugGizmoCategory.Camera);
            DrawCategoryToggle(manager, DebugGizmoCategory.Projectile);
            DrawCategoryToggle(manager, DebugGizmoCategory.SpawnGroup);
            DrawCategoryToggle(manager, DebugGizmoCategory.Animation);
        }

        private static void DrawCategoryToggle(DebugGizmoManager manager, DebugGizmoCategory category)
        {
            bool value = manager.IsCategoryEnabled(category);
            bool next = EditorGUILayout.Toggle(category.ToString(), value);
            if (next != value)
                manager.SetCategory(category, next);
        }

        private static void DrawContentTypes(DebugGizmoManager manager)
        {
            EditorGUILayout.LabelField("Content Types", EditorStyles.boldLabel);
            DrawContentTypeToggle(manager, DebugGizmoContentType.PlayerCombatHit);
            DrawContentTypeToggle(manager, DebugGizmoContentType.EnemyDetection);
            DrawContentTypeToggle(manager, DebugGizmoContentType.MotionWarp);
        }

        private static void DrawContentTypeToggle(DebugGizmoManager manager, DebugGizmoContentType contentType)
        {
            bool value = manager.IsContentTypeEnabled(contentType);
            bool next = EditorGUILayout.Toggle(contentType.ToString(), value);
            if (next != value)
                manager.SetContentType(contentType, next);
        }

        private static void DrawFocus(DebugGizmoManager manager)
        {
            EditorGUILayout.LabelField("Focus", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.ObjectField("Focus Object", manager.FocusObject, typeof(GameObject), true);
                if (GUILayout.Button("Selection", GUILayout.Width(80f)))
                    manager.SetFocusObject(Selection.activeGameObject);
                if (GUILayout.Button("Clear", GUILayout.Width(55f)))
                    manager.SetFocusObject(null);
            }
        }

        private static void DrawProviders(DebugGizmoManager manager)
        {
            EditorGUILayout.LabelField("Providers", EditorStyles.boldLabel);
            int visibleCount = 0;
            for (int i = 0; i < manager.Providers.Count; i++)
            {
                if (manager.PassesProviderFilters(manager.Providers[i], false))
                    visibleCount++;
            }

            EditorGUILayout.LabelField("Visible Providers", $"{visibleCount} / {manager.Providers.Count}");

            using (new EditorGUI.DisabledScope(true))
            {
                for (int i = 0; i < manager.Providers.Count; i++)
                {
                    IDebugGizmoProvider provider = manager.Providers[i];
                    if (provider == null)
                        continue;

                    bool visible = manager.PassesProviderFilters(provider, false);
                    string state = visible ? "ON" : "OFF";
                    EditorGUILayout.TextField(state, manager.GetProviderDisplayName(provider));
                }
            }
        }

        private static void DrawRecorder(DebugGizmoManager manager)
        {
            EditorGUILayout.LabelField("Recorder", EditorStyles.boldLabel);
            bool recording = manager.Recorder.IsRecording;
            bool next = EditorGUILayout.Toggle("Record Frames", recording);
            if (next != recording)
                manager.Recorder.SetRecording(next);

            EditorGUILayout.LabelField("Recorded Frames", manager.Recorder.Frames.Count.ToString());
            if (GUILayout.Button("Clear Snapshots"))
                manager.Recorder.Clear();
        }
    }
}
#endif
