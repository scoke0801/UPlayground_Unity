#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Balance
{
    public static class BalanceCombatEstimator
    {
        public static BalanceScenarioResult Analyze(
            ActorDefinitionSO actor,
            BalanceScenarioAsset scenario,
            BalanceScenarioInput fallbackInput)
        {
            var result = new BalanceScenarioResult
            {
                Actor = actor,
                TargetDuration = GetTargetDuration(actor, scenario, fallbackInput),
            };

            int monsterLevel = scenario != null && !scenario.useActorDefinitionLevel
                ? Mathf.Max(1, scenario.overrideMonsterLevel)
                : Mathf.Max(1, actor != null ? actor.level : fallbackInput.MonsterLevel);

            float assumedDistance = scenario != null ? scenario.assumedDistance : fallbackInput.AssumedDistance;
            result.MonsterLevel = monsterLevel;
            result.Messages.AddRange(BalanceActorDataValidator.Validate(actor, scenario, assumedDistance, monsterLevel));

            if (actor == null || actor.statData == null || actor.attackData == null || result.HasError)
            {
                result.Status = BalanceCheckStatus.InvalidData;
                result.Summary = "필수 데이터가 부족합니다.";
                return result;
            }

            float monsterHealth = Mathf.Max(1f, actor.statData.GetBase(StatType.MaxHealth));
            float monsterAttackPower = Mathf.Max(0f, actor.statData.GetBase(StatType.AttackPower));
            float monsterDefense = Mathf.Clamp01(actor.statData.GetBase(StatType.Defense));

            float playerHealth = Mathf.Max(1f, ReadPlayerStat(scenario, StatType.MaxHealth));
            float playerAttackPower = Mathf.Max(0f, ReadPlayerAttackPower(scenario, fallbackInput));
            float playerDefense = Mathf.Clamp01(ReadPlayerStat(scenario, StatType.Defense));
            result.MonsterHealth = monsterHealth;
            result.PlayerAttackPower = playerAttackPower;

            result.EnemyExpectedDps = EstimateEnemyDps(
                actor.attackData,
                monsterLevel,
                assumedDistance,
                monsterAttackPower,
                playerDefense,
                scenario,
                result);

            float rawPlayerDps = BalanceAttackAnalyzer.EstimatePlayerRawDps(
                scenario != null ? scenario.playerAttackData : null,
                scenario != null ? scenario.playerAttackInterval : fallbackInput.PlayerAttackInterval,
                scenario != null ? scenario.manualPlayerDps : fallbackInput.ManualPlayerDps);

            result.PlayerExpectedDps = rawPlayerDps * playerAttackPower * (1f - monsterDefense);

            result.PlayerTimeToDeath = result.EnemyExpectedDps > 0f
                ? playerHealth / result.EnemyExpectedDps
                : float.PositiveInfinity;
            result.MonsterTimeToDeath = result.PlayerExpectedDps > 0f
                ? monsterHealth / result.PlayerExpectedDps
                : float.PositiveInfinity;

            result.Status = DecideStatus(result, scenario, fallbackInput);
            result.Summary = BuildSummary(result);
            return result;
        }

        private static float EstimateEnemyDps(
            EnemyAttackDataSO attackData,
            int monsterLevel,
            float assumedDistance,
            float monsterAttackPower,
            float playerDefense,
            BalanceScenarioAsset scenario,
            BalanceScenarioResult result)
        {
            List<EnemyAttackInfo> skills = BalanceAttackAnalyzer.GetUsableEnemySkills(attackData, assumedDistance, monsterLevel);
            result.AvailableSkillCount = skills.Count;
            if (skills.Count == 0)
                return 0f;

            float totalWeight = 0f;
            for (int i = 0; i < skills.Count; i++)
                totalWeight += Mathf.Max(0f, skills[i].selectionWeight);

            if (totalWeight <= 0f)
                return 0f;

            float hitReceiveRate = scenario != null ? scenario.hitReceiveRate : 0.45f;
            float dodgeSuccessRate = scenario != null ? scenario.dodgeSuccessRate : 0.15f;
            float parrySuccessRate = scenario != null ? scenario.parrySuccessRate : 0.05f;
            float guardMitigationRate = scenario != null ? scenario.guardMitigationRate : 0.35f;
            float defenseMultiplier = 1f - Mathf.Clamp01(playerDefense);
            float avoidMultiplier = Mathf.Clamp01(hitReceiveRate) * (1f - Mathf.Clamp01(dodgeSuccessRate));
            float parryMultiplier = 1f - Mathf.Clamp01(parrySuccessRate);
            float guardMultiplier = 1f - Mathf.Clamp01(guardMitigationRate);

            float dps = 0f;
            float opportunities = 0f;
            float basicChance = 0f;
            float heavyChance = 0f;
            float skillChance = 0f;
            for (int i = 0; i < skills.Count; i++)
            {
                EnemyAttackInfo skill = skills[i];
                float chance = Mathf.Max(0f, skill.selectionWeight) / totalWeight;
                float cooldown = Mathf.Max(0.05f, Mathf.Max(skill.cooldown, attackData.globalCooldown));
                float rawDamage = BalanceAttackAnalyzer.SumDamage(skill.baseInfo);
                float expectedDamage = rawDamage * monsterAttackPower * defenseMultiplier * avoidMultiplier;

                if (skill.defenseType == AttackDefenseType.Parryable)
                    expectedDamage *= parryMultiplier;
                else if (skill.defenseType == AttackDefenseType.GuardableOnly)
                    expectedDamage *= guardMultiplier;

                float contribution = chance * expectedDamage / cooldown;
                dps += contribution;
                opportunities += chance * result.TargetDuration / cooldown;
                AccumulateCategoryChance(skill, chance, ref basicChance, ref heavyChance, ref skillChance);

                result.SkillBreakdowns.Add(new BalanceSkillBreakdown
                {
                    Name = skill.baseInfo.animKey.ToString(),
                    Damage = expectedDamage,
                    Weight = skill.selectionWeight,
                    SelectionChance = chance,
                    Cooldown = cooldown,
                    DpsContribution = contribution,
                    HitPhaseCount = BalanceAttackAnalyzer.CountHitPhases(skill.baseInfo),
                    Category = skill.attackCategory.ToString(),
                });
            }

            result.EnemyAttackOpportunities = opportunities;
            result.BasicAttackChance = basicChance;
            result.HeavyAttackChance = heavyChance;
            result.SkillAttackChance = skillChance;
            result.StrongAttackChance = heavyChance + skillChance;
            return dps;
        }

        private static void AccumulateCategoryChance(
            EnemyAttackInfo skill,
            float chance,
            ref float basicChance,
            ref float heavyChance,
            ref float skillChance)
        {
            switch (ResolveCategory(skill))
            {
                case EnemyAttackCategory.Heavy:
                    heavyChance += chance;
                    break;
                case EnemyAttackCategory.Skill:
                    skillChance += chance;
                    break;
                default:
                    basicChance += chance;
                    break;
            }
        }

        private static EnemyAttackCategory ResolveCategory(EnemyAttackInfo skill)
        {
            if (skill == null)
                return EnemyAttackCategory.Basic;

            if (skill.attackCategory is EnemyAttackCategory.Basic or EnemyAttackCategory.Heavy or EnemyAttackCategory.Skill)
                return skill.attackCategory;

            AnimKey key = skill.baseInfo != null ? skill.baseInfo.animKey : AnimKey.None;
            int value = (int)key;
            if (value >= (int)AnimKey.HeavyAttack_1 && value <= (int)AnimKey.HeavyAttack_10)
                return EnemyAttackCategory.Heavy;
            if (key == AnimKey.Fly_Attack ||
                (value >= (int)AnimKey.Skill_1 && value <= (int)AnimKey.Skill_9) ||
                (value >= (int)AnimKey.Counter_Attack_1 && value <= (int)AnimKey.Counter_Attack_2))
                return EnemyAttackCategory.Skill;

            return EnemyAttackCategory.Basic;
        }

        private static float ReadPlayerStat(BalanceScenarioAsset scenario, StatType type)
        {
            if (scenario?.playerStatData != null)
                return scenario.playerStatData.GetBase(type);

            return ActorStatSO.GetDefault(type);
        }

        private static float ReadPlayerAttackPower(BalanceScenarioAsset scenario, BalanceScenarioInput fallbackInput)
        {
            if (scenario?.playerStatData != null)
                return scenario.playerStatData.GetBase(StatType.AttackPower);

            if (scenario != null)
                return scenario.manualPlayerAttackPower;

            return fallbackInput.PlayerAttackPower;
        }

        private static float GetTargetDuration(ActorDefinitionSO actor, BalanceScenarioAsset scenario, BalanceScenarioInput fallbackInput)
        {
            if (scenario != null)
                return Mathf.Max(1f, scenario.targetDuration);

            if (fallbackInput.TargetDuration > 0f)
                return fallbackInput.TargetDuration;

            if (actor == null)
                return 30f;

            return actor.grade switch
            {
                MonsterActorGrade.Elite => 45f,
                MonsterActorGrade.Boss => 90f,
                _ => 20f,
            };
        }

        private static BalanceCheckStatus DecideStatus(
            BalanceScenarioResult result,
            BalanceScenarioAsset scenario,
            BalanceScenarioInput fallbackInput)
        {
            if (result.HasError)
                return BalanceCheckStatus.InvalidData;

            float minOpportunities = scenario != null ? scenario.minAttackOpportunities : fallbackInput.MinAttackOpportunities;
            if (result.EnemyExpectedDps <= 0f || result.EnemyAttackOpportunities < minOpportunities)
                return BalanceCheckStatus.Stalled;

            if (result.PlayerTimeToDeath < result.TargetDuration)
                return BalanceCheckStatus.TooLethal;

            if (result.MonsterTimeToDeath < result.TargetDuration)
                return BalanceCheckStatus.TooEasy;

            return BalanceCheckStatus.Stable;
        }

        private static string BuildSummary(BalanceScenarioResult result)
        {
            return result.Status switch
            {
                BalanceCheckStatus.InvalidData => "데이터 누락",
                BalanceCheckStatus.TooEasy => $"몬스터가 {FormatTime(result.MonsterTimeToDeath)}에 쓰러질 것으로 추정",
                BalanceCheckStatus.TooLethal => $"플레이어가 {FormatTime(result.PlayerTimeToDeath)}에 쓰러질 것으로 추정",
                BalanceCheckStatus.Stalled => "공격 기회 또는 유효 DPS가 부족",
                BalanceCheckStatus.Stable => "기준 시간 이상 전투 유지 가능",
                _ => string.Empty,
            };
        }

        public static string FormatTime(float value)
        {
            if (float.IsPositiveInfinity(value))
                return "INF";
            if (float.IsNaN(value))
                return "-";
            return $"{value:F1}s";
        }
    }

    public readonly struct BalanceScenarioInput
    {
        public readonly float TargetDuration;
        public readonly float AssumedDistance;
        public readonly int MonsterLevel;
        public readonly float PlayerAttackPower;
        public readonly float ManualPlayerDps;
        public readonly float PlayerAttackInterval;
        public readonly float MinAttackOpportunities;

        public BalanceScenarioInput(
            float targetDuration,
            float assumedDistance,
            int monsterLevel,
            float playerAttackPower,
            float manualPlayerDps,
            float playerAttackInterval,
            float minAttackOpportunities)
        {
            TargetDuration = targetDuration;
            AssumedDistance = assumedDistance;
            MonsterLevel = monsterLevel;
            PlayerAttackPower = playerAttackPower;
            ManualPlayerDps = manualPlayerDps;
            PlayerAttackInterval = playerAttackInterval;
            MinAttackOpportunities = minAttackOpportunities;
        }
    }
}
#endif
