using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.HUD.EditorTools
{
    public static partial class UIHudPlayerInfoPrefabBuilder
    {
        private const string LayerLabSkin =
            "Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/";
        private const string StaminaWaterMaterialPath = "Assets/10.Datas/UI/StaminaWater.mat";

        /// <summary>스태미나 곡선의 물결 재질과 비상호작용 표시 그룹을 저작한다.</summary>
        private static void ApplyStamina(GameObject root)
        {
            var panel = root.transform.Find("StaminaPanel") as RectTransform;
            if (panel == null)
                throw new System.InvalidOperationException("StaminaPanel이 없습니다.");
            panel.anchoredPosition = new Vector2(80f, 400f);
            panel.sizeDelta = Vector2.one * 128f;
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true)) text.gameObject.SetActive(false);
            var water = AssetDatabase.LoadAssetAtPath<Material>(StaminaWaterMaterialPath);
            if (water == null) throw new System.InvalidOperationException("스태미나 곡선 재질이 없습니다.");
            var group = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
            var data = new SerializedObject(root.GetComponent<UI_HUD_PlayerInfo>());
            var fill = data.FindProperty("_staminaFill").objectReferenceValue as Image;
            if (fill == null) throw new System.InvalidOperationException("_staminaFill 참조가 없습니다.");
            SetRef(data, "_staminaGroup", group);
            SetRef(data, "_staminaText", null);
            data.FindProperty("_staminaNormalColor").colorValue = Color.white;
            data.FindProperty("_staminaLowColor").colorValue = new Color(1f, .55f, .38f, 1f);
            data.ApplyModifiedPropertiesWithoutUndo();
            foreach (var image in panel.GetComponentsInChildren<Image>(true))
                image.gameObject.SetActive(image == fill);
            // 프레임과 잔량을 한 쿼드에서 그려 끝부분이 잘리지 않게 한다.
            fill.sprite = LoadSprite(LayerLabSkin + "Frame/BorderFrame_Circle.png");
            fill.type = Image.Type.Simple;
            fill.material = water;
            fill.color = Color.white;
            fill.preserveAspect = false;
            fill.useSpriteMesh = false;
            fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = new Vector2(.5f, .5f);
            fill.rectTransform.sizeDelta = Vector2.one * 128f;
            fill.rectTransform.anchoredPosition = Vector2.zero;
            fill.rectTransform.localRotation = Quaternion.identity;
            fill.raycastTarget = false;
            panel.gameObject.SetActive(false);
        }

        /// <summary>체력과 피해 잔상을 은색 외곽선의 절삭형 바에 표시하고 숫자를 내부에 배치한다.</summary>
        private static void ApplyHealthFrame(GameObject root)
        {
            var panel = root.transform.Find("HpPanel");
            if (panel == null) return;
            var hud = root.GetComponent<UI_HUD_PlayerInfo>();
            var data = new SerializedObject(hud);
            var fill = data.FindProperty("_boardHpFill").objectReferenceValue as Image;
            var trail = data.FindProperty("_boardHpWhiteFill").objectReferenceValue as Image;
            var text = data.FindProperty("_hpText").objectReferenceValue as TMP_Text;
            var background = panel.Find("BG")?.GetComponent<Image>();
            if (fill == null || trail == null || background == null) return;

            const string shape = LayerLabSkin + "Slider/Slider_Border_Tapered_03_";
            background.sprite = LoadSprite(shape + "FillArea.png");
            background.type = Image.Type.Sliced;
            background.material = null;
            background.color = new Color(.10f, .12f, .10f, .72f);
            background.pixelsPerUnitMultiplier = 1f;
            Place(background.rectTransform, new Vector2(480f, 22f), Vector2.zero);
            background.transform.SetAsFirstSibling();

            var area = MakeImage("HpFillArea", panel, shape + "FillArea.png",
                new Vector2(480f, 22f), Vector2.zero);
            area.type = Image.Type.Sliced;
            area.pixelsPerUnitMultiplier = 1f;
            area.preserveAspect = false;
            area.color = Color.white;
            area.transform.SetSiblingIndex(1);
            var mask = EnsureComponent<Mask>(area.gameObject);
            mask.showMaskGraphic = false;
            // Filled는 채움 비율, 부모의 9슬라이스 Mask는 고정된 양 끝 윤곽만 담당한다.
            trail.transform.SetParent(area.transform, false);
            fill.transform.SetParent(area.transform, false);
            trail.transform.SetAsFirstSibling();
            fill.transform.SetAsLastSibling();
            Place(trail.rectTransform, new Vector2(480f, 22f), Vector2.zero);
            Place(fill.rectTransform, new Vector2(480f, 22f), Vector2.zero);
            fill.sprite = trail.sprite = LoadSprite(LayerLabSkin + "Slider/Slider_Level_Slider_Fill_Bg.png");
            fill.material = trail.material = null;
            fill.type = trail.type = Image.Type.Filled;
            fill.fillMethod = trail.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = trail.fillOrigin = 0;
            fill.color = new Color(.52f, .77f, .28f, 1f);
            trail.color = new Color(1f, .78f, .53f, 1f);
            // 원본의 22px 채움·28px 안쪽선·32px 바깥선 간격을 유지해야 절삭 모서리가 맞는다.
            var inner = MakeImage("HpInnerBorder", panel, shape + "BorderInner.png",
                new Vector2(486f, 28f), Vector2.zero);
            inner.type = Image.Type.Sliced;
            inner.pixelsPerUnitMultiplier = 1f;
            inner.preserveAspect = false;
            inner.material = null;
            inner.color = new Color(.08f, .11f, .13f, 1f);
            inner.enabled = true;
            inner.transform.SetAsLastSibling();
            var border = MakeImage("HpBorder", panel, shape + "Border.png",
                new Vector2(490f, 32f), Vector2.zero);
            border.type = Image.Type.Sliced;
            border.pixelsPerUnitMultiplier = 1f;
            border.preserveAspect = false;
            border.material = null;
            border.color = new Color(.78f, .84f, .87f, 1f);
            border.enabled = true;
            border.transform.SetAsLastSibling();
            if (text != null)
            {
                Place(text.rectTransform, new Vector2(480f, 24f), Vector2.zero);
                text.fontSize = 16f;
                text.color = new Color(.9f, .95f, .94f, 1f);
                text.alignment = TextAlignmentOptions.Center;
                text.transform.SetAsLastSibling();
            }
            ApplyCharacterIdentity(root, data, text);
        }

        /// <summary>체력 바 옆에 레벨과 속성 표시를 만들고 속성별 아이콘 목록을 연결한다.</summary>
        private static void ApplyCharacterIdentity(GameObject root, SerializedObject data, TMP_Text fontSource)
        {
            var panel = EnsureRect("LevelPanel", root.transform, new Vector2(84f, 64f), new Vector2(294f, 14f));
            var level = MakeText("LevelText", panel, fontSource, new Vector2(84f, 28f), new Vector2(0f, -14f));
            level.fontSize = 20f;
            level.enableAutoSizing = false;
            level.textWrappingMode = TextWrappingModes.NoWrap;
            level.text = "LV 1";
            var element = MakeText("ElementText", panel, fontSource, new Vector2(84f, 24f), new Vector2(0f, 22f));
            element.fontSize = 18f;
            element.enableAutoSizing = false;
            element.textWrappingMode = TextWrappingModes.NoWrap;
            element.text = "무속성";
            SetRef(data, "_levelText", level);
            SetRef(data, "_elementText", element);
            var frame = MakeImage("ElementFrame", panel, LayerLabSkin + "Frame/BorderFrame_Circle.png",
                new Vector2(36f, 36f), new Vector2(0f, 22f));
            const string iconFolder = LayerLabSkin + "Icon_PictoIcons/Original/function_icon_";
            var icon = MakeImage("ElementIcon", panel, iconFolder + "diamond.png",
                new Vector2(22f, 22f), new Vector2(0f, 22f));
            SetRef(data, "_elementFrame", frame);
            SetRef(data, "_elementIcon", icon);
            string[] iconNames = { "diamond", "fire", "water", "leaf", "sun", "moon" };
            var entries = data.FindProperty("_elementIcons");
            entries.arraySize = iconNames.Length;
            for (int i = 0; i < iconNames.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("element").intValue = i;
                entry.FindPropertyRelative("sprite").objectReferenceValue = LoadSprite(iconFolder + iconNames[i] + ".png");
            }
            level.gameObject.SetActive(false);
            icon.gameObject.SetActive(false);
            frame.gameObject.SetActive(false);
            element.gameObject.SetActive(false);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path)
            ?? throw new System.InvalidOperationException("스프라이트 누락: " + path);

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null) component = go.AddComponent<T>();
            return component;
        }

        private static RectTransform EnsureRect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = parent.Find(name) as RectTransform;
            if (rect == null)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                rect = (RectTransform)go.transform;
            }
            Place(rect, size, position);
            rect.gameObject.SetActive(true);
            return rect;
        }

        private static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        private static Image MakeImage(string name, Transform parent, string path, Vector2 size, Vector2 position)
        {
            var image = EnsureComponent<Image>(EnsureRect(name, parent, size, position).gameObject);
            image.sprite = LoadSprite(path);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text MakeText(string name, Transform parent, TMP_Text source, Vector2 size, Vector2 position)
        {
            var text = EnsureComponent<TextMeshProUGUI>(EnsureRect(name, parent, size, position).gameObject);
            text.font = source.font;
            text.fontSize = 16f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.text = string.Empty;
            text.raycastTarget = false;
            text.outlineWidth = .15f;
            text.outlineColor = Color.black;
            return text;
        }
    }
}
