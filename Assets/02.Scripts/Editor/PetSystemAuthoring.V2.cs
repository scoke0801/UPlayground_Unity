using System;
using System.IO;
using System.Linq;
using Animancer;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Animation;
using UPlayGround.Ability.Core;
using UPlayGround.Gameplay.Tag;
using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Stat;
using UPlayGround.UI;
using UPlayGround.UI.InputPrompt;

namespace UPlayGround.EditorTools
{
    public static partial class PetSystemAuthoring
    {
        [UPlaygroundTool("PetSystem.ApplyV2")]
        public static void ApplyV2()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
            var catalog = AssetDatabase.LoadAssetAtPath<PetCatalogSO>(DataRoot + "PetCatalog.asset");
            if (catalog == null) throw new InvalidOperationException("1차 펫 카탈로그가 없습니다.");
            var skills = LoadOrCreate<PetSkillCatalogSO>(DataRoot + "PetSkills.asset");
            skills.supportObstructionMask = LayerMask.GetMask("Ground");
            skills.menuFrame = ImportSprite("Frame", new Vector4(96, 96, 96, 96));
            skills.buttonPlate = ImportSprite("Button", new Vector4(170, 90, 170, 90));
            skills.titleDecoration = ImportSprite("Title", Vector4.zero);
            if (skills.skills.Count == 0) AuthorSkills(skills);
            catalog.skills = skills;
            foreach (var definition in catalog.definitions)
            {
                if (definition == null) continue;
                definition.modelId = "Monkey";
                definition.previewPrefab = CreatePreviewPrefab();
                definition.previewIdle = AssetDatabase.LoadAllAssetsAtPath(ClipRoot + "Anim_Chibi@IdleA.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
                var follow = definition.followProfile;
                follow.returnDistance = 20f; follow.recallDelay = 1f; follow.recallCooldown = 3f;
                follow.recoveryInterval = 1f;
                if (skills.Find(PetSkillCatalogSO.PickupRange).GetLevel(5).explorationValue + skills.lootLeashMargin >= follow.returnDistance)
                    throw new InvalidOperationException("회수 leash는 일반 복귀 거리보다 작아야 합니다.");
                Save(follow); Save(definition);
            }
            var motions = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(DataRoot + "MonkeyMotions.asset");
            var sit = CreateMotion("Sit");
            // 마지막 앉기 자세를 유지하며 공용 FBX의 loop 설정은 바꾸지 않는다.
            sit.motionSet = MotionSet.CreateAuthored("Pet.Sit", MotionSectionEndPolicy.Hold);
            var sitClip = AssetDatabase.LoadAllAssetsAtPath(ClipRoot + "Anim_Chibi@Sit.fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
            sit.motionSet.motions.Add(new UPlayGround.Animation.Motion { id = Guid.NewGuid().ToString("N"), motionName = "Sit", motionClip = sitClip });
            foreach (var definition in catalog.definitions) { definition.seatedMotion = sit; Save(definition); }
            Save(sit); Save(motions); Save(skills); Save(catalog);
            AuthorSeat(); ExtendMenu(skills); AddTabPrompts(); ApplyGraphLayout(); RepairMenuFooter();
            AssetDatabase.Refresh();
        }

        private static void AuthorSkills(PetSkillCatalogSO catalog)
        {
            string[] names = { "아이템 줍기", "줍기 범위", "응급 회복", "기세", "버팀" };
            float[][] values = {
                new[] { 1f, .8f, .6f, .4f, .2f }, new[] { 8f, 10f, 12f, 14f, 16f },
                new[] { .08f, .10f, .12f, .14f, .16f }, new[] { .04f, .06f, .08f, .10f, .12f },
                new[] { .06f, .09f, .12f, .15f, .18f } };
            int[] costs = { 100, 250, 500, 1000, 0 };
            for (int skillIndex = 0; skillIndex < PetSkillCatalogSO.SkillIds.Length; skillIndex++)
            {
                string id = PetSkillCatalogSO.SkillIds[skillIndex];
                string key = id.Substring("Pet.Skill.".Length);
                var skill = new PetSkillDefinition { skillId = id, displayName = names[skillIndex], icon = ImportSprite(key, Vector4.zero),
                    activateFxKey = id == PetSkillCatalogSO.Heal ? "PlayerHeal" : null };
                if (skillIndex >= 2) ConfigureSupportPolicy(skill);
                for (int level = 0; level < PetSkillCatalogSO.MaxLevel; level++)
                {
                    var entry = new PetSkillLevel { upgradeCost = costs[level] };
                    if (skillIndex < 2) entry.explorationValue = values[skillIndex][level];
                    else
                    {
                        var effect = LoadOrCreate<GameplayEffectSO>(DataRoot + $"GE_Pet_{key}_L{level + 1}.asset");
                        effect.effectId = $"Pet.Effect.{key}.L{level + 1}";
                        effect.stackingKey = "Pet.Support." + key;
                        effect.stackPolicy = GameplayEffectStackPolicy.RejectNew; effect.maxStackCount = 1;
                        effect.polarity = GameplayEffectPolarity.Beneficial;
                        effect.durationType = skillIndex == 2 ? GameplayEffectDurationType.Instant : GameplayEffectDurationType.Duration;
                        effect.durationSeconds = skillIndex == 2 ? 0f : 8f;
                        effect.ignorePassiveDurationModifiers = true;
                        effect.removalPolicy = GameplayEffectRemovalPolicy.PersistOnPlayerActor;
                        effect.savePolicy = GameplayEffectSavePolicy.SaveRemainingDuration;
                        effect.presentation.displayName = names[skillIndex]; effect.presentation.icon = skill.icon;
                        effect.modifiers.Clear();
                        if (skillIndex == 2)
                        {
                            effect.instantExecution = GameplayEffectInstantExecution.Healing;
                            effect.healMaxHealthRatio = values[skillIndex][level];
                        }
                        else effect.modifiers.Add(new GameplayEffectModifierDefinition
                        {
                            attributeId = skillIndex == 2 ? "Vital.Health" : skillIndex == 3 ? "Combat.AttackPower" : "Combat.Defense",
                            modifierType = skillIndex == 2 ? ModifierType.Flat : ModifierType.Percent,
                            magnitudeSource = skillIndex == 2 ? GameplayEffectMagnitudeSource.AttributeBased : GameplayEffectMagnitudeSource.Fixed,
                            sourceAttributeId = skillIndex == 2 ? "Vital.MaxHealth" : null,
                            captureSource = GameplayEffectCaptureSource.Target,
                            coefficient = values[skillIndex][level], value = skillIndex == 2 ? 0f : values[skillIndex][level]
                        });
                        var ability = LoadOrCreate<GameplayAbilitySO>(DataRoot + $"GA_Pet_{key}_L{level + 1}.asset");
                        ability.abilityId = $"Pet.Support.{key}.L{level + 1}";
                        ability.executionMode = AbilityExecutionMode.InstantEffect;
                        ability.presentation.displayName = names[skillIndex]; ability.presentation.icon = skill.icon;
                        ability.presentation.category = AbilityCategory.Support;
                        ability.activation.targetRelation = AbilityTargetRelation.Self;
                        ability.activation.targetPolicy = AbilityTargetPolicy.Required;
                        ability.concurrency = AbilityConcurrencyPolicy.Background;
                        ability.persistence.backgroundMaxDurationSeconds = 1f;
                        ability.cooldown.cooldownGroupId = "Pet.Support." + key;
                        ability.cooldown.scope = AbilityCooldownScope.PlayerActor;
                        ability.cooldown.durationSeconds = skill.triggerPolicy.internalCooldown;
                        ability.variants.Clear();
                        ability.variants.Add(new AbilityVariantDefinition { variantId = "Default", targetEffects = new() { effect } });
                        entry.ability = ability;
                        if (level == 0) skill.supportAbility = ability;
                        Save(effect); Save(ability);
                    }
                    skill.levels.Add(entry);
                }
                catalog.skills.Add(skill);
            }
        }
        private static Sprite ImportSprite(string name, Vector4 border)
        {
            string path = "Assets/04.Images/UI/Pet/" + name + ".png";
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("ImageGen 리소스 누락: " + path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            var textureSettings = new TextureImporterSettings(); importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(textureSettings);
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = name is "Frame" or "Button" or "Title" ? 2048 : 256;
            importer.isReadable = true; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            // 원본의 투명 여백은 유지하고 Sprite rect만 알파 경계로 저작한다.
            var pixels = texture.GetPixels32();
            int minX = texture.width, minY = texture.height, maxX = 0, maxY = 0;
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a > 32)
                    { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
            if (minX >= maxX) throw new InvalidOperationException("투명도/도상 검수 실패: " + path);
            // 원본 픽셀 좌표로 rect를 기록한다(아이콘 importer의 축소는 별개).
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            float scaleX = (float)width / texture.width, scaleY = (float)height / texture.height;
            importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618
            importer.spritesheet = new[] { new SpriteMetaData { name = name, alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(.5f, .5f), border = border,
                rect = new Rect(minX * scaleX, minY * scaleY, (maxX - minX + 1) * scaleX, (maxY - minY + 1) * scaleY) } };
#pragma warning restore CS0618
            importer.isReadable = false; importer.SaveAndReimport();
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
        }
        private static GameObject CreatePreviewPrefab()
        {
            const string path = PrefabRoot + "Pet_Monkey_Preview.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var original = AssetDatabase.LoadAssetAtPath<GameObject>(MonkeyRoot + "Prefab/Characters/Monkey/Chibi_Monkey_00.prefab");
            var preview = UnityEngine.Object.Instantiate(original);
            try
            {
                foreach (var behaviour in preview.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(behaviour);
                foreach (var collider in preview.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                var animator = preview.GetComponentInChildren<Animator>();
                animator.applyRootMotion = false; animator.runtimeAnimatorController = null;
                var animancer = preview.AddComponent<AnimancerComponent>(); animancer.Animator = animator;
                preview.name = "Pet_Monkey_Preview";
                return PrefabUtility.SaveAsPrefabAsset(preview, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(preview); }
        }
        private static void AuthorSeat()
        {
            const string path = "Assets/03.Prefabs/Traversal/PlayerGlider.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view = root.GetComponent<PlayerGliderView>();
                var data = new SerializedObject(view);
                var canopy = (Transform)data.FindProperty("_canopy").objectReferenceValue;
                var seat = canopy.Find("PetSeatAnchor");
                if (seat == null)
                {
                    seat = new GameObject("PetSeatAnchor").transform; seat.SetParent(canopy, false);
                    // 25% Sit 자세의 중앙 접촉면을 날개 보강대 상단에 맞춘다.
                    seat.localPosition = new Vector3(0f, .07f, .25f);
                }
                SetReference(view, "_petSeatAnchor", seat);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void Save(UnityEngine.Object asset)
        { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }

        [UPlaygroundTool("PetSystem.AddV2TabPrompts")]
        public static void AddTabPrompts()
        {
            const string path = "Assets/03.Prefabs/UI/Scene/Pet/UI_Scene_PetMenu.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var skills = AssetDatabase.LoadAssetAtPath<PetSkillCatalogSO>(DataRoot + "PetSkills.asset");
                if (root.GetComponent<GraphicRaycaster>() == null) root.AddComponent<GraphicRaycaster>();
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    var rect = (RectTransform)button.transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                    button.transition = Selectable.Transition.ColorTint;
                    var colors = button.colors;
                    colors.normalColor = Color.white; colors.highlightedColor = new Color(.78f, 1f, .9f);
                    colors.selectedColor = new Color(.78f, 1f, .9f); colors.pressedColor = new Color(.64f, .88f, .77f);
                    colors.disabledColor = new Color(.6f, .6f, .6f, .8f); button.colors = colors;
                    var image = button.targetGraphic as Image;
                    if (image != null)
                    { image.overrideSprite = null; image.sprite = skills.buttonPlate; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 6f; }
                    var label = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                    { label.enableAutoSizing = true; label.fontSizeMin = 16f; label.fontSizeMax = 22f; label.textWrappingMode = TextWrappingModes.NoWrap; }
                    if (button.GetComponent<UITabButton>() == null)
                    {
                        var common = button.GetComponent<UICommonButton>() ?? button.gameObject.AddComponent<UICommonButton>();
                        SetReference(common, "_button", button); SetReference(common, "_background", image);
                        SetReference(common, "_buttonText", button.GetComponentInChildren<TextMeshProUGUI>());
                        SetReference(common, "_visualTarget", rect);
                        var commonData = new SerializedObject(common); commonData.FindProperty("_tintBackground").boolValue = false;
                        commonData.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                var tabs = root.transform.Find("PetTabs");
                for (int i = 0; i < 2; i++)
                {
                    string name = "TabPrompt" + i;
                    if (tabs.Find(name) != null) continue;
                    var icon = RectObject(name, tabs, new Vector2(36, 36), new Vector2(i == 0 ? -420 : 420, 0)).AddComponent<Image>();
                    var prompt = icon.gameObject.AddComponent<UIInputPromptIcon>();
                    var serialized = new SerializedObject(prompt);
                    serialized.FindProperty("_mapName").stringValue = "UI";
                    serialized.FindProperty("_actionName").stringValue = i == 0 ? "MainTabPrevious" : "MainTabNext";
                    serialized.FindProperty("_deviceFilter").enumValueIndex = (int)DevicePromptFilter.GamepadOnly;
                    serialized.FindProperty("_iconImage").objectReferenceValue = icon;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        /// <summary>기존 나가기 영역을 두 버튼으로 나누고 이미 분리된 배치는 보존한다.</summary>
        private static void RepairMenuFooter()
        {
            const string path = "Assets/03.Prefabs/UI/HUD/UI_Scene_MenuPanel.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var menu = new SerializedObject(root.GetComponent<UI_Scene_MenuPanel>());
                var exit = (menu.FindProperty("_exitButton").objectReferenceValue as Button)?.transform as RectTransform;
                var pet = (menu.FindProperty("_petButton").objectReferenceValue as Button)?.transform as RectTransform;
                if (exit == null || pet == null || exit.parent != pet.parent) return;
                float distance = Mathf.Abs(exit.anchoredPosition.x - pet.anchoredPosition.x);
                if (distance >= (exit.sizeDelta.x + pet.sizeDelta.x) * .5f) return;
                const float gap = 40f;
                Vector2 center = exit.anchoredPosition;
                float width = (exit.sizeDelta.x - gap) * .5f;
                exit.sizeDelta = pet.sizeDelta = new Vector2(width, exit.sizeDelta.y);
                exit.anchoredPosition = center + Vector2.right * ((width + gap) * .5f);
                pet.anchoredPosition = center - Vector2.right * ((width + gap) * .5f);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ExtendMenu(PetSkillCatalogSO skills)
        {
            const string path = "Assets/03.Prefabs/UI/Scene/Pet/UI_Scene_PetMenu.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var menu = root.GetComponent<UI_Scene_PetMenu>();
                if (root.transform.Find("V2ModelPage") != null) return;
                s_font = root.GetComponentsInChildren<TextMeshProUGUI>(true).First(t => t.font != null).font;
                s_buttonTemplate = root.GetComponentsInChildren<Button>(true).First().gameObject;
                root.GetComponent<Image>().sprite = skills.menuFrame;
                root.GetComponent<Image>().color = Color.white;
                var modelPage = RectObject("V2ModelPage", root.transform, new Vector2(980, 420), new Vector2(0, -5));
                var skillPage = RectObject("V2SkillPage", root.transform, new Vector2(980, 420), new Vector2(0, -5));
                var appearances = root.transform.Find("Appearances");
                appearances.SetParent(modelPage.transform, false);
                var grid = appearances.GetComponent<GridLayoutGroup>(); grid.constraintCount = 2;
                grid.cellSize = new Vector2(145, 58); grid.spacing = new Vector2(14, 12);
                var appearanceRect = (RectTransform)appearances; appearanceRect.sizeDelta = new Vector2(320, 350); appearanceRect.anchoredPosition = new Vector2(270, 0);
                var previewImage = RectObject("Preview", modelPage.transform, new Vector2(360, 350), new Vector2(-240, 0)).AddComponent<RawImage>();
                var preview = previewImage.gameObject.AddComponent<UICharacterPreviewRenderer>();
                SetReference(preview, "_display", previewImage);
                var model = Button(modelPage.transform, "Model", "원숭이", new Vector2(-240, -190), new Vector2(300, 50));
                var apply = Button(modelPage.transform, "Apply", "이 모습으로 함께하기", new Vector2(270, -190), new Vector2(310, 50));
                var tabs = RectObject("PetTabs", root.transform, new Vector2(800, 55), new Vector2(0, 205)).AddComponent<UITabGroup>();
                var tabData = new SerializedObject(tabs); var tabList = tabData.FindProperty("_tabs"); tabList.arraySize = 2;
                for (int i = 0; i < 2; i++)
                {
                    var button = Button(tabs.transform, "Tab" + i, i == 0 ? "모델 / 외형" : "스킬", new Vector2(i == 0 ? -210 : 210, 0), new Vector2(360, 50));
                    var tab = button.gameObject.AddComponent<UITabButton>();
                    SetReference(tab, "_button", button); SetReference(tab, "_background", button.GetComponent<Image>());
                    SetReference(tab, "_label", button.GetComponentInChildren<TextMeshProUGUI>());
                    tabList.GetArrayElementAtIndex(i).objectReferenceValue = tab;
                }
                tabData.ApplyModifiedPropertiesWithoutUndo();
                var skillRoot = RectObject("SkillList", skillPage.transform, new Vector2(380, 400), new Vector2(-270, 0));
                var layout = skillRoot.AddComponent<VerticalLayoutGroup>(); layout.spacing = 12; layout.childControlHeight = false; layout.childControlWidth = false;
                var template = Button(skillRoot.transform, "SkillTemplate", "", Vector2.zero, new Vector2(380, 66));
                var icon = RectObject("SkillIcon", template.transform, new Vector2(54, 54), new Vector2(-155, 0)).AddComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false;
                var labelRect = template.GetComponentInChildren<TextMeshProUGUI>().rectTransform;
                labelRect.offsetMin += new Vector2(60, 0);
                template.gameObject.SetActive(false);
                var detail = Text(skillPage.transform, "SkillDetail", "", new Vector2(480, 270), new Vector2(230, 50), 22);
                detail.alignment = TextAlignmentOptions.TopLeft;
                var select = Button(skillPage.transform, "SelectSkill", "", new Vector2(220, -115), new Vector2(420, 55));
                var upgrade = Button(skillPage.transform, "Upgrade", "한 단계 강화", new Vector2(220, -185), new Vector2(420, 55));
                var retry = Button(root.transform, "RetrySave", "저장 다시 시도", new Vector2(360, -270), new Vector2(250, 46));
                root.transform.Find("PetName").GetComponent<RectTransform>().anchoredPosition = new Vector2(-300, 270);
                root.transform.Find("PetName").GetComponent<RectTransform>().sizeDelta = new Vector2(300, 45);
                root.transform.Find("Bond").GetComponent<RectTransform>().anchoredPosition = new Vector2(120, 270);
                root.transform.Find("Bond").GetComponent<RectTransform>().sizeDelta = new Vector2(540, 45);
                root.transform.Find("Status").GetComponent<RectTransform>().sizeDelta = new Vector2(950, 60);
                root.transform.Find("Status").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -235);
                var ornament = RectObject("TitleDecoration", root.transform, new Vector2(760, 70), new Vector2(0, 320)).AddComponent<Image>();
                ornament.sprite = skills.titleDecoration; ornament.preserveAspect = true; ornament.raycastTarget = false;
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    var image = button.GetComponent<Image>();
                    if (image != null) { image.sprite = skills.buttonPlate; image.type = Image.Type.Sliced; image.color = Color.white; }
                    foreach (var label in button.GetComponentsInChildren<TextMeshProUGUI>()) label.color = new Color(.08f, .13f, .12f);
                }
                SetReference(menu, "_tabs", tabs); SetReference(menu, "_modelPage", modelPage); SetReference(menu, "_skillPage", skillPage);
                SetReference(menu, "_preview", preview); SetReference(menu, "_model", model); SetReference(menu, "_apply", apply);
                SetReference(menu, "_skillTemplate", template); SetReference(menu, "_skillRoot", skillRoot.transform);
                SetReference(menu, "_skillDetail", detail); SetReference(menu, "_selectSkill", select); SetReference(menu, "_upgrade", upgrade);
                SetReference(menu, "_retrySave", retry);
                skillPage.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
