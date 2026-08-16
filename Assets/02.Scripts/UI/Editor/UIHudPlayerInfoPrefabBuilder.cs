using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI.HUD.EditorTools
{
    /// <summary>
    /// 현재 UI_HUD_PlayerInfo 프리팹의 기존 계층은 보존하고,
    /// 전투 자원 표시와 버프·디버프 아이콘 영역을 멱등적으로 구성한다.
    /// </summary>
    public static class UIHudPlayerInfoPrefabBuilder
    {
        private const string PrefabPath =
            "Assets/03.Prefabs/UI/HUD/UI_HUD_PlayerInfo.prefab";
        private const string EffectAreaName = "EffectArea";
        private const float EffectAreaHeight = 310f;
        private const float EffectAreaBottom = 18f;
        private const float EffectIconSize = 44f;
        private const float EffectIconSpacing = 6f;

        private static readonly Color Navy =
            new Color(0.025f, 0.075f, 0.13f, 0.94f);
        private static readonly Color Beneficial =
            new Color32(0x42, 0xE3, 0x9A, 0xFF);
        private static readonly Color Stamina =
            new Color(0.96f, 0.7f, 0.18f, 1f);
        private static readonly Color StaminaFrame =
            new Color(0.96f, 0.7f, 0.18f, 0.78f);
        private static readonly Color TextMain =
            new Color(0.95f, 0.98f, 1f, 1f);

        private static Sprite UISprite =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        public static void Build()
        {
            if (!File.Exists(PrefabPath))
            {
                Debug.LogError(
                    $"[HudPlayerInfoBuilder] 대상 프리팹이 없습니다: {PrefabPath}");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                UI_HUD_PlayerInfo hud = root.GetComponent<UI_HUD_PlayerInfo>();
                if (hud == null)
                {
                    Debug.LogError(
                        "[HudPlayerInfoBuilder] 루트에 UI_HUD_PlayerInfo가 없어 중단합니다.");
                    return;
                }

                RefineCombatResourcePanels(root.transform, hud);

                Transform existing = root.transform.Find(EffectAreaName);
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);

                GameObject area = NewUI(EffectAreaName, root.transform);
                RectTransform areaRt = Rt(area);
                RectTransform hpBar =
                    root.transform.Find("HpPanel/HpFullBar") as RectTransform;
                float areaWidth = hpBar != null ? hpBar.rect.width : 424f;
                float areaCenterX =
                    hpBar != null ? hpBar.anchoredPosition.x : -3f;
                SetAnchored(
                    areaRt,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(areaWidth, EffectAreaHeight),
                    new Vector2(
                        areaCenterX,
                        EffectAreaBottom + EffectAreaHeight * 0.5f));
                area.AddComponent<RectMask2D>();
                areaRt.SetSiblingIndex(0);

                GameObject iconRoot = NewUI("EffectIconRow", area.transform);
                RectTransform iconRootRt = Rt(iconRoot);
                Stretch(iconRootRt);
                var layout = iconRoot.AddComponent<GridLayoutGroup>();
                layout.startCorner = GridLayoutGroup.Corner.LowerLeft;
                layout.startAxis = GridLayoutGroup.Axis.Horizontal;
                layout.cellSize = Vector2.one * EffectIconSize;
                layout.spacing = Vector2.one * EffectIconSpacing;
                layout.childAlignment = TextAnchor.LowerLeft;
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = Mathf.Max(
                    1,
                    Mathf.FloorToInt(
                        (areaWidth + EffectIconSpacing)
                        / (EffectIconSize + EffectIconSpacing)));

                UIGameplayEffectIcon template = CreateIconTemplate(iconRoot.transform);
                template.gameObject.SetActive(false);

                GameObject overflow = NewUI("OverflowBadge", area.transform);
                SetAnchored(
                    Rt(overflow),
                    Vector2.one,
                    Vector2.one,
                    Vector2.one,
                    new Vector2(42f, 26f),
                    Vector2.zero);
                Image overflowBg = AddImage(overflow, Navy, UISprite, sliced: true);
                overflowBg.raycastTarget = false;
                AddOutline(overflow, Beneficial, 1f);
                TextMeshProUGUI overflowText = AddText(
                    NewUI("Text", overflow.transform),
                    "+1",
                    16f,
                    TextMain,
                    TextAlignmentOptions.Center);
                Stretch(Rt(overflowText.gameObject));
                overflowText.fontStyle = FontStyles.Bold;
                overflowText.raycastTarget = false;
                overflow.SetActive(false);

                var so = new SerializedObject(hud);
                SetRef(so, "_effectArea", areaRt);
                SetRef(so, "_effectIconRoot", iconRootRt);
                SetRef(so, "_effectIconTemplate", template);
                SetRef(so, "_effectOverflowText", overflowText);
                SetRef(so, "_effectFallbackIcon", UISprite);
                SetInt(so, "_maxVisibleEffects", 10);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log(
                    $"[HudPlayerInfoBuilder] 전투 자원과 상태 효과 영역 구성 완료: {PrefabPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static void RefineCombatResourcePanels(
            Transform root,
            UI_HUD_PlayerInfo hud)
        {
            SetResourcePanelLayout(
                root,
                "HpPanel",
                Vector2.zero,
                new Vector2(600f, 40f));
            SetResourcePanelLayout(
                root,
                "SkillPanel",
                new Vector2(0f, -37f),
                new Vector2(550f, 36f));
            RectTransform panel = SetResourcePanelLayout(
                root,
                "StaminaPanel",
                new Vector2(0f, -72f),
                new Vector2(500f, 32f));
            if (panel == null)
                return;

            Transform frameTransform = panel.Find("StaminaFrame")
                ?? panel.Find("BG");
            if (frameTransform != null)
            {
                frameTransform.name = "StaminaFrame";
                RectTransform frameRect = frameTransform as RectTransform;
                if (frameRect != null)
                {
                    frameRect.anchoredPosition = Vector2.zero;
                    frameRect.sizeDelta = new Vector2(0f, -10f);
                }
                Image frame = frameTransform.GetComponent<Image>();
                if (frame != null)
                    frame.color = StaminaFrame;
            }

            Image fill = panel.Find("StaminaFill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = Stamina;
                RectTransform fillRect = fill.rectTransform;
                fillRect.anchoredPosition = new Vector2(-2.75f, 0f);
                fillRect.sizeDelta = new Vector2(-153f, -17f);
            }

            TextMeshProUGUI text =
                panel.Find("StaminaText")?.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = "100/100";
                text.fontSize = 20f;
                text.raycastTarget = false;
                text.rectTransform.anchoredPosition = Vector2.zero;
                text.rectTransform.sizeDelta = new Vector2(480f, 30f);
            }

            var so = new SerializedObject(hud);
            SetRef(so, "_staminaPanel", panel);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform SetResourcePanelLayout(
            Transform root,
            string panelName,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            RectTransform panel = root.Find(panelName) as RectTransform;
            if (panel == null)
            {
                Debug.LogWarning(
                    $"[HudPlayerInfoBuilder] {panelName}이 없어 레이아웃 보정을 건너뜁니다.");
                return null;
            }

            panel.anchoredPosition = anchoredPosition;
            panel.sizeDelta = size;
            return panel;
        }

        private static UIGameplayEffectIcon CreateIconTemplate(Transform parent)
        {
            GameObject root = NewUI("EffectIconTemplate", parent);
            RectTransform rootRt = Rt(root);
            rootRt.sizeDelta = new Vector2(44f, 44f);
            var layout = root.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = 44f;
            layout.minHeight = layout.preferredHeight = 44f;

            Image border = AddImage(root, Beneficial, UISprite, sliced: true);
            border.raycastTarget = false;
            AddOutline(root, new Color(0f, 0f, 0f, 0.75f), 1f);

            GameObject plate = NewUI("Plate", root.transform);
            InsetStretch(Rt(plate), 3f);
            Image plateImage = AddImage(plate, Navy, UISprite, sliced: true);
            plateImage.raycastTarget = false;

            GameObject iconGo = NewUI("Icon", plate.transform);
            InsetStretch(Rt(iconGo), 3f);
            Image icon = AddImage(iconGo, Color.white);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TextMeshProUGUI fallbackText = AddText(
                NewUI("FallbackText", plate.transform),
                "?",
                22f,
                TextMain,
                TextAlignmentOptions.Center);
            Stretch(Rt(fallbackText.gameObject));
            fallbackText.fontStyle = FontStyles.Bold;
            fallbackText.raycastTarget = false;

            GameObject timeShadeGo = NewUI("TimeShade", plate.transform);
            Stretch(Rt(timeShadeGo));
            Image timeShade = AddImage(
                timeShadeGo,
                new Color(0f, 0.02f, 0.05f, 0.72f),
                UISprite);
            timeShade.type = Image.Type.Filled;
            timeShade.fillMethod = Image.FillMethod.Radial360;
            timeShade.fillOrigin = 2;
            timeShade.fillClockwise = false;
            timeShade.fillAmount = 0f;
            timeShade.raycastTarget = false;

            TextMeshProUGUI polarityText = AddText(
                NewUI("Polarity", root.transform),
                "+",
                15f,
                Color.white,
                TextAlignmentOptions.Center);
            SetAnchored(
                Rt(polarityText.gameObject),
                Vector2.one,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(18f, 18f),
                new Vector2(-2f, -2f));
            polarityText.fontStyle = FontStyles.Bold;
            polarityText.raycastTarget = false;

            GameObject stackBadge = NewUI("StackBadge", root.transform);
            SetAnchored(
                Rt(stackBadge),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(20f, 18f),
                new Vector2(-1f, 1f));
            Image stackBg = AddImage(
                stackBadge,
                new Color(0.02f, 0.04f, 0.07f, 0.94f),
                UISprite,
                sliced: true);
            stackBg.raycastTarget = false;
            TextMeshProUGUI stackText = AddText(
                NewUI("Text", stackBadge.transform),
                "2",
                13f,
                TextMain,
                TextAlignmentOptions.Center);
            Stretch(Rt(stackText.gameObject));
            stackText.fontStyle = FontStyles.Bold;
            stackText.raycastTarget = false;
            stackBadge.SetActive(false);

            TextMeshProUGUI remainingText = AddText(
                NewUI("Remaining", root.transform),
                "9.9",
                13f,
                Color.white,
                TextAlignmentOptions.Center);
            SetAnchored(
                Rt(remainingText.gameObject),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(42f, 17f),
                new Vector2(0f, 1f));
            remainingText.fontStyle = FontStyles.Bold;
            remainingText.outlineWidth = 0.18f;
            remainingText.outlineColor = Color.black;
            remainingText.raycastTarget = false;

            UIGameplayEffectIcon result = root.AddComponent<UIGameplayEffectIcon>();
            var so = new SerializedObject(result);
            SetRef(so, "_border", border);
            SetRef(so, "_icon", icon);
            SetRef(so, "_timeShade", timeShade);
            SetRef(so, "_fallbackText", fallbackText);
            SetRef(so, "_stackBadge", stackBadge);
            SetRef(so, "_stackText", stackText);
            SetRef(so, "_remainingText", remainingText);
            SetRef(so, "_polarityText", polarityText);
            so.ApplyModifiedPropertiesWithoutUndo();
            return result;
        }

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform Rt(GameObject go) =>
            go.GetComponent<RectTransform>();

        private static void SetAnchored(
            RectTransform rt,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 size,
            Vector2 position)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void InsetStretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static Image AddImage(
            GameObject go,
            Color color,
            Sprite sprite = null,
            bool sliced = false)
        {
            Image image = go.AddComponent<Image>();
            image.color = color;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
            return image;
        }

        private static Outline AddOutline(
            GameObject go,
            Color color,
            float distance)
        {
            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
            return outline;
        }

        private static TextMeshProUGUI AddText(
            GameObject go,
            string text,
            float size,
            Color color,
            TextAlignmentOptions alignment)
        {
            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;
            return label;
        }

        private static void SetRef(
            SerializedObject so,
            string propertyName,
            Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
            else
                Debug.LogWarning(
                    $"[HudPlayerInfoBuilder] 직렬화 필드 없음: {propertyName}");
        }

        private static void SetInt(
            SerializedObject so,
            string propertyName,
            int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }
    }
}
