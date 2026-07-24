using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Ability.Core
{
    [Serializable]
    public readonly struct AttributeId : IEquatable<AttributeId>, IComparable<AttributeId>
    {
        [SerializeField] private readonly string _value;

        public string Value => _value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(_value);

        public AttributeId(string value)
        {
            _value = value?.Trim() ?? string.Empty;
        }

        public bool Equals(AttributeId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AttributeId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public int CompareTo(AttributeId other) =>
            StringComparer.Ordinal.Compare(Value, other.Value);
        public override string ToString() => Value;

        public static bool operator ==(AttributeId left, AttributeId right) => left.Equals(right);
        public static bool operator !=(AttributeId left, AttributeId right) => !left.Equals(right);
        public static implicit operator AttributeId(string value) => new(value);
    }

    public static class AttributeIds
    {
        public static class Vital
        {
            public static readonly AttributeId Health = new("Vital.Health");
            public static readonly AttributeId MaxHealth = new("Vital.MaxHealth");
            public static readonly AttributeId HealthRegenRate = new("Vital.HealthRegenRate");
            public static readonly AttributeId Poise = new("Vital.Poise");
            public static readonly AttributeId MaxPoise = new("Vital.MaxPoise");
            public static readonly AttributeId PoiseRecoveryRate = new("Vital.PoiseRecoveryRate");
            public static readonly AttributeId PoiseRecoveryDelay = new("Vital.PoiseRecoveryDelay");
        }

        public static class Combat
        {
            public static readonly AttributeId AttackPower = new("Combat.AttackPower");
            public static readonly AttributeId Defense = new("Combat.Defense");
            public static readonly AttributeId CritRate = new("Combat.CritRate");
            public static readonly AttributeId CritMultiplier = new("Combat.CritMultiplier");
            public static readonly AttributeId AttackSpeed = new("Combat.AttackSpeed");
            public static readonly AttributeId DamageTakenMultiplier = new("Combat.DamageTakenMultiplier");
            public static readonly AttributeId InvincibleDurationMultiplier = new("Combat.InvincibleDurationMultiplier");
        }

        public static class Movement
        {
            public static readonly AttributeId MoveSpeed = new("Movement.MoveSpeed");
            public static readonly AttributeId DashDistance = new("Movement.DashDistance");
        }

        public static class Resource
        {
            public static readonly AttributeId UltimateEnergy = new("Resource.UltimateEnergy");
            public static readonly AttributeId MaxUltimateEnergy = new("Resource.MaxUltimateEnergy");
            public static readonly AttributeId GenerationMultiplier = new("Resource.GenerationMultiplier");
            public static readonly AttributeId Forte = new("Resource.Forte");
            public static readonly AttributeId Concerto = new("Resource.Concerto");
            public static readonly AttributeId SkillCharge = new("Resource.SkillCharge");
        }

        public static class Life
        {
            public static readonly AttributeId GatheringPower = new("Life.GatheringPower");
        }

        public static class Meta
        {
            public static readonly AttributeId IncomingDamage = new("Meta.IncomingDamage");
            public static readonly AttributeId IncomingHealing = new("Meta.IncomingHealing");
            public static readonly AttributeId IncomingPoiseDamage = new("Meta.IncomingPoiseDamage");
            public static readonly AttributeId IncomingBreakDamage = new("Meta.IncomingBreakDamage");
        }
    }

    public enum AttributeClampPolicy
    {
        None,
        FixedRange,
        AttributeRange,
    }

    public enum AttributeMaxChangePolicy
    {
        Clamp,
        PreserveRatio,
        PreserveAbsolute,
        FillOnIncrease,
        Refill,
    }

    public enum AttributeModifierOperation
    {
        Add,
        Percent,
        Multiply,
        Override,
    }

    /// <summary>
    /// 데이터 소스와 런타임 Effect 생성 경계에서 사용하는 안정 Attribute 보정값.
    /// 프로젝트 전용 enum이나 Unity 오브젝트 수명에 의존하지 않는다.
    /// </summary>
    public readonly struct AttributeModifierValue
    {
        public AttributeId AttributeId { get; }
        public AttributeModifierOperation Operation { get; }
        public float Value { get; }

        public AttributeModifierValue(
            AttributeId attributeId,
            AttributeModifierOperation operation,
            float value)
        {
            AttributeId = attributeId;
            Operation = operation;
            Value = value;
        }
    }

    [Serializable]
    public sealed class GameplayAttributeDefinition
    {
        [SerializeField] private string _attributeId;
        [SerializeField] private float _defaultBaseValue;
        [SerializeField] private AttributeClampPolicy _clampPolicy;
        [SerializeField] private float _fixedMinimum;
        [SerializeField] private float _fixedMaximum;
        [SerializeField] private string _minimumAttributeId;
        [SerializeField] private string _maximumAttributeId;
        [SerializeField] private string _dependentResourceId;
        [SerializeField] private AttributeMaxChangePolicy _maxChangePolicy = AttributeMaxChangePolicy.Clamp;
        [SerializeField] private bool _saveBaseValue;
        [SerializeField] private bool _isMetaAttribute;

        public AttributeId AttributeId => new(_attributeId);
        public float DefaultBaseValue => _defaultBaseValue;
        public AttributeClampPolicy ClampPolicy => _clampPolicy;
        public float FixedMinimum => _fixedMinimum;
        public float FixedMaximum => _fixedMaximum;
        public AttributeId MinimumAttributeId => new(_minimumAttributeId);
        public AttributeId MaximumAttributeId => new(_maximumAttributeId);
        public AttributeId DependentResourceId => new(_dependentResourceId);
        public AttributeMaxChangePolicy MaxChangePolicy => _maxChangePolicy;
        public bool SaveBaseValue => _saveBaseValue;
        public bool IsMetaAttribute => _isMetaAttribute;

        public GameplayAttributeDefinition(
            AttributeId attributeId,
            float defaultBaseValue,
            AttributeClampPolicy clampPolicy = AttributeClampPolicy.None,
            float fixedMinimum = 0f,
            float fixedMaximum = float.MaxValue,
            AttributeId minimumAttributeId = default,
            AttributeId maximumAttributeId = default,
            AttributeId dependentResourceId = default,
            AttributeMaxChangePolicy maxChangePolicy = AttributeMaxChangePolicy.Clamp,
            bool saveBaseValue = false,
            bool isMetaAttribute = false)
        {
            _attributeId = attributeId.Value;
            _defaultBaseValue = defaultBaseValue;
            _clampPolicy = clampPolicy;
            _fixedMinimum = fixedMinimum;
            _fixedMaximum = fixedMaximum;
            _minimumAttributeId = minimumAttributeId.Value;
            _maximumAttributeId = maximumAttributeId.Value;
            _dependentResourceId = dependentResourceId.Value;
            _maxChangePolicy = maxChangePolicy;
            _saveBaseValue = saveBaseValue;
            _isMetaAttribute = isMetaAttribute;
        }
    }

    [CreateAssetMenu(
        fileName = "AttributeSet_",
        menuName = "UPlayGround/Ability/Attribute Set Definition")]
    public sealed class AttributeSetDefinitionSO : ScriptableObject
    {
        [SerializeField] private string _setId;
        [SerializeField] private List<GameplayAttributeDefinition> _attributes = new();

        public string SetId => _setId?.Trim() ?? string.Empty;
        public IReadOnlyList<GameplayAttributeDefinition> Attributes => _attributes;
    }

    public readonly struct GameplayAttributeValue
    {
        public float BaseValue { get; }
        public float CurrentValue { get; }

        public GameplayAttributeValue(float baseValue, float currentValue)
        {
            BaseValue = baseValue;
            CurrentValue = currentValue;
        }
    }

    public readonly struct AttributeModifierHandle : IEquatable<AttributeModifierHandle>
    {
        public ulong Value { get; }
        public bool IsValid => Value != 0;

        public AttributeModifierHandle(ulong value) => Value = value;
        public bool Equals(AttributeModifierHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AttributeModifierHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public readonly struct AttributeTransactionHandle : IEquatable<AttributeTransactionHandle>
    {
        public ulong Value { get; }
        public bool IsValid => Value != 0;

        public AttributeTransactionHandle(ulong value) => Value = value;
        public bool Equals(AttributeTransactionHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AttributeTransactionHandle other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }

    public readonly struct AttributeChangedEvent
    {
        public AttributeId AttributeId { get; }
        public float OldBase { get; }
        public float NewBase { get; }
        public float OldCurrent { get; }
        public float NewCurrent { get; }
        public AttributeTransactionHandle TransactionHandle { get; }
        public ulong SourceSpecHandle { get; }

        public AttributeChangedEvent(
            AttributeId attributeId,
            float oldBase,
            float newBase,
            float oldCurrent,
            float newCurrent,
            AttributeTransactionHandle transactionHandle,
            ulong sourceSpecHandle)
        {
            AttributeId = attributeId;
            OldBase = oldBase;
            NewBase = newBase;
            OldCurrent = oldCurrent;
            NewCurrent = newCurrent;
            TransactionHandle = transactionHandle;
            SourceSpecHandle = sourceSpecHandle;
        }
    }

    public interface IAttributeReader
    {
        bool TryGet(AttributeId id, out GameplayAttributeValue value);
        event Action<AttributeChangedEvent> AttributeChanged;
    }
}
