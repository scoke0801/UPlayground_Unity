using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.UI.Guide.EditorTools
{
    /// <summary>
    /// 이미지형 가이드 팝업 프리팹을 생성/재생성하고 UIPrefabDatabase에 등록한다.
    /// </summary>
    public static class UIGuidePopupPrefabBuilder
    {
        private const string PrefabPath = "Assets/03.Prefabs/UI/Popup/UI_GuidePopup.prefab";
        private const string DatabasePath = "Assets/10.Datas/Path/UIPrefabDatabase.asset";

        private static readonly Color Dim = new(0f, 0f, 0f, 0.42f);
        private static readonly Color Panel = new(0.78f, 0.76f, 0.68f, 0.90f);
        private static readonly Color PanelEdge = new(0.90f, 0.88f, 0.80f, 0.95f);
        private static readonly Color ImageFrame = new(0.03f, 0.03f, 0.025f, 1f);
        private static readonly Color TextMain = new(0.24f, 0.26f, 0.28f, 1f);
        private static readonly Color ButtonBg = new(0.35f, 0.34f, 0.30f, 0.80f);

        // Dim 위에 떠 있는 중앙 UI 영역 크기(참조 해상도 2560x1440 기준).
        // 가로는 좁고 세로가 긴 세로형 비율(약 0.9:1).
        private static readonly Vector2 PanelSize = new(1120f, 1260f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static void Build()
        {
            EnsureFolder("Assets/03.Prefabs/UI/Popup");

            // Canvas가 씬 계층의 루트인 상태에서는 Unity가 RectTransform을 구동하며,
            // 에디터용 임시 오브젝트의 0 스케일/0 크기가 프리팹에 직렬화될 수 있다.
            // 실제 런타임과 동일하게 부모 Canvas 아래에서 팝업을 구성한다.
            GameObject stagingCanvas = new("GuidePopupBuildCanvas", typeof(RectTransform), typeof(Canvas));
            GameObject root = new("UI_GuidePopup", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
            root.transform.SetParent(stagingCanvas.transform, false);
            try
            {
                stagingCanvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = (int)CanvasLayer.Popup;

                var scaler = root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(2560f, 1440f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

                var popup = root.AddComponent<UI_GuidePopup>();
                Stretch(root);

                // Dim은 화면 전체를 덮고, 오픈/클로즈 트윈에서 알파를 조절할 수 있도록 CanvasGroup을 둔다.
                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                AddImage(dim, Dim);
                var dimCanvasGroup = dim.AddComponent<CanvasGroup>();

                // Panel은 전체 화면이 아니라 중앙 고정 UI 영역(참조 해상도 2560x1440의 약 65%)이다.
                var panel = NewUI("Panel", root.transform);
                Anchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
                AddImage(panel, Panel, UISprite, true);

                var border = NewUI("Border", panel.transform);
                StretchWithMargin(border, 14f);
                AddImage(border, PanelEdge, UISprite, true).raycastTarget = false;

                var icon = NewUI("GuideIcon", panel.transform);
                Anchor(icon, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(165f, -84f), new Vector2(88f, 88f));
                AddImage(icon, new Color(0.94f, 0.92f, 0.84f, 1f), UISprite, true).raycastTarget = false;
                var iconText = NewUI("IconText", icon.transform);
                Stretch(iconText);
                AddText(iconText, "!", 48f, TextMain, TextAlignmentOptions.Center).raycastTarget = false;

                var imageFrame = NewUI("ImageFrame", panel.transform);
                Anchor(imageFrame, new Vector2(0.08f, 0.51f), new Vector2(0.92f, 0.94f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                AddImage(imageFrame, ImageFrame, UISprite, true);

                var image = NewUI("GuideImage", imageFrame.transform);
                StretchWithMargin(image, 8f);
                var guideImage = AddImage(image, Color.white);
                guideImage.preserveAspect = true;

                var video = NewUI("GuideVideo", imageFrame.transform);
                StretchWithMargin(video, 8f);
                var guideVideoImage = video.AddComponent<RawImage>();
                guideVideoImage.color = Color.white;
                guideVideoImage.enabled = false;
                var videoPlayer = video.AddComponent<VideoPlayer>();
                videoPlayer.playOnAwake = false;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

                var title = NewUI("Title", panel.transform);
                Anchor(title, new Vector2(0.05f, 0.43f), new Vector2(0.95f, 0.49f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var titleText = AddText(title, "가이드 제목", 32f, TextMain, TextAlignmentOptions.Left);
                titleText.fontStyle = FontStyles.Bold;

                var line = NewUI("Divider", panel.transform);
                Anchor(line, new Vector2(0.04f, 0.425f), new Vector2(0.96f, 0.429f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                AddImage(line, new Color(0.93f, 0.91f, 0.84f, 0.9f));

                var body = NewUI("Body", panel.transform);
                Anchor(body, new Vector2(0.04f, 0.17f), new Vector2(0.96f, 0.405f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var bodyText = AddText(body, "가이드 내용을 입력하세요.", 30f, TextMain, TextAlignmentOptions.TopLeft);
                bodyText.textWrappingMode = TextWrappingModes.Normal;

                var prevButton = MakeButton("PreviousButton", panel.transform, "이전");
                Anchor(prevButton.gameObject, new Vector2(0.41f, 0.02f), new Vector2(0.49f, 0.08f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                var page = NewUI("PageText", panel.transform);
                Anchor(page, new Vector2(0.41f, 0.02f), new Vector2(0.59f, 0.08f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                var pageText = AddText(page, "1/1", 30f, Color.white, TextAlignmentOptions.Center);
                // PageText 영역이 이전/다음 버튼과 겹치므로 레이캐스트를 막지 않도록 해제
                pageText.raycastTarget = false;

                var nextButton = MakeButton("NextButton", panel.transform, "다음");
                Anchor(nextButton.gameObject, new Vector2(0.51f, 0.02f), new Vector2(0.59f, 0.08f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                var closeButton = MakeButton("CloseButton", panel.transform, "X");
                Anchor(closeButton.gameObject, new Vector2(0.925f, 0.925f), new Vector2(0.975f, 0.975f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                var so = new SerializedObject(popup);
                so.FindProperty("_layer").intValue = (int)CanvasLayer.Popup;
                SetRef(so, "_dim", dimCanvasGroup);
                SetRef(so, "_panel", (RectTransform)panel.transform);
                SetRef(so, "_guideImage", guideImage);
                SetRef(so, "_guideVideoImage", guideVideoImage);
                SetRef(so, "_videoPlayer", videoPlayer);
                SetRef(so, "_titleText", titleText);
                SetRef(so, "_bodyText", bodyText);
                SetRef(so, "_pageText", pageText);
                SetRef(so, "_previousButton", prevButton);
                SetRef(so, "_nextButton", nextButton);
                SetRef(so, "_closeButton", closeButton);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                RegisterDatabase();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                Debug.Log("[GuidePopupBuilder] UI_GuidePopup 프리팹 생성 및 DB 등록 완료.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stagingCanvas);
            }
        }

        private static Button MakeButton(string name, Transform parent, string label)
        {
            var go = NewUI(name, parent);
            var image = AddImage(go, ButtonBg, UISprite, true);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var labelGo = NewUI("Label", go.transform);
            Stretch(labelGo);
            var text = AddText(labelGo, label, 24f, Color.white, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            return button;
        }

        private static void RegisterDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(DatabasePath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            if (database == null || prefab == null)
            {
                Debug.LogWarning("[GuidePopupBuilder] UIPrefabDatabase 등록을 건너뜁니다. DB 또는 프리팹을 찾지 못했습니다.");
                return;
            }

            database.RemovePrefab(UI_GuidePopup.UIKey);
            database.AddPrefab(UI_GuidePopup.UIKey, prefab, CanvasLayer.Popup, "이미지와 설명을 표시하는 가이드 팝업");
            EditorUtility.SetDirty(database);
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchWithMargin(GameObject go, float margin)
        {
            Stretch(go);
            var rt = (RectTransform)go.transform;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
        }

        private static void Anchor(GameObject go, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static Image AddImage(GameObject go, Color color, Sprite sprite = null, bool sliced = false)
        {
            var image = go.AddComponent<Image>();
            image.color = color;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
            return image;
        }

        private static TextMeshProUGUI AddText(GameObject go, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.richText = true;
            label.overflowMode = TextOverflowModes.Ellipsis;
            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;
            return label;
        }

        private static void SetRef(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[GuidePopupBuilder] 프로퍼티 없음: {propertyName}");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
