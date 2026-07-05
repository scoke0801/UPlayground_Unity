using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;

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
        private const string PartyEntryPrefabPath = "Assets/03.Prefabs/UI/Scene/Inventory/UIPartyEquipSelectorEntry.prefab";

        private const int GridColumnCount = 10;
        private const int InitialRowCount = 12;
        private const float SlotCellSize = 155f;
        private const float SlotOverlaySize = 150f;

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
            var partyEntry = BuildPartyEntryPrefab();

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
                StretchInset(window, 18, 8, 18, 8);
                AddImage(window, WindowBg, UISprite, sliced: true);
                AddVLG(window, spacing: 8, pad: 14).childForceExpandHeight = false;

                // ── 헤더 ──
                var header = NewUI("Header", window.transform);
                SetHeight(header, 48);
                var title = NewUI("Title", header.transform);
                Stretch(title);
                AddText(title, "인벤토리", 28, TextMain, TextAlignmentOptions.Center);
                var btnClose = MakeButton("BtnClose", header.transform, "X", out _);
                AnchorTopRight(Rt(btnClose.gameObject), 38, 38);

                // ── 파티 장비 바 (헤더와 본문 사이, 전체 폭) — 본문 3열 레이아웃을 건드리지 않는다 ──
                var partyBar = NewUI("PartyEquipBar", window.transform);
                SetHeight(partyBar, 150);
                AddImage(partyBar, PanelBg, UISprite, sliced: true);
                AddHLG(partyBar, spacing: 12, pad: 8).childForceExpandHeight = true;

                // 파티원 선택 영역 (런타임에 Roster로 채움)
                var partyBox = NewUI("PartyBox", partyBar.transform);
                AddFlexibleW(partyBox, 1f);
                AddVLG(partyBox, spacing: 4, pad: 4).childForceExpandHeight = false;
                var partyTitle = AddText(NewUI("PartyTitle", partyBox.transform), "파티", 18, TextMain, TextAlignmentOptions.Left);
                SetHeight(partyTitle.gameObject, 22);
                var partySelector = NewUI("PartySelector", partyBox.transform);
                AddFlexible(partySelector, 1);
                AddImage(partySelector, new Color(0.08f, 0.09f, 0.12f, 0.6f), UISprite, sliced: true);
                var pGrid = partySelector.AddComponent<GridLayoutGroup>();
                pGrid.cellSize = new Vector2(80, 96);
                pGrid.spacing = new Vector2(6, 6);
                pGrid.padding = new RectOffset(8, 8, 8, 8);
                pGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
                pGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                pGrid.constraintCount = 1;

                // 선택 캐릭터 장비 영역 (주/보조 무기 + 방어구 5)
                var equipBox = NewUI("EquipBox", partyBar.transform);
                SetWidth(equipBox, 600);
                AddVLG(equipBox, spacing: 4, pad: 4).childForceExpandHeight = false;
                var txtSelChar = AddText(NewUI("SelectedCharName", equipBox.transform), "캐릭터", 18, Accent, TextAlignmentOptions.Left);
                SetHeight(txtSelChar.gameObject, 22);
                var equipGrid = NewUI("EquipmentSlots", equipBox.transform);
                AddFlexible(equipGrid, 1);
                AddImage(equipGrid, new Color(0.08f, 0.09f, 0.12f, 0.6f), UISprite, sliced: true);
                var eGrid = equipGrid.AddComponent<GridLayoutGroup>();
                eGrid.cellSize = new Vector2(72, 88);
                eGrid.spacing = new Vector2(6, 6);
                eGrid.padding = new RectOffset(8, 8, 8, 8);
                eGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
                eGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                eGrid.constraintCount = 1;

                var slotDefs = new (EquipPosition pos, string label)[]
                {
                    (EquipPosition.RightHand, "주무기"), (EquipPosition.LeftHand, "보조"),
                    (EquipPosition.Head, "머리"),        (EquipPosition.Chest, "상의"),
                    (EquipPosition.Pants, "하의"),       (EquipPosition.Shoes, "신발"),
                    (EquipPosition.Gloves, "장갑")
                };
                var equipSlots = new UIEquipmentSlot[slotDefs.Length];
                for (int i = 0; i < slotDefs.Length; i++)
                    equipSlots[i] = BuildEquipmentSlot(equipGrid.transform, slotDefs[i].pos, slotDefs[i].label);

                // ── 본문(좌 탭 / 중앙 그리드 / 우 상세) ──
                var body = NewUI("Body", window.transform);
                AddFlexible(body, 1);
                AddHLG(body, spacing: 12, pad: 0).childForceExpandHeight = true;

                // ===== 좌측 카테고리 탭 =====
                var left = NewUI("CategoryPanel", body.transform);
                AddImage(left, PanelBg, UISprite, sliced: true);
                SetWidth(left, minWidth: 240, preferredWidth: 145);
                AddVLG(left, spacing: 6, pad: 8).childForceExpandHeight = false;
                var tabAll   = MakeTab("TabAll",       left.transform, "전체");
                var tabCons  = MakeTab("TabConsumable", left.transform, "소비");
                var tabEquip = MakeTab("TabEquipment",  left.transform, "장비");
                var tabMat   = MakeTab("TabMaterial",   left.transform, "재료");
                var tabQuest = MakeTab("TabQuest",      left.transform, "퀘스트");
                var tabImp   = MakeTab("TabImportant",  left.transform, "중요 아이템");

                // 탭 그룹(단일 선택 관리) — 배치 순서는 UI_Inventory.TabOrder와 반드시 일치
                var tabGroup = left.AddComponent<UITabGroup>();
                tabGroup.SetTabs(new[] { tabAll, tabCons, tabEquip, tabMat, tabQuest, tabImp });

                // ===== 중앙 그리드 =====
                var center = NewUI("GridPanel", body.transform);
                AddImage(center, new Color(0.08f, 0.09f, 0.12f, 0.75f), UISprite, sliced: true);
                AddFlexibleW(center, 1f);
                AddVLG(center, spacing: 8, pad: 10).childForceExpandHeight = false;

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
                SetWidth(right, 300);
                AddVLG(right, spacing: 8, pad: 12).childForceExpandHeight = false;

                // 이름 + 등급
                var nameRow = NewUI("NameRow", right.transform);
                SetHeight(nameRow, 36);
                AddHLG(nameRow, spacing: 8, pad: 0);
                var txtName = AddText(NewUI("Name", nameRow.transform), "아이템 이름", 22, TextMain, TextAlignmentOptions.Left);
                AddFlexibleW(txtName.gameObject, 1f);
                var txtRarity = AddText(NewUI("Rarity", nameRow.transform), "희귀", 18, Accent, TextAlignmentOptions.Right);
                SetWidth(txtRarity.gameObject, 72);

                var txtType = AddText(NewUI("Type", right.transform), "장비", 18, TextSub, TextAlignmentOptions.Left);
                SetHeight(txtType.gameObject, 24);

                // 아이콘 + 보유수량/무게
                var iconRow = NewUI("IconRow", right.transform);
                SetHeight(iconRow, 130);
                AddHLG(iconRow, spacing: 12, pad: 0);
                var iconGo = NewUI("Icon", iconRow.transform);
                SetWidth(iconGo, 112);
                AddImage(iconGo, SlotBg, UISprite, sliced: true);
                var itemIconGo = NewUI("ItemImage", iconGo.transform);
                InsetStretch(Rt(itemIconGo), 6);
                var imgIcon = AddImage(itemIconGo, Color.white);
                var infoCol = NewUI("Info", iconRow.transform);
                AddFlexibleW(infoCol, 1f);
                AddVLG(infoCol, spacing: 6, pad: 8).childForceExpandHeight = false;
                var txtCountDetail = BuildInfoRow(infoCol.transform, "보유 수량", "0", out _);
                var txtWeightDetail = BuildInfoRow(infoCol.transform, "무게", "0.0", out _);

                var txtDesc = AddText(NewUI("Description", right.transform), "아이템 설명", 17, TextSub, TextAlignmentOptions.TopLeft);
                SetHeight(txtDesc.gameObject, 74);

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
                var txtAtk     = BuildStatOptionRow(statPanel.transform, "옵션 1");
                var txtCrit    = BuildStatOptionRow(statPanel.transform, "옵션 2");
                var txtCritDmg = BuildStatOptionRow(statPanel.transform, "옵션 3");
                var txtAtkSpd  = BuildStatOptionRow(statPanel.transform, "옵션 4");

                // ── 하단 바 ──
                var bottom = NewUI("BottomBar", window.transform);
                SetHeight(bottom, 58);
                AddHLG(bottom, spacing: 10, pad: 4);

                // 골드
                var goldBox = NewUI("GoldBox", bottom.transform);
                SetWidth(goldBox, 135);
                AddHLG(goldBox, spacing: 8, pad: 6);
                var coin = NewUI("Coin", goldBox.transform);
                SetWidth(coin, 40);
                AddImage(coin, Gold, UISprite, sliced: true);
                var txtGold = AddText(NewUI("Gold", goldBox.transform), "0", 24, Gold, TextAlignmentOptions.Left);
                AddFlexibleW(txtGold.gameObject, 1f);

                // 무게 바
                var weightBox = NewUI("WeightBox", bottom.transform);
                SetWidth(weightBox, 220);
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
                SetWidth(btnSort.gameObject, 112);
                var btnUse   = MakeCommonButton("BtnUse",   bottom.transform, "사용",   BtnBg,     out _);
                SetWidth(btnUse.gameObject, 112);
                var btnEquip = MakeCommonButton("BtnEquip", bottom.transform, "장착",   AccentBtn, out _);
                SetWidth(btnEquip.gameObject, 112);
                var btnDrop  = MakeCommonButton("BtnDrop",  bottom.transform, "버리기", DangerBtn, out _);
                SetWidth(btnDrop.gameObject, 112);

                // 호버 하이라이트 (코드에서 슬롯으로 재부모됨)
                var clickTap = NewUI("ItemClickTap", window.transform);
                Center(Rt(clickTap), SlotOverlaySize, SlotOverlaySize);
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

                SetRef(so, "_tabGroup",      tabGroup);

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

                // 파티 장비 패널
                SetRef(so, "_partySelectorContainer",   partySelector.transform);
                SetRef(so, "_partyEntryPrefab",         partyEntry);
                SetRef(so, "_selectedCharacterNameText", txtSelChar);
                SetRefArray(so, "_equipmentSlots", equipSlots);

                // 풀링 단위를 그리드 열 수와 맞춤: 기본 12행 = 120슬롯.
                // 표시 용량은 InventoryManager.MaxSlots(120)를 따른다.
                var pRow = so.FindProperty("_slotCountPerRow");
                if (pRow != null) pRow.intValue = GridColumnCount;
                var pStartRows = so.FindProperty("_startRowCount");
                if (pStartRows != null) pStartRows.intValue = InitialRowCount;
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
            Center(Rt(go), 64, 64);
            var slot = go.AddComponent<UI_InventorySlot>();

            // 포커스(선택) 하이라이트 — 슬롯보다 약간 크게, 첫 자식(뒤쪽)으로 두어 불투명한 슬롯 배경 밖 테두리 림만 보이게 함.
            // 이렇게 하면 아이콘 위를 덮지 않고 골드 테두리로 포커스를 표현한다.
            var focus = NewUI("FocusHighlight", go.transform);
            Center(Rt(focus), SlotOverlaySize, SlotOverlaySize);
            var focusImg = AddImage(focus, new Color(0.95f, 0.78f, 0.35f, 1f), UISprite, sliced: true);
            focusImg.raycastTarget = false;
            focus.SetActive(false);

            // 키보드/게임패드 네비게이션으로 포커스를 받도록 Selectable 부착.
            // 시각 전환은 UI_InventorySlot의 ISelectHandler에서 직접 처리하므로 transition은 None.
            var selectable = go.AddComponent<Selectable>();
            selectable.transition = Selectable.Transition.None;

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
            AnchorBottomLeft(Rt(enhance), 34, 20);
            var txtEnhance = AddText(enhance, "+5", 15, Accent, TextAlignmentOptions.BottomLeft);
            txtEnhance.raycastTarget = false;

            var count = NewUI("Count", content.transform);
            AnchorBottomRight(Rt(count), 34, 20);
            var txtCount = AddText(count, "1", 15, TextMain, TextAlignmentOptions.BottomRight);
            txtCount.raycastTarget = false;

            var weight = NewUI("Weight", content.transform);
            AnchorTopLeft(Rt(weight), 38, 18);
            var txtWeight = AddText(weight, "0.0", 12, TextSub, TextAlignmentOptions.TopLeft);
            txtWeight.raycastTarget = false;

            // 장착 중인 파티원 초상 뱃지 — 우상단 (여러 명이면 첫 초상 + "+N")
            var badge = NewUI("EquippedBadge", content.transform);
            AnchorTopRight(Rt(badge), 40, 40);
            var badgeImg = AddImage(badge, new Color(0f, 0f, 0f, 0.55f), UISprite, sliced: true);
            badgeImg.raycastTarget = false;
            var badgePortraitGo = NewUI("Portrait", badge.transform);
            InsetStretch(Rt(badgePortraitGo), 3);
            var badgePortrait = AddImage(badgePortraitGo, Color.white);
            badgePortrait.raycastTarget = false;
            var badgeTextGo = NewUI("Text", badge.transform);
            Stretch(badgeTextGo);
            var badgeText = AddText(badgeTextGo, "", 11, TextMain, TextAlignmentOptions.BottomRight);
            badgeText.raycastTarget = false;
            badge.SetActive(false);

            var so = new SerializedObject(slot);
            SetRef(so, "_rootContent",   content);
            SetRef(so, "_rootEmptySlot", empty);
            SetRef(so, "_txtCount",      txtCount);
            SetRef(so, "_txtWeight",     txtWeight);
            SetRef(so, "_txtEnhance",    txtEnhance);
            SetRef(so, "_imgItem",       imgItem);
            SetRef(so, "_imgRarity",     imgRarity);
            SetRef(so, "_focusHighlight", focus);
            SetRef(so, "_equippedBadge",     badge);
            SetRef(so, "_equippedPortrait",  badgePortrait);
            SetRef(so, "_equippedBadgeText", badgeText);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, SlotPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UI_InventorySlot>();
        }

        // 파티원 선택 버튼 프리팹 (초상 + 이름 + 선택 하이라이트).
        private static UIPartyEquipSelectorEntry BuildPartyEntryPrefab()
        {
            var go = NewUI("UIPartyEquipSelectorEntry", null);
            Center(Rt(go), 84, 100);
            var entry = go.AddComponent<UIPartyEquipSelectorEntry>();
            AddImage(go, SlotBg, UISprite, sliced: true);

            var hl = NewUI("SelectedHighlight", go.transform);
            Center(Rt(hl), 88, 104);
            var hlImg = AddImage(hl, new Color(0.35f, 0.80f, 0.90f, 1f), UISprite, sliced: true);
            hlImg.raycastTarget = false;
            hl.SetActive(false);

            var portraitGo = NewUI("Portrait", go.transform);
            var prt = Rt(portraitGo);
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 1f);
            prt.sizeDelta = new Vector2(72, 72);
            prt.anchoredPosition = new Vector2(0, -6);
            var portrait = AddImage(portraitGo, Color.white);
            portrait.raycastTarget = false;

            var nameGo = NewUI("Name", go.transform);
            var nrt = Rt(nameGo);
            nrt.anchorMin = new Vector2(0, 0);
            nrt.anchorMax = new Vector2(1, 0);
            nrt.pivot = new Vector2(0.5f, 0);
            nrt.sizeDelta = new Vector2(0, 22);
            nrt.anchoredPosition = new Vector2(0, 4);
            var nameText = AddText(nameGo, "이름", 14, TextMain, TextAlignmentOptions.Center);
            nameText.raycastTarget = false;

            var so = new SerializedObject(entry);
            SetRef(so, "_portrait",          portrait);
            SetRef(so, "_nameText",          nameText);
            SetRef(so, "_selectedHighlight", hl);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, PartyEntryPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UIPartyEquipSelectorEntry>();
        }

        // 장비 슬롯 한 개(그리드에 인라인 배치). 아이콘 + 빈 오버레이 + 하단 라벨.
        private static UIEquipmentSlot BuildEquipmentSlot(Transform parent, EquipPosition slot, string label)
        {
            var go = NewUI($"EquipSlot_{slot}", parent);
            var comp = go.AddComponent<UIEquipmentSlot>();
            AddImage(go, SlotBg, UISprite, sliced: true);

            var empty = NewUI("Empty", go.transform);
            Stretch(empty);
            AddImage(empty, new Color(0.10f, 0.12f, 0.15f, 1f), UISprite, sliced: true);

            var iconGo = NewUI("Icon", go.transform);
            InsetStretch(Rt(iconGo), 5);
            var icon = AddImage(iconGo, Color.white);
            icon.raycastTarget = false;
            icon.enabled = false;

            var lblGo = NewUI("Label", go.transform);
            var lrt = Rt(lblGo);
            lrt.anchorMin = new Vector2(0, 0);
            lrt.anchorMax = new Vector2(1, 0);
            lrt.pivot = new Vector2(0.5f, 0);
            lrt.sizeDelta = new Vector2(0, 16);
            lrt.anchoredPosition = Vector2.zero;
            var lblText = AddText(lblGo, label, 12, TextSub, TextAlignmentOptions.Center);
            lblText.raycastTarget = false;

            var so = new SerializedObject(comp);
            SetRef(so, "_icon",         icon);
            SetRef(so, "_emptyOverlay", empty);
            SetRef(so, "_slotLabel",    lblText);
            var pSlot = so.FindProperty("_slot");
            if (pSlot != null) pSlot.enumValueIndex = (int)slot; // EquipPosition은 0부터 순차라 index==값
            so.ApplyModifiedPropertiesWithoutUndo();
            return comp;
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

        private static void StretchInset(GameObject go, float left, float top, float right, float bottom)
        {
            var rt = Rt(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
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

        private static UITabButton MakeTab(string name, Transform parent, string label)
        {
            var btn = MakeButton(name, parent, label, out var lbl, BtnBg);
            lbl.alignment = TextAlignmentOptions.Left;
            lbl.margin = new Vector4(12, 0, 0, 0);
            SetHeight(btn.gameObject, 32);

            // 선택 시 배경=AccentBtn/라벨=TextMain, 비선택 시 배경=BtnBg/라벨=TextSub
            var tab = btn.gameObject.AddComponent<UITabButton>();
            tab.Configure(
                btn,
                btn.targetGraphic as Image,
                lbl,
                normalBg:     BtnBg,
                selectedBg:   AccentBtn,
                normalText:   TextSub,
                selectedText: TextMain);
            return tab;
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

        private static TextMeshProUGUI BuildStatOptionRow(Transform parent, string name)
        {
            var row = NewUI(name + "Row", parent);
            SetHeight(row.gameObject, 30);
            AddHLG(row, spacing: 0, pad: 0);
            var val = AddText(NewUI("Value", row.transform), string.Empty, 18, TextMain, TextAlignmentOptions.Left);
            AddFlexibleW(val.gameObject, 1f);
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

        private static void SetWidth(GameObject go, float minWidth, float preferredWidth)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minWidth = minWidth;
            le.preferredWidth = preferredWidth;
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
            grid.cellSize = new Vector2(SlotCellSize, SlotCellSize);
            grid.spacing = new Vector2(5, 5);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumnCount;

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

        private static void SetRefArray(SerializedObject so, string propName, UnityEngine.Object[] values)
        {
            var p = so.FindProperty(propName);
            if (p == null)
            {
                Debug.LogWarning($"[InvBuilder] 직렬화 프로퍼티를 찾을 수 없음: {propName}");
                return;
            }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        #endregion
    }
}
