using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI.HUD.EditorTools
{
    /// <summary>
    /// UI_HUD 시안의 좌하단 퀵슬롯과 우하단 전투 스킬 가이드를 uGUI 프리팹으로 생성한다.
    /// UI Toolkit/UI Builder를 사용하지 않으며, 프로젝트의 기존 코드형 프리팹 빌더와 같은 방식이다.
    /// </summary>
    public static class UIHudCombatWidgetsPrefabBuilder
    {
        private const string SkillPrefabPath = "Assets/03.Prefabs/UI/HUD/Skill/UI_HudSkill.prefab";
        private const string QuickSlotPrefabPath = "Assets/03.Prefabs/UI/HUD/QuickSlot/UI_HudQuickSlot.prefab";
        private const string DatabasePath = "Assets/10.Datas/Path/UIPrefabDatabase.asset";
        private const string QuickSlotKey = "HudQuickSlot";
        private const string GlyphDataPath = "Assets/10.Datas/UI/Input/InputGlyphData.asset";
        private const string HudSlotSpritePath = "Assets/04.Images/UI/Img_HudSlot.png";

        private static readonly Color Navy = new(0.025f, 0.075f, 0.13f, 0.88f);
        private static readonly Color Cyan = new(0.12f, 0.68f, 1f, 0.95f);
        private static readonly Color CyanSoft = new(0.20f, 0.75f, 1f, 0.28f);
        private static readonly Color Gold = new(1f, 0.78f, 0.22f, 1f);
        private static readonly Color TextMain = new(0.93f, 0.97f, 1f, 1f);
        private static readonly Color TextSub = new(0.58f, 0.78f, 0.93f, 1f);

        private static Sprite UISprite =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        [MenuItem("UPlayGround/UI/프리팹 빌드/HUD 스킬 + 퀵슬롯")]
        public static void Build()
        {
            EnsureFolder(Path.GetDirectoryName(SkillPrefabPath)?.Replace('\\', '/'));
            EnsureFolder(Path.GetDirectoryName(QuickSlotPrefabPath)?.Replace('\\', '/'));

            GameObject skill = BuildSkillPrefab();
            GameObject quickSlot = BuildQuickSlotPrefab();
            RegisterDatabase(skill, quickSlot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = skill;
            Debug.Log("[HudCombatBuilder] UI_HudSkill / UI_HudQuickSlot 생성 및 DB 등록 완료.");
        }

        private static GameObject BuildSkillPrefab()
        {
            var root = NewUI("UI_HudSkill", null);
            ConfigureHudRoot(root, new Vector2(600f, 255f), new Vector2(-24f, 48f), rightAligned: true);
            var hud = root.AddComponent<UI_HudSkill>();
            var glyphData = AssetDatabase.LoadAssetAtPath<InputGlyphDataSO>(GlyphDataPath);

            var slots = new[]
            {
                MakeSkillSlot(root.transform, "Ultimate", ComboInputToken.Skill2, "얼티밋",
                    PlayerAction.SkillUltimate, LoadSprite("Assets/04.Images/UI/SkillIcon/Skill_Ultimate.png"),
                    new Vector2(-304f, 200.8f), LabelSide.Right, glyphData, true),
                MakeSkillSlot(root.transform, "Ability", ComboInputToken.Skill1, "어빌리티",
                    PlayerAction.SkillAbility, LoadSprite("Assets/04.Images/UI/SkillIcon/Skill_Ability.png"),
                    new Vector2(-303f, 55f), LabelSide.Right, glyphData, true),
                MakeSkillSlot(root.transform, "ElementalImbue", ComboInputToken.ElementalImbue, "속성",
                    PlayerAction.ElementBuff, LoadSprite("Assets/04.Images/UI/EffectIcon/Buff_Icon_01.png"),
                    new Vector2(-516f, 200.8f), LabelSide.Left, glyphData, true),
                MakeSkillSlot(root.transform, "HeavyAttack", ComboInputToken.HeavyAttack, "강공격",
                    PlayerAction.HeavyAttack, LoadSprite("Assets/04.Images/UI/SkillIcon/HeavyAttack.png"),
                    new Vector2(-410f, 186f), LabelSide.Top, glyphData, false),
                MakeSkillSlot(root.transform, "Dash", ComboInputToken.Dash, "회피",
                    PlayerAction.Dash, LoadSprite("Assets/04.Images/UI/SkillIcon/Dash.png"),
                    new Vector2(-352f, 128f), LabelSide.Right, glyphData, false),
                MakeSkillSlot(root.transform, "LightAttack", ComboInputToken.LightAttack, "공격",
                    PlayerAction.Attack, LoadSprite("Assets/04.Images/UI/SkillIcon/lightAttack.png"),
                    new Vector2(-468f, 128f), LabelSide.Left, glyphData, false),
                MakeSkillSlot(root.transform, "Jump", ComboInputToken.Jump, "점프",
                    PlayerAction.Jump,
                    LoadSprite("Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Demo/Demo_Play/Joystick_Action_Icon_Jump.png"),
                    new Vector2(-410f, 70f), LabelSide.Bottom, glyphData, false),
            };

            var so = new SerializedObject(hud);
            SetObjectArray(so, "_slots", slots);
            SetBool(so, "_ensureDashSlot", false);
            SetBool(so, "_ensureElementalImbueSlot", false);
            SetEnum(so, "_layer", (int)CanvasLayer.HUD);
            SetBool(so, "_canCloseWithEsc", false);
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, SkillPrefabPath);
            Object.DestroyImmediate(root);
            return asset;
        }

        private static UISkillSlot MakeSkillSlot(
            Transform parent,
            string name,
            ComboInputToken token,
            string label,
            string action,
            Sprite icon,
            Vector2 position,
            LabelSide side,
            InputGlyphDataSO glyphData,
            bool usesGauge)
        {
            var go = NewUI($"UISkillSlot_{name}", parent);
            SetAnchored(Rt(go), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(.5f, .5f),
                usesGauge ? new Vector2(240f, 100f) : new Vector2(220f, 88f), position);
            var slot = go.AddComponent<UISkillSlot>();
            var dimGroup = go.AddComponent<CanvasGroup>();

            var ribbon = NewUI("LabelRibbon", go.transform);
            RectTransform ribbonRt = Rt(ribbon);
            ribbonRt.anchorMin = ribbonRt.anchorMax = new Vector2(.5f, .5f);
            switch (side)
            {
                case LabelSide.Right:
                    ribbonRt.pivot = new Vector2(0f, .5f);
                    ribbonRt.anchoredPosition = new Vector2(80f, 0f);
                    break;
                case LabelSide.Left:
                    ribbonRt.pivot = new Vector2(1f, .5f);
                    ribbonRt.anchoredPosition = new Vector2(-80f, 0f);
                    break;
                case LabelSide.Top:
                    ribbonRt.pivot = new Vector2(.5f, 0f);
                    ribbonRt.anchoredPosition = new Vector2(0f, 80f);
                    break;
                default:
                    ribbonRt.pivot = new Vector2(.5f, 1f);
                    ribbonRt.anchoredPosition = new Vector2(0f, -80f);
                    break;
            }
            bool compactSideLabel =
                token == ComboInputToken.Dash && side == LabelSide.Right;
            ribbonRt.sizeDelta = side is LabelSide.Left or LabelSide.Right
                ? new Vector2(compactSideLabel ? 76f : 112f, 30f)
                : new Vector2(92f, 28f);
            AddImage(ribbon, new Color(0.025f, 0.12f, 0.20f, .88f), UISprite, true).raycastTarget = false;

            TextAlignmentOptions labelAlignment = side switch
            {
                LabelSide.Right => TextAlignmentOptions.MidlineRight,
                LabelSide.Left => TextAlignmentOptions.MidlineLeft,
                _ => TextAlignmentOptions.Center,
            };
            var ribbonText = AddText(NewUI("Label", ribbon.transform), label, 17f, TextMain, labelAlignment);
            InsetStretch(Rt(ribbonText.gameObject), 9f);
            ribbonText.raycastTarget = false;

            var diamond = NewUI("Diamond", go.transform);
            SetAnchored(Rt(diamond), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(82f, 82f), Vector2.zero);
            Rt(diamond).localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddImage(
                    diamond,
                    usesGauge ? new Color(.08f, .14f, .20f, .94f) : Navy,
                    LoadSprite(HudSlotSpritePath))
                .raycastTarget = false;
            AddOutline(diamond, usesGauge ? Gold : Cyan, 2f);

            var glow = NewUI("ComboGlow", diamond.transform);
            Stretch(glow);
            AddImage(glow, new Color(.18f, .78f, 1f, .42f), UISprite, true).raycastTarget = false;
            AddOutline(glow, Color.white, 4f);
            glow.SetActive(false);

            var content = NewUI("Content", diamond.transform);
            Stretch(content);
            Rt(content).localRotation = Quaternion.Euler(0f, 0f, -45f);

            var iconGo = NewUI("Icon", content.transform);
            SetAnchored(Rt(iconGo), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(42f, 42f), Vector2.zero);
            var iconImage = AddImage(iconGo, Color.white);
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            var cooldown = NewUI("Cooldown", content.transform);
            Stretch(cooldown);
            Rt(cooldown).localRotation = Quaternion.Euler(0f, 0f, 45f);
            var cooldownImage = AddImage(
                cooldown,
                new Color(.1764706f, .1764706f, .1764706f, .47843137f),
                UISprite);
            cooldownImage.type = Image.Type.Filled;
            cooldownImage.fillMethod = Image.FillMethod.Radial360;
            cooldownImage.fillOrigin = 2;
            cooldownImage.fillClockwise = false;
            cooldownImage.raycastTarget = false;
            cooldown.SetActive(false);

            var cooldownText = AddText(NewUI("CooldownText", content.transform), string.Empty, 17f,
                Color.white, TextAlignmentOptions.Center);
            Stretch(cooldownText.gameObject);
            cooldownText.raycastTarget = false;

            var keyCap = NewUI("KeyCap", go.transform);
            Vector2 keyPosition = token switch
            {
                ComboInputToken.HeavyAttack => new Vector2(0f, 62f),
                ComboInputToken.Dash => new Vector2(62f, 0f),
                ComboInputToken.Jump => new Vector2(0f, -62f),
                ComboInputToken.LightAttack => new Vector2(-62f, 0f),
                _ => side == LabelSide.Left ? new Vector2(-55f, 0f) : new Vector2(55f, 0f),
            };
            SetAnchored(Rt(keyCap), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(30f, 30f), keyPosition);
            AddImage(keyCap, new Color(.04f, .10f, .18f, 1f), UISprite, true);
            AddOutline(keyCap, usesGauge ? Gold : Cyan, 2f);

            var glyphImageGo = NewUI("Glyph", keyCap.transform);
            InsetStretch(Rt(glyphImageGo), 5f);
            var glyphImage = AddImage(glyphImageGo, Color.white);
            glyphImage.preserveAspect = true;
            glyphImage.raycastTarget = false;
            var fallback = AddText(NewUI("Fallback", keyCap.transform), action, 12f, Color.white,
                TextAlignmentOptions.Center);
            Stretch(fallback.gameObject);
            fallback.enableAutoSizing = true;
            fallback.fontSizeMin = 8f;
            fallback.fontSizeMax = 15f;
            fallback.raycastTarget = false;

            var prompt = keyCap.AddComponent<UI_InputPromptIcon>();
            var promptSo = new SerializedObject(prompt);
            SetString(promptSo, "_mapName", InputMapNames.PlayerAction);
            SetString(promptSo, "_actionName", action);
            SetRef(promptSo, "_glyphData", glyphData);
            SetRef(promptSo, "_iconImage", glyphImage);
            SetRef(promptSo, "_fallbackLabel", fallback);
            promptSo.ApplyModifiedPropertiesWithoutUndo();

            var slotSo = new SerializedObject(slot);
            SetEnum(slotSo, "_token", (int)token);
            if (token == ComboInputToken.ElementalImbue)
                SetInt(slotSo, "_gaugeSlotOverride", (int)PlayerSkillSlot.ElementalImbue);
            SetRef(slotSo, "_icon", icon);
            SetBool(slotSo, "_useGaugeFeature", usesGauge);
            SetBool(slotSo, "_showOnlyWhenGaugeFull", token == ComboInputToken.Skill2);
            SetBool(slotSo, "_showGaugeUi", false);
            SetBool(slotSo, "_showCooldownUi", usesGauge || token == ComboInputToken.Dash);
            SetRef(slotSo, "_iconImage", iconImage);
            SetRef(slotSo, "_keyIcon", prompt);
            SetRef(slotSo, "_labelText", ribbonText);
            SetString(slotSo, "_defaultLabel", label);
            SetRef(slotSo, "_readyGlow", glow);
            SetRef(slotSo, "_comboGlow", glow);
            SetRef(slotSo, "_dimGroup", dimGroup);
            SetFloat(slotSo, "_dimAlpha", 0.32f);
            SetRef(slotSo, "_cooldownRoot", cooldown);
            SetRef(slotSo, "_cooldownFill", cooldownImage);
            SetRef(slotSo, "_cooldownText", cooldownText);
            SetRef(slotSo, "_tweenTarget", Rt(diamond));
            slotSo.ApplyModifiedPropertiesWithoutUndo();
            return slot;
        }

        private static GameObject BuildQuickSlotPrefab()
        {
            var root = NewUI("UI_HudQuickSlot", null);
            ConfigureHudRoot(root, new Vector2(320f, 320f), new Vector2(40f, 42f), rightAligned: false);
            var hud = root.AddComponent<UI_HudQuickSlot>();
            var glyphData = AssetDatabase.LoadAssetAtPath<InputGlyphDataSO>(GlyphDataPath);

            var positions = new[]
            {
                new Vector2(0f, 58f), new Vector2(58f, 0f),
                new Vector2(0f, -58f), new Vector2(-58f, 0f),
            };
            var actions = new[]
            {
                PlayerAction.QuickSlot_Up,
                PlayerAction.QuickSlot_Right,
                PlayerAction.QuickSlot_Down,
                PlayerAction.QuickSlot_Left,
            };
            var slots = new UIHudQuickSlotEntry[4];

            for (int i = 0; i < slots.Length; i++)
                slots[i] = MakeQuickSlot(
                    root.transform, $"Slot_{i + 1}", i, actions[i], positions[i], glyphData);

            var so = new SerializedObject(hud);
            SetObjectArray(so, "_slots", slots);
            SetEnum(so, "_layer", (int)CanvasLayer.HUD);
            SetBool(so, "_canCloseWithEsc", false);
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, QuickSlotPrefabPath);
            Object.DestroyImmediate(root);
            return asset;
        }

        private static UIHudQuickSlotEntry MakeQuickSlot(
            Transform parent,
            string name,
            int slotIndex,
            string actionName,
            Vector2 position,
            InputGlyphDataSO glyphData)
        {
            var go = NewUI(name, parent);
            SetAnchored(Rt(go), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(100f, 100f), position);
            var entry = go.AddComponent<UIHudQuickSlotEntry>();
            var state = go.AddComponent<CanvasGroup>();

            var diamond = NewUI("Diamond", go.transform);
            SetAnchored(Rt(diamond), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(82f, 82f), Vector2.zero);
            Rt(diamond).localRotation = Quaternion.Euler(0f, 0f, 45f);
            var background = AddImage(diamond, Color.white, LoadSprite(HudSlotSpritePath));
            var rarityOutline = AddOutline(diamond, Cyan, 2f);

            var content = NewUI("Content", diamond.transform);
            InsetStretch(Rt(content), 12f);
            Rt(content).localRotation = Quaternion.Euler(0f, 0f, -45f);

            var icon = AddImage(NewUI("Icon", content.transform), Color.white);
            SetAnchored(Rt(icon.gameObject), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(50f, 50f), Vector2.zero);
            icon.sprite = null;
            icon.enabled = false;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var cooldown = NewUI("Cooldown", content.transform);
            Stretch(cooldown);
            Rt(cooldown).localRotation = Quaternion.Euler(0f, 0f, 45f);
            var cooldownFill = AddImage(
                cooldown,
                new Color(.1764706f, .1764706f, .1764706f, .60784316f),
                UISprite);
            cooldownFill.type = Image.Type.Filled;
            cooldownFill.fillMethod = Image.FillMethod.Radial360;
            cooldownFill.fillOrigin = 2;
            cooldownFill.fillClockwise = false;
            cooldownFill.raycastTarget = false;
            var cooldownText = AddText(
                NewUI("CooldownText", cooldown.transform),
                string.Empty,
                15f,
                Color.white,
                TextAlignmentOptions.Center);
            Stretch(cooldownText.gameObject);
            Rt(cooldownText.gameObject).localRotation = Quaternion.Euler(0f, 0f, -45f);
            cooldownText.raycastTarget = false;
            cooldown.SetActive(false);

            var countBadge = NewUI("CountBadge", content.transform);
            SetAnchored(Rt(countBadge), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(30f, 20f), new Vector2(15f, -15f));
            AddImage(countBadge, new Color(.02f, .055f, .09f, .9f), UISprite, true).raycastTarget = false;
            var count = AddText(NewUI("Count", countBadge.transform), string.Empty, 17f, TextMain,
                TextAlignmentOptions.Center);
            Stretch(count.gameObject);
            count.fontStyle = FontStyles.Bold;
            count.raycastTarget = false;

            var keyCap = NewUI("KeyCap", go.transform);
            Vector2 keyPosition = slotIndex switch
            {
                0 => new Vector2(0f, 62f),
                1 => new Vector2(62f, 0f),
                2 => new Vector2(0f, -62f),
                _ => new Vector2(-62f, 0f),
            };
            SetAnchored(Rt(keyCap), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(28f, 28f), keyPosition);
            AddImage(keyCap, new Color(.04f, .10f, .18f, 1f), UISprite, true);
            AddOutline(keyCap, Cyan, 2f);

            var glyphImageGo = NewUI("Glyph", keyCap.transform);
            InsetStretch(Rt(glyphImageGo), 4f);
            var glyphImage = AddImage(glyphImageGo, Color.white);
            glyphImage.preserveAspect = true;
            glyphImage.raycastTarget = false;

            var keyText = AddText(NewUI("Fallback", keyCap.transform), actionName, 12f, Color.white,
                TextAlignmentOptions.Center);
            Stretch(keyText.gameObject);
            keyText.enableAutoSizing = true;
            keyText.fontSizeMin = 7f;
            keyText.fontSizeMax = 13f;
            keyText.raycastTarget = false;

            var prompt = keyCap.AddComponent<UI_InputPromptIcon>();
            var promptSo = new SerializedObject(prompt);
            SetString(promptSo, "_mapName", InputMapNames.PlayerAction);
            SetString(promptSo, "_actionName", actionName);
            SetRef(promptSo, "_glyphData", glyphData);
            SetRef(promptSo, "_iconImage", glyphImage);
            SetRef(promptSo, "_fallbackLabel", keyText);
            promptSo.ApplyModifiedPropertiesWithoutUndo();

            var empty = AddText(NewUI("Empty", content.transform), "+", 30f, TextSub,
                TextAlignmentOptions.Center);
            Stretch(empty.gameObject);
            empty.gameObject.SetActive(false);

            var button = go.AddComponent<Button>();
            button.targetGraphic = background;
            var so = new SerializedObject(entry);
            SetEnum(so, "_slotIndex", slotIndex);
            SetRef(so, "_slotBackground", background);
            SetRef(so, "_rarityOutline", rarityOutline);
            SetRef(so, "_iconImage", icon);
            SetRef(so, "_countRoot", countBadge);
            SetRef(so, "_countText", count);
            SetRef(so, "_emptyMark", empty.gameObject);
            SetRef(so, "_stateGroup", state);
            SetRef(so, "_useButton", button);
            SetRef(so, "_cooldownRoot", cooldown);
            SetRef(so, "_cooldownFill", cooldownFill);
            SetRef(so, "_cooldownText", cooldownText);
            SetRef(so, "_tweenTarget", Rt(diamond));
            so.ApplyModifiedPropertiesWithoutUndo();
            return entry;
        }

        private static void RegisterDatabase(GameObject skill, GameObject quickSlot)
        {
            var db = AssetDatabase.LoadAssetAtPath<UIPrefabDatabase>(DatabasePath);
            if (db == null)
            {
                Debug.LogWarning("[HudCombatBuilder] UIPrefabDatabase를 찾지 못해 등록을 건너뜁니다.");
                return;
            }

            db.RemovePrefab(UIKeyType.HudSkill.ToKey());
            db.AddPrefab(UIKeyType.HudSkill.ToKey(), skill, CanvasLayer.HUD, "우하단 전투/스킬 입력 HUD");
            db.RemovePrefab(QuickSlotKey);
            db.AddPrefab(QuickSlotKey, quickSlot, CanvasLayer.HUD, "좌하단 소비 아이템 퀵슬롯 HUD");
            EditorUtility.SetDirty(db);
        }

        private static void ConfigureHudRoot(GameObject root, Vector2 size, Vector2 position, bool rightAligned)
        {
            var rt = Rt(root);
            rt.anchorMin = rt.anchorMax = rightAligned ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            rt.pivot = rightAligned ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            var canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = false;
            root.AddComponent<CanvasGroup>();
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform Rt(GameObject go) => go.GetComponent<RectTransform>();

        private static void Stretch(GameObject go)
        {
            var rt = Rt(go);
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

        private static void SetAnchored(
            RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 position)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        private static Image AddImage(GameObject go, Color color, Sprite sprite = null, bool sliced = false)
        {
            var image = go.AddComponent<Image>();
            image.color = color;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            }
            return image;
        }

        private static Outline AddOutline(GameObject go, Color color, float distance)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
            return outline;
        }

        private static TextMeshProUGUI AddText(
            GameObject go, string text, float size, Color color, TextAlignmentOptions alignment)
        {
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;
            return label;
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
            else
                Debug.LogWarning($"[HudCombatBuilder] 직렬화 필드 없음: {propertyName}");
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.boolValue = value;
        }

        private static void SetEnum(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private static void SetInt(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private static void SetString(SerializedObject so, string propertyName, string value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.stringValue = value;
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
                property.floatValue = value;
        }

        private static void SetObjectArray<T>(SerializedObject so, string propertyName, T[] values)
            where T : Object
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
                return;

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private enum LabelSide
        {
            Left,
            Right,
            Top,
            Bottom,
        }
    }
}
