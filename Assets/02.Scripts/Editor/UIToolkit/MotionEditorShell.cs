using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.Animation.Editor.UIToolkit
{
    /// <summary>
    /// MotionSet 에디터의 UI Toolkit 셸.
    /// 마이그레이션 중인 IMGUI 영역은 책임별 IMGUIContainer로 격리하고,
    /// 각 Phase에서 컨테이너 하나씩 UI Toolkit 뷰로 교체한다.
    /// </summary>
    internal sealed class MotionEditorShell : IDisposable
    {
        const string CommonStylePath =
            "Assets/02.Scripts/Editor/UIToolkit/Styles/UPlayGroundEditor.uss";
        const string MotionStylePath =
            "Assets/02.Scripts/Editor/UIToolkit/Styles/MotionEditor.uss";
        const string SidebarWidthPrefs = "MotionSetWindow_SidebarWidth";
        const string InspectorWidthPrefs = "MotionSetWindow_InspectorWidth";
        const long SideEffectIntervalMs = 100L;

        readonly VisualElement _root;
        readonly VisualElement _sidebarPane;
        readonly VisualElement _inspectorPane;
        readonly IVisualElementScheduledItem _sideEffectSchedule;

        public MotionEditorShell(
            VisualElement root,
            Action drawToolbar,
            VisualElement controlPanels,
            Action drawPreviewControls,
            VisualElement sidebarContent,
            VisualElement bodyContent,
            VisualElement inspectorContent,
            Action runControlPanelSideEffects)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _root.Clear();
            _root.AddToClassList("up-editor-root");
            _root.AddToClassList(EditorGUIUtility.isProSkin
                ? "up-theme-dark"
                : "up-theme-light");

            StyleSheet commonStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(CommonStylePath);
            if (commonStyle != null)
                _root.styleSheets.Add(commonStyle);
            else
                Debug.LogWarning($"Motion Editor 공통 스타일을 찾을 수 없습니다: {CommonStylePath}");
            StyleSheet motionStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(MotionStylePath);
            if (motionStyle != null)
                _root.styleSheets.Add(motionStyle);
            else
                Debug.LogWarning($"Motion Editor 전용 스타일을 찾을 수 없습니다: {MotionStylePath}");

            _root.Add(CreateIMGUIContainer(drawToolbar, "up-motion-toolbar"));
            if (controlPanels != null)
                _root.Add(controlPanels);
            _root.Add(CreateIMGUIContainer(drawPreviewControls, "up-motion-preview-controls"));

            float sidebarWidth = Mathf.Clamp(
                EditorPrefs.GetFloat(SidebarWidthPrefs, 310f),
                280f,
                480f);
            var mainSplit = new TwoPaneSplitView(
                0,
                sidebarWidth,
                TwoPaneSplitViewOrientation.Horizontal);
            mainSplit.AddToClassList("up-motion-main-split");

            _sidebarPane = new VisualElement();
            _sidebarPane.AddToClassList("up-motion-sidebar");
            _sidebarPane.RegisterCallback<GeometryChangedEvent>(SaveSidebarWidth);
            if (sidebarContent != null)
                _sidebarPane.Add(sidebarContent);
            mainSplit.Add(_sidebarPane);

            float inspectorWidth = Mathf.Clamp(
                EditorPrefs.GetFloat(InspectorWidthPrefs, 320f),
                280f,
                520f);
            var contentSplit = new TwoPaneSplitView(
                1,
                inspectorWidth,
                TwoPaneSplitViewOrientation.Horizontal);
            contentSplit.AddToClassList("up-motion-content-split");

            var bodyPane = new VisualElement();
            bodyPane.AddToClassList("up-motion-body");
            if (bodyContent != null)
                bodyPane.Add(bodyContent);
            contentSplit.Add(bodyPane);

            _inspectorPane = new VisualElement();
            _inspectorPane.AddToClassList("up-motion-inspector-pane");
            _inspectorPane.RegisterCallback<GeometryChangedEvent>(SaveInspectorWidth);
            if (inspectorContent != null)
                _inspectorPane.Add(inspectorContent);
            contentSplit.Add(_inspectorPane);

            mainSplit.Add(contentSplit);
            _root.Add(mainSplit);

            runControlPanelSideEffects?.Invoke();
            if (runControlPanelSideEffects != null)
            {
                _sideEffectSchedule = _root.schedule
                    .Execute(runControlPanelSideEffects)
                    .Every(SideEffectIntervalMs);
            }
        }

        public void SetSidebarVisible(bool visible)
        {
            _sidebarPane.EnableInClassList("up-hidden", !visible);
            _root.EnableInClassList("up-motion-with-sidebar", visible);
        }

        public void Dispose()
        {
            _sideEffectSchedule?.Pause();
            _sidebarPane.UnregisterCallback<GeometryChangedEvent>(SaveSidebarWidth);
            _inspectorPane.UnregisterCallback<GeometryChangedEvent>(SaveInspectorWidth);
        }

        void SaveSidebarWidth(GeometryChangedEvent evt)
        {
            if (_sidebarPane.ClassListContains("up-hidden"))
                return;

            float width = evt.newRect.width;
            if (width >= 280f && width <= 480f)
                EditorPrefs.SetFloat(SidebarWidthPrefs, width);
        }

        void SaveInspectorWidth(GeometryChangedEvent evt)
        {
            float width = evt.newRect.width;
            if (width >= 280f && width <= 520f)
                EditorPrefs.SetFloat(InspectorWidthPrefs, width);
        }

        static IMGUIContainer CreateIMGUIContainer(Action handler, string className)
        {
            var container = new IMGUIContainer(() => handler?.Invoke());
            container.AddToClassList(className);
            return container;
        }
    }
}
