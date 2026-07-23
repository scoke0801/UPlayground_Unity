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
        private readonly GameActor _owner;
        private readonly Dictionary<ulong, GameplayTagSourceHandle> _tagHandles = new();
        private readonly Dictionary<ulong, AttributeModifierHandle> _modifierHandles = new();
        private ulong _nextTagHandle = 1;
        private ulong _nextModifierHandle = 1;

        public UPlayGroundAbilityOwnerPorts(GameActor owner)
        {
            _owner = owner;
        }

        public bool TryGet(string resourceId, out float current, out float maximum)
        {
            current = 0f;
            maximum = 0f;
            if (TryGetResourceAttributeIds(
                    resourceId, out AttributeId currentId, out AttributeId maximumId)
                && _owner?.AbilitySystem?.Attributes != null)
            {
                current = _owner.AbilitySystem.Attributes.GetCurrent(currentId);
                maximum = _owner.AbilitySystem.Attributes.GetCurrent(maximumId);
                return true;
            }
            return false;
        }

        public bool TrySet(string resourceId, float value)
        {
            if (!TryGet(resourceId, out float current, out float maximum)
                || _owner?.AbilitySystem == null
                || !Enum.TryParse(resourceId, out UPlayGround.Data.Ability.AbilityResourceType resourceType))
                return false;
            float clamped = Mathf.Clamp(value, 0f, maximum);
            return _owner.AbilitySystem.ApplyResourceDelta(
                resourceType,
                clamped - current,
                $"GE_LegacyResourcePort.{resourceType}").Succeeded;
        }

        public bool Has(string tagId)
        {
            return TryParseTag(tagId, out GameplayTagId id)
                   && (_owner?.AbilitySystem?.Tags?.Has(
                       new AbilityTagId(id.ToTag().TagName)) ?? false);
        }

        public AbilityTagHandle Add(string tagId, string sourceType, ulong sourceId)
        {
            if (!TryParseTag(tagId, out GameplayTagId id)
                || _owner?.AbilitySystem?.Tags == null)
                return default;

            GameplayTagSourceHandle tagHandle = _owner.AbilitySystem.Tags.Add(
                new AbilityTagId(id.ToTag().TagName), sourceType, sourceId);
            if (!tagHandle.IsValid) return default;

            ulong value = _nextTagHandle++;
            _tagHandles[value] = tagHandle;
            return new AbilityTagHandle(value);
        }

        public bool Remove(AbilityTagHandle handle)
        {
            if (!handle.IsValid
                || !_tagHandles.Remove(handle.Value, out GameplayTagSourceHandle tagHandle)
                || _owner?.AbilitySystem?.Tags == null)
                return false;
            return _owner.AbilitySystem.Tags.Remove(tagHandle);
        }

        public AbilityModifierHandle AddModifier(
            string statId,
            AbilityModifierOperation operation,
            float magnitude,
            string sourceType,
            ulong sourceId)
        {
            if (_owner?.AbilitySystem?.Attributes == null
                || !Enum.TryParse(statId, out StatType statType))
                return default;
            ulong value = _nextModifierHandle++;
            AttributeModifierHandle attributeHandle = _owner.AbilitySystem.Attributes.AddModifier(
                UPlayGroundAttributeMapping.GetAttributeId(statType),
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
                || _owner?.AbilitySystem?.Attributes == null)
                return false;
            return _owner.AbilitySystem.Attributes.RemoveModifier(attributeHandle);
        }

        private static bool TryParseTag(string tagId, out GameplayTagId id)
        {
            if (string.IsNullOrWhiteSpace(tagId)
                || !Enum.TryParse(tagId, out id)
                || id == GameplayTagId.None)
            {
                id = GameplayTagId.None;
                return false;
            }
            return true;
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
                currentId = AttributeIds.Vital.Health;
                maximumId = AttributeIds.Vital.MaxHealth;
                return true;
            }
            if (string.Equals(
                    resourceId,
                    UPlayGround.Data.Ability.AbilityResourceType.UltimateEnergy.ToString(),
                    StringComparison.Ordinal))
            {
                currentId = AttributeIds.Resource.UltimateEnergy;
                maximumId = AttributeIds.Resource.MaxUltimateEnergy;
                return true;
            }
            currentId = default;
            maximumId = default;
            return false;
        }
    }
}
