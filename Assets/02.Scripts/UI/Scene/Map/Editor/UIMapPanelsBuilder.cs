using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.Map.EditorTools
{
    /// <summary>
    /// 맵 UI(UI_Map)에 시안 패널(헤더/범례·필터/지역정보/줌 슬라이더·%)을 "추가"하는 에디터 툴.
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
        private const string MainPrefabPath = "Assets/03.Prefabs/UI/Scene/Map/UI_Map.prefab";

        private static readonly Color PanelBg  = new Color(0.06f, 0.09f, 0.13f, 0.92f);
        private static readonly Color SlotBg   = new Color(0.14f, 0.17f, 0.22f, 1f);
        private static readonly Color BtnBg    = new Color(0.18f, 0.24f, 0.30f, 1f);
        private static readonly Color TextMain = new Color(0.90f, 0.92f, 0.95f, 1f);
        private static readonly Color TextSub  = new Color(0.62f, 0.68f, 0.74f, 1f);
        private static readonly Color Gold     = new Color(0.90f, 0.78f, 0.45f, 1f);
        private static readonly Color Accent   = new Color(0.35f, 0.80f, 0.90f, 1f);

        private static Sprite UISprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        private static Sprite Knob     => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        private static Sprite Checkmark=> AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

        [MenuItem("UPlayGround/UI/맵 UI 패널 추가 (초안)")]
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
                var map = root.GetComponent<UI_Map>();
                if (map == null)
                {
                    Debug.LogError("[MapBuilder] 루트에 UI_Map 컴포넌트가 없습니다. 중단.");
                    return;
                }

                var so = new SerializedObject(map);

                // ── 헤더(상단 중앙 지역명) ──
                var header = GetOrReplace(root, "MapHeaderPanel");
                SetAnchored(Rt(header), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                            new Vector2(900, 56), new Vector2(0, -18));
                AddText(NewUI("MapTitle", header.transform), "지도", 30, TextMain, TextAlignmentOptions.Left)
                    .rectTransform.anchoredPosition = new Vector2(-380, 0);
                var headerRegion = AddText(NewUI("Region", header.transform), "벨리안 대륙    그레이우드 평원", 24, Gold, TextAlignmentOptions.Center);
                Stretch(headerRegion.gameObject);
                SetRef(so, "_headerRegionText", headerRegion);

                // ── 범례/필터 패널(우측) ──
                var legend = GetOrReplace(root, "MapLegendPanel");
                SetAnchored(Rt(legend), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                            new Vector2(300, 720), new Vector2(-20, -100));
                AddImage(legend, PanelBg, UISprite, sliced: true);
                AddVLG(legend, spacing: 4, pad: 12).childForceExpandHeight = false;

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
                var tUser   = MakeLegendRow(legend.transform, "유저 마커",            new Color(0.40f, 0.55f, 0.95f));

                var spacer = NewUI("Spacer", legend.transform);
                AddFlexible(spacer, 1);
                var clearBtn = MakeButton("ClearAllButton", legend.transform, "전체 해제", out _, BtnBg);
                SetHeight(clearBtn.gameObject, 48);

                SetRef(so, "_togglePlayer", tPlayer);
                SetRef(so, "_toggleQuest",  tQuest);
                SetRef(so, "_toggleEnemy",  tEnemy);
                SetRef(so, "_toggleNpc",    tNpc);
                SetRef(so, "_toggleStatic", tStatic);
                SetRef(so, "_toggleUser",   tUser);
                SetRef(so, "_clearAllButton", clearBtn);

                // ── 지역 정보 패널(좌하단) ──
                var region = GetOrReplace(root, "MapRegionPanel");
                SetAnchored(Rt(region), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                            new Vector2(320, 280), new Vector2(20, 20));
                AddImage(region, PanelBg, UISprite, sliced: true);
                AddVLG(region, spacing: 6, pad: 12).childForceExpandHeight = false;
                var regionName = AddText(NewUI("Name", region.transform), "지역명", 24, TextMain, TextAlignmentOptions.Left);
                SetHeight(regionName.gameObject, 32);
                var thumbGo = NewUI("Thumbnail", region.transform);
                SetHeight(thumbGo, 110);
                var thumb = AddImage(thumbGo, SlotBg, UISprite, sliced: true);
                var regionLevel = AddText(NewUI("Level", region.transform), "권장 레벨  Lv. 1 ~ 1", 18, Gold, TextAlignmentOptions.Left);
                SetHeight(regionLevel.gameObject, 24);
                var regionDesc = AddText(NewUI("Desc", region.transform), "지역 설명", 16, TextSub, TextAlignmentOptions.TopLeft);
                AddFlexible(regionDesc.gameObject, 1);
                var regionInfoBtn = MakeButton("RegionInfoButton", region.transform, "지역 정보", out _, BtnBg);
                SetHeight(regionInfoBtn.gameObject, 42);

                SetRef(so, "_regionNameText",  regionName);
                SetRef(so, "_regionLevelText", regionLevel);
                SetRef(so, "_regionDescText",  regionDesc);
                SetRef(so, "_regionThumbnail", thumb);

                // ── 줌 슬라이더(우측, 버튼 영역 아래) ──
                var zoomWrap = GetOrReplace(root, "MapZoomSlider");
                SetAnchored(Rt(zoomWrap), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                            new Vector2(34, 220), new Vector2(-30, 40));
                var slider = MakeVerticalSlider(zoomWrap);
                SetRef(so, "_zoomSlider", slider);

                // ── 줌 % (우하단) ──
                var zpct = GetOrReplace(root, "MapZoomPercent");
                SetAnchored(Rt(zpct), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                            new Vector2(140, 44), new Vector2(-30, 24));
                AddImage(zpct, PanelBg, UISprite, sliced: true);
                var zoomText = AddText(NewUI("Text", zpct.transform), "100%", 22, TextMain, TextAlignmentOptions.Center);
                Stretch(zoomText.gameObject);
                SetRef(so, "_zoomText", zoomText);

                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, MainPrefabPath);
                Debug.Log("[MapBuilder] UI_Map 패널 추가 완료 (코어 스캐폴드 보존).");
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

        private static Slider MakeVerticalSlider(GameObject parent)
        {
            var slider = parent.AddComponent<Slider>();
            AddImage(parent, SlotBg, UISprite, sliced: true);
            slider.direction = Slider.Direction.BottomToTop;

            var handleArea = NewUI("Handle Slide Area", parent.transform);
            SetAnchored(Rt(handleArea), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var handle = NewUI("Handle", handleArea.transform);
            var hrt = Rt(handle);
            hrt.sizeDelta = new Vector2(30, 30);
            var handleImg = AddImage(handle, Accent, Knob != null ? Knob : UISprite, sliced: false);

            slider.handleRect = hrt;
            slider.targetGraphic = handleImg;
            slider.value = 1f;
            return slider;
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
