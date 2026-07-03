using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Inventory.EditorTools
{
    /// <summary>
    /// 인벤토리 UI(UI_Inventory) 프리팹 초안을 코드로 생성하고 SerializeField를 자동 연결하는 에디터 툴.
    ///
    /// - 기존 UI_Inventory.prefab의 루트/스크립트(guid)는 유지한 채 자식 계층만 재구성(덮어쓰기).
    /// - 슬롯 서브 프리팹(UI_InventorySlot)도 생성해 참조 연결.
    /// - 재실행 가능(idempotent). "동작하는 회색 초안"이 목표.
    /// - 정렬은 하단 "정렬" 버튼(클릭 시 순환)으로 처리하며 헤더 TMP_Dropdown은 생성하지 않는다
    ///   (UI_Inventory는 둘 중 하나만 있어도 동작. 드롭다운이 필요하면 Unity에서 수동 추가·연결).
    /// </summary>
    public static class UIInventoryPrefabBuilder
    {
        private const string MainPrefabPath = "Assets/03.Prefabs/UI/Scene/Inventory/UI_Inventory.prefab";
        private const string SlotPrefabPath = "Assets/03.Prefabs/UI/Scene/Inventory/UI_InventorySlot.prefab";

        private static readonly Color Dim       = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color WindowBg  = new Color(0.07f, 0.09f, 0.12f, 0.98f);
        private static readonly Color PanelBg   = new Color(0.11f, 0.14f, 0.18f, 1f);
        private static readonly Color SlotBg    = new Color(0.16f, 0.19f, 0.24f, 1f);
        private static readonly Color BtnBg     = new Color(0.20f, 0.28f, 0.34f, 1f);
        private static readonly Color AccentBtn = new Color(0.18f, 0.45f, 0.55f, 1f);
        private static readonly Color DangerBtn = new Color(0.45f, 0.18f, 0.18f, 1f);
        private static readonly Color TextMain  = new Color(0.90f, 0.92f, 0.95f, 1f);
        private static readonly Color TextSub   = new Color(0.65f, 0.70f, 0.76f, 1f);
        private static readonly Color Gold      = new Color(0.95f, 0.78f, 0.35f, 1f);
        private static readonly Color Accent    = new Color(0.35f, 0.80f, 0.90f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        [MenuItem("UPlayGround/UI/인벤토리 UI 프리팹 빌드 (초안)")]
        public static void Build()
        {
            if (!System.IO.File.Exists(MainPrefabPath))
            {
                EditorUtility.DisplayDialog("인벤토리 UI 빌더",
                    $"대상 프리팹을 찾을 수 없습니다:\n{MainPrefabPath}", "확인");
                return;
            }

            var slot = BuildSlotPrefab();

            var root = PrefabUtility.LoadPrefabContents(MainPrefabPath);
            try
            {
                var inv = root.GetComponent<UI_Inventory>();
                if (inv == null)
                {
                    Debug.LogError("[InvBuilder] 루트에 UI_Inventory 컴포넌트가 없습니다. 중단.");
                    return;
                }

                ClearChildren(root.transform);

                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                AddImage(dim, Dim);

                var window = NewUI("Window", root.transform);
                Center(Rt(window), 1720, 960);
                AddImage(window, WindowBg, UISprite, sliced: true);
                AddVLG(window, spacing: 8, pad: 16).childForceExpandHeight = false;

                // ── 헤더 ──
                var header = NewUI("Header", window.transform);
                SetHeight(header, 60);
                var title = NewUI("Title", header.transform);
                Stretch(title);
                AddText(title, "인벤토리", 34, TextMain, TextAlignmentOptions.Center);
                var btnClose = MakeButton("BtnClose", header.transform, "X", out _);
                AnchorTopRight(Rt(btnClose.gameObject), 48, 48);

                // ── 본문(좌 탭 / 중앙 그리드 / 우 상세) ──
                var body = NewUI("Body", window.transform);
                AddFlexible(body, 1);
                AddHLG(body, spacing: 12, pad: 0).childForceExpandHeight = true;

                // ===== 좌측 카테고리 탭 =====
                var left = NewUI("CategoryPanel", body.transform);
                AddImage(left, PanelBg, UISprite, sliced: true);
                SetWidth(left, 240);
                AddVLG(left, spacing: 6, pad: 10).childForceExpandHeight = false;
                var tabAll   = MakeTab("TabAll",       left.transform, "전체");
                var tabCons  = MakeTab("TabConsumable", left.transform, "소비");
                var tabEquip = MakeTab("TabEquipment",  left.transform, "장비");
                var tabMat   = MakeTab("TabMaterial",   left.transform, "재료");
                var tabQuest = MakeTab("TabQuest",      left.transform, "퀘스트");
                var tabImp   = MakeTab("TabImportant",  left.transform, "중요 아이템");

                // ===== 중앙 그리드 =====
                var center = NewUI("GridPanel", body.transform);
                AddFlexibleW(center, 1f);
                AddVLG(center, spacing: 8, pad: 0).childForceExpandHeight = false;

                var gridHeader = NewUI("GridHeader", center.transform);
                SetHeight(gridHeader, 36);
                AddHLG(gridHeader, spacing: 8, pad: 0);
                var txtCount = AddText(NewUI("ItemCount", gridHeader.transform), "전체 0 / 120", 20, TextSub, TextAlignmentOptions.Left);
                AddFlexibleW(txtCount.gameObject, 1f);

                var gridScroll = NewUI("GridScroll", center.transform);
                AddFlexible(gridScroll, 1);
                var gridContent = BuildGridScroll(gridScroll);

                // ===== 우측 상세 =====
                var right = NewUI("DetailPanel", body.transform);
                AddImage(right, PanelBg, UISprite, sliced: true);
                SetWidth(right, 480);
                AddVLG(right, spacing: 8, pad: 14).childForceExpandHeight = false;

                // 이름 + 등급
                var nameRow = NewUI("NameRow", right.transform);
                SetHeight(nameRow, 44);
                AddHLG(nameRow, spacing: 8, pad: 0);
                var txtName = AddText(NewUI("Name", nameRow.transform), "아이템 이름", 28, TextMain, TextAlignmentOptions.Left);
                AddFlexibleW(txtName.gameObject, 1f);
                var txtRarity = AddText(NewUI("Rarity", nameRow.transform), "희귀", 22, Accent, TextAlignmentOptions.Right);
                SetWidth(txtRarity.gameObject, 120);

                var txtType = AddText(NewUI("Type", right.transform), "장비", 18, TextSub, TextAlignmentOptions.Left);
                SetHeight(txtType.gameObject, 24);

                // 아이콘 + 보유수량/무게
                var iconRow = NewUI("IconRow", right.transform);
                SetHeight(iconRow, 180);
                AddHLG(iconRow, spacing: 12, pad: 0);
                var iconGo = NewUI("Icon", iconRow.transform);
                SetWidth(iconGo, 180);
                var imgIcon = AddImage(iconGo, SlotBg, UISprite, sliced: true);
                var infoCol = NewUI("Info", iconRow.transform);
                AddFlexibleW(infoCol, 1f);
                AddVLG(infoCol, spacing: 6, pad: 8).childForceExpandHeight = false;
                var txtCountDetail = BuildInfoRow(infoCol.transform, "보유 수량", "0", out _);
                var txtWeightDetail = BuildInfoRow(infoCol.transform, "무게", "0.0", out _);

                var txtDesc = AddText(NewUI("Description", right.transform), "아이템 설명", 20, TextSub, TextAlignmentOptions.TopLeft);
                SetHeight(txtDesc.gameObject, 80);

                // 장착 부위
                var equipRow = NewUI("EquipSlotRow", right.transform);
                SetHeight(equipRow, 34);
                AddHLG(equipRow, spacing: 8, pad: 0);
                AddText(NewUI("Label", equipRow.transform), "장착 부위", 18, TextSub, TextAlignmentOptions.Left);
                var txtEquipSlot = AddText(NewUI("Value", equipRow.transform), "보조 무기", 18, TextMain, TextAlignmentOptions.Right);
                AddFlexibleW(txtEquipSlot.gameObject, 1f);

                // 능력치 패널
                var statPanel = NewUI("StatPanel", right.transform);
                AddImage(statPanel, SlotBg, UISprite, sliced: true);
                AddFlexible(statPanel, 1);
                AddVLG(statPanel, spacing: 4, pad: 10).childForceExpandHeight = false;
                AddText(NewUI("Title", statPanel.transform), "능력치", 20, TextMain, TextAlignmentOptions.Left);
                var txtAtk     = BuildInfoRow(statPanel.transform, "공격력",      "0",   out _);
                var txtCrit    = BuildInfoRow(statPanel.transform, "치명타 확률", "0%",  out _);
                var txtCritDmg = BuildInfoRow(statPanel.transform, "치명타 피해", "0%",  out _);
                var txtAtkSpd  = BuildInfoRow(statPanel.transform, "공격 속도",   "1.0", out _);

                // ── 하단 바 ──
                var bottom = NewUI("BottomBar", window.transform);
                SetHeight(bottom, 78);
                AddHLG(bottom, spacing: 10, pad: 4);

                // 골드
                var goldBox = NewUI("GoldBox", bottom.transform);
                SetWidth(goldBox, 220);
                AddHLG(goldBox, spacing: 8, pad: 6);
                var coin = NewUI("Coin", goldBox.transform);
                SetWidth(coin, 40);
                AddImage(coin, Gold, UISprite, sliced: true);
                var txtGold = AddText(NewUI("Gold", goldBox.transform), "0", 24, Gold, TextAlignmentOptions.Left);
                AddFlexibleW(txtGold.gameObject, 1f);

                // 무게 바
                var weightBox = NewUI("WeightBox", bottom.transform);
                SetWidth(weightBox, 340);
                var wbBar = NewUI("Bar", weightBox.transform);
                Stretch(wbBar);
                AddImage(wbBar, SlotBg, UISprite, sliced: true);
                var wbFill = NewUI("Fill", wbBar.transform);
                Stretch(wbFill);
                var imgWeightFill = AddImage(wbFill, AccentBtn, UISprite, sliced: true);
                imgWeightFill.type = Image.Type.Filled;
                imgWeightFill.fillMethod = Image.FillMethod.Horizontal;
                imgWeightFill.fillAmount = 0f;
                var wbText = NewUI("Text", wbBar.transform);
                Stretch(wbText);
                var txtWeight = AddText(wbText, "(0.0/500.0)", 18, TextMain, TextAlignmentOptions.Center);
                txtWeight.raycastTarget = false;

                var spacer = NewUI("Spacer", bottom.transform);
                AddFlexibleW(spacer, 1f);

                var btnSort  = MakeCommonButton("BtnSort",  bottom.transform, "정렬",   BtnBg,     out _);
                SetWidth(btnSort.gameObject, 150);
                var btnUse   = MakeCommonButton("BtnUse",   bottom.transform, "사용",   BtnBg,     out _);
                SetWidth(btnUse.gameObject, 150);
                var btnEquip = MakeCommonButton("BtnEquip", bottom.transform, "장착",   AccentBtn, out _);
                SetWidth(btnEquip.gameObject, 150);
                var btnDrop  = MakeCommonButton("BtnDrop",  bottom.transform, "버리기", DangerBtn, out _);
                SetWidth(btnDrop.gameObject, 150);

                // 호버 하이라이트 (코드에서 슬롯으로 재부모됨)
                var clickTap = NewUI("ItemClickTap", window.transform);
                Center(Rt(clickTap), 78, 78);
                var tapImg = AddImage(clickTap, new Color(0.35f, 0.80f, 0.90f, 0.25f), UISprite, sliced: true);
                tapImg.raycastTarget = false;
                clickTap.SetActive(false);

                // ── 필드 연결 ──
                var so = new SerializedObject(inv);
                SetRef(so, "_itemPanelPrefab", slot);
                SetRef(so, "_content",         gridContent.transform);
                SetRef(so, "_imgWeightFill",   imgWeightFill);
                SetRef(so, "_txtWeight",       txtWeight);
                SetRef(so, "_itemClickTap",    clickTap);

                SetRef(so, "_tabAll",        tabAll);
                SetRef(so, "_tabConsumable", tabCons);
                SetRef(so, "_tabEquipment",  tabEquip);
                SetRef(so, "_tabMaterial",   tabMat);
                SetRef(so, "_tabQuest",      tabQuest);
                SetRef(so, "_tabImportant",  tabImp);

                SetRef(so, "_txtItemCount", txtCount);
                SetRef(so, "_txtGold",      txtGold);
                SetRef(so, "_sortButton",   btnSort);

                SetRef(so, "_selectedItemPrefab",    right);
                SetRef(so, "_selectedItemImage",     imgIcon);
                SetRef(so, "_selectedItemCountText", txtCountDetail);
                SetRef(so, "_selectedItemNameText",  txtName);
                SetRef(so, "_selectedItemTypeText",  txtType);
                SetRef(so, "_selectedItemDescText",  txtDesc);

                SetRef(so, "_selectedRarityText",    txtRarity);
                SetRef(so, "_selectedWeightText",    txtWeightDetail);
                SetRef(so, "_selectedEquipSlotText", txtEquipSlot);
                SetRef(so, "_statPanel",             statPanel);
                SetRef(so, "_statAttackText",        txtAtk);
                SetRef(so, "_statCritText",          txtCrit);
                SetRef(so, "_statCritDmgText",       txtCritDmg);
                SetRef(so, "_statAtkSpeedText",      txtAtkSpd);

                SetRef(so, "_useButton",   btnUse);
                SetRef(so, "_equipButton", btnEquip);
                SetRef(so, "_dropButton",  btnDrop);

                // 풀링 단위를 그리드 열 수(8)와 맞춤
                var pRow = so.FindProperty("_slotCountPerRow");
                if (pRow != null) pRow.intValue = 8;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
                Debug.Log("[InvBuilder] UI_Inventory 프리팹 초안 생성 완료.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
        }

        // ──────────────────────────────────────────────────────────
        #region 슬롯 서브 프리팹

        private static UI_InventorySlot BuildSlotPrefab()
        {
            var go = NewUI("UI_InventorySlot", null);
            Center(Rt(go), 78, 78);
            // UI_InventorySlot(UI_Base 상속)은 [RequireComponent(Canvas)]로 Canvas가 자동 추가된다.
            var slot = go.AddComponent<UI_InventorySlot>();

            // 빈 슬롯 프레임
            var empty = NewUI("EmptySlot", go.transform);
            Stretch(empty);
            AddImage(empty, new Color(0.10f, 0.12f, 0.15f, 1f), UISprite, sliced: true);

            // 콘텐츠 루트
            var content = NewUI("Content", go.transform);
            Stretch(content);

            var rarity = NewUI("Rarity", content.transform);
            Stretch(rarity);
            var imgRarity = AddImage(rarity, Color.white, UISprite, sliced: true);

            var icon = NewUI("Icon", content.transform);
            InsetStretch(Rt(icon), 6);
            var imgItem = AddImage(icon, Color.white);

            var enhance = NewUI("Enhance", content.transform);
            AnchorBottomLeft(Rt(enhance), 40, 24);
            var txtEnhance = AddText(enhance, "+5", 18, Accent, TextAlignmentOptions.BottomLeft);
            txtEnhance.raycastTarget = false;

            var count = NewUI("Count", content.transform);
            AnchorBottomRight(Rt(count), 40, 24);
            var txtCount = AddText(count, "1", 18, TextMain, TextAlignmentOptions.BottomRight);
            txtCount.raycastTarget = false;

            var weight = NewUI("Weight", content.transform);
            AnchorTopLeft(Rt(weight), 44, 20);
            var txtWeight = AddText(weight, "0.0", 14, TextSub, TextAlignmentOptions.TopLeft);
            txtWeight.raycastTarget = false;

            var so = new SerializedObject(slot);
            SetRef(so, "_rootContent",   content);
            SetRef(so, "_rootEmptySlot", empty);
            SetRef(so, "_txtCount",      txtCount);
            SetRef(so, "_txtWeight",     txtWeight);
            SetRef(so, "_txtEnhance",    txtEnhance);
            SetRef(so, "_imgItem",       imgItem);
            SetRef(so, "_imgRarity",     imgRarity);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, SlotPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UI_InventorySlot>();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region UI 빌드 헬퍼

        private static RectTransform Rt(GameObject go) => go.GetComponent<RectTransform>();

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go)
        {
            var rt = Rt(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void InsetStretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Center(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void AnchorTopRight(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-8, -8);
        }

        private static void AnchorTopLeft(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(4, -4);
        }

        private static void AnchorBottomRight(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-4, 2);
        }

        private static void AnchorBottomLeft(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(4, 2);
        }

        private static Image AddImage(GameObject go, Color color, Sprite sprite = null, bool sliced = false)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
            return img;
        }

        private static TextMeshProUGUI AddText(GameObject go, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
            return t;
        }

        private static Button MakeButton(string name, Transform parent, string label, out TextMeshProUGUI labelText, Color? bg = null)
        {
            var go = NewUI(name, parent);
            var img = AddImage(go, bg ?? BtnBg, UISprite, sliced: true);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var lblGo = NewUI("Label", go.transform);
            Stretch(lblGo);
            labelText = AddText(lblGo, label, 22, TextMain, TextAlignmentOptions.Center);
            labelText.raycastTarget = false;
            return btn;
        }

        private static Button MakeTab(string name, Transform parent, string label)
        {
            var btn = MakeButton(name, parent, label, out var lbl, BtnBg);
            lbl.alignment = TextAlignmentOptions.Left;
            lbl.margin = new Vector4(12, 0, 0, 0);
            SetHeight(btn.gameObject, 52);
            return btn;
        }

        private static UICommonButton MakeCommonButton(string name, Transform parent, string label, Color bg, out TextMeshProUGUI labelText)
        {
            var btn = MakeButton(name, parent, label, out labelText, bg);
            var cb = btn.gameObject.AddComponent<UICommonButton>();
            var so = new SerializedObject(cb);
            SetRef(so, "_button", btn);
            SetRef(so, "_buttonText", labelText);
            so.ApplyModifiedPropertiesWithoutUndo();
            return cb;
        }

        /// <summary> "라벨 ......... 값" 한 줄. 값 TMP를 반환. </summary>
        private static TextMeshProUGUI BuildInfoRow(Transform parent, string label, string value, out TextMeshProUGUI labelText)
        {
            var row = NewUI(label + "Row", parent);
            SetHeight(row.gameObject, 30);
            AddHLG(row, spacing: 8, pad: 0);
            labelText = AddText(NewUI("Label", row.transform), label, 18, TextSub, TextAlignmentOptions.Left);
            AddFlexibleW(labelText.gameObject, 1f);
            var val = AddText(NewUI("Value", row.transform), value, 18, TextMain, TextAlignmentOptions.Right);
            SetWidth(val.gameObject, 120);
            return val;
        }

        private static VerticalLayoutGroup AddVLG(GameObject go, float spacing, int pad)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        private static HorizontalLayoutGroup AddHLG(GameObject go, float spacing, int pad)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            h.childAlignment = TextAnchor.MiddleCenter;
            return h;
        }

        private static void SetHeight(GameObject go, float hgt)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = hgt;
            le.flexibleHeight = 0;
        }

        private static void SetWidth(GameObject go, float w)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = w;
            le.flexibleWidth = 0;
        }

        private static void AddFlexible(GameObject go, float flexH)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleHeight = flexH;
        }

        private static void AddFlexibleW(GameObject go, float flexW)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = flexW;
        }

        private static GameObject BuildGridScroll(GameObject scrollGo)
        {
            AddImage(scrollGo, new Color(0.05f, 0.06f, 0.08f, 1f), UISprite, sliced: true);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = NewUI("Viewport", scrollGo.transform);
            Stretch(viewport);
            AddImage(viewport, new Color(1, 1, 1, 0.01f));
            viewport.AddComponent<RectMask2D>();

            var content = NewUI("Content", viewport.transform);
            var crt = Rt(content);
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = Vector2.zero;

            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(78, 78);
            grid.spacing = new Vector2(6, 6);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = Rt(viewport);
            scrollRect.content = crt;
            return content;
        }

        private static void SetRef(SerializedObject so, string propName, UnityEngine.Object value)
        {
            var p = so.FindProperty(propName);
            if (p == null)
            {
                Debug.LogWarning($"[InvBuilder] 직렬화 프로퍼티를 찾을 수 없음: {propName}");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        #endregion
    }
}
