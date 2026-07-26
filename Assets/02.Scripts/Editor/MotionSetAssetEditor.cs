using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Animation.Editor.UIToolkit;
using UPlayGround.Animation.Editor.UIToolkit.Timeline;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// MotionSetAsset 커스텀 인스펙터.
    /// MotionSetDrawer를 사용해 인스펙터 안에서 직접 편집할 수 있습니다.
    /// </summary>
    [CustomEditor(typeof(MotionSetAsset))]
    public class MotionSetAssetEditor : UnityEditor.Editor
    {
        MotionSetDrawer _drawer;
        MotionEventInspectorView _inspectorView;
        TimelineView _timelineView;
        IMGUIContainer _authoringContainer;

        void OnEnable()
        {
            _drawer = new MotionSetDrawer(
                () => target,       // Undo/Dirty 대상 = 에셋 자체
                Repaint             // 리페인트 콜백
            );
            _drawer.onSelectionChanged = QueueInspectorRefresh;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.AddToClassList("up-editor-root");
            root.AddToClassList("up-motion-asset-inspector");

            AddStyle(root, "Assets/02.Scripts/Editor/UIToolkit/Styles/UPlayGroundEditor.uss");
            AddStyle(root, "Assets/02.Scripts/Editor/UIToolkit/Styles/MotionEditor.uss");

            _inspectorView = new MotionEventInspectorView(Repaint);
            var authoring = new Foldout { text = "MotionSet 구성 · 이벤트 목록", value = false };
            _authoringContainer = new IMGUIContainer(DrawMotionEditorBody);
            authoring.Add(_authoringContainer);
            root.Add(authoring);

            _timelineView = new TimelineView(
                () => (target as MotionSetAsset)?.motionSet,
                () => _drawer,
                () => target,
                HandleTimelineChanged,
                null);
            root.Add(_timelineView);
            root.Add(_inspectorView);

            var openButton = new Button(OpenEditorWindow) { text = "에디터 창에서 열기" };
            openButton.AddToClassList("up-open-motion-editor");
            root.Add(openButton);

            RefreshInspector();
            return root;
        }

        void DrawMotionEditorBody()
        {
            var asset = (MotionSetAsset)target;
            if (asset == null)
                return;

            EnsureMotionSet(asset);
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            _drawer.DrawFullGUI(asset.motionSet);
            EditorGUILayout.Space(4);
            _drawer.DrawEventsGUI(asset.motionSet);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }
        }

        void QueueInspectorRefresh()
        {
            _timelineView?.RefreshData(true);
            _inspectorView?.schedule.Execute(RefreshInspector).StartingIn(0);
        }

        void HandleTimelineChanged()
        {
            _authoringContainer?.MarkDirtyRepaint();
            QueueInspectorRefresh();
        }

        void RefreshInspector()
        {
            _inspectorView?.Refresh(target as MotionSetAsset, _drawer);
        }

        static void AddStyle(VisualElement root, string path)
        {
            StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            if (style != null)
                root.styleSheets.Add(style);
        }

        static void EnsureMotionSet(MotionSetAsset asset)
        {
            if (asset.motionSet != null)
                return;

            Undo.RecordObject(asset, "Init MotionSet");
            asset.motionSet = MotionSet.CreateAuthored(asset.name);
            EditorUtility.SetDirty(asset);
        }

        void OpenEditorWindow()
        {
            var asset = (MotionSetAsset)target;
            if (asset == null)
                return;

            Selection.activeObject = asset;
            var window = EditorWindow.GetWindow<MotionSetEditorWindow>();
            window.titleContent = new GUIContent("애니메이션 에디터");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

    }
}
