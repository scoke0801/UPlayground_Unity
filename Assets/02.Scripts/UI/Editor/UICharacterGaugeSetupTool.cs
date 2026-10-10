using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Party;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.UI.Editor
{
    /// <summary>기존 HUD에 공용 문양·입력 글리프·접근성 설정을 고정 프리팹으로 저작합니다.</summary>
    public static class UICharacterGaugeSetupTool
    {
        public const string ToolId = "UPlayGround/UI/HUD/캐릭터 문양 게이지 적용";
        private const string Folder = "Assets/10.Datas/UI/CharacterGauge";
        private const string ShaderGraphPath = "Assets/06.Shaders/UI/CharacterGaugeCanvas.shadergraph";
        private const string Skin = "Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/";
        private const string SilhouettePath = Skin + "Frame/PanelFrame_02_Deco.png";
        private const string PlayerPath = "Assets/03.Prefabs/UI/HUD/UI_HUD_PlayerInfo.prefab";
        private const string SkillPath = "Assets/03.Prefabs/UI/HUD/Skill/UI_HUD_Skill.prefab";
        private const string SettingsPath = "Assets/03.Prefabs/UI/Scene/UI_Scene_SettingMenu.prefab";
        [Serializable] private sealed class SeedList { public Seed[] profiles; }
        [Serializable] private sealed class Seed { public string id; public string color; public string motif; }

        /// <summary>전용 아트를 우선 연결하고 누락된 캐릭터에는 공용 대체 프로필을 제공합니다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool(ToolId)]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Play Mode 종료와 컴파일 오류 해결 후 실행하세요.");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderGraphPath);
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("CharacterGauge 셰이더 컴파일을 먼저 확인하세요.");
            var silhouette = LoadSprite(SilhouettePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/CharacterGauge.mat");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, Folder + "/CharacterGauge.mat");
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
            }
            var fallback = AssetDatabase.LoadAssetAtPath<CharacterGaugeVisualProfileSO>(Folder + "/Neutral.asset");
            if (fallback == null)
            {
                fallback = ScriptableObject.CreateInstance<CharacterGaugeVisualProfileSO>();
                fallback.silhouetteSprite = silhouette;
                fallback.flowMap = CreateFlowMap();
                fallback.usesPlaceholderArtwork = true;
                AssetDatabase.CreateAsset(fallback, Folder + "/Neutral.asset");
            }
            CreateProfiles(fallback);
            EditPrefab(PlayerPath, root => ApplyPlayer(root, fallback, material));
            EditPrefab(SkillPath, ApplySkills);
            EditPrefab(SettingsPath, ApplySettings);
            AssetDatabase.Refresh();
            Debug.Log("[캐릭터 게이지] HUD 적용. 캐릭터별 전용 문양과 공용 대체 프로필 연결을 유지합니다.");
        }

        private static void CreateProfiles(CharacterGaugeVisualProfileSO fallback)
        {
            var seeds = JsonUtility.FromJson<SeedList>(File.ReadAllText(Folder + "/ProfileSeed.json"));
            foreach (var seed in seeds.profiles)
            {
                string path = Folder + "/CharacterGauge_" + seed.id + ".asset";
                var profile = AssetDatabase.LoadAssetAtPath<CharacterGaugeVisualProfileSO>(path);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<CharacterGaugeVisualProfileSO>();
                    ColorUtility.TryParseHtmlString(seed.color, out profile.energyColor);
                    profile.fallbackProfile = fallback;
                    profile.usesPlaceholderArtwork = true;
                    AssetDatabase.CreateAsset(profile, path);
                }
                ConnectArtwork(profile, seed.id);
                var definition = AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinitionSO>(
                    "Assets/10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_" + seed.id + ".asset");
                if (definition == null) throw new InvalidOperationException("캐릭터 정의 누락: " + seed.id);
                if (definition.gaugeVisualProfile != null) continue;
                definition.gaugeVisualProfile = profile;
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssetIfDirty(definition);
            }
        }

        private static void ConnectArtwork(CharacterGaugeVisualProfileSO profile, string id)
        {
            if (profile.HasArtwork && !profile.usesPlaceholderArtwork) return;
            string path = "Assets/04.Images/UI/CharacterGauge/Gauge_" + id;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path + ".png");
            if (sprite == null) return;
            profile.silhouetteSprite = sprite;
            profile.artworkUvRect = FindArtworkUvRect(path + ".png");
            profile.flowMap = null;
            profile.fillMode = CharacterGaugeFillMode.CenterOut;
            profile.referenceSize = new Vector2(430f, 36f);
            profile.anchorOffset = new Vector2(0f, 39f);
            profile.usesPlaceholderArtwork = false;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        private static Rect FindArtworkUvRect(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                texture.LoadImage(File.ReadAllBytes(path));
                Color32[] pixels = texture.GetPixels32();
                int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a <= 8) continue;
                    int x = i % texture.width, y = i / texture.width;
                    minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y);
                    maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
                }
                if (maxX < minX) throw new InvalidOperationException("문양의 불투명 영역이 없습니다: " + path);
                return new Rect(minX / (float)texture.width, minY / (float)texture.height,
                    (maxX - minX + 1f) / texture.width, (maxY - minY + 1f) / texture.height);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static Texture2D CreateFlowMap()
        {
            const string path = Folder + "/NeutralFlow.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                source.LoadImage(File.ReadAllBytes(SilhouettePath));
                Color32[] pixels = source.GetPixels32();
                int width = source.width, height = source.height;
                var distances = new int[pixels.Length];
                Array.Fill(distances, -1);
                var queue = new Queue<int>();
                int maxDistance = 1;
                // 각 연결 영역의 중심에 가까운 픽셀에서 출발해 문양 안쪽으로만 전파합니다.
                while (true)
                {
                    int seed = -1;
                    float closest = float.MaxValue;
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        if (pixels[i].a < 8 || distances[i] >= 0) continue;
                        float distance = Mathf.Abs(i % width - width * .5f) + i / width;
                        if (distance >= closest) continue;
                        closest = distance; seed = i;
                    }
                    if (seed < 0) break;
                    distances[seed] = Mathf.RoundToInt(closest);
                    queue.Enqueue(seed);
                    while (queue.Count > 0)
                    {
                        int index = queue.Dequeue();
                        int x = index % width, y = index / width;
                        maxDistance = Mathf.Max(maxDistance, distances[index]);
                        for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                            int next = ny * width + nx;
                            if (pixels[next].a < 8 || distances[next] >= 0) continue;
                            distances[next] = distances[index] + 1;
                            queue.Enqueue(next);
                        }
                    }
                }
                var histogram = new float[maxDistance + 1];
                float total = 0f;
                for (int i = 0; i < pixels.Length; i++)
                    if (distances[i] >= 0) { histogram[distances[i]] += pixels[i].a; total += pixels[i].a; }
                for (int i = 1; i < histogram.Length; i++) histogram[i] += histogram[i - 1];
                var flow = new Color[pixels.Length];
                for (int i = 0; i < flow.Length; i++)
                {
                    if (distances[i] < 0) continue;
                    flow[i] = new Color(histogram[distances[i]] / Mathf.Max(1f, total),
                        distances[i] / (float)maxDistance, 0f, 1f);
                }
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
                    { name = "NeutralFlow", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                texture.SetPixels(flow); texture.Apply();
                AssetDatabase.CreateAsset(texture, path);
                return texture;
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        private static void ApplyPlayer(GameObject root, CharacterGaugeVisualProfileSO fallback, Material material)
        {
            var hud = root.GetComponent<UI_HUD_PlayerInfo>();
            var serialized = new SerializedObject(hud);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(.5f, 0f);
            rootRect.pivot = new Vector2(.5f, .5f);
            rootRect.anchoredPosition = new Vector2(0f, 54f);
            rootRect.sizeDelta = new Vector2(600f, 32f);
            foreach (string name in new[] { "SkillPanel" })
                if (root.transform.Find(name) is Transform child) child.gameObject.SetActive(false);
            var hp = root.transform.Find("HpPanel") as RectTransform;
            hp.anchoredPosition = Vector2.zero;
            hp.sizeDelta = new Vector2(520f, 32f);
            var hpText = serialized.FindProperty("_hpText").objectReferenceValue as TMP_Text;
            hpText.fontSize = 16f;
            hpText.alignment = TextAlignmentOptions.Center;
            Place(hpText.rectTransform, new Vector2(480f, 24f), Vector2.zero);
            hpText.rectTransform.SetAsLastSibling();
            hpText.raycastTarget = false;
            var hpFill = serialized.FindProperty("_boardHpFill").objectReferenceValue as Image;
            var hpTrail = serialized.FindProperty("_boardHpWhiteFill").objectReferenceValue as Image;
            Sprite bar = LoadSprite(Skin + "Slider/Slider_Basic_Rectangle_Bg.png");
            Place(hpFill.rectTransform, new Vector2(480f, 22f), Vector2.zero);
            Place(hpTrail.rectTransform, new Vector2(480f, 22f), Vector2.zero);
            hpFill.sprite = hpTrail.sprite = LoadSprite(Skin + "Slider/Slider_Level_Slider_Fill_Bg.png");
            hpFill.type = hpTrail.type = Image.Type.Filled;
            hpFill.fillMethod = hpTrail.fillMethod = Image.FillMethod.Horizontal;
            hpFill.fillOrigin = hpTrail.fillOrigin = 0;
            hpFill.raycastTarget = hpTrail.raycastTarget = false;
            hpFill.color = new Color(.25f, .92f, .56f, 1f);
            hpTrail.color = new Color(1f, .78f, .53f, 1f);
            // HpFullBar는 지연 피해량이다. 배경으로 재사용하면 피해 추적 표시가 깨진다.
            var hpTrack = hp.Find("BG").GetComponent<Image>();
            Place(hpTrack.rectTransform, new Vector2(486f, 26f), Vector2.zero);
            hpTrack.sprite = bar;
            hpTrack.type = Image.Type.Simple;
            hpTrack.color = new Color(.035f, .06f, .075f, .8f);
            hpTrack.raycastTarget = false;
            hpTrack.enabled = true;
            hpTrack.gameObject.SetActive(true);
            ApplyHealthFrame(root);
            var effects = root.transform.Find("EffectArea") as RectTransform;
            if (effects != null) { effects.anchoredPosition = new Vector2(0f, 132f); effects.sizeDelta = new Vector2(360f, 60f); }
            var gaugeRoot = Ensure("CharacterGauge", root.transform, new Vector2(430f, 36f), new Vector2(0f, 39f));
            var group = EnsureComponent<CanvasGroup>(gaugeRoot.gameObject);
            group.blocksRaycasts = false; group.interactable = false;
            var gauge = EnsureComponent<UICharacterGauge>(gaugeRoot.gameObject);
            var imageRect = Ensure("Silhouette", gaugeRoot, new Vector2(430f, 36f), Vector2.zero);
            var image = EnsureComponent<RawImage>(imageRect.gameObject);
            image.raycastTarget = false;
            image.texture = fallback.silhouetteSprite.texture; image.material = material;
            var contrastRect = Ensure("ContrastSilhouette", gaugeRoot, imageRect.sizeDelta, Vector2.zero);
            contrastRect.SetAsFirstSibling();
            var contrast = EnsureComponent<Image>(contrastRect.gameObject);
            contrast.sprite = fallback.silhouetteSprite;
            contrast.color = new Color(.025f, .035f, .045f, 1f);
            contrast.raycastTarget = false;
            var outline = EnsureComponent<Outline>(contrast.gameObject);
            outline.effectColor = contrast.color;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = true;
            var ready = MakeImage("Ready", gaugeRoot, Skin + "UI_Etc/Toggle_Check_White_Icon_Check.png", new Vector2(12f, 12f), new Vector2(235f, 0f));
            if (gaugeRoot.Find("Locked") is Transform locked)
                UnityEngine.Object.DestroyImmediate(locked.gameObject);
            var cooldown = MakeText("Cooldown", gaugeRoot, hpText, new Vector2(48f, 20f), new Vector2(250f, 0f));
            var gaugeData = new SerializedObject(gauge);
            SetRef(gaugeData, "_image", image); SetRef(gaugeData, "_group", group);
            SetRef(gaugeData, "_contrastSilhouette", contrast);
            SetRef(gaugeData, "_materialTemplate", material); SetRef(gaugeData, "_fallbackProfile", fallback);
            SetRef(gaugeData, "_readyMark", ready.gameObject); SetRef(gaugeData, "_lockedMark", null);
            SetRef(gaugeData, "_cooldownText", cooldown);
            gaugeData.ApplyModifiedPropertiesWithoutUndo();
            SetRef(serialized, "_characterGauge", gauge);
            SetRef(serialized, "_staminaText", null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ApplyStamina(root);
            ready.gameObject.SetActive(false); cooldown.gameObject.SetActive(false);
        }

        /// <summary>스태미나 곡선의 물결 재질과 비상호작용 표시 그룹을 저작합니다.</summary>
        internal static void ApplyStamina(GameObject root)
        {
            var panel = root.transform.Find("StaminaPanel") as RectTransform;
            panel.anchoredPosition = new Vector2(80f, 400f);
            panel.sizeDelta = Vector2.one * 128f;
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true)) text.gameObject.SetActive(false);
            var water = AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/UI/StaminaWater.mat");
            if (water == null) throw new InvalidOperationException("스태미나 곡선 재질이 없습니다.");
            var group = panel.GetComponent<CanvasGroup>() ?? panel.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;
            var data = new SerializedObject(root.GetComponent<UI_HUD_PlayerInfo>());
            var fill = data.FindProperty("_staminaFill").objectReferenceValue as Image;
            SetRef(data, "_staminaGroup", group);
            SetRef(data, "_staminaText", null);
            data.FindProperty("_staminaNormalColor").colorValue = Color.white;
            data.FindProperty("_staminaLowColor").colorValue = new Color(1f, .55f, .38f, 1f);
            data.ApplyModifiedPropertiesWithoutUndo();
            foreach (var image in panel.GetComponentsInChildren<Image>(true))
                image.gameObject.SetActive(image == fill);
            // 프레임과 잔량을 한 쿼드에서 그려 끝부분이 잘리지 않게 합니다.
            fill.sprite = LoadSprite(Skin + "Frame/BorderFrame_Circle.png");
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

        /// <summary>체력과 피해 잔상을 은색 외곽선의 절삭형 바에 표시하고 숫자를 내부에 배치합니다.</summary>
        public static void ApplyHealthFrame(GameObject root)
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

            const string shape = Skin + "Slider/Slider_Border_Tapered_03_";
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
            // Filled는 채움 비율, 부모의 9슬라이스 Mask는 고정된 양 끝 윤곽만 담당합니다.
            trail.transform.SetParent(area.transform, false);
            fill.transform.SetParent(area.transform, false);
            trail.transform.SetAsFirstSibling();
            fill.transform.SetAsLastSibling();
            Place(trail.rectTransform, new Vector2(480f, 22f), Vector2.zero);
            Place(fill.rectTransform, new Vector2(480f, 22f), Vector2.zero);
            fill.sprite = trail.sprite = LoadSprite(Skin + "Slider/Slider_Level_Slider_Fill_Bg.png");
            fill.material = trail.material = null;
            fill.type = trail.type = Image.Type.Filled;
            fill.fillMethod = trail.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = trail.fillOrigin = 0;
            fill.color = new Color(.52f, .77f, .28f, 1f);
            trail.color = new Color(1f, .78f, .53f, 1f);
            // 원본의 22px 채움·28px 안쪽선·32px 바깥선 간격을 유지해야 절삭 모서리가 맞습니다.
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

        private static void ApplyCharacterIdentity(GameObject root, SerializedObject data, TMP_Text fontSource)
        {
            var panel = Ensure("LevelPanel", root.transform, new Vector2(84f, 64f), new Vector2(294f, 14f));
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
            var frame = MakeImage("ElementFrame", panel, Skin + "Frame/BorderFrame_Circle.png",
                new Vector2(36f, 36f), new Vector2(0f, 22f));
            const string iconFolder = Skin + "Icon_PictoIcons/Original/function_icon_";
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

        private static void ApplySkills(GameObject root)
        {
            var hud = root.GetComponent<UI_HUD_Skill>();
            var data = new SerializedObject(hud);
            var names = new[] { "UISkillSlot_ElementalImbue", "UISkillSlot_Ability", "UISkillSlot_Ultimate" };
            var slots = data.FindProperty("_slots"); slots.arraySize = names.Length;
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(324f, 144f);
            rect.anchoredPosition = new Vector2(-40f, 28f);
            foreach (Transform child in root.transform) child.gameObject.SetActive(Array.IndexOf(names, child.name) >= 0);
            for (int i = 0; i < names.Length; i++)
            {
                var slot = root.transform.Find(names[i]).GetComponent<UISkillSlot>();
                slots.GetArrayElementAtIndex(i).objectReferenceValue = slot;
                var slotRect = (RectTransform)slot.transform;
                slotRect.anchorMin = slotRect.anchorMax = new Vector2(1f, 0f);
                slotRect.anchoredPosition = new Vector2(-270f + i * 108f, 70f);
                slotRect.sizeDelta = new Vector2(100f, 132f);
                var slotData = new SerializedObject(slot);
                string iconPath = i == 0 ? "Assets/04.Images/UI/SkillIcon/Skill_Ability.png"
                    : i == 1 ? "Assets/04.Images/UI/SkillIcon/HeavyAttack.png" : "Assets/04.Images/UI/SkillIcon/Skill_Ultimate.png";
                Sprite icon = LoadSprite(iconPath);
                slotData.FindProperty("_icon").objectReferenceValue = icon;
                (slotData.FindProperty("_iconImage").objectReferenceValue as Image).sprite=icon;
                slotData.ApplyModifiedPropertiesWithoutUndo();
                ConfigureSkill(slot);
            }
            data.FindProperty("_ensureDashSlot").boolValue = false;
            data.FindProperty("_ensureElementalImbueSlot").boolValue = false;
            SetPositions(data.FindProperty("_keyboardPositions"));
            SetPositions(data.FindProperty("_gamepadPositions"));
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSkill(UISkillSlot slot)
        {
            var data = new SerializedObject(slot);
            var icon = data.FindProperty("_iconImage").objectReferenceValue as Image;
            var label = data.FindProperty("_labelText").objectReferenceValue as TMP_Text;
            if (label == null) label = slot.transform.Find("UnavailableReason")?.GetComponent<TMP_Text>();
            var key = data.FindProperty("_keyIcon").objectReferenceValue as UIInputPromptIcon;
            var cooldown = data.FindProperty("_cooldownText").objectReferenceValue as TMP_Text;
            icon.transform.SetParent(slot.transform, false);
            Place(icon.rectTransform, new Vector2(40f, 40f), new Vector2(0f, 20f));
            icon.color = new Color(.96f, .97f, 1f, 1f);
            cooldown.transform.SetParent(slot.transform, false);
            Place(cooldown.rectTransform, new Vector2(72f, 24f), new Vector2(0f, 20f));
            Place((RectTransform)key.transform, new Vector2(92f, 28f), new Vector2(0f, -28f));
            foreach (var image in key.GetComponents<Image>()) image.enabled = false;
            foreach (Transform child in slot.transform)
                if (child != icon.transform && child != cooldown.transform && child != key.transform
                    && child.name != "Diamond" && child.name != "LabelRibbon")
                    child.gameObject.SetActive(false);
            ConfigureSkillFrame(slot);
            var ribbon = slot.transform.Find("LabelRibbon") as RectTransform;
            if (ribbon != null)
            {
                ribbon.gameObject.SetActive(true);
                Place(ribbon, new Vector2(100f, 20f), new Vector2(0f, 62f));
                var ribbonImage = ribbon.GetComponent<Image>();
                if (ribbonImage != null) ribbonImage.enabled = false;
                ConfigureSlotText(ribbon.GetComponentInChildren<TMP_Text>(true), 14f);
            }
            var reason = MakeText("UnavailableReason", slot.transform, label,
                new Vector2(100f, 20f), new Vector2(0f, -54f));
            ConfigureSlotText(reason, 12f);
            SetRef(data, "_unavailableReason", reason);
            SetRef(data, "_labelText", null); SetRef(data, "_readyGlow", null); SetRef(data, "_comboGlow", null);
            SetRef(data, "_cooldownRoot", null); SetRef(data, "_cooldownFill", null);
            SetRef(data, "_tweenTarget", icon.rectTransform);
            // 입력 글리프와 사유는 항상 읽히도록 아이콘만 흐리게 한다.
            var previousDim = data.FindProperty("_dimGroup").objectReferenceValue as CanvasGroup;
            if (previousDim != null) previousDim.alpha = 1f;
            SetRef(data, "_dimGroup", EnsureComponent<CanvasGroup>(icon.gameObject));
            data.FindProperty("_showOnlyWhenGaugeFull").boolValue = false;
            data.FindProperty("_hideIconWhenUnavailable").boolValue = false;
            data.FindProperty("_dimAlpha").floatValue = .4f;
            data.FindProperty("_usePunch").floatValue = .08f;
            data.FindProperty("_useDuration").floatValue = .12f;
            data.FindProperty("_readyPunch").floatValue = .12f;
            data.FindProperty("_readyDuration").floatValue = .18f;
            data.ApplyModifiedPropertiesWithoutUndo();
            ConfigureComboPrompt(key, reason);
            cooldown.transform.SetAsLastSibling();
        }

        private static void ConfigureSkillFrame(UISkillSlot slot)
        {
            var frame = slot.transform.Find("Diamond") as RectTransform;
            if (frame == null) return;
            frame.gameObject.SetActive(true);
            Place(frame, new Vector2(64f, 64f), new Vector2(0f, 20f));
            var background = frame.GetComponent<Image>();
            background.sprite = LoadSprite(Skin + "Frame/SlotFrame_Circle_White_Bg.png");
            background.type = Image.Type.Simple;
            background.color = new Color(.035f, .06f, .085f, .5f);
            foreach (Transform child in frame)
                child.gameObject.SetActive(false);
            var border = MakeImage("ComboGlow", frame, Skin + "Frame/BorderFrame_Circle.png",
                new Vector2(64f, 64f), Vector2.zero);
            border.type = Image.Type.Simple;
            border.color = new Color(.88f, .92f, .96f, .5f);
        }

        private static void ConfigureSlotText(TMP_Text text, float size)
        {
            if (text == null) return;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.color = new Color(.78f, .82f, .88f, 1f);
        }

        private static void ConfigureComboPrompt(UIInputPromptIcon prompt, TMP_Text fontSource)
        {
            var row = Ensure("ComboGlyphs", prompt.transform, new Vector2(80f,28f), Vector2.zero);
            var layout = EnsureComponent<HorizontalLayoutGroup>(row.gameObject);
            layout.childAlignment = TextAnchor.MiddleCenter; layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false; layout.spacing = 2f;
            var item = Ensure("Template", row, new Vector2(32f,28f), Vector2.zero);
            var element = EnsureComponent<LayoutElement>(item.gameObject); element.preferredWidth = 32f; element.preferredHeight = 28f;
            var icon = EnsureComponent<Image>(Ensure("Icon",item,new Vector2(25f,25f),new Vector2(3f,0f)).gameObject);
            icon.preserveAspect = true; icon.raycastTarget = false;
            var label = MakeText("Fallback",item,fontSource,new Vector2(25f,25f),new Vector2(3f,0f));
            var plus = MakeText("Separator",item,fontSource,new Vector2(9f,25f),new Vector2(-16f,0f)); plus.text = "+";
            var glyph = EnsureComponent<UIInputPromptGlyphItem>(item.gameObject);
            var glyphData = new SerializedObject(glyph);
            SetRef(glyphData,"_icon",icon); SetRef(glyphData,"_label",label); SetRef(glyphData,"_separator",plus.gameObject);
            glyphData.ApplyModifiedPropertiesWithoutUndo(); item.gameObject.SetActive(false);
            var data = new SerializedObject(prompt);
            SetRef(data,"_comboContainer",row); SetRef(data,"_comboItemTemplate",glyph);
            var single = data.FindProperty("_iconImage").objectReferenceValue as Image;
            if(single != null) Place(single.rectTransform,new Vector2(28f,28f),Vector2.zero);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplySettings(GameObject root)
        {
            var page = root.GetComponentInChildren<UISettingPageGamePlay>(true);
            var content = page.transform.Find("Viewport/ScrollContent") ?? page.transform;
            var row = content.Find("Row_HUD 움직임 줄이기");
            if (row == null)
            {
                row = UnityEngine.Object.Instantiate(content.Find("Row_전투 진동").gameObject,content).transform;
                row.name = "Row_HUD 움직임 줄이기";
                row.Find("Label").GetComponent<TMP_Text>().text = "HUD 움직임 줄이기";
            }
            // 인덱스로 바인딩하는 기존 옵션의 순서를 보존합니다.
            row.SetAsLastSibling();
            var data = new SerializedObject(page);
            SetRef(data,"_reduceHudMotion",row.GetComponentInChildren<UISwitchButton>(true));
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPositions(SerializedProperty property)
        {
            property.arraySize = 3;
            for(int i=0;i<3;i++) property.GetArrayElementAtIndex(i).vector2Value = new Vector2(-270f + i * 108f, 70f);
        }
        private static void EditPrefab(string path, Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)>0)
                        throw new InvalidOperationException("Missing Script: "+path);
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path)
            ?? throw new InvalidOperationException("스프라이트 누락: "+path);
        private static T EnsureComponent<T>(GameObject go) where T:Component
        {
            T component = go.GetComponent<T>();
            if (component == null) component = go.AddComponent<T>();
            return component;
        }
        private static RectTransform Ensure(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = parent.Find(name) as RectTransform;
            if(rect == null) { var go = new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false); rect=(RectTransform)go.transform; }
            Place(rect,size,position); rect.gameObject.SetActive(true); return rect;
        }
        private static void Place(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            rect.sizeDelta=size; rect.anchoredPosition=position; rect.localRotation=Quaternion.identity; rect.localScale=Vector3.one;
        }
        private static Image MakeImage(string name,Transform parent,string path,Vector2 size,Vector2 position)
        {
            var image=EnsureComponent<Image>(Ensure(name,parent,size,position).gameObject);
            image.sprite=LoadSprite(path); image.preserveAspect=true; image.raycastTarget=false; return image;
        }
        private static TMP_Text MakeText(string name,Transform parent,TMP_Text source,Vector2 size,Vector2 position)
        {
            var text=EnsureComponent<TextMeshProUGUI>(Ensure(name,parent,size,position).gameObject);
            text.font=source.font; text.fontSize=16f; text.color=Color.white; text.alignment=TextAlignmentOptions.Center;
            text.text=string.Empty; text.raycastTarget=false; text.outlineWidth=.15f; text.outlineColor=Color.black;
            return text;
        }
        private static void SetRef(SerializedObject data,string property,UnityEngine.Object value) => data.FindProperty(property).objectReferenceValue=value;
    }
}
