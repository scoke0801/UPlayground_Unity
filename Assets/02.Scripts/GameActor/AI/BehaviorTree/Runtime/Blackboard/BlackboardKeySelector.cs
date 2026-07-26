using System;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 인스펙터에서 Blackboard 키를 타입 필터링된 드롭다운으로 고를 수 있게 해주는 직렬화 가능한 selector.
    /// stableId를 주 식별자로, key 문자열을 마이그레이션·디버그용 캐시 이름으로 보관한다.
    /// 제네릭 ScriptableObject/struct는 Unity 직렬화 한계로 인스펙터 표시가 막히기 때문에 이 방식을 채택한다.
    /// </summary>
    [Serializable]
    public struct BlackboardKeySelector : IEquatable<BlackboardKeySelector>
    {
        [SerializeField] private string _stableId;
        [SerializeField] private string _key;
        [SerializeField] private BlackboardValueType _expectedType;

        public BlackboardKeySelector(string key, BlackboardValueType expectedType)
        {
            // ScriptableObject의 직렬화 생성자/필드 초기화에서도 호출된다.
            // 이 시점에는 Resources.Load가 금지되므로 안정 ID 해석은 런타임 접근과
            // 에디터 마이그레이션 단계에 맡긴다.
            _stableId = string.Empty;
            _key = key;
            _expectedType = expectedType;
        }

        public BlackboardKeySelector(
            BlackboardKeyReference reference,
            BlackboardValueType expectedType)
        {
            _stableId = reference.StableId;
            _key = reference.KeyName;
            _expectedType = expectedType;
        }

        public string StableId => _stableId ?? string.Empty;
        public string Key => _key;
        public BlackboardKeyReference Reference =>
            BlackboardKeyReference.CreateResolved(_stableId, _key);
        public BlackboardValueType ExpectedType => _expectedType;
        public bool HasKey => !string.IsNullOrWhiteSpace(_key);

        public bool Resolve(Blackboard blackboard)
        {
            if (blackboard == null || !HasKey)
                return false;

            var entry = blackboard.FindEntry(Reference);
            return entry != null && entry.ValueType == _expectedType;
        }

        public bool Equals(BlackboardKeySelector other)
        {
            return Reference == other.Reference && _expectedType == other._expectedType;
        }

        public override bool Equals(object obj)
        {
            return obj is BlackboardKeySelector other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Reference.GetHashCode() * 397) ^ (int)_expectedType;
        }

        public override string ToString()
        {
            return HasKey ? $"{_key} ({_expectedType})" : $"<unset {_expectedType}>";
        }
    }
}
