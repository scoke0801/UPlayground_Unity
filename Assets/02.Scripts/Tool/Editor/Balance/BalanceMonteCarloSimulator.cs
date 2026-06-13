#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 공격 데이터 기반 확률적 전투 시뮬레이션(몬테카를로).
    /// BalanceCombatEstimator가 기대값(평균) 하나만 내는 것과 달리,
    /// 스킬 선택/쿨다운/회피·패리 확률/크리티컬을 실제로 굴려 N회 반복 → TTK 분포(P10/P50/P90)와
    /// 플레이어 사망률을 산출한다. 수식이 못 잡는 분산과 꼬리 위험(불운 연속 피격)을 드러내는 용도.
    /// 가정은 Estimator와 동일: 거리 고정, 멀티히트는 일괄 적용, 이동/포지셔닝은 모델링하지 않음.
    /// </summary>
    public static class BalanceMonteCarloSimulator
    {
        public const float MaxFightDuration = 300f;
        private const float TickStep = 0.05f;

        public sealed class SimulationResult
        {
            public int Runs;
            public int MonsterKills;
            public int PlayerDeaths;
            public int Timeouts;
            public float KillRate => Runs > 0 ? (float)MonsterKills / Runs : 0f;
            public float DeathRate => Runs > 0 ? (float)PlayerDeaths / Runs : 0f;
            public float KillP10;
            public float KillP50;
            public float KillP90;
            public float KillAvg;
            public float AvgDamageTakenPerFight;
            public readonly List<float> KillTimes = new();
            public int[] Histogram;
            public float HistogramMin;
            public float HistogramMax;
            public string Error;
        }

        private sealed class SkillSim
        {
            public float Damage;
            public float PoiseDamage;
            public float Weight;
            public float Cooldown;
            public AttackDefenseType DefenseType;
            public float ReadyAt;
        }

        public static SimulationResult Run(
            ActorDefinitionSO actor,
            BalanceScenarioAsset scenario,
            BalanceScenarioInput fallback,
            int runs,
            int seed)
        {
            var result = new SimulationResult { Runs = Mathf.Max(1, runs) };

            if (actor == null || actor.statData == null)
            {
                result.Error = "statData가 없어 시뮬레이션할 수 없습니다.";
                return result;
            }

            int monsterLevel = scenario != null && !scenario.useActorDefinitionLevel
                ? Mathf.Max(1, scenario.overrideMonsterLevel)
                : Mathf.Max(1, actor.level);
            float assumedDistance = scenario != null ? scenario.assumedDistance : fallback.AssumedDistance;

            // 몬스터 측 파라미터
            float monsterMaxHp = Mathf.Max(1f, actor.statData.GetBase(StatType.MaxHealth));
            float monsterAtk = Mathf.Max(0f, actor.statData.GetBase(StatType.AttackPower));
            float monsterDef = Mathf.Clamp01(actor.statData.GetBase(StatType.Defense));

            List<EnemyAttackInfo> usable = BalanceAttackAnalyzer.GetUsableEnemySkills(actor.attackData, assumedDistance, monsterLevel);
            float globalCooldown = actor.attackData != null ? Mathf.Max(0.05f, actor.attackData.globalCooldown) : 1f;

            // 플레이어 측 파라미터 (Estimator와 동일한 읽기 규칙)
            float playerMaxHp = Mathf.Max(1f, ReadPlayerStat(scenario, StatType.MaxHealth));
            float playerDef = Mathf.Clamp01(ReadPlayerStat(scenario, StatType.Defense));
            float playerAtkPower = ReadPlayerAttackPower(scenario, fallback);
            float critRate = Mathf.Clamp01(ReadPlayerStat(scenario, StatType.CritRate));
            float critMultiplier = Mathf.Max(1f, ReadPlayerStat(scenario, StatType.CritMultiplier));
            float attackInterval = Mathf.Max(0.05f, scenario != null ? scenario.playerAttackInterval : fallback.PlayerAttackInterval);

            List<(float damage, float breakDamage)> playerAttacks = BuildPlayerAttackPool(scenario);
            float manualDamagePerSwing = (scenario != null ? scenario.manualPlayerDps : fallback.ManualPlayerDps) * attackInterval;

            // 방어 가정 확률
            float hitReceiveRate = Mathf.Clamp01(scenario != null ? scenario.hitReceiveRate : 0.45f);
            float dodgeSuccessRate = Mathf.Clamp01(scenario != null ? scenario.dodgeSuccessRate : 0.15f);
            float parrySuccessRate = Mathf.Clamp01(scenario != null ? scenario.parrySuccessRate : 0.05f);
            float guardMitigationRate = Mathf.Clamp01(scenario != null ? scenario.guardMitigationRate : 0.35f);

            // 브레이크 게이지
            MonsterBreakGaugeSO breakData = actor.breakGaugeData;
            bool useBreak = breakData != null && breakData.useBreakGauge;
            float breakGaugeMax = 1f;
            if (useBreak)
            {
                float gradeScale = breakData.gradePolicy != null ? breakData.gradePolicy.GetGaugeMultiplier(actor.grade) : 1f;
                breakGaugeMax = Mathf.Max(1f, breakData.maxGauge * gradeScale);
            }

            var skillSims = new List<SkillSim>(usable.Count);
            for (int i = 0; i < usable.Count; i++)
            {
                skillSims.Add(new SkillSim
                {
                    Damage = BalanceAttackAnalyzer.SumDamage(usable[i].baseInfo),
                    PoiseDamage = BalanceAttackAnalyzer.SumPoiseDamage(usable[i].baseInfo),
                    Weight = Mathf.Max(0f, usable[i].selectionWeight),
                    Cooldown = Mathf.Max(0.05f, usable[i].cooldown),
                    DefenseType = usable[i].defenseType,
                });
            }

            if (skillSims.Count == 0 && playerAttacks.Count == 0 && manualDamagePerSwing <= 0f)
            {
                result.Error = "사용 가능한 공격이 양쪽 모두 없습니다.";
                return result;
            }

            float totalDamageTaken = 0f;
            for (int run = 0; run < result.Runs; run++)
            {
                var random = new System.Random(seed + run * 7919);
                float fightDamageTaken = SimulateFight(
                    random, skillSims, globalCooldown,
                    monsterMaxHp, monsterAtk, monsterDef,
                    playerMaxHp, playerDef, playerAtkPower, critRate, critMultiplier,
                    attackInterval, playerAttacks, manualDamagePerSwing,
                    hitReceiveRate, dodgeSuccessRate, parrySuccessRate, guardMitigationRate,
                    useBreak, breakData, breakGaugeMax,
                    out float killTime, out bool playerDied);

                totalDamageTaken += fightDamageTaken;
                if (killTime > 0f)
                {
                    result.MonsterKills++;
                    result.KillTimes.Add(killTime);
                }
                else if (playerDied)
                {
                    result.PlayerDeaths++;
                }
                else
                {
                    result.Timeouts++;
                }
            }

            result.AvgDamageTakenPerFight = totalDamageTaken / result.Runs;
            ComputeStatistics(result);
            return result;
        }

        private static float SimulateFight(
            System.Random random,
            List<SkillSim> skills,
            float globalCooldown,
            float monsterMaxHp, float monsterAtk, float monsterDef,
            float playerMaxHp, float playerDef, float playerAtkPower, float critRate, float critMultiplier,
            float attackInterval, List<(float damage, float breakDamage)> playerAttacks, float manualDamagePerSwing,
            float hitReceiveRate, float dodgeSuccessRate, float parrySuccessRate, float guardMitigationRate,
            bool useBreak, MonsterBreakGaugeSO breakData, float breakGaugeMax,
            out float killTime, out bool playerDied)
        {
            killTime = 0f;
            playerDied = false;

            float monsterHp = monsterMaxHp;
            float playerHp = playerMaxHp;
            float damageTaken = 0f;

            float nextPlayerAttack = attackInterval;
            float monsterGlobalReady = globalCooldown; // 첫 공격도 글로벌 쿨다운 이후
            for (int i = 0; i < skills.Count; i++)
                skills[i].ReadyAt = 0f;

            float breakGauge = breakGaugeMax;
            float exposedUntil = -1f;
            float breakResist = useBreak ? Mathf.Clamp01(breakData.breakResist) : 0f;
            float exposedMultiplier = useBreak ? Mathf.Max(0f, breakData.damageTakenMultiplierWhileExposed) : 1f;

            for (float t = 0f; t < MaxFightDuration; t += TickStep)
            {
                bool exposed = useBreak && t < exposedUntil;

                // 플레이어 공격
                if (t >= nextPlayerAttack)
                {
                    nextPlayerAttack += attackInterval;

                    float rawDamage;
                    float breakDamage;
                    if (playerAttacks.Count > 0)
                    {
                        (float damage2, float breakDmg) = playerAttacks[random.Next(playerAttacks.Count)];
                        rawDamage = damage2;
                        breakDamage = breakDmg;
                    }
                    else
                    {
                        rawDamage = manualDamagePerSwing;
                        breakDamage = 0f;
                    }

                    float damage = rawDamage * playerAtkPower * (1f - monsterDef);
                    if (random.NextDouble() < critRate)
                        damage *= critMultiplier;
                    if (exposed)
                        damage *= exposedMultiplier;

                    monsterHp -= damage;
                    if (monsterHp <= 0f)
                    {
                        killTime = t;
                        return damageTaken;
                    }

                    // 브레이크 게이지 누적 (노출 중에는 누적하지 않음)
                    if (useBreak && !exposed && breakDamage > 0f)
                    {
                        breakGauge -= breakDamage * (1f - breakResist);
                        if (breakGauge <= 0f)
                        {
                            exposedUntil = t + Mathf.Max(0.1f, breakData.exposedDuration);
                            breakGauge = breakGaugeMax * (1f - Mathf.Clamp01(breakData.resetGaugeRatioOnExpire));
                        }
                    }
                }

                // 몬스터 공격 — 노출(브레이크) 중에는 행동 불가
                if (!exposed && t >= monsterGlobalReady && skills.Count > 0)
                {
                    SkillSim selected = SelectSkill(random, skills, t);
                    if (selected != null)
                    {
                        selected.ReadyAt = t + selected.Cooldown;
                        monsterGlobalReady = t + globalCooldown;

                        // 명중 판정: 피격률 × (1-회피), Parryable은 패리 확률로 무효화
                        bool landed = random.NextDouble() < hitReceiveRate * (1f - dodgeSuccessRate);
                        if (landed && selected.DefenseType == AttackDefenseType.Parryable)
                            landed = random.NextDouble() >= parrySuccessRate;

                        if (landed)
                        {
                            float damage = selected.Damage * monsterAtk * (1f - playerDef);
                            if (selected.DefenseType == AttackDefenseType.GuardableOnly)
                                damage *= 1f - guardMitigationRate;

                            playerHp -= damage;
                            damageTaken += damage;
                            if (playerHp <= 0f)
                            {
                                playerDied = true;
                                return damageTaken;
                            }
                        }
                    }
                }
            }

            return damageTaken;
        }

        private static SkillSim SelectSkill(System.Random random, List<SkillSim> skills, float time)
        {
            float totalWeight = 0f;
            for (int i = 0; i < skills.Count; i++)
            {
                if (time >= skills[i].ReadyAt)
                    totalWeight += skills[i].Weight;
            }

            if (totalWeight <= 0f)
                return null;

            float roll = (float)(random.NextDouble() * totalWeight);
            float accumulated = 0f;
            for (int i = 0; i < skills.Count; i++)
            {
                if (time < skills[i].ReadyAt)
                    continue;
                accumulated += skills[i].Weight;
                if (roll <= accumulated)
                    return skills[i];
            }

            return null;
        }

        private static List<(float damage, float breakDamage)> BuildPlayerAttackPool(BalanceScenarioAsset scenario)
        {
            var pool = new List<(float, float)>();
            PlayerAttackDataSO data = scenario != null ? scenario.playerAttackData : null;
            if (data == null)
                return pool;

            AddAttacks(pool, data.liteComboAttackList);
            AddAttacks(pool, data.heavyComboAttackList);
            AddAttacks(pool, data.jumpAttackList);
            AddAttacks(pool, data.dashAttackList);
            AddAttacks(pool, data.skillAttackList);
            AddAttack(pool, data.counterAttack);
            AddAttack(pool, data.parryCounterAttack);
            AddAttack(pool, data.entryAttack);
            AddAttack(pool, data.swapEvadeCounterAttack);
            AddAttack(pool, data.swapSpecialAttack);
            return pool;
        }

        private static void AddAttacks(List<(float, float)> pool, List<PlayerAttackInfo> list)
        {
            if (list == null)
                return;
            for (int i = 0; i < list.Count; i++)
                AddAttack(pool, list[i]);
        }

        private static void AddAttack(List<(float, float)> pool, PlayerAttackInfo info)
        {
            if (info?.baseInfo == null)
                return;

            float damage = BalanceAttackAnalyzer.SumDamage(info.baseInfo);
            if (damage <= 0f)
                return;

            pool.Add((damage, BalanceAttackAnalyzer.SumBreakDamage(info.baseInfo)));
        }

        private static void ComputeStatistics(SimulationResult result)
        {
            if (result.KillTimes.Count == 0)
                return;

            result.KillTimes.Sort();
            result.KillP10 = Percentile(result.KillTimes, 0.10f);
            result.KillP50 = Percentile(result.KillTimes, 0.50f);
            result.KillP90 = Percentile(result.KillTimes, 0.90f);

            float sum = 0f;
            for (int i = 0; i < result.KillTimes.Count; i++)
                sum += result.KillTimes[i];
            result.KillAvg = sum / result.KillTimes.Count;

            // 히스토그램 (24 버킷)
            const int buckets = 24;
            result.HistogramMin = result.KillTimes[0];
            result.HistogramMax = Mathf.Max(result.KillTimes[result.KillTimes.Count - 1], result.HistogramMin + 0.1f);
            result.Histogram = new int[buckets];
            float range = result.HistogramMax - result.HistogramMin;
            for (int i = 0; i < result.KillTimes.Count; i++)
            {
                int bucket = Mathf.Clamp(Mathf.FloorToInt((result.KillTimes[i] - result.HistogramMin) / range * buckets), 0, buckets - 1);
                result.Histogram[bucket]++;
            }
        }

        private static float Percentile(List<float> sorted, float percentile)
        {
            if (sorted.Count == 0)
                return 0f;
            int index = Mathf.Clamp(Mathf.RoundToInt(percentile * (sorted.Count - 1)), 0, sorted.Count - 1);
            return sorted[index];
        }

        private static float ReadPlayerStat(BalanceScenarioAsset scenario, StatType type)
        {
            if (scenario?.playerStatData != null)
                return scenario.playerStatData.GetBase(type);
            return ActorStatSO.GetDefault(type);
        }

        private static float ReadPlayerAttackPower(BalanceScenarioAsset scenario, BalanceScenarioInput fallback)
        {
            if (scenario?.playerStatData != null)
                return scenario.playerStatData.GetBase(StatType.AttackPower);
            if (scenario != null)
                return scenario.manualPlayerAttackPower;
            return fallback.PlayerAttackPower;
        }
    }
}
#endif
