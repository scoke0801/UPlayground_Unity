using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.EditorTools
{
    /// <summary>
    /// UPlayGround 툴 런처가 자동 발견할 에디터 도구 실행 메서드를 표시한다.
    /// Unity 상단 메뉴에는 노출하지 않으며, 정적 매개변수 없는 메서드에만 사용한다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class UPlaygroundToolAttribute : Attribute
    {
        public string Id { get; }
        public bool IsValidateFunction { get; }

        // 기존 MenuItem의 명명 인수와 호환해 기계적인 이관이 가능하도록 유지한다.
        public int priority { get; set; }

        public UPlaygroundToolAttribute(string id)
            : this(id, false, 0)
        {
        }

        public UPlaygroundToolAttribute(string id, bool isValidateFunction)
            : this(id, isValidateFunction, 0)
        {
        }

        public UPlaygroundToolAttribute(string id, bool isValidateFunction, int priority)
        {
            Id = id;
            IsValidateFunction = isValidateFunction;
            this.priority = priority;
        }
    }

    /// <summary>
    /// 프로젝트 에디터 도구가 같은 시각 언어와 점진적 UI Toolkit 이식 경로를 사용하도록 돕는다.
    /// 기존 IMGUI 기능은 IMGUIContainer에 그대로 보존하고, 창 크롬만 공통화할 수 있다.
    /// </summary>
    public static class UPlaygroundEditorUX
    {
        public const string CommonStylePath =
            "Assets/02.Scripts/Editor/UIToolkit/Styles/UPlayGroundEditor.uss";

        public static void PrepareRoot(VisualElement root, string toolClass = null)
        {
            if (root == null)
                return;

            // 이전 버전의 공통 초기화가 root.styleSheets.Clear()를 호출해
            // 이미 열려 있던 창의 Editor 기본 폰트/컨트롤 스타일을 제거했다.
            // 새 창과 손상된 기존 창 모두 안전하도록 내장 스타일을 중복 없이 복구한다.
            AddBuiltInStyle(root, "StyleSheets/Extensions/base/common.uss");
            if (!EditorGUIUtility.isProSkin)
                AddBuiltInStyle(root, "StyleSheets/Extensions/base/light.uss");
            AddBuiltInStyle(root, EditorGUIUtility.isProSkin
                ? "StyleSheets/Generated/ToolbarDark.uss"
                : "StyleSheets/Generated/ToolbarLight.uss");

            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(CommonStylePath);
            // root에는 Unity Editor 기본 ThemeStyleSheet가 연결되어 있다.
            // Clear하면 폰트와 기본 컨트롤 레이아웃까지 사라지므로 기존 시트를 반드시 보존한다.
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
                root.styleSheets.Add(styleSheet);

            root.AddToClassList("up-editor-root");
            root.AddToClassList(EditorGUIUtility.isProSkin ? "up-theme-dark" : "up-theme-light");
            if (!string.IsNullOrWhiteSpace(toolClass))
                root.AddToClassList(toolClass);
        }

        private static void AddBuiltInStyle(VisualElement root, string resourcePath)
        {
            StyleSheet styleSheet = EditorGUIUtility.Load(resourcePath) as StyleSheet;
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
                root.styleSheets.Add(styleSheet);
        }

        public static VisualElement CreateHeader(
            string title,
            string description,
            string iconName,
            string badge = "UI TOOLKIT")
        {
            var header = new VisualElement();
            header.AddToClassList("up-tool-header");

            var icon = new Image
            {
                image = EditorGUIUtility.IconContent(iconName).image,
                scaleMode = ScaleMode.ScaleToFit,
            };
            icon.AddToClassList("up-tool-header__icon");
            header.Add(icon);

            var copy = new VisualElement();
            copy.AddToClassList("up-tool-header__copy");
            var titleRow = new VisualElement();
            titleRow.AddToClassList("up-tool-header__title-row");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("up-tool-header__title");
            titleRow.Add(titleLabel);

            if (!string.IsNullOrWhiteSpace(badge))
            {
                var badgeLabel = new Label(badge);
                badgeLabel.AddToClassList("up-tool-header__badge");
                titleRow.Add(badgeLabel);
            }

            copy.Add(titleRow);
            var descriptionLabel = new Label(description);
            descriptionLabel.AddToClassList("up-tool-header__description");
            copy.Add(descriptionLabel);
            header.Add(copy);
            return header;
        }

        public static void BuildLegacyWindow(
            VisualElement root,
            string title,
            string description,
            string iconName,
            Action drawGui,
            string toolClass = null)
        {
            root.Clear();
            PrepareRoot(root, toolClass);
            root.AddToClassList("up-legacy-tool");
            root.Add(CreateHeader(title, description, iconName, "HYBRID UI"));

            var host = new IMGUIContainer(() => drawGui?.Invoke());
            host.AddToClassList("up-legacy-host");
            root.Add(host);
        }
    }
}
