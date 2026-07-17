using System;
using UnityEngine;

namespace UPlayGround.Gameplay.Tag
{
    /// <summary>
    /// Unreal Engine GameplayTag 개념을 차용한 계층형 태그.
    /// '.'으로 구분된 계층 구조를 사용한다. 예) "State.Combat.Sprint"
    /// </summary>
    [Serializable]
    public struct GameplayTag : IEquatable<GameplayTag>
    {
        [SerializeField] private string _tagName;

        public string TagName => _tagName;

        public GameplayTag(string tagName) => _tagName = tagName ?? string.Empty;

        /// <summary>
        /// 이 태그가 parent의 자식(또는 동일)인지 확인.
        /// "State.Combat"은 "State"의 자식이지만 "State2"의 자식은 아님.
        /// </summary>
        public bool IsChildOf(GameplayTag parent)
        {
            if (string.IsNullOrEmpty(parent.TagName)) return false;
            return _tagName == parent.TagName
                || _tagName.StartsWith(parent.TagName + ".", StringComparison.Ordinal);
        }

        public bool IsValid() => !string.IsNullOrEmpty(_tagName);

        public bool Equals(GameplayTag other) =>
            string.Equals(_tagName, other._tagName, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is GameplayTag other && Equals(other);
        public override int GetHashCode() => _tagName?.GetHashCode() ?? 0;

        public static bool operator ==(GameplayTag a, GameplayTag b) => a.Equals(b);
        public static bool operator !=(GameplayTag a, GameplayTag b) => !a.Equals(b);

        public override string ToString() => _tagName ?? "(없음)";

        public static implicit operator GameplayTag(string tagName) => new(tagName);
    }
}
