using System;
using System.Globalization;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class BlackboardCompareNode : BTConditionNode
    {
        [SerializeField] private string _key;
        [SerializeField] private BlackboardComparisonType _comparison = BlackboardComparisonType.Equal;
        [SerializeField] private string _value;
        [SerializeField] private string _valueKey;
        [SerializeField] private bool _ignoreCase = true;

        public string Key
        {
            get => _key;
            set => _key = value;
        }

        public BlackboardComparisonType Comparison
        {
            get => _comparison;
            set => _comparison = value;
        }

        public string Value
        {
            get => _value;
            set => _value = value;
        }

        public string ValueKey
        {
            get => _valueKey;
            set => _valueKey = value;
        }

        public bool IgnoreCase
        {
            get => _ignoreCase;
            set => _ignoreCase = value;
        }

        protected override BTStatus OnUpdate()
        {
            if (Context?.Blackboard == null)
                return BTStatus.Failure;

            var entry = Context.Blackboard.FindEntry(_key);
            if (entry == null)
            {
                Context.DebugTrace?.Record(this, "BlackboardCompare", BTStatus.Failure, $"키를 찾을 수 없음: {_key}");
                return BTStatus.Failure;
            }

            if (entry.ValueType == BlackboardValueType.Object)
            {
                Context.DebugTrace?.Record(this, "BlackboardCompare", BTStatus.Failure, $"Object 타입은 비교 불가: {_key}");
                return BTStatus.Failure;
            }

            var success = entry.ValueType switch
            {
                BlackboardValueType.Bool => CompareBool(entry.BoolValue),
                BlackboardValueType.Int => CompareInt(entry.IntValue),
                BlackboardValueType.Float => CompareFloat(entry.FloatValue),
                BlackboardValueType.String => CompareString(entry.StringValue),
                _ => false
            };
            var status = success ? BTStatus.Success : BTStatus.Failure;
            Context.DebugTrace?.Record(this, "BlackboardCompare", status, $"{_key} {FormatComparison()}");
            return status;
        }

        private bool CompareBool(bool left)
        {
            if (_comparison is not (BlackboardComparisonType.Equal or BlackboardComparisonType.NotEqual))
            {
                Context.DebugTrace?.Record(this, "BlackboardCompare", BTStatus.Failure, $"Bool 키에 산술 비교({_comparison}) 사용 불가: {_key}");
                return false;
            }

            if (!TryResolveBool(out var right))
                return false;

            return _comparison == BlackboardComparisonType.Equal ? left == right : left != right;
        }

        private bool CompareInt(int left)
        {
            if (!TryResolveInt(out var right))
                return false;

            return _comparison switch
            {
                BlackboardComparisonType.Equal => left == right,
                BlackboardComparisonType.NotEqual => left != right,
                BlackboardComparisonType.Less => left < right,
                BlackboardComparisonType.LessOrEqual => left <= right,
                BlackboardComparisonType.Greater => left > right,
                BlackboardComparisonType.GreaterOrEqual => left >= right,
                _ => false
            };
        }

        private bool CompareFloat(float left)
        {
            return TryResolveFloat(out var right) && CompareNumber(left, right);
        }

        private bool CompareString(string left)
        {
            if (_comparison is not (BlackboardComparisonType.Equal or BlackboardComparisonType.NotEqual))
            {
                Context.DebugTrace?.Record(this, "BlackboardCompare", BTStatus.Failure, $"String 키에 산술 비교({_comparison}) 사용 불가: {_key}");
                return false;
            }

            if (!TryResolveString(out var right))
                return false;

            var comparison = _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var equals = string.Equals(left, right, comparison);
            return _comparison == BlackboardComparisonType.Equal ? equals : !equals;
        }

        private bool CompareNumber(float left, float right)
        {
            return _comparison switch
            {
                BlackboardComparisonType.Equal => Mathf.Approximately(left, right),
                BlackboardComparisonType.NotEqual => !Mathf.Approximately(left, right),
                BlackboardComparisonType.Less => left < right,
                BlackboardComparisonType.LessOrEqual => left <= right,
                BlackboardComparisonType.Greater => left > right,
                BlackboardComparisonType.GreaterOrEqual => left >= right,
                _ => false
            };
        }

        private bool TryResolveBool(out bool value)
        {
            if (!string.IsNullOrWhiteSpace(_valueKey))
                return Context.Blackboard.TryGetBool(_valueKey, out value);

            return bool.TryParse(_value, out value);
        }

        private bool TryResolveInt(out int value)
        {
            if (!string.IsNullOrWhiteSpace(_valueKey))
                return Context.Blackboard.TryGetInt(_valueKey, out value);

            return int.TryParse(_value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private bool TryResolveFloat(out float value)
        {
            if (!string.IsNullOrWhiteSpace(_valueKey))
                return Context.Blackboard.TryGetFloat(_valueKey, out value);

            return float.TryParse(_value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private bool TryResolveString(out string value)
        {
            if (!string.IsNullOrWhiteSpace(_valueKey))
                return Context.Blackboard.TryGetString(_valueKey, out value);

            value = _value ?? string.Empty;
            return true;
        }

        private string FormatComparison()
        {
            var right = string.IsNullOrWhiteSpace(_valueKey) ? _value : $"${_valueKey}";
            return $"{_comparison} {right}";
        }
    }
}
