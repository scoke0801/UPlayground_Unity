using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>
    /// 이전 대화내역 패널(UI_DialogueBacklog) 프리팹 초안을 코드로 생성/재구성하는 에디터 툴.
    ///
    /// - 로그 엔트리 템플릿(UI_DialogueBacklogEntry) 프리팹과 본체 프리팹 2개를 함께 만든다.
    /// - 프리팹이 없으면 새로 만들고, 있으면 루트/스크립트(guid)는 유지한 채 자식 계층만 재구성한다.
    /// - 초안 생성용이며 최종 비주얼 튜닝은 생성된 프리팹에서 수작업으로 마감한다.
    /// - 빌드 후 UIPrefabDatabase에 Key "DialogueBacklog"로 수동 등록해야 한다.
    /// </summary>
    public static class UIDialogueBacklogPrefabBuilder
    {
        private const string PrefabDir = "Assets/03.Prefabs/UI/Dialogue";
        private const string PrefabPath = PrefabDir + "/UI_DialogueBacklog.prefab";
        private const string EntryPrefabPath = PrefabDir + "/UI_DialogueBacklogEntry.prefab";

        private static readonly Color Dim = new(0f, 0f, 0f, 0.72f);
        private static readonly Color PanelBackground = new(0.07f, 0.07f, 0.09f, 0.94f);
        private static readonly Color EntryBackground = new(1f, 1f, 1f, 0.04f);
        private static readonly Color SpeakerColor = new(1f, 0.78f, 0.42f, 1f);
        private static readonly Color BodyColor = new(0.92f, 0.90f, 0.86f, 1f);

        public static void Build()
        {
            EnsureFolder(PrefabDir);

            GameObject entryPrefab = BuildEntryPrefab();
            BuildPanelPrefab(entryPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        // ── 로그 엔트리 템플릿 ────────────────────────────────────────────

        private static GameObject BuildEntryPrefab()
        {
            GameObject root;
            bool isNew = !File.Exists(EntryPrefabPath);

            if (isNew)
            {
                root = new GameObject("UI_DialogueBacklogEntry", typeof(RectTransform));
                root.AddComponent<UI_DialogueBacklogEntry>();
            }
            else
            {
                root = PrefabUtility.LoadPrefabContents(EntryPrefabPath);
            }

            try
            {
                var entry = root.GetComponent<UI_DialogueBacklogEntry>();
                if (entry == null)
                {
                    Debug.LogError("[DialogueBacklogBuilder] 엔트리 루트에 UI_DialogueBacklogEntry가 없습니다. 중단.");
                    return null;
                }

                ClearChildren(root.transform);

                // 엔트리는 절대 좌표를 쓰지 않는다.
                // 부모 Content의 VerticalLayoutGroup이 childControlHeight로 높이를 읽어가려면
                // 엔트리 자신이 레이아웃 그룹으로 preferredHeight를 계산해 줘야 한다.
                // (예전 구조의 절대 앵커 + ContentSizeFitter 조합이 항목 겹침의 원인이었다.)
                var background = root.GetComponent<Image>();
                if (background == null) background = root.AddComponent<Image>();
                background.color = EntryBackground;
                background.raycastTarget = false;

                var rootLayout = root.GetComponent<HorizontalLayoutGroup>();
                if (rootLayout == null) rootLayout = root.AddComponent<HorizontalLayoutGroup>();
                rootLayout.padding = new RectOffset(12, 16, 10, 10);
                rootLayout.spacing = 12f;
                rootLayout.childAlignment = TextAnchor.UpperLeft;
                rootLayout.childControlWidth = true;
                rootLayout.childControlHeight = true;
                rootLayout.childForceExpandWidth = false;
                rootLayout.childForceExpandHeight = false;

                // 이전 구조에서 붙였을 수 있는 ContentSizeFitter를 제거한다(부모 레이아웃과 충돌).
                var staleFitter = root.GetComponent<ContentSizeFitter>();
                if (staleFitter != null) UnityEngine.Object.DestroyImmediate(staleFitter, true);
                var staleLayoutElement = root.GetComponent<LayoutElement>();
                if (staleLayoutElement != null) UnityEngine.Object.DestroyImmediate(staleLayoutElement, true);

                // 좌측 소형 초상화 — 고정 크기
                var portrait = NewUI("Portrait", root.transform);
                var portraitImage = portrait.AddComponent<Image>();
                portraitImage.preserveAspect = true;
                portraitImage.raycastTarget = false;

                var portraitLayout = portrait.AddComponent<LayoutElement>();
                portraitLayout.preferredWidth = 64f;
                portraitLayout.preferredHeight = 64f;
                portraitLayout.flexibleWidth = 0f;
                portraitLayout.flexibleHeight = 0f;

                // 우측 텍스트 열 — 남는 폭을 모두 차지하고 내용만큼 세로로 늘어난다.
                var column = NewUI("TextColumn", root.transform);
                var columnLayout = column.AddComponent<VerticalLayoutGroup>();
                columnLayout.spacing = 4f;
                columnLayout.childAlignment = TextAnchor.UpperLeft;
                columnLayout.childControlWidth = true;
                columnLayout.childControlHeight = true;
                columnLayout.childForceExpandWidth = true;
                columnLayout.childForceExpandHeight = false;

                var columnElement = column.AddComponent<LayoutElement>();
                columnElement.flexibleWidth = 1f;

                // 화자명
                var speaker = NewUI("SpeakerText", column.transform);
                var speakerText = speaker.AddComponent<TextMeshProUGUI>();
                speakerText.text = "화자";
                speakerText.fontSize = 22;
                speakerText.color = SpeakerColor;
                speakerText.raycastTarget = false;
                speakerText.enableWordWrapping = false;
                speakerText.overflowMode = TextOverflowModes.Ellipsis;

                // 본문 — 리치 텍스트(색상 태그)를 그대로 표시. 줄 수만큼 높이가 늘어난다.
                var body = NewUI("BodyText", column.transform);
                var bodyText = body.AddComponent<TextMeshProUGUI>();
                bodyText.text = "본문";
                bodyText.fontSize = 24;
                bodyText.color = BodyColor;
                bodyText.richText = true;
                bodyText.raycastTarget = false;
                bodyText.enableWordWrapping = true;
                bodyText.overflowMode = TextOverflowModes.Overflow;

                var so = new SerializedObject(entry);
                SetRef(so, "speakerText", speakerText);
                SetRef(so, "bodyText", bodyText);
                SetRef(so, "portraitImage", portraitImage);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, EntryPrefabPath);
            }
            finally
            {
                if (isNew)
                    UnityEngine.Object.DestroyImmediate(root);
                else
                    PrefabUtility.UnloadPrefabContents(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(EntryPrefabPath);
        }

        // ── 본체 패널 ────────────────────────────────────────────────────

        private static void BuildPanelPrefab(GameObject entryPrefabAsset)
        {
            GameObject root;
            bool isNew = !File.Exists(PrefabPath);

            if (isNew)
            {
                root = new GameObject("UI_DialogueBacklog",
                    typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
                root.AddComponent<UI_DialogueBacklog>();
            }
            else
            {
                root = PrefabUtility.LoadPrefabContents(PrefabPath);
            }

            try
            {
                var backlog = root.GetComponent<UI_DialogueBacklog>();
                if (backlog == null)
                {
                    Debug.LogError("[DialogueBacklogBuilder] 루트에 UI_DialogueBacklog 컴포넌트가 없습니다. 중단.");
                    return;
                }

                ClearChildren(root.transform);

                // 루트는 반드시 화면 전체 stretch여야 한다.
                // UIManager.ShowUI는 인스턴스화 후 RectTransform을 보정하지 않으므로,
                // 루트가 기본 100x100 중앙 박스로 남으면 암전과 패널 위치가 모두 어긋난다.
                Stretch(root);

                // 전체 화면 암전 (뒤 대화 입력 차단)
                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                var dimImage = dim.AddComponent<Image>();
                dimImage.color = Dim;

                // 중앙 패널
                var panel = NewUI("Panel", root.transform);
                var panelRt = panel.GetComponent<RectTransform>();
                panelRt.anchorMin = new Vector2(0.5f, 0.5f);
                panelRt.anchorMax = new Vector2(0.5f, 0.5f);
                panelRt.pivot = new Vector2(0.5f, 0.5f);
                panelRt.sizeDelta = new Vector2(1200f, 760f);
                panel.AddComponent<Image>().color = PanelBackground;

                // 제목
                var title = NewUI("TitleText", panel.transform);
                var titleRt = title.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0f, 1f);
                titleRt.anchorMax = new Vector2(1f, 1f);
                titleRt.pivot = new Vector2(0.5f, 1f);
                titleRt.offsetMin = new Vector2(32f, 0f);
                titleRt.offsetMax = new Vector2(-32f, 0f);
                titleRt.sizeDelta = new Vector2(titleRt.sizeDelta.x, 56f);
                titleRt.anchoredPosition = new Vector2(0f, -20f);

                var titleText = title.AddComponent<TextMeshProUGUI>();
                titleText.text = "이전 대화";
                titleText.fontSize = 32;
                titleText.color = SpeakerColor;
                titleText.raycastTarget = false;

                // 스크롤 뷰
                var scrollGo = NewUI("ScrollView", panel.transform);
                var scrollRt = scrollGo.GetComponent<RectTransform>();
                scrollRt.anchorMin = Vector2.zero;
                scrollRt.anchorMax = Vector2.one;
                scrollRt.offsetMin = new Vector2(24f, 88f);
                scrollRt.offsetMax = new Vector2(-24f, -84f);

                var scrollRect = scrollGo.AddComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 40f;

                var viewport = NewUI("Viewport", scrollGo.transform);
                Stretch(viewport);
                var viewportRt = viewport.GetComponent<RectTransform>();
                viewport.AddComponent<RectMask2D>();

                // 뷰포트에 raycast 대상 Graphic이 없으면 마우스 휠 이벤트가 ScrollRect까지 오지 않아
                // 스크롤이 전혀 동작하지 않는다. 로그 항목들은 raycastTarget이 꺼져 있으므로 여기서 받아야 한다.
                var viewportImage = viewport.AddComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.002f);
                viewportImage.raycastTarget = true;

                var content = NewUI("Content", viewport.transform);
                var contentRt = content.GetComponent<RectTransform>();
                contentRt.anchorMin = new Vector2(0f, 1f);
                contentRt.anchorMax = new Vector2(1f, 1f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.offsetMin = new Vector2(0f, 0f);
                contentRt.offsetMax = new Vector2(0f, 0f);

                var contentLayout = content.AddComponent<VerticalLayoutGroup>();
                contentLayout.spacing = 8f;
                contentLayout.padding = new RectOffset(8, 8, 8, 8);
                contentLayout.childForceExpandHeight = false;
                contentLayout.childControlHeight = true;
                contentLayout.childControlWidth = true;

                var contentFitter = content.AddComponent<ContentSizeFitter>();
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollRect.viewport = viewportRt;
                scrollRect.content = contentRt;

                // 스크롤바는 노출하지 않는다. 휠과 드래그로만 이동한다.
                // (뷰포트의 raycast 대상 Image가 있어야 휠·드래그 이벤트가 ScrollRect까지 전달된다.)
                scrollRect.verticalScrollbar = null;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

                // 비어 있을 때 안내
                var empty = NewUI("EmptyMessage", panel.transform);
                Stretch(empty);
                var emptyText = empty.AddComponent<TextMeshProUGUI>();
                emptyText.text = "아직 지나간 대화가 없습니다.";
                emptyText.fontSize = 26;
                emptyText.color = new Color(BodyColor.r, BodyColor.g, BodyColor.b, 0.6f);
                emptyText.alignment = TextAlignmentOptions.Center;
                emptyText.raycastTarget = false;

                // 닫기 버튼
                var closeGo = NewUI("CloseButton", panel.transform);
                var closeRt = closeGo.GetComponent<RectTransform>();
                closeRt.anchorMin = new Vector2(0.5f, 0f);
                closeRt.anchorMax = new Vector2(0.5f, 0f);
                closeRt.pivot = new Vector2(0.5f, 0f);
                closeRt.anchoredPosition = new Vector2(0f, 20f);
                closeRt.sizeDelta = new Vector2(220f, 52f);

                var closeImage = closeGo.AddComponent<Image>();
                closeImage.color = new Color(1f, 1f, 1f, 0.10f);
                var closeButton = closeGo.AddComponent<Button>();
                closeButton.targetGraphic = closeImage;

                var closeLabel = NewUI("Label", closeGo.transform);
                Stretch(closeLabel);
                var closeLabelText = closeLabel.AddComponent<TextMeshProUGUI>();
                closeLabelText.text = "닫기";
                closeLabelText.fontSize = 24;
                closeLabelText.color = BodyColor;
                closeLabelText.alignment = TextAlignmentOptions.Center;
                closeLabelText.raycastTarget = false;

                // ── 필드 연결 ──
                var so = new SerializedObject(backlog);
                SetRef(so, "scrollRect", scrollRect);
                SetRef(so, "entryContainer", contentRt);
                SetRef(so, "entryPrefab",
                    entryPrefabAsset != null ? entryPrefabAsset.GetComponent<UI_DialogueBacklogEntry>() : null);
                SetRef(so, "emptyMessage", empty);
                SetRef(so, "closeButton", closeButton);
                SetEnum(so, "_layer", (int)CanvasLayer.Popup);
                SetBool(so, "_canCloseWithEsc", true);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

                Debug.Log($"[DialogueBacklogBuilder] UI_DialogueBacklog 프리팹 생성 완료: {PrefabPath}\n" +
                          $"엔트리 템플릿: {EntryPrefabPath}\n" +
                          "UIPrefabDatabase에 Key 'DialogueBacklog' / Default Layer 'Popup'으로 등록하세요.");
            }
            finally
            {
                if (isNew)
                    UnityEngine.Object.DestroyImmediate(root);
                else
                    PrefabUtility.UnloadPrefabContents(root);
            }
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
            else Debug.LogWarning($"[DialogueBacklogBuilder] 직렬화 필드 '{propertyName}'을 찾지 못했습니다.");
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
