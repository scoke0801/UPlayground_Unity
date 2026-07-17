using System;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 인스펙터에서 Blackboard 키를 타입 필터링된 드롭다운으로 고를 수 있게 해주는 직렬화 가능한 selector.
    /// 내부적으로는 string 키를 보관하므로 기존 SetXxx/TryGetXxx API와 그대로 호환된다.
    /// 제네릭 ScriptableObject/struct는 Unity 직렬화 한계로 인스펙터 표시가 막히기 때문에 이 방식을 채택한다.
    /// </summary>
    [Serializable]
    public struct BlackboardKeySelector : IEquatable<BlackboardKeySelector>
    {
        [SerializeField] private string _key;
        [SerializeField] private BlackboardValueType _expectedType;

        public BlackboardKeySelector(string key, BlackboardValueType expectedType)
        {
            _key = key;
            _expectedType = expectedType;
        }

        public string Key => _key;
        public BlackboardValueType ExpectedType => _expectedType;
        public bool HasKey => !string.IsNullOrWhiteSpace(_key);

        public bool Resolve(Blackboard blackboard)
        {
            if (blackboard == null || !HasKey)
                return false;

            var entry = blackboard.FindEntry(_key);
            return entry != null && entry.ValueType == _expectedType;
        }

        public bool Equals(BlackboardKeySelector other)
        {
            return _key == other._key && _expectedType == other._expectedType;
        }

        public override bool Equals(object obj)
        {
            return obj is BlackboardKeySelector other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((_key?.GetHashCode() ?? 0) * 397) ^ (int)_expectedType;
        }

        public override string ToString()
        {
            return HasKey ? $"{_key} ({_expectedType})" : $"<unset {_expectedType}>";
        }
    }
}
