using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Map.EditorTools
{
    /// <summary>
    /// 맵 UI(UI_Scene_Map)에 시안 패널(헤더/범례·필터/지역정보/줌 슬라이더·%)을 "추가"하는 에디터 툴.
    ///
    /// ⚠ 다른 빌더와 달리 자식 전체를 지우지 않는다.
    ///   맵 코어 스캐폴드(MapViewport/MapContainer/MapBackground/컨테이너/기존 버튼)는 절대 건드리지 않고,
    ///   새 패널 GameObject만 이름 기준으로 찾아 교체(GetOrReplace)한다. → 좌표 로직 보존.
    ///
    /// 범례/필터는 현재 아이콘 시스템이 구분 가능한 6개 그룹(MapMarkerCategory)으로 실제 동작한다.
    /// 지역 탭(다중 지역 전환)은 내비게이션 시스템이 없어 생성하지 않는다.
    /// </summary>
    public static class UIMapPanelsBuilder
    {
        private const string MainPrefabPath = "Assets/03.Prefabs/UI/Scene/Map/UI_Scene_Map.prefab";

        private static readonly Color PanelBg  = new Color(0.06f, 0.09f, 0.13f, 0.92f);
        private static readonly Color SlotBg   = new Color(0.14f, 0.17f, 0.22f, 1f);
        private static readonly Color BtnBg    = new Color(0.18f, 0.24f, 0.30f, 1f);
        private static readonly Color TextMain = new Color(0.90f, 0.92f, 0.95f, 1f);
        private static readonly Color TextSub  = new Color(0.62f, 0.68f, 0.74f, 1f);
        private static readonly Color Gold     = new Color(0.90f, 0.78f, 0.45f, 1f);
        private static readonly Color Accent   = new Color(0.35f, 0.80f, 0.90f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        private static Sprite Checkmark=> AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

        public static void Build()
        {
            if (!System.IO.File.Exists(MainPrefabPath))
            {
                EditorUtility.DisplayDialog("맵 UI 빌더",
                    $"대상 프리팹을 찾을 수 없습니다:\n{MainPrefabPath}", "확인");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(MainPrefabPath);
            try
            {
                var map = root.GetComponent<UI_Scene_Map>();
                if (map == null)
                {
                    Debug.LogError("[MapBuilder] 루트에 UI_Scene_Map 컴포넌트가 없습니다. 중단.");
                    return;
                }

                var so = new SerializedObject(map);

                // ── 지도 전용 가상 커서 ──
                // 오른쪽 스틱으로 MapViewport 안의 마커를 직접 가리킨다.
                // 일반 버튼/필터는 기존 UINavigation을 유지하므로 커서는 뷰포트 안에만 둔다.
                var viewport = root.transform.Find("MapViewport") as RectTransform;
                if (viewport != null)
                {
                    var oldCursor = viewport.Find("VirtualCursor");
                    if (oldCursor != null)
                        UnityEngine.Object.DestroyImmediate(oldCursor.gameObject);

                    var cursorGo = NewUI("VirtualCursor", viewport);
                    var cursorRt = Rt(cursorGo);
                    SetAnchored(
                        cursorRt,
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        new Vector2(32f, 32f),
                        Vector2.zero);
                    var cursorImage = AddImage(cursorGo, Gold, UISprite, sliced: true);
                    cursorImage.raycastTarget = false;
                    var outline = cursorGo.AddComponent<Outline>();
                    outline.effectColor = new Color(0.02f, 0.04f, 0.06f, 0.95f);
                    outline.effectDistance = new Vector2(3f, -3f);
                    cursorGo.SetActive(false);

                    var virtualCursor = root.GetComponent<UIVirtualCursorController>();
                    if (virtualCursor == null)
                        virtualCursor = root.AddComponent<UIVirtualCursorController>();
                    virtualCursor.Configure(cursorRt, viewport, root.GetComponent<Canvas>());
                    SetRef(so, "_virtualCursor", virtualCursor);
                }
                else
                {
                    Debug.LogWarning("[MapBuilder] MapViewport를 찾지 못해 가상 커서를 생성하지 않았습니다.");
                }

                // UI_Scene_Map은 전체 화면 맵 스캐폴드 위에 코너 패널을 얹는 구조라 슬라이드시킬 단일 창이 없다.
                // 따라서 UI_SceneBase._sceneContent는 의도적으로 비워 두고 루트 CanvasGroup 페이드만 사용한다.

                // ── 좌상단 타이틀 칩 (시안: 좌상단 "지도" 장식 라벨) ──
                var titleChip = GetOrReplace(root, "MapTitleChip");
                SetAnchored(Rt(titleChip), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(150, 48), new Vector2(20, -18));
                AddImage(titleChip, PanelBg, UISprite, sliced: true);
                var chipLabel = AddText(NewUI("Label", titleChip.transform), "지도", 26, TextMain, TextAlignmentOptions.Center);
                Stretch(chipLabel.gameObject);

                // ── 헤더(상단 중앙: 타이틀 + 지역 브레드크럼) ──
                var header = GetOrReplace(root, "MapHeaderPanel");
                SetAnchored(Rt(header), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(760, 48), new Vector2(0, -22));
                var headerHlg = AddHLG(header, spacing: 40, pad: 0);
                headerHlg.childForceExpandWidth = false;
                headerHlg.childAlignment = TextAnchor.MiddleCenter;
                AddText(NewUI("MapTitle", header.transform), "지도", 28, TextMain, TextAlignmentOptions.Center);
                var headerRegion = AddText(NewUI("Region", header.transform), "벨리안 대륙    그레이우드 평원", 24, Gold, TextAlignmentOptions.Center);
                SetRef(so, "_headerRegionText", headerRegion);

                var navigationPrompts = UPlayGround.UI.EditorTools.UIInputPromptBarBuilderUtility
                    .AddBar(
                        root.transform,
                        "MapPromptBar",
                        42f,
                        new UPlayGround.UI.EditorTools.UIInputPromptBarBuilderUtility.PromptSpec(
                            UPlayGround.InputDefine.UIAction.MainTabPrevious, "이전 메뉴"),
                        new UPlayGround.UI.EditorTools.UIInputPromptBarBuilderUtility.PromptSpec(
                            UPlayGround.InputDefine.UIAction.MainTabNext, "다음 메뉴"),
                        new UPlayGround.UI.EditorTools.UIInputPromptBarBuilderUtility.PromptSpec(
                            UPlayGround.InputDefine.UIAction.Submit, "마커 선택"),
                        new UPlayGround.UI.EditorTools.UIInputPromptBarBuilderUtility.PromptSpec(
                            UPlayGround.InputDefine.UIAction.Cancel, "닫기"));
                SetAnchored(
                    (RectTransform)navigationPrompts.transform,
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f),
                    new Vector2(900f, 42f),
                    new Vector2(0f, 18f));

                // ── 범례/필터 패널(우상단, 콘텐츠 맞춤 컴팩트 높이) ──
                var legend = GetOrReplace(root, "MapLegendPanel");
                SetAnchored(Rt(legend), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                            new Vector2(300, 0), new Vector2(-20, -70));
                AddImage(legend, PanelBg, UISprite, sliced: true);
                AddVLG(legend, spacing: 4, pad: 12).childForceExpandHeight = false;
                // 세로축은 자식 콘텐츠 합에 맞춤(시안처럼 상단 우측에 컴팩트하게)
                FitPreferredHeight(legend);

                var tabRow = NewUI("Tabs", legend.transform);
                SetHeight(tabRow, 40);
                AddHLG(tabRow, spacing: 4, pad: 0).childForceExpandWidth = true;
                MakeStaticTab(tabRow.transform, "범례", true);
                MakeStaticTab(tabRow.transform, "필터", false);

                var tPlayer = MakeLegendRow(legend.transform, "플레이어",            Accent);
                var tQuest  = MakeLegendRow(legend.transform, "퀘스트 목표",          Gold);
                var tEnemy  = MakeLegendRow(legend.transform, "적",                  new Color(0.85f, 0.30f, 0.30f));
                var tNpc    = MakeLegendRow(legend.transform, "NPC / 상인 / 채집",     new Color(0.55f, 0.80f, 0.45f));
                var tStatic = MakeLegendRow(legend.transform, "포탈 / 거점 / 던전",     new Color(0.55f, 0.75f, 0.95f));

                var clearBtn = MakeButton("ClearAllButton", legend.transform, "전체 해제", out _, BtnBg);
                SetHeight(clearBtn.gameObject, 44);

                SetRef(so, "_togglePlayer", tPlayer);
                SetRef(so, "_toggleQuest",  tQuest);
                SetRef(so, "_toggleEnemy",  tEnemy);
                SetRef(so, "_toggleNpc",    tNpc);
                SetRef(so, "_toggleStatic", tStatic);
                SetRef(so, "_clearAllButton", clearBtn);

                // ── 지역 정보 패널(좌하단) ──
                var region = GetOrReplace(root, "MapRegionPanel");
                SetAnchored(Rt(region), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                            new Vector2(320, 280), new Vector2(20, 20));
                AddImage(region, PanelBg, UISprite, sliced: true);
                AddVLG(region, spacing: 6, pad: 12).childForceExpandHeight = false;
                var regionName = AddText(NewUI("Name", region.transform), "지역명", 24, TextMain, TextAlignmentOptions.Left);
                SetHeight(regionName.gameObject, 32);
                var regionLevel = AddText(NewUI("Level", region.transform), "권장 레벨  Lv. 1 ~ 1", 18, Gold, TextAlignmentOptions.Left);
                SetHeight(regionLevel.gameObject, 24);
                var regionDesc = AddText(NewUI("Desc", region.transform), "지역 설명", 16, TextSub, TextAlignmentOptions.TopLeft);
                AddFlexible(regionDesc.gameObject, 1);
                var regionInfoBtn = MakeButton("RegionInfoButton", region.transform, "지역 정보", out _, BtnBg);
                SetHeight(regionInfoBtn.gameObject, 42);

                SetRef(so, "_regionNameText",  regionName);
                SetRef(so, "_regionLevelText", regionLevel);
                SetRef(so, "_regionDescText",  regionDesc);
                SetRef(so, "_regionInfoButton", regionInfoBtn);

                // ── 세로 줌 슬라이더 제거 ──
                // 시안에는 우측 세로 슬라이더가 없다. 줌은 스캐폴드 "Buttons"(확대/축소/내 위치)
                // 버튼 바 + 우하단 % 표기로 처리한다. 이전 빌드가 만든 슬라이더가 있으면 정리한다.
                var oldSlider = root.transform.Find("MapZoomSlider");
                if (oldSlider != null) UnityEngine.Object.DestroyImmediate(oldSlider.gameObject);
                SetRef(so, "_zoomSlider", null);

                // ── 줌 % (우하단 코너, 확대/축소 버튼 바 아래). 라벨+값 세로 스택 ──
                var zpct = GetOrReplace(root, "MapZoomPercent");
                SetAnchored(Rt(zpct), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                            new Vector2(150, 56), new Vector2(-20, 20));
                AddImage(zpct, PanelBg, UISprite, sliced: true);
                AddVLG(zpct, spacing: 0, pad: 4).childForceExpandHeight = true;
                var zoomLabel = AddText(NewUI("Label", zpct.transform), "줌 배율", 13, TextSub, TextAlignmentOptions.Center);
                SetHeight(zoomLabel.gameObject, 16);
                var zoomText = AddText(NewUI("Text", zpct.transform), "200%", 22, TextMain, TextAlignmentOptions.Center);
                SetRef(so, "_zoomText", zoomText);

                // ── 지역 선택 패널(좌측, 타이틀 칩 아래). 런타임에 DB 지역으로 채워짐 ──
                var regionSel = GetOrReplace(root, "MapRegionSelectorPanel");
                SetAnchored(Rt(regionSel), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                            new Vector2(210, 0), new Vector2(20, -76));
                AddImage(regionSel, PanelBg, UISprite, sliced: true);
                AddVLG(regionSel, spacing: 4, pad: 10).childForceExpandHeight = false;
                FitPreferredHeight(regionSel);

                var regionSelTitle = AddText(NewUI("Title", regionSel.transform), "지역 이동", 18, Gold, TextAlignmentOptions.Left);
                SetHeight(regionSelTitle.gameObject, 26);

                var regionContent = NewUI("Content", regionSel.transform);
                AddVLG(regionContent, spacing: 4, pad: 0).childForceExpandHeight = false;
                var regionTemplate = MakeButton("RegionButtonTemplate", regionContent.transform, "지역", out _, BtnBg);
                SetHeight(regionTemplate.gameObject, 36);
                regionTemplate.gameObject.SetActive(false);   // 복제용 템플릿

                SetRef(so, "_regionListContent",    Rt(regionContent));
                SetRef(so, "_regionButtonTemplate", regionTemplate);

                // ── 이동 확인 팝업(중앙 오버레이, 기본 숨김) ──
                var confirm = GetOrReplace(root, "MapConfirmPanel");
                Stretch(confirm);
                AddImage(confirm, new Color(0f, 0f, 0f, 0.6f));   // 딤 + 하위 클릭 차단

                var confirmBox = NewUI("Box", confirm.transform);
                SetAnchored(Rt(confirmBox), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                            new Vector2(520, 220), Vector2.zero);
                AddImage(confirmBox, PanelBg, UISprite, sliced: true);

                var confirmMsg = AddText(NewUI("Message", confirmBox.transform), "이동하시겠습니까?", 22, TextMain, TextAlignmentOptions.Center);
                SetAnchored(Rt(confirmMsg.gameObject), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(-40, 90), new Vector2(0, -30));

                var confirmBtnRow = NewUI("Buttons", confirmBox.transform);
                SetAnchored(Rt(confirmBtnRow), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(440, 60), new Vector2(0, 30));
                AddHLG(confirmBtnRow, spacing: 20, pad: 0).childForceExpandWidth = true;
                var yesBtn = MakeButton("YesButton", confirmBtnRow.transform, "확인", out _, new Color(0.20f, 0.45f, 0.30f, 1f));
                var noBtn  = MakeButton("NoButton",  confirmBtnRow.transform, "취소", out _, BtnBg);

                SetRef(so, "_confirmPanel",       confirm);
                SetRef(so, "_confirmMessageText", confirmMsg);
                SetRef(so, "_confirmYesButton",   yesBtn);
                SetRef(so, "_confirmNoButton",    noBtn);
                confirm.SetActive(false);

                // ── 지역 상세 정보 팝업(중앙 오버레이, 기본 숨김) ──
                var detail = GetOrReplace(root, "MapRegionDetailPanel");
                Stretch(detail);
                AddImage(detail, new Color(0f, 0f, 0f, 0.6f));   // 딤 + 하위 클릭 차단

                var detailBox = NewUI("Box", detail.transform);
                SetAnchored(Rt(detailBox), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                            new Vector2(560, 440), Vector2.zero);
                AddImage(detailBox, PanelBg, UISprite, sliced: true);

                var detailTitle = AddText(NewUI("Title", detailBox.transform), "지역 정보", 26, Gold, TextAlignmentOptions.Center);
                SetAnchored(Rt(detailTitle.gameObject), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(-40, 44), new Vector2(0, -24));

                var detailBody = AddText(NewUI("Body", detailBox.transform), "", 18, TextMain, TextAlignmentOptions.TopLeft);
                var detailBodyRt = Rt(detailBody.gameObject);
                detailBodyRt.anchorMin = new Vector2(0f, 0f); detailBodyRt.anchorMax = new Vector2(1f, 1f);
                detailBodyRt.offsetMin = new Vector2(24, 80); detailBodyRt.offsetMax = new Vector2(-24, -80);

                var detailClose = MakeButton("CloseButton", detailBox.transform, "닫기", out _, BtnBg);
                SetAnchored(Rt(detailClose.gameObject), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(220, 52), new Vector2(0, 24));

                SetRef(so, "_regionDetailPanel",       detail);
                SetRef(so, "_regionDetailTitle",       detailTitle);
                SetRef(so, "_regionDetailBody",        detailBody);
                SetRef(so, "_regionDetailCloseButton", detailClose);
                detail.SetActive(false);

                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
                Debug.Log("[MapBuilder] UI_Scene_Map 패널 추가 완료 (코어 스캐폴드 보존).");
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
        #region 위젯 헬퍼

        private static void MakeStaticTab(Transform parent, string label, bool active)
        {
            var go = NewUI(label, parent);
            AddImage(go, active ? Accent : BtnBg, UISprite, sliced: true);
            var lbl = AddText(NewUI("Label", go.transform), label, 20, active ? Color.black : TextMain, TextAlignmentOptions.Center);
            Stretch(lbl.gameObject);
        }

        /// <summary> 아이콘 + 라벨 + 체크박스(Toggle) 한 줄. Toggle 반환. </summary>
        private static Toggle MakeLegendRow(Transform parent, string label, Color iconColor)
        {
            var row = NewUI(label + "Row", parent);
            SetHeight(row, 44);
            AddHLG(row, spacing: 8, pad: 4);

            var icon = NewUI("Icon", row.transform);
            SetWidth(icon, 28);
            AddImage(icon, iconColor, UISprite, sliced: true);

            var lbl = AddText(NewUI("Label", row.transform), label, 18, TextMain, TextAlignmentOptions.Left);
            AddFlexibleW(lbl.gameObject, 1f);

            // Toggle
            var toggleGo = NewUI("Toggle", row.transform);
            SetWidth(toggleGo, 30);
            var bg = AddImage(toggleGo, SlotBg, UISprite, sliced: true);
            var toggle = toggleGo.AddComponent<Toggle>();
            var check = NewUI("Checkmark", toggleGo.transform);
            Stretch(check);
            var checkImg = AddImage(check, Accent, Checkmark != null ? Checkmark : UISprite, sliced: false);
            toggle.targetGraphic = bg;
            toggle.graphic = checkImg;
            toggle.isOn = true;
            return toggle;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 공용 헬퍼

        private static GameObject GetOrReplace(GameObject root, string name)
        {
            var existing = root.transform.Find(name);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            return NewUI(name, root.transform);
        }

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

        private static void SetAnchored(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size, Vector2 pos)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.sizeDelta = size; rt.anchoredPosition = pos;
        }

        private static Image AddImage(GameObject go, Color color, Sprite sprite = null, bool sliced = false)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            if (sprite != null) { img.sprite = sprite; img.type = sliced ? Image.Type.Sliced : Image.Type.Simple; }
            return img;
        }

        private static TextMeshProUGUI AddText(GameObject go, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
            return t;
        }

        private static Button MakeButton(string name, Transform parent, string label, out TextMeshProUGUI labelText, Color bg)
        {
            var go = NewUI(name, parent);
            var img = AddImage(go, bg, UISprite, sliced: true);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var lblGo = NewUI("Label", go.transform);
            Stretch(lblGo);
            labelText = AddText(lblGo, label, 20, TextMain, TextAlignmentOptions.Center);
            labelText.raycastTarget = false;
            return btn;
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

        /// <summary> 세로축을 자식 콘텐츠 합(preferred)에 맞춘다. VLG와 함께 쓴다. </summary>
        private static void FitPreferredHeight(GameObject go)
        {
            var fitter = go.GetComponent<ContentSizeFitter>() ?? go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
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
            if (p == null) { Debug.LogWarning($"[MapBuilder] 프로퍼티 없음: {propName}"); return; }
            p.objectReferenceValue = value;
        }

        #endregion
    }
}
