using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Party.EditorTools
{
    /// <summary>
    /// 동료(파티) UI(UI_PartyMenu) 프리팹 초안을 코드로 생성하고 SerializeField를 자동 연결하는 에디터 툴.
    ///
    /// - 기존 UI_PartyMenu.prefab / UIPartyMenuEntry.prefab / UIPartyBattleEntry.prefab을 덮어쓴다(자식 재구성).
    /// - 3열(보유 동료 / 출전 파티 / 상세) + 하단 바 구성.
    /// - 배틀 슬롯 4개는 인라인 인스턴스.
    /// - 상세 패널에 캐릭터 속성·패시브 4종을 생성하고 런타임 필드에 연결한다.
    /// - 재실행 가능(idempotent).
    /// </summary>
    public static class UIPartyMenuPrefabBuilder
    {
        private const string MainPrefabPath  = "Assets/03.Prefabs/UI/Scene/Party/UI_PartyMenu.prefab";
        private const string EntryPrefabPath = "Assets/03.Prefabs/UI/Scene/Party/UIPartyMenuEntry.prefab";
        private const string BattleEntryPrefabPath = "Assets/03.Prefabs/UI/Scene/Party/UIPartyBattleEntry.prefab";
        private const string AssistEntryPrefabPath = "Assets/03.Prefabs/UI/Scene/Party/UIAssistRosterEntry.prefab";

        private static readonly Color Dim       = new Color(0f, 0f, 0f, 0.52f);
        private static readonly Color WindowBg  = new Color(0.07f, 0.09f, 0.13f, 0.98f);
        private static readonly Color PanelBg   = new Color(0.10f, 0.13f, 0.18f, 1f);
        private static readonly Color SlotBg    = new Color(0.15f, 0.19f, 0.25f, 1f);
        private static readonly Color FieldBg   = new Color(0.08f, 0.10f, 0.14f, 1f);
        private static readonly Color BtnBg     = new Color(0.20f, 0.27f, 0.34f, 1f);
        private static readonly Color DangerBg  = new Color(0.35f, 0.12f, 0.14f, 1f);
        private static readonly Color TextMain  = new Color(0.90f, 0.92f, 0.95f, 1f);
        private static readonly Color TextSub   = new Color(0.62f, 0.68f, 0.74f, 1f);
        private static readonly Color Gold      = new Color(0.95f, 0.78f, 0.35f, 1f);
        private static readonly Color Accent    = new Color(0.35f, 0.80f, 0.90f, 1f);
        private static readonly Color HpGreen   = new Color(0.35f, 0.85f, 0.45f, 1f);
        private static readonly Color Danger    = new Color(0.90f, 0.35f, 0.35f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static void Build()
        {
            if (!System.IO.File.Exists(MainPrefabPath))
            {
                EditorUtility.DisplayDialog("동료 UI 빌더",
                    $"대상 프리팹을 찾을 수 없습니다:\n{MainPrefabPath}", "확인");
                return;
            }

            var entryPrefab = BuildMenuEntryPrefab();
            BuildBattleEntryPrefab();
            var assistEntryPrefab = BuildAssistEntryPrefab();

            var root = PrefabUtility.LoadPrefabContents(MainPrefabPath);
            try
            {
                var menu = root.GetComponent<UI_PartyMenu>();
                if (menu == null)
                {
                    Debug.LogError("[PartyBuilder] 루트에 UI_PartyMenu 컴포넌트가 없습니다. 중단.");
                    return;
                }

                ClearChildren(root.transform);

                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                AddImage(dim, Dim);

                var window = NewUI("Window", root.transform);
                Center(Rt(window), 1760, 980);
                AddImage(window, WindowBg, UISprite, sliced: true);

                var contentRoot = NewUI("Content", window.transform);
                StretchInset(contentRoot, 12, 12, 12, 12);
                AddVLG(contentRoot, spacing: 10, pad: 12).childForceExpandHeight = false;

                // 헤더
                var header = NewUI("Header", contentRoot.transform);
                SetHeight(header, 46);
                var title = NewUI("Title", header.transform);
                Stretch(title);
                AddText(title, "동료", 30, TextMain, TextAlignmentOptions.Center);
                var btnClose = MakeButton("BtnClose", header.transform, "X", out _);
                AnchorTopRight(Rt(btnClose.gameObject), 42, 42);

                // 본문
                var body = NewUI("Body", contentRoot.transform);
                AddFlexible(body, 1);
                var bodyLayout = AddHLG(body, spacing: 14, pad: 0);
                bodyLayout.childForceExpandHeight = true;
                bodyLayout.childAlignment = TextAnchor.UpperLeft;

                // ===== 좌: 보유 동료 =====
                var left = NewUI("RosterPanel", body.transform);
                AddImage(left, PanelBg, UISprite, sliced: true);
                SetWidth(left, 440);
                AddVLG(left, spacing: 8, pad: 10).childForceExpandHeight = false;
                var rosterHeader = NewUI("RosterHeader", left.transform);
                SetHeight(rosterHeader, 32);
                AddHLG(rosterHeader, spacing: 8, pad: 0);
                AddText(NewUI("Label", rosterHeader.transform), "보유 동료", 22, TextMain, TextAlignmentOptions.Left);
                var rosterCount = AddText(NewUI("Count", rosterHeader.transform), "0", 20, TextSub, TextAlignmentOptions.Right);
                AddFlexibleW(rosterCount.gameObject, 1f);
                var rosterScroll = NewUI("RosterScroll", left.transform);
                AddFlexible(rosterScroll, 1);
                var rosterContent = BuildVerticalScroll(rosterScroll);

                // ===== 좌 하단: 어시스트 (사이클 보스 영입 동료) =====
                var assistPanelComp = BuildAssistSection(left.transform, assistEntryPrefab);

                // ===== 중: 출전 파티 =====
                var center = NewUI("BattlePanel", body.transform);
                AddImage(center, PanelBg, UISprite, sliced: true);
                AddFlexibleW(center, 1f);
                AddVLG(center, spacing: 10, pad: 16).childForceExpandHeight = false;
                var battleHeader = NewUI("BattleHeader", center.transform);
                SetHeight(battleHeader, 38);
                AddHLG(battleHeader, spacing: 8, pad: 0);
                AddText(NewUI("Label", battleHeader.transform), "출전 파티", 22, TextMain, TextAlignmentOptions.Left);
                var battleCount = AddText(NewUI("Count", battleHeader.transform), "0 / 4", 20, TextSub, TextAlignmentOptions.Right);
                AddFlexibleW(battleCount.gameObject, 1f);

                // 카드 영역을 상/하 스페이서로 감싸 세로 중앙에 배치(해상도 독립적).
                var battleBlankTop = NewUI("BattleBlankTop", center.transform);
                AddFlexible(battleBlankTop, 1f);

                var cardsRow = NewUI("Cards", center.transform);
                SetHeight(cardsRow, 420);       // 최소 높이 보장
                AddFlexible(cardsRow, 1f);       // 남는 세로 공간을 카드가 함께 채움
                var cardsLayout = AddHLG(cardsRow, spacing: 14, pad: 0);
                cardsLayout.childForceExpandWidth = true;
                cardsLayout.childAlignment = TextAnchor.UpperLeft;
                var battleEntries = new UIPartyBattleEntry[4];
                for (int i = 0; i < 4; i++)
                    battleEntries[i] = BuildBattleCard(cardsRow.transform, $"BattleCard{i + 1}", i, addFlexibleWidth: true);

                // 출전 파티 속성 구성 요약
                var elementSummary = AddText(NewUI("ElementSummary", center.transform),
                    "속성 없음", 18, TextSub, TextAlignmentOptions.Left);
                SetHeight(elementSummary.gameObject, 26);

                var battleBlank = NewUI("BattleBlank", center.transform);
                AddFlexible(battleBlank, 1f);

                // ===== 상세 =====
                var detail = BuildDetailPanel(body.transform, out var detailComp);

                // ── 하단 바 ──
                var bottom = NewUI("BottomBar", contentRoot.transform);
                SetHeight(bottom, 70);
                AddHLG(bottom, spacing: 12, pad: 4);
                var cpText = BuildStatBox(bottom.transform, "총 파티 전투력", "0", Gold, 240);
                var memText = BuildStatBox(bottom.transform, "출전 인원", "0 / 4", TextMain, 180);
                var spacer = NewUI("Spacer", bottom.transform);
                AddFlexibleW(spacer, 1f);
                var btnAuto  = MakeButton("AutoButton",   bottom.transform, "자동 편성", out _, BtnBg);    SetWidth(btnAuto.gameObject, 170);
                var btnSave  = MakeButton("SaveButton",   bottom.transform, "저장",     out _, Accent);   SetWidth(btnSave.gameObject, 150);
                var btnDisB  = MakeButton("DisbandBattle",bottom.transform, "출전 해제", out _, BtnBg);    SetWidth(btnDisB.gameObject, 170);
                var btnDisP  = MakeButton("DisbandParty", bottom.transform, "파티 해제", out _, DangerBg); SetWidth(btnDisP.gameObject, 170);

                // ── 필드 연결 ──
                var so = new SerializedObject(menu);
                SetRef(so, "_sceneContent",          window.GetComponent<RectTransform>()); // Scene 열기/닫기 슬라이드 대상
                SetRef(so, "_content",               rosterContent.transform);
                SetRef(so, "_partyMenuEntryPrefab",  entryPrefab);
                SetArray(so, "_partyBattleEntries",  battleEntries);
                SetRef(so, "_saveButton",            btnSave);
                SetRef(so, "_autoOrganizationButton",btnAuto);
                SetRef(so, "_disbandBattleButton",   btnDisB);
                SetRef(so, "_disbandPartyButton",    btnDisP);
                SetRef(so, "_closeButton",           btnClose);
                SetRef(so, "_partyCombatPowerText",  cpText);
                SetRef(so, "_rosterCountText",       rosterCount);
                SetRef(so, "_battlePartyCountText",  battleCount);
                SetRef(so, "_battleMemberCountText", memText);
                SetRef(so, "_partyWeightSummaryText", elementSummary);
                SetRef(so, "_detailPanel",           detailComp);
                SetRef(so, "_assistPanel",           assistPanelComp);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
                Debug.Log("[PartyBuilder] UI_PartyMenu 프리팹 초안 생성 완료.");
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
        #region 목록 엔트리 서브 프리팹

        private static UIPartyMenuEntry BuildMenuEntryPrefab()
        {
            var go = NewUI("UIPartyMenuEntry", null);
            SetHeight(go, 64);
            var img = AddImage(go, SlotBg, UISprite, sliced: true);
            var entry = go.AddComponent<UIPartyMenuEntry>();
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            AddHLG(go, spacing: 8, pad: 6);

            var iconGo = NewUI("Icon", go.transform);
            SetWidth(iconGo, 50);
            var icon = AddImage(iconGo, Color.white);
            icon.preserveAspect = true;

            var col = NewUI("Col", go.transform);
            AddFlexibleW(col, 1f);
            AddVLG(col, spacing: 2, pad: 2).childForceExpandHeight = false;
            var name = AddText(NewUI("Name", col.transform), "이름", 20, TextMain, TextAlignmentOptions.Left);
            var level = AddText(NewUI("Level", col.transform), "Lv. 1", 15, TextSub, TextAlignmentOptions.Left);

            var weaponGo = NewUI("Weapon", go.transform);
            SetWidth(weaponGo, 28);
            var weapon = AddImage(weaponGo, TextSub);

            var orderRoot = NewUI("OrderBadge", go.transform);
            SetWidth(orderRoot, 30);
            AddImage(orderRoot, Accent, UISprite, sliced: true);
            var orderText = AddText(NewUI("Text", orderRoot.transform), "1", 18, Color.black, TextAlignmentOptions.Center);
            Stretch(orderText.gameObject);
            orderRoot.SetActive(false);

            var selected = NewUI("Selected", go.transform);
            Stretch(selected);
            var selImg = AddImage(selected, new Color(Accent.r, Accent.g, Accent.b, 0.12f));
            selImg.raycastTarget = false;
            AddOutline(selected, new Color(Accent.r, Accent.g, Accent.b, 0.95f), new Vector2(3f, -3f));
            selected.AddComponent<LayoutElement>().ignoreLayout = true;
            selected.transform.SetAsFirstSibling();
            selected.SetActive(false);

            var dimmed = NewUI("Dimmed", go.transform);
            Stretch(dimmed);
            var dimImg = AddImage(dimmed, new Color(0f, 0f, 0f, 0.42f));
            dimImg.raycastTarget = false;
            dimmed.AddComponent<LayoutElement>().ignoreLayout = true;
            dimmed.SetActive(true);

            var so = new SerializedObject(entry);
            SetRef(so, "_characterIcon",     icon);
            SetRef(so, "_characterNameText", name);
            SetRef(so, "_characterLevelText",level);
            SetRef(so, "_dimmedImage",       dimmed);
            SetRef(so, "_button",            btn);
            SetRef(so, "_partyOrderRoot",    orderRoot);
            SetRef(so, "_partyOrderText",    orderText);
            SetRef(so, "_selectedImage",     selected);
            SetRef(so, "_weaponIcon",        weapon);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, EntryPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UIPartyMenuEntry>();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 배틀 카드 (인라인)

        private static UIPartyBattleEntry BuildBattleEntryPrefab()
        {
            var entry = BuildBattleCard(null, "UIPartyBattleEntry", 0, addFlexibleWidth: false);
            var asset = PrefabUtility.SaveAsPrefabAsset(entry.gameObject, BattleEntryPrefabPath);
            UnityEngine.Object.DestroyImmediate(entry.gameObject);
            return asset.GetComponent<UIPartyBattleEntry>();
        }

        private static UIPartyBattleEntry BuildBattleCard(Transform parent, string cardName, int index, bool addFlexibleWidth)
        {
            var card = NewUI(cardName, parent);
            SetWidth(card, 172);
            if (addFlexibleWidth)
                AddFlexibleW(card, 1f);
            var img = AddImage(card, SlotBg, UISprite, sliced: true);
            var entry = card.AddComponent<UIPartyBattleEntry>();
            var btn = card.AddComponent<Button>();
            btn.targetGraphic = img;

            // ── 편성된 캐릭터 내용 (미편성 슬롯에서는 숨김) ──
            var content = NewUI("Content", card.transform);
            Stretch(content);
            AddVLG(content, spacing: 6, pad: 10).childForceExpandHeight = false;

            var portrait = NewUI("Portrait", content.transform);
            AddFlexible(portrait, 1);
            AddImage(portrait, FieldBg, UISprite, sliced: true);
            var portraitImageGo = NewUI("Image", portrait.transform);
            StretchInset(portraitImageGo, 8, 6, 8, 6);
            var portImg = AddImage(portraitImageGo, Color.white);
            portImg.preserveAspect = true;

            var name = AddText(NewUI("Name", content.transform), "이름", 22, TextMain, TextAlignmentOptions.Center);
            SetHeight(name.gameObject, 30);
            var level = AddText(NewUI("Level", content.transform), "Lv. 1", 17, TextSub, TextAlignmentOptions.Center);
            SetHeight(level.gameObject, 22);

            var hpBar = NewUI("HpBar", content.transform);
            SetHeight(hpBar, 12);
            AddImage(hpBar, new Color(0.08f, 0.10f, 0.13f, 1f), UISprite, sliced: true);
            var hpFillGo = NewUI("Fill", hpBar.transform);
            Stretch(hpFillGo);
            var hpFill = AddImage(hpFillGo, HpGreen, UISprite, sliced: true);
            hpFill.type = Image.Type.Filled;
            hpFill.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillAmount = 1f;
            var hpText = AddText(NewUI("HpText", content.transform), "0 / 0", 16, TextMain, TextAlignmentOptions.Center);
            SetHeight(hpText.gameObject, 20);

            // 오버레이들 (content 하위 — 미편성 시 캐릭터 내용과 함께 숨김)
            var orderRoot = NewUI("OrderBadge", content.transform);
            AnchorTopLeft(Rt(orderRoot), 32, 32);
            AddImage(orderRoot, Accent, UISprite, sliced: true);
            orderRoot.AddComponent<LayoutElement>().ignoreLayout = true;
            var orderText = AddText(NewUI("Text", orderRoot.transform), "1", 18, Color.black, TextAlignmentOptions.Center);
            Stretch(orderText.gameObject);

            var weaponGo = NewUI("Weapon", content.transform);
            AnchorTopRight(Rt(weaponGo), 28, 28);
            var weapon = AddImage(weaponGo, TextSub);
            weaponGo.AddComponent<LayoutElement>().ignoreLayout = true;

            var dead = NewUI("DeadText", content.transform);
            AnchorCenter(Rt(dead), 140, 30);
            var deadTxt = AddText(dead, "전투 불능", 20, Danger, TextAlignmentOptions.Center);
            deadTxt.raycastTarget = false;
            dead.AddComponent<LayoutElement>().ignoreLayout = true;
            dead.SetActive(false);

            var selected = NewUI("Selected", content.transform);
            Stretch(selected);
            var selImg = AddImage(selected, new Color(Accent.r, Accent.g, Accent.b, 0.10f));
            selImg.raycastTarget = false;
            AddOutline(selected, new Color(Accent.r, Accent.g, Accent.b, 0.95f), new Vector2(4f, -4f));
            selected.AddComponent<LayoutElement>().ignoreLayout = true;
            selected.transform.SetAsFirstSibling();
            selected.SetActive(false);

            // ── 빈 슬롯 플레이스홀더 (미편성 슬롯에서만 표시) ──
            var empty = NewUI("Empty", card.transform);
            Stretch(empty);
            empty.AddComponent<LayoutElement>().ignoreLayout = true;
            var emptyBg = AddImage(empty, new Color(0f, 0f, 0f, 0.20f), UISprite, sliced: true);
            emptyBg.raycastTarget = false; // 클릭은 뒤쪽 카드 버튼으로 통과(선택 없음)
            AddOutline(empty, new Color(TextSub.r, TextSub.g, TextSub.b, 0.28f), new Vector2(2f, -2f));
            var emptyLabelGo = NewUI("Label", empty.transform);
            Stretch(emptyLabelGo);
            var emptyLabel = AddText(emptyLabelGo, "+\n빈 슬롯", 22,
                new Color(TextSub.r, TextSub.g, TextSub.b, 0.65f), TextAlignmentOptions.Center);
            emptyLabel.raycastTarget = false;
            empty.SetActive(false); // 기본은 비활성(런타임 Unbind에서 켬)

            var so = new SerializedObject(entry);
            SetRef(so, "_characterIcon",      portImg);
            SetRef(so, "_characterNameText",  name);
            SetRef(so, "_characterLevelText", level);
            SetRef(so, "_partyOrderRoot",     orderRoot);
            SetRef(so, "_partyOrderText",     orderText);
            SetRef(so, "_weaponIcon",         weapon);
            SetRef(so, "_selectedImage",      selected);
            SetRef(so, "_slotButton",         btn);
            SetRef(so, "_hpFill",             hpFill);
            SetRef(so, "_hpText",             hpText);
            SetRef(so, "_deadText",           dead);
            SetRef(so, "_contentRoot",        content);
            SetRef(so, "_emptyRoot",          empty);
            so.ApplyModifiedPropertiesWithoutUndo();

            return entry;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 어시스트 (사이클 보스 영입 동료)

        private static UIAssistRosterEntry BuildAssistEntryPrefab()
        {
            var go = NewUI("UIAssistRosterEntry", null);
            SetHeight(go, 54);
            var img = AddImage(go, SlotBg, UISprite, sliced: true);
            var entry = go.AddComponent<UIAssistRosterEntry>();
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            AddHLG(go, spacing: 8, pad: 6);

            var iconGo = NewUI("Icon", go.transform);
            SetWidth(iconGo, 42);
            var icon = AddImage(iconGo, Color.white);
            icon.preserveAspect = true;

            var col = NewUI("Col", go.transform);
            AddFlexibleW(col, 1f);
            AddVLG(col, spacing: 2, pad: 2).childForceExpandHeight = false;
            var name = AddText(NewUI("Name", col.transform), "어시스트", 18, TextMain, TextAlignmentOptions.Left);
            var role = AddText(NewUI("Role", col.transform), "역할", 14, TextSub, TextAlignmentOptions.Left);

            var cooldown = AddText(NewUI("Cooldown", go.transform), "", 16, TextSub, TextAlignmentOptions.Right);
            SetWidth(cooldown.gameObject, 44);

            var equipped = NewUI("EquippedMark", go.transform);
            SetWidth(equipped, 54);
            AddImage(equipped, Accent, UISprite, sliced: true);
            var equippedText = AddText(NewUI("Text", equipped.transform), "장착", 14, Color.black, TextAlignmentOptions.Center);
            Stretch(equippedText.gameObject);
            equipped.SetActive(false);

            var replace = NewUI("ReplaceMark", go.transform);
            SetWidth(replace, 54);
            AddImage(replace, DangerBg, UISprite, sliced: true);
            var replaceText = AddText(NewUI("Text", replace.transform), "방출", 14, TextMain, TextAlignmentOptions.Center);
            Stretch(replaceText.gameObject);
            replace.SetActive(false);

            var so = new SerializedObject(entry);
            SetRef(so, "_icon",                 icon);
            SetRef(so, "_nameText",             name);
            SetRef(so, "_roleText",             role);
            SetRef(so, "_cooldownText",         cooldown);
            SetRef(so, "_equippedMark",         equipped);
            SetRef(so, "_replaceCandidateMark", replace);
            SetRef(so, "_button",               btn);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, AssistEntryPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UIAssistRosterEntry>();
        }

        private static UIAssistRosterPanel BuildAssistSection(Transform parent, UIAssistRosterEntry entryPrefab)
        {
            var section = NewUI("AssistSection", parent);
            var comp = section.AddComponent<UIAssistRosterPanel>();
            AddVLG(section, spacing: 6, pad: 0).childForceExpandHeight = false;

            var header = NewUI("AssistHeader", section.transform);
            SetHeight(header, 30);
            AddHLG(header, spacing: 8, pad: 0);
            AddText(NewUI("Label", header.transform), "어시스트", 22, TextMain, TextAlignmentOptions.Left);
            var count = AddText(NewUI("Count", header.transform), "0 / 4", 20, TextSub, TextAlignmentOptions.Right);
            AddFlexibleW(count.gameObject, 1f);

            // 교체 대기 안내 (로스터 가득 + 신규 영입 시에만 활성)
            var pending = NewUI("PendingBlock", section.transform);
            SetHeight(pending, 88);
            AddImage(pending, DangerBg, UISprite, sliced: true);
            AddHLG(pending, spacing: 8, pad: 8).childForceExpandWidth = false;
            var pendingIconGo = NewUI("Icon", pending.transform);
            SetWidth(pendingIconGo, 56);
            var pendingIcon = AddImage(pendingIconGo, Color.white);
            pendingIcon.preserveAspect = true;
            var pendingText = AddText(NewUI("Text", pending.transform), "신규 영입 대기", 15, TextMain, TextAlignmentOptions.Left);
            AddFlexibleW(pendingText.gameObject, 1f);
            var btnDiscard = MakeButton("DiscardButton", pending.transform, "포기", out _, BtnBg);
            SetWidth(btnDiscard.gameObject, 70);

            var scroll = NewUI("AssistScroll", section.transform);
            SetHeight(scroll, 220);
            var content = BuildVerticalScroll(scroll);

            var empty = NewUI("Empty", section.transform);
            SetHeight(empty, 30);
            AddText(empty, "영입한 어시스트가 없습니다", 16, TextSub, TextAlignmentOptions.Center);

            var so = new SerializedObject(comp);
            SetRef(so, "_content",              content.transform);
            SetRef(so, "_entryPrefab",          entryPrefab);
            SetRef(so, "_rosterCountText",      count);
            SetRef(so, "_emptyRoot",            empty);
            SetRef(so, "_pendingRoot",          pending);
            SetRef(so, "_pendingIcon",          pendingIcon);
            SetRef(so, "_pendingText",          pendingText);
            SetRef(so, "_discardPendingButton", btnDiscard);
            so.ApplyModifiedPropertiesWithoutUndo();

            pending.SetActive(false);
            return comp;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 상세 패널

        private static GameObject BuildDetailPanel(Transform parent, out UI_PartyDetailPanel comp)
        {
            var detail = NewUI("DetailPanel", parent);
            AddImage(detail, PanelBg, UISprite, sliced: true);
            SetWidth(detail, 530);
            comp = detail.AddComponent<UI_PartyDetailPanel>();
            var detailLayout = AddVLG(detail, spacing: 10, pad: 12);
            detailLayout.childAlignment = TextAnchor.UpperLeft;

            // 상단: 캐릭터 프리뷰
            var preview = NewUI("Preview", detail.transform);
            SetHeight(preview, 390);
            AddImage(preview, FieldBg, UISprite, sliced: true);
            var portraitGo = NewUI("Portrait", preview.transform);
            StretchInset(portraitGo, 28, 18, 28, 18);
            portraitGo.AddComponent<LayoutElement>().ignoreLayout = true;
            AddImage(portraitGo, FieldBg, UISprite, sliced: true);
            var portraitImageGo = NewUI("Image", portraitGo.transform);
            StretchInset(portraitImageGo, 10, 8, 10, 8);
            var portrait = AddImage(portraitImageGo, Color.white);
            portrait.preserveAspect = true;

            // 하단: 선택 캐릭터 정보. 내용이 늘어나도 상세 패널 밖으로 그려지지 않도록 마스크된 스크롤 영역으로 둔다.
            var inspectorScroll = NewUI("InspectorScroll", detail.transform);
            AddFlexible(inspectorScroll, 1f);
            var inspector = BuildVerticalScroll(inspectorScroll);
            var inspectorLayout = inspector.GetComponent<VerticalLayoutGroup>();
            inspectorLayout.spacing = 7;
            inspectorLayout.padding = new RectOffset(0, 0, 0, 0);

            var info = NewUI("Info", inspector.transform);
            AddImage(info, FieldBg, UISprite, sliced: true);
            AddVLG(info, spacing: 4, pad: 10).childForceExpandHeight = false;
            var name = AddText(NewUI("Name", info.transform), "이름", 26, TextMain, TextAlignmentOptions.Left); SetHeight(name.gameObject, 34);
            var stars = AddText(NewUI("Stars", info.transform), "★★★★★", 20, Gold, TextAlignmentOptions.Left); SetHeight(stars.gameObject, 26);
            var level = AddText(NewUI("Level", info.transform), "Lv.1 / 40", 20, TextMain, TextAlignmentOptions.Left); SetHeight(level.gameObject, 26);
            var expBar = NewUI("ExpBar", info.transform); SetHeight(expBar, 10); AddImage(expBar, SlotBg, UISprite, sliced: true);
            var expFillGo = NewUI("Fill", expBar.transform); Stretch(expFillGo);
            var expFill = AddImage(expFillGo, Accent, UISprite, sliced: true); expFill.type = Image.Type.Filled; expFill.fillMethod = Image.FillMethod.Horizontal; expFill.fillAmount = 0f;
            var expText = AddText(NewUI("Exp", info.transform), "0 / 0", 15, TextSub, TextAlignmentOptions.Right); SetHeight(expText.gameObject, 20);
            var weaponVal = BuildStatRow(info.transform, "무기", "-");
            var cpVal = BuildStatRow(info.transform, "전투력", "0"); cpVal.color = Gold;
            var hpVal = BuildStatRow(info.transform, "HP", "0 / 0");
            var hpBar = NewUI("HpBar", info.transform); SetHeight(hpBar, 10); AddImage(hpBar, SlotBg, UISprite, sliced: true);
            var hpFillGo = NewUI("Fill", hpBar.transform); Stretch(hpFillGo);
            var hpFill = AddImage(hpFillGo, HpGreen, UISprite, sliced: true); hpFill.type = Image.Type.Filled; hpFill.fillMethod = Image.FillMethod.Horizontal; hpFill.fillAmount = 1f;
            var weaponIconGo = NewUI("WeaponIcon", info.transform); SetHeight(weaponIconGo, 1); // 아이콘은 무기 행 옆에 두기 어려워 별도 최소 배치
            var weaponIcon = AddImage(weaponIconGo, TextSub); weaponIcon.enabled = false;

            // 능력치
            AddText(NewUI("StatTitle", inspector.transform), "능력치", 20, TextMain, TextAlignmentOptions.Left);
            var statBox = NewUI("Stats", inspector.transform);
            AddImage(statBox, FieldBg, UISprite, sliced: true);
            AddVLG(statBox, spacing: 2, pad: 8).childForceExpandHeight = false;
            var atk    = BuildStatRow(statBox.transform, "공격력", "0");
            var def    = BuildStatRow(statBox.transform, "방어력", "0");
            var hp     = BuildStatRow(statBox.transform, "체력", "0");
            var crit   = BuildStatRow(statBox.transform, "치명타 확률", "0%");
            var critDmg= BuildStatRow(statBox.transform, "치명타 피해", "0%");
            var atkSpd = BuildStatRow(statBox.transform, "공격 속도", "-");

            // 역할 태그
            AddText(NewUI("RoleTitle", inspector.transform), "역할/특성", 20, TextMain, TextAlignmentOptions.Left);
            var roleRow = NewUI("Roles", inspector.transform);
            SetHeight(roleRow, 46);
            AddHLG(roleRow, spacing: 8, pad: 0).childForceExpandWidth = true;
            var roleMelee = BuildRoleTag(roleRow.transform, "근접");
            var roleBal   = BuildRoleTag(roleRow.transform, "균형");
            var roleMob   = BuildRoleTag(roleRow.transform, "기동");

            // 속성
            AddText(NewUI("ElementTitle", inspector.transform), "속성", 20, TextMain, TextAlignmentOptions.Left);
            var elementBox = NewUI("Element", inspector.transform);
            AddImage(elementBox, FieldBg, UISprite, sliced: true);
            AddVLG(elementBox, spacing: 2, pad: 8).childForceExpandHeight = false;
            var elementText = BuildStatRow(elementBox.transform, "속성", "-");
            var elementDescription = AddText(NewUI("Description", elementBox.transform),
                "-", 15, TextSub, TextAlignmentOptions.Left);
            SetHeight(elementDescription.gameObject, 44);

            // 패시브
            AddText(NewUI("PassiveTitle", inspector.transform), "패시브", 20, TextMain, TextAlignmentOptions.Left);
            var passiveRow = NewUI("Passives", inspector.transform);
            SetHeight(passiveRow, 70);
            var passiveLayout = AddHLG(passiveRow, spacing: 8, pad: 0);
            passiveLayout.childForceExpandWidth = false;
            passiveLayout.childAlignment = TextAnchor.MiddleLeft;
            var passiveSlots = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                var slot = NewUI($"Passive{i + 1}", passiveRow.transform);
                passiveSlots[i] = slot;
                SetWidth(slot, 112);
                AddImage(slot, SlotBg, UISprite, sliced: true);
                AddHLG(slot, spacing: 5, pad: 5).childForceExpandWidth = false;

                var iconGo = NewUI("Icon", slot.transform);
                SetWidth(iconGo, 34);
                var icon = AddImage(iconGo, TextSub, UISprite, sliced: true);
                icon.preserveAspect = true;
                icon.enabled = false;

                var label = AddText(NewUI("Label", slot.transform), "-", 14,
                    TextMain, TextAlignmentOptions.MidlineLeft);
                AddFlexibleW(label.gameObject, 1f);
                label.textWrappingMode = TextWrappingModes.Normal;
            }

            var so = new SerializedObject(comp);
            SetRef(so, "_root",            detail);
            SetRef(so, "_portrait",        portrait);
            SetRef(so, "_nameText",        name);
            SetRef(so, "_starsText",       stars);
            SetRef(so, "_levelText",       level);
            SetRef(so, "_expFill",         expFill);
            SetRef(so, "_expText",         expText);
            SetRef(so, "_weaponIcon",      weaponIcon);
            SetRef(so, "_weaponNameText",  weaponVal);
            SetRef(so, "_combatPowerText", cpVal);
            SetRef(so, "_hpText",          hpVal);
            SetRef(so, "_hpFill",          hpFill);
            SetRef(so, "_statAttackText",   atk);
            SetRef(so, "_statDefenseText",  def);
            SetRef(so, "_statHealthText",   hp);
            SetRef(so, "_statCritRateText", crit);
            SetRef(so, "_statCritDmgText",  critDmg);
            SetRef(so, "_statAtkSpeedText", atkSpd);
            SetRef(so, "_roleMelee",    roleMelee);
            SetRef(so, "_roleBalanced", roleBal);
            SetRef(so, "_roleMobility", roleMob);
            SetRef(so, "_elementText",            elementText);
            SetRef(so, "_elementDescriptionText", elementDescription);
            SetArray(so, "_passiveSlots",         passiveSlots);
            so.ApplyModifiedPropertiesWithoutUndo();

            return detail;
        }

        private static GameObject BuildRoleTag(Transform parent, string label)
        {
            var tag = NewUI(label + "Tag", parent);
            AddImage(tag, BtnBg, UISprite, sliced: true);
            var lbl = AddText(NewUI("Label", tag.transform), label, 18, TextMain, TextAlignmentOptions.Center);
            Stretch(lbl.gameObject);
            var hl = NewUI("Highlight", tag.transform);
            Stretch(hl);
            var hlImg = AddImage(hl, new Color(Accent.r, Accent.g, Accent.b, 0.30f));
            hlImg.raycastTarget = false;
            hl.SetActive(false);
            return hl;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 공용 헬퍼

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
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void StretchInset(GameObject go, float left, float top, float right, float bottom)
        {
            var rt = Rt(go);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static void Center(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void AnchorCenter(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void AnchorTopRight(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-6, -6);
        }

        private static void AnchorTopLeft(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(6, -6);
        }

        private static Image AddImage(GameObject go, Color color, Sprite sprite = null, bool sliced = false)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            if (sprite != null) { img.sprite = sprite; img.type = sliced ? Image.Type.Sliced : Image.Type.Simple; }
            return img;
        }

        private static Outline AddOutline(GameObject go, Color color, Vector2 distance)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            return outline;
        }

        private static TextMeshProUGUI AddText(GameObject go, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
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

        /// <summary> "라벨 ... 값" 한 줄. 값 TMP 반환. </summary>
        private static TextMeshProUGUI BuildStatRow(Transform parent, string label, string value)
        {
            var row = NewUI(label + "Row", parent);
            SetHeight(row, 28);
            AddHLG(row, spacing: 6, pad: 0);
            var lbl = AddText(NewUI("Label", row.transform), label, 17, TextSub, TextAlignmentOptions.Left);
            AddFlexibleW(lbl.gameObject, 1f);
            var val = AddText(NewUI("Value", row.transform), value, 17, TextMain, TextAlignmentOptions.Right);
            SetWidth(val.gameObject, 110);
            return val;
        }

        /// <summary> 하단바용 "라벨 / 값" 세로 박스. 값 TMP 반환. </summary>
        private static TextMeshProUGUI BuildStatBox(Transform parent, string label, string value, Color valueColor, float width)
        {
            var box = NewUI(label + "Box", parent);
            SetWidth(box, width);
            AddVLG(box, spacing: 2, pad: 4);
            AddText(NewUI("Label", box.transform), label, 15, TextSub, TextAlignmentOptions.Left);
            var val = AddText(NewUI("Value", box.transform), value, 26, valueColor, TextAlignmentOptions.Left);
            return val;
        }

        private static GameObject BuildVerticalScroll(GameObject scrollGo)
        {
            AddImage(scrollGo, new Color(0.05f, 0.06f, 0.08f, 1f), UISprite, sliced: true);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false; scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = NewUI("Viewport", scrollGo.transform);
            Stretch(viewport);
            AddImage(viewport, new Color(1, 1, 1, 0.01f));
            viewport.AddComponent<RectMask2D>();

            var content = NewUI("Content", viewport.transform);
            var crt = Rt(content);
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1f); crt.anchoredPosition = Vector2.zero; crt.sizeDelta = Vector2.zero;
            AddVLG(content, spacing: 4, pad: 6).childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = Rt(viewport);
            scrollRect.content = crt;
            return content;
        }

        private static VerticalLayoutGroup AddVLG(GameObject go, float spacing, int pad)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        private static HorizontalLayoutGroup AddHLG(GameObject go, float spacing, int pad)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.padding = new RectOffset(pad, pad, pad, pad);
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            h.childAlignment = TextAnchor.MiddleCenter;
            return h;
        }

        private static void SetHeight(GameObject go, float hgt)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = hgt; le.flexibleHeight = 0;
        }

        private static void SetWidth(GameObject go, float w)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = w; le.flexibleWidth = 0;
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

        private static void SetRef(SerializedObject so, string propName, UnityEngine.Object value)
        {
            var p = so.FindProperty(propName);
            if (p == null) { Debug.LogWarning($"[PartyBuilder] 프로퍼티 없음: {propName}"); return; }
            p.objectReferenceValue = value;
        }

        private static void SetArray(SerializedObject so, string propName, UnityEngine.Object[] values)
        {
            var p = so.FindProperty(propName);
            if (p == null) { Debug.LogWarning($"[PartyBuilder] 배열 프로퍼티 없음: {propName}"); return; }
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
