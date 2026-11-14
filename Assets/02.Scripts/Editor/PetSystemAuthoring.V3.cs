using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Pet;
using UPlayGround.Gameplay.Tag;
using UPlayGround.UI;

namespace UPlayGround.EditorTools
{
    public static partial class PetSystemAuthoring
    {
        /// <summary>안정 ID와 참조를 유지하며 기존 지원 데이터만 V3 정책으로 이전한다.</summary>
        [UPlaygroundTool("PetSystem.ApplyV3P1")]
        public static void ApplyV3P1()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode에서 실행해야 합니다.");
            var catalog = AssetDatabase.LoadAssetAtPath<PetSkillCatalogSO>(DataRoot + "PetSkills.asset");
            if (catalog == null) throw new InvalidOperationException("펫 스킬 데이터가 없습니다.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                Undo.RecordObject(catalog, "펫 지원 정책 이전");
                catalog.supportMinimumInterval = 8f;
                for (int i = 0; i < catalog.skills.Count; i++)
                {
                    var skill = catalog.skills[i];
                    if (skill.supportAbility == null) continue;
                    ConfigureSupportPolicy(skill);
                    for (int j = 0; j < skill.levels.Count; j++)
                    {
                        var level = skill.levels[j];
                        var ability = level.ability;
                        var effect = level.Effect;
                        if (ability == null || effect == null) throw new InvalidOperationException("지원 레벨 참조가 누락됐습니다.");
                        Undo.RecordObject(ability, "펫 지원 쿨다운 이전");
                        Undo.RecordObject(effect, "펫 지원 효과 이전");
                        ability.cooldown.cooldownGroupId = "Pet.Support." + skill.skillId.Substring("Pet.Skill.".Length);
                        ability.cooldown.durationSeconds = skill.triggerPolicy.internalCooldown;
                        ability.cooldown.scope = AbilityCooldownScope.PlayerActor;
                        ability.presentation.displayName = effect.presentation.displayName = skill.displayName;
                        if (skill.triggerPolicy.kind == PetSupportTriggerKind.HealthBelow)
                            effect.healMaxHealthRatio = .08f + .02f * j;
                        EditorUtility.SetDirty(ability); EditorUtility.SetDirty(effect);
                    }
                }
                EditorUtility.SetDirty(catalog);
                foreach (var skill in catalog.skills)
                    foreach (var level in skill.levels)
                        if (level.ability != null) { Save(level.Effect); Save(level.ability); }
                Save(catalog);
                AuthorSupportIndicator(catalog);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                AssetDatabase.SaveAssets();
                throw;
            }
        }

        private static void ConfigureSupportPolicy(PetSkillDefinition skill)
        {
            var policy = new PetSupportTriggerPolicy { requiresCanSupport = true };
            switch (skill.skillId)
            {
                case PetSkillCatalogSO.Heal:
                    skill.displayName = "응급 회복";
                    policy.kind = PetSupportTriggerKind.HealthBelow; policy.threshold = .35f; policy.internalCooldown = 45f;
                    skill.activateFxKey = "PlayerHeal";
                    break;
                case PetSkillCatalogSO.AttackBuff:
                    skill.displayName = "기세";
                    policy.kind = PetSupportTriggerKind.PerfectDefense; policy.internalCooldown = 10f;
                    skill.activateFxKey = "playerFullChargeFX";
                    break;
                case PetSkillCatalogSO.DefenseBuff:
                    skill.displayName = "버팀";
                    policy.kind = PetSupportTriggerKind.HeavyHitTaken; policy.internalCooldown = 15f;
                    skill.activateFxKey = "playerGuardFX";
                    policy.hitTags.AddRange(new[] { GameplayTags.Trigger_Player_Hit_Heavy, GameplayTags.Trigger_Player_Hit_KnockBack,
                        GameplayTags.Trigger_Player_Hit_Stun, GameplayTags.Trigger_Player_Hit_Airborne,
                        GameplayTags.Trigger_Player_Hit_Knockdown, GameplayTags.Trigger_Player_Hit_Grab });
                    break;
                default: throw new InvalidOperationException("이전 대상이 아닌 지원 ID입니다: " + skill.skillId);
            }
            skill.triggerPolicy = policy;
        }

        private static void AuthorSupportIndicator(PetSkillCatalogSO catalog)
        {
            const string path = "Assets/03.Prefabs/UI/HUD/UI_HUD_PlayerInfo.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.GetComponentInChildren<UIPetSupportIndicator>(true) != null) return;
                var hud = new SerializedObject(root.GetComponent<UI_HUD_PlayerInfo>());
                var effectArea = (RectTransform)hud.FindProperty("_effectArea").objectReferenceValue;
                var container = RectObject("PetSupport", effectArea.parent, new Vector2(150, 44), Vector2.zero);
                var rect = (RectTransform)container.transform;
                rect.anchorMin = effectArea.anchorMin; rect.anchorMax = effectArea.anchorMax; rect.pivot = effectArea.pivot;
                rect.anchoredPosition = effectArea.anchoredPosition + Vector2.down * 52f;
                var icon = RectObject("Icon", rect, new Vector2(40, 40), new Vector2(-52, 0)).AddComponent<Image>();
                icon.sprite = catalog.Find(PetSkillCatalogSO.Heal).icon; icon.preserveAspect = true; icon.raycastTarget = false;
                var source = root.GetComponentInChildren<TextMeshProUGUI>(true);
                var label = UnityEngine.Object.Instantiate(source, rect);
                label.name = "Status"; label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f);
                label.rectTransform.pivot = new Vector2(.5f, .5f);
                label.rectTransform.sizeDelta = new Vector2(110, 40); label.rectTransform.anchoredPosition = new Vector2(30, 0);
                label.fontSize = 15; label.enableAutoSizing = false; label.text = "도움 대기"; label.raycastTarget = false;
                var indicator = container.AddComponent<UIPetSupportIndicator>();
                var serialized = new SerializedObject(indicator);
                serialized.FindProperty("_icon").objectReferenceValue = icon;
                serialized.FindProperty("_status").objectReferenceValue = label;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    /// <summary>정책 쿨다운을 편집하면 GAS 실행용 파생값을 함께 갱신한다.</summary>
    [CustomEditor(typeof(PetSkillCatalogSO))]
    public sealed class PetSkillCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (!EditorGUI.EndChangeCheck()) return;
            var catalog = (PetSkillCatalogSO)target;
            foreach (var skill in catalog.skills)
            {
                if (skill?.supportAbility == null || skill.triggerPolicy == null) continue;
                foreach (var level in skill.levels)
                {
                    var ability = level?.ability;
                    if (ability == null || ability.cooldown.durationSeconds == skill.triggerPolicy.internalCooldown) continue;
                    Undo.RecordObject(ability, "펫 지원 쿨다운 동기화");
                    ability.cooldown.durationSeconds = skill.triggerPolicy.internalCooldown;
                    EditorUtility.SetDirty(ability);
                }
            }
        }
    }
}
