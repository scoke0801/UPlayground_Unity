using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Combat
{
    [CreateAssetMenu(fileName = "CombatDefensePolicy", menuName = "UPlayGround/Combat/Defense Policy")]
    public class CombatDefensePolicySO : ScriptableObject
    {
        [Tooltip("Unblockable 공격을 가드 상태에서 막을 수 있는지. 꺼두면 Guarded보다 UnblockableHit가 우선한다.")]
        public bool allowGuardAgainstUnblockable;

        [Tooltip("Unblockable 공격을 공격 중 패리로 막을 수 있는지.")]
        public bool allowParryAgainstUnblockable;

        [Tooltip("Unblockable 공격도 퍼펙트 도지 성공으로 무효화할 수 있는지.")]
        public bool allowPerfectDodgeAgainstUnblockable = true;

        public bool CanGuard(AttackDefenseType defenseType)
            => defenseType != AttackDefenseType.Unblockable || allowGuardAgainstUnblockable;

        public bool CanParry(AttackDefenseType defenseType)
            => defenseType != AttackDefenseType.Unblockable || allowParryAgainstUnblockable;

        public bool CanPerfectDodge(AttackDefenseType defenseType)
            => defenseType != AttackDefenseType.Unblockable || allowPerfectDodgeAgainstUnblockable;
    }
}
