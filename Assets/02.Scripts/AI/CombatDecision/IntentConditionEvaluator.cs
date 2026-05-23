using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.AI.CombatDecision
{
    /// <summary>
    /// IntentEvaluationContext를 입력으로 받아 조건 식별자/연속값 식별자를 평가한다.
    /// 평가 결과는 EnemyIntentWeightsSO 기반 점수 계산에 사용된다.
    /// </summary>
    public static class IntentConditionEvaluator
    {
        public static bool Evaluate(IntentConditionId id, in IntentEvaluationContext ctx)
        {
            switch (id)
            {
                case IntentConditionId.InAttackRange:
                    return ctx.Distance <= ctx.OptimalDistance && ctx.HasAvailableAttack;
                case IntentConditionId.TooClose:
                    return ctx.Distance <= ctx.PersonalSpace;
                case IntentConditionId.UnderPreferredRange:
                    return ctx.Distance < Mathf.Max(ctx.PersonalSpace, ctx.PreferredRange - 0.45f);
                case IntentConditionId.OverPreferredRange:
                    return ctx.Distance > ctx.PreferredRange + 0.75f;
                case IntentConditionId.IsDistanceWithinOptimal:
                    return ctx.Distance <= ctx.OptimalDistance;
                case IntentConditionId.IsDistanceWithinPreferredPlusBuffer:
                    return ctx.Distance <= ctx.PreferredRange + 1.5f;
                case IntentConditionId.IsDistanceWithinMinDistance:
                    return ctx.Distance <= ctx.MinDistance;
                case IntentConditionId.IsDistanceFarFromOptimal:
                    return ctx.Distance > ctx.OptimalDistance + 1.5f;

                case IntentConditionId.LowHealth:
                    return ctx.HealthPercent <= 0.35f;
                case IntentConditionId.IsPoiseBroken:
                    return ctx.IsPoiseBroken;
                case IntentConditionId.TimeSinceRetreatBelowMinCooldown:
                    return ctx.TimeSinceRetreat < ctx.MinRetreatCooldown;

                case IntentConditionId.ActionDelayElapsed:
                    return ctx.ActionDelayElapsed;
                case IntentConditionId.CanUseSkill:
                    return ctx.CanUseSkill;
                case IntentConditionId.HasAvailableAttack:
                    return ctx.HasAvailableAttack;
                case IntentConditionId.HasGuardMotion:
                    return ctx.HasGuardMotion;

                case IntentConditionId.IsPlayerAttacking:
                    return ctx.IsPlayerAttacking;
                case IntentConditionId.IsPlayerGuarding:
                    return ctx.IsPlayerGuarding;
                case IntentConditionId.IsPlayerStaggered:
                    return ctx.IsPlayerStaggered;
                case IntentConditionId.IsPlayerRecovering:
                    return ctx.IsPlayerRecovering;

                case IntentConditionId.IsPlayerDodgingFrequently:
                    return ctx.IsPlayerDodgingFrequently;
                case IntentConditionId.IsPlayerAttackingFrequently:
                    return ctx.IsPlayerAttackingFrequently;
                case IntentConditionId.IsPlayerGuardingFrequently:
                    return ctx.IsPlayerGuardingFrequently;
                case IntentConditionId.IsPlayerRecoveringFrequently:
                    return ctx.IsPlayerRecoveringFrequently;

                case IntentConditionId.WasHitRecently:
                    return ctx.WasHitRecently;

                case IntentConditionId.None:
                default:
                    return false;
            }
        }

        public static float GetContinuous(ContinuousValueId id, in IntentEvaluationContext ctx)
        {
            switch (id)
            {
                case ContinuousValueId.Aggression:      return ctx.Aggression;
                case ContinuousValueId.ReactionChance:  return ctx.ReactionChance;
                case ContinuousValueId.PunishChance:    return ctx.PunishChance;
                case ContinuousValueId.CounterChance:   return ctx.CounterChance;
                case ContinuousValueId.RetreatChance:   return ctx.RetreatChance;
                case ContinuousValueId.GuardChance:     return ctx.GuardChance;
                case ContinuousValueId.CircleWeight:    return Mathf.Max(0f, ctx.CircleWeight);
                case ContinuousValueId.None:
                default:                                return 0f;
            }
        }

        /// <summary>
        /// 여러 조건 항(ConditionTerm)을 mode(AND/OR)로 결합해 평가한다.
        /// 빈 리스트는 true로 취급한다 (조건 없음 = 항상 적용).
        /// </summary>
        public static bool EvaluateTerms(ConditionMode mode, IList<ConditionTerm> terms, in IntentEvaluationContext ctx)
        {
            if (terms == null || terms.Count == 0)
                return true;

            if (mode == ConditionMode.Any)
            {
                for (var i = 0; i < terms.Count; i++)
                {
                    var result = Evaluate(terms[i].conditionId, in ctx);
                    if (terms[i].negate) result = !result;
                    if (result) return true;
                }
                return false;
            }

            for (var i = 0; i < terms.Count; i++)
            {
                var result = Evaluate(terms[i].conditionId, in ctx);
                if (terms[i].negate) result = !result;
                if (!result) return false;
            }
            return true;
        }
    }
}
