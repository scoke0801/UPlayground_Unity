using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Crafting.EditorTools
{
    /// <summary>
    /// 제작 UI(UI_CraftMenu) 프리팹 초안을 코드로 생성하고 모든 SerializeField를 자동 연결하는 에디터 툴.
    ///
    /// - 기존 UI_CraftMenu.prefab의 루트/스크립트(guid)는 유지한 채, 자식 계층만 재구성한다.
    /// - 레시피 슬롯 / 재료 슬롯 서브 프리팹도 함께 생성해 참조를 연결한다.
    /// - 재실행 가능(idempotent): 실행할 때마다 자식을 지우고 동일한 초안으로 다시 만든다.
    /// - 시안 재현이 아니라 "동작하는 회색 초안"이 목표. 색/스프라이트/폰트는 Unity에서 다듬는다.
    /// </summary>
    public static class UICraftMenuPrefabBuilder
    {
        private const string MainPrefabPath       = "Assets/03.Prefabs/UI/Scene/Craft/UI_CraftMenu.prefab";
        private const string RecipeSlotPrefabPath = "Assets/03.Prefabs/UI/Scene/Craft/UI_CraftingRecipeSlot.prefab";
        private const string IngrSlotPrefabPath   = "Assets/03.Prefabs/UI/Scene/Craft/UI_CraftingIngredientSlot.prefab";

        // ──── 색상 팔레트(초안용) ────
        private static readonly Color Dim        = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color WindowBg   = new Color(0.07f, 0.09f, 0.12f, 0.98f);
        private static readonly Color PanelBg    = new Color(0.11f, 0.14f, 0.18f, 1f);
        private static readonly Color SlotBg     = new Color(0.16f, 0.19f, 0.24f, 1f);
        private static readonly Color BtnBg      = new Color(0.20f, 0.28f, 0.34f, 1f);
        private static readonly Color AccentBtn  = new Color(0.18f, 0.45f, 0.55f, 1f);
        private static readonly Color TextMain   = new Color(0.90f, 0.92f, 0.95f, 1f);
        private static readonly Color TextSub    = new Color(0.65f, 0.70f, 0.76f, 1f);
        private static readonly Color Green      = new Color(0.30f, 0.85f, 0.40f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        [MenuItem("UPlayGround/UI/제작 UI 프리팹 빌드 (초안)")]
        public static void Build()
        {
            if (!System.IO.File.Exists(MainPrefabPath))
            {
                EditorUtility.DisplayDialog("제작 UI 빌더",
                    $"대상 프리팹을 찾을 수 없습니다:\n{MainPrefabPath}", "확인");
                return;
            }

            // 1) 서브 프리팹(슬롯) 먼저 생성
            var recipeSlot = BuildRecipeSlotPrefab();
            var ingrSlot   = BuildIngredientSlotPrefab();

            // 2) 메인 프리팹 로드 → 자식 재구성
            var root = PrefabUtility.LoadPrefabContents(MainPrefabPath);
            try
            {
                var menu = root.GetComponent<UI_CraftMenu>();
                if (menu == null)
                {
                    Debug.LogError("[CraftBuilder] 루트에 UI_CraftMenu 컴포넌트가 없습니다. 중단.");
                    return;
                }

                ClearChildren(root.transform);

                // ── 딤 배경 ──
                var dim = NewUI("Dim", root.transform);
                Stretch(dim);
                AddImage(dim, Dim);

                // ── 윈도우 ──
                var window = NewUI("Window", root.transform);
                Center(Rt(window), 1600, 880);
                AddImage(window, WindowBg, UISprite, sliced: true);
                var winV = AddVLG(window, spacing: 8, pad: 16);
                winV.childForceExpandHeight = false;

                // ── 헤더 ──
                var header = NewUI("Header", window.transform);
                SetHeight(header, 64);
                var headerTitle = NewUI("Title", header.transform);
                Stretch(headerTitle);
                AddText(headerTitle, "제작", 34, TextMain, TextAlignmentOptions.Center);
                var btnClose = MakeButton("BtnClose", header.transform, "X", out _);
                AnchorTopRight(Rt(btnClose.gameObject), 48, 48);

                // ── 본문(좌/우 분할) ──
                var body = NewUI("Body", window.transform);
                AddFlexible(body, flexH: 1);
                var bodyH = AddHLG(body, spacing: 12, pad: 0);
                bodyH.childForceExpandHeight = true;

                // ================= 좌측 =================
                var left = NewUI("LeftPanel", body.transform);
                AddImage(left, PanelBg, UISprite, sliced: true);
                AddFlexibleW(left, 2f);
                var leftV = AddVLG(left, spacing: 8, pad: 10);
                leftV.childForceExpandHeight = false;

                // 탭 5개
                var tabs = NewUI("Tabs", left.transform);
                SetHeight(tabs, 44);
                AddHLG(tabs, spacing: 4, pad: 0).childForceExpandWidth = true; // 5개 균등 분할
                var tabAll        = MakeButton("TabAll",        tabs.transform, "전체", out _);
                var tabConsumable = MakeButton("TabConsumable", tabs.transform, "소비", out _);
                var tabEquipment  = MakeButton("TabEquipment",  tabs.transform, "장비", out _);
                var tabMaterial   = MakeButton("TabMaterial",   tabs.transform, "재료", out _);
                var tabSpecial    = MakeButton("TabSpecial",    tabs.transform, "특수", out _);

                // 레시피 스크롤
                var recipeScroll = NewUI("RecipeScroll", left.transform);
                AddFlexible(recipeScroll, flexH: 1);
                var recipeContent = BuildVerticalScroll(recipeScroll, out _);

                // ================= 우측 =================
                var right = NewUI("RightColumn", body.transform);
                AddFlexibleW(right, 3f);
                var rightV = AddVLG(right, spacing: 8, pad: 0);
                rightV.childForceExpandHeight = false;

                // 상세 패널(선택 전 비활성)
                var detail = NewUI("DetailPanel", right.transform);
                AddImage(detail, PanelBg, UISprite, sliced: true);
                AddFlexible(detail, flexH: 1);
                var detailV = AddVLG(detail, spacing: 8, pad: 14);
                detailV.childForceExpandHeight = false;

                // 결과 행: 아이콘 + (이름/배지)
                var resultRow = NewUI("ResultRow", detail.transform);
                SetHeight(resultRow, 110);
                AddHLG(resultRow, spacing: 12, pad: 0);
                var resultIconGo = NewUI("ResultIcon", resultRow.transform);
                SetWidth(resultIconGo, 110);
                var imgResultIcon = AddImage(resultIconGo, SlotBg, UISprite, sliced: true);
                var nameCol = NewUI("NameCol", resultRow.transform);
                AddFlexibleW(nameCol, 1f);
                AddVLG(nameCol, spacing: 4, pad: 4);
                var txtResultName   = AddText(NewUI("ResultName", nameCol.transform), "결과 아이템", 28, TextMain, TextAlignmentOptions.Left);
                SetHeight(txtResultName.gameObject, 40);
                var txtCategoryBadge = AddText(NewUI("CategoryBadge", nameCol.transform), "장비", 20, Green, TextAlignmentOptions.Left);
                SetHeight(txtCategoryBadge.gameObject, 26);

                var txtDescription = AddText(NewUI("Description", detail.transform), "아이템 설명", 20, TextSub, TextAlignmentOptions.TopLeft);
                SetHeight(txtDescription.gameObject, 56);

                AddText(NewUI("IngredientTitle", detail.transform), "필요 재료", 22, TextMain, TextAlignmentOptions.Left);

                // 재료 컨테이너
                var ingrBox = NewUI("IngredientBox", detail.transform);
                AddImage(ingrBox, SlotBg, UISprite, sliced: true);
                AddFlexible(ingrBox, flexH: 1);
                var ingrContent = NewUI("Content", ingrBox.transform);
                Stretch(ingrContent);
                var ingrVlg = AddVLG(ingrContent, spacing: 4, pad: 8);
                ingrVlg.childForceExpandHeight = false;
                ingrVlg.childAlignment = TextAnchor.UpperCenter;

                // 비용 / 시간
                var costRow = NewUI("CostRow", detail.transform);
                SetHeight(costRow, 40);
                AddHLG(costRow, spacing: 20, pad: 0);
                var txtCost     = AddText(NewUI("Cost", costRow.transform), "비용 0 G", 22, TextMain, TextAlignmentOptions.Left);
                AddFlexibleW(txtCost.gameObject, 1f);
                var txtCastTime = AddText(NewUI("CastTime", costRow.transform), "제작 시간 0.0초", 22, TextMain, TextAlignmentOptions.Right);
                AddFlexibleW(txtCastTime.gameObject, 1f);

                // 하단 조작 패널
                var bottom = NewUI("BottomPanel", right.transform);
                AddImage(bottom, PanelBg, UISprite, sliced: true);
                SetHeight(bottom, 140);
                var bottomV = AddVLG(bottom, spacing: 8, pad: 12);
                bottomV.childForceExpandHeight = false;

                // 수량 + 제작 행
                var qtyRow = NewUI("QtyRow", bottom.transform);
                SetHeight(qtyRow, 60);
                AddHLG(qtyRow, spacing: 8, pad: 0);
                var qtyLabel = AddText(NewUI("QtyLabel", qtyRow.transform), "제작 수량", 22, TextSub, TextAlignmentOptions.Center);
                SetWidth(qtyLabel.gameObject, 120);
                var btnMinus = MakeButton("BtnMinus", qtyRow.transform, "-", out _);
                SetWidth(btnMinus.gameObject, 60);
                var txtQtyGo = NewUI("QtyValue", qtyRow.transform);
                SetWidth(txtQtyGo, 90);
                AddImage(txtQtyGo, SlotBg, UISprite, sliced: true);
                var txtQty = AddText(NewUI("Value", txtQtyGo.transform), "1", 26, TextMain, TextAlignmentOptions.Center);
                Stretch(txtQty.gameObject);
                var btnPlus = MakeButton("BtnPlus", qtyRow.transform, "+", out _);
                SetWidth(btnPlus.gameObject, 60);
                var btnMax = MakeButton("BtnMax", qtyRow.transform, "MAX", out _);
                SetWidth(btnMax.gameObject, 90);
                var btnCraft = MakeButton("BtnCraft", qtyRow.transform, "제작", out var txtCraftButton, AccentBtn);
                AddFlexibleW(btnCraft.gameObject, 1f);

                // 진행 바 행
                var progRow = NewUI("ProgressRow", bottom.transform);
                AddFlexible(progRow, flexH: 1);
                var barBgGo = NewUI("BarBg", progRow.transform);
                Stretch(barBgGo);
                AddImage(barBgGo, SlotBg, UISprite, sliced: true);
                var barFillGo = NewUI("BarFill", barBgGo.transform);
                Stretch(barFillGo);
                var imgProgressBar = AddImage(barFillGo, AccentBtn, UISprite, sliced: true);
                imgProgressBar.type = Image.Type.Filled;
                imgProgressBar.fillMethod = Image.FillMethod.Horizontal;
                imgProgressBar.fillAmount = 0f;
                var percentGo = NewUI("PercentText", barBgGo.transform);
                Stretch(percentGo);
                var txtProgressPercent = AddText(percentGo, "0%", 20, TextMain, TextAlignmentOptions.Center);
                txtProgressPercent.raycastTarget = false;
                var statusGo = NewUI("StatusText", barBgGo.transform);
                AnchorRight(Rt(statusGo), 160, 30);
                var txtCraftStatus = AddText(statusGo, "대기 중", 18, TextSub, TextAlignmentOptions.Right);
                txtCraftStatus.raycastTarget = false;

                // 3) 필드 자동 연결
                var so = new SerializedObject(menu);
                SetRef(so, "_recipeListContent", recipeContent.transform);
                SetRef(so, "_recipeSlotPrefab",  recipeSlot);
                SetRef(so, "_tabAll",        tabAll);
                SetRef(so, "_tabConsumable", tabConsumable);
                SetRef(so, "_tabEquipment",  tabEquipment);
                SetRef(so, "_tabMaterial",   tabMaterial);
                SetRef(so, "_tabSpecial",    tabSpecial);
                SetRef(so, "_detailPanel",       detail);
                SetRef(so, "_imgResultIcon",     imgResultIcon);
                SetRef(so, "_txtResultName",     txtResultName);
                SetRef(so, "_txtCategoryBadge",  txtCategoryBadge);
                SetRef(so, "_txtDescription",    txtDescription);
                SetRef(so, "_ingredientContent", ingrContent.transform);
                SetRef(so, "_ingredientSlotPrefab", ingrSlot);
                SetRef(so, "_txtCost",     txtCost);
                SetRef(so, "_txtCastTime", txtCastTime);
                SetRef(so, "_btnCraft",        btnCraft);
                SetRef(so, "_txtCraftButton",  txtCraftButton);
                SetRef(so, "_btnQtyMinus",     btnMinus);
                SetRef(so, "_btnQtyPlus",      btnPlus);
                SetRef(so, "_btnQtyMax",       btnMax);
                SetRef(so, "_txtQty",          txtQty);
                SetRef(so, "_imgProgressBar",  imgProgressBar);
                SetRef(so, "_txtProgressPercent", txtProgressPercent);
                SetRef(so, "_txtCraftStatus",  txtCraftStatus);
                SetRef(so, "_btnClose",        btnClose);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
                Debug.Log("[CraftBuilder] UI_CraftMenu 프리팹 초안 생성 완료.");
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

        private static UI_CraftingRecipeSlot BuildRecipeSlotPrefab()
        {
            var go = NewUI("UI_CraftingRecipeSlot", null);
            SetHeight(go, 64);
            AddImage(go, SlotBg, UISprite, sliced: true);
            var slot = go.AddComponent<UI_CraftingRecipeSlot>();
            AddHLG(go, spacing: 8, pad: 8);

            var iconGo = NewUI("Icon", go.transform);
            SetWidth(iconGo, 48);
            var img = AddImage(iconGo, new Color(1, 1, 1, 1));

            var name = AddText(NewUI("Name", go.transform), "레시피", 22, TextMain, TextAlignmentOptions.Left);
            AddFlexibleW(name.gameObject, 1f);

            var status = AddText(NewUI("Status", go.transform), "제작 가능", 18, Green, TextAlignmentOptions.Right);
            SetWidth(status.gameObject, 120);

            var dotGo = NewUI("CraftableDot", go.transform);
            SetWidth(dotGo, 16);
            var dot = AddImage(dotGo, Green);

            var overlay = NewUI("SelectOverlay", go.transform);
            Stretch(overlay);
            var overlayImg = AddImage(overlay, new Color(0.35f, 0.75f, 0.85f, 0.18f));
            overlayImg.raycastTarget = false;
            overlay.AddComponent<LayoutElement>().ignoreLayout = true; // HLG 레이아웃에서 제외(오버레이)
            overlay.transform.SetAsFirstSibling();
            overlay.SetActive(false);

            var so = new SerializedObject(slot);
            SetRef(so, "_imgResultIcon", img);
            SetRef(so, "_txtRecipeName", name);
            SetRef(so, "_imgCraftable",  dot);
            SetRef(so, "_txtStatus",     status);
            SetRef(so, "_selectOverlay", overlay);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, RecipeSlotPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UI_CraftingRecipeSlot>();
        }

        private static UI_CraftingIngredientSlot BuildIngredientSlotPrefab()
        {
            var go = NewUI("UI_CraftingIngredientSlot", null);
            SetHeight(go, 48);
            var slot = go.AddComponent<UI_CraftingIngredientSlot>();
            AddHLG(go, spacing: 8, pad: 6);

            var iconGo = NewUI("Icon", go.transform);
            SetWidth(iconGo, 40);
            var img = AddImage(iconGo, new Color(1, 1, 1, 1));

            var name = AddText(NewUI("Name", go.transform), "재료", 20, TextMain, TextAlignmentOptions.Left);
            AddFlexibleW(name.gameObject, 1f);

            var countBgGo = NewUI("CountBg", go.transform);
            SetWidth(countBgGo, 90);
            var countBg = AddImage(countBgGo, new Color(0.2f, 0.8f, 0.3f, 0.2f), UISprite, sliced: true);
            var count = AddText(NewUI("Count", countBgGo.transform), "0/0", 20, Green, TextAlignmentOptions.Center);
            Stretch(count.gameObject);

            var so = new SerializedObject(slot);
            SetRef(so, "_imgIcon",    img);
            SetRef(so, "_txtName",    name);
            SetRef(so, "_txtCount",   count);
            SetRef(so, "_imgCountBg", countBg);
            so.ApplyModifiedPropertiesWithoutUndo();

            var asset = PrefabUtility.SaveAsPrefabAsset(go, IngrSlotPrefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return asset.GetComponent<UI_CraftingIngredientSlot>();
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

        private static void AnchorRight(RectTransform rt, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(-8, 0);
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
            h.childForceExpandWidth = false;   // 폭은 flexibleWidth 비율/고정폭으로 제어
            h.childForceExpandHeight = true;
            h.childAlignment = TextAnchor.MiddleCenter;
            return h;
        }

        private static void SetHeight(GameObject go, float h)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = h;
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

        /// <summary> ScrollRect + Viewport(Mask) + Content(VLG+Fitter) 구성. Content Transform 반환. </summary>
        private static GameObject BuildVerticalScroll(GameObject scrollGo, out ScrollRect scrollRect)
        {
            AddImage(scrollGo, new Color(0.05f, 0.06f, 0.08f, 1f), UISprite, sliced: true);
            scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = NewUI("Viewport", scrollGo.transform);
            Stretch(viewport);
            var vpImg = AddImage(viewport, new Color(1, 1, 1, 0.01f));
            viewport.AddComponent<RectMask2D>();

            var content = NewUI("Content", viewport.transform);
            var crt = Rt(content);
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(0, 0);
            var cv = AddVLG(content, spacing: 4, pad: 6);
            cv.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = Rt(viewport);
            scrollRect.content = crt;
            return content;
        }

        private static void SetRef(SerializedObject so, string propName, UnityEngine.Object value)
        {
            var p = so.FindProperty(propName);
            if (p == null)
            {
                Debug.LogWarning($"[CraftBuilder] 직렬화 프로퍼티를 찾을 수 없음: {propName}");
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
