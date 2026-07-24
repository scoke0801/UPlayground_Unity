using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.SettingMenu.EditorTools
{
    /// <summary>
    /// 설정 메뉴(UI_SettingMenu) 프리팹 초안을 코드로 재구성하고 SerializeField를 자동 연결하는 에디터 툴.
    ///
    /// - 기존 UI_SettingMenu.prefab의 루트/스크립트(guid)는 유지한 채 자식 계층만 재구성(덮어쓰기).
    /// - 재실행 가능(idempotent). "동작하는 회색 초안"이 목표이며 아이콘/스프라이트/폰트는 Unity에서 다듬는다.
    /// - 기존 공용 컨트롤 프리팹(UICommonSlider/UISwitcherButton/UICommonDropDown)을 인스턴스화해
    ///   기존 페이지 스크립트(UISettingPageGamePlay 등)의 연동 규약을 그대로 만족시킨다.
    ///
    /// 연동 규약(중요):
    ///  - 게임플레이/오디오 페이지는 자식 컨트롤을 GetComponentsInChildren '순서'로 매핑한다.
    ///      게임플레이 슬라이더[0]=수평, [1]=수직 / 스위치[0]=Y반전, [1]=화면흔들림, [2]=타겟보정 / 드롭다운[0]=언어
    ///      오디오   슬라이더[0]=마스터, [1]=배경음악, [2]=효과음, [3]=음성
    ///  - 그래픽 페이지는 명시적 [SerializeField] 참조(_resolutionDropdown/_windowModeDropdown/_qualityDropdown/_frameRateSlider/_brightnessSlider) + 스위치[0]=백그라운드실행.
    ///  - 게임플레이 언어 드롭다운은 페이지가 옵션을 세팅하지 않으므로 여기서 옵션을 authoring한다.
    ///  - _audioMixer 필드는 건드리지 않는다(기존 연결 유지).
    /// </summary>
    public static class UISettingMenuPrefabBuilder
    {
        private const string MainPrefabPath   = "Assets/03.Prefabs/UI/Scene/UI_SettingMenu.prefab";
        private const string SliderPrefabPath   = "Assets/03.Prefabs/UI/Common/UICommonSlider.prefab";
        private const string SwitchPrefabPath   = "Assets/03.Prefabs/UI/Common/UISwitcherButton.prefab";
        private const string DropdownPrefabPath = "Assets/03.Prefabs/UI/Common/UICommonDropDown.prefab";

        private static readonly Color Dim       = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color PanelBg   = new Color(0.05f, 0.07f, 0.10f, 0.98f);
        private static readonly Color NavBg     = new Color(0.07f, 0.10f, 0.14f, 1f);
        private static readonly Color TabBg     = new Color(0.09f, 0.12f, 0.16f, 1f);
        private static readonly Color TabActive = new Color(0.10f, 0.20f, 0.26f, 1f);
        private static readonly Color RowBg     = new Color(0.08f, 0.11f, 0.15f, 1f);
        private static readonly Color ApplyBg   = new Color(0.10f, 0.28f, 0.34f, 1f);
        private static readonly Color CancelBg  = new Color(0.10f, 0.13f, 0.17f, 1f);
        private static readonly Color ResetBg   = new Color(0.28f, 0.10f, 0.12f, 1f);
        private static readonly Color TextMain  = new Color(0.90f, 0.92f, 0.95f, 1f);
        private static readonly Color TextGold  = new Color(0.86f, 0.80f, 0.62f, 1f);
        private static readonly Color TextSub   = new Color(0.72f, 0.78f, 0.84f, 1f);
        private static readonly Color Accent    = new Color(0.35f, 0.80f, 0.90f, 1f);
        private static readonly Color IconTint  = new Color(0.80f, 0.85f, 0.90f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static void Build()
        {
            if (!System.IO.File.Exists(MainPrefabPath))
            {
                EditorUtility.DisplayDialog("설정 메뉴 빌더",
                    $"대상 프리팹을 찾을 수 없습니다:\n{MainPrefabPath}", "확인");
                return;
            }
            if (!VerifyControlPrefabs()) return;

            var root = PrefabUtility.LoadPrefabContents(MainPrefabPath);
            try
            {
                var menu = root.GetComponent<UI_SettingMenu>();
                if (menu == null)
                {
                    Debug.LogError("[SettingBuilder] 루트에 UI_SettingMenu 컴포넌트가 없습니다. 중단.");
                    return;
                }

                ClearChildren(root.transform);

                // 반투명 배경
                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                AddImage(dim, Dim);

                // 설정은 전체 화면을 덮는 형태. 패널을 화면 전체로 스트레치한다(여백 없음).
                var panel = NewUI("Panel", root.transform);
                Stretch(panel);
                AddImage(panel, PanelBg, UISprite, sliced: true);
                var panelV = AddVLG(panel, spacing: 12, pad: 20);
                panelV.childForceExpandHeight = false;

                // ── 헤더(제목 + X) ──
                var header = NewUI("Header", panel.transform);
                SetHeight(header, 60);
                AddHLG(header, spacing: 10, pad: 0).childAlignment = TextAnchor.MiddleCenter;
                var title = AddText(NewUI("Title", header.transform), "설정", 38, TextGold, TextAlignmentOptions.Center);
                AddFlexibleW(title.gameObject, 1f);
                var btnClose = MakeSimpleButton("CloseButton", header.transform, "X", CancelBg, TextMain, width: 60, fontSize: 30);

                // ── 본문(좌측 탭 + 우측 콘텐츠) ──
                var body = NewUI("Body", panel.transform);
                AddFlexibleH(body, 1f);
                AddHLG(body, spacing: 16, pad: 0).childForceExpandHeight = true;

                // 좌측 탭 내비
                var nav = NewUI("TabNav", body.transform);
                SetWidth(nav, 300);
                AddImage(nav, NavBg, UISprite, sliced: true);
                var navV = AddVLG(nav, spacing: 10, pad: 16);
                navV.childForceExpandHeight = false;
                navV.childAlignment = TextAnchor.UpperCenter;
                var tabGameplay = MakeTabButton(nav.transform, "게임플레이");
                var tabGraphic  = MakeTabButton(nav.transform, "그래픽");
                var tabAudio    = MakeTabButton(nav.transform, "오디오");
                var tabKeys     = MakeTabButton(nav.transform, "키 설정");

                // 탭 그룹(단일 선택 관리) — 배치 순서는 UI_SettingMenu의 페이지 인덱스와 일치
                var tabGroup = nav.AddComponent<UITabGroup>();
                tabGroup.SetTabs(new[] { tabGameplay, tabGraphic, tabAudio, tabKeys });

                // 우측 콘텐츠 컨테이너(패널들이 겹쳐 stretch, 활성 탭만 표시)
                var content = NewUI("Content", body.transform);
                AddFlexibleW(content, 1f);

                // 각 페이지 패널 빌드
                var gameplayPage = BuildGameplayPanel(content.transform);
                var graphicPage  = BuildGraphicPanel(content.transform);
                var audioPage    = BuildAudioPanel(content.transform);
                var keysPage     = BuildKeysPanel(content.transform);

                // 기본 표시: 게임플레이만
                graphicPage.gameObject.SetActive(false);
                audioPage.gameObject.SetActive(false);
                keysPage.gameObject.SetActive(false);

                // ── 푸터(초기화/취소/적용) ──
                var footer = NewUI("Footer", panel.transform);
                SetHeight(footer, 68);
                AddHLG(footer, spacing: 16, pad: 0).childAlignment = TextAnchor.MiddleCenter;
                AddFlexibleW(NewUI("FooterSpacerL", footer.transform), 1f);
                var btnReset  = MakeSimpleButton("ResetButton",  footer.transform, "초기화", ResetBg,  TextMain, width: 240, fontSize: 26);
                var btnCancel = MakeSimpleButton("CancelButton", footer.transform, "취소",  CancelBg, TextMain, width: 240, fontSize: 26);
                var btnApply  = MakeSimpleButton("ApplyButton",  footer.transform, "적용",  ApplyBg,  TextMain, width: 240, fontSize: 26);
                AddFlexibleW(NewUI("FooterSpacerR", footer.transform), 1f);

                // ── 필드 연결 ──
                var so = new SerializedObject(menu);
                SetRef(so, "_sceneContent", panel.GetComponent<RectTransform>()); // Scene 열기/닫기 슬라이드 대상
                SetRef(so, "_panelGameplay", gameplayPage);
                SetRef(so, "_panelGraphics", graphicPage);
                SetRef(so, "_panelAudio",    audioPage);
                SetRef(so, "_panelKeys",     keysPage);
                SetRef(so, "_tabGroup",      tabGroup);
                SetRef(so, "_btnApply",      btnApply);
                SetRef(so, "_btnCancel",     btnCancel);
                SetRef(so, "_btnReset",      btnReset);
                SetRef(so, "_btnClose",      btnClose);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
                Debug.Log("[SettingBuilder] UI_SettingMenu 프리팹 초안 재구성 완료.");
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
        #region 패널 빌드

        private static T NewPagePanel<T>(string name, Transform content) where T : UISettingPageBase
        {
            var panel = NewUI(name, content);
            Stretch(panel);
            var v = AddVLG(panel, spacing: 10, pad: 24);
            v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.UpperCenter;
            return panel.AddComponent<T>();
        }

        private static UISettingPageGamePlay BuildGameplayPanel(Transform content)
        {
            var page = NewPagePanel<UISettingPageGamePlay>("Panel_Gameplay", content);
            var root = page.transform;

            AddSectionHeader(root, "카메라 설정");
            MakeSliderRow(root, "수평 감도", 1, 10, 5);   // sliders[0]
            MakeSliderRow(root, "수직 감도", 1, 10, 5);   // sliders[1]
            MakeSwitchRow(root, "Y축 반전");              // switches[0]

            AddSectionHeader(root, "전투 설정");
            MakeSwitchRow(root, "화면 흔들림");           // switches[1]
            MakeSwitchRow(root, "타겟 보정");             // switches[2]

            AddSectionHeader(root, "언어 설정");
            MakeDropdownRow(root, "언어", new[] { "한국어", "English", "日本語" }, 0); // dropdowns[0]
            return page;
        }

        private static UISettingPageGraphic BuildGraphicPanel(Transform content)
        {
            var page = NewPagePanel<UISettingPageGraphic>("Panel_Graphics", content);
            var root = page.transform;

            AddSectionHeader(root, "화면 설정");
            var res    = MakeDropdownRow(root, "해상도",   null, 0); // 옵션은 런타임에 페이지가 채움
            var win    = MakeDropdownRow(root, "창 모드",  null, 0); // 옵션은 런타임에 페이지가 채움
            var quality = MakeDropdownRow(root, "그래픽 품질", null, 2); // 옵션은 런타임에 페이지가 채움
            var fps    = MakeSliderRow(root, "프레임 제한", 30, 144, 60);
            var bright = MakeSliderRow(root, "화면 밝기",   0, 10, 5);

            AddSectionHeader(root, "기타");
            MakeSwitchRow(root, "백그라운드 실행");        // switches[0]

            var gso = new SerializedObject(page);
            SetRef(gso, "_resolutionDropdown", res);
            SetRef(gso, "_windowModeDropdown", win);
            SetRef(gso, "_qualityDropdown", quality);
            SetRef(gso, "_frameRateSlider",    fps);
            SetRef(gso, "_brightnessSlider",   bright);
            gso.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static UISettingPageAudio BuildAudioPanel(Transform content)
        {
            var page = NewPagePanel<UISettingPageAudio>("Panel_Audio", content);
            var root = page.transform;

            AddSectionHeader(root, "볼륨");
            MakeSliderRow(root, "마스터",   0, 10, 8); // sliders[0]
            MakeSliderRow(root, "배경음악", 0, 10, 7); // sliders[1]
            MakeSliderRow(root, "효과음",   0, 10, 9); // sliders[2]
            MakeSliderRow(root, "음성",     0, 10, 8); // sliders[3]
            return page;
        }

        private static UISettingPageKeyBinding BuildKeysPanel(Transform content)
        {
            var page = NewPagePanel<UISettingPageKeyBinding>("Panel_Keys", content);
            Transform root = page.transform;
            AddSectionHeader(root, "키 설정");

            var toolbar = NewUI("DeviceToolbar", root);
            SetHeight(toolbar, 60);
            AddHLG(toolbar, spacing: 10, pad: 0).childAlignment = TextAnchor.MiddleLeft;
            Button keyboardButton = MakeSimpleButton(
                "KeyboardMouseButton", toolbar.transform, "키보드/마우스",
                TabActive, TextMain, width: 230, fontSize: 22);
            Button gamepadButton = MakeSimpleButton(
                "GamepadButton", toolbar.transform, "게임패드",
                TabBg, TextMain, width: 180, fontSize: 22);
            AddFlexibleW(NewUI("ToolbarSpacer", toolbar.transform), 1f);

            GameObject categoryObject = InstantiateControl(DropdownPrefabPath, toolbar.transform);
            SetWidth(categoryObject, 260);
            TMP_Dropdown categoryDropdown = categoryObject.GetComponent<TMP_Dropdown>();

            var scrollObject = NewUI("BindingScroll", root);
            AddFlexibleH(scrollObject, 1f);
            AddImage(scrollObject, NavBg, UISprite, sliced: true);
            ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 42f;

            var viewport = NewUI("Viewport", scrollObject.transform);
            StretchMargin(viewport, 8, 8, 8, 8);
            Image viewportImage = AddImage(viewport, new Color(0f, 0f, 0f, 0.001f));
            viewportImage.raycastTarget = true;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            var bindingContent = NewUI("Content", viewport.transform);
            RectTransform bindingContentRect = Rt(bindingContent);
            bindingContentRect.anchorMin = new Vector2(0f, 1f);
            bindingContentRect.anchorMax = new Vector2(1f, 1f);
            bindingContentRect.pivot = new Vector2(0.5f, 1f);
            bindingContentRect.offsetMin = Vector2.zero;
            bindingContentRect.offsetMax = Vector2.zero;
            AddVLG(bindingContent, spacing: 8, pad: 4);
            var fitter = bindingContent.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = Rt(viewport);
            scrollRect.content = bindingContentRect;

            UIKeyBindingRow rowTemplate = MakeKeyBindingRow(bindingContent.transform);
            rowTemplate.gameObject.name = "BindingRowTemplate";
            rowTemplate.gameObject.SetActive(false);

            var resetBar = NewUI("ResetBar", root);
            SetHeight(resetBar, 56);
            AddHLG(resetBar, spacing: 10, pad: 0).childAlignment = TextAnchor.MiddleRight;
            AddFlexibleW(NewUI("ResetSpacer", resetBar.transform), 1f);
            Button resetDeviceButton = MakeSimpleButton(
                "ResetDeviceButton", resetBar.transform, "현재 장치 초기화",
                ResetBg, TextMain, width: 230, fontSize: 21);

            GameObject captureOverlay = MakeOverlay(root, "CaptureOverlay");
            TextMeshProUGUI captureTitle = AddText(
                NewUI("Title", captureOverlay.transform),
                "입력 변경", 30, TextGold, TextAlignmentOptions.Center);
            SetHeight(captureTitle.gameObject, 52);
            TextMeshProUGUI captureMessage = AddText(
                NewUI("Message", captureOverlay.transform),
                "새 키를 입력하세요.", 24, TextMain, TextAlignmentOptions.Center);
            SetHeight(captureMessage.gameObject, 100);
            captureOverlay.SetActive(false);

            GameObject conflictOverlay = MakeOverlay(root, "ConflictOverlay");
            TextMeshProUGUI conflictMessage = AddText(
                NewUI("Message", conflictOverlay.transform),
                "이미 사용 중인 입력입니다.", 24, TextMain, TextAlignmentOptions.Center);
            SetHeight(conflictMessage.gameObject, 110);
            var conflictButtons = NewUI("Buttons", conflictOverlay.transform);
            SetHeight(conflictButtons, 58);
            AddHLG(conflictButtons, spacing: 12, pad: 0).childAlignment = TextAnchor.MiddleCenter;
            Button replaceButton = MakeSimpleButton(
                "ReplaceButton", conflictButtons.transform, "기존 해제 후 적용",
                ApplyBg, TextMain, width: 250, fontSize: 21);
            Button conflictCancelButton = MakeSimpleButton(
                "CancelButton", conflictButtons.transform, "취소",
                CancelBg, TextMain, width: 160, fontSize: 21);
            conflictOverlay.SetActive(false);

            var so = new SerializedObject(page);
            SetRef(so, "_keyboardMouseButton", keyboardButton);
            SetRef(so, "_gamepadButton", gamepadButton);
            SetRef(so, "_categoryDropdown", categoryDropdown);
            SetRef(so, "_content", bindingContentRect);
            SetRef(so, "_rowTemplate", rowTemplate);
            SetRef(so, "_resetDeviceButton", resetDeviceButton);
            SetRef(so, "_captureOverlay", captureOverlay);
            SetRef(so, "_captureTitle", captureTitle);
            SetRef(so, "_captureMessage", captureMessage);
            SetRef(so, "_conflictOverlay", conflictOverlay);
            SetRef(so, "_conflictMessage", conflictMessage);
            SetRef(so, "_replaceButton", replaceButton);
            SetRef(so, "_conflictCancelButton", conflictCancelButton);
            so.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static UIKeyBindingRow MakeKeyBindingRow(Transform parent)
        {
            var row = NewUI("BindingRow", parent);
            SetHeight(row, 64);
            AddImage(row, RowBg, UISprite, sliced: true);
            AddHLG(row, spacing: 10, pad: 10).childAlignment = TextAnchor.MiddleLeft;

            TextMeshProUGUI actionLabel = AddText(
                NewUI("ActionLabel", row.transform),
                "액션", 22, TextSub, TextAlignmentOptions.Left);
            SetWidth(actionLabel.gameObject, 220);

            Button primaryButton = MakeSimpleButton(
                "PrimaryButton", row.transform, "Primary",
                TabActive, TextMain, width: 230, fontSize: 20);
            TextMeshProUGUI primaryLabel = primaryButton.GetComponentInChildren<TextMeshProUGUI>(true);

            Button secondaryButton = MakeSimpleButton(
                "SecondaryButton", row.transform, "미지정",
                TabBg, TextMain, width: 230, fontSize: 20);
            TextMeshProUGUI secondaryLabel = secondaryButton.GetComponentInChildren<TextMeshProUGUI>(true);

            Button resetButton = MakeSimpleButton(
                "ResetButton", row.transform, "초기화",
                CancelBg, TextSub, width: 110, fontSize: 18);

            UIKeyBindingRow component = row.AddComponent<UIKeyBindingRow>();
            var so = new SerializedObject(component);
            SetRef(so, "_actionLabel", actionLabel);
            SetRef(so, "_primaryButton", primaryButton);
            SetRef(so, "_primaryLabel", primaryLabel);
            SetRef(so, "_secondaryButton", secondaryButton);
            SetRef(so, "_secondaryLabel", secondaryLabel);
            SetRef(so, "_resetButton", resetButton);
            so.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }

        private static GameObject MakeOverlay(Transform parent, string name)
        {
            var overlay = NewUI(name, parent);
            StretchMargin(overlay, 80, 100, 80, 100);
            AddImage(overlay, new Color(0.025f, 0.035f, 0.05f, 0.98f), UISprite, sliced: true);
            VerticalLayoutGroup layout = AddVLG(overlay, spacing: 16, pad: 32);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandHeight = false;

            LayoutElement element = overlay.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
            overlay.transform.SetAsLastSibling();
            return overlay;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 공용 빌드 헬퍼

        private static bool VerifyControlPrefabs()
        {
            foreach (var p in new[] { SliderPrefabPath, SwitchPrefabPath, DropdownPrefabPath })
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(p) == null)
                {
                    EditorUtility.DisplayDialog("설정 메뉴 빌더",
                        $"공용 컨트롤 프리팹을 찾을 수 없습니다:\n{p}", "확인");
                    return false;
                }
            }
            return true;
        }

        private static GameObject InstantiateControl(string path, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            return go;
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
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void StretchMargin(GameObject go, float l, float t, float r, float b)
        {
            var rt = Rt(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b);
            rt.offsetMax = new Vector2(-r, -t);
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

        private static void AddFlexibleW(GameObject go, float flexW)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = flexW;
        }

        private static void AddFlexibleH(GameObject go, float flexH)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleHeight = flexH;
        }

        private static Button MakeSimpleButton(string name, Transform parent, string label, Color bg, Color labelColor,
                                               float width, float fontSize)
        {
            var go = NewUI(name, parent);
            if (width > 0) SetWidth(go, width);
            var img = AddImage(go, bg, UISprite, sliced: true);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var lbl = AddText(NewUI("Label", go.transform), label, fontSize, labelColor, TextAlignmentOptions.Center);
            lbl.raycastTarget = false;
            Stretch(lbl.gameObject);
            return btn;
        }

        private static UITabButton MakeTabButton(Transform parent, string label)
        {
            var go = NewUI("Tab_" + label, parent);
            SetHeight(go, 88);
            var img = AddImage(go, TabBg, UISprite, sliced: true);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            AddHLG(go, spacing: 14, pad: 16).childAlignment = TextAnchor.MiddleLeft;

            var icon = NewUI("Icon", go.transform);
            SetWidth(icon, 40);
            var iconImg = AddImage(icon, IconTint, UISprite, sliced: true);
            iconImg.raycastTarget = false;

            var lbl = AddText(NewUI("Label", go.transform), label, 26, TextMain, TextAlignmentOptions.Left);
            AddFlexibleW(lbl.gameObject, 1f);
            lbl.raycastTarget = false;

            // 선택 시 배경=TabActive/라벨=TextMain, 비선택 시 배경=TabBg/라벨=TextSub
            var tab = go.AddComponent<UITabButton>();
            tab.Configure(
                btn, img, lbl,
                normalBg:     TabBg,
                selectedBg:   TabActive,
                normalText:   TextSub,
                selectedText: TextMain);
            return tab;
        }

        private static void AddSectionHeader(Transform parent, string text)
        {
            var h = NewUI("Section_" + text, parent);
            SetHeight(h, 42);
            AddText(h, "◆ " + text, 26, Accent, TextAlignmentOptions.Left);
        }

        /// <summary> 라벨(좌, 고정폭) + 슬라이더(우, 가변폭). 슬라이더 범위/기본값 설정 후 UICommonSlider 반환. </summary>
        private static UICommonSlider MakeSliderRow(Transform parent, string label, float min, float max, float defaultVal)
        {
            var row = NewUI("Row_" + label, parent);
            SetHeight(row, 60);
            AddImage(row, RowBg, UISprite, sliced: true);
            AddHLG(row, spacing: 16, pad: 12).childAlignment = TextAnchor.MiddleLeft;

            var lbl = AddText(NewUI("Label", row.transform), label, 24, TextSub, TextAlignmentOptions.Left);
            SetWidth(lbl.gameObject, 220);

            var ctrl = InstantiateControl(SliderPrefabPath, row.transform);
            AddFlexibleW(ctrl, 1f);
            var slider = ctrl.GetComponent<Slider>();
            if (slider != null)
            {
                slider.minValue = min;
                slider.maxValue = max;
                slider.wholeNumbers = true;
                slider.SetValueWithoutNotify(Mathf.Clamp(defaultVal, min, max));
            }
            return ctrl.GetComponent<UICommonSlider>();
        }

        /// <summary> 라벨(좌, 가변폭) + 스위치(우, 고정폭). </summary>
        private static UISwitchButton MakeSwitchRow(Transform parent, string label)
        {
            var row = NewUI("Row_" + label, parent);
            SetHeight(row, 60);
            AddImage(row, RowBg, UISprite, sliced: true);
            AddHLG(row, spacing: 16, pad: 12).childAlignment = TextAnchor.MiddleLeft;

            var lbl = AddText(NewUI("Label", row.transform), label, 24, TextSub, TextAlignmentOptions.Left);
            AddFlexibleW(lbl.gameObject, 1f);

            var ctrl = InstantiateControl(SwitchPrefabPath, row.transform);
            SetWidth(ctrl, 200);
            return ctrl.GetComponent<UISwitchButton>();
        }

        /// <summary> 라벨(좌, 가변폭) + 드롭다운(우, 고정폭). options가 있으면 authoring. </summary>
        private static UICommonDropdown MakeDropdownRow(Transform parent, string label, string[] options, int defaultIndex)
        {
            var row = NewUI("Row_" + label, parent);
            SetHeight(row, 60);
            AddImage(row, RowBg, UISprite, sliced: true);
            AddHLG(row, spacing: 16, pad: 12).childAlignment = TextAnchor.MiddleLeft;

            var lbl = AddText(NewUI("Label", row.transform), label, 24, TextSub, TextAlignmentOptions.Left);
            AddFlexibleW(lbl.gameObject, 1f);

            var ctrl = InstantiateControl(DropdownPrefabPath, row.transform);
            SetWidth(ctrl, 360);

            if (options != null && options.Length > 0)
            {
                var dd = ctrl.GetComponent<TMP_Dropdown>();
                if (dd != null)
                {
                    dd.options = new List<TMP_Dropdown.OptionData>();
                    foreach (var o in options)
                        dd.options.Add(new TMP_Dropdown.OptionData(o));
                    dd.SetValueWithoutNotify(Mathf.Clamp(defaultIndex, 0, options.Length - 1));
                    dd.RefreshShownValue();
                }
            }
            return ctrl.GetComponent<UICommonDropdown>();
        }

        private static void SetRef(SerializedObject so, string propName, UnityEngine.Object value)
        {
            var p = so.FindProperty(propName);
            if (p == null)
            {
                Debug.LogWarning($"[SettingBuilder] 직렬화 프로퍼티를 찾을 수 없음: {propName}");
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
