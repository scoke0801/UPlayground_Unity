using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>
    /// 대화 재생 컨트롤 바(UI_DialogueControlBar) 프리팹 초안을 코드로 생성/재구성하는 에디터 툴.
    ///
    /// - 프리팹이 없으면 새로 만들고, 있으면 루트/스크립트(guid)는 유지한 채 자식 계층만 재구성한다.
    /// - 좌상단 가로 배치의 정지/자동/스킵/이력 버튼 4종. 재실행 가능(idempotent).
    /// - 초안 생성용이며 최종 비주얼 튜닝은 생성된 프리팹에서 수작업으로 마감한다.
    /// - 빌드 후 UIPrefabDatabase에 Key "DialogueControlBar"로 수동 등록해야 한다.
    /// </summary>
    public static class UIDialogueControlBarPrefabBuilder
    {
        private const string PrefabDir = "Assets/03.Prefabs/UI/Dialogue";
        private const string PrefabPath = PrefabDir + "/UI_DialogueControlBar.prefab";

        private static readonly Color BarBackground = new(0f, 0f, 0f, 0.45f);
        private static readonly Color ButtonBackground = new(0.10f, 0.10f, 0.12f, 0.75f);
        private static readonly Color LabelInactive = new(0.82f, 0.80f, 0.74f, 1f);

        private const float ButtonHeight = 48f;
        private const float ButtonSpacing = 8f;

        public static void Build()
        {
            EnsureFolder(PrefabDir);

            GameObject root;
            bool isNew = !File.Exists(PrefabPath);

            if (isNew)
            {
                root = new GameObject("UI_DialogueControlBar",
                    typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
                root.AddComponent<UI_DialogueControlBar>();
            }
            else
            {
                root = PrefabUtility.LoadPrefabContents(PrefabPath);
            }

            try
            {
                var controlBar = root.GetComponent<UI_DialogueControlBar>();
                if (controlBar == null)
                {
                    Debug.LogError("[DialogueControlBarBuilder] 루트에 UI_DialogueControlBar 컴포넌트가 없습니다. 중단.");
                    return;
                }

                // 수동으로 옮겨 둔 바 위치는 재실행해도 잃지 않도록 먼저 읽어 둔다.
                BarPlacement placement = ReadExistingBarPlacement(root.transform);

                ClearChildren(root.transform);

                // 루트는 반드시 화면 전체 stretch여야 한다.
                // UIManager.ShowUI는 인스턴스화 후 RectTransform을 보정하지 않으므로,
                // 루트가 기본 100x100 중앙 박스로 남으면 자식의 앵커가 '화면 중앙'을 기준으로 잡힌다.
                Stretch(root);

                // 바 — 버튼 폭 합에 맞춰 자동으로 늘어난다.
                // 기본 위치는 상단 중앙이다. 좌상단은 미니맵·인게임 시계·퀘스트 트래커가 이미 점유하고 있어
                // 레퍼런스대로 좌상단에 두면 HUD와 겹친다.
                var bar = NewUI("Bar", root.transform);
                var barRt = bar.GetComponent<RectTransform>();
                barRt.anchorMin = placement.AnchorMin;
                barRt.anchorMax = placement.AnchorMax;
                barRt.pivot = placement.Pivot;
                barRt.anchoredPosition = placement.AnchoredPosition;

                var barImage = bar.AddComponent<Image>();
                barImage.color = BarBackground;
                barImage.raycastTarget = false;

                var layout = bar.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(
                    (int)ButtonSpacing, (int)ButtonSpacing, (int)ButtonSpacing, (int)ButtonSpacing);
                layout.spacing = ButtonSpacing;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.childControlWidth = true;
                layout.childControlHeight = true;

                var barFitter = bar.AddComponent<ContentSizeFitter>();
                barFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                barFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // 아이콘 글리프(❚❚·≫·💬)는 프로젝트 한글 폰트에 없어 □로 깨지므로 텍스트 라벨을 쓴다.
                Button pause = CreateButton(bar.transform, "PauseButton", "정지", 96f, out TextMeshProUGUI pauseLabel);
                Button auto = CreateButton(bar.transform, "AutoButton", "AUTO", 96f, out TextMeshProUGUI autoLabel);
                Button skip = CreateButton(bar.transform, "SkipButton", "스킵", 96f, out _);
                Button backlog = CreateButton(bar.transform, "BacklogButton", "이전 대화", 140f, out _);

                // ── 필드 연결 ──
                var so = new SerializedObject(controlBar);
                SetRef(so, "pauseButton", pause);
                SetRef(so, "autoButton", auto);
                SetRef(so, "skipButton", skip);
                SetRef(so, "backlogButton", backlog);
                SetRef(so, "pauseLabel", pauseLabel);
                SetRef(so, "autoLabel", autoLabel);
                SetEnum(so, "_layer", (int)CanvasLayer.Scene);
                SetBool(so, "_canCloseWithEsc", false);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

                Debug.Log($"[DialogueControlBarBuilder] UI_DialogueControlBar 프리팹 생성 완료: {PrefabPath}\n" +
                          "UIPrefabDatabase에 Key 'DialogueControlBar' / Default Layer 'Scene'으로 등록하세요.");
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

        /// <summary>바의 앵커·피벗·위치. 재실행 시 수동 조정값을 보존하기 위해 분리했다.</summary>
        private readonly struct BarPlacement
        {
            public readonly Vector2 AnchorMin;
            public readonly Vector2 AnchorMax;
            public readonly Vector2 Pivot;
            public readonly Vector2 AnchoredPosition;

            public BarPlacement(Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
            {
                AnchorMin = anchorMin;
                AnchorMax = anchorMax;
                Pivot = pivot;
                AnchoredPosition = anchoredPosition;
            }

            /// <summary>
            /// 대화 패널 바로 위, 패널 우측 끝에 맞춘 기본 위치(우측 하단 앵커 기준).
            /// UI_Dialogue 프리팹의 Panel은 2560x1440 기준으로 우측 끝 x≈2078, 위쪽 끝 y≈599이므로
            /// 그보다 살짝 위에 얹어 대화창과 HUD 어느 쪽과도 겹치지 않게 한다.
            /// </summary>
            public static BarPlacement Default => new(
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-482f, 612f));

            // 과거 빌더가 만들었던 기본값들. 사용자가 옮긴 것과 구분해 새 기본값으로 갱신하기 위해 남긴다.
            private static readonly Vector2[] LegacyDefaultPositions =
            {
                new(40f, -32f), // v1: 좌상단 (미니맵·시계·퀘스트 HUD와 충돌)
                new(0f, -24f),  // v2: 상단 중앙
            };

            public bool IsLegacyDefault()
            {
                foreach (Vector2 legacy in LegacyDefaultPositions)
                {
                    if ((AnchoredPosition - legacy).sqrMagnitude < 0.01f)
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 기존 바의 배치를 읽되, 과거 빌더 기본값 그대로면 새 기본값으로 갱신한다.
        /// 수동으로 옮겨 둔 위치만 보존된다.
        /// </summary>
        private static BarPlacement ReadExistingBarPlacement(Transform root)
        {
            Transform existing = root.Find("Bar");
            if (existing == null || existing is not RectTransform rt)
                return BarPlacement.Default;

            var placement = new BarPlacement(rt.anchorMin, rt.anchorMax, rt.pivot, rt.anchoredPosition);
            return placement.IsLegacyDefault() ? BarPlacement.Default : placement;
        }

        private static Button CreateButton(
            Transform parent, string name, string label, float width, out TextMeshProUGUI labelText)
        {
            var go = NewUI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, ButtonHeight);

            // HorizontalLayoutGroup이 childControl*=true이므로 크기는 LayoutElement로 알린다.
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = ButtonHeight;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            var image = go.AddComponent<Image>();
            image.color = ButtonBackground;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            // 키보드/패드 내비게이션 대상에서 제외한다.
            // 선택 상태가 되면 대화 진행 키가 이 버튼의 Submit으로 소비된다.
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var labelGo = NewUI("Label", go.transform);
            Stretch(labelGo);

            labelText = labelGo.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 22;
            labelText.color = LabelInactive;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.raycastTarget = false;

            return button;
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

        private static void Stretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetRef(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            var property = so.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
            else Debug.LogWarning($"[DialogueControlBarBuilder] 직렬화 필드 '{propertyName}'을 찾지 못했습니다.");
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
