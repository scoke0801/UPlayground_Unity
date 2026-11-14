using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat
{
    /// <summary>플레이어 상태를 변경하지 않고 방어 결과를 계산하기 위한 입력 스냅샷.</summary>
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
        public readonly DefenseOutcome GuardOutcome;
        public readonly CombatDefensePolicySO Policy;
        public readonly bool IsProjectileGuardWindow;
        public readonly bool IsGuardBroken;
        public readonly UnityEngine.Vector3 Forward;
        public readonly ulong ProjectileGuardActionId;
        public readonly ulong AttackActionId;
        public readonly ulong AssistActionId;

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
            DefenseOutcome guardOutcome = DefenseOutcome.Block,
            CombatDefensePolicySO policy = null,
            bool isProjectileGuardWindow = false, bool isGuardBroken = false,
            UnityEngine.Vector3 forward = default, ulong projectileGuardActionId = 0,
            ulong attackActionId = 0, ulong assistActionId = 0)
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
            GuardOutcome = guardOutcome;
            Policy = policy;
            IsProjectileGuardWindow = isProjectileGuardWindow;
            IsGuardBroken = isGuardBroken;
            Forward = forward;
            ProjectileGuardActionId = projectileGuardActionId;
            AttackActionId = attackActionId;
            AssistActionId = assistActionId;
        }
    }

    /// <summary>피격 정보와 방어 스냅샷에서 상호 배타적인 방어 결과 하나를 선택한다.</summary>
    public static class DefenseResolver
    {
        /// <summary>가드, 공격 쳐내기, 회피, 무적 순서로 플레이어 방어 결과를 확정한다.</summary>
        public static DefenseResult ResolvePlayerDefense(
            in PlayerDefenseQuery query,
            in HitContext hit)
        {
            AttackDefenseType defenseType = hit.DefenseType;

            if (hit.IsProjectile)
            {
                bool eligible = hit.ProjectileContact.IsValid && hit.IsReflectableProjectile
                    && defenseType == AttackDefenseType.Parryable && query.Policy != null
                    && IsProjectileInFront(query.Forward, hit.AttackDirection, query.Policy.projectileParryFrontAngle);
                if (query.IsGuarding && query.IsGuardState && !query.IsGuardBroken
                    && defenseType != AttackDefenseType.Unblockable)
                {
                    if (eligible && query.IsProjectileGuardWindow)
                        return new DefenseResult(DefenseOutcome.PerfectGuard, false,
                            ProjectileDefenseSource.Guard, query.ProjectileGuardActionId);
                    DefenseOutcome outcome = query.GuardOutcome == DefenseOutcome.GuardBreak
                        ? DefenseOutcome.GuardBreak : DefenseOutcome.Block;
                    return new DefenseResult(outcome, outcome == DefenseOutcome.GuardBreak && query.CanTakeDamage);
                }
                if (eligible && !query.IsGuardState)
                {
                    if (query.IsAssistParryWindow)
                        return new DefenseResult(DefenseOutcome.AttackClash, false, ProjectileDefenseSource.Assist, query.AssistActionId);
                    if (query.IsAttackState && query.IsAttackCollisionActive && query.IsCurrentAttackParryCapable)
                        return new DefenseResult(DefenseOutcome.AttackClash, false, ProjectileDefenseSource.Attack, query.AttackActionId);
                }
                // 투사체는 위 전용 경로 외에는 디버그/근접 정책으로 성공시킬 수 없다.
                if (!query.CanTakeDamage)
                    return new DefenseResult(query.IsPerfectDodgeState && query.IsPerfectDodgeWindow
                        && CombatPolicyResolver.CanPerfectDodge(query.Policy, defenseType)
                        ? DefenseOutcome.PerfectDodge : DefenseOutcome.Invincible, false);
                return defenseType == AttackDefenseType.Unblockable
                    ? new DefenseResult(DefenseOutcome.UnblockableHit, true) : DefenseResult.None;
            }

            if (query.IsGuarding
                && query.IsGuardState
                && CombatPolicyResolver.CanGuard(query.Policy, defenseType))
            {
                DefenseOutcome guardOutcome = IsValidGuardOutcome(query.GuardOutcome)
                    ? query.GuardOutcome
                    : DefenseOutcome.Block;
                return new DefenseResult(
                    guardOutcome,
                    guardOutcome == DefenseOutcome.GuardBreak && query.CanTakeDamage);
            }

            if (CanParry(query, defenseType, hit.IsProjectile, hit.IsReflectableProjectile))
                return new DefenseResult(DefenseOutcome.AttackClash, false);

            if (!query.CanTakeDamage)
            {
                if (query.IsPerfectDodgeState
                    && query.IsPerfectDodgeWindow
                    && CombatPolicyResolver.CanPerfectDodge(query.Policy, defenseType))
                {
                    return new DefenseResult(DefenseOutcome.PerfectDodge, false);
                }

                return new DefenseResult(DefenseOutcome.Invincible, false);
            }

            return defenseType == AttackDefenseType.Unblockable
                ? new DefenseResult(DefenseOutcome.UnblockableHit, true)
                : DefenseResult.None;
        }

        private static bool IsValidGuardOutcome(DefenseOutcome outcome)
            => outcome is DefenseOutcome.Block
                or DefenseOutcome.PerfectGuard
                or DefenseOutcome.GuardBreak;

        public static bool IsProjectileInFront(UnityEngine.Vector3 forward, UnityEngine.Vector3 incoming, float angle)
        {
            forward.y = incoming.y = 0f;
            return forward.sqrMagnitude > 0.000001f && incoming.sqrMagnitude > 0.000001f
                && UnityEngine.Vector3.Dot(forward.normalized, -incoming.normalized)
                    >= UnityEngine.Mathf.Cos(angle * 0.5f * UnityEngine.Mathf.Deg2Rad);
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
