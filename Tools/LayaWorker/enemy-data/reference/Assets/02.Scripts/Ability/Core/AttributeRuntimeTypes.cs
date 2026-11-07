using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Ability.Core
{
    /// <summary>
    /// 프로젝트 어댑터가 직렬화 문자열에 Attribute 선택 UI를 제공할 수 있게 하는
    /// Core 비의존 에디터 힌트.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AttributeIdSelectorAttribute : PropertyAttribute
    {
    }

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

    /// <summary>
    /// 런타임 세션 안에서만 유효한 Attribute 인터닝 핸들.
    /// 직렬화나 세이브에는 저장하지 않는다.
    /// </summary>
    public readonly struct AttributeHandle : IEquatable<AttributeHandle>
    {
        public int Index { get; }
        public bool IsValid => Index > 0;

        public AttributeHandle(int index) => Index = index;
        public bool Equals(AttributeHandle other) => Index == other.Index;
        public override bool Equals(object obj) =>
            obj is AttributeHandle other && Equals(other);
        public override int GetHashCode() => Index;
        public static bool operator ==(
            AttributeHandle left,
            AttributeHandle right) => left.Equals(right);
        public static bool operator !=(
            AttributeHandle left,
            AttributeHandle right) => !left.Equals(right);
    }

    public enum AttributeValueFormat
    {
        Flat,
        Percent01,
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
    /// 프로젝트 레지스트리가 Core에 전달하는 불변 Attribute 메타데이터.
    /// Core는 이 타입을 통해 Data/Resources 구현을 알지 않는다.
    /// </summary>
    public readonly struct AttributeMetadata
    {
        public string AttributeId { get; }
        public string DisplayName { get; }
        public AttributeValueFormat Format { get; }
        public string Unit { get; }
        public float DefaultBaseValue { get; }
        public AttributeClampPolicy ClampPolicy { get; }
        public float FixedMinimum { get; }
        public float FixedMaximum { get; }
        public string MinimumAttributeId { get; }
        public string MaximumAttributeId { get; }
        public string DependentResourceId { get; }
        public AttributeMaxChangePolicy MaxChangePolicy { get; }
        public bool SaveBaseValue { get; }
        public bool IsMetaAttribute { get; }

        public AttributeMetadata(
            string attributeId,
            string displayName,
            AttributeValueFormat format,
            string unit,
            float defaultBaseValue,
            AttributeClampPolicy clampPolicy,
            float fixedMinimum,
            float fixedMaximum,
            string minimumAttributeId,
            string maximumAttributeId,
            string dependentResourceId,
            AttributeMaxChangePolicy maxChangePolicy,
            bool saveBaseValue,
            bool isMetaAttribute)
        {
            AttributeId = attributeId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Format = format;
            Unit = unit ?? string.Empty;
            DefaultBaseValue = defaultBaseValue;
            ClampPolicy = clampPolicy;
            FixedMinimum = fixedMinimum;
            FixedMaximum = fixedMaximum;
            MinimumAttributeId = minimumAttributeId ?? string.Empty;
            MaximumAttributeId = maximumAttributeId ?? string.Empty;
            DependentResourceId = dependentResourceId ?? string.Empty;
            MaxChangePolicy = maxChangePolicy;
            SaveBaseValue = saveBaseValue;
            IsMetaAttribute = isMetaAttribute;
        }
    }

    /// <summary>
    /// 프로젝트 비의존 Core와 프로젝트 Attribute 레지스트리 사이의 포트.
    /// </summary>
    public interface IAttributeResolver
    {
        bool TryResolve(string attributeIdOrAlias, out AttributeHandle handle);
        bool TryGetMetadata(
            AttributeHandle handle,
            out AttributeMetadata metadata);
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
        [SerializeField, AttributeIdSelector] private string _attributeId;
        [SerializeField] private float _defaultBaseValue;
        [SerializeField] private AttributeClampPolicy _clampPolicy;
        [SerializeField] private float _fixedMinimum;
        [SerializeField] private float _fixedMaximum;
        [SerializeField, AttributeIdSelector] private string _minimumAttributeId;
        [SerializeField, AttributeIdSelector] private string _maximumAttributeId;
        [SerializeField, AttributeIdSelector] private string _dependentResourceId;
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
