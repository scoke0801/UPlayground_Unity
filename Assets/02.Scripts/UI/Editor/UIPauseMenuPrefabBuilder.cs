using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.PauseMenu.EditorTools
{
    /// <summary>
    /// 일시정지 메뉴(UI_Scene_PauseMenu) 프리팹 초안을 코드로 생성하고 SerializeField를 자동 연결하는 에디터 툴.
    ///
    /// - 기존 UI_Scene_PauseMenu.prefab의 루트/스크립트(guid)는 유지한 채 자식 계층만 재구성(덮어쓰기).
    /// - 재실행 가능(idempotent). "동작하는 회색 초안"이 목표이며 아이콘/스프라이트/폰트는 Unity에서 다듬는다.
    /// - 버튼 4종(재개/저장/타이틀/종료) + 플레이 시간 + 상태 문구를 배선한다.
    /// </summary>
    public static class UIPauseMenuPrefabBuilder
    {
        private const string MainPrefabPath = "Assets/03.Prefabs/UI/Scene/UI_Scene_PauseMenu.prefab";

        private static readonly Color Dim      = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color PanelBg  = new Color(0.06f, 0.09f, 0.13f, 0.98f);
        private static readonly Color BtnBg    = new Color(0.10f, 0.13f, 0.17f, 1f);
        private static readonly Color ResumeBg = new Color(0.10f, 0.30f, 0.36f, 1f);
        private static readonly Color DangerBg = new Color(0.30f, 0.10f, 0.12f, 1f);
        private static readonly Color TextMain = new Color(0.90f, 0.92f, 0.95f, 1f);
        private static readonly Color TextSub  = new Color(0.62f, 0.68f, 0.74f, 1f);
        private static readonly Color Accent   = new Color(0.35f, 0.80f, 0.90f, 1f);
        private static readonly Color Danger   = new Color(0.90f, 0.40f, 0.40f, 1f);
        private static readonly Color IconTint = new Color(0.80f, 0.85f, 0.90f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static void Build()
        {
            if (!System.IO.File.Exists(MainPrefabPath))
            {
                EditorUtility.DisplayDialog("일시정지 메뉴 빌더",
                    $"대상 프리팹을 찾을 수 없습니다:\n{MainPrefabPath}", "확인");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(MainPrefabPath);
            try
            {
                var menu = root.GetComponent<UI_Scene_PauseMenu>();
                if (menu == null)
                {
                    Debug.LogError("[PauseBuilder] 루트에 UI_Scene_PauseMenu 컴포넌트가 없습니다. 중단.");
                    return;
                }

                ClearChildren(root.transform);

                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                AddImage(dim, Dim);
                // 팝업 오픈/클로즈 트윈이 알파를 페이드하는 대상. UI_PopupBase._dim에 연결한다.
                var dimGroup = dim.AddComponent<CanvasGroup>();

                // 중앙 패널
                var panel = NewUI("Panel", root.transform);
                Center(Rt(panel), 560, 780);
                AddImage(panel, PanelBg, UISprite, sliced: true);
                AddVLG(panel, spacing: 12, pad: 24).childForceExpandHeight = false;

                // 제목
                var title = NewUI("Title", panel.transform);
                SetHeight(title, 50);
                AddText(title, "메뉴", 34, TextMain, TextAlignmentOptions.Center);

                // 플레이 시간 (아이콘 + 텍스트)
                var timeRow = NewUI("PlayTimeRow", panel.transform);
                SetHeight(timeRow, 30);
                AddHLG(timeRow, spacing: 6, pad: 0).childAlignment = TextAnchor.MiddleCenter;
                var clock = NewUI("ClockIcon", timeRow.transform);
                SetWidth(clock, 24);
                AddImage(clock, IconTint, UISprite, sliced: true);
                var playTime = AddText(NewUI("PlayTime", timeRow.transform), "플레이 시간 00:00:00", 20, TextSub, TextAlignmentOptions.Left);
                SetWidth(playTime.gameObject, 260);

                // 구분 여백
                var gap = NewUI("Gap", panel.transform);
                SetHeight(gap, 8);

                // 버튼 4종
                var resumeBtn = MakeMenuButton("ResumeButton",    panel.transform, "재개",        ResumeBg, TextMain, highlight: true);
                var saveBtn   = MakeMenuButton("SaveButton",      panel.transform, "저장",        BtnBg,    TextMain);
                var titleBtn  = MakeMenuButton("GotoTitleButton", panel.transform, "타이틀로 이동", BtnBg,    TextMain);
                var exitBtn   = MakeMenuButton("ExitButton",      panel.transform, "게임 종료",     DangerBg, Danger);

                // 구분선
                var divider = NewUI("Divider", panel.transform);
                SetHeight(divider, 12);

                // 상태 문구
                var status = NewUI("StatusText", panel.transform);
                SetHeight(status, 28);
                var statusText = AddText(status, "게임이 일시정지되었습니다", 18, TextSub, TextAlignmentOptions.Center);

                UPlayGround.UI.EditorTools.UIInputPromptBarBuilderUtility
                    .AddSubmitCancelBar(panel.transform, "선택", "재개");

                // ── 필드 연결 ──
                var so = new SerializedObject(menu);
                SetRef(so, "resumeButton",    resumeBtn);
                SetRef(so, "saveButton",      saveBtn);
                SetRef(so, "gotoTitleButton", titleBtn);
                SetRef(so, "exitButton",      exitBtn);
                SetRef(so, "playTimeText",    playTime);
                SetRef(so, "pauseStatusText", statusText);
                // UI_PopupBase 트윈 대상: Dim 페이드 + Panel 스케일
                SetRef(so, "_dim",   dimGroup);
                SetRef(so, "_panel", Rt(panel));
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
                Debug.Log("[PauseBuilder] UI_Scene_PauseMenu 프리팹 초안 생성 완료.");
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
        #region 빌드 헬퍼

        /// <summary> 아이콘 + 라벨을 가진 메뉴 버튼. highlight=true면 재개 하이라이트(테두리+좌측 포인터). </summary>
        private static Button MakeMenuButton(string name, Transform parent, string label, Color bg, Color labelColor,
                                             bool highlight = false)
        {
            var go = NewUI(name, parent);
            SetHeight(go, 84);
            var img = AddImage(go, bg, UISprite, sliced: true);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            go.AddComponent<UISelectOnPointerEnter>();

            AddHLG(go, spacing: 16, pad: 20).childAlignment = TextAnchor.MiddleLeft;

            var iconGo = NewUI("Icon", go.transform);
            SetWidth(iconGo, 40);
            AddImage(iconGo, IconTint, UISprite, sliced: true);

            var lbl = AddText(NewUI("Label", go.transform), label, 26, labelColor, TextAlignmentOptions.Left);
            AddFlexibleW(lbl.gameObject, 1f);
            lbl.raycastTarget = false;

            if (highlight)
            {
                // 청록 테두리(오버레이). 좌측 포인터는 사용하지 않는다.
                var outline = NewUI("Highlight", go.transform);
                Stretch(outline);
                var ol = AddImage(outline, new Color(Accent.r, Accent.g, Accent.b, 0.16f), UISprite, sliced: true);
                ol.raycastTarget = false;
                outline.AddComponent<LayoutElement>().ignoreLayout = true;
                outline.transform.SetAsFirstSibling();
            }

            return btn;
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

        private static void Center(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = Vector2.zero;
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

        private static void SetRef(SerializedObject so, string propName, UnityEngine.Object value)
        {
            var p = so.FindProperty(propName);
            if (p == null)
            {
                Debug.LogWarning($"[PauseBuilder] 직렬화 프로퍼티를 찾을 수 없음: {propName}");
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
