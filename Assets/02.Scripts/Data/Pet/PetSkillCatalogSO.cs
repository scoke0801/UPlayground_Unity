using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Ability.Core;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Pet
{
    /// <summary>탐험 수치와 지원 GAS를 메뉴와 실행 양쪽에 공급한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/Pet/Skills")]
    public sealed class PetSkillCatalogSO : ScriptableObject
    {
        public const string Pickup = "Pet.Skill.Pickup";
        public const string PickupRange = "Pet.Skill.PickupRange";
        public const string Heal = "Pet.Skill.Heal";
        public const string AttackBuff = "Pet.Skill.AttackBuff";
        public const string DefenseBuff = "Pet.Skill.DefenseBuff";
        public static readonly string[] SkillIds = { Pickup, PickupRange, Heal, AttackBuff, DefenseBuff };
        public const int MaxLevel = 5;
        public List<PetSkillDefinition> skills = new();
        [Min(0f)] public float lootLeashMargin = 2f;
        [Min(0f)] public float supportDistance = 10f;
        [Min(0f)] public float supportMinimumInterval = 8f;
        [Min(0f)] public float supportFlashDuration = .6f;
        public LayerMask supportObstructionMask;
        public Sprite menuFrame;
        public Sprite buttonPlate;
        public Sprite titleDecoration;
        public PetSkillDefinition Find(string id)
        {
            for (int i = 0; i < skills.Count; i++)
                if (skills[i] != null && skills[i].skillId == id) return skills[i];
            return null;
        }
    }

    public enum PetSupportTriggerKind { HealthBelow, PerfectDefense, HeavyHitTaken }

    /// <summary>지원 신호와 체력 조건을 같은 데이터 계약으로 평가한다.</summary>
    [Serializable]
    public sealed class PetSupportTriggerPolicy
    {
        public PetSupportTriggerKind kind;
        [Range(0f, 1f)] public float threshold;
        [Min(0f)] public float internalCooldown;
        public bool requiresCanSupport = true;
        public bool requiresCombat;
        public List<GameplayTag> hitTags = new();

        /// <summary>다른 종류의 신호나 범위를 벗어난 체력은 반응시키지 않는다.</summary>
        public bool Matches(PetSupportTriggerKind signal, float healthRatio, bool inCombat)
            => kind == signal && (!requiresCombat || inCombat)
                && (kind != PetSupportTriggerKind.HealthBelow
                    || (!float.IsNaN(healthRatio) && healthRatio >= 0f && healthRatio <= threshold));

        /// <summary>저작된 피격 태그의 하위 사건만 큰 피격으로 인정한다.</summary>
        public bool MatchesHit(AbilityTagId tag)
        {
            for (int i = 0; i < hitTags.Count; i++)
                if (tag.IsChildOf(new AbilityTagId(hitTags[i].TagName))) return true;
            return false;
        }
    }

    [Serializable]
    public sealed class PetSkillDefinition
    {
        public string skillId;
        public string displayName;
        public Sprite icon;
        public GameplayAbilitySO supportAbility;
        public PetSupportTriggerPolicy triggerPolicy = new();
        [Tooltip("지원이 발동될 때 플레이어 위치에서 재생할 FX 키. 비우면 재생하지 않는다.")]
        public string activateFxKey;
        public string petActivateFxKey;
        public List<PetSkillLevel> levels = new();
        public PetSkillLevel GetLevel(int level) => level >= 1 && level <= levels.Count ? levels[level - 1] : null;

        /// <summary>화면을 연 뒤 비용이나 효과가 바뀐 오래된 강화 요청을 거절한다.</summary>
        public int GetRevision()
        {
            var hash = new HashCode();
            hash.Add(skillId); hash.Add(supportAbility); hash.Add(activateFxKey); hash.Add(petActivateFxKey);
            if (triggerPolicy != null)
            {
                hash.Add(triggerPolicy.kind); hash.Add(triggerPolicy.threshold);
                hash.Add(triggerPolicy.internalCooldown); hash.Add(triggerPolicy.requiresCanSupport);
                hash.Add(triggerPolicy.requiresCombat);
                foreach (var tag in triggerPolicy.hitTags) hash.Add(tag.TagName);
            }
            foreach (var level in levels)
            {
                if (level == null) { hash.Add(0); continue; }
                hash.Add(level.upgradeCost); hash.Add(level.explorationValue);
                hash.Add(level.ability);
                if (level.ability != null)
                {
                    hash.Add(level.ability.cooldown.durationSeconds);
                    hash.Add(level.ability.cooldown.cooldownGroupId);
                    hash.Add(level.ability.cooldown.scope);
                }
                var effect = level.Effect; hash.Add(effect);
                if (effect == null) continue;
                hash.Add(effect.durationSeconds); hash.Add(effect.healAmount); hash.Add(effect.healMaxHealthRatio);
                foreach (var modifier in effect.modifiers)
                {
                    if (modifier == null) { hash.Add(0); continue; }
                    hash.Add(modifier.attributeId); hash.Add(modifier.modifierType); hash.Add(modifier.magnitudeSource);
                    hash.Add(modifier.value); hash.Add(modifier.coefficient); hash.Add(modifier.perLevel);
                }
            }
            return hash.ToHashCode();
        }
    }

    [Serializable]
    public sealed class PetSkillLevel
    {
        public int upgradeCost;
        public float explorationValue;
        public GameplayAbilitySO ability;
        public GameplayEffectSO Effect => ability?.variants?.Count > 0
            && ability.variants[0].targetEffects.Count > 0 ? ability.variants[0].targetEffects[0] : null;
    }

    public readonly struct PetUpgradeRequest
    {
        public readonly string SkillId;
        public readonly int ExpectedLevel;
        public readonly int ExpectedCost;
        public readonly int DataRevision;
        public PetUpgradeRequest(string skillId, int level, int cost, int revision)
        { SkillId = skillId; ExpectedLevel = level; ExpectedCost = cost; DataRevision = revision; }
    }

    public static class PetUpgradeRules
    {
        /// <summary>레벨·비용·저작 상태가 화면에서 확인한 값과 일치할 때 한 단계만 허용한다.</summary>
        public static bool CanUpgrade(PetSkillDefinition skill, int actualLevel, PetUpgradeRequest request)
        {
            var level = skill?.GetLevel(actualLevel);
            return skill != null && skill.skillId == request.SkillId && actualLevel == request.ExpectedLevel
                && actualLevel >= 1 && actualLevel < PetSkillCatalogSO.MaxLevel && skill.GetLevel(actualLevel + 1) != null
                && level != null && level.upgradeCost > 0 && level.upgradeCost == request.ExpectedCost
                && skill.GetRevision() == request.DataRevision;
        }
    }
}
