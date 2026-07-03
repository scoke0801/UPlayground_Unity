using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Respawn.EditorTools
{
    /// <summary>
    /// 부활 팝업(UI_RespawnPopup) 프리팹 초안을 코드로 생성하고 SerializeField를 자동 연결하는 에디터 툴.
    ///
    /// - 기존 UI_RespawnPopup.prefab의 루트/스크립트(guid)는 유지한 채 자식 계층만 재구성(덮어쓰기).
    /// - 제자리 부활(붉은색) / 포탈 부활(청록색) 2카드 + 제목/부제 + 하단 경고.
    /// - 아이콘은 회색/색조 플레이스홀더. 재실행 가능(idempotent).
    /// </summary>
    public static class UIRespawnPopupPrefabBuilder
    {
        private const string MainPrefabPath = "Assets/03.Prefabs/UI/UI_RespawnPopup.prefab";

        private static readonly Color Dim      = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color WindowBg = new Color(0.06f, 0.06f, 0.08f, 0.98f);
        private static readonly Color TextMain = new Color(0.92f, 0.92f, 0.95f, 1f);
        private static readonly Color TextSub  = new Color(0.62f, 0.66f, 0.72f, 1f);

        // 붉은(제자리)
        private static readonly Color RedCard = new Color(0.14f, 0.07f, 0.08f, 1f);
        private static readonly Color RedBtn  = new Color(0.35f, 0.12f, 0.14f, 1f);
        private static readonly Color Red     = new Color(0.90f, 0.35f, 0.38f, 1f);
        // 청록(포탈)
        private static readonly Color TealCard = new Color(0.07f, 0.12f, 0.14f, 1f);
        private static readonly Color TealBtn  = new Color(0.12f, 0.30f, 0.34f, 1f);
        private static readonly Color Teal     = new Color(0.40f, 0.82f, 0.88f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        [MenuItem("UPlayGround/UI/부활 팝업 프리팹 빌드 (초안)")]
        public static void Build()
        {
            if (!System.IO.File.Exists(MainPrefabPath))
            {
                EditorUtility.DisplayDialog("부활 팝업 빌더",
                    $"대상 프리팹을 찾을 수 없습니다:\n{MainPrefabPath}", "확인");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(MainPrefabPath);
            try
            {
                var popup = root.GetComponent<UI_RespawnPopup>();
                if (popup == null)
                {
                    Debug.LogError("[RespawnBuilder] 루트에 UI_RespawnPopup 컴포넌트가 없습니다. 중단.");
                    return;
                }

                ClearChildren(root.transform);

                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                AddImage(dim, Dim);

                var window = NewUI("Window", root.transform);
                Center(Rt(window), 1200, 780);
                AddImage(window, WindowBg, UISprite, sliced: true);
                AddVLG(window, spacing: 10, pad: 28).childForceExpandHeight = false;

                // 제목 / 부제 (정적)
                var title = NewUI("Title", window.transform);
                SetHeight(title, 56);
                AddText(title, "부활 선택", 40, TextMain, TextAlignmentOptions.Center);
                var subtitle = NewUI("Subtitle", window.transform);
                SetHeight(subtitle, 30);
                AddText(subtitle, "부활 방식을 선택하세요", 22, TextSub, TextAlignmentOptions.Center);

                // 카드 2장
                var cards = NewUI("Cards", window.transform);
                AddFlexible(cards, 1);
                AddHLG(cards, spacing: 24, pad: 10).childForceExpandWidth = true;

                // ── 제자리 부활 (붉은) ──
                var spotCard = MakeCard(cards.transform, RedCard);
                MakeIcon(spotCard.transform, Red);
                AddCardTitle(spotCard.transform, "제자리 부활");
                AddCardDesc(spotCard.transform, "사망한 위치에서 즉시 부활합니다.\n<color=#E6595F>부활석을 소비합니다.</color>");
                var spotItemCount = AddCardLine(spotCard.transform, "보유 부활석 x0", Red);
                var spotHeal      = AddCardLine(spotCard.transform, "HP 50% 회복", TextMain);
                var spotBtn = MakeButton("SpotReviveButton", spotCard.transform, "부활석 사용", out var spotLabel, RedBtn);
                SetHeight(spotBtn.gameObject, 64);

                // ── 포탈 부활 (청록) ──
                var portalCard = MakeCard(cards.transform, TealCard);
                MakeIcon(portalCard.transform, Teal);
                AddCardTitle(portalCard.transform, "포탈 부활");
                AddCardDesc(portalCard.transform, "가장 가까운 포탈 지점에서\n안전하게 부활합니다.");
                AddCardSpacer(portalCard.transform);
                var portalHeal = AddCardLine(portalCard.transform, "HP 100% 회복", Teal);
                var portalBtn = MakeButton("PortalReviveButton", portalCard.transform, "가까운 포탈에서 부활", out var portalLabel, TealBtn);
                SetHeight(portalBtn.gameObject, 64);

                // 하단 경고
                var warn = NewUI("Warning", window.transform);
                SetHeight(warn, 34);
                var warnText = AddText(warn, "⚠ 전멸 상태입니다. 부활 방식을 선택해야 합니다.", 20, Red, TextAlignmentOptions.Center);

                // ── 필드 연결 ──
                var so = new SerializedObject(popup);
                SetRef(so, "_spotReviveButton",   spotBtn);
                SetRef(so, "_spotReviveLabel",    spotLabel);
                SetRef(so, "_spotItemCountText",  spotItemCount);
                SetRef(so, "_spotHealText",       spotHeal);
                SetRef(so, "_portalReviveButton", portalBtn);
                SetRef(so, "_portalReviveLabel",  portalLabel);
                SetRef(so, "_portalHealText",     portalHeal);
                SetRef(so, "_warningText",        warnText);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
                Debug.Log("[RespawnBuilder] UI_RespawnPopup 프리팹 초안 생성 완료.");
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
        #region 카드 헬퍼

        private static GameObject MakeCard(Transform parent, Color bg)
        {
            var card = NewUI("Card", parent);
            AddImage(card, bg, UISprite, sliced: true);
            AddFlexibleW(card, 1f);
            var v = AddVLG(card, spacing: 12, pad: 24);
            v.childAlignment = TextAnchor.UpperCenter;
            v.childForceExpandHeight = false;
            return card;
        }

        private static void MakeIcon(Transform parent, Color tint)
        {
            var icon = NewUI("Icon", parent);
            SetHeight(icon, 180);
            var img = AddImage(icon, tint, UISprite, sliced: true);
            img.color = new Color(tint.r, tint.g, tint.b, 0.85f);
        }

        private static void AddCardTitle(Transform parent, string text)
        {
            var go = NewUI("CardTitle", parent);
            SetHeight(go, 44);
            AddText(go, text, 32, TextMain, TextAlignmentOptions.Center);
        }

        private static void AddCardDesc(Transform parent, string text)
        {
            var go = NewUI("CardDesc", parent);
            SetHeight(go, 64);
            AddText(go, text, 20, TextSub, TextAlignmentOptions.Center);
        }

        private static TextMeshProUGUI AddCardLine(Transform parent, string text, Color color)
        {
            var go = NewUI("CardLine", parent);
            SetHeight(go, 34);
            return AddText(go, text, 22, color, TextAlignmentOptions.Center);
        }

        private static void AddCardSpacer(Transform parent)
        {
            var go = NewUI("Spacer", parent);
            SetHeight(go, 34);
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
            if (sprite != null) { img.sprite = sprite; img.type = sliced ? Image.Type.Sliced : Image.Type.Simple; }
            return img;
        }

        private static TextMeshProUGUI AddText(GameObject go, string text, float size, Color color, TextAlignmentOptions align)
        {
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.alignment = align;
            t.richText = true;
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
            if (p == null) { Debug.LogWarning($"[RespawnBuilder] 프로퍼티 없음: {propName}"); return; }
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
