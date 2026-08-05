using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Stat;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Gameplay.Ability
{
    /// <summary>
    /// 독립 Ability Core 포트를 UPlayGround의 GameActor 구현에 연결한다.
    /// 프로젝트 타입 변환은 이 어댑터 밖으로 노출하지 않는다.
    /// </summary>
    internal sealed class UPlayGroundAbilityOwnerPorts :
        IAbilityResourcePort,
        IAbilityTagPort,
        IAbilityStatPort
    {
        private readonly AbilitySystemComponent _abilitySystem;
        private readonly Dictionary<ulong, GameplayTagSourceHandle> _tagHandles = new();
        private readonly Dictionary<ulong, AttributeModifierHandle> _modifierHandles = new();
        private ulong _nextTagHandle = 1;
        private ulong _nextModifierHandle = 1;

        public UPlayGroundAbilityOwnerPorts(AbilitySystemComponent abilitySystem)
        {
            _abilitySystem = abilitySystem
                ?? throw new ArgumentNullException(nameof(abilitySystem));
        }

        public bool TryGet(string resourceId, out float current, out float maximum)
        {
            current = 0f;
            maximum = 0f;
            if (TryGetResourceAttributeIds(
                    resourceId, out AttributeId currentId, out AttributeId maximumId)
                && _abilitySystem.Attributes != null)
            {
                current = _abilitySystem.Attributes.GetCurrent(currentId);
                maximum = _abilitySystem.Attributes.GetCurrent(maximumId);
                return true;
            }
            return false;
        }

        public bool TrySet(string resourceId, float value)
        {
            if (!TryGet(resourceId, out float current, out float maximum)
                || !Enum.TryParse(resourceId, out UPlayGround.Data.Ability.AbilityResourceType resourceType))
                return false;
            float clamped = Mathf.Clamp(value, 0f, maximum);
            return _abilitySystem.ApplyResourceDelta(
                resourceType,
                clamped - current,
                $"GE_AbilityResourcePort.{resourceType}").Succeeded;
        }

        public bool Has(string tagId)
        {
            return Has(tagId, true);
        }

        public bool Has(string tagId, bool matchHierarchy)
        {
            return GameplayTagRegistry.TryResolve(tagId, out GameplayTag tag)
                   && (_abilitySystem.Tags?.Has(
                       new AbilityTagId(tag.TagName), matchHierarchy) ?? false);
        }

        public AbilityTagHandle Add(string tagId, string sourceType, ulong sourceId)
        {
            if (!GameplayTagRegistry.TryResolve(tagId, out GameplayTag tag)
                || _abilitySystem.Tags == null)
                return default;

            GameplayTagSourceHandle tagHandle = _abilitySystem.Tags.Add(
                new AbilityTagId(tag.TagName), sourceType, sourceId);
            if (!tagHandle.IsValid) return default;

            ulong value = _nextTagHandle++;
            _tagHandles[value] = tagHandle;
            return new AbilityTagHandle(value);
        }

        public bool Remove(AbilityTagHandle handle)
        {
            if (!handle.IsValid
                || !_tagHandles.Remove(handle.Value, out GameplayTagSourceHandle tagHandle)
                || _abilitySystem.Tags == null)
                return false;
            return _abilitySystem.Tags.Remove(tagHandle);
        }

        public AbilityModifierHandle AddModifier(
            string statId,
            AbilityModifierOperation operation,
            float magnitude,
            string sourceType,
            ulong sourceId)
        {
            if (!AttributeRegistry.TryResolve(
                    statId,
                    out AttributeReference reference)
                || _abilitySystem.Attributes == null)
                return default;
            AttributeId attributeId = reference.ToCoreId();
            ulong value = _nextModifierHandle++;
            AttributeModifierHandle attributeHandle = _abilitySystem.Attributes.AddModifier(
                attributeId,
                operation switch
                {
                    AbilityModifierOperation.Add => AttributeModifierOperation.Add,
                    AbilityModifierOperation.Percent => AttributeModifierOperation.Percent,
                    AbilityModifierOperation.Multiply => AttributeModifierOperation.Multiply,
                    AbilityModifierOperation.Override => AttributeModifierOperation.Override,
                    _ => throw new ArgumentOutOfRangeException(nameof(operation)),
                },
                magnitude,
                sourceType,
                sourceId);
            if (!attributeHandle.IsValid) return default;
            _modifierHandles[value] = attributeHandle;
            return new AbilityModifierHandle(value);
        }

        public bool RemoveModifier(AbilityModifierHandle handle)
        {
            if (!handle.IsValid
                || !_modifierHandles.Remove(handle.Value, out AttributeModifierHandle attributeHandle)
                || _abilitySystem.Attributes == null)
                return false;
            return _abilitySystem.Attributes.RemoveModifier(attributeHandle);
        }

        private static bool TryGetResourceAttributeIds(
            string resourceId,
            out AttributeId currentId,
            out AttributeId maximumId)
        {
            if (string.Equals(
                    resourceId,
                    UPlayGround.Data.Ability.AbilityResourceType.Health.ToString(),
                    StringComparison.Ordinal))
            {
                currentId = global::UPlayGround.Data.Stat.Attributes.Vital.Health;
                maximumId = global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth;
                return true;
            }
            if (string.Equals(
                    resourceId,
                    UPlayGround.Data.Ability.AbilityResourceType.UltimateEnergy.ToString(),
                    StringComparison.Ordinal))
            {
                currentId = global::UPlayGround.Data.Stat.Attributes.Resource.UltimateEnergy;
                maximumId = global::UPlayGround.Data.Stat.Attributes.Resource.MaxUltimateEnergy;
                return true;
            }
            if (string.Equals(
                    resourceId,
                    UPlayGround.Data.Ability.AbilityResourceType.Forte.ToString(),
                    StringComparison.Ordinal))
            {
                currentId = global::UPlayGround.Data.Stat.Attributes.Resource.Forte;
                maximumId = global::UPlayGround.Data.Stat.Attributes.Resource.MaxForte;
                return true;
            }
            if (string.Equals(
                    resourceId,
                    UPlayGround.Data.Ability.AbilityResourceType.Concerto.ToString(),
                    StringComparison.Ordinal))
            {
                currentId = global::UPlayGround.Data.Stat.Attributes.Resource.Concerto;
                maximumId = global::UPlayGround.Data.Stat.Attributes.Resource.MaxConcerto;
                return true;
            }
            currentId = default;
            maximumId = default;
            return false;
        }
    }
}
