#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 목표 전투시간 역산기 (Phase 2).
    /// <see cref="BalanceCombatEstimator"/>가 HP·피해에 선형이라는 점을 이용해,
    /// "이 몬스터가 기준 플레이어 상대로 목표 시간만큼 버티고/잡히게" 하는 HP와 피해 배율을 역산한다.
    ///
    /// - 몬스터HP   = 플레이어 예상 DPS × 목표 처치시간
    /// - 피해 배율  = (플레이어 HP / 목표 생존시간) / 현재 적 DPS
    ///
    /// 목표 처치/생존 시간은 시나리오/등급 기준 시간(result.TargetDuration)을 그대로 쓴다.
    /// 적 DPS·플레이어 DPS는 이미 방어/회피 가정을 반영한 값이므로, 같은 결과를 기준으로 한 배율은
    /// 그 가정들이 상쇄되어 일관된다.
    /// </summary>
    public static class BalanceTargetSolver
    {
        public readonly struct Recommendation
        {
            public Recommendation(
                bool canSolveHealth,
                bool canSolveDamage,
                float currentHealth,
                float recommendedHealth,
                float currentEnemyDps,
                float recommendedDamageScale,
                float targetKillTime,
                float targetSurvivalTime)
            {
                CanSolveHealth = canSolveHealth;
                CanSolveDamage = canSolveDamage;
                CurrentHealth = currentHealth;
                RecommendedHealth = recommendedHealth;
                CurrentEnemyDps = currentEnemyDps;
                RecommendedDamageScale = recommendedDamageScale;
                TargetKillTime = targetKillTime;
                TargetSurvivalTime = targetSurvivalTime;
            }

            public bool CanSolveHealth { get; }
            public bool CanSolveDamage { get; }
            public float CurrentHealth { get; }
            public float RecommendedHealth { get; }
            public float CurrentEnemyDps { get; }
            public float RecommendedDamageScale { get; }
            public float TargetKillTime { get; }
            public float TargetSurvivalTime { get; }
        }

        public static Recommendation Solve(BalanceScenarioResult result)
        {
            if (result == null)
                return default;

            float targetTime = Mathf.Max(0.1f, result.TargetDuration);

            // 처치시간 목표 → HP 역산
            bool canSolveHealth = result.PlayerExpectedDps > 0f;
            float recommendedHealth = canSolveHealth ? result.PlayerExpectedDps * targetTime : result.MonsterHealth;

            // 생존시간 목표 → 적 DPS 목표 → 피해 배율 역산
            bool canSolveDamage = result.EnemyExpectedDps > 0f && result.PlayerHealth > 0f;
            float recommendedDamageScale = 1f;
            if (canSolveDamage)
            {
                float targetEnemyDps = result.PlayerHealth / targetTime;
                recommendedDamageScale = targetEnemyDps / result.EnemyExpectedDps;
            }

            return new Recommendation(
                canSolveHealth,
                canSolveDamage,
                result.MonsterHealth,
                recommendedHealth,
                result.EnemyExpectedDps,
                recommendedDamageScale,
                targetTime,
                targetTime);
        }

        /// <summary>권장 HP를 몬스터 statData.MaxHealth에 적용한다(Undo 가능).</summary>
        public static bool ApplyHealth(ActorDefinitionSO actor, float recommendedHealth)
        {
            if (actor == null || actor.statData == null)
                return false;

            Undo.RecordObject(actor.statData, "Apply Recommended Monster HP");
            actor.statData.EditorSet(StatType.MaxHealth, Mathf.Max(1f, Mathf.Round(recommendedHealth)));
            EditorUtility.SetDirty(actor.statData);
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary>모든 공격 HitPhase.damage에 배율을 곱한다(Undo 가능). 저장 피해이므로 런타임 공격력은 그대로 곱해진다.</summary>
        public static bool ApplyDamageScale(ActorDefinitionSO actor, float scale)
        {
            if (actor == null || actor.attackData == null || actor.attackData.skills == null)
                return false;
            if (scale <= 0f || Mathf.Approximately(scale, 1f))
                return false;

            Undo.RecordObject(actor.attackData, "Apply Recommended Damage Scale");
            for (int i = 0; i < actor.attackData.skills.Count; i++)
            {
                EnemyAttackInfo skill = actor.attackData.skills[i];
                if (skill?.baseInfo?.hitPhases == null)
                    continue;

                for (int p = 0; p < skill.baseInfo.hitPhases.Count; p++)
                {
                    HitPhaseData phase = skill.baseInfo.hitPhases[p];
                    if (phase == null)
                        continue;
                    phase.damage = Mathf.Max(1f, Mathf.Round(phase.damage * scale));
                }
            }

            EditorUtility.SetDirty(actor.attackData);
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
#endif
