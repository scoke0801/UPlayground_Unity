using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.UI.DevCheat;

namespace UPlayGround.UI.DevCheat.EditorTools
{
    /// <summary>
    /// 개발 치트 패널(UI_DevCheatPanel) 프리팹 초안을 코드로 생성/재구성하고 SerializeField를 연결하는
    /// 에디터 툴("UI Builder").
    ///
    /// - 대상 프리팹이 없으면 새로 생성(Canvas/CanvasGroup/GraphicRaycaster/UI_DevCheatPanel 부착).
    ///   있으면 자식 계층만 재구성(멱등).
    /// - 골격(헤더/좌측 탭 레일/탭 콘텐츠 컨테이너 N개/하단 로그 바)만 구성한다. 개수는 TabLabels 길이(현재 10개)를 따른다.
    ///   각 탭의 실제 콘텐츠는 UI_DevCheatPanel이 런타임에 코드로 채운다.
    /// - 완료 후 UIPrefabDatabase에 "DevCheatPanel" 키로 자동 등록한다.
    /// </summary>
    public static class UIDevCheatPanelPrefabBuilder
    {
        private const string MainPrefabPath = "Assets/03.Prefabs/UI/UI_DevCheatPanel.prefab";
        private const string DbKey = "DevCheatPanel";

        private static readonly string[] TabLabels =
        {
            "기즈모", "아이템", "퀘스트", "플레이어 스텟",
            "파티원", "시간", "전투", "버프 / 디버프", "도감", "스폰",
        };

        private static readonly Color Dim       = new(0f, 0f, 0f, 0.55f);
        private static readonly Color WindowBg  = new(0.07f, 0.09f, 0.12f, 0.98f);
        private static readonly Color PanelBg   = new(0.10f, 0.13f, 0.17f, 1f);
        private static readonly Color RailBg    = new(0.08f, 0.10f, 0.14f, 1f);
        private static readonly Color BtnBg     = new(0.20f, 0.28f, 0.34f, 1f);
        private static readonly Color TextMain  = new(0.90f, 0.92f, 0.95f, 1f);
        private static readonly Color TextSub   = new(0.62f, 0.68f, 0.74f, 1f);
        private static readonly Color Accent    = new(0.35f, 0.80f, 0.90f, 1f);

