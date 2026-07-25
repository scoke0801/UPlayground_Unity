#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;
using UPlayGround.Data.Ability;
using UPlayGround.EditorTools;

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

            if (actor == null
                || actor.attributeProfile == null
                || actor.EffectiveAbilitySet == null
                || result.HasError)
            {
                result.Status = BalanceCheckStatus.InvalidData;
                result.Summary = "필수 데이터가 부족합니다.";
                return result;
            }

            float monsterHealth = Mathf.Max(1f,
                BalanceAttributeProfileUtility.Get(actor, global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth, 100f));
            float monsterAttackPower = Mathf.Max(0f,
                BalanceAttributeProfileUtility.Get(actor, global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, 1f));
            float monsterDefense = Mathf.Clamp01(
                BalanceAttributeProfileUtility.Get(actor, global::UPlayGround.Data.Stat.Attributes.Combat.Defense));

            float playerHealth = Mathf.Max(1f, ReadPlayerStat(scenario, global::UPlayGround.Data.Stat.Attributes.Vital.MaxHealth));
            float playerAttackPower = Mathf.Max(0f, ReadPlayerAttackPower(scenario, fallbackInput));
            float playerDefense = Mathf.Clamp01(ReadPlayerStat(scenario, global::UPlayGround.Data.Stat.Attributes.Combat.Defense));
            float playerMaxPoise = Mathf.Max(0f, ReadPlayerStat(scenario, global::UPlayGround.Data.Stat.Attributes.Vital.MaxPoise));
            float playerPoiseRecovery = Mathf.Max(0f, ReadPlayerStat(scenario, global::UPlayGround.Data.Stat.Attributes.Vital.PoiseRecoveryRate));
            result.MonsterHealth = monsterHealth;
            result.PlayerHealth = playerHealth;
            result.PlayerAttackPower = playerAttackPower;
            result.PlayerMaxPoise = playerMaxPoise;
            result.PlayerPoiseRecoveryRate = playerPoiseRecovery;
            CountSkillUnlock(actor.EffectiveAbilitySet, monsterLevel, out int unlockedCount, out int lockedCount);
            result.UnlockedSkillCount = unlockedCount;
            result.LockedSkillCount = lockedCount;

            result.EnemyExpectedDps = EstimateEnemyDps(
                actor.EffectiveAbilitySet,
                monsterLevel,
                assumedDistance,
                monsterAttackPower,
                playerDefense,
                scenario,
                result);

            float rawPlayerDps = BalanceAttackAnalyzer.EstimatePlayerRawDps(
                scenario != null ? scenario.playerAbilitySet : null,
                scenario != null ? scenario.playerAttackInterval : fallbackInput.PlayerAttackInterval,
                scenario != null ? scenario.manualPlayerDps : fallbackInput.ManualPlayerDps);

            result.PlayerExpectedDps = rawPlayerDps * playerAttackPower * (1f - monsterDefense);
            ApplyPlayerBreakEstimate(actor, scenario, fallbackInput, result);

            result.PlayerTimeToDeath = result.EnemyExpectedDps > 0f
                ? playerHealth / result.EnemyExpectedDps
                : float.PositiveInfinity;
            result.MonsterTimeToDeath = result.PlayerExpectedDps > 0f
                ? monsterHealth / result.PlayerExpectedDps
                : float.PositiveInfinity;
            result.MonsterTimeToDeathWithBreak = result.PlayerEffectiveDpsWithBreak > 0f
                ? monsterHealth / result.PlayerEffectiveDpsWithBreak
                : float.PositiveInfinity;
            ApplyQualityMetrics(result);
            // 플레이어 가드 브레이크는 가드 횟수 기반(포이즈 무관)이므로 '브레이크 시간'은 추정하지 않는다.
            // 대신 적의 초당 경직 압박이 플레이어 포이즈 회복을 넘어서는지를 순 압박으로 표시한다.
            result.NetPoisePressure = result.EnemyPoiseDps - playerPoiseRecovery;

            result.Status = DecideStatus(result, scenario, fallbackInput);
            result.RecommendedAction = BuildRecommendedAction(result);
            BalanceActorDataValidator.AppendPostAnalysisMessages(result);
            result.Summary = BuildSummary(result);
            return result;
        }

        private static void ApplyQualityMetrics(BalanceScenarioResult result)
        {
            float target = Mathf.Max(0.1f, result.TargetDuration);
            float killTime = result.MonsterTimeToDeathWithBreak > 0f && !float.IsPositiveInfinity(result.MonsterTimeToDeathWithBreak)
                ? result.MonsterTimeToDeathWithBreak
                : result.MonsterTimeToDeath;

            result.PlayerSurvivalRatio = ToFiniteRatio(result.PlayerTimeToDeath, target);
            result.MonsterKillRatio = ToFiniteRatio(killTime, target);

            float survivalScore = ScoreRatio(result.PlayerSurvivalRatio);
            float killScore = ScoreRatio(result.MonsterKillRatio);
            float opportunityScore = result.EnemyAttackOpportunities >= 1f
                ? Mathf.Clamp01(result.EnemyAttackOpportunities / Mathf.Max(1f, result.TargetDuration / 4f))
                : 0f;
            float dominancePenalty = Mathf.Clamp01((result.TopAttackDpsShare - 0.35f) / 0.35f);
            float strongPenalty = Mathf.Clamp01((result.StrongAttackChance - GetStrongChanceSoftCap(result.Actor)) / 0.35f);

            float score01 = Mathf.Clamp01(
                survivalScore * 0.34f +
                killScore * 0.34f +
                opportunityScore * 0.18f +
                (1f - dominancePenalty) * 0.08f +
                (1f - strongPenalty) * 0.06f);
            result.BalanceScore = Mathf.Round(score01 * 100f);
        }

        private static float ToFiniteRatio(float value, float target)
        {
            if (float.IsNaN(value) || value <= 0f)
                return 0f;
            if (float.IsPositiveInfinity(value))
                return 3f;
            return Mathf.Clamp(value / target, 0f, 3f);
        }

        private static float ScoreRatio(float ratio)
        {
            if (ratio <= 0f)
                return 0f;

            // 1.0이 목표 정중앙. 0.65~1.6 정도는 허용하고, 바깥으로 갈수록 급격히 감점한다.
            float logDistance = Mathf.Abs(Mathf.Log(Mathf.Max(0.01f, ratio), 2f));
            return Mathf.Clamp01(1f - logDistance / 1.35f);
        }

        private static float GetStrongChanceSoftCap(ActorDefinitionSO actor)
        {
            MonsterActorGrade grade = actor != null ? actor.grade : MonsterActorGrade.Normal;
            return grade switch
            {
                MonsterActorGrade.Boss => 0.65f,
                MonsterActorGrade.Elite => 0.45f,
                _ => 0.25f,
            };
        }

        private static string BuildRecommendedAction(BalanceScenarioResult result)
        {
            if (result == null || result.Status == BalanceCheckStatus.InvalidData)
                return "필수 데이터 보정";

            if (result.EnemyExpectedDps <= 0f || result.EnemyAttackOpportunities < 1f)
                return "공격 풀/거리/쿨다운 확인";

            if (result.PlayerSurvivalRatio < 0.8f)
                return result.TopAttackDpsShare > 0.35f
                    ? $"최대 기여 공격({result.TopAttackName}) 피해/가중치 하향"
                    : "몬스터 피해량 또는 피격 가정 하향";

            if (result.MonsterKillRatio < 0.8f)
                return "몬스터 HP/방어/브레이크 보정 상향";

            if (result.MonsterKillRatio > 1.6f)
                return "몬스터 HP/방어 하향 또는 플레이어 DPS 가정 확인";

            if (result.TopAttackDpsShare > 0.35f)
                return $"공격 과점 완화: {result.TopAttackName}";

            if (result.StrongAttackChance > GetStrongChanceSoftCap(result.Actor))
                return "Heavy/Skill selectionWeight 하향";

            if (result.NetPoisePressure > 0f)
                return "Poise damage/회복률 확인";

            return "유지 또는 플레이테스트 검증";
        }

        private static void ApplyPlayerBreakEstimate(
            ActorDefinitionSO actor,
            BalanceScenarioAsset scenario,
            BalanceScenarioInput fallbackInput,
            BalanceScenarioResult result)
        {
            result.PlayerEffectiveDpsWithBreak = result.PlayerExpectedDps;
            result.MonsterTimeToDeathWithBreak = result.MonsterTimeToDeath;

            MonsterBreakGaugeSO breakData = actor != null ? actor.breakGaugeData : null;
            if (breakData == null || !breakData.useBreakGauge)
                return;

            float attackInterval = scenario != null ? scenario.playerAttackInterval : fallbackInput.PlayerAttackInterval;
            float rawBreakDps = BalanceAttackAnalyzer.EstimatePlayerRawBreakDps(
                scenario != null ? scenario.playerAbilitySet : null,
                attackInterval);

            result.PlayerExpectedBreakDps = rawBreakDps * (1f - Mathf.Clamp01(breakData.breakResist));
            result.MonsterBreakResist = Mathf.Clamp01(breakData.breakResist);
            result.BreakExposedDuration = Mathf.Max(0.1f, breakData.exposedDuration);
            result.BreakDamageTakenMultiplier = Mathf.Max(0f, breakData.damageTakenMultiplierWhileExposed);

            MonsterActorGrade grade = actor != null ? actor.grade : MonsterActorGrade.Normal;
            float gradeScale = breakData.gradePolicy != null ? breakData.gradePolicy.GetGaugeMultiplier(grade) : 1f;
            result.MonsterBreakGauge = Mathf.Max(1f, breakData.maxGauge * gradeScale);

            if (result.PlayerExpectedBreakDps <= 0f)
                return;

            result.EstimatedTimeToBreak = result.MonsterBreakGauge / result.PlayerExpectedBreakDps;
            float cycle = Mathf.Max(0.1f, result.EstimatedTimeToBreak + result.BreakExposedDuration);
            result.EstimatedBreaksPerFight = result.TargetDuration / cycle;
            result.BreakExposedUptime = Mathf.Clamp01(result.BreakExposedDuration / cycle);

            // 통합 취약 배율(CombatPolicyResolver.GetVulnerabilityMultiplier) 중 Break 노출 가동률 성분만 모델링한다.
            // 리액션 상태(Stun/Knockdown/Airborne/Grabbed) 배율은 가동률이 플레이어 콤보·Poise에 의존하는 transient라
            // 정상상태 추정에서 의도적으로 제외한다. 따라서 이 값은 버스트 윈도우를 포함한 실측 대비 하한이다.
            float bonusMultiplier = Mathf.Lerp(1f, result.BreakDamageTakenMultiplier, result.BreakExposedUptime);
            result.PlayerEffectiveDpsWithBreak = result.PlayerExpectedDps * Mathf.Max(0f, bonusMultiplier);
        }

        private static void CountSkillUnlock(
            AbilitySetSO data,
            int level,
            out int unlocked,
            out int locked)
        {
            unlocked = 0;
            locked = 0;
            List<AbilityAttackEditorUtility.Entry> entries =
                AbilityAttackEditorUtility.Collect(data, true);
            for (int i = 0; i < entries.Count; i++)
            {
                AbilityAttackInfo skill = entries[i].AttackInfo;
                if (skill == null || skill.baseInfo == null || skill.skillType != SkillType.Attack)
                    continue;

                if (skill.IsUnlockedForLevel(level))
                    unlocked++;
                else
                    locked++;
            }
        }

        private static float EstimateEnemyDps(
            AbilitySetSO attackData,
            int monsterLevel,
            float assumedDistance,
            float monsterAttackPower,
            float playerDefense,
            BalanceScenarioAsset scenario,
            BalanceScenarioResult result)
        {
            List<AbilityAttackInfo> skills = BalanceAttackAnalyzer.GetUsableEnemySkills(attackData, assumedDistance, monsterLevel);
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
            float poiseDps = 0f;
            float opportunities = 0f;
            float basicChance = 0f;
            float heavyChance = 0f;
            float skillChance = 0f;
            for (int i = 0; i < skills.Count; i++)
            {
                AbilityAttackInfo skill = skills[i];
                float chance = Mathf.Max(0f, skill.selectionWeight) / totalWeight;
                GameplayAbilitySO ability =
                    BalanceAttackAnalyzer.FindAbility(attackData, skill);
                float cooldown = Mathf.Max(
                    0.05f,
                    ability?.cooldown?.durationSeconds ?? 0f);
                float rawDamage = BalanceAttackAnalyzer.SumDamage(skill.baseInfo);
                float expectedDamage = rawDamage * monsterAttackPower * defenseMultiplier * avoidMultiplier;

                if (skill.defenseType == AttackDefenseType.Parryable)
                    expectedDamage *= parryMultiplier;
                else if (skill.defenseType == AttackDefenseType.GuardableOnly)
                    expectedDamage *= guardMultiplier;

                float contribution = chance * expectedDamage / cooldown;
                dps += contribution;

                // 경직 압박: 방어계수로 줄지 않으며, 실제 피격(회피 보정)만 반영한다.
                float rawPoise = BalanceAttackAnalyzer.SumPoiseDamage(skill.baseInfo);
                float poiseContribution = chance * rawPoise * avoidMultiplier / cooldown;
                poiseDps += poiseContribution;

                opportunities += chance * result.TargetDuration / cooldown;
                AccumulateCategoryChance(skill, chance, ref basicChance, ref heavyChance, ref skillChance);

                result.SkillBreakdowns.Add(new BalanceSkillBreakdown
                {
                    Name = skill.baseInfo.motionRef != null ? skill.baseInfo.motionRef.name : "-",
                    Damage = expectedDamage,
                    PoiseDamage = rawPoise,
                    Weight = skill.selectionWeight,
                    SelectionChance = chance,
                    Cooldown = cooldown,
                    DpsContribution = contribution,
                    PoiseContribution = poiseContribution,
                    HitPhaseCount = BalanceAttackAnalyzer.CountHitPhases(skill.baseInfo),
                    Category = skill.attackCategory.ToString(),
                    IsStrong = BalanceAttackAnalyzer.IsStrongEnemyAttack(skill),
                    UseDangerRing = skill.useDangerRing,
                    UseTelegraph = skill.useTelegraph,
                    DangerRingDuration = skill.dangerRingDuration,
                    DefenseType = skill.defenseType.ToString(),
                });
            }

            result.EnemyAttackOpportunities = opportunities;
            result.BasicAttackChance = basicChance;
            result.HeavyAttackChance = heavyChance;
            result.SkillAttackChance = skillChance;
            result.StrongAttackChance = heavyChance + skillChance;
            result.EnemyPoiseDps = poiseDps;

            // DPS 기여도 내림차순 정렬 + 최대 기여 공격 비중 산출
            result.SkillBreakdowns.Sort((a, b) => b.DpsContribution.CompareTo(a.DpsContribution));
            for (int i = 0; i < result.SkillBreakdowns.Count; i++)
                result.SkillBreakdowns[i].DpsShare = dps > 0f ? result.SkillBreakdowns[i].DpsContribution / dps : 0f;
            if (result.SkillBreakdowns.Count > 0)
            {
                result.TopAttackName = result.SkillBreakdowns[0].Name;
                result.TopAttackDpsShare = result.SkillBreakdowns[0].DpsShare;
            }

            return dps;
        }

        private static void AccumulateCategoryChance(
            AbilityAttackInfo skill,
            float chance,
            ref float basicChance,
            ref float heavyChance,
            ref float skillChance)
        {
            switch (ResolveCategory(skill))
            {
                case AbilityAttackCategory.Heavy:
                    heavyChance += chance;
                    break;
                case AbilityAttackCategory.Skill:
                    skillChance += chance;
                    break;
                default:
                    basicChance += chance;
                    break;
            }
        }

        private static AbilityAttackCategory ResolveCategory(AbilityAttackInfo skill)
        {
            if (skill == null)
                return AbilityAttackCategory.Basic;

            if (skill.attackCategory is AbilityAttackCategory.Basic or AbilityAttackCategory.Heavy or AbilityAttackCategory.Skill)
                return skill.attackCategory;

            return AbilityAttackCategory.Basic;
        }

        private static float ReadPlayerStat(BalanceScenarioAsset scenario, AttributeId attributeId)
        {
            if (scenario?.playerAttributeProfile != null
                && scenario.playerAttributeProfile.TryGetBaseValue(
                    attributeId, out float value))
                return value;
            return UPlayGroundAttributeDefaults.Get(attributeId);
        }

        private static float ReadPlayerAttackPower(BalanceScenarioAsset scenario, BalanceScenarioInput fallbackInput)
        {
            if (scenario?.playerAttributeProfile != null
                && scenario.playerAttributeProfile.TryGetBaseValue(
                    global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, out float attackPower))
                return attackPower;

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

            if (result.MonsterTimeToDeathWithBreak < result.TargetDuration)
                return BalanceCheckStatus.TooEasy;

            return BalanceCheckStatus.Stable;
        }

        private static string BuildSummary(BalanceScenarioResult result)
        {
            return result.Status switch
            {
                BalanceCheckStatus.InvalidData => "데이터 누락",
                BalanceCheckStatus.TooEasy => result.MonsterTimeToDeathWithBreak < result.MonsterTimeToDeath
                    ? $"몬스터가 브레이크 포함 {FormatTime(result.MonsterTimeToDeathWithBreak)}에 쓰러질 것으로 추정"
                    : $"몬스터가 {FormatTime(result.MonsterTimeToDeath)}에 쓰러질 것으로 추정",
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
