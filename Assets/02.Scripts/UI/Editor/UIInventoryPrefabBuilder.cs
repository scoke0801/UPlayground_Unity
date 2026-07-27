using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.EnumType;
using UPlayGround.InputDefine;
using UPlayGround.UI.InputPrompt;

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
        private const string GlyphDataPath = "Assets/10.Datas/UI/Input/InputGlyphData.asset";

        private const int GridColumnCount = 10;
        private const int InitialRowCount = 12;
        private const float SlotCellSize = 118f;
        private const float SlotOverlaySize = 88f;

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
                Stretch(window);
                AddImage(window, WindowBg, UISprite, sliced: true);
                AddVLG(window, spacing: 8, pad: 16).childForceExpandHeight = false;

                // ── 헤더 ──
                var header = NewUI("Header", window.transform);
                SetHeight(header, 44);
                AddHLG(header, spacing: 12, pad: 0);
                var title = AddText(NewUI("Title", header.transform), "인벤토리", 25, TextMain, TextAlignmentOptions.Left);
                SetWidth(title.gameObject, 260);
                UPlayGround.UI.EditorTools.UIInputPromptBarBuilderUtility
                    .AddMainAndSubNavigationBar(header.transform, "이전 분류", "다음 분류");
                var headerSpacer = NewUI("Spacer", header.transform);
                AddFlexibleW(headerSpacer, 1f);

                var goldBox = NewUI("GoldBox", header.transform);
                SetWidth(goldBox, 126);
                AddHLG(goldBox, spacing: 8, pad: 4);
                var coin = NewUI("Coin", goldBox.transform);
                SetWidth(coin, 28);
                AddImage(coin, Gold, UISprite, sliced: true);
                var txtGold = AddText(NewUI("Gold", goldBox.transform), "0", 20, TextMain, TextAlignmentOptions.Left);
                AddFlexibleW(txtGold.gameObject, 1f);

                var weightBox = NewUI("WeightBox", header.transform);
                SetWidth(weightBox, 188);
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
                var txtWeight = AddText(wbText, "0.0 / 500.0 kg", 16, TextMain, TextAlignmentOptions.Center);
                txtWeight.raycastTarget = false;

                var playTime = AddText(NewUI("PlayTime", header.transform), "플레이 시간  --:--:--", 15, TextSub, TextAlignmentOptions.Center);
                SetWidth(playTime.gameObject, 205);
                var btnClose = MakeButton("BtnClose", header.transform, "X", out _);
                SetWidth(btnClose.gameObject, 40);

                // ── 장비 대상 카드 + 선택 캐릭터 요약 ──
                var partyBar = NewUI("PartyEquipBar", window.transform);
                SetHeight(partyBar, 190);
                AddHLG(partyBar, spacing: 8, pad: 0).childForceExpandHeight = true;

                // 파티원 선택 영역 (런타임 Roster 카드)
                var partyBox = NewUI("PartyBox", partyBar.transform);
                AddImage(partyBox, new Color(0.05f, 0.07f, 0.09f, 0.9f), UISprite, sliced: true);
                AddFlexibleW(partyBox, 1f);
                AddVLG(partyBox, spacing: 6, pad: 10).childForceExpandHeight = false;
                var partyTitle = AddText(NewUI("PartyTitle", partyBox.transform), "장비 대상", 16, TextMain, TextAlignmentOptions.Left);
                SetHeight(partyTitle.gameObject, 20);
                var partySelector = NewUI("PartySelector", partyBox.transform);
                AddFlexible(partySelector, 1);
                var pGrid = partySelector.AddComponent<GridLayoutGroup>();
                pGrid.cellSize = new Vector2(190, 142);
                pGrid.spacing = new Vector2(8, 0);
                pGrid.padding = new RectOffset(0, 0, 0, 0);
                pGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
                pGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                pGrid.constraintCount = 1;

                var summary = NewUI("CharacterSummary", partyBar.transform);
                AddFlexibleW(summary, 0.6f);
                AddImage(summary, new Color(0.05f, 0.07f, 0.09f, 0.94f), UISprite, sliced: true);
                var portraitGo = NewUI("Portrait", summary.transform);
                var portraitRt = Rt(portraitGo);
                portraitRt.anchorMin = new Vector2(0.62f, 0f);
                portraitRt.anchorMax = new Vector2(1f, 1f);
                portraitRt.offsetMin = Vector2.zero;
                portraitRt.offsetMax = Vector2.zero;
                var selectedCharPortrait = AddImage(portraitGo, Color.white);
                selectedCharPortrait.preserveAspect = true;
                selectedCharPortrait.raycastTarget = false;

                var summaryInfo = NewUI("Info", summary.transform);
                StretchInset(summaryInfo, 16, 12, 210, 10);
                AddVLG(summaryInfo, spacing: 2, pad: 0).childForceExpandHeight = false;
                var summaryNameRow = NewUI("NameRow", summaryInfo.transform);
                SetHeight(summaryNameRow, 26);
                AddHLG(summaryNameRow, spacing: 8, pad: 0);
                var txtSelChar = AddText(NewUI("SelectedCharName", summaryNameRow.transform), "캐릭터", 23, TextMain, TextAlignmentOptions.Left);
                AddFlexibleW(txtSelChar.gameObject, 1f);
                var activeBadge = AddText(NewUI("ActiveBadge", summaryNameRow.transform), "출전 중", 13, Accent, TextAlignmentOptions.Center);
                SetWidth(activeBadge.gameObject, 66);
                var charLevel = AddText(NewUI("Level", summaryInfo.transform), "Lv.1", 16, Accent, TextAlignmentOptions.Left);
                SetHeight(charLevel.gameObject, 18);
                var expBar = NewUI("ExpBar", summaryInfo.transform);
                SetHeight(expBar, 8);
                AddImage(expBar, SlotBg, UISprite, sliced: true);
                var expFillGo = NewUI("Fill", expBar.transform);
                Stretch(expFillGo);
                var charExpFill = AddImage(expFillGo, AccentBtn, UISprite, sliced: true);
                charExpFill.type = Image.Type.Filled;
                charExpFill.fillMethod = Image.FillMethod.Horizontal;
                var expTextGo = NewUI("ExpText", summaryInfo.transform);
                SetHeight(expTextGo, 14);
                var charExpText = AddText(expTextGo, "0 / 0", 12, TextSub, TextAlignmentOptions.Right);
                var charHp = BuildSummaryStatRow(summaryInfo.transform, "HP", "0 / 0");
                var charAttack = BuildSummaryStatRow(summaryInfo.transform, "공격력", "0");
                var charDefense = BuildSummaryStatRow(summaryInfo.transform, "방어력", "0");
                var charCrit = BuildSummaryStatRow(summaryInfo.transform, "치명타", "0%");
                var charPower = BuildSummaryStatRow(summaryInfo.transform, "전투력", "0");

                // ── 본문(좌 탭 / 중앙 그리드 / 우 상세) ──
                var body = NewUI("Body", window.transform);
                AddFlexible(body, 1);
                AddHLG(body, spacing: 12, pad: 0).childForceExpandHeight = true;

                // ===== 좌측 카테고리 탭 =====
                var left = NewUI("CategoryPanel", body.transform);
                AddImage(left, PanelBg, UISprite, sliced: true);
                SetWidth(left, 190);
                AddVLG(left, spacing: 6, pad: 8).childForceExpandHeight = false;
                var tabAll   = MakeTab("TabAll",       left.transform, "전체");
                var tabCons  = MakeTab("TabConsumable", left.transform, "소비");
                var tabEquip = MakeTab("TabEquipment",  left.transform, "장비");
                var tabMat   = MakeTab("TabMaterial",   left.transform, "재료");
                var tabQuest = MakeTab("TabQuest",      left.transform, "퀘스트");
                var tabImp   = MakeTab("TabImportant",  left.transform, "중요 아이템");
                var categorySpacer = NewUI("Spacer", left.transform);
                AddFlexible(categorySpacer, 1f);
                var filterButton = MakeButton("BtnFilter", left.transform, "필터 / 정렬", out var filterText, BtnBg);
                filterText.fontSize = 15;
                SetHeight(filterButton.gameObject, 46);

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
                var btnSort = MakeCommonButton("BtnSort", gridHeader.transform, "정렬 : 최근 획득순", BtnBg, out var sortModeText);
                sortModeText.fontSize = 15;
                SetWidth(btnSort.gameObject, 184);

                var gridScroll = NewUI("GridScroll", center.transform);
                AddFlexible(gridScroll, 1);
                var gridContent = BuildGridScroll(gridScroll);

                // ===== 우측 장착 장비 + 아이템 상세 =====
                var right = NewUI("RightPanel", body.transform);
                SetWidth(right, 545);
                AddVLG(right, spacing: 8, pad: 0).childForceExpandHeight = false;

                var equipmentPanel = NewUI("EquipmentPanel", right.transform);
                SetHeight(equipmentPanel, 124);
                AddImage(equipmentPanel, PanelBg, UISprite, sliced: true);
                AddVLG(equipmentPanel, spacing: 5, pad: 8).childForceExpandHeight = false;
                var equipmentTitle = AddText(NewUI("Title", equipmentPanel.transform), "장착 장비", 17, TextMain, TextAlignmentOptions.Left);
                SetHeight(equipmentTitle.gameObject, 21);
                var equipGrid = NewUI("EquipmentSlots", equipmentPanel.transform);
                AddFlexible(equipGrid, 1);
                var eGrid = equipGrid.AddComponent<GridLayoutGroup>();
                eGrid.cellSize = new Vector2(68, 82);
                eGrid.spacing = new Vector2(7, 0);
                eGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
                eGrid.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                eGrid.constraintCount = 1;

                var slotDefs = new (EquipPosition pos, string label)[]
                {
                    (EquipPosition.RightHand, "무기"), (EquipPosition.LeftHand, "보조"),
                    (EquipPosition.Head, "머리"),      (EquipPosition.Chest, "상의"),
                    (EquipPosition.Pants, "하의"),     (EquipPosition.Gloves, "장갑"),
                    (EquipPosition.Shoes, "신발")
                };
                var equipSlots = new UIEquipmentSlot[slotDefs.Length];
                for (int i = 0; i < slotDefs.Length; i++)
                    equipSlots[i] = BuildEquipmentSlot(equipGrid.transform, slotDefs[i].pos, slotDefs[i].label);

                var detail = NewUI("DetailPanel", right.transform);
                AddImage(detail, PanelBg, UISprite, sliced: true);
                AddFlexible(detail, 1);
                AddVLG(detail, spacing: 6, pad: 10).childForceExpandHeight = false;

                // 이름 + 등급
                var nameRow = NewUI("NameRow", detail.transform);
                SetHeight(nameRow, 30);
                AddHLG(nameRow, spacing: 8, pad: 0);
                var txtName = AddText(NewUI("Name", nameRow.transform), "아이템 이름", 21, TextMain, TextAlignmentOptions.Left);
                AddFlexibleW(txtName.gameObject, 1f);
                var txtRarity = AddText(NewUI("Rarity", nameRow.transform), "희귀", 15, Accent, TextAlignmentOptions.Right);
                SetWidth(txtRarity.gameObject, 72);

                var txtType = AddText(NewUI("Type", detail.transform), "장비", 15, TextSub, TextAlignmentOptions.Left);
                SetHeight(txtType.gameObject, 20);

                // 아이콘 + 보유수량/무게
                var iconRow = NewUI("IconRow", detail.transform);
                SetHeight(iconRow, 104);
                AddHLG(iconRow, spacing: 12, pad: 0);
                var iconGo = NewUI("Icon", iconRow.transform);
                SetWidth(iconGo, 104);
                AddImage(iconGo, SlotBg, UISprite, sliced: true);
                var itemIconGo = NewUI("ItemImage", iconGo.transform);
                InsetStretch(Rt(itemIconGo), 6);
                var imgIcon = AddImage(itemIconGo, Color.white);
                var infoCol = NewUI("Info", iconRow.transform);
                AddFlexibleW(infoCol, 1f);
                AddVLG(infoCol, spacing: 6, pad: 8).childForceExpandHeight = false;
                var txtCountDetail = BuildInfoRow(infoCol.transform, "보유 수량", "0", out _);
                var txtWeightDetail = BuildInfoRow(infoCol.transform, "무게", "0.0", out _);

                var txtDesc = AddText(NewUI("Description", detail.transform), "아이템 설명", 15, TextSub, TextAlignmentOptions.TopLeft);
                SetHeight(txtDesc.gameObject, 58);

                // 장착 부위
                var equipRow = NewUI("EquipSlotRow", detail.transform);
                SetHeight(equipRow, 28);
                AddHLG(equipRow, spacing: 8, pad: 0);
                AddText(NewUI("Label", equipRow.transform), "장착 부위", 18, TextSub, TextAlignmentOptions.Left);
                var txtEquipSlot = AddText(NewUI("Value", equipRow.transform), "보조 무기", 18, TextMain, TextAlignmentOptions.Right);
                AddFlexibleW(txtEquipSlot.gameObject, 1f);

                // 능력치 패널
                var statPanel = NewUI("StatPanel", detail.transform);
                AddImage(statPanel, SlotBg, UISprite, sliced: true);
                SetHeight(statPanel, 150);
                AddVLG(statPanel, spacing: 4, pad: 10).childForceExpandHeight = false;
                AddText(NewUI("Title", statPanel.transform), "능력치", 20, TextMain, TextAlignmentOptions.Left);
                var txtAtk     = BuildStatOptionRow(statPanel.transform, "옵션 1");
                var txtCrit    = BuildStatOptionRow(statPanel.transform, "옵션 2");
                var txtCritDmg = BuildStatOptionRow(statPanel.transform, "옵션 3");
                var txtAtkSpd  = BuildStatOptionRow(statPanel.transform, "옵션 4");

                var comparisonPanel = NewUI("ComparisonPanel", detail.transform);
                SetHeight(comparisonPanel, 170);
                AddImage(comparisonPanel, new Color(0.07f, 0.11f, 0.14f, 1f), UISprite, sliced: true);
                AddVLG(comparisonPanel, spacing: 5, pad: 8).childForceExpandHeight = false;
                var comparisonTitle = AddText(NewUI("Title", comparisonPanel.transform), "장비 능력치 비교", 15, Accent, TextAlignmentOptions.Left);
                SetHeight(comparisonTitle.gameObject, 20);
                var comparisonName = AddText(NewUI("ItemName", comparisonPanel.transform), "현재 장비 → 선택 장비", 15, TextMain, TextAlignmentOptions.Left);
                SetHeight(comparisonName.gameObject, 24);
                var comparisonStats = AddText(NewUI("Stats", comparisonPanel.transform),
                    "공격력  0 → 0  —", 14, TextMain, TextAlignmentOptions.TopLeft);
                comparisonStats.richText = true;
                comparisonStats.textWrappingMode = TextWrappingModes.NoWrap;
                AddFlexible(comparisonStats.gameObject, 1f);
                comparisonPanel.SetActive(false);
                var detailSpacer = NewUI("Spacer", detail.transform);
                AddFlexible(detailSpacer, 1f);

                // ── 하단 바 ──
                var bottom = NewUI("BottomBar", window.transform);
                SetHeight(bottom, 48);
                AddImage(bottom, new Color(0.05f, 0.07f, 0.09f, 0.96f), UISprite, sliced: true);
                AddHLG(bottom, spacing: 10, pad: 4);

                // 선택한 소모품을 등록할 수 있을 때만 런타임에서 노출한다.
                var quickSlotRegistration = NewUI("QuickSlotRegistration", bottom.transform);
                SetWidth(quickSlotRegistration, 292);
                AddHLG(quickSlotRegistration, spacing: 8, pad: 0);
                var quickLabel = AddText(NewUI("QuickSlotLabel", quickSlotRegistration.transform), "퀵슬롯 등록", 14,
                    TextSub, TextAlignmentOptions.Center);
                SetWidth(quickLabel.gameObject, 92);
                var glyphData = AssetDatabase.LoadAssetAtPath<InputGlyphDataSO>(GlyphDataPath);
                var quickSlotActions = new[]
                {
                    PlayerAction.QuickSlot_Up,
                    PlayerAction.QuickSlot_Right,
                    PlayerAction.QuickSlot_Down,
                    PlayerAction.QuickSlot_Left,
                };
                var quickButtons = new UICommonButton[UIQuickSlotAssignments.SlotCount];
                for (int i = 0; i < quickButtons.Length; i++)
                {
                    quickButtons[i] = MakeCommonButton(
                        $"BtnQuickSlot{i + 1}", quickSlotRegistration.transform, quickSlotActions[i], BtnBg,
                        out var quickSlotFallback);
                    ConfigureInputPrompt(
                        quickButtons[i].gameObject,
                        quickSlotFallback,
                        quickSlotActions[i],
                        glyphData);
                    SetWidth(quickButtons[i].gameObject, 42);
                }
                quickSlotRegistration.SetActive(false);

                var bottomSpacer = NewUI("Spacer", bottom.transform);
                AddFlexibleW(bottomSpacer, 1f);
                var btnUse   = MakeCommonButton("BtnUse",   bottom.transform, "사용",   BtnBg,     out _);
                SetWidth(btnUse.gameObject, 104);
                var btnEquip = MakeCommonButton("BtnEquip", bottom.transform, "장착",   AccentBtn, out _);
                SetWidth(btnEquip.gameObject, 104);
                var btnDrop  = MakeCommonButton("BtnDrop",  bottom.transform, "버리기", DangerBtn, out _);
                SetWidth(btnDrop.gameObject, 104);

                // 호버 하이라이트 (코드에서 슬롯으로 재부모됨)
                var clickTap = NewUI("ItemClickTap", window.transform);
                Center(Rt(clickTap), SlotOverlaySize, SlotOverlaySize);
                var tapImg = AddImage(clickTap, new Color(0.35f, 0.80f, 0.90f, 0.25f), UISprite, sliced: true);
                tapImg.raycastTarget = false;
                clickTap.SetActive(false);

                // ── 필드 연결 ──
                var so = new SerializedObject(inv);
                SetRef(so, "_sceneContent",    window.GetComponent<RectTransform>()); // Scene 열기/닫기 슬라이드 대상
                SetRef(so, "_itemPanelPrefab", slot);
                SetRef(so, "_content",         gridContent.transform);
                SetRef(so, "_itemGrid",        gridContent.GetComponent<GridLayoutGroup>());
                SetRef(so, "_itemScrollRect",   gridContent.GetComponentInParent<ScrollRect>());
                SetRef(so, "_imgWeightFill",   imgWeightFill);
                SetRef(so, "_txtWeight",       txtWeight);
                SetRef(so, "_itemClickTap",    clickTap);

                SetRef(so, "_tabGroup",      tabGroup);

                SetRef(so, "_txtItemCount", txtCount);
                SetRef(so, "_txtGold",      txtGold);
                SetRef(so, "_sortButton",   btnSort);
                SetRef(so, "_sortModeText", sortModeText);
                SetRef(so, "_txtPlayTime",  playTime);

                SetRef(so, "_selectedItemPrefab",    detail);
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
                SetRef(so, "_comparisonPanel",       comparisonPanel);
                SetRef(so, "_comparisonItemNameText", comparisonName);
                SetRef(so, "_comparisonStatsText",   comparisonStats);

                SetRef(so, "_useButton",   btnUse);
                SetRef(so, "_equipButton", btnEquip);
                SetRef(so, "_dropButton",  btnDrop);
                SetRefArray(so, "_quickSlotButtons", quickButtons);
                SetRef(so, "_quickSlotRegistrationRoot", quickSlotRegistration);
                SetRef(so, "_btnClose",    btnClose);

                // 파티 장비 패널
                SetRef(so, "_partySelectorContainer",   partySelector.transform);
                SetRef(so, "_partyEntryPrefab",         partyEntry);
                SetRef(so, "_selectedCharacterNameText", txtSelChar);
                SetRefArray(so, "_equipmentSlots", equipSlots);
                SetRef(so, "_selectedCharacterPortrait",        selectedCharPortrait);
                SetRef(so, "_selectedCharacterLevelText",       charLevel);
                SetRef(so, "_selectedCharacterExpFill",         charExpFill);
                SetRef(so, "_selectedCharacterExpText",         charExpText);
                SetRef(so, "_selectedCharacterCombatPowerText", charPower);
                SetRef(so, "_selectedCharacterHpText",          charHp);
                SetRef(so, "_selectedCharacterAttackText",      charAttack);
                SetRef(so, "_selectedCharacterDefenseText",     charDefense);
                SetRef(so, "_selectedCharacterCritText",        charCrit);

                // 기본 풀은 10열 x 12행 = 120슬롯. 런타임 표시 열 수는 화면 폭에 따라 8~12열로 조정된다.
                // 표시 용량은 IUIInventoryService.MaxSlots(120)를 따른다.
                var pRow = so.FindProperty("_slotCountPerRow");
                if (pRow != null) pRow.intValue = GridColumnCount;
                var pStartRows = so.FindProperty("_startRowCount");
                if (pStartRows != null) pStartRows.intValue = InitialRowCount;

                // _sceneContent는 UI_Scene의 열기/닫기 트윈 대상이다.
                // 연결 실패 상태로 프리팹을 저장하면 화면 전환이 깨지므로 저장 자체를 중단한다.
                SerializedProperty sceneContentProperty = so.FindProperty("_sceneContent");
                if (sceneContentProperty?.objectReferenceValue == null)
                    throw new System.InvalidOperationException(
                        "[InvBuilder] _sceneContent 연결에 실패하여 프리팹 저장을 중단합니다.");

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
            Stretch(focus);
            Rt(focus).offsetMin = new Vector2(-3f, -3f);
            Rt(focus).offsetMax = new Vector2(3f, 3f);
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

            var cooldown = NewUI("Cooldown", content.transform);
            Stretch(cooldown);
            var cooldownFill = AddImage(cooldown, new Color(0f, 0f, 0f, 0.72f), UISprite);
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Radial360;
            cooldownFill.fillOrigin = 2;
            cooldownFill.fillClockwise = false;
            cooldownFill.raycastTarget = false;
            var cooldownText = AddText(
                NewUI("CooldownText", cooldown.transform),
                string.Empty,
                15,
                TextMain,
                TextAlignmentOptions.Center);
            ConfigureSlotOverlayText(cooldownText);
            Stretch(cooldownText.gameObject);
            cooldownText.raycastTarget = false;
            cooldown.SetActive(false);

            var enhance = NewUI("Enhance", content.transform);
            AnchorBottomLeft(Rt(enhance), 38, 22);
            AddSlotTextBackdrop(enhance);
            var enhanceText = NewUI("Text", enhance.transform);
            InsetStretch(Rt(enhanceText), 3f);
            var txtEnhance = AddText(enhanceText, "+5", 15, Accent, TextAlignmentOptions.Center);
            ConfigureSlotOverlayText(txtEnhance, Gold);
            txtEnhance.raycastTarget = false;

            var count = NewUI("Count", content.transform);
            AnchorBottomRight(Rt(count), 38, 22);
            AddSlotTextBackdrop(count);
            var countText = NewUI("Text", count.transform);
            InsetStretch(Rt(countText), 3f);
            var txtCount = AddText(countText, "1", 15, TextMain, TextAlignmentOptions.Center);
            ConfigureSlotOverlayText(txtCount);
            txtCount.raycastTarget = false;

            var weight = NewUI("Weight", content.transform);
            AnchorTopLeft(Rt(weight), 42, 22);
            AddSlotTextBackdrop(weight);
            var weightText = NewUI("Text", weight.transform);
            InsetStretch(Rt(weightText), 3f);
            var txtWeight = AddText(weightText, "0.0", 12, TextSub, TextAlignmentOptions.Center);
            ConfigureSlotOverlayText(txtWeight);
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
            SetRef(so, "_enhanceRoot",   enhance);
            SetRef(so, "_txtEnhance",    txtEnhance);
            SetRef(so, "_imgItem",       imgItem);
            SetRef(so, "_imgRarity",     imgRarity);
            SetRef(so, "_focusHighlight", focus);
            SetRef(so, "_equippedBadge",     badge);
            SetRef(so, "_equippedPortrait",  badgePortrait);
            SetRef(so, "_equippedBadgeText", badgeText);
            SetRef(so, "_cooldownRoot", cooldown);
            SetRef(so, "_cooldownFill", cooldownFill);
            SetRef(so, "_cooldownText", cooldownText);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, SlotPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UI_InventorySlot>();
        }

        // 파티원 선택 버튼 프리팹 (초상 + 이름 + 선택 하이라이트).
        private static UIPartyEquipSelectorEntry BuildPartyEntryPrefab()
        {
            var go = NewUI("UIPartyEquipSelectorEntry", null);
            Center(Rt(go), 190, 142);
            var entry = go.AddComponent<UIPartyEquipSelectorEntry>();
            AddImage(go, SlotBg, UISprite, sliced: true);

            var hl = NewUI("SelectedHighlight", go.transform);
            Center(Rt(hl), 194, 146);
            var hlImg = AddImage(hl, new Color(0.1f, 0.95f, 1f, 1f), UISprite, sliced: true);
            hlImg.raycastTarget = false;
            hl.SetActive(false);

            var portraitGo = NewUI("Portrait", go.transform);
            var prt = Rt(portraitGo);
            prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 1f);
            prt.sizeDelta = new Vector2(178, 108);
            prt.anchoredPosition = new Vector2(0, -5);
            var portrait = AddImage(portraitGo, Color.white);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;

            var nameBar = NewUI("NameBar", go.transform);
            var nameBarRt = Rt(nameBar);
            nameBarRt.anchorMin = new Vector2(0, 0);
            nameBarRt.anchorMax = new Vector2(1, 0);
            nameBarRt.pivot = new Vector2(0.5f, 0);
            nameBarRt.sizeDelta = new Vector2(0, 34);
            AddImage(nameBar, new Color(0f, 0f, 0f, 0.68f));

            var nameGo = NewUI("Name", nameBar.transform);
            var nrt = Rt(nameGo);
            nrt.anchorMin = new Vector2(0, 0);
            nrt.anchorMax = new Vector2(1, 0);
            nrt.pivot = new Vector2(0.5f, 0);
            nrt.sizeDelta = new Vector2(0, 18);
            nrt.anchoredPosition = new Vector2(0, 16);
            var nameText = AddText(nameGo, "이름", 15, TextMain, TextAlignmentOptions.Center);
            nameText.raycastTarget = false;

            var levelGo = NewUI("Level", nameBar.transform);
            var levelRt = Rt(levelGo);
            levelRt.anchorMin = new Vector2(0, 0);
            levelRt.anchorMax = new Vector2(1, 0);
            levelRt.pivot = new Vector2(0.5f, 0);
            levelRt.sizeDelta = new Vector2(-10, 15);
            levelRt.anchoredPosition = new Vector2(0, 2);
            var levelText = AddText(levelGo, "Lv.1", 12, TextSub, TextAlignmentOptions.Right);
            levelText.raycastTarget = false;

            var indexGo = NewUI("Index", go.transform);
            AnchorTopLeft(Rt(indexGo), 28, 24);
            AddImage(indexGo, new Color(0.16f, 0.2f, 0.25f, 0.94f), UISprite, sliced: true).raycastTarget = false;
            var indexLabelGo = NewUI("Label", indexGo.transform);
            Stretch(indexLabelGo);
            var indexText = AddText(indexLabelGo, "1", 15, TextMain, TextAlignmentOptions.Center);
            indexText.raycastTarget = false;

            var activeBadge = NewUI("ActiveBadge", go.transform);
            AnchorTopRight(Rt(activeBadge), 62, 24);
            AddImage(activeBadge, Gold, UISprite, sliced: true).raycastTarget = false;
            AddText(NewUI("Mark", activeBadge.transform), "출전 중", 11, new Color(0.15f, 0.12f, 0.04f, 1f), TextAlignmentOptions.Center);
            Stretch(activeBadge.transform.GetChild(0).gameObject);

            var lockedOverlay = NewUI("LockedOverlay", go.transform);
            Stretch(lockedOverlay);
            AddImage(lockedOverlay, new Color(0.02f, 0.03f, 0.04f, 0.88f), UISprite, sliced: true).raycastTarget = false;
            var lockedLabelGo = NewUI("Label", lockedOverlay.transform);
            Stretch(lockedLabelGo);
            var lockedLabel = AddText(lockedLabelGo, "잠금 해제\n파티 편성에서 설정", 14, TextSub, TextAlignmentOptions.Center);
            lockedLabel.raycastTarget = false;
            lockedOverlay.SetActive(false);

            var so = new SerializedObject(entry);
            SetRef(so, "_portrait",          portrait);
            SetRef(so, "_nameText",          nameText);
            SetRef(so, "_indexText",         indexText);
            SetRef(so, "_levelText",         levelText);
            SetRef(so, "_activeBadge",       activeBadge);
            SetRef(so, "_lockedOverlay",     lockedOverlay);
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

        private static void ConfigureSlotOverlayText(TextMeshProUGUI text, Color? color = null)
        {
            if (text == null)
                return;

            text.color = color ?? Color.white;
            text.fontStyle = FontStyles.Bold;
            text.outlineColor = new Color(0f, 0f, 0f, 0.96f);
            text.outlineWidth = 0.28f;
        }

        private static void AddSlotTextBackdrop(GameObject target)
        {
            var backdrop = AddImage(
                target,
                new Color(0.015f, 0.025f, 0.04f, 0.84f),
                UISprite,
                sliced: true);
            backdrop.raycastTarget = false;
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
            SetHeight(btn.gameObject, 50);

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

        /// <summary>
        /// 버튼에 실제 InputAction 바인딩을 표시한다.
        /// 활성 입력 장치와 리바인딩이 바뀌면 UI_InputPromptIcon이 글리프/폴백 문자를 갱신한다.
        /// </summary>
        private static void ConfigureInputPrompt(
            GameObject button,
            TextMeshProUGUI fallback,
            string actionName,
            InputGlyphDataSO glyphData)
        {
            fallback.enableAutoSizing = true;
            fallback.fontSizeMin = 7f;
            fallback.fontSizeMax = 15f;
            fallback.raycastTarget = false;

            var glyphGo = NewUI("Glyph", button.transform);
            InsetStretch(Rt(glyphGo), 7f);
            var glyph = AddImage(glyphGo, Color.white);
            glyph.preserveAspect = true;
            glyph.raycastTarget = false;

            var prompt = button.AddComponent<UI_InputPromptIcon>();
            var so = new SerializedObject(prompt);
            SetString(so, "_mapName", InputMapNames.PlayerAction);
            SetString(so, "_actionName", actionName);
            SetRef(so, "_glyphData", glyphData);
            SetRef(so, "_iconImage", glyph);
            SetRef(so, "_fallbackLabel", fallback);
            so.ApplyModifiedPropertiesWithoutUndo();
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

        private static TextMeshProUGUI BuildSummaryStatRow(Transform parent, string label, string value)
        {
            var row = NewUI(label + "Row", parent);
            SetHeight(row, 16);
            AddHLG(row, spacing: 6, pad: 0);
            var labelText = AddText(NewUI("Label", row.transform), label, 12, TextSub, TextAlignmentOptions.Left);
            AddFlexibleW(labelText.gameObject, 1f);
            var valueText = AddText(NewUI("Value", row.transform), value, 12, TextMain, TextAlignmentOptions.Right);
            SetWidth(valueText.gameObject, 110);
            return valueText;
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

        private static void SetString(SerializedObject so, string propName, string value)
        {
            var p = so.FindProperty(propName);
            if (p == null)
            {
                Debug.LogWarning($"[InvBuilder] 직렬화 프로퍼티를 찾을 수 없음: {propName}");
                return;
            }
            p.stringValue = value;
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
