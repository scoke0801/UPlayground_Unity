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
    public sealed class MotionEditorShell : IDisposable
    {
        const string CommonStylePath =
            "Assets/02.Scripts/Editor/UIToolkit/Styles/UPlayGroundEditor.uss";
        const string MotionStylePath =
            "Assets/02.Scripts/MotionSet/Editor/Styles/MotionEditor.uss";
        const string SidebarWidthPrefs = "MotionSetWindow_SidebarWidth";
        const string InspectorWidthPrefs = "MotionSetWindow_InspectorWidth";
        const long SideEffectIntervalMs = 100L;
        const float MinTimelineWidth = 420f;

        readonly VisualElement _root;
        readonly VisualElement _sidebarPane;
        readonly VisualElement _inspectorPane;
        readonly IVisualElementScheduledItem _sideEffectSchedule;
        bool _sidebarRequestedVisible = true;

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
            _root.RegisterCallback<GeometryChangedEvent>(UpdateResponsiveLayout);

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
            _sidebarRequestedVisible = visible;
            ApplyResponsiveLayout(_root.resolvedStyle.width);
        }

        public void Dispose()
        {
            _sideEffectSchedule?.Pause();
            _root.UnregisterCallback<GeometryChangedEvent>(UpdateResponsiveLayout);
            _sidebarPane.UnregisterCallback<GeometryChangedEvent>(SaveSidebarWidth);
            _inspectorPane.UnregisterCallback<GeometryChangedEvent>(SaveInspectorWidth);
        }

        void UpdateResponsiveLayout(GeometryChangedEvent evt)
        {
            ApplyResponsiveLayout(evt.newRect.width);
        }

        /// <summary>
        /// 사이드바와 인스펙터를 모두 접어도 타임라인이 쓸 만한 폭을 남기지 못할 때만
        /// 인스펙터를 접는다. minSize(560)와 저장된 패널 폭을 기준으로 하므로,
        /// 도킹된 일반 크기(800~1200px)에서 인스펙터가 사라지지 않는다.
        /// </summary>
        void ApplyResponsiveLayout(float width)
        {
            bool sidebarVisible = _sidebarRequestedVisible;
            // 숨겨진 패널의 resolvedStyle.width는 0이라 이를 기준으로 판단하면
            // 숨김↔표시가 GeometryChangedEvent로 무한 반복된다. 저장된 희망 폭을 쓴다.
            float reserved =
                (sidebarVisible ? EditorPrefs.GetFloat(SidebarWidthPrefs, 310f) : 0f) +
                EditorPrefs.GetFloat(InspectorWidthPrefs, 320f);
            bool hideInspector = width > 0f &&
                                 width - reserved < MinTimelineWidth;
            _sidebarPane.EnableInClassList("up-hidden", !sidebarVisible);
            _inspectorPane.EnableInClassList("up-hidden", hideInspector);
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
