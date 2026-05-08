using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    /// <summary>
    /// 색상 항목(헤어 컬러 / 눈 컬러 / 피부 등)을 가로로 나열하는 32x32 스워치 드로어.
    /// 아이콘 텍스처가 있으면 그것을 표시하고, 없으면 빈 사각형을 표시한다.
    /// 선택된 항목은 두꺼운 테두리로 강조된다.
    /// </summary>
    public static class ColorSwatchDrawer
    {
        private const float SWATCH_SIZE = 32f;

        public static ScriptableObject Draw(
            IReadOnlyList<ScriptableObject> items,
            ScriptableObject selected,
            IconResolver iconResolver,
            bool allowNone = false)
        {
            if (items == null) items = System.Array.Empty<ScriptableObject>();
            var newSelection = selected;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (allowNone)
                {
                    if (DrawSwatch(null, null, selected == null, "None"))
                        newSelection = null;
                }

                for (int i = 0; i < items.Count; i++)
                {
                    var so = items[i];
                    if (so == null) continue;

                    var icon = iconResolver != null ? iconResolver.GetIcon(so) : null;
                    if (DrawSwatch(so, icon, so == selected, so.name))
                        newSelection = so;
                }

                GUILayout.FlexibleSpace();
            }

            return newSelection;
        }

        private static bool DrawSwatch(ScriptableObject so, Texture2D icon, bool isSelected, string tooltip)
        {
            var rect = GUILayoutUtility.GetRect(SWATCH_SIZE, SWATCH_SIZE,
                GUILayout.Width(SWATCH_SIZE), GUILayout.Height(SWATCH_SIZE));

            // 배경 (아이콘 또는 회색)
            if (icon != null)
            {
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleAndCrop);
            }
            else
            {
                EditorGUI.DrawRect(rect, new Color(0.4f, 0.4f, 0.4f, 1f));
                if (so != null)
                {
                    var labelRect = new Rect(rect.x, rect.y + rect.height * 0.5f - 6f, rect.width, 12f);
                    GUI.Label(labelRect, "?", EditorStyles.centeredGreyMiniLabel);
                }
            }

            // 선택 테두리
            if (isSelected)
            {
                var borderColor = EditorGUIUtility.isProSkin
                    ? new Color(0.30f, 0.55f, 1.0f, 1f)
                    : new Color(0.10f, 0.30f, 0.85f, 1f);
                DrawRectBorder(rect, borderColor, 2f);
            }
            else
            {
                DrawRectBorder(rect, new Color(0f, 0f, 0f, 0.4f), 1f);
            }

            // 마우스 입력 처리
            var ev = Event.current;
            bool clicked = false;
            if (ev.type == EventType.MouseDown && ev.button == 0 && rect.Contains(ev.mousePosition))
            {
                clicked = true;
                ev.Use();
            }

            // tooltip
            GUI.Label(rect, new GUIContent(string.Empty, tooltip));

            return clicked;
        }

        private static void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            // top
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            // bottom
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            // left
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            // right
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
    }
}