        public static void Build()
        {
            EnsurePrefabExists();

            var root = PrefabUtility.LoadPrefabContents(MainPrefabPath);
            try
            {
                var panel = root.GetComponent<UI_DevCheatPanel>();
                if (panel == null)
                {
                    Debug.LogError("[DevCheatBuilder] 루트에 UI_DevCheatPanel 컴포넌트가 없습니다. 중단.");
                    return;
                }

                var rootRt = root.GetComponent<RectTransform>();
                Stretch(rootRt);
                // 루트 ScreenSpaceOverlay Canvas는 저장 시 구동 스케일 0으로 직렬화될 수 있어 명시적으로 1로 고정.
                rootRt.localScale = Vector3.one;

                // ScreenSpaceOverlay + overrideSorting(최상위)로 어떤 부모/중첩 상황에서도
                // 화면 최상단 오버레이로 확실히 렌더되게 한다. (WorldSpace로 두면 월드 공간에
                // 초소형으로 렌더되어 화면에 보이지 않는다.)
                // 다른 팝업(UI_RespawnPopup 등)과 동일하게 부모 레이어 Canvas(Canvas_System, order 3000)의
                // 렌더링/정렬을 그대로 상속시킨다. overrideSorting을 쓰면 오히려 정렬이 어긋날 수 있으므로 끈다.
                var rootCanvas = root.GetComponent<Canvas>();
                if (rootCanvas != null)
                {
                    rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    rootCanvas.overrideSorting = false;
                    rootCanvas.sortingOrder = 0;
                }

                ClearChildren(root.transform);

                // ── Dim ──
                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                AddImage(dim, Dim);

                // ── Window ──
                var window = NewUI("Window", root.transform);
                StretchInset(window, 40, 30, 40, 30);
                AddImage(window, WindowBg);
                var wv = AddVLG(window, 8, 16);
                wv.childForceExpandHeight = false;

                // ── Header ──
                var header = NewUI("Header", window.transform);
                SetHeight(header, 52);
                AddText(header, "DEVELOPMENT CHEAT PANEL  |  UPLAYGROUND", 26, Accent, TextAlignmentOptions.Center, stretch: true);
                var closeBtn = MakeButton("BtnClose", header.transform, "X", 24, new Color(0.3f, 0.12f, 0.14f, 1f));
                AnchorTopRight(closeBtn.GetComponent<RectTransform>(), 44, 44);

                // ── Body ──
                var body = NewUI("Body", window.transform);
                AddFlexibleH(body, 1);
                AddHLG(body, 12, 0);

                // 좌측 탭 레일
                var rail = NewUI("TabRail", body.transform);
                SetWidth(rail, 260);
                AddImage(rail, RailBg);
                var railV = AddVLG(rail, 6, 10);
                railV.childForceExpandHeight = false;

                var tabButtons = new Button[TabLabels.Length];
                for (int i = 0; i < TabLabels.Length; i++)
                {
                    tabButtons[i] = MakeButton($"Tab{i}", rail.transform, TabLabels[i], 18, PanelBg);
                    SetHeight((RectTransform)tabButtons[i].transform, 54);
                }

                // 탭 미리보기 박스
                var previewBox = NewUI("PreviewBox", rail.transform);
                AddFlexibleH(previewBox, 1);
                AddImage(previewBox, new Color(0.06f, 0.08f, 0.11f, 0.8f));
                var previewText = AddText(previewBox, "탭 미리보기", 15, TextSub, TextAlignmentOptions.TopLeft, stretch: true);
                previewText.margin = new Vector4(10, 10, 10, 10);

                // 탭 호스트(콘텐츠 컨테이너 스택)
                var host = NewUI("TabHost", body.transform);
                AddFlexibleW(host, 1);

                var tabPanels = new RectTransform[TabLabels.Length];
                for (int i = 0; i < TabLabels.Length; i++)
                {
                    var p = NewUI($"TabPanel_{i}", host.transform);
                    Stretch(p);
                    p.gameObject.SetActive(i == 0);
                    tabPanels[i] = p;
                }

                // ── 하단 로그 바 ──
                var logBar = NewUI("LogBar", window.transform);
                SetHeight(logBar, 160);
                AddImage(logBar, new Color(0.06f, 0.08f, 0.11f, 0.9f));
                AddHLG(logBar, 10, 10);

                var logArea = NewUI("LogArea", logBar.transform);
                AddFlexibleW(logArea, 1);
                logArea.gameObject.AddComponent<RectMask2D>();
                var logText = AddText(logArea, "최근 실행 로그", 15, TextMain, TextAlignmentOptions.TopLeft, stretch: true);

                var clearBtn = MakeButton("BtnClearLog", logBar.transform, "로그 지우기", 16, BtnBg);
                SetWidth((RectTransform)clearBtn.transform, 150);

                // ── 필드 연결 ──
                var so = new SerializedObject(panel);
                SetRef(so, "_closeButton", closeBtn);
                SetRef(so, "_tabPreviewText", previewText);
                SetRef(so, "_logText", logText);
                SetRef(so, "_clearLogButton", clearBtn);
                SetRefArray(so, "_tabButtons", tabButtons);
                SetRefArray(so, "_tabPanels", tabPanels);
                // UI_Base 필드: System 레이어 + ESC 닫기 허용
                var layerProp = so.FindProperty("_layer");
                if (layerProp != null) layerProp.intValue = (int)CanvasLayer.System;
                var escProp = so.FindProperty("_canCloseWithEsc");
                if (escProp != null) escProp.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
                Debug.Log("[DevCheatBuilder] UI_DevCheatPanel 프리팹 초안 생성 완료.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            RegisterInDatabase();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
        }

        // ── 프리팹 생성(없을 때) ─────────────────────────────────────
        private static void EnsurePrefabExists()
        {
            if (File.Exists(MainPrefabPath))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(MainPrefabPath));

            var go = new GameObject("UI_DevCheatPanel", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;   // 부모 레이어 Canvas 상속(다른 팝업과 동일)
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<CanvasGroup>();
            go.AddComponent<UI_DevCheatPanel>();

            PrefabUtility.SaveAsPrefabAsset(go, MainPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log($"[DevCheatBuilder] 새 프리팹 생성: {MainPrefabPath}");
        }

        // ── UIPrefabDatabase 등록 ────────────────────────────────────
        private static void RegisterInDatabase()
        {
            string[] guids = AssetDatabase.FindAssets("t:UIPrefabDatabase");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[DevCheatBuilder] UIPrefabDatabase 에셋을 찾지 못했습니다. 수동 등록이 필요합니다.");
                return;
            }

            string dbPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var db = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(dbPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainPrefabPath);
            if (db == null || prefab == null)
                return;

            if (db.HasKey(DbKey))
            {
                Debug.Log($"[DevCheatBuilder] '{DbKey}' 키가 이미 등록되어 있어 스킵합니다.");
                return;
            }

            db.AddPrefab(DbKey, prefab, CanvasLayer.System, "개발 치트 패널 (F11)");
            EditorUtility.SetDirty(db);
            Debug.Log($"[DevCheatBuilder] UIPrefabDatabase에 '{DbKey}' 등록 완료: {dbPath}");
        }

        // ── 헬퍼 ─────────────────────────────────────────────────────
        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void StretchInset(RectTransform rt, float l, float t, float r, float b)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        private static void AnchorTopRight(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-6, -4);
        }

        private static Image AddImage(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static VerticalLayoutGroup AddVLG(RectTransform rt, float spacing, int pad)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad);
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            v.childControlWidth = true; v.childControlHeight = true;
            return v;
        }

