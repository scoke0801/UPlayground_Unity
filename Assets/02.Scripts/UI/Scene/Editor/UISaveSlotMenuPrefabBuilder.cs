using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.SaveMenu.EditorTools
{
    /// <summary>
    /// 세이브 슬롯 선택(UI_SaveSlotMenu) 프리팹 초안을 코드로 생성하고 SerializeField를 자동 연결하는 에디터 툴.
    ///
    /// - 기존 UI_SaveMenu.prefab의 루트/스크립트(guid)는 유지한 채 자식 계층만 재구성(덮어쓰기).
    /// - 재실행 가능(idempotent). "동작하는 회색 초안"이 목표이며 아이콘/스프라이트/폰트는 Unity에서 다듬는다.
    /// - 스크롤 슬롯 목록(번호 + 썸네일 + 상태/일시/맵/진행도 + 버전 + 저장·삭제 버튼) + 상단 X·하단 닫기를 배선한다.
    /// - 썸네일은 런타임 저장 캡처가 있으면 교체되고, 없으면 회색 플레이스홀더로 표시된다.
    /// </summary>
    public static class UISaveSlotMenuPrefabBuilder
    {
        private const string MainPrefabPath = "Assets/03.Prefabs/UI/Scene/UI_SaveMenu.prefab";

        private static readonly Color Dim        = new Color(0f, 0f, 0f, 0.65f);
        private static readonly Color PanelBg    = new Color(0.05f, 0.07f, 0.10f, 0.98f);
        private static readonly Color SlotBg     = new Color(0.08f, 0.11f, 0.15f, 1f);
        private static readonly Color ThumbBg    = new Color(0.03f, 0.04f, 0.06f, 1f);
        private static readonly Color SaveBg     = new Color(0.10f, 0.28f, 0.34f, 1f);
        private static readonly Color DeleteBg   = new Color(0.28f, 0.09f, 0.11f, 1f);
        private static readonly Color CloseBg    = new Color(0.10f, 0.13f, 0.17f, 1f);
        private static readonly Color TextMain   = new Color(0.90f, 0.92f, 0.95f, 1f);
        private static readonly Color TextSub    = new Color(0.62f, 0.68f, 0.74f, 1f);
        private static readonly Color TextFaint  = new Color(0.45f, 0.50f, 0.56f, 1f);
        private static readonly Color Accent     = new Color(0.45f, 0.85f, 0.55f, 1f);   // "세이브 있음" 초록
        private static readonly Color Danger     = new Color(0.90f, 0.40f, 0.40f, 1f);
        private static readonly Color IconTint   = new Color(0.80f, 0.85f, 0.90f, 1f);
        private static readonly Color NumberTint = new Color(0.80f, 0.75f, 0.60f, 1f);   // 골드빛 슬롯 번호

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        [MenuItem("UPlayGround/UI/세이브 슬롯 메뉴 프리팹 빌드 (초안)")]
        public static void Build()
        {
            if (!System.IO.File.Exists(MainPrefabPath))
            {
                EditorUtility.DisplayDialog("세이브 슬롯 메뉴 빌더",
                    $"대상 프리팹을 찾을 수 없습니다:\n{MainPrefabPath}", "확인");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(MainPrefabPath);
            try
            {
                var menu = root.GetComponent<UI_SaveSlotMenu>();
                if (menu == null)
                {
                    Debug.LogError("[SaveMenuBuilder] 루트에 UI_SaveSlotMenu 컴포넌트가 없습니다. 중단.");
                    return;
                }

                ClearChildren(root.transform);

                // 반투명 배경
                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                AddImage(dim, Dim);

                // 중앙 패널
                var panel = NewUI("Panel", root.transform);
                Center(Rt(panel), 1500, 860);
                AddImage(panel, PanelBg, UISprite, sliced: true);
                AddVLG(panel, spacing: 16, pad: 32).childForceExpandHeight = false;

                // ── 헤더 (제목 + 상단 X) ──
                var header = NewUI("Header", panel.transform);
                SetHeight(header, 64);
                AddHLG(header, spacing: 12, pad: 0).childAlignment = TextAnchor.MiddleCenter;

                var title = AddText(NewUI("Title", header.transform), "저장할 슬롯 선택", 40, TextMain, TextAlignmentOptions.Center);
                AddFlexibleW(title.gameObject, 1f);

                var closeX = MakeButton("CloseButton", header.transform, "X", CloseBg, TextMain, width: 64, fontSize: 30);

                // ── 슬롯 리스트 (ScrollRect) ──
                var scroll = NewUI("SlotScroll", panel.transform);
                AddFlexibleH(scroll, 1f);
                AddImage(scroll, new Color(0f, 0f, 0f, 0f));
                var scrollRect = scroll.AddComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.scrollSensitivity = 42f;

                var viewport = NewUI("Viewport", scroll.transform);
                Stretch(viewport);
                AddImage(viewport, new Color(0f, 0f, 0f, 0.01f));
                var mask = viewport.AddComponent<Mask>();
                mask.showMaskGraphic = false;

                var content = NewUI("Content", viewport.transform);
                var contentRt = Rt(content);
                contentRt.anchorMin = new Vector2(0f, 1f);
                contentRt.anchorMax = new Vector2(1f, 1f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.offsetMin = new Vector2(0f, 0f);
                contentRt.offsetMax = new Vector2(0f, 0f);
                var contentLayout = AddVLG(content, spacing: 16, pad: 0);
                contentLayout.childForceExpandHeight = false;
                var fitter = content.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                scrollRect.viewport = Rt(viewport);
                scrollRect.content = contentRt;

                var rowTemplate = MakeSlotRow(content.transform, 0);
                rowTemplate.root.name = "SlotTemplate";

                // ── 푸터 (닫기 + Esc) ──
                var footer = NewUI("Footer", panel.transform);
                SetHeight(footer, 60);
                AddHLG(footer, spacing: 10, pad: 0).childAlignment = TextAnchor.MiddleRight;

                var spacer = NewUI("Spacer", footer.transform);
                AddFlexibleW(spacer, 1f);
                var closeAlt = MakeButton("CloseButtonAlt", footer.transform, "닫기", CloseBg, TextMain, width: 180, fontSize: 26);
                var esc = AddText(NewUI("EscHint", footer.transform), "Esc", 22, TextFaint, TextAlignmentOptions.Center);
                SetWidth(esc.gameObject, 70);

                // ── 필드 연결 ──
                var so = new SerializedObject(menu);
                SetRef(so, "_titleText",      title);
                SetRef(so, "_closeButton",    closeX);
                SetRef(so, "_closeButtonAlt", closeAlt);
                SetRef(so, "_slotRoot",       content.transform);

                // 지역 표시용 MapConfigDatabaseSO 자동 연결(프로젝트에 하나만 있다고 가정).
                var mapDbGuids = AssetDatabase.FindAssets("t:MapConfigDatabaseSO");
                if (mapDbGuids.Length > 0)
                {
                    var mapDb = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.UI.MapConfigDatabaseSO>(
                        AssetDatabase.GUIDToAssetPath(mapDbGuids[0]));
                    SetRef(so, "_mapConfigDB", mapDb);
                }

                var templateProp = so.FindProperty("_slotTemplate");
                if (templateProp == null)
                {
                    Debug.LogError("[SaveMenuBuilder] '_slotTemplate' 프로퍼티를 찾을 수 없습니다. 중단.");
                    return;
                }
                BindSlotRefs(templateProp, rowTemplate);

                var slotsProp = so.FindProperty("_slots");
                if (slotsProp != null)
                {
                    slotsProp.arraySize = 1;
                    BindSlotRefs(slotsProp.GetArrayElementAtIndex(0), rowTemplate);
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
                Debug.Log("[SaveMenuBuilder] UI_SaveMenu 프리팹 초안 생성 완료.");
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
        #region 슬롯 행 빌드

        private struct SlotRefs
        {
            public GameObject root;
            public Button selectButton;
            public TextMeshProUGUI selectLabel;
            public Button deleteButton;
            public Image thumbnail;
            public TextMeshProUGUI statusText;
            public TextMeshProUGUI dateText;
            public TextMeshProUGUI mapText;
            public TextMeshProUGUI progressText;
        }

        private static SlotRefs MakeSlotRow(Transform parent, int index)
        {
            var refs = new SlotRefs();

            var row = NewUI($"Slot{index + 1}", parent);
            refs.root = row;
            SetHeight(row, 200);
            AddImage(row, SlotBg, UISprite, sliced: true);
            AddHLG(row, spacing: 24, pad: 20).childAlignment = TextAnchor.MiddleLeft;

            // 번호 컬럼
            var numCol = NewUI("NumberCol", row.transform);
            SetWidth(numCol, 120);
            AddVLG(numCol, spacing: 2, pad: 0).childAlignment = TextAnchor.MiddleCenter;
            AddText(NewUI("Number", numCol.transform), (index + 1).ToString(), 64, NumberTint, TextAlignmentOptions.Center);
            AddText(NewUI("SlotLabel", numCol.transform), $"슬롯 {index + 1}", 20, TextSub, TextAlignmentOptions.Center);

            // 썸네일 (플레이스홀더)
            var thumbGo = NewUI("Thumbnail", row.transform);
            SetWidth(thumbGo, 300);
            refs.thumbnail = AddImage(thumbGo, ThumbBg, UISprite, sliced: true);

            // 정보 컬럼 (상태/일시/맵/진행도)
            var infoCol = NewUI("InfoCol", row.transform);
            AddFlexibleW(infoCol, 1f);
            AddVLG(infoCol, spacing: 8, pad: 0).childAlignment = TextAnchor.MiddleLeft;
            refs.statusText   = AddText(NewUI("StatusText", infoCol.transform),   "세이브 있음",           26, Accent,  TextAlignmentOptions.Left);
            refs.dateText     = AddText(NewUI("DateText", infoCol.transform),     "2026-01-01 00:00:00", 22, TextSub, TextAlignmentOptions.Left);
            refs.mapText      = AddText(NewUI("MapText", infoCol.transform),      "맵: -",               22, TextSub, TextAlignmentOptions.Left);
            refs.progressText = AddText(NewUI("ProgressText", infoCol.transform), "진행도: 0",            22, TextSub, TextAlignmentOptions.Left);

            // 버튼 컬럼 (저장 + 삭제)
            var btnCol = NewUI("ButtonCol", row.transform);
            SetWidth(btnCol, 240);
            AddVLG(btnCol, spacing: 10, pad: 0).childAlignment = TextAnchor.MiddleCenter;

            refs.selectButton = MakeButton("SaveButton", btnCol.transform, "저장", SaveBg, TextMain, width: 0, fontSize: 26, out refs.selectLabel);
            SetHeight(refs.selectButton.gameObject, 60);
            AddFlexibleW(refs.selectButton.gameObject, 1f);

            refs.deleteButton = MakeButton("DeleteButton", btnCol.transform, "삭제", DeleteBg, Danger, width: 0, fontSize: 26);
            SetHeight(refs.deleteButton.gameObject, 60);
            AddFlexibleW(refs.deleteButton.gameObject, 1f);

            return refs;
        }

        #endregion

        // ──────────────────────────────────────────────────────────
        #region 빌드 헬퍼

        private static Button MakeButton(string name, Transform parent, string label, Color bg, Color labelColor,
                                         float width, float fontSize)
        {
            return MakeButton(name, parent, label, bg, labelColor, width, fontSize, out _);
        }

        private static Button MakeButton(string name, Transform parent, string label, Color bg, Color labelColor,
                                         float width, float fontSize, out TextMeshProUGUI outLabel)
        {
            var go = NewUI(name, parent);
            if (width > 0) SetWidth(go, width);
            var img = AddImage(go, bg, UISprite, sliced: true);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            outLabel = AddText(NewUI("Label", go.transform), label, fontSize, labelColor, TextAlignmentOptions.Center);
            outLabel.raycastTarget = false;
            Stretch(outLabel.gameObject);
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

        private static void AddFlexibleH(GameObject go, float flexH)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.flexibleHeight = flexH;
        }

        private static void SetRef(SerializedObject so, string propName, UnityEngine.Object value)
        {
            var p = so.FindProperty(propName);
            if (p == null)
            {
                Debug.LogWarning($"[SaveMenuBuilder] 직렬화 프로퍼티를 찾을 수 없음: {propName}");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static void SetRel(SerializedProperty element, string relName, UnityEngine.Object value)
        {
            var p = element.FindPropertyRelative(relName);
            if (p == null)
            {
                Debug.LogWarning($"[SaveMenuBuilder] 슬롯 상대 프로퍼티를 찾을 수 없음: {relName}");
                return;
            }
            p.objectReferenceValue = value;
        }

        private static void BindSlotRefs(SerializedProperty element, SlotRefs refs)
        {
            SetRel(element, "root",         refs.root);
            SetRel(element, "selectButton", refs.selectButton);
            SetRel(element, "selectLabel",  refs.selectLabel);
            SetRel(element, "deleteButton", refs.deleteButton);
            SetRel(element, "thumbnail",    refs.thumbnail);
            SetRel(element, "statusText",   refs.statusText);
            SetRel(element, "dateText",     refs.dateText);
            SetRel(element, "mapText",      refs.mapText);
            SetRel(element, "progressText", refs.progressText);
            SetRel(element, "infoText",     null);
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        #endregion
    }
}
