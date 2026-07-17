using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Data.Combat
{
    /// <summary>
    /// 플레이어 스킬 입력 슬롯. 런타임/데이터/UI 모두 2슬롯으로 고정한다.
    /// </summary>
    public enum PlayerSkillSlot
    {
        Ability = 0,
        Ultimate = 1,
    }

    public enum SkillGroundCondition
    {
        Any,
        Grounded,
        Airborne,
    }

    public enum SkillCostPolicy
    {
        UseGaugeSlot,
        NoCost,
    }

    public enum SkillCooldownPolicy
    {
        UseGaugeSlot,
        NoCooldown,
    }

    [Serializable]
    public sealed class SkillVariantCondition
    {
        [Tooltip("지상/공중 조건")]
        public SkillGroundCondition groundCondition = SkillGroundCondition.Any;

        [Tooltip("현재 자원 게이지가 이 값 이상이어야 한다. 0이면 검사하지 않는다.")]
        [Min(0f)] public float minSkillGauge = 0f;

        [Tooltip("현재 자원 게이지가 최대치여야 한다.")]
        public bool requiresFullSkillGauge = false;

        [Tooltip("이 태그를 모두 보유해야 한다.")]
        public List<GameplayTagId> requiredTagIds = new();

        [Tooltip("이 태그 중 하나라도 보유하면 선택할 수 없다.")]
        public List<GameplayTagId> blockedTagIds = new();

        public bool IsMatch(in PlayerSkillContext context)
        {
            if (groundCondition == SkillGroundCondition.Grounded && !context.IsGrounded)
                return false;
            if (groundCondition == SkillGroundCondition.Airborne && context.IsGrounded)
                return false;

            if (minSkillGauge > 0f && context.CurrentSkillGauge < minSkillGauge)
                return false;

            if (requiresFullSkillGauge && (context.MaxSkillGauge <= 0f || context.CurrentSkillGauge < context.MaxSkillGauge))
                return false;

            if (!CheckTags(context.Tags))
                return false;

            return true;
        }

        private bool CheckTags(IGameplayTagReader tags)
        {
            if (tags == null)
                return requiredTagIds == null || requiredTagIds.Count == 0;

            if (requiredTagIds != null)
            {
                for (int i = 0; i < requiredTagIds.Count; i++)
                {
                    GameplayTagId id = requiredTagIds[i];
                    if (id == GameplayTagId.None) continue;
                    if (!tags.HasTag(id)) return false;
                }
            }

            if (blockedTagIds != null)
            {
                for (int i = 0; i < blockedTagIds.Count; i++)
                {
                    GameplayTagId id = blockedTagIds[i];
                    if (id == GameplayTagId.None) continue;
                    if (tags.HasTag(id)) return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public sealed class PlayerSkillVariant
    {
        [Tooltip("식별용 이름. 비워도 런타임에는 영향 없음.")]
        public string variantName = "Default";

        [Tooltip("이 Variant가 재생할 MotionSet AnimKey. None이면 attackInfo.baseInfo.animKey를 사용한다.")]
        public AnimKey animKey = AnimKey.None;

        [Tooltip("이 Variant의 공격 데이터. animKey만 다르게 쓰고 싶으면 baseInfo는 공유하고 animKey 필드만 오버라이드한다.")]
        public PlayerAttackInfo attackInfo = new();

        [Tooltip("Variant 선택 조건")]
        public SkillVariantCondition condition = new();

        [Tooltip("조건이 동시에 성립할 때 높은 값이 먼저 선택된다.")]
        public int priority = 0;

        public AnimKey ResolveAnimKey()
        {
            if (animKey != AnimKey.None)
                return animKey;
            return attackInfo?.baseInfo?.animKey ?? AnimKey.None;
        }

        public bool IsExecutable => attackInfo?.baseInfo != null && ResolveAnimKey() != AnimKey.None;
    }

    [Serializable]
    public sealed class PlayerSkillDefinition
    {
        public PlayerSkillSlot slot = PlayerSkillSlot.Ability;
        public string displayName = "Skill";
        public SkillCostPolicy costPolicy = SkillCostPolicy.NoCost;
        public SkillCooldownPolicy cooldownPolicy = SkillCooldownPolicy.UseGaugeSlot;
        public List<PlayerSkillVariant> variants = new();
    }

    public readonly struct PlayerSkillContext
    {
        public readonly bool IsGrounded;
        public readonly IGameplayTagReader Tags;
        public readonly float CurrentSkillGauge;
        public readonly float MaxSkillGauge;

        public PlayerSkillContext(
            bool isGrounded,
            IGameplayTagReader tags,
            float currentSkillGauge,
            float maxSkillGauge)
        {
            IsGrounded = isGrounded;
            Tags = tags;
            CurrentSkillGauge = currentSkillGauge;
            MaxSkillGauge = maxSkillGauge;
        }
    }

    public readonly struct PlayerSkillResolveResult
    {
        public readonly PlayerSkillDefinition Definition;
        public readonly PlayerSkillVariant Variant;
        public readonly PlayerAttackInfo AttackInfo;
        public readonly AnimKey AnimKey;

        public bool IsValid => AttackInfo?.baseInfo != null && AnimKey != AnimKey.None;

        public PlayerSkillResolveResult(
            PlayerSkillDefinition definition,
            PlayerSkillVariant variant,
            PlayerAttackInfo attackInfo,
            AnimKey animKey)
        {
            Definition = definition;
            Variant = variant;
            AttackInfo = attackInfo;
            AnimKey = animKey;
        }
    }

    public static class PlayerSkillResolver
    {
        public static bool TryResolve(
            PlayerAttackDataSO data,
            int skillSlot,
            in PlayerSkillContext context,
            out PlayerSkillResolveResult result)
        {
            result = default;

            if (!IsValidSlot(skillSlot) || data == null)
                return false;

            // 정의 우선(definition-authoritative): 슬롯에 정의가 있으면 그 정의로만 판정한다.
            // Variant 조건이 모두 실패해도 레거시로 폴백하지 않고 미발동(false)한다.
            // 레거시(skillAttackList) 폴백은 해당 슬롯에 정의가 아예 없을 때만 사용한다.
            PlayerSkillDefinition definition = FindDefinition(data.skillDefinitions, skillSlot);
            if (definition != null)
                return TryResolveFromDefinition(definition, context, out result);

            return TryResolveLegacy(data, skillSlot, out result);
        }

        private static bool TryResolveFromDefinition(
            PlayerSkillDefinition definition,
            in PlayerSkillContext context,
            out PlayerSkillResolveResult result)
        {
            result = default;
            if (definition?.variants == null || definition.variants.Count == 0)
                return false;

            PlayerSkillVariant best = null;
            int bestPriority = int.MinValue;

            for (int i = 0; i < definition.variants.Count; i++)
            {
                PlayerSkillVariant variant = definition.variants[i];
                if (variant == null || !variant.IsExecutable)
                    continue;

                if (variant.condition != null && !variant.condition.IsMatch(context))
                    continue;

                if (best != null && variant.priority <= bestPriority)
                    continue;

                best = variant;
                bestPriority = variant.priority;
            }

            if (best == null)
                return false;

            result = new PlayerSkillResolveResult(
                definition,
                best,
                best.attackInfo,
                best.ResolveAnimKey());
            return true;
        }

        private static bool TryResolveLegacy(
            PlayerAttackDataSO data,
            int skillSlot,
            out PlayerSkillResolveResult result)
        {
            result = default;
            if (data.skillAttackList == null || skillSlot < 0 || skillSlot >= data.skillAttackList.Count)
                return false;

            PlayerAttackInfo attackInfo = data.skillAttackList[skillSlot];
            AnimKey animKey = attackInfo?.baseInfo?.animKey ?? AnimKey.None;
            if (attackInfo?.baseInfo == null || animKey == AnimKey.None)
                return false;

            result = new PlayerSkillResolveResult(null, null, attackInfo, animKey);
            return true;
        }

        private static PlayerSkillDefinition FindDefinition(List<PlayerSkillDefinition> definitions, int skillSlot)
        {
            if (definitions == null)
                return null;

            for (int i = 0; i < definitions.Count; i++)
            {
                PlayerSkillDefinition definition = definitions[i];
                if (definition != null && (int)definition.slot == skillSlot)
                    return definition;
            }

            return null;
        }

        public static bool IsValidSlot(int skillSlot)
            => skillSlot >= 0 && skillSlot < 2;
    }
}
