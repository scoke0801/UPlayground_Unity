using UnityEngine;

namespace UPlayGround.AI.CombatDecision
{
    /// <summary>
    /// EnemyCombatDecisionEvaluator의 레거시 매직 넘버 점수 계산.
    /// IW_Default_Melee SO 자산의 회귀 동등성 기준선.
    /// SO 기반 경로와 결과가 달라지면 안 되며, 새 SO 도입은 이 함수를 더 이상 호출하지 않을 뿐 결과는 동일해야 한다.
    /// </summary>
    public static class LegacyIntentScoring
    {
        public struct Scores
        {
            public float attack;
            public float punish;
            public float counter;
            public float pressure;
            public float chase;
            public float retreat;
            public float keepDistance;
            public float defend;
            public float recover;
        }

        public static Scores Compute(in IntentEvaluationContext c)
        {
            var inAttackRange   = c.Distance <= c.OptimalDistance && c.HasAvailableAttack;
            var tooClose        = c.Distance <= c.PersonalSpace;
            var underPreferred  = c.Distance < Mathf.Max(c.PersonalSpace, c.PreferredRange - 0.45f);
            var overPreferred   = c.Distance > c.PreferredRange + 0.75f;
            var lowHealth       = c.HealthPercent <= 0.35f;

            var s = new Scores();

            s.attack = 0.10f + c.Aggression * 0.42f;
            if (inAttackRange && c.ActionDelayElapsed) s.attack += 0.45f;
            if (c.CanUseSkill) s.attack += 0.08f;
            if (c.IsPlayerStaggered) s.attack += 0.18f;
            if (c.IsPlayerRecoveringFrequently) s.attack += 0.10f;
            if (!c.HasAvailableAttack || !c.ActionDelayElapsed) s.attack *= 0.55f;

            s.punish = 0.03f + c.PunishChance * 0.45f;
            if (c.IsPlayerRecovering || c.IsPlayerStaggered) s.punish += 0.45f;
            if (c.IsPlayerDodgingFrequently) s.punish += 0.18f;
            if (c.IsPlayerRecoveringFrequently) s.punish += 0.18f;
            if (inAttackRange && c.ActionDelayElapsed) s.punish += 0.22f;
            if (!c.HasAvailableAttack || !c.ActionDelayElapsed) s.punish *= 0.45f;

            s.counter = 0.04f + c.CounterChance * 0.50f;
            if (c.IsPlayerAttacking && c.Distance <= c.OptimalDistance) s.counter += c.ReactionChance * 0.45f + 0.16f;
            if (tooClose && c.IsPlayerAttacking) s.counter += 0.16f;
            if (c.IsPlayerAttackingFrequently) s.counter += 0.20f;
            if (!c.ActionDelayElapsed) s.counter *= 0.65f;

            s.pressure = 0.12f + c.Aggression * 0.24f + Mathf.Max(0f, c.CircleWeight) * 0.22f;
            if (!c.IsPlayerAttacking && c.Distance <= c.PreferredRange + 1.5f) s.pressure += 0.16f;
            if (c.IsPlayerGuarding || c.IsPlayerDodgingFrequently) s.pressure += 0.12f;
            if (c.IsPlayerGuardingFrequently) s.pressure += 0.14f;
            if (!c.ActionDelayElapsed) s.pressure += 0.10f;

            s.chase = overPreferred ? 0.55f + c.Aggression * 0.25f : 0.05f;
            if (c.Distance > c.OptimalDistance + 1.5f) s.chase += 0.18f;

            s.retreat = 0.02f + c.RetreatChance * 0.42f;
            if (tooClose) s.retreat += 0.35f;
            if (c.WasHitRecently) s.retreat += 0.18f;
            if (c.IsPoiseBroken) s.retreat += 0.30f;
            if (lowHealth) s.retreat += 0.18f;
            if (c.IsPlayerAttackingFrequently && tooClose) s.retreat += 0.12f;
            if (c.TimeSinceRetreat < c.MinRetreatCooldown) s.retreat *= 0.35f;

            s.keepDistance = 0.05f;
            if (underPreferred) s.keepDistance += 0.34f;
            if (c.IsPlayerAttacking && c.Distance <= c.MinDistance) s.keepDistance += 0.20f;
            if (c.IsPlayerAttackingFrequently && c.Distance <= c.OptimalDistance) s.keepDistance += 0.12f;
            if (overPreferred) s.keepDistance *= 0.45f;

            s.defend = 0.04f + c.GuardChance * 0.38f;
            if (c.HasGuardMotion && c.IsPlayerAttacking && c.Distance <= c.OptimalDistance) s.defend += 0.30f;
            if (c.WasHitRecently && !c.IsPoiseBroken) s.defend += 0.12f;
            if (c.IsPlayerAttackingFrequently) s.defend += 0.16f;

            s.recover = lowHealth ? 0.22f : 0.02f;
            if (c.WasHitRecently && lowHealth) s.recover += 0.18f;
            if (tooClose || c.IsPlayerAttacking) s.recover *= 0.55f;

            return s;
        }
    }
}
