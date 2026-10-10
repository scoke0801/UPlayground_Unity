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
        private const string Skin = "Assets/ExternalAssets/UI/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/";
        private const string SilhouettePath = Skin + "Frame/PanelFrame_02_Deco.png";
        private const string PlayerPath = "Assets/03.Prefabs/UI/HUD/UI_HUD_PlayerInfo.prefab";
        private const string SkillPath = "Assets/03.Prefabs/UI/HUD/Skill/UI_HUD_Skill.prefab";
        private const string SettingsPath = "Assets/03.Prefabs/UI/Scene/UI_Scene_SettingMenu.prefab";
        [Serializable] private sealed class SeedList { public Seed[] profiles; }
        [Serializable] private sealed class Seed { public string id; public string color; public string motif; }

        /// <summary>전용 아트는 보존하고 미연결 캐릭터에 공용 대체 문양과 표시 프리셋을 연결합니다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool(ToolId)]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Play Mode 종료와 컴파일 오류 해결 후 실행하세요.");
            var shader = Shader.Find("UPlayGround/UI/CharacterGauge");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("CharacterGauge 셰이더 컴파일을 먼저 확인하세요.");
            var silhouette = LoadSprite(SilhouettePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/CharacterGauge.mat");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, Folder + "/CharacterGauge.mat");
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
            Debug.Log("[캐릭터 게이지] HUD 적용. 12종 전용 문양은 미제작 상태이며 공용 장식을 사용합니다.");
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
                var definition = AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinitionSO>(
                    "Assets/10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_" + seed.id + ".asset");
                if (definition == null) throw new InvalidOperationException("캐릭터 정의 누락: " + seed.id);
                if (definition.gaugeVisualProfile != null) continue;
                definition.gaugeVisualProfile = profile;
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssetIfDirty(definition);
            }
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
            rootRect.anchoredPosition = new Vector2(0f, 60f);
            rootRect.sizeDelta = new Vector2(440f, 40f);
            foreach (string name in new[] { "LevelPanel", "SkillPanel" })
                if (root.transform.Find(name) is Transform child) child.gameObject.SetActive(false);
            var hp = root.transform.Find("HpPanel") as RectTransform;
            hp.anchoredPosition = Vector2.zero;
            hp.sizeDelta = new Vector2(440f, 28f);
            var hpText = serialized.FindProperty("_hpText").objectReferenceValue as TMP_Text;
            hpText.fontSize = 18f;
            Place(hpText.rectTransform, new Vector2(180f,24f), Vector2.zero);
            var hpFill = serialized.FindProperty("_boardHpFill").objectReferenceValue as Image;
            var hpTrail = serialized.FindProperty("_boardHpWhiteFill").objectReferenceValue as Image;
            Place(hpFill.rectTransform,new Vector2(360f,10f),Vector2.zero);
            Place(hpTrail.rectTransform,new Vector2(360f,10f),Vector2.zero);
            hpFill.color = new Color(.16f,.82f,.3f,1f);
            var hpFrame = hp.Find("HpFullBar").GetComponent<Image>();
            Place(hpFrame.rectTransform,new Vector2(400f,18f),Vector2.zero);
            hpFrame.color = new Color(.85f,.72f,.42f,1f);
            foreach (var hpImage in hp.GetComponentsInChildren<Image>(true))
                if (hpImage != hpFill && hpImage != hpTrail && hpImage != hpFrame) hpImage.enabled = false;
            var effects = root.transform.Find("EffectArea") as RectTransform;
            if (effects != null) { effects.anchoredPosition = new Vector2(0f, 145f); effects.sizeDelta = new Vector2(424f, 80f); }
            var gaugeRoot = Ensure("CharacterGauge", root.transform, new Vector2(240f, 60f), new Vector2(0f, 42f));
            var group = EnsureComponent<CanvasGroup>(gaugeRoot.gameObject);
            group.blocksRaycasts = false; group.interactable = false;
            var gauge = EnsureComponent<UICharacterGauge>(gaugeRoot.gameObject);
            var imageRect = Ensure("Silhouette", gaugeRoot, new Vector2(240f, 60f), Vector2.zero);
            var image = EnsureComponent<RawImage>(imageRect.gameObject);
            image.raycastTarget = false;
            image.texture = fallback.silhouetteSprite.texture; image.material = material;
            var ready = MakeImage("Ready", gaugeRoot, Skin + "UI_Etc/Toggle_Check_White_Icon_Check.png", new Vector2(13f, 13f), new Vector2(0f, -22f));
            var locked = MakeImage("Locked", gaugeRoot, Skin + "UI_Etc/InputField_Icon_Lock.png", new Vector2(13f, 15f), new Vector2(0f, -22f));
            var cooldown = MakeText("Cooldown", gaugeRoot, hpText, new Vector2(52f, 20f), new Vector2(40f, -22f));
            var gaugeData = new SerializedObject(gauge);
            SetRef(gaugeData, "_image", image); SetRef(gaugeData, "_group", group);
            SetRef(gaugeData, "_materialTemplate", material); SetRef(gaugeData, "_fallbackProfile", fallback);
            SetRef(gaugeData, "_readyMark", ready.gameObject); SetRef(gaugeData, "_lockedMark", locked.gameObject);
            SetRef(gaugeData, "_cooldownText", cooldown);
            gaugeData.ApplyModifiedPropertiesWithoutUndo();
            SetRef(serialized, "_characterGauge", gauge);
            SetRef(serialized, "_staminaText", null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ApplyStamina(root);
            ready.gameObject.SetActive(false); locked.gameObject.SetActive(false); cooldown.gameObject.SetActive(false);
        }

        private static void ApplyStamina(GameObject root)
        {
            var panel = root.transform.Find("StaminaPanel") as RectTransform;
            panel.anchoredPosition = new Vector2(80f, 400f);
            panel.sizeDelta = new Vector2(100f, 100f);
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true)) text.gameObject.SetActive(false);
            Sprite ring = LoadSprite(Skin + "Frame/BorderFrame_Circle.png");
            foreach (var image in panel.GetComponentsInChildren<Image>(true))
            {
                image.sprite = ring; image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Radial360; image.fillOrigin = 2;
                image.fillClockwise = true; image.fillAmount = .22f;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f,.5f);
                image.rectTransform.sizeDelta = new Vector2(100f,100f);
                image.rectTransform.anchoredPosition = Vector2.zero;
                image.rectTransform.localRotation = Quaternion.Euler(0,0,-50f);
                image.raycastTarget = false;
            }
        }

        private static void ApplySkills(GameObject root)
        {
            var hud = root.GetComponent<UI_HUD_Skill>();
            var data = new SerializedObject(hud);
            var names = new[] { "UISkillSlot_ElementalImbue", "UISkillSlot_Ability", "UISkillSlot_Ultimate" };
            var slots = data.FindProperty("_slots"); slots.arraySize = names.Length;
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(300f, 120f); rect.anchoredPosition = new Vector2(-40f, 35f);
            foreach (Transform child in root.transform) child.gameObject.SetActive(Array.IndexOf(names, child.name) >= 0);
            for (int i = 0; i < names.Length; i++)
            {
                var slot = root.transform.Find(names[i]).GetComponent<UISkillSlot>();
                slots.GetArrayElementAtIndex(i).objectReferenceValue = slot;
                var slotRect = (RectTransform)slot.transform;
                slotRect.anchorMin = slotRect.anchorMax = new Vector2(1f, 0f);
                slotRect.anchoredPosition = new Vector2(-250f + i * 95f, 60f);
                slotRect.sizeDelta = new Vector2(82f, 110f);
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
            SetPositions(data.FindProperty("_keyboardPositions"), false);
            SetPositions(data.FindProperty("_gamepadPositions"), true);
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
            Place(icon.rectTransform, new Vector2(40f, 40f), new Vector2(0f, 15f));
            icon.color = new Color(1f,.95f,.8f,1f);
            cooldown.transform.SetParent(slot.transform, false);
            Place(cooldown.rectTransform, new Vector2(70f, 22f), new Vector2(0f,-12f));
            Place((RectTransform)key.transform, new Vector2(80f, 28f), new Vector2(0f,-40f));
            foreach (var image in key.GetComponents<Image>()) image.enabled = false;
            foreach (Transform child in slot.transform)
                if (child != icon.transform && child != cooldown.transform && child != key.transform)
                    child.gameObject.SetActive(false);
            var reason = MakeText("UnavailableReason", slot.transform, label, new Vector2(90f,18f), new Vector2(0f,-64f));
            reason.fontSize = 12f;
            SetRef(data, "_unavailableReason", reason);
            SetRef(data, "_labelText", null); SetRef(data, "_readyGlow", null); SetRef(data, "_comboGlow", null);
            SetRef(data, "_cooldownRoot", null); SetRef(data, "_cooldownFill", null);
            SetRef(data, "_tweenTarget", icon.rectTransform);
            data.FindProperty("_showOnlyWhenGaugeFull").boolValue = false;
            data.FindProperty("_dimAlpha").floatValue = .5f;
            data.ApplyModifiedPropertiesWithoutUndo();
            ConfigureComboPrompt(key, reason);
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

        private static void SetPositions(SerializedProperty property, bool gamepad)
        {
            property.arraySize = 3;
            for(int i=0;i<3;i++) property.GetArrayElementAtIndex(i).vector2Value = new Vector2(-250f+i*95f,gamepad && i==2 ? 78f : 60f);
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
