#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.Data.Editor.Authoring
{
    /// <summary>
    /// 데이터 저작 허브 전용 색상과 공통 표면 스타일입니다.
    /// </summary>
    internal static class DataAuthoringTheme
    {
        public static Color Window => EditorGUIUtility.isProSkin
            ? new Color(0.075f, 0.085f, 0.10f)
            : new Color(0.82f, 0.84f, 0.87f);

        public static Color Surface => EditorGUIUtility.isProSkin
            ? new Color(0.105f, 0.12f, 0.145f)
            : new Color(0.91f, 0.92f, 0.94f);

        public static Color SurfaceRaised => EditorGUIUtility.isProSkin
            ? new Color(0.14f, 0.155f, 0.18f)
            : new Color(0.97f, 0.97f, 0.98f);

        public static Color Border => EditorGUIUtility.isProSkin
            ? new Color(0.25f, 0.28f, 0.33f, 0.8f)
            : new Color(0.54f, 0.57f, 0.62f, 0.65f);

        public static Color Muted => EditorGUIUtility.isProSkin
            ? new Color(0.62f, 0.66f, 0.72f)
            : new Color(0.34f, 0.37f, 0.42f);

        public static Color Accent => new Color(0.27f, 0.52f, 0.96f);
        public static Color Error => new Color(0.95f, 0.30f, 0.24f);
        public static Color Warning => new Color(1f, 0.67f, 0.16f);
        public static Color Success => new Color(0.28f, 0.72f, 0.35f);

        public static void SetBorder(VisualElement element, Color? color = null, float width = 1f)
        {
            Color resolved = color ?? Border;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftColor = resolved;
            element.style.borderRightColor = resolved;
            element.style.borderTopColor = resolved;
            element.style.borderBottomColor = resolved;
        }

        public static void Round(VisualElement element, float radius = 4f)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        public static void StyleButton(Button button, bool primary = false)
        {
            button.style.height = 30f;
            button.style.paddingLeft = 12f;
            button.style.paddingRight = 12f;
            button.style.marginLeft = 3f;
            button.style.marginRight = 3f;
            button.style.backgroundColor = primary ? Accent : SurfaceRaised;
            if (primary)
                button.style.color = Color.white;
            SetBorder(button, primary ? Accent : Border);
            Round(button, 4f);
        }

        public static void StyleBadge(Label badge, Color color)
        {
            badge.style.minWidth = 22f;
            badge.style.height = 20f;
            badge.style.paddingLeft = 6f;
            badge.style.paddingRight = 6f;
            badge.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.style.fontSize = 10f;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            badge.style.color = Color.white;
            badge.style.backgroundColor = color;
            Round(badge, 9f);
        }
    }
}
#endif
