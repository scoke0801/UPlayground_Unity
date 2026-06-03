using UPlayGround.Data;
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

        public PlayerDefenseQuery(
            bool isGuarding,
            bool isGuardState,
            bool isAttackState,
            bool isAttackCollisionActive,
            bool isCurrentAttackParryCapable,
            bool isPerfectDodgeState,
            bool isPerfectDodgeWindow,
            bool canTakeDamage,
            bool alwaysParry)
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
        }
    }

    public static class DefenseResolver
    {
        public static DefenseResult ResolvePlayerDefense(
            in PlayerDefenseQuery query,
            AttackData attackData)
        {
            if (query.IsGuarding && query.IsGuardState)
                return new DefenseResult(DefenseOutcome.Guarded, false);

            if (CanParry(query))
                return new DefenseResult(DefenseOutcome.Parried, false);

            if (!query.CanTakeDamage)
            {
                if (query.IsPerfectDodgeState && query.IsPerfectDodgeWindow)
                    return new DefenseResult(DefenseOutcome.PerfectDodged, false);

                return new DefenseResult(DefenseOutcome.Invincible, false);
            }

            return attackData?.defenseType == AttackDefenseType.Unblockable
                ? new DefenseResult(DefenseOutcome.UnblockableHit, true)
                : DefenseResult.None;
        }

        private static bool CanParry(in PlayerDefenseQuery query)
        {
            if (query.AlwaysParry)
                return true;

            return query.IsAttackState
                   && query.IsAttackCollisionActive
                   && query.IsCurrentAttackParryCapable;
        }
    }
}
