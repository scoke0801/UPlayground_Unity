using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Party;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.UI.CharacterSelect.EditorTools
{
    /// <summary>
    /// 신규 게임 캐릭터 선택 UI(UI_Scene_CharacterSelect) 프리팹 초안을 코드로 생성하고
    /// SerializeField 를 자동 배선하는 에디터 툴.
    ///
    /// - 카드 프리팹(UICharacterSelectCard) + 메인 프리팹(UI_Scene_CharacterSelect)을 처음부터 생성한다.
    /// - 기존 파일이 있으면 덮어쓴다(idempotent).
    /// - 스프라이트/3D 프리뷰 렌더러/데이터(SO)는 배선 대상이 아니며 Unity 에디터에서 수동 연결한다.
    /// </summary>
    public static class UICharacterSelectPrefabBuilder
    {
        private const string Dir            = "Assets/03.Prefabs/UI/Scene/CharacterSelect";
        private const string MainPrefabPath = Dir + "/UI_Scene_CharacterSelect.prefab";
        private const string CardPrefabPath = Dir + "/UICharacterSelectCard.prefab";

        private const string DatabasePath = "Assets/10.Datas/Path/UIPrefabDatabase.asset";
        private const string UiKey        = "CharacterSelect";
        private const CanvasLayer UiLayer = CanvasLayer.Scene;

        // 프리팹 _database 필드에 자동 연결할 캐릭터 선택 목록 SO 경로.
        private const string CharacterDbPath = "Assets/10.Datas/Party/CharacterSelectDatabase.asset";

        // 초상화/무기 아이콘 재사용 소스(PartyMemberData) 경로.
        private const string MemberDataPath = "Assets/10.Datas/Party/PartyMemberData.asset";
        private const string PassiveDbPath = "Assets/10.Datas/Ability/CharacterPassiveDatabase.asset";

        private const int PassiveRowCount = CharacterPassiveSetSO.MaxCharacterSelectRepresentatives;

        // 카드 크기(캐릭터가 많아 가로로 다 들어가도록 축소). 하단 버튼과 겹치지 않게 배치도 함께 조정.
        private const float CardWidth  = 180f;
        private const float CardHeight = 230f;

        private static readonly Color Dim       = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color PanelBg   = new Color(0.09f, 0.11f, 0.16f, 0.96f);
        private static readonly Color CardBg    = new Color(0.14f, 0.17f, 0.23f, 1f);
        private static readonly Color RowBg     = new Color(0.12f, 0.15f, 0.20f, 1f);
        private static readonly Color PreviewBg = new Color(0.06f, 0.07f, 0.10f, 1f);
        private static readonly Color BtnBg     = new Color(0.20f, 0.27f, 0.34f, 1f);
        private static readonly Color Accent    = new Color(0.35f, 0.80f, 0.90f, 1f);
        private static readonly Color TextMain  = new Color(0.90f, 0.92f, 0.95f, 1f);
        private static readonly Color TextSub   = new Color(0.62f, 0.68f, 0.74f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static void Build()
        {
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
                AssetDatabase.Refresh();
            }

            var cardPrefab = BuildCardPrefab();
            var mainPrefab = BuildMainPrefab(cardPrefab);
            bool registered = RegisterInDatabase(mainPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("캐릭터 선택 UI 빌더",
                "프리팹 빌드 완료.\n\n" +
                "· " + MainPrefabPath + "\n" +
                "· " + CardPrefabPath + "\n\n" +
                (registered
                    ? $"UIPrefabDatabase 등록 완료 (key: {UiKey}, layer: {UiLayer}).\n\n"
                    : "UIPrefabDatabase 등록 실패 — 콘솔 경고 확인.\n\n") +
                "남은 수동 작업: 데이터(SO)·스프라이트·3D 프리뷰 렌더러 연결.",
                "확인");
        }

        /// <summary> 메인 프리팹을 UIPrefabDatabase 에 등록한다(중복 시 갱신). </summary>
        private static bool RegisterInDatabase(GameObject mainPrefab)
        {
            if (mainPrefab == null) return false;

            var db = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(DatabasePath);
            if (db == null)
            {
                Debug.LogWarning($"[CharacterSelectBuilder] UIPrefabDatabase 를 찾을 수 없어 등록을 건너뜁니다: {DatabasePath}");
                return false;
            }

            // 재실행 시 항상 최신 프리팹으로 갱신되도록 제거 후 재등록(idempotent).
            db.RemovePrefab(UiKey);
            db.AddPrefab(UiKey, mainPrefab, UiLayer, "신규 게임 캐릭터 선택 화면");
            EditorUtility.SetDirty(db);
            return true;
        }

        // ──────────────────────────────────────────────────────────
        #region 카드 프리팹

        private static UICharacterSelectCard BuildCardPrefab()
        {
            var root = NewUI("UICharacterSelectCard", null);
            Rt(root).sizeDelta = new Vector2(CardWidth, CardHeight);

            var cg = root.AddComponent<CanvasGroup>();
            var rootImg = AddImage(root, CardBg, UISprite, sliced: true);
            var btn = root.AddComponent<Button>();
            btn.targetGraphic = rootImg;

            var le = root.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = CardWidth;
            le.minHeight = le.preferredHeight = CardHeight;
            le.flexibleWidth = le.flexibleHeight = 0;

            var card = root.AddComponent<UICharacterSelectCard>();

            var content = NewUI("Content", root.transform);
            Stretch(content);

            var portrait = NewUI("Portrait", content.transform);
            StretchInset(portrait, 10, 10, 10, 52);
            var portraitImg = AddImage(portrait, Color.white);
            portraitImg.preserveAspect = true;
            portraitImg.raycastTarget = false;

            // 선택 강조 테두리 — 채움이 아니라 4변 얇은 바(초상화를 가리지 않는다). CanvasGroup 으로 페이드.
            var frame = NewUI("SelectedFrame", content.transform);
            Stretch(frame);
            var frameGroup = frame.AddComponent<CanvasGroup>();
            frameGroup.alpha = 0f;
            const float th = 5f;
            AddBorderEdge(frame.transform, Accent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, th)); // 상
            AddBorderEdge(frame.transform, Accent, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, th)); // 하
            AddBorderEdge(frame.transform, Accent, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f), new Vector2(th, 0)); // 좌
            AddBorderEdge(frame.transform, Accent, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f), new Vector2(th, 0)); // 우
            frame.SetActive(false);

            var nameBar = NewUI("NameBar", content.transform);
            AnchorBottomStretch(nameBar, 44);
            AddImage(nameBar, new Color(0f, 0f, 0f, 0.6f)).raycastTarget = false;
            var nameText = AddText(NewUI("Name", nameBar.transform), "이름", 22, TextMain, TextAlignmentOptions.Center);
            Stretch(nameText.gameObject);
            nameText.raycastTarget = false;

            // 잠금(비활성) 오버레이 — 잠긴 캐릭터는 흐리게 + 자물쇠 표시. 기본 비활성.
            var lockedGo = NewUI("LockedOverlay", content.transform);
            Stretch(lockedGo);
            AddImage(lockedGo, new Color(0f, 0f, 0f, 0.62f), UISprite, sliced: true).raycastTarget = false;
            var lockLabel = AddText(NewUI("LockLabel", lockedGo.transform), "LOCKED", 22, new Color(0.85f, 0.86f, 0.9f, 1f), TextAlignmentOptions.Center);
            Stretch(lockLabel.gameObject);
            lockLabel.raycastTarget = false;
            lockedGo.SetActive(false);

            var so = new SerializedObject(card);
            SetRef(so, "_button", btn);
            SetRef(so, "_content", Rt(content));
            SetRef(so, "_canvasGroup", cg);
            SetRef(so, "_portrait", portraitImg);
            SetRef(so, "_selectedFrame", frameGroup);
            SetRef(so, "_nameText", nameText);
            SetRef(so, "_lockedOverlay", lockedGo);
            so.ApplyModifiedPropertiesWithoutUndo();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved.GetComponent<UICharacterSelectCard>();
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 메인 프리팹

        private static GameObject BuildMainPrefab(UICharacterSelectCard cardPrefab)
        {
            var root = NewUI("UI_Scene_CharacterSelect", null);

            // UI_Base 는 Canvas 를 요구한다. 이 프리팹은 UIManager 가 Canvas_Scene 자식으로 인스턴스화된다.
            // 중첩 Canvas 는 독립된 레이캐스트 경계를 만들기 때문에, 부모(Canvas_Scene) GraphicRaycaster 는
            // 이 캔버스 하위 그래픽을 히트하지 못한다. → 자체 GraphicRaycaster 가 반드시 필요하다.
            // (동일 패턴: UI_Scene_TitleMenu / UI_Scene_PartyMenu 도 각자 GraphicRaycaster 를 가진다.)
            //
            // 렌더모드: 프리팹을 root 로 저장할 때 ScreenSpaceOverlay/Camera 캔버스는 root RectTransform 을
            // driven 하여 스케일/anchor 가 0 으로 붕괴한다. WorldSpace 는 driven 하지 않아 stretch 가 보존되고,
            // 자식 캔버스가 되면 renderMode 값 자체는 무시(부모 Overlay 상속)되므로 클릭도 정상 동작한다.
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            root.AddComponent<GraphicRaycaster>();

            var ui = root.AddComponent<UI_Scene_CharacterSelect>();

            // Canvas 부착 후 root 를 명시적으로 전체 화면 stretch + 스케일 1 로 고정한다.
            Stretch(root);
            root.transform.localScale = Vector3.one;

            // 배경 딤
            var dim = NewUI("Dim", root.transform);
            Stretch(dim);
            AddImage(dim, Dim);

            // 타이틀
            var title = NewUI("Title", root.transform);
            var trt = Rt(title);
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(900, 72);
            trt.anchoredPosition = new Vector2(0, -40);
            AddText(title, "주인공 선택", 48, TextMain, TextAlignmentOptions.Center);

            // 좌측 대형 프리뷰 영역 (하단 카드 영역과 겹치지 않도록 바닥에서 띄운다)
            var preview = NewUI("PreviewArea", root.transform);
            var prt = Rt(preview);
            prt.anchorMin = prt.anchorMax = new Vector2(0f, 0.5f);
            prt.pivot = new Vector2(0f, 0.5f);
            prt.sizeDelta = new Vector2(720, 820);
            prt.anchoredPosition = new Vector2(60, 120);
            AddImage(preview, PreviewBg, UISprite, sliced: true);

            var portraitLarge = NewUI("PortraitLarge", preview.transform);
            StretchInset(portraitLarge, 16, 16, 16, 16);
            var portraitLargeImg = AddImage(portraitLarge, Color.white);
            portraitLargeImg.preserveAspect = true;
            portraitLargeImg.raycastTarget = false;
            portraitLargeImg.enabled = false;

            var previewRaw = NewUI("CharacterPreview", preview.transform);
            StretchInset(previewRaw, 16, 16, 16, 16);
            var previewRawImg = previewRaw.AddComponent<RawImage>();
            previewRawImg.raycastTarget = false;
            previewRawImg.enabled = false;

            // 우측 상세 패널
            var detail = NewUI("DetailPanel", root.transform);
            var drt = Rt(detail);
            drt.anchorMin = drt.anchorMax = new Vector2(1f, 0.5f);
            drt.pivot = new Vector2(1f, 0.5f);
            drt.sizeDelta = new Vector2(560, 860);
            drt.anchoredPosition = new Vector2(-40, 140);
            var detailGroup = detail.AddComponent<CanvasGroup>();
            detailGroup.alpha = 0f;
            detailGroup.interactable = false;
            detailGroup.blocksRaycasts = false;
            AddImage(detail, PanelBg, UISprite, sliced: true);

            var detailContent = NewUI("Content", detail.transform);
            StretchInset(detailContent, 24, 24, 24, 24);
            AddVLG(detailContent, spacing: 12, pad: 0).childForceExpandHeight = false;

            var nameText = AddText(NewUI("Name", detailContent.transform), "이름", 36, TextMain, TextAlignmentOptions.Left);
            SetHeight(nameText.gameObject, 48);
            var taglineText = AddText(NewUI("Tagline", detailContent.transform), "한 줄 소개", 20, TextSub, TextAlignmentOptions.Left);
            SetHeight(taglineText.gameObject, 30);
            var elementText = AddText(NewUI("Element", detailContent.transform), "속성: 불", 20, Accent, TextAlignmentOptions.Left);
            SetHeight(elementText.gameObject, 30);

            AddDivider(detailContent.transform);

            AddText(NewUI("PassivesHeader", detailContent.transform), "대표 패시브", 22, Accent, TextAlignmentOptions.Left);
            var passiveRows = new UIPassiveAbilityRow[PassiveRowCount];
            for (int i = 0; i < PassiveRowCount; i++)
                passiveRows[i] = BuildPassiveRow(detailContent.transform, i);
            var passiveEmpty = NewUI("PassiveEmpty", detailContent.transform);
            SetHeight(passiveEmpty, 42);
            AddText(passiveEmpty, "대표 패시브 정보 없음", 17, TextSub, TextAlignmentOptions.Left);

            // 하단 버튼 — 카드 스트립 위쪽(우측)에 배치해 카드와 겹치지 않게 한다.
            var cancelBtn = MakeButton("CancelButton", root.transform, "취소", out _, BtnBg);
            var crt = Rt(cancelBtn.gameObject);
            crt.anchorMin = crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(1f, 0f);
            crt.sizeDelta = new Vector2(210, 62);
            crt.anchoredPosition = new Vector2(-270, 350);

            var confirmBtn = MakeButton("ConfirmButton", root.transform, "시작", out _, Accent);
            var cfrt = Rt(confirmBtn.gameObject);
            cfrt.anchorMin = cfrt.anchorMax = new Vector2(1f, 0f);
            cfrt.pivot = new Vector2(1f, 0f);
            cfrt.sizeDelta = new Vector2(210, 62);
            cfrt.anchoredPosition = new Vector2(-40, 350);

            confirmBtn.interactable = false;

            // 카드 행 (하단 중앙, 가로 배치 + 콘텐츠 크기 맞춤)
            var cardRow = NewUI("CardRow", root.transform);
            var crrt = Rt(cardRow);
            crrt.anchorMin = crrt.anchorMax = new Vector2(0.5f, 0f);
            crrt.pivot = new Vector2(0.5f, 0f);
            crrt.sizeDelta = new Vector2(0, 260);
            crrt.anchoredPosition = new Vector2(0, 20);
            var cardLayout = AddHLG(cardRow, spacing: 16, pad: 6);
            cardLayout.childControlHeight = true;
            cardLayout.childForceExpandHeight = false;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childAlignment = TextAnchor.MiddleCenter;
            var fitter = cardRow.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            // 필드 배선
            var so = new SerializedObject(ui);
            var characterDb = AssetDatabase.LoadAssetAtPath<CharacterSelectDatabaseSO>(CharacterDbPath);
            if (characterDb != null) SetRef(so, "_database", characterDb);
            else Debug.LogWarning($"[CharacterSelectBuilder] 캐릭터 목록 SO를 찾지 못해 _database 는 비웁니다: {CharacterDbPath}");

            var memberData = AssetDatabase.LoadAssetAtPath<PartyMemberDataSO>(MemberDataPath);
            if (memberData != null) SetRef(so, "_memberData", memberData);
            else Debug.LogWarning($"[CharacterSelectBuilder] PartyMemberData 를 찾지 못해 _memberData 는 비웁니다: {MemberDataPath}");

            var passiveDb = AssetDatabase.LoadAssetAtPath<CharacterPassiveDatabaseSO>(PassiveDbPath);
            if (passiveDb != null) SetRef(so, "_passiveDatabase", passiveDb);
            else Debug.LogWarning($"[CharacterSelectBuilder] 패시브 DB를 찾지 못해 _passiveDatabase 는 비웁니다: {PassiveDbPath}");

            SetRef(so, "_cardPrefab", cardPrefab);
            SetRef(so, "_cardRoot", cardRow.transform);
            SetRef(so, "_portraitLarge", portraitLargeImg);
            SetRef(so, "_characterPreview", previewRawImg);
            SetRef(so, "_detailGroup", detailGroup);
            SetRef(so, "_detailPanel", drt);
            SetRef(so, "_detailNameText", nameText);
            SetRef(so, "_detailTaglineText", taglineText);
            SetRef(so, "_elementText", elementText);
            SetArray(so, "_passiveRows", passiveRows);
            SetRef(so, "_passiveEmptyRoot", passiveEmpty);
            SetRef(so, "_confirmButton", confirmBtn);
            SetRef(so, "_cancelButton", cancelBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            var saved = PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return saved;
        }

        private static UIPassiveAbilityRow BuildPassiveRow(Transform parent, int index)
        {
            var root = NewUI($"PassiveRow{index + 1}", parent);
            SetHeight(root, 92);
            AddImage(root, RowBg, UISprite, sliced: true);
            AddHLG(root, spacing: 10, pad: 8).childAlignment = TextAnchor.MiddleLeft;

            var iconGo = NewUI("Icon", root.transform);
            SetWidth(iconGo, 52);
            var icon = AddImage(iconGo, Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;

            var texts = NewUI("Texts", root.transform);
            AddFlexibleW(texts, 1f);
            AddVLG(texts, spacing: 2, pad: 0);
            var title = AddText(NewUI("Title", texts.transform), "패시브", 19, TextMain, TextAlignmentOptions.Left);
            var desc = AddText(NewUI("Description", texts.transform), "패시브 설명", 15, TextSub, TextAlignmentOptions.TopLeft);
            var trigger = AddText(NewUI("Trigger", texts.transform), "상시", 14, Accent, TextAlignmentOptions.Left);

            var row = root.AddComponent<UIPassiveAbilityRow>();
            var so = new SerializedObject(row);
            SetRef(so, "_icon", icon);
            SetRef(so, "_title", title);
            SetRef(so, "_description", desc);
            SetRef(so, "_trigger", trigger);
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return row;
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

        private static void AnchorBottomStretch(GameObject go, float height)
        {
            var rt = Rt(go);
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0, height);
            rt.anchoredPosition = Vector2.zero;
        }

        private static Image AddImage(GameObject go, Color color, Sprite sprite = null, bool sliced = false)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            if (sprite != null) { img.sprite = sprite; img.type = sliced ? Image.Type.Sliced : Image.Type.Simple; }
            return img;
        }

        // 선택 테두리의 한 변(얇은 바)을 만든다.
        private static void AddBorderEdge(Transform parent, Color color, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size)
        {
            var e = NewUI("Edge", parent);
            var rt = Rt(e);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
            AddImage(e, color).raycastTarget = false;
        }

        private static void AddDivider(Transform parent)
        {
            var div = NewUI("Divider", parent);
            SetHeight(div, 2);
            AddImage(div, new Color(1f, 1f, 1f, 0.12f)).raycastTarget = false;
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
            labelText = AddText(lblGo, label, 24, TextMain, TextAlignmentOptions.Center);
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

        private static void SetWidth(GameObject go, float w)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minWidth = le.preferredWidth = w; le.flexibleWidth = 0;
        }

        private static void AddFlexibleW(GameObject go, float flexW)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleWidth = flexW;
        }

        private static void SetRef(SerializedObject so, string propName, UnityEngine.Object value)
        {
            var p = so.FindProperty(propName);
            if (p == null) { Debug.LogWarning($"[CharacterSelectBuilder] 프로퍼티 없음: {propName}"); return; }
            p.objectReferenceValue = value;
        }

        private static void SetArray(SerializedObject so, string propName, UnityEngine.Object[] values)
        {
            var p = so.FindProperty(propName);
            if (p == null) { Debug.LogWarning($"[CharacterSelectBuilder] 배열 프로퍼티 없음: {propName}"); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        #endregion
    }
}
