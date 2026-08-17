using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Quest.EditorTools
{
    /// <summary>
    /// 퀘스트 UI(UI_Scene_QuestMenu) 프리팹 초안을 코드로 생성하고 SerializeField를 자동 연결하는 에디터 툴.
    ///
    /// - 기존 UI_Scene_QuestMenu.prefab의 루트/스크립트(guid)는 유지한 채 자식 계층만 재구성.
    /// - 퀘스트/목표/보상 슬롯 서브 프리팹도 함께 생성해 참조를 연결.
    /// - 재실행 가능(idempotent). "동작하는 회색 초안"이 목표이며 색/폰트/스프라이트는 Unity에서 다듬는다.
    /// </summary>
    public static class UIQuestMenuPrefabBuilder
    {
        private const string MainPrefabPath   = "Assets/03.Prefabs/UI/Scene/Quest/UI_Scene_QuestMenu.prefab";
        private const string QuestSlotPath    = "Assets/03.Prefabs/UI/Scene/Quest/UIQuestSlot.prefab";
        private const string ObjectiveSlotPath = "Assets/03.Prefabs/UI/Scene/Quest/UIQuestObjectiveSlot.prefab";
        private const string RewardSlotPath   = "Assets/03.Prefabs/UI/Scene/Quest/UIQuestRewardSlot.prefab";

        private static readonly Color Dim       = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color WindowBg  = new Color(0.07f, 0.09f, 0.12f, 0.98f);
        private static readonly Color PanelBg   = new Color(0.11f, 0.14f, 0.18f, 1f);
        private static readonly Color SlotBg    = new Color(0.16f, 0.19f, 0.24f, 1f);
        private static readonly Color BtnBg     = new Color(0.20f, 0.28f, 0.34f, 1f);
        private static readonly Color AccentBtn = new Color(0.18f, 0.45f, 0.55f, 1f);
        private static readonly Color DangerBtn = new Color(0.45f, 0.18f, 0.18f, 1f);
        private static readonly Color TextMain  = new Color(0.90f, 0.92f, 0.95f, 1f);
        private static readonly Color TextSub   = new Color(0.65f, 0.70f, 0.76f, 1f);
        private static readonly Color Accent    = new Color(0.95f, 0.60f, 0.20f, 1f);
        private static readonly Color Green     = new Color(0.35f, 0.85f, 0.45f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static void Build()
        {
            if (!System.IO.File.Exists(MainPrefabPath))
            {
                EditorUtility.DisplayDialog("퀘스트 UI 빌더",
                    $"대상 프리팹을 찾을 수 없습니다:\n{MainPrefabPath}", "확인");
                return;
            }

            var questSlot = BuildQuestSlotPrefab();
            var objSlot   = BuildObjectiveSlotPrefab();
            var rewardSlot = BuildRewardSlotPrefab();

            var root = PrefabUtility.LoadPrefabContents(MainPrefabPath);
            try
            {
                var menu = root.GetComponent<UI_Scene_QuestMenu>();
                if (menu == null)
                {
                    Debug.LogError("[QuestBuilder] 루트에 UI_Scene_QuestMenu 컴포넌트가 없습니다. 중단.");
                    return;
                }

                ClearChildren(root.transform);

                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                AddImage(dim, Dim);

                // 윈도우는 전체 화면을 채운다.
                var window = NewUI("Window", root.transform);
                Stretch(window);
                AddImage(window, WindowBg, UISprite, sliced: true);
                AddVLG(window, spacing: 8, pad: 16).childForceExpandHeight = false;

                // ── 헤더 ──
                var header = NewUI("Header", window.transform);
                SetHeight(header, 64);
                var title = NewUI("Title", header.transform);
                Stretch(title);
                AddText(title, "퀘스트", 34, TextMain, TextAlignmentOptions.Center);
                var btnClose = MakeButton("BtnClose", header.transform, "X", out _);
                AnchorTopRight(Rt(btnClose.gameObject), 48, 48);

                // ── 본문 ──
                var body = NewUI("Body", window.transform);
                AddFlexible(body, 1);
                var bodyH = AddHLG(body, spacing: 12, pad: 0);
                bodyH.childForceExpandHeight = true;

                // ===== 좌측 =====
                var left = NewUI("LeftPanel", body.transform);
                AddImage(left, PanelBg, UISprite, sliced: true);
                SetWidth(left, 560);
                AddVLG(left, spacing: 8, pad: 10).childForceExpandHeight = false;

                var tabs = NewUI("Tabs", left.transform);
                SetHeight(tabs, 48);
                AddHLG(tabs, spacing: 4, pad: 0).childForceExpandWidth = true;
                var tabAvail = MakeTab("TabAvailable", tabs.transform, "수락 가능", out var cntAvail);
                var tabActive = MakeTab("TabActive",   tabs.transform, "진행 중",  out var cntActive);
                var tabDone  = MakeTab("TabCompleted", tabs.transform, "완료",     out var cntDone);
                var tabFail  = MakeTab("TabFailed",    tabs.transform, "실패",     out var cntFail);

                // 탭 그룹(단일 선택 관리) — 배치 순서는 UI_Scene_QuestMenu.TabOrder와 반드시 일치
                var tabGroup = tabs.AddComponent<UITabGroup>();
                tabGroup.SetTabs(new[] { tabAvail, tabActive, tabDone, tabFail });

                var scroll = NewUI("QuestScroll", left.transform);
                AddFlexible(scroll, 1);
                var listContent = BuildVerticalScroll(scroll);

                // ===== 우측 =====
                var right = NewUI("RightColumn", body.transform);
                AddFlexibleW(right, 1f);
                AddVLG(right, spacing: 8, pad: 0).childForceExpandHeight = false;

                // 상세 패널
                var detail = NewUI("DetailPanel", right.transform);
                AddImage(detail, PanelBg, UISprite, sliced: true);
                var detailGroup = detail.AddComponent<CanvasGroup>();
                AddFlexible(detail, 1);
                AddVLG(detail, spacing: 8, pad: 14).childForceExpandHeight = false;

                // 제목 + 상태 배지
                var titleRow = NewUI("TitleRow", detail.transform);
                SetHeight(titleRow, 46);
                AddHLG(titleRow, spacing: 10, pad: 0);
                var txtTitle = AddText(NewUI("QuestTitle", titleRow.transform), "퀘스트 제목", 30, TextMain, TextAlignmentOptions.Left);
                AddFlexibleW(txtTitle.gameObject, 1f);
                var badgeGo = NewUI("StatusBadge", titleRow.transform);
                SetWidth(badgeGo, 150);
                AddImage(badgeGo, SlotBg, UISprite, sliced: true);
                var txtBadge = AddText(NewUI("Value", badgeGo.transform), "진행 중", 20, Accent, TextAlignmentOptions.Center);
                Stretch(txtBadge.gameObject);

                var txtDesc = AddText(NewUI("Description", detail.transform), "퀘스트 설명", 20, TextSub, TextAlignmentOptions.TopLeft);
                SetHeight(txtDesc.gameObject, 64);

                AddText(NewUI("ObjectiveTitle", detail.transform), "목표", 22, TextMain, TextAlignmentOptions.Left);

                var objBox = NewUI("ObjectiveBox", detail.transform);
                AddImage(objBox, SlotBg, UISprite, sliced: true);
                AddFlexible(objBox, 1);
                var objContent = NewUI("Content", objBox.transform);
                Stretch(objContent);
                AddVLG(objContent, spacing: 4, pad: 8).childForceExpandHeight = false;

                AddText(NewUI("RewardTitle", detail.transform), "보상", 22, TextMain, TextAlignmentOptions.Left);

                var rewardRow = NewUI("RewardRow", detail.transform);
                SetHeight(rewardRow, 96);
                AddHLG(rewardRow, spacing: 10, pad: 0);
                var txtGold = BuildRewardStatBox("GoldBox", rewardRow.transform, "골드", "0");
                var txtExp  = BuildRewardStatBox("ExpBox",  rewardRow.transform, "경험치", "0");
                var itemBox = NewUI("ItemBox", rewardRow.transform);
                AddImage(itemBox, SlotBg, UISprite, sliced: true);
                AddFlexibleW(itemBox, 1f);
                var rewardItemContent = NewUI("Content", itemBox.transform);
                Stretch(rewardItemContent);
                var rewardHlg = AddHLG(rewardItemContent, spacing: 6, pad: 8);
                rewardHlg.childAlignment = TextAnchor.MiddleLeft;

                // 하단 버튼
                var buttons = NewUI("Buttons", right.transform);
                SetHeight(buttons, 60);
                AddHLG(buttons, spacing: 8, pad: 0);
                var btnTrack = MakeButton("BtnTrack", buttons.transform, "추적", out var txtTrack, AccentBtn);
                AddFlexibleW(btnTrack.gameObject, 1f);
                var btnComplete = MakeButton("BtnComplete", buttons.transform, "완료", out _, BtnBg);
                AddFlexibleW(btnComplete.gameObject, 1f);
                var btnAbandon = MakeButton("BtnAbandon", buttons.transform, "포기", out _, DangerBtn);
                AddFlexibleW(btnAbandon.gameObject, 1f);

                // ── 필드 연결 ──
                var so = new SerializedObject(menu);
                SetRef(so, "_sceneContent",  window.GetComponent<RectTransform>()); // Scene 열기/닫기 슬라이드 대상
                SetRef(so, "_tabGroup",      tabGroup);
                SetRef(so, "_txtCountAvailable", cntAvail);
                SetRef(so, "_txtCountActive",    cntActive);
                SetRef(so, "_txtCountCompleted", cntDone);
                SetRef(so, "_txtCountFailed",    cntFail);
                SetRef(so, "_questListContent", listContent.transform);
                SetRef(so, "_questSlotPrefab",  questSlot);
                SetRef(so, "_detailPanel",     detail);
                SetRef(so, "_detailPanelGroup", detailGroup);
                SetRef(so, "_txtQuestTitle",   txtTitle);
                SetRef(so, "_txtStatusBadge",  txtBadge);
                SetRef(so, "_txtQuestDesc",    txtDesc);
                SetRef(so, "_objectiveContent",    objContent.transform);
                SetRef(so, "_objectiveSlotPrefab", objSlot);
                SetRef(so, "_txtRewardGold",   txtGold);
                SetRef(so, "_txtRewardExp",    txtExp);
                SetRef(so, "_rewardItemContent",    rewardItemContent.transform);
                SetRef(so, "_rewardItemSlotPrefab", rewardSlot);
                SetRef(so, "_btnTrack",     btnTrack);
                SetRef(so, "_txtTrackButton", txtTrack);
                SetRef(so, "_btnComplete",  btnComplete);
                SetRef(so, "_btnAbandon",   btnAbandon);
                SetRef(so, "_btnClose",     btnClose);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
                Debug.Log("[QuestBuilder] UI_Scene_QuestMenu 프리팹 초안 생성 완료.");
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

        private static UIQuestSlot BuildQuestSlotPrefab()
        {
            var go = NewUI("UIQuestSlot", null);
            SetHeight(go, 72);
            AddImage(go, SlotBg, UISprite, sliced: true);
            var slot = go.AddComponent<UIQuestSlot>();
            AddHLG(go, spacing: 10, pad: 8);

            var iconGo = NewUI("Icon", go.transform);
            SetWidth(iconGo, 44);
            var icon = AddImage(iconGo, new Color(0.45f, 0.70f, 0.95f, 1f), UISprite, sliced: true);

            var textCol = NewUI("TextCol", go.transform);
            AddFlexibleW(textCol, 1f);
            AddVLG(textCol, spacing: 2, pad: 2).childForceExpandHeight = false;
            var name = AddText(NewUI("Name", textCol.transform), "퀘스트 이름", 22, TextMain, TextAlignmentOptions.Left);
            SetHeight(name.gameObject, 30);
            var summary = AddText(NewUI("Summary", textCol.transform), "짧은 부제", 16, TextSub, TextAlignmentOptions.Left);
            SetHeight(summary.gameObject, 22);

            var trackGo = NewUI("TrackIndicator", go.transform);
            SetWidth(trackGo, 32);
            AddImage(trackGo, Accent, UISprite, sliced: true);
            trackGo.SetActive(false);

            var overlay = NewUI("SelectOverlay", go.transform);
            Stretch(overlay);
            var ov = AddImage(overlay, new Color(0.35f, 0.75f, 0.85f, 0.18f));
            ov.raycastTarget = false;
            overlay.AddComponent<LayoutElement>().ignoreLayout = true;
            overlay.transform.SetAsFirstSibling();
            overlay.SetActive(false);

            var so = new SerializedObject(slot);
            SetRef(so, "_imgIcon",        icon);
            SetRef(so, "_txtName",        name);
            SetRef(so, "_txtSummary",     summary);
            SetRef(so, "_trackIndicator", trackGo);
            SetRef(so, "_selectOverlay",  overlay);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, QuestSlotPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UIQuestSlot>();
        }

        private static UIQuestObjectiveSlot BuildObjectiveSlotPrefab()
        {
            var go = NewUI("UIQuestObjectiveSlot", null);
            SetHeight(go, 40);
            var slot = go.AddComponent<UIQuestObjectiveSlot>();
            AddHLG(go, spacing: 8, pad: 4);

            var checkGo = NewUI("Check", go.transform);
            SetWidth(checkGo, 24);
            var check = AddImage(checkGo, Green, UISprite, sliced: true);

            var desc = AddText(NewUI("Description", go.transform), "목표 설명", 20, TextMain, TextAlignmentOptions.Left);
            AddFlexibleW(desc.gameObject, 1f);

            var progress = AddText(NewUI("Progress", go.transform), "0 / 0", 20, Green, TextAlignmentOptions.Right);
            SetWidth(progress.gameObject, 110);

            var so = new SerializedObject(slot);
            SetRef(so, "_imgCheck",       check);
            SetRef(so, "_txtDescription", desc);
            SetRef(so, "_txtProgress",    progress);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, ObjectiveSlotPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UIQuestObjectiveSlot>();
        }

        private static UIQuestRewardSlot BuildRewardSlotPrefab()
        {
            var go = NewUI("UIQuestRewardSlot", null);
            SetWidth(go, 64);
            SetHeight(go, 64);
            AddImage(go, SlotBg, UISprite, sliced: true);
            var slot = go.AddComponent<UIQuestRewardSlot>();

            var iconGo = NewUI("Icon", go.transform);
            Stretch(iconGo);
            var icon = AddImage(iconGo, new Color(1, 1, 1, 1));

            var countGo = NewUI("Count", go.transform);
            AnchorBottomRight(Rt(countGo), 30, 24);
            var count = AddText(countGo, "1", 18, TextMain, TextAlignmentOptions.BottomRight);
            count.raycastTarget = false;

            var so = new SerializedObject(slot);
            SetRef(so, "_imgIcon",  icon);
            SetRef(so, "_txtCount", count);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, RewardSlotPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UIQuestRewardSlot>();
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

        private static void AnchorBottomRight(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-4, 2);
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

        /// <summary> 라벨 + 우상단 카운트 배지 + UITabButton(선택 하이라이트)을 가진 탭 버튼. </summary>
        private static UITabButton MakeTab(string name, Transform parent, string label, out TextMeshProUGUI countText)
        {
            var btn = MakeButton(name, parent, label, out var labelText, BtnBg);
            var badge = NewUI("Count", btn.transform);
            AnchorTopRight(Rt(badge), 30, 24);
            countText = AddText(badge, "0", 16, Accent, TextAlignmentOptions.Center);
            countText.raycastTarget = false;

            // 선택 시 배경=AccentBtn/라벨=TextMain, 비선택 시 배경=BtnBg/라벨=TextSub
            var tab = btn.gameObject.AddComponent<UITabButton>();
            tab.Configure(
                btn,
                btn.targetGraphic as Image,
                labelText,
                normalBg:     BtnBg,
                selectedBg:   AccentBtn,
                normalText:   TextSub,
                selectedText: TextMain);
            return tab;
        }

        /// <summary> 골드/경험치용 라벨+값 박스. 값 TMP를 반환. </summary>
        private static TextMeshProUGUI BuildRewardStatBox(string name, Transform parent, string label, string value)
        {
            var box = NewUI(name, parent);
            SetWidth(box, 150);
            AddImage(box, SlotBg, UISprite, sliced: true);
            AddVLG(box, spacing: 2, pad: 8);
            AddText(NewUI("Label", box.transform), label, 16, TextSub, TextAlignmentOptions.Center);
            var val = AddText(NewUI("Value", box.transform), value, 24, TextMain, TextAlignmentOptions.Center);
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

        private static GameObject BuildVerticalScroll(GameObject scrollGo)
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
            AddVLG(content, spacing: 4, pad: 6).childForceExpandHeight = false;
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
                Debug.LogWarning($"[QuestBuilder] 직렬화 프로퍼티를 찾을 수 없음: {propName}");
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
