using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.EditorTools;
using UPlayGround.Manager;

namespace UPlayGround.UI.Merchant.EditorTools
{
    /// <summary>공용 비주얼 테마로 상점 화면과 동적 품목 슬롯 프리팹을 생성한다.</summary>
    public static class UIMerchantPrefabBuilder
    {
        private const string ToolId = "UPlayGround/생성 도구/상점 UI 프리팹 빌드";
        private const string PrefabDirectory = "Assets/03.Prefabs/UI/Scene/Merchant";
        private const string MainPrefabPath = PrefabDirectory + "/UI_Scene_Merchant.prefab";
        private const string SlotPrefabPath = PrefabDirectory + "/UIMerchantItemSlot.prefab";
        private const string ThemePath = "Assets/10.Datas/UI/UIVisualTheme.asset";
        private const string UiDatabasePath = "Assets/10.Datas/Path/UIPrefabDatabase.asset";

        [UPlaygroundTool(ToolId, false, 70)]
        public static void Build()
        {
            UIVisualThemeSO theme = AssetDatabase.LoadAssetAtPath<UIVisualThemeSO>(ThemePath);
            if (theme == null)
                throw new FileNotFoundException("공용 UI 비주얼 테마를 찾을 수 없습니다.", ThemePath);

            Directory.CreateDirectory(PrefabDirectory);
            GameObject slotPrefab = BuildSlotPrefab(theme);
            GameObject mainPrefab = BuildMainPrefab(theme, slotPrefab);
            RegisterUiPrefab(mainPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MerchantUIBuilder] 상점 UI 생성 완료: {MainPrefabPath}");
        }

