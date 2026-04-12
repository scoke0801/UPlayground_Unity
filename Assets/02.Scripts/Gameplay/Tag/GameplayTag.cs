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

    /// <summary>
    /// 프로젝트 전역 GameplayTag 상수 모음.
    /// 새 태그 추가 시 여기에 등록하고 에디터에서도 동일 문자열 사용.
    /// </summary>
    public static class GameplayTags
    {
        // ── 이동 상태 ──────────────────────────────────────────────────
        public static readonly GameplayTag State_Move      = new("State.Move");
        public static readonly GameplayTag State_Sprint    = new("State.Sprint");
        public static readonly GameplayTag State_Dash      = new("State.Dash");
        public static readonly GameplayTag State_Jump      = new("State.Jump");
        public static readonly GameplayTag State_Airborne  = new("State.Airborne");
        public static readonly GameplayTag State_Crouching = new("State.Crouching");
        public static readonly GameplayTag State_Dodge     = new("State.Dodge");

        // ── 전투 상태 ──────────────────────────────────────────────────
        public static readonly GameplayTag State_Combat         = new("State.Combat");
        public static readonly GameplayTag State_Combat_Attack  = new("State.Combat.Attack");
        public static readonly GameplayTag State_Combat_Guard   = new("State.Combat.Guard");
        public static readonly GameplayTag State_Combat_Charge  = new("State.Combat.Charge");
        public static readonly GameplayTag State_Combat_DashAtk = new("State.Combat.DashAttack");
        public static readonly GameplayTag State_Combat_JumpAtk = new("State.Combat.JumpAttack");

        // ── 피격 / 사망 ────────────────────────────────────────────────
        public static readonly GameplayTag State_Hit     = new("State.Hit");
        public static readonly GameplayTag State_Death   = new("State.Death");
        public static readonly GameplayTag State_Grabbed = new("State.Grabbed");

        // ── 인터랙션 ───────────────────────────────────────────────────
        public static readonly GameplayTag State_Interaction = new("State.Interaction");

        // ── 콤보 입력 추적 ─────────────────────────────────────────────
        // PlayerCombat이 콤보 체인에서 각 히트마다 추가/제거
        public static readonly GameplayTag Combo_Light = new("Combo.Light");
        public static readonly GameplayTag Combo_Heavy = new("Combo.Heavy");
    }
}
