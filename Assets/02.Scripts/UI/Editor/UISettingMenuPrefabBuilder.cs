using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.UI.InputPrompt;

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
    ///      게임플레이 슬라이더[0]=수평, [1]=수직 / 스위치[0]=Y반전, [1]=화면흔들림, [2]=타겟보정
    ///      게임플레이 드롭다운[0]=언어, [1]=대화 타이핑 속도, [2]=대화 자동 재생 간격
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
        private const string InputGlyphDataPath = "Assets/10.Datas/UI/Input/InputGlyphData.asset";

        private static readonly Color Dim       = new Color(0.01f, 0.025f, 0.04f, 0.92f);
        private static readonly Color PanelBg   = new Color(0.035f, 0.055f, 0.08f, 0.99f);
        private static readonly Color NavBg     = new Color(0.055f, 0.075f, 0.105f, 1f);
        private static readonly Color TabBg     = new Color(1f, 1f, 1f, 0f);
        private static readonly Color TabActive = new Color(0.14f, 0.27f, 0.48f, 0.82f);
        private static readonly Color RowBg     = new Color(0.065f, 0.09f, 0.125f, 1f);
        private static readonly Color ApplyBg   = new Color(0.15f, 0.35f, 0.68f, 1f);
        private static readonly Color CancelBg  = new Color(0.065f, 0.09f, 0.125f, 1f);
        private static readonly Color ResetBg   = new Color(0.075f, 0.105f, 0.145f, 1f);
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

                // 상단 탭 → 본문 → 하단 액션의 시각 계층을 가진 전체 화면 패널.
                var panel = NewUI("Panel", root.transform);
                Stretch(panel);
                AddImage(panel, PanelBg, UISprite, sliced: true);
                var panelV = AddVLG(panel, spacing: 14, pad: 30);
                panelV.childForceExpandHeight = false;

                // ── 헤더(제목 + 가로 탭 + 닫기) ──
                var header = NewUI("Header", panel.transform);
                SetHeight(header, 64);
                AddHLG(header, spacing: 8, pad: 0).childAlignment = TextAnchor.MiddleLeft;

                var title = AddText(
                    NewUI("Title", header.transform), "설정", 32, TextMain, TextAlignmentOptions.Left);
                title.fontStyle = FontStyles.Bold;
                SetWidth(title.gameObject, 260);

                var tabGameplay = MakeTopTabButton(header.transform, "게임플레이");
                var tabGraphic  = MakeTopTabButton(header.transform, "그래픽");
                var tabAudio    = MakeTopTabButton(header.transform, "사운드");
                var tabKeys     = MakeTopTabButton(header.transform, "키 설정");

                // 탭 그룹의 순서는 UI_SettingMenu의 페이지 인덱스와 일치해야 한다.
                var tabGroup = header.AddComponent<UITabGroup>();
                tabGroup.SetTabs(new[] { tabGameplay, tabGraphic, tabAudio, tabKeys });

                AddFlexibleW(NewUI("HeaderSpacer", header.transform), 1f);
                var btnClose = MakeSimpleButton(
                    "CloseButton", header.transform, "닫기",
                    CancelBg, TextSub, width: 132, fontSize: 18);

                var headerLine = NewUI("HeaderLine", panel.transform);
                SetHeight(headerLine, 1);
                AddImage(headerLine, new Color(0.22f, 0.29f, 0.38f, 0.75f));

                // ── 본문 ──
                var body = NewUI("Body", panel.transform);
                AddFlexibleH(body, 1f);
                AddImage(body, new Color(0.02f, 0.035f, 0.055f, 0.36f), UISprite, sliced: true);

                // 콘텐츠 패널들이 겹쳐 stretch되고 활성 탭만 표시된다.
                var content = NewUI("Content", body.transform);
                Stretch(content);

                // 각 페이지 패널 빌드
                var gameplayPage = BuildGameplayPanel(content.transform);
                var graphicPage  = BuildGraphicPanel(content.transform);
                var audioPage    = BuildAudioPanel(content.transform);
                var keysPage     = BuildKeysPanel(content.transform);

                // 기본 표시: 게임플레이만
                graphicPage.gameObject.SetActive(false);
                audioPage.gameObject.SetActive(false);
                keysPage.gameObject.SetActive(false);

                // ── 푸터(단축키 힌트 + 초기화/취소/적용) ──
                var footer = NewUI("Footer", panel.transform);
                SetHeight(footer, 62);
                AddHLG(footer, spacing: 12, pad: 0).childAlignment = TextAnchor.MiddleCenter;

                UPlayGround.UI.EditorTools.UIInputPromptBarBuilderUtility
                    .AddMainAndSubNavigationBar(footer.transform, "이전 설정 탭", "다음 설정 탭");

                var btnReset = MakeSimpleButton(
                    "ResetButton", footer.transform, "R  기본값 복원",
                    ResetBg, TextSub, width: 190, fontSize: 17);
                var btnCancel = MakeSimpleButton(
                    "CancelButton", footer.transform, "취소",
                    CancelBg, TextMain, width: 170, fontSize: 18);
                var btnApply = MakeSimpleButton(
                    "ApplyButton", footer.transform, "적용",
                    ApplyBg, TextMain, width: 210, fontSize: 20);

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

        // 대화 타이핑 속도·자동 재생 간격 공통 옵션. SettingsData의 인덱스 의미(0=느림,1=보통,2=빠름)와 순서를 맞춘다.
        private static readonly string[] DialogueSpeedOptions = { "느림", "보통", "빠름" };

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

            AddSectionHeader(root, "대화 설정");
            MakeDropdownRow(root, "타이핑 속도", DialogueSpeedOptions, 1); // dropdowns[1]
            MakeDropdownRow(root, "자동 재생 간격", DialogueSpeedOptions, 1); // dropdowns[2]
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
            // 액션 수에 따라 런타임에 3분할 계층을 만든다. 구형 정적 자식과
            // LayoutElement가 크기 계산에 개입하지 않도록 깨끗한 호스트만 둔다.
            var panel = NewUI("Panel_Keys", content);
            Stretch(panel);
            var page = panel.AddComponent<UISettingPageKeyBinding>();

            InputGlyphDataSO glyphData =
                AssetDatabase.LoadAssetAtPath<InputGlyphDataSO>(InputGlyphDataPath);
            if (glyphData == null)
            {
                Debug.LogWarning(
                    $"[SettingBuilder] 입력 글리프 데이터를 찾을 수 없습니다: {InputGlyphDataPath}");
                return page;
            }

            var so = new SerializedObject(page);
            SetRef(so, "_glyphData", glyphData);
            so.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        // 키 바인딩 행은 UIKeyBindingRow.Build()가 런타임에 직접 구성한다.
        // 프리팹의 Panel_Keys도 자식 없이 UISettingPageKeyBinding만 갖고 있으므로
        // 여기서 정적 BindingRow를 만들던 MakeKeyBindingRow는 제거했다.

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

        private static UITabButton MakeTopTabButton(Transform parent, string label)
        {
            var go = NewUI("Tab_" + label, parent);
            SetWidth(go, 152);
            var img = AddImage(go, TabBg, UISprite, sliced: true);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var lbl = AddText(
                NewUI("Label", go.transform), label, 20, TextMain, TextAlignmentOptions.Center);
            Stretch(lbl.gameObject);
            lbl.raycastTarget = false;

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