        private static GameObject BuildSlotPrefab(UIVisualThemeSO theme)
        {
            GameObject root = NewUI("UIMerchantItemSlot", null);
            try
            {
                SetHeight(root, 86f);
                Image background = AddImage(root, theme.Surface, theme.CardFrame, true);
                Button button = root.AddComponent<Button>();
                button.targetGraphic = background;
                var slot = root.AddComponent<UIMerchantItemSlot>();

                HorizontalLayoutGroup layout = AddHorizontalLayout(root, 12f, 10);
                layout.childForceExpandHeight = true;

                GameObject iconFrame = NewUI("IconFrame", root.transform);
                SetWidth(iconFrame, 66f);
                AddImage(iconFrame, theme.SurfaceRaised, theme.CardFrame, true);
                GameObject iconObject = NewUI("Icon", iconFrame.transform);
                InsetStretch(Rect(iconObject), 7f);
                Image icon = AddImage(iconObject, Color.white);

                GameObject copy = NewUI("Copy", root.transform);
                AddFlexibleWidth(copy, 1f);
                VerticalLayoutGroup copyLayout = AddVerticalLayout(copy, 2f, 2);
                copyLayout.childForceExpandHeight = false;
                TextMeshProUGUI itemName = AddText(
                    NewUI("ItemName", copy.transform),
                    "품목",
                    theme.BodySize,
                    theme.TextMain,
                    TextAlignmentOptions.Left);
                SetHeight(itemName.gameObject, 34f);
                TextMeshProUGUI secondary = AddText(
                    NewUI("Secondary", copy.transform),
                    "남은 수량",
                    theme.CaptionSize,
                    theme.TextSub,
                    TextAlignmentOptions.Left);
                SetHeight(secondary.gameObject, 26f);

                TextMeshProUGUI price = AddText(
                    NewUI("Price", root.transform),
                    "0 G",
                    theme.LabelSize,
                    theme.Warning,
                    TextAlignmentOptions.Right);
                SetWidth(price.gameObject, 150f);

                GameObject overlay = NewUI("SelectedOverlay", root.transform);
                Stretch(overlay);
                Image overlayImage = AddImage(
                    overlay,
                    Color.white,
                    theme.SlotFocusFrame ?? theme.TabFocusFrame,
                    true);
                overlayImage.raycastTarget = false;
                overlay.SetActive(false);

                var serialized = new SerializedObject(slot);
                SetReference(serialized, "_itemIcon", icon);
                SetReference(serialized, "_itemName", itemName);
                SetReference(serialized, "_price", price);
                SetReference(serialized, "_secondary", secondary);
                SetReference(serialized, "_selectedOverlay", overlay);
                SetReference(serialized, "_visualTarget", Rect(root));
                serialized.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, SlotPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject BuildMainPrefab(UIVisualThemeSO theme, GameObject slotPrefab)
        {
            GameObject root = new(
                "UI_Scene_Merchant",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(UI_Scene_Merchant));
            try
            {
                RectTransform rootRect = Rect(root);
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                UI_Scene_Merchant merchant = root.GetComponent<UI_Scene_Merchant>();

                GameObject dim = NewUI("Dim", root.transform);
                Stretch(dim);
                AddImage(dim, theme.ScreenDim);

                GameObject window = NewUI("Window", root.transform);
                Stretch(window);
                AddImage(window, theme.Panel, theme.PanelFrame, true);
                VerticalLayoutGroup windowLayout = AddVerticalLayout(window, 12f, 24);
                windowLayout.childForceExpandHeight = false;

                GameObject header = NewUI("Header", window.transform);
                SetHeight(header, 72f);
                HorizontalLayoutGroup headerLayout = AddHorizontalLayout(header, 12f, 0);
                TextMeshProUGUI merchantName = AddText(
                    NewUI("MerchantName", header.transform),
                    "상점",
                    theme.TitleSize,
                    theme.TextMain,
                    TextAlignmentOptions.Left);
                AddFlexibleWidth(merchantName.gameObject, 1f);

                GameObject goldPanel = NewUI("GoldPanel", header.transform);
                SetWidth(goldPanel, 250f);
                AddImage(goldPanel, theme.SurfaceRaised, theme.CardFrame, true);
                TextMeshProUGUI goldText = AddText(
                    NewUI("Gold", goldPanel.transform),
                    "0 G",
                    theme.HeadingSize,
                    theme.Warning,
                    TextAlignmentOptions.Center);
                Stretch(goldText.gameObject);
                Button closeButton = MakeButton(
                    "CloseButton",
                    header.transform,
                    "닫기",
                    theme,
                    out _);
                SetWidth(closeButton.gameObject, 120f);

                GameObject tabs = NewUI("TradeTabs", window.transform);
                SetHeight(tabs, 56f);
                HorizontalLayoutGroup tabLayout = AddHorizontalLayout(tabs, 8f, 0);
                tabLayout.childForceExpandWidth = true;
                UITabButton buyTab = MakeTab("BuyTab", tabs.transform, "구매", theme);
                UITabButton sellTab = MakeTab("SellTab", tabs.transform, "판매", theme);
                UITabGroup tradeTabs = tabs.AddComponent<UITabGroup>();
                tradeTabs.SetTabs(new[] { buyTab, sellTab });

                GameObject body = NewUI("Body", window.transform);
                AddFlexibleHeight(body, 1f);
                HorizontalLayoutGroup bodyLayout = AddHorizontalLayout(body, 14f, 0);
                bodyLayout.childForceExpandHeight = true;

                GameObject listPanel = NewUI("ListPanel", body.transform);
                AddFlexibleWidth(listPanel, 2f);
                AddImage(listPanel, theme.Surface, theme.CardFrame, true);
                VerticalLayoutGroup listLayout = AddVerticalLayout(listPanel, 8f, 12);
                listLayout.childForceExpandHeight = false;
                TextMeshProUGUI listTitle = AddText(
                    NewUI("ListTitle", listPanel.transform),
                    "거래 품목",
                    theme.HeadingSize,
                    theme.TextMain,
                    TextAlignmentOptions.Left);
                SetHeight(listTitle.gameObject, 44f);
                GameObject scrollObject = NewUI("ItemScroll", listPanel.transform);
                AddFlexibleHeight(scrollObject, 1f);
                Transform listContent = BuildVerticalScroll(scrollObject, theme, out ScrollRect listScroll);
                GameObject emptyState = NewUI("EmptyState", listPanel.transform);
                SetHeight(emptyState, 70f);
                TextMeshProUGUI emptyStateText = AddText(
                    NewUI("EmptyText", emptyState.transform),
                    "거래할 물건이 없습니다.",
                    theme.BodySize,
                    theme.TextMuted,
                    TextAlignmentOptions.Center);
                Stretch(emptyStateText.gameObject);
                emptyState.SetActive(false);

                GameObject detail = NewUI("DetailPanel", body.transform);
                AddFlexibleWidth(detail, 3f);
                AddImage(detail, theme.Surface, theme.CardFrame, true);
                VerticalLayoutGroup detailLayout = AddVerticalLayout(detail, 12f, 18);
                detailLayout.childForceExpandHeight = false;

                GameObject itemRow = NewUI("ItemRow", detail.transform);
                SetHeight(itemRow, 130f);
                HorizontalLayoutGroup itemRowLayout = AddHorizontalLayout(itemRow, 16f, 0);
                GameObject detailIconFrame = NewUI("IconFrame", itemRow.transform);
                SetWidth(detailIconFrame, 122f);
                AddImage(detailIconFrame, theme.SurfaceRaised, theme.CardFrame, true);
                GameObject detailIconObject = NewUI("Icon", detailIconFrame.transform);
                InsetStretch(Rect(detailIconObject), 10f);
                Image detailIcon = AddImage(detailIconObject, Color.white);

                GameObject itemCopy = NewUI("ItemCopy", itemRow.transform);
                AddFlexibleWidth(itemCopy, 1f);
                VerticalLayoutGroup itemCopyLayout = AddVerticalLayout(itemCopy, 6f, 2);
                itemCopyLayout.childForceExpandHeight = false;
                TextMeshProUGUI detailName = AddText(
                    NewUI("ItemName", itemCopy.transform),
                    "품목 이름",
                    theme.HeadingSize,
                    theme.TextMain,
                    TextAlignmentOptions.Left);
                SetHeight(detailName.gameObject, 48f);
                TextMeshProUGUI detailPrice = AddText(
                    NewUI("Price", itemCopy.transform),
                    "개당 0 G",
                    theme.BodySize,
                    theme.Warning,
                    TextAlignmentOptions.Left);
                SetHeight(detailPrice.gameObject, 38f);

                TextMeshProUGUI description = AddText(
                    NewUI("Description", detail.transform),
                    "물건 설명",
                    theme.BodySize,
                    theme.TextSub,
                    TextAlignmentOptions.TopLeft);
                AddFlexibleHeight(description.gameObject, 1f);
                description.textWrappingMode = TextWrappingModes.Normal;

                TextMeshProUGUI availability = AddText(
                    NewUI("Availability", detail.transform),
                    "보유 0",
                    theme.LabelSize,
                    theme.TextSub,
                    TextAlignmentOptions.Left);
                SetHeight(availability.gameObject, 38f);

                GameObject actionRow = NewUI("ActionRow", detail.transform);
                SetHeight(actionRow, 68f);
                HorizontalLayoutGroup actionLayout = AddHorizontalLayout(actionRow, 8f, 0);
                TextMeshProUGUI quantityLabel = AddText(
                    NewUI("QuantityLabel", actionRow.transform),
                    "수량",
                    theme.LabelSize,
                    theme.TextSub,
                    TextAlignmentOptions.Center);
                SetWidth(quantityLabel.gameObject, 70f);
                Button minusButton = MakeButton("QuantityMinus", actionRow.transform, "-", theme, out _);
                SetWidth(minusButton.gameObject, 64f);
                GameObject quantityFrame = NewUI("QuantityFrame", actionRow.transform);
                SetWidth(quantityFrame, 100f);
                AddImage(quantityFrame, theme.SurfaceRaised, theme.CardFrame, true);
                TextMeshProUGUI quantityText = AddText(
                    NewUI("Quantity", quantityFrame.transform),
                    "1",
                    theme.HeadingSize,
                    theme.TextMain,
                    TextAlignmentOptions.Center);
                Stretch(quantityText.gameObject);
                Button plusButton = MakeButton("QuantityPlus", actionRow.transform, "+", theme, out _);
                SetWidth(plusButton.gameObject, 64f);
                Button maximumButton = MakeButton("QuantityMax", actionRow.transform, "최대", theme, out _);
                SetWidth(maximumButton.gameObject, 92f);
                Button tradeButton = MakeButton(
                    "TradeButton",
                    actionRow.transform,
                    "구매",
                    theme,
                    out TextMeshProUGUI tradeButtonText,
                    true);
                AddFlexibleWidth(tradeButton.gameObject, 1f);

                GameObject statusObject = NewUI("Status", detail.transform);
                SetHeight(statusObject, 44f);
                CanvasGroup statusCanvas = statusObject.AddComponent<CanvasGroup>();
                TextMeshProUGUI statusText = AddText(
                    NewUI("Message", statusObject.transform),
                    string.Empty,
                    theme.LabelSize,
                    theme.Positive,
                    TextAlignmentOptions.Center);
                Stretch(statusText.gameObject);

                SerializedObject serialized = new(merchant);
                serialized.FindProperty("_layer").intValue = (int)CanvasLayer.Scene;
                serialized.FindProperty("_canCloseWithEsc").boolValue = true;
                SetReference(serialized, "_sceneContent", Rect(window));
                SetReference(serialized, "_merchantName", merchantName);
                SetReference(serialized, "_goldText", goldText);
                SetReference(serialized, "_goldPanel", Rect(goldPanel));
                SetReference(serialized, "_closeButton", closeButton);
                SetReference(serialized, "_tradeTabs", tradeTabs);
                SetReference(serialized, "_listScroll", listScroll);
                SetReference(serialized, "_listContent", listContent);
                SetReference(serialized, "_itemSlotPrefab", slotPrefab.GetComponent<UIMerchantItemSlot>());
                SetReference(serialized, "_emptyState", emptyState);
                SetReference(serialized, "_emptyStateText", emptyStateText);
                SetReference(serialized, "_detailPanel", detail);
                SetReference(serialized, "_detailIcon", detailIcon);
                SetReference(serialized, "_detailName", detailName);
                SetReference(serialized, "_detailDescription", description);
                SetReference(serialized, "_detailPrice", detailPrice);
                SetReference(serialized, "_detailAvailability", availability);
                SetReference(serialized, "_quantityMinusButton", minusButton);
                SetReference(serialized, "_quantityPlusButton", plusButton);
                SetReference(serialized, "_quantityMaxButton", maximumButton);
                SetReference(serialized, "_quantityText", quantityText);
                SetReference(serialized, "_tradeButton", tradeButton);
                SetReference(serialized, "_tradeButtonText", tradeButtonText);
                SetReference(serialized, "_statusText", statusText);
                SetReference(serialized, "_statusCanvas", statusCanvas);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void RegisterUiPrefab(GameObject prefab)
        {
            UIPrefabDatabase database = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(UiDatabasePath);
            if (database == null)
                throw new FileNotFoundException("UI 프리팹 데이터베이스를 찾을 수 없습니다.", UiDatabasePath);

            database.RemovePrefab("Merchant");
            database.AddPrefab(
                "Merchant",
                prefab,
                CanvasLayer.Scene,
                "상인 구매·판매 화면");
            EditorUtility.SetDirty(database);
        }

        private static UITabButton MakeTab(
            string name,
            Transform parent,
            string label,
            UIVisualThemeSO theme)
        {
            Button button = MakeButton(name, parent, label, theme, out TextMeshProUGUI text);
            Image background = button.GetComponent<Image>();
            GameObject indicator = NewUI("Selected", button.transform);
            RectTransform indicatorRect = Rect(indicator);
            indicatorRect.anchorMin = new Vector2(0f, 0f);
            indicatorRect.anchorMax = new Vector2(1f, 0f);
            indicatorRect.pivot = new Vector2(0.5f, 0f);
            indicatorRect.sizeDelta = new Vector2(0f, 5f);
            indicatorRect.anchoredPosition = Vector2.zero;
            Image indicatorImage = AddImage(
                indicator,
                theme.Focus,
                theme.TabFocusFrame,
                true);
            indicatorImage.raycastTarget = false;

            UITabButton tab = button.gameObject.AddComponent<UITabButton>();
            tab.Configure(
                button,
                background,
                text,
                theme.Surface,
                Color.Lerp(theme.SurfaceRaised, theme.Focus, 0.18f),
                theme.TextSub,
                theme.TextMain);
            SerializedObject serialized = new(tab);
            SetReference(serialized, "_selectedIndicator", indicator);
            SetReference(serialized, "_theme", theme);
            SetReference(serialized, "_visualTarget", Rect(button.gameObject));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return tab;
        }

        private static Button MakeButton(
            string name,
            Transform parent,
            string label,
            UIVisualThemeSO theme,
            out TextMeshProUGUI text,
            bool accent = false)
        {
            GameObject buttonObject = NewUI(name, parent);
            Image background = AddImage(
                buttonObject,
                accent ? Color.Lerp(theme.SurfaceRaised, theme.Focus, 0.22f) : theme.SurfaceRaised,
                theme.ButtonFrame,
                true);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = background;
            text = AddText(
                NewUI("Label", buttonObject.transform),
                label,
                theme.LabelSize,
                theme.TextMain,
                TextAlignmentOptions.Center);
            Stretch(text.gameObject);
            return button;
        }

        private static Transform BuildVerticalScroll(
            GameObject scrollObject,
            UIVisualThemeSO theme,
            out ScrollRect scrollRect)
        {
            AddImage(scrollObject, theme.SurfaceRaised, theme.CardFrame, true);
            scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = NewUI("Viewport", scrollObject.transform);
            Stretch(viewport);
            Image viewportImage = AddImage(viewport, new Color(1f, 1f, 1f, 0.01f));
            viewportImage.raycastTarget = true;
            viewport.AddComponent<RectMask2D>();

            GameObject content = NewUI("Content", viewport.transform);
            RectTransform contentRect = Rect(content);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            VerticalLayoutGroup contentLayout = AddVerticalLayout(content, 7f, 8);
            contentLayout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = Rect(viewport);
            scrollRect.content = contentRect;
            return content.transform;
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            if (parent != null)
                gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static Image AddImage(
            GameObject gameObject,
            Color color,
            Sprite sprite = null,
            bool sliced = false)
        {
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            if (sliced && sprite != null)
                image.type = Image.Type.Sliced;
            return image;
        }

        private static TextMeshProUGUI AddText(
            GameObject gameObject,
            string text,
            float size,
            Color color,
            TextAlignmentOptions alignment)
        {
            TextMeshProUGUI label = gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private static VerticalLayoutGroup AddVerticalLayout(GameObject gameObject, float spacing, int padding)
        {
            VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        private static HorizontalLayoutGroup AddHorizontalLayout(GameObject gameObject, float spacing, int padding)
        {
            HorizontalLayoutGroup layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleCenter;
            return layout;
        }

        private static void SetReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new MissingReferenceException($"직렬화 필드를 찾을 수 없습니다: {propertyName}");
            property.objectReferenceValue = value;
        }

        private static RectTransform Rect(GameObject gameObject) =>
            (RectTransform)gameObject.transform;

        private static void Stretch(GameObject gameObject) => Stretch(Rect(gameObject));

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void InsetStretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static LayoutElement GetLayoutElement(GameObject gameObject) =>
            gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();

        private static void SetHeight(GameObject gameObject, float height)
        {
            LayoutElement element = GetLayoutElement(gameObject);
            element.minHeight = height;
            element.preferredHeight = height;
            element.flexibleHeight = 0f;
        }

        private static void SetWidth(GameObject gameObject, float width)
        {
            LayoutElement element = GetLayoutElement(gameObject);
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
        }

        private static void AddFlexibleWidth(GameObject gameObject, float value)
        {
            GetLayoutElement(gameObject).flexibleWidth = value;
        }

        private static void AddFlexibleHeight(GameObject gameObject, float value)
        {
            GetLayoutElement(gameObject).flexibleHeight = value;
        }
    }
}
