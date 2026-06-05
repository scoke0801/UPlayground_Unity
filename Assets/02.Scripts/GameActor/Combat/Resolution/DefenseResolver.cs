using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat
{
    public readonly struct PlayerDefenseQuery
    {
        public readonly bool IsGuarding;
        public readonly bool IsGuardState;
        public readonly bool IsAttackState;
        public readonly bool IsAttackCollisionActive;
        public readonly bool IsCurrentAttackParryCapable;
        public readonly bool IsPerfectDodgeState;
        public readonly bool IsPerfectDodgeWindow;
        public readonly bool CanTakeDamage;
        public readonly bool AlwaysParry;
        public readonly CombatDefensePolicySO Policy;

        public PlayerDefenseQuery(
            bool isGuarding,
            bool isGuardState,
            bool isAttackState,
            bool isAttackCollisionActive,
            bool isCurrentAttackParryCapable,
            bool isPerfectDodgeState,
            bool isPerfectDodgeWindow,
            bool canTakeDamage,
            bool alwaysParry,
            CombatDefensePolicySO policy = null)
        {
            IsGuarding = isGuarding;
            IsGuardState = isGuardState;
            IsAttackState = isAttackState;
            IsAttackCollisionActive = isAttackCollisionActive;
            IsCurrentAttackParryCapable = isCurrentAttackParryCapable;
            IsPerfectDodgeState = isPerfectDodgeState;
            IsPerfectDodgeWindow = isPerfectDodgeWindow;
            CanTakeDamage = canTakeDamage;
            AlwaysParry = alwaysParry;
            Policy = policy;
        }
    }

    public static class DefenseResolver
    {
        public static DefenseResult ResolvePlayerDefense(
            in PlayerDefenseQuery query,
            AttackData attackData)
        {
            AttackDefenseType defenseType = attackData?.defenseType ?? AttackDefenseType.Parryable;

            if (query.IsGuarding
                && query.IsGuardState
                && CombatPolicyResolver.CanGuard(query.Policy, defenseType))
            {
                return new DefenseResult(DefenseOutcome.Guarded, false);
            }

            if (CanParry(query, defenseType, attackData))
                return new DefenseResult(DefenseOutcome.Parried, false);

            if (!query.CanTakeDamage)
            {
                if (query.IsPerfectDodgeState
                    && query.IsPerfectDodgeWindow
                    && CombatPolicyResolver.CanPerfectDodge(query.Policy, defenseType))
                {
                    return new DefenseResult(DefenseOutcome.PerfectDodged, false);
                }

                return new DefenseResult(DefenseOutcome.Invincible, false);
            }

            return defenseType == AttackDefenseType.Unblockable
                ? new DefenseResult(DefenseOutcome.UnblockableHit, true)
                : DefenseResult.None;
        }

        private static bool CanParry(in PlayerDefenseQuery query, AttackDefenseType defenseType, AttackData attackData)
        {
            // 투사체/AOE는 전달 방식 자체가 패리·카운터 대상이 아니다(디버그 AlwaysParry보다 우선).
            if (attackData != null && attackData.isProjectile)
                return false;

            if (query.AlwaysParry)
                return CombatPolicyResolver.CanParry(query.Policy, defenseType);

            return query.IsAttackState
                   && query.IsAttackCollisionActive
                   && query.IsCurrentAttackParryCapable
                   && CombatPolicyResolver.CanParry(query.Policy, defenseType);
        }
    }
}
