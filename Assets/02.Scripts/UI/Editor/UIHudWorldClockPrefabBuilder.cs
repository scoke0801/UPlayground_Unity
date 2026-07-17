using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.HUD.EditorTools
{
    /// <summary>
    /// 인게임 시계 HUD(UI_HudWorldClock) 프리팹을 코드로 생성/재구성하는 에디터 툴.
    ///
    /// - 프리팹이 없으면 새로 만들고, 있으면 루트/스크립트(guid)는 유지한 채 자식 계층만 재구성한다.
    /// - 좌상단 미니맵(240x240) 바로 아래에 배치: 시간대 색상 점 + HH:MM + "N일차 · 시간대".
    /// - 빌드 후 UIPrefabDatabase에 Key "HudWorldClock" / Default Layer "HUD"로 수동 등록해야 한다.
    /// </summary>
    public static class UIHudWorldClockPrefabBuilder
    {
        private const string PrefabDir = "Assets/03.Prefabs/UI/HUD";
        private const string PrefabPath = PrefabDir + "/UI_HudWorldClock.prefab";

        private static readonly Color PanelBg  = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color TextMain = new Color(0.94f, 0.93f, 0.88f, 1f);
        private static readonly Color TextSub  = new Color(0.72f, 0.72f, 0.68f, 1f);

        [MenuItem("UPlayGround/UI/인게임 시계 HUD 프리팹 빌드")]
        public static void Build()
        {
            EnsureFolder(PrefabDir);

            GameObject root;
            bool isNew = !File.Exists(PrefabPath);

            if (isNew)
            {
                root = new GameObject("UI_HudWorldClock",
                    typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
                root.AddComponent<UI_HudWorldClock>();
            }
            else
            {
                root = PrefabUtility.LoadPrefabContents(PrefabPath);
            }

            try
            {
                var clock = root.GetComponent<UI_HudWorldClock>();
                if (clock == null)
                {
                    Debug.LogError("[HudWorldClockBuilder] 루트에 UI_HudWorldClock 컴포넌트가 없습니다. 중단.");
                    return;
                }

                ClearChildren(root.transform);

                // 패널: 좌상단, 미니맵(약 y 25~265) 바로 아래
                var panel = NewUI("Panel", root.transform);
                var panelRt = panel.GetComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0f, 1f);
                panelRt.anchorMax = new Vector2(0f, 1f);
                panelRt.pivot = new Vector2(0f, 1f);
                panelRt.anchoredPosition = new Vector2(24f, -280f);
                panelRt.sizeDelta = new Vector2(238f, 56f);
                var panelImage = panel.AddComponent<Image>();
                panelImage.color = PanelBg;
                panelImage.raycastTarget = false;

                // 시간대 색상 점
                var icon = NewUI("PeriodIcon", panel.transform);
                var iconRt = icon.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(14f, 0f);
                iconRt.sizeDelta = new Vector2(14f, 14f);
                var iconImage = icon.AddComponent<Image>();
                iconImage.color = new Color(1f, 0.92f, 0.55f, 1f);
                iconImage.raycastTarget = false;

                // 시각 (HH:MM)
                var timeGo = NewUI("TimeText", panel.transform);
                var timeRt = timeGo.GetComponent<RectTransform>();
                timeRt.anchorMin = new Vector2(0f, 0f);
                timeRt.anchorMax = new Vector2(0f, 1f);
                timeRt.pivot = new Vector2(0f, 0.5f);
                timeRt.anchoredPosition = new Vector2(38f, 0f);
                timeRt.sizeDelta = new Vector2(96f, 0f);
                var timeText = timeGo.AddComponent<TextMeshProUGUI>();
                timeText.text = "08:00";
                timeText.fontSize = 30;
                timeText.color = TextMain;
                timeText.alignment = TextAlignmentOptions.MidlineLeft;
                timeText.raycastTarget = false;

                // 일차 · 시간대
                var dayGo = NewUI("DayText", panel.transform);
                var dayRt = dayGo.GetComponent<RectTransform>();
                dayRt.anchorMin = new Vector2(0f, 0f);
                dayRt.anchorMax = new Vector2(1f, 1f);
                dayRt.pivot = new Vector2(1f, 0.5f);
                dayRt.offsetMin = new Vector2(136f, 0f);
                dayRt.offsetMax = new Vector2(-12f, 0f);
                var dayText = dayGo.AddComponent<TextMeshProUGUI>();
                dayText.text = "1일차 · 낮";
                dayText.fontSize = 18;
                dayText.color = TextSub;
                dayText.alignment = TextAlignmentOptions.MidlineRight;
                dayText.raycastTarget = false;

                // ── 필드 연결 ──
                var so = new SerializedObject(clock);
                SetRef(so, "_timeText", timeText);
                SetRef(so, "_dayText", dayText);
                SetRef(so, "_periodIcon", iconImage);
                SetEnum(so, "_layer", (int)CanvasLayer.HUD);
                SetBool(so, "_canCloseWithEsc", false);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

                Debug.Log($"[HudWorldClockBuilder] UI_HudWorldClock 프리팹 생성 완료: {PrefabPath}\n" +
                          "UIPrefabDatabase에 Key 'HudWorldClock' / Default Layer 'HUD'로 등록하세요.");
            }
            finally
            {
                if (isNew)
                    UnityEngine.Object.DestroyImmediate(root);
                else
                    PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        // ──────────────────────────────────────────────────────────
        #region 헬퍼

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetRef(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            var property = so.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
            else Debug.LogWarning($"[HudWorldClockBuilder] 직렬화 필드 '{propertyName}'을 찾지 못했습니다.");
        }

        private static void SetEnum(SerializedObject so, string propertyName, int value)
        {
            var property = so.FindProperty(propertyName);
            if (property != null) property.intValue = value;
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            var property = so.FindProperty(propertyName);
            if (property != null) property.boolValue = value;
        }

        #endregion
    }
}
