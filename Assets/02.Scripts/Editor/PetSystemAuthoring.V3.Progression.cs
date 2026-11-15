using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Pet;
using UPlayGround.Data.Stat;
using UPlayGround.UI;

namespace UPlayGround.EditorTools
{
    public static partial class PetSystemAuthoring
    {
        /// <summary>원본 GUID를 보존하고 신규 성장·교란 GAS와 화면 참조를 저작한다.</summary>
        [UPlaygroundTool("PetSystem.ApplyV3P2P3")]
        public static void ApplyV3P2P3()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<PetSkillCatalogSO>(DataRoot + "PetSkills.asset");
                if (catalog == null) throw new InvalidOperationException("펫 스킬 데이터가 없습니다.");
                Undo.RecordObject(catalog, "펫 유대·교란 저작");
                catalog.bondProgression = LoadOrCreateV3<PetBondProgressionSO>(DataRoot + "PetBondProgression.asset");
                foreach (var skill in catalog.skills)
                    if (skill.supportAbility != null && skill.target != PetSupportTarget.BrokenEnemy) ConfigureSupportPolicy(skill);
                var template = catalog.Find(PetSkillCatalogSO.AttackBuff);
                var signatureEffect = LoadOrCreateV3<GameplayEffectSO>(DataRoot + "GE_Pet_Signature.asset");
                Undo.RecordObject(signatureEffect, "펫 유대 효과 저작");
                signatureEffect.effectId = "Pet.Effect.Signature";
                signatureEffect.stackingKey = "Pet.Signature";
                signatureEffect.durationType = GameplayEffectDurationType.Duration;
                signatureEffect.durationSeconds = 3f;
                signatureEffect.ignorePassiveDurationModifiers = true;
                signatureEffect.polarity = GameplayEffectPolarity.Beneficial;
                signatureEffect.removalPolicy = GameplayEffectRemovalPolicy.PersistOnPlayerActor;
                signatureEffect.savePolicy = GameplayEffectSavePolicy.SaveRemainingDuration;
                signatureEffect.presentation.displayName = "가벼운 손놀림";
                signatureEffect.presentation.icon = template.icon;
                signatureEffect.modifiers.Clear();
                signatureEffect.modifiers.Add(new GameplayEffectModifierDefinition { attributeId = "Combat.AttackSpeed",
                    modifierType = ModifierType.Percent, magnitudeSource = GameplayEffectMagnitudeSource.Fixed, value = .03f });
                var signature = AuthorSupportAbility("Signature", "가벼운 손놀림", 0, template.supportAbility,
                    signatureEffect, 60f, AbilityTargetRelation.Self);
                catalog.signatureAbility = signature;
                catalog.signatureFxKey = "playerFullChargeFX";
                var disruption = catalog.Find(PetSkillCatalogSO.Disruption);
                if (disruption == null)
                {
                    disruption = new PetSkillDefinition { skillId = PetSkillCatalogSO.Disruption };
                    catalog.skills.Add(disruption);
                }
                disruption.displayName = "달려들기";
                disruption.icon = catalog.Find(PetSkillCatalogSO.PickupRange).icon;
                disruption.family = PetSupportFamily.Disruption;
                disruption.target = PetSupportTarget.BrokenEnemy;
                disruption.activateFxKey = "playerGuardFX";
                disruption.petActivateFxKey = "playerFullChargeFX";
                disruption.triggerPolicy = new PetSupportTriggerPolicy { kind = PetSupportTriggerKind.EnemyBroken,
                    internalCooldown = 20f, requiresCanSupport = true, requiresCombat = true };
                disruption.levels.Clear();
                int[] costs = { 100, 250, 500, 1000, 0 };
                for (int i = 0; i < PetSkillCatalogSO.MaxLevel; i++)
                {
                    var effect = LoadOrCreateV3<PetDisruptionEffectSO>(DataRoot + $"GE_Pet_Disruption_L{i + 1}.asset");
                    Undo.RecordObject(effect, "펫 교란 효과 저작");
                    effect.effectId = $"Pet.Effect.Disruption.L{i + 1}";
                    effect.stackingKey = "Pet.Support.Disruption";
                    effect.durationType = GameplayEffectDurationType.Duration; effect.durationSeconds = 4f;
                    effect.distractionSeconds = 1f + .2f * i;
                    effect.polarity = GameplayEffectPolarity.Harmful;
                    effect.ignorePassiveDurationModifiers = true;
                    effect.removalPolicy = GameplayEffectRemovalPolicy.RemoveOnSwap;
                    effect.savePolicy = GameplayEffectSavePolicy.DoNotSave;
                    effect.presentation.displayName = disruption.displayName; effect.presentation.icon = disruption.icon;
                    effect.modifiers.Clear();
                    effect.modifiers.Add(new GameplayEffectModifierDefinition { attributeId = "Vital.PoiseRecoveryDelay",
                        modifierType = ModifierType.Flat, magnitudeSource = GameplayEffectMagnitudeSource.Fixed, value = 1f + .25f * i });
                    var ability = AuthorSupportAbility("Disruption", disruption.displayName, i + 1,
                        template.supportAbility, effect, disruption.triggerPolicy.internalCooldown, AbilityTargetRelation.Enemy);
                    disruption.levels.Add(new PetSkillLevel { upgradeCost = costs[i], ability = ability });
                    if (i == 0) disruption.supportAbility = ability;
                    Save(effect); Save(ability);
                }
                foreach (var skill in catalog.skills)
                    if (skill.supportAbility != null && string.IsNullOrWhiteSpace(skill.petActivateFxKey))
                        skill.petActivateFxKey = "playerFullChargeFX";
                Save(signatureEffect); Save(signature); Save(catalog.bondProgression); Save(catalog);
                AuthorProgressionMenu(); AuthorSecondSupportIndicator();
                Undo.CollapseUndoOperations(group);
            }
            catch { Undo.RevertAllDownToGroup(group); AssetDatabase.SaveAssets(); throw; }
        }

        private static T LoadOrCreateV3<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, "펫 V3 데이터 생성");
            return asset;
        }

        private static GameplayAbilitySO AuthorSupportAbility(string key, string displayName, int level,
            GameplayAbilitySO template, GameplayEffectSO effect, float cooldown, AbilityTargetRelation relation)
        {
            string suffix = level > 0 ? $"_L{level}" : "";
            var ability = LoadOrCreateV3<GameplayAbilitySO>(DataRoot + $"GA_Pet_{key}{suffix}.asset");
            Undo.RecordObject(ability, "펫 지원 GAS 저작");
            EditorUtility.CopySerialized(template, ability);
            ability.abilityId = "Pet.Support." + key + (level > 0 ? $".L{level}" : "");
            ability.name = "GA_Pet_" + key + suffix;
            ability.presentation.displayName = displayName;
            ability.presentation.icon = effect.presentation.icon;
            ability.activation.targetRelation = relation;
            ability.cooldown.cooldownGroupId = "Pet.Support." + key;
            ability.cooldown.durationSeconds = cooldown;
            ability.cooldown.scope = AbilityCooldownScope.PlayerActor;
            ability.variants.Clear();
            ability.variants.Add(new AbilityVariantDefinition { variantId = "Default", targetEffects = new() { effect } });
            return ability;
        }

        private static void AuthorProgressionMenu()
        {
            const string path = "Assets/03.Prefabs/UI/Scene/Pet/UI_Scene_PetMenu.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var menu = root.GetComponent<UI_Scene_PetMenu>();
                var data = new SerializedObject(menu);
                var select = (Button)data.FindProperty("_selectSkill").objectReferenceValue;
                var second = select.transform.parent.Find("SelectSecondSkill")?.GetComponent<Button>();
                if (second == null) { second = UnityEngine.Object.Instantiate(select, select.transform.parent); second.name = "SelectSecondSkill"; }
                data.FindProperty("_selectSecondSkill").objectReferenceValue = second;
                var detail = (TextMeshProUGUI)data.FindProperty("_skillDetail").objectReferenceValue;
                Stretch(detail.transform, Vector2.zero, Vector2.one, new Vector2(48, 300), new Vector2(-48, -54));
                detail.fontSize = 30; detail.enableAutoSizing = true; detail.fontSizeMin = 22; detail.fontSizeMax = 30;
                Stretch(select.transform, Vector2.zero, new Vector2(1, 0), new Vector2(48, 206), new Vector2(-48, 270));
                Stretch(second.transform, Vector2.zero, new Vector2(1, 0), new Vector2(48, 124), new Vector2(-48, 188));
                Stretch(((Button)data.FindProperty("_upgrade").objectReferenceValue).transform,
                    Vector2.zero, new Vector2(1, 0), new Vector2(48, 42), new Vector2(-48, 106));
                var slots = data.FindProperty("_skillSlots");
                var graph = ((RectTransform)slots.GetArrayElementAtIndex(0).objectReferenceValue).parent;
                slots.arraySize = 6;
                Vector2[] anchors = { new(.3f, .73f), new(.7f, .73f), new(.12f, .22f), new(.37f, .22f), new(.62f, .22f), new(.87f, .22f) };
                for (int i = 0; i < 6; i++)
                {
                    var slot = graph.Find("Slot" + i) ?? RectObject("Slot" + i, graph, new Vector2(250, 248), Vector2.zero).transform;
                    Pin(slot, anchors[i], Vector2.zero, new Vector2(250, 248)); slots.GetArrayElementAtIndex(i).objectReferenceValue = slot;
                }
                var paths = data.FindProperty("_skillPaths");
                if (paths.arraySize < 6)
                {
                    var original = (UISkillTreeCurve)paths.GetArrayElementAtIndex(4).objectReferenceValue;
                    var pathView = UnityEngine.Object.Instantiate(original, original.transform.parent); pathView.name = "Path5";
                    paths.arraySize = 6; paths.GetArrayElementAtIndex(5).objectReferenceValue = pathView;
                }
                var hub = (RectTransform)data.FindProperty("_skillHubs").GetArrayElementAtIndex(1).objectReferenceValue;
                hub.GetComponent<TextMeshProUGUI>().text = "함께 받는 도움";
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void AuthorSecondSupportIndicator()
        {
            const string path = "Assets/03.Prefabs/UI/HUD/UI_HUD_PlayerInfo.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var first = root.GetComponentInChildren<UIPetSupportIndicator>(true);
                if (first == null) throw new InvalidOperationException("기존 펫 지원 HUD가 없습니다.");
                var parent = first.transform.parent;
                var second = parent.Find("PetSupportSecond")?.GetComponent<UIPetSupportIndicator>();
                if (second == null) { second = UnityEngine.Object.Instantiate(first, parent); second.name = "PetSupportSecond"; }
                ((RectTransform)second.transform).anchoredPosition = ((RectTransform)first.transform).anchoredPosition + Vector2.down * 46f;
                var data = new SerializedObject(second); data.FindProperty("_slotIndex").intValue = 1; data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
