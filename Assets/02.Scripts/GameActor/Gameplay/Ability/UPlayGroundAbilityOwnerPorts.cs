using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
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
        private readonly Dictionary<ulong, GameplayTagHandle> _tagHandles = new();
        private readonly Dictionary<ulong, object> _modifierSources = new();
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
            if (string.Equals(
                    resourceId,
                    UPlayGround.Data.Ability.AbilityResourceType.Health.ToString(),
                    StringComparison.Ordinal)
                && _owner is IDamageable damageable)
            {
                current = damageable.GetCurrentHealth();
                float percent = damageable.GetHealthPercent();
                maximum = percent > 0f ? current / percent : _owner?.Stats?.MaxHealth ?? 0f;
                return true;
            }
            if (!string.Equals(
                    resourceId,
                    UPlayGround.Data.Ability.AbilityResourceType.UltimateEnergy.ToString(),
                    StringComparison.Ordinal)
                || _owner is not PlayerActor player
                || player.SkillGauge == null)
                return false;

            current = player.SkillGauge.CurrentGauge;
            maximum = player.SkillGauge.MaxGauge;
            return true;
        }

        public bool TrySet(string resourceId, float value)
        {
            if (string.Equals(
                    resourceId,
                    UPlayGround.Data.Ability.AbilityResourceType.Health.ToString(),
                    StringComparison.Ordinal)
                && _owner is IDamageable damageable)
            {
                float delta = value - damageable.GetCurrentHealth();
                if (delta < 0f) return false;
                if (delta > 0f) damageable.Heal(delta);
                return true;
            }
            if (!TryGet(resourceId, out _, out float maximum)
                || _owner is not PlayerActor player)
                return false;
            player.SkillGauge.SetGauge(Mathf.Clamp(value, 0f, maximum));
            return true;
        }

        public bool Has(string tagId)
        {
            return TryParseTag(tagId, out GameplayTagId id)
                   && (_owner?.Tags?.HasTag(id) ?? false);
        }

        public AbilityTagHandle Add(string tagId, string sourceType, ulong sourceId)
        {
            if (!TryParseTag(tagId, out GameplayTagId id) || _owner?.Tags == null)
                return default;

            GameplayTagHandle projectHandle = _owner.Tags.AddTag(
                id,
                new GameplayTagSource(sourceType, sourceId));
            if (!projectHandle.IsValid) return default;

            ulong value = _nextTagHandle++;
            _tagHandles[value] = projectHandle;
            return new AbilityTagHandle(value);
        }

        public bool Remove(AbilityTagHandle handle)
        {
            if (!handle.IsValid
                || !_tagHandles.Remove(handle.Value, out GameplayTagHandle projectHandle)
                || _owner?.Tags == null)
                return false;
            return _owner.Tags.RemoveTag(projectHandle);
        }

        public AbilityModifierHandle AddModifier(
            string statId,
            AbilityModifierOperation operation,
            float magnitude,
            string sourceType,
            ulong sourceId)
        {
            if (_owner?.Stats == null
                || !Enum.TryParse(statId, out StatType statType))
                return default;

            ModifierType modifierType = operation switch
            {
                AbilityModifierOperation.Add => ModifierType.Flat,
                AbilityModifierOperation.Percent => ModifierType.Percent,
                AbilityModifierOperation.Multiply => ModifierType.Multiply,
                _ => throw new NotSupportedException(
                    $"UPlayGround는 '{operation}' Stat 연산을 지원하지 않습니다."),
            };
            ulong value = _nextModifierHandle++;
            var source = new object();
            _modifierSources[value] = source;
            _owner.Stats.AddModifier(new StatModifier(
                statType, modifierType, magnitude, source, -1f));
            return new AbilityModifierHandle(value);
        }

        public bool RemoveModifier(AbilityModifierHandle handle)
        {
            if (!handle.IsValid
                || !_modifierSources.Remove(handle.Value, out object source)
                || _owner?.Stats == null)
                return false;
            _owner.Stats.RemoveModifiersBySource(source);
            return true;
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
    }
}
