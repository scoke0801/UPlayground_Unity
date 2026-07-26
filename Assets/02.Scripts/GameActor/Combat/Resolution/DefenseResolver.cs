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
        public readonly bool IsAssistParryWindow;
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
            bool isAssistParryWindow = false,
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
            IsAssistParryWindow = isAssistParryWindow;
            Policy = policy;
        }
    }

    public static class DefenseResolver
    {
        public static DefenseResult ResolvePlayerDefense(
            in PlayerDefenseQuery query,
            in HitContext hit)
        {
            AttackDefenseType defenseType = hit.DefenseType;

            if (query.IsGuarding
                && query.IsGuardState
                && CombatPolicyResolver.CanGuard(query.Policy, defenseType))
            {
                return new DefenseResult(DefenseOutcome.Guarded, false);
            }

            if (CanParry(query, defenseType, hit.IsProjectile, hit.IsReflectableProjectile))
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

        private static bool CanParry(
            in PlayerDefenseQuery query,
            AttackDefenseType defenseType,
            bool isProjectile,
            bool isReflectableProjectile)
        {
            // 투사체/AOE는 전달 방식 자체가 패리·카운터 대상이 아니다(디버그 AlwaysParry보다 우선).
            if (isProjectile && !isReflectableProjectile)
                return false;

            if (query.AlwaysParry)
                return CombatPolicyResolver.CanParry(query.Policy, defenseType);

            // 어시스트 스왑 패리(§4.3): 입장 캐릭터의 패리 윈도우 중 피격은 패리로 라우팅.
            // Unblockable(빨강 Danger Ring)은 명시적으로 제외해 회피 강제 원칙을 유지한다(정책 미설정 환경 포함).
            if (query.IsAssistParryWindow && defenseType != AttackDefenseType.Unblockable)
                return CombatPolicyResolver.CanParry(query.Policy, defenseType);

            return query.IsAttackState
                   && query.IsAttackCollisionActive
                   && query.IsCurrentAttackParryCapable
                   && CombatPolicyResolver.CanParry(query.Policy, defenseType);
        }
    }
}
