using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat
{
    public readonly struct PlayerReactionQuery
    {
        public readonly bool IgnoreHitReaction;
        public readonly bool CanTransitionToHit;
        public readonly bool IsAlreadyHitOrGrabbed;
        public readonly bool ShouldEnterAirborne;
        public readonly bool IsStaggerImmune;

        public PlayerReactionQuery(
            bool ignoreHitReaction,
            bool canTransitionToHit,
            bool isAlreadyHitOrGrabbed,
            bool shouldEnterAirborne,
            bool isStaggerImmune = false)
        {
            IgnoreHitReaction = ignoreHitReaction;
            CanTransitionToHit = canTransitionToHit;
            IsAlreadyHitOrGrabbed = isAlreadyHitOrGrabbed;
            ShouldEnterAirborne = shouldEnterAirborne;
            IsStaggerImmune = isStaggerImmune;
        }
    }

    public readonly struct MonsterReactionQuery
    {
        public readonly bool PoiseBrokenNow;
        public readonly bool CanPlayHitReaction;
        public readonly bool ShouldEnterAirborne;
        public readonly bool CanEnterKnockdown;
        public readonly MonsterActorGrade Grade;
        public readonly CombatReactionPolicySO Policy;

        public MonsterReactionQuery(
            bool poiseBrokenNow,
            bool canPlayHitReaction,
            bool shouldEnterAirborne,
            bool canEnterKnockdown,
            MonsterActorGrade grade = MonsterActorGrade.Normal,
            CombatReactionPolicySO policy = null)
        {
            PoiseBrokenNow = poiseBrokenNow;
            CanPlayHitReaction = canPlayHitReaction;
            ShouldEnterAirborne = shouldEnterAirborne;
            CanEnterKnockdown = canEnterKnockdown;
            Grade = grade;
            Policy = policy;
        }
    }

    public static class ReactionResolver
    {
        public static ReactionDecision ResolvePlayerReaction(
            in PlayerReactionQuery query,
            AttackData attackData)
        {
            if (query.IgnoreHitReaction || attackData == null)
                return ReactionDecision.None;

            bool shouldEnterReactionBlock = !query.IsAlreadyHitOrGrabbed;

            // 경직 내성(Stagger Protection): 리액션 회복 직후 창 동안 약한 리액션(None/Light/Hit)을 무시한다.
            // 데미지는 본류(TakeDamage)에서 이미 적용 — 여기서는 상태 전환/카메라 흔들림만 억제해 통제권을 보호한다.
            // 큰 리액션(Heavy/넉백/에어본/다운/스턴/잡기)은 통과시켜 강한 한 방엔 여전히 흔들리게 한다.
            if (query.IsStaggerImmune && IsMinorPlayerReaction(attackData.reactionType))
                shouldEnterReactionBlock = false;

            bool shouldEnterState = shouldEnterReactionBlock && query.CanTransitionToHit;

            return new ReactionDecision(
                shouldApplyForce: true,
                shouldEnterState,
                shouldPlayCameraFeedback: shouldEnterReactionBlock,
                ResolveTargetState(attackData, query.ShouldEnterAirborne, attackData.reactionType == AttackReactionType.Knockdown));
        }

        public static ReactionDecision ResolveMonsterReaction(
            in MonsterReactionQuery query,
            AttackData attackData)
        {
            // guaranteedReaction: 등급 리액션 정책을 우회해 피격 리액션을 보장한다(보스 등 강인한 적도 흔들림).
            // 단 상태 기반 제외(Death/Hit/Airborne 등 = CanPlayHitReaction)는 안전상 유지한다.
            bool guaranteed = attackData != null && attackData.guaranteedReaction;
            bool shouldPlayHitReaction = attackData != null
                                         && attackData.reactionType != AttackReactionType.None
                                         && query.CanPlayHitReaction
                                         && (guaranteed
                                             || (attackData.forceReaction
                                                 && CombatPolicyResolver.AllowsMonsterForceReaction(query.Policy, query.Grade)));
            bool shouldApplyForce = query.PoiseBrokenNow || shouldPlayHitReaction;

            if (query.PoiseBrokenNow)
            {
                CombatReactionState poiseBreakState = query.CanEnterKnockdown
                    ? CombatReactionState.Knockdown
                    : CombatReactionState.Stun;
                poiseBreakState = ApplyMonsterPolicy(query, poiseBreakState);

                return new ReactionDecision(
                    shouldApplyForce,
                    shouldEnterState: poiseBreakState != CombatReactionState.None,
                    shouldPlayCameraFeedback: false,
                    poiseBreakState);
            }

            if (!shouldPlayHitReaction)
                return new ReactionDecision(shouldApplyForce, false, false, CombatReactionState.None);

            CombatReactionState targetState = ResolveTargetState(
                attackData,
                query.ShouldEnterAirborne,
                query.CanEnterKnockdown);

            // guaranteed면 상태 허용/Poise브레이크요구 정책을 모두 우회한다.
            if (!guaranteed)
            {
                targetState = ApplyMonsterPolicy(query, targetState);

                if (CombatPolicyResolver.RequiresPoiseBreakForMonsterState(query.Policy, query.Grade))
                    targetState = CombatReactionState.None;
            }

            return new ReactionDecision(
                shouldApplyForce,
                shouldEnterState: targetState != CombatReactionState.None,
                shouldPlayCameraFeedback: false,
                targetState);
        }

        private static CombatReactionState ApplyMonsterPolicy(
            in MonsterReactionQuery query,
            CombatReactionState state)
        {
            return CombatPolicyResolver.AllowsMonsterReactionState(query.Policy, query.Grade, state)
                ? state
                : CombatReactionState.None;
        }

        // 경직 내성 창에서 무시할 "약한" 리액션 분류.
        // Light/Hit(및 None)만 약한 리액션 — 이 외(Heavy/넉백/에어본/다운/스턴/잡기)는 통과시킨다.
        // OnDamaged가 흡수된 피격의 히트스톱을 함께 생략하기 위해 참조하므로 public.
        public static bool IsMinorPlayerReaction(AttackReactionType reaction)
            => reaction is AttackReactionType.None
                        or AttackReactionType.Light
                        or AttackReactionType.Hit;

        private static CombatReactionState ResolveTargetState(
            AttackData attackData,
            bool shouldEnterAirborne,
            bool canEnterKnockdown)
        {
            if (shouldEnterAirborne)
                return CombatReactionState.Airborne;

            return attackData?.reactionType switch
            {
                AttackReactionType.Grab => CombatReactionState.Grabbed,
                AttackReactionType.Stun => CombatReactionState.Stun,
                AttackReactionType.Knockdown when canEnterKnockdown => CombatReactionState.Knockdown,
                _ => CombatReactionState.Hit,
            };
        }
    }
}
