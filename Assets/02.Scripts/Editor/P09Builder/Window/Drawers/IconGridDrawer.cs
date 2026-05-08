using System.Collections.Generic;
using System.Text.RegularExpressions;
using P09.Modular.Humanoid.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    /// <summary>
    /// ScriptableObject 리스트를 아이콘 그리드 형태로 그리는 GUI 헬퍼.
    /// 선택된 항목은 강조 배경, 아이콘이 없으면 SO 이름을 라벨로 대체한다.
    /// </summary>
    public static class IconGridDrawer
    {
        private const float CellGap = 4f;
        private static readonly Regex _trailingNumber = new Regex(@"(\d+)\s*$", RegexOptions.Compiled);

        public static ScriptableObject Draw(
            IReadOnlyList<ScriptableObject> items,
            ScriptableObject selected,
            IconResolver iconResolver,
            int columns = 8,
            float iconSize = 52f,
            bool allowNone = true,
            string noneLabel = "None",
            BuilderSex? preferredSex = null)
        {
            if (items == null) items = System.Array.Empty<ScriptableObject>();
            if (columns < 1) columns = 1;
            iconSize = Mathf.Max(48f, iconSize);

            var newSelection = selected;

            // (None) 버튼이 있으면 +1 슬롯
            int totalSlots = items.Count + (allowNone ? 1 : 0);
            int rows = Mathf.CeilToInt(totalSlots / (float)columns);

            for (int r = 0; r < rows; r++)
            {
                using (new EditorGUILayout.HorizontalScope(GUILayout.Height(iconSize + CellGap)))
                {
                    for (int c = 0; c < columns; c++)
                    {
                        int slotIdx = r * columns + c;
                        if (slotIdx >= totalSlots) break;

                        // 첫 슬롯에 None 처리
                        if (allowNone && slotIdx == 0)
                        {
                            if (DrawCell(null, null, selected == null, iconSize, noneLabel, noneLabel))
                                newSelection = null;
                            continue;
                        }

                        int itemIdx = allowNone ? slotIdx - 1 : slotIdx;
                        if (itemIdx < 0 || itemIdx >= items.Count) continue;

                        var so = items[itemIdx];
                        bool isSelected = (so == selected);
                        var icon = iconResolver != null ? iconResolver.GetIcon(so, preferredSex) : null;
                        var label = MakeShortLabel(so, itemIdx);
                        var tooltip = so != null ? so.name : string.Empty;

                        if (DrawCell(so, icon, isSelected, iconSize, label, tooltip))
                            newSelection = so;
                    }
                    GUILayout.FlexibleSpace();
                }
            }

            return newSelection;
        }

        private static bool DrawCell(ScriptableObject so, Texture2D icon, bool isSelected,
            float size, string label, string tooltip)
        {
            var rect = GUILayoutUtility.GetRect(size, size,
                GUILayout.Width(size), GUILayout.Height(size));

            var evt = Event.current;
            bool clicked = evt.type == EventType.MouseDown &&
                           evt.button == 0 &&
                           rect.Contains(evt.mousePosition);
            if (clicked)
                evt.Use();

            var bg = isSelected
                ? (EditorGUIUtility.isProSkin ? new Color(0.22f, 0.42f, 0.75f, 1f) : new Color(0.62f, 0.78f, 1f, 1f))
                : (EditorGUIUtility.isProSkin ? new Color(0.24f, 0.24f, 0.24f, 1f) : new Color(0.72f, 0.72f, 0.72f, 1f));
            EditorGUI.DrawRect(rect, bg);

            var contentRect = new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 18f);
            if (icon != null)
            {
                GUI.DrawTexture(contentRect, icon, ScaleMode.ScaleToFit, true);
            }
            else
            {
                var fallbackRect = new Rect(rect.x + 4f, rect.y + 6f, rect.width - 8f, rect.height - 26f);
                EditorGUI.DrawRect(fallbackRect, new Color(0f, 0f, 0f, 0.12f));
                GUI.Label(fallbackRect, label, CenteredCellLabelStyle);
            }

            var labelRect = new Rect(rect.x + 2f, rect.yMax - 16f, rect.width - 4f, 14f);
            GUI.Label(labelRect, label, FooterLabelStyle);

            DrawRectBorder(rect, isSelected
                ? new Color(0.30f, 0.60f, 1f, 1f)
                : new Color(0f, 0f, 0f, 0.35f), isSelected ? 2f : 1f);

            GUI.Label(rect, new GUIContent(string.Empty, tooltip));
            return clicked;
        }

        private static GUIStyle CenteredCellLabelStyle
        {
            get
            {
                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip,
                    wordWrap = false,
                    fontSize = 10,
                };
                return style;
            }
        }

        private static GUIStyle FooterLabelStyle
        {
            get
            {
                var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    clipping = TextClipping.Clip,
                    wordWrap = false,
                    fontSize = 9,
                };
                return style;
            }
        }

        private static string MakeShortLabel(ScriptableObject so, int index)
        {
            if (so == null)
                return "None";

            if (so is IEditPartData data && data.ContentId > 0)
                return data.ContentId.ToString("00");

            var match = _trailingNumber.Match(so.name);
            if (match.Success)
                return match.Groups[1].Value.PadLeft(2, '0');

            return (index + 1).ToString("00");
        }

        private static void DrawRectBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private static bool DrawIconButton(ScriptableObject so, Texture2D icon, bool isSelected, float size)
        {
            var prevColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.30f, 0.55f, 1.0f, 1f)
                    : new Color(0.55f, 0.75f, 1.0f, 1f);

            bool clicked;
            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));

            if (icon != null)
            {
                clicked = GUI.Button(rect, new GUIContent(icon, so != null ? so.name : ""), GUI.skin.button);
            }
            else
            {
                var label = so != null ? ShortenName(so.name) : "?";
                var content = new GUIContent(label, so != null ? so.name : "");
                clicked = GUI.Button(rect, content, EditorStyles.miniButton);
            }

            GUI.backgroundColor = prevColor;
            return clicked;
        }

        private static bool DrawNoneButton(bool isSelected, float size, string noneLabel)
        {
            var prevColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.30f, 0.55f, 1.0f, 1f)
                    : new Color(0.55f, 0.75f, 1.0f, 1f);

            var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
            bool clicked = GUI.Button(rect, new GUIContent(noneLabel), EditorStyles.miniButton);

            GUI.backgroundColor = prevColor;
            return clicked;
        }

        private static string ShortenName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            if (name.Length <= 8) return name;
            return name.Substring(0, 6) + "..";
        }
    }
}