        private static HorizontalLayoutGroup AddHLG(RectTransform rt, float spacing, int pad)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.padding = new RectOffset(pad, pad, pad, pad);
            h.childForceExpandWidth = false; h.childForceExpandHeight = true;
            h.childControlWidth = true; h.childControlHeight = true;
            return h;
        }

        private static void AddFlexibleW(RectTransform rt, float flex)
            => LE(rt).flexibleWidth = flex;

        private static void AddFlexibleH(RectTransform rt, float flex)
            => LE(rt).flexibleHeight = flex;

        private static void SetHeight(RectTransform rt, float h)
        {
            var le = LE(rt);
            le.minHeight = h; le.preferredHeight = h;
        }

        private static void SetWidth(RectTransform rt, float w)
        {
            var le = LE(rt);
            le.minWidth = w; le.preferredWidth = w; le.flexibleWidth = 0;
        }

        private static LayoutElement LE(RectTransform rt)
            => rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();

        private static TextMeshProUGUI AddText(RectTransform parent, string text, int size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool stretch = false)
        {
            var rt = NewUI("Text", parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.raycastTarget = false;
            if (stretch) Stretch(rt);
            return t;
        }

        private static Button MakeButton(string name, Transform parent, string label, int fontSize, Color bg)
        {
            var rt = NewUI(name, parent);
            var img = AddImage(rt, bg);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var t = AddText(rt, label, fontSize, TextMain, TextAlignmentOptions.Center, stretch: true);
            return btn;
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        private static void SetRef(SerializedObject so, string prop, UnityEngine.Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning($"[DevCheatBuilder] 직렬화 필드 '{prop}' 를 찾지 못했습니다.");
        }

        private static void SetRefArray(SerializedObject so, string prop, UnityEngine.Object[] items)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogWarning($"[DevCheatBuilder] 배열 필드 '{prop}' 없음."); return; }
            p.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
