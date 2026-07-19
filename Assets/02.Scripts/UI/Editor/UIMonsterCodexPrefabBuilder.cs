using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.UI.EditorTools
{
    /// <summary>몬스터 도감 Scene UI와 메뉴 진입 버튼을 생성하고 DB에 등록한다.</summary>
    public static class UIMonsterCodexPrefabBuilder
    {
        private const string RootFolder = "Assets/03.Prefabs/UI/Scene/Codex";
        private const string MainPath = RootFolder + "/UI_MonsterCodex.prefab";
        private const string SlotPath = RootFolder + "/UIMonsterCodexSlot.prefab";
        private const string MenuPanelPath = "Assets/03.Prefabs/UI/HUD/UI_MenuPanel.prefab";
        private const string CodexMenuIconPath =
            "Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/IconMisc/MenuIcon_Monster.png";

        private static readonly Color Window = new(0.06f, 0.08f, 0.11f, 0.98f);
        private static readonly Color Panel = new(0.11f, 0.14f, 0.18f, 1f);
        private static readonly Color Slot = new(0.16f, 0.19f, 0.24f, 1f);
        private static readonly Color Text = new(0.9f, 0.92f, 0.95f, 1f);

        [MenuItem("UPlayGround/UI/프리팹 빌드/몬스터 도감")]
        public static void Build()
        {
            EnsureFolder(RootFolder);
            UIMonsterCodexSlot slotPrefab = BuildSlot();
            GameObject mainPrefab = BuildMain(slotPrefab);
            RegisterUI(mainPrefab);
            BindMenuButton();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = mainPrefab;
            Debug.Log("[MonsterCodexUIBuilder] 도감 UI, 메뉴 버튼, UI DB 등록 완료");
        }

        [InitializeOnLoadMethod]
        private static void RepairBrokenLayoutAfterScriptReload()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode ||
                    EditorApplication.isCompiling)
                {
                    return;
                }

                bool repaired = false;
                if (!File.Exists(MainPath) || NeedsCodexLayoutRepair())
                {
                    EnsureFolder(RootFolder);
                    UIMonsterCodexSlot slotPrefab = BuildSlot();
                    GameObject mainPrefab = BuildMain(slotPrefab);
                    RegisterUI(mainPrefab);
                    repaired = true;
                    Debug.Log("[MonsterCodexUIBuilder] 잘못 생성된 도감 화면 레이아웃을 자동 복구했습니다.");
                }

                if (File.Exists(MenuPanelPath) && NeedsMenuLayoutRepair())
                {
                    BindMenuButton();
                    repaired = true;
                    Debug.Log("[MonsterCodexUIBuilder] 잘못 중첩된 도감 메뉴 버튼을 자동 복구했습니다.");
                }

                if (repaired)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
            };
        }

        private static UIMonsterCodexSlot BuildSlot()
        {
            GameObject root = NewUI("UIMonsterCodexSlot", null);
            Image background = root.AddComponent<Image>();
            background.color = Slot;
            Button slotButton = root.AddComponent<Button>();
            slotButton.targetGraphic = background;
            root.AddComponent<LayoutElement>().preferredHeight = 92f;
            HorizontalLayoutGroup row = root.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(10, 10, 8, 8);
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = true;
            UIMonsterCodexSlot slot = root.AddComponent<UIMonsterCodexSlot>();

            Image portrait = NewUI("Portrait", root.transform).AddComponent<Image>();
            portrait.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;
            portrait.preserveAspect = true;

            GameObject labels = NewUI("Labels", root.transform);
            labels.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup labelsLayout = labels.AddComponent<VerticalLayoutGroup>();
            labelsLayout.spacing = 5f;
            labelsLayout.childControlWidth = true;
            labelsLayout.childControlHeight = true;
            labelsLayout.childForceExpandWidth = true;
            labelsLayout.childForceExpandHeight = false;
            TextMeshProUGUI name = AddText(NewUI("Name", labels.transform), "???", 22);
            TextMeshProUGUI progress = AddText(NewUI("Progress", labels.transform), "0%", 17);

            Image progressBg = NewUI("ProgressBar", labels.transform).AddComponent<Image>();
            progressBg.color = new Color(0f, 0f, 0f, 0.5f);
            progressBg.gameObject.AddComponent<LayoutElement>().preferredHeight = 8f;
            Image fill = NewUI("Fill", progressBg.transform).AddComponent<Image>();
            Stretch(fill.rectTransform);
            fill.color = new Color(0.95f, 0.6f, 0.2f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;

            GameObject selection = NewUI("Selection", root.transform);
            Stretch(selection.GetComponent<RectTransform>());
            selection.AddComponent<LayoutElement>().ignoreLayout = true;
            Image selectionImage = selection.AddComponent<Image>();
            selectionImage.color = new Color(1f, 0.65f, 0.2f, 0.18f);
            selectionImage.raycastTarget = false;

            SerializedObject serialized = new(slot);
            Set(serialized, "_portrait", portrait);
            Set(serialized, "_name", name);
            Set(serialized, "_progressFill", fill);
            Set(serialized, "_progressLabel", progress);
            Set(serialized, "_selection", selection);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, SlotPath);
            Object.DestroyImmediate(root);
            return saved.GetComponent<UIMonsterCodexSlot>();
        }

        private static GameObject BuildMain(UIMonsterCodexSlot slotPrefab)
        {
            GameObject root = NewUI("UI_MonsterCodex", null);
            Stretch(root.GetComponent<RectTransform>());
            UI_MonsterCodex menu = root.AddComponent<UI_MonsterCodex>();
            root.AddComponent<CanvasGroup>();
            root.AddComponent<GraphicRaycaster>();

            Image dim = NewUI("Dim", root.transform).AddComponent<Image>();
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.65f);

            GameObject window = NewUI("Window", root.transform);
            Stretch(window.GetComponent<RectTransform>(), 28f);
            window.AddComponent<Image>().color = Window;

            GameObject header = NewUI("Header", window.transform);
            SetTopRect(header.GetComponent<RectTransform>(), 18f, 18f, 18f, 64f);
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;
            TextMeshProUGUI title = AddText(NewUI("Title", header.transform), "몬스터 도감", 34);
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            Button close = MakeButton("Close", header.transform, "닫기");
            close.gameObject.AddComponent<LayoutElement>().preferredWidth = 100f;

            GameObject filters = NewUI("Filters", window.transform);
            SetTopRect(filters.GetComponent<RectTransform>(), 18f, 18f, 92f, 52f);
            HorizontalLayoutGroup filterLayout = filters.AddComponent<HorizontalLayoutGroup>();
            filterLayout.spacing = 10f;
            filterLayout.childControlWidth = true;
            filterLayout.childControlHeight = true;
            filterLayout.childForceExpandWidth = false;
            filterLayout.childForceExpandHeight = true;
            TMP_Dropdown grade = MakeDropdown("GradeFilter", filters.transform);
            TMP_Dropdown element = MakeDropdown("ElementFilter", filters.transform);

            GameObject body = NewUI("Body", window.transform);
            RectTransform bodyRect = body.GetComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(18f, 18f);
            bodyRect.offsetMax = new Vector2(-18f, -156f);
            HorizontalLayoutGroup bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 14f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = false;
            bodyLayout.childForceExpandHeight = true;

            GameObject left = NewUI("ListPanel", body.transform);
            left.AddComponent<Image>().color = Panel;
            left.AddComponent<LayoutElement>().preferredWidth = 620f;
            GameObject scrollObject = NewUI("Scroll", left.transform);
            Stretch(scrollObject.GetComponent<RectTransform>(), 10f);
            ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
            Image scrollMaskImage = scrollObject.AddComponent<Image>();
            scrollMaskImage.color = new Color(0f, 0f, 0f, 0.01f);
            scrollObject.AddComponent<Mask>().showMaskGraphic = false;
            GameObject content = NewUI("Content", scrollObject.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            VerticalLayoutGroup listLayout = content.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 7f;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRect;
            scroll.vertical = true;
            scroll.horizontal = false;

            GameObject detail = NewUI("DetailPanel", body.transform);
            detail.AddComponent<Image>().color = Panel;
            detail.AddComponent<LayoutElement>().flexibleWidth = 1f;
            CanvasGroup detailGroup = detail.AddComponent<CanvasGroup>();
            VerticalLayoutGroup detailLayout = detail.AddComponent<VerticalLayoutGroup>();
            detailLayout.padding = new RectOffset(22, 22, 22, 22);
            detailLayout.spacing = 12f;
            detailLayout.childControlWidth = true;
            detailLayout.childControlHeight = true;
            detailLayout.childForceExpandWidth = true;
            detailLayout.childForceExpandHeight = false;
            Image portrait = NewUI("Portrait", detail.transform).AddComponent<Image>();
            portrait.preserveAspect = true;
            portrait.gameObject.AddComponent<LayoutElement>().preferredHeight = 360f;
            TextMeshProUGUI name = AddText(NewUI("Name", detail.transform), "???", 32);
            TextMeshProUGUI elementLabel = AddText(NewUI("Element", detail.transform), "속성: ?", 21);
            TextMeshProUGUI progressLabel = AddText(NewUI("Progress", detail.transform), "기록 0%", 21);
            TextMeshProUGUI description = AddText(NewUI("Description", detail.transform), "???", 20);
            description.gameObject.AddComponent<LayoutElement>().preferredHeight = 130f;
            TextMeshProUGUI bonuses = AddText(NewUI("Bonuses", detail.transform), "???", 21);

            SerializedObject serialized = new(menu);
            Set(serialized, "_sceneContent", window.GetComponent<RectTransform>());
            Set(serialized, "_gradeFilter", grade);
            Set(serialized, "_elementFilter", element);
            Set(serialized, "_listContent", content.transform);
            Set(serialized, "_slotPrefab", slotPrefab);
            Set(serialized, "_detailGroup", detailGroup);
            Set(serialized, "_portrait", portrait);
            Set(serialized, "_name", name);
            Set(serialized, "_description", description);
            Set(serialized, "_element", elementLabel);
            Set(serialized, "_progress", progressLabel);
            Set(serialized, "_bonuses", bonuses);
            Set(serialized, "_closeButton", close);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, MainPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static void RegisterUI(GameObject prefab)
        {
            string[] guids = AssetDatabase.FindAssets("t:UIPrefabDatabase");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[MonsterCodexUIBuilder] UIPrefabDatabase를 찾지 못했습니다.");
                return;
            }

            UIPrefabDatabase database = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
            if (!database.HasKey(UIKeyType.MonsterCodex.ToKey()))
                database.AddPrefab(
                    UIKeyType.MonsterCodex.ToKey(),
                    prefab,
                    CanvasLayer.Scene,
                    "몬스터 도감");
        }

        private static void BindMenuButton()
        {
            if (!File.Exists(MenuPanelPath))
            {
                Debug.LogWarning($"[MonsterCodexUIBuilder] 메뉴 프리팹 없음: {MenuPanelPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(MenuPanelPath);
            try
            {
                UI_MenuPanel panel = root.GetComponent<UI_MenuPanel>();
                if (panel == null)
                {
                    Debug.LogWarning("[MonsterCodexUIBuilder] UI_MenuPanel 컴포넌트를 찾지 못했습니다.");
                    return;
                }

                SerializedObject serialized = new(panel);
                SerializedProperty codexProperty = serialized.FindProperty("_codexButton");
                Button codex = codexProperty.objectReferenceValue as Button;
                Button party = serialized.FindProperty("_partyButton").objectReferenceValue as Button;
                if (party == null)
                {
                    Debug.LogWarning("[MonsterCodexUIBuilder] 파티 버튼을 찾지 못했습니다.");
                    return;
                }

                Transform partySlot = party.transform.parent;
                Transform gridTransform = partySlot != null ? partySlot.parent : null;
                GridLayoutGroup grid = gridTransform != null
                    ? gridTransform.GetComponent<GridLayoutGroup>()
                    : null;
                if (partySlot == null || grid == null)
                {
                    Debug.LogWarning("[MonsterCodexUIBuilder] 메뉴 슬롯 그리드를 찾지 못했습니다.");
                    return;
                }

                // 구버전 빌더는 PartyButton만 복제해 PartySlot 안에 겹쳐 놓았다.
                if (codex != null && codex.transform.parent == partySlot)
                {
                    Object.DestroyImmediate(codex.gameObject);
                    codex = null;
                    codexProperty.objectReferenceValue = null;
                }

                Transform codexSlot = gridTransform.Find("CodexSlot");
                if (codexSlot == null)
                {
                    GameObject clone = Object.Instantiate(partySlot.gameObject, gridTransform);
                    clone.name = "CodexSlot";
                    codexSlot = clone.transform;
                    codex = clone.GetComponentInChildren<Button>(true);
                }
                else
                {
                    codex = codexSlot.GetComponentInChildren<Button>(true);
                }

                if (codex == null)
                {
                    Debug.LogWarning("[MonsterCodexUIBuilder] 생성된 도감 슬롯에 Button이 없습니다.");
                    return;
                }

                codex.gameObject.name = "CodexButton";
                foreach (TextMeshProUGUI label in
                         codexSlot.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    label.text = "도감";
                }

                Sprite codexIcon = AssetDatabase.LoadAssetAtPath<Sprite>(CodexMenuIconPath);
                Image icon = codex.transform.childCount > 0
                    ? codex.transform.GetChild(0).GetComponent<Image>()
                    : null;
                if (icon != null && codexIcon != null)
                    icon.sprite = codexIcon;

                codexProperty.objectReferenceValue = codex;
                ConfigureMenuGrid(grid);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, MenuPanelPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool NeedsMenuLayoutRepair()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MenuPanelPath);
            try
            {
                UI_MenuPanel panel = root.GetComponent<UI_MenuPanel>();
                if (panel == null)
                    return false;

                SerializedObject serialized = new(panel);
                Button party = serialized.FindProperty("_partyButton").objectReferenceValue as Button;
                Button codex = serialized.FindProperty("_codexButton").objectReferenceValue as Button;
                if (party == null || codex == null)
                    return true;

                Transform partySlot = party.transform.parent;
                Transform gridTransform = partySlot != null ? partySlot.parent : null;
                GridLayoutGroup grid = gridTransform != null
                    ? gridTransform.GetComponent<GridLayoutGroup>()
                    : null;
                if (codex.transform.parent == partySlot ||
                    grid == null ||
                    codex.transform.parent.parent != gridTransform ||
                    gridTransform.Find("CodexSlot") == null)
                {
                    return true;
                }

                RectTransform codexSlot = codex.transform.parent as RectTransform;
                return grid.enabled ||
                       codexSlot == null ||
                       codexSlot.anchorMin.x < 0.8f;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool NeedsCodexLayoutRepair()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(MainPath);
            try
            {
                UI_MonsterCodex codex = root.GetComponent<UI_MonsterCodex>();
                if (codex == null)
                    return true;

                HorizontalOrVerticalLayoutGroup[] groups =
                    root.GetComponentsInChildren<HorizontalOrVerticalLayoutGroup>(true);
                if (groups.Length == 0)
                    return true;

                foreach (HorizontalOrVerticalLayoutGroup group in groups)
                {
                    if (!group.childControlWidth || !group.childControlHeight)
                        return true;
                }

                UIMonsterCodexSlot slot =
                    AssetDatabase.LoadAssetAtPath<UIMonsterCodexSlot>(SlotPath);
                Transform window = root.transform.Find("Window");
                Transform body = window != null ? window.Find("Body") : null;
                RectTransform bodyRect = body as RectTransform;
                return window == null ||
                       window.GetComponent<VerticalLayoutGroup>() != null ||
                       bodyRect == null ||
                       !Mathf.Approximately(bodyRect.offsetMax.y, -156f) ||
                       root.GetComponent<GraphicRaycaster>() == null ||
                       slot == null ||
                       slot.GetComponent<Button>() == null;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureMenuGrid(GridLayoutGroup grid)
        {
            grid.enabled = false;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(220f, 270f);
            grid.spacing = new Vector2(0f, 10f);
            grid.padding = new RectOffset(10, 10, 20, 0);

            var slots = new System.Collections.Generic.List<RectTransform>();
            foreach (Transform slot in grid.transform)
            {
                Button button = slot.GetComponentInChildren<Button>(true);
                if (button == null)
                    continue;

                if (slot is RectTransform slotRect)
                    slots.Add(slotRect);

                RectTransform buttonRect = button.transform as RectTransform;
                if (buttonRect != null)
                    buttonRect.sizeDelta = new Vector2(200f, 200f);

                if (button.transform.childCount == 0)
                    continue;

                RectTransform iconRect = button.transform.GetChild(0) as RectTransform;
                if (iconRect != null)
                {
                    iconRect.sizeDelta = new Vector2(130f, 130f);
                    iconRect.anchoredPosition = new Vector2(0f, 12f);
                }
            }

            // 4 + 3 구성에서 마지막 줄을 반 칸 중앙으로 이동한다.
            for (int index = 0; index < slots.Count; index++)
            {
                bool secondRow = index >= 4;
                int column = secondRow ? index - 4 : index;
                int columnCount = secondRow ? Mathf.Max(1, slots.Count - 4) : 4;
                float normalizedX = (column + 0.5f) / columnCount;

                RectTransform slot = slots[index];
                slot.anchorMin = slot.anchorMax = new Vector2(normalizedX, 1f);
                slot.pivot = new Vector2(0.5f, 1f);
                slot.anchoredPosition = new Vector2(0f, secondRow ? -300f : -20f);
                slot.sizeDelta = new Vector2(220f, 270f);
                slot.localScale = Vector3.one;
            }
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static TextMeshProUGUI AddText(GameObject go, string value, float size)
        {
            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = Text;
            text.alignment = TextAlignmentOptions.Left;
            return text;
        }

        private static Button MakeButton(string name, Transform parent, string label)
        {
            GameObject go = NewUI(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = Slot;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            TextMeshProUGUI text = AddText(NewUI("Label", go.transform), label, 20);
            text.alignment = TextAlignmentOptions.Center;
            Stretch(text.rectTransform);
            return button;
        }

        private static TMP_Dropdown MakeDropdown(string name, Transform parent)
        {
            GameObject go = NewUI(name, parent);
            go.AddComponent<Image>().color = Slot;
            LayoutElement dropdownLayout = go.AddComponent<LayoutElement>();
            dropdownLayout.preferredWidth = 260f;
            dropdownLayout.preferredHeight = 52f;
            TMP_Dropdown dropdown = go.AddComponent<TMP_Dropdown>();
            TextMeshProUGUI caption = AddText(NewUI("Label", go.transform), "전체", 19);
            Stretch(caption.rectTransform, 12f);
            dropdown.captionText = caption;

            GameObject template = NewUI("Template", go.transform);
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0f, 240f);
            template.AddComponent<Image>().color = Panel;
            ScrollRect scroll = template.AddComponent<ScrollRect>();

            GameObject viewport = NewUI("Viewport", template.transform);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            GameObject content = NewUI("Content", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = Vector2.zero;
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            GameObject item = NewUI("Item", content.transform);
            item.AddComponent<LayoutElement>().preferredHeight = 38f;
            Image itemImage = item.AddComponent<Image>();
            itemImage.color = Slot;
            Toggle toggle = item.AddComponent<Toggle>();
            toggle.targetGraphic = itemImage;
            TextMeshProUGUI itemLabel = AddText(NewUI("Item Label", item.transform), "옵션", 18);
            Stretch(itemLabel.rectTransform, 10f);

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;
            template.SetActive(false);
            return dropdown;
        }

        private static void Set(SerializedObject serialized, string name, Object value)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Center(RectTransform rect, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopRect(
            RectTransform rect,
            float left,
            float right,
            float top,
            float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            string[] parts = path.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
