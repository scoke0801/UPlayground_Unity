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

        private GameplayTag(string tagName) => _tagName = tagName;

        /// <summary>
        /// 정적 코드 의미 슬롯을 위한 생성 경로.
        /// Unity 객체 생성자에서 Resources.Load가 호출되지 않도록 값만 만든다.
        /// 이 경로의 등록 여부는 빌드 검증기가 전수 검사한다.
        /// </summary>
        internal static GameplayTag CreateCodeDefined(string tagName) =>
            new(tagName);

        internal static GameplayTag CreateRegistered(string tagName)
        {
            if (!GameplayTagRegistry.IsRegistered(tagName))
            {
                throw new ArgumentException(
                    $"GameplayTagRegistry에 등록되지 않은 태그입니다: '{tagName}'",
                    nameof(tagName));
            }

            return new GameplayTag(tagName);
        }

        /// <summary>
        /// 이 태그가 parent의 자식(또는 동일)인지 확인.
        /// "State.Combat"은 "State"의 자식이지만 "State2"의 자식은 아님.
        /// </summary>
        public bool IsChildOf(GameplayTag parent)
        {
            if (!IsValid() || !parent.IsValid()) return false;
            return _tagName == parent.TagName
                || _tagName.StartsWith(parent.TagName + ".", StringComparison.Ordinal);
        }

        public bool IsValid() => GameplayTagRegistry.IsRegistered(_tagName);

        public bool Equals(GameplayTag other) =>
            string.Equals(_tagName, other._tagName, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is GameplayTag other && Equals(other);
        public override int GetHashCode() => _tagName?.GetHashCode() ?? 0;

        public static bool operator ==(GameplayTag a, GameplayTag b) => a.Equals(b);
        public static bool operator !=(GameplayTag a, GameplayTag b) => !a.Equals(b);

        public override string ToString() => _tagName ?? "(없음)";
    }
}
