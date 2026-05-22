using UPlayGround.AI.BehaviorTree;
using UPlayGround.AI.CombatDecision;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>
    /// 몬스터 전투 의도를 점수화한다.
    /// 실행 상태 전환은 하지 않고 BT가 읽을 Blackboard 값만 계산한다.
    /// </summary>
    public class EnemyCombatDecisionEvaluator : MonoBehaviour
    {
        private const float DefaultAggression = 0.5f;
        private const float DefaultReactionChance = 0.35f;
        private const float DefaultCounterChance = 0.2f;
        private const float DefaultPunishChance = 0.35f;
        private const float DefaultRetreatChance = 0.2f;
        private const float DefaultGuardChance = 0.25f;
        private const float DefaultCircleWeight = 0.35f;

        private EnemyDetection _detection;
        private EnemyCombat _combat;
        private EnemyTacticalMemory _memory;
        private EnemyAIContext _context;
        private PoiseStat _poise;

        private readonly IntentScore[] _scores = new IntentScore[9];

        private void Awake()
        {
            CacheComponents();
        }

        public bool TryEvaluate(Blackboard blackboard, out CombatIntentEvaluation evaluation)
        {
            CacheComponents();

            evaluation = default;
            if (blackboard == null || _detection == null || !_detection.HasTarget)
                return false;

            var distance = _detection.DistanceToTarget;
            var behavior = _context?.BehaviorData;
            var phase = _context?.CurrentPhase;

            var optimalDistance = ReadFloat(blackboard, "optimalCombatDistance", _context?.OptimalCombatDistance ?? behavior?.optimalCombatDistance ?? 2.5f);
            var minDistance = ReadFloat(blackboard, "minCombatDistance", _context?.MinCombatDistance ?? behavior?.minCombatDistance ?? 1.5f);
            var personalSpace = ReadFloat(blackboard, "personalSpaceDistance", _context?.PersonalSpaceDistance ?? behavior?.personalSpaceDistance ?? 0.8f);
            var preferredRange = ReadFloat(blackboard, EnemyBlackboardKeys.AIPreferredRange, optimalDistance);
            var aggression = Read01(blackboard, EnemyBlackboardKeys.AIAggression, DefaultAggression);
            var reactionChance = Read01(blackboard, EnemyBlackboardKeys.AIReactionChance, DefaultReactionChance);
            var counterChance = Read01(blackboard, EnemyBlackboardKeys.AICounterChance, DefaultCounterChance);
            var punishChance = Read01(blackboard, EnemyBlackboardKeys.AIPunishRecoveryChance, DefaultPunishChance);
            var retreatChance = Read01(blackboard, EnemyBlackboardKeys.RetreatChance, behavior?.retreatChance ?? DefaultRetreatChance);
            var guardChance = Read01(blackboard, EnemyBlackboardKeys.GuardChance, behavior?.guardChance ?? DefaultGuardChance);
            var circleWeight = ReadFloat(blackboard, "circleWeight", DefaultCircleWeight);
            var minRetreatCooldown = ReadFloat(blackboard, EnemyBlackboardKeys.AIMinRetreatCooldown, 1.5f);

            var isPlayerAttacking = _memory != null && _memory.IsPlayerAttacking();
            var isPlayerGuarding = _memory != null && _memory.IsPlayerGuarding();
            var isPlayerStaggered = _memory != null && _memory.IsPlayerStaggered();
            var isPlayerRecovering = _memory != null && _memory.IsPlayerRecovering();
            var isPlayerDodgingFrequently = _memory != null && _memory.IsPlayerDodgingFrequently();
            var isPlayerAttackingFrequently = _memory != null && _memory.IsPlayerAttackingFrequently();
            var isPlayerGuardingFrequently = _memory != null && _memory.IsPlayerGuardingFrequently();
            var isPlayerRecoveringFrequently = _memory != null && _memory.IsPlayerRecoveringFrequently();
            var wasHitRecently = _memory != null && _memory.WasHitRecently();
            var timeSinceRetreat = _memory?.TimeSinceLastRetreat() ?? 999f;
            var hitAccuracy = _memory?.GetHitAccuracy() ?? 0.5f;
            var playerReadSummary = _memory?.BuildPlayerReadSummary() ?? "Dodge=0, Guard=0, Attack=0, Recover=0";
            var isPoiseBroken = _poise != null && _poise.IsPoiseBroken;
            var hpPercent = _context?.HealthPercent ?? 1f;
            var canUseSkill = _context?.CanUseSkill() ?? false;
            var hasAvailableAttack = _combat != null && _combat.HasAvailableSkillAtDistance(distance);
            var actionDelayElapsed = !blackboard.TryGetFloat(EnemyBlackboardKeys.NextActionAllowedTime, out var nextActionTime)
                                     || Time.time >= nextActionTime;

            var inAttackRange = distance <= optimalDistance && hasAvailableAttack;
            var tooClose = distance <= personalSpace;
            var underPreferredRange = distance < Mathf.Max(personalSpace, preferredRange - 0.45f);
            var overPreferredRange = distance > preferredRange + 0.75f;
            var lowHealth = hpPercent <= 0.35f;

            var attackScore = 0.10f + aggression * 0.42f;
            if (inAttackRange && actionDelayElapsed) attackScore += 0.45f;
            if (canUseSkill) attackScore += 0.08f;
            if (isPlayerStaggered) attackScore += 0.18f;
            if (isPlayerRecoveringFrequently) attackScore += 0.10f;
            if (!hasAvailableAttack || !actionDelayElapsed) attackScore *= 0.55f;

            var punishScore = 0.03f + punishChance * 0.45f;
            if (isPlayerRecovering || isPlayerStaggered) punishScore += 0.45f;
            if (isPlayerDodgingFrequently) punishScore += 0.18f;
            if (isPlayerRecoveringFrequently) punishScore += 0.18f;
            if (inAttackRange && actionDelayElapsed) punishScore += 0.22f;
            if (!hasAvailableAttack || !actionDelayElapsed) punishScore *= 0.45f;

            var counterScore = 0.04f + counterChance * 0.5f;
            if (isPlayerAttacking && distance <= optimalDistance) counterScore += reactionChance * 0.45f + 0.16f;
            if (tooClose && isPlayerAttacking) counterScore += 0.16f;
            if (isPlayerAttackingFrequently) counterScore += 0.20f;
            if (!actionDelayElapsed) counterScore *= 0.65f;

            var pressureScore = 0.12f + aggression * 0.24f + Mathf.Max(0f, circleWeight) * 0.22f;
            if (!isPlayerAttacking && distance <= preferredRange + 1.5f) pressureScore += 0.16f;
            if (isPlayerGuarding || isPlayerDodgingFrequently) pressureScore += 0.12f;
            if (isPlayerGuardingFrequently) pressureScore += 0.14f;
            if (!actionDelayElapsed) pressureScore += 0.10f;

            var chaseScore = overPreferredRange ? 0.55f + aggression * 0.25f : 0.05f;
            if (distance > optimalDistance + 1.5f) chaseScore += 0.18f;

            var retreatScore = 0.02f + retreatChance * 0.42f;
            if (tooClose) retreatScore += 0.35f;
            if (wasHitRecently) retreatScore += 0.18f;
            if (isPoiseBroken) retreatScore += 0.30f;
            if (lowHealth) retreatScore += 0.18f;
            if (isPlayerAttackingFrequently && tooClose) retreatScore += 0.12f;
            if (timeSinceRetreat < minRetreatCooldown) retreatScore *= 0.35f;

            var keepDistanceScore = 0.05f;
            if (underPreferredRange) keepDistanceScore += 0.34f;
            if (isPlayerAttacking && distance <= minDistance) keepDistanceScore += 0.20f;
            if (isPlayerAttackingFrequently && distance <= optimalDistance) keepDistanceScore += 0.12f;
            if (overPreferredRange) keepDistanceScore *= 0.45f;

            var defendScore = 0.04f + guardChance * 0.38f;
            if (_context?.HasGuardMotion == true && isPlayerAttacking && distance <= optimalDistance) defendScore += 0.30f;
            if (wasHitRecently && !isPoiseBroken) defendScore += 0.12f;
            if (isPlayerAttackingFrequently) defendScore += 0.16f;

            var recoverScore = lowHealth ? 0.22f : 0.02f;
            if (wasHitRecently && lowHealth) recoverScore += 0.18f;
            if (tooClose || isPlayerAttacking) recoverScore *= 0.55f;

            ApplyPhaseWeights(
                phase,
                ref attackScore,
                ref punishScore,
                ref counterScore,
                ref pressureScore,
                ref chaseScore,
                ref retreatScore,
                ref keepDistanceScore,
                ref defendScore,
                ref recoverScore);

            ApplyRoleWeights(
                behavior != null ? behavior.aiRole : EnemyAIRole.Melee,
                ref attackScore,
                ref punishScore,
                ref counterScore,
                ref pressureScore,
                ref chaseScore,
                ref retreatScore,
                ref keepDistanceScore,
                ref defendScore,
                ref recoverScore);

            var rhythmPhase = ResolveRhythmPhase(actionDelayElapsed, attackScore, pressureScore, retreatScore, overPreferredRange);

            ApplyLastIntentPenalty(blackboard, ref attackScore, CombatIntent.Attack);
            ApplyLastIntentPenalty(blackboard, ref punishScore, CombatIntent.Punish);
            ApplyLastIntentPenalty(blackboard, ref counterScore, CombatIntent.Counter);
            ApplyLastIntentPenalty(blackboard, ref pressureScore, CombatIntent.Pressure);
            ApplyLastIntentPenalty(blackboard, ref chaseScore, CombatIntent.Chase);
            ApplyLastIntentPenalty(blackboard, ref retreatScore, CombatIntent.Retreat);
            ApplyLastIntentPenalty(blackboard, ref keepDistanceScore, CombatIntent.KeepDistance);
            ApplyLastIntentPenalty(blackboard, ref defendScore, CombatIntent.Defend);
            ApplyLastIntentPenalty(blackboard, ref recoverScore, CombatIntent.Recover);

            FillScores(
                Mathf.Clamp01(attackScore),
                Mathf.Clamp01(punishScore),
                Mathf.Clamp01(counterScore),
                Mathf.Clamp01(pressureScore),
                Mathf.Clamp01(chaseScore),
                Mathf.Clamp01(retreatScore),
                Mathf.Clamp01(keepDistanceScore),
                Mathf.Clamp01(defendScore),
                Mathf.Clamp01(recoverScore));

            var selected = SelectWeightedTopIntent();

            var role = behavior != null ? behavior.aiRole : EnemyAIRole.Melee;
            var reason = BuildReason(selected.Intent, selected.Score, distance, hitAccuracy, phase, role, playerReadSummary);
            evaluation = new CombatIntentEvaluation(
                selected.Intent,
                GetScore(CombatIntent.Attack),
                GetScore(CombatIntent.Punish),
                GetScore(CombatIntent.Counter),
                GetScore(CombatIntent.Pressure),
                GetScore(CombatIntent.Chase),
                GetScore(CombatIntent.Retreat),
                GetScore(CombatIntent.KeepDistance),
                GetScore(CombatIntent.Defend),
                GetScore(CombatIntent.Recover),
                rhythmPhase,
                reason);
            return true;
        }

        private void CacheComponents()
        {
            _detection ??= GetComponent<EnemyDetection>();
            _combat ??= GetComponent<EnemyCombat>();
            _memory ??= GetComponent<EnemyTacticalMemory>();
            _context ??= GetComponent<EnemyAIContext>();
            _poise ??= GetComponent<PoiseStat>();
        }

        private void FillScores(
            float attack,
            float punish,
            float counter,
            float pressure,
            float chase,
            float retreat,
            float keepDistance,
            float defend,
            float recover)
        {
            _scores[0] = new IntentScore(CombatIntent.Attack, attack);
            _scores[1] = new IntentScore(CombatIntent.Punish, punish);
            _scores[2] = new IntentScore(CombatIntent.Counter, counter);
            _scores[3] = new IntentScore(CombatIntent.Pressure, pressure);
            _scores[4] = new IntentScore(CombatIntent.Chase, chase);
            _scores[5] = new IntentScore(CombatIntent.Retreat, retreat);
            _scores[6] = new IntentScore(CombatIntent.KeepDistance, keepDistance);
            _scores[7] = new IntentScore(CombatIntent.Defend, defend);
            _scores[8] = new IntentScore(CombatIntent.Recover, recover);
        }

        private IntentScore SelectWeightedTopIntent()
        {
            System.Array.Sort(_scores, (a, b) => b.Score.CompareTo(a.Score));

            var candidateCount = Mathf.Min(3, _scores.Length);
            var totalWeight = 0f;
            for (var i = 0; i < candidateCount; i++)
                totalWeight += Mathf.Max(0.01f, _scores[i].Score);

            var roll = Random.Range(0f, totalWeight);
            var accumulated = 0f;
            for (var i = 0; i < candidateCount; i++)
            {
                accumulated += Mathf.Max(0.01f, _scores[i].Score);
                if (roll <= accumulated)
                    return _scores[i];
            }

            return _scores[0];
        }

        private static void ApplyPhaseWeights(
            BehaviorPhase phase,
            ref float attack,
            ref float punish,
            ref float counter,
            ref float pressure,
            ref float chase,
            ref float retreat,
            ref float keepDistance,
            ref float defend,
            ref float recover)
        {
            if (phase == null)
                return;

            attack *= Mathf.Max(0f, phase.attackWeight);
            punish *= Mathf.Max(0f, phase.punishWeight);
            counter *= Mathf.Max(0f, phase.counterWeight);
            pressure *= Mathf.Max(0f, phase.pressureWeight);
            chase *= Mathf.Max(0f, phase.chaseWeight);
            retreat *= Mathf.Max(0f, phase.retreatWeight);
            keepDistance *= Mathf.Max(0f, phase.keepDistanceWeight);
            defend *= Mathf.Max(0f, phase.defendWeight);
            recover *= Mathf.Max(0f, phase.recoverWeight);
        }

        private static void ApplyRoleWeights(
            EnemyAIRole role,
            ref float attack,
            ref float punish,
            ref float counter,
            ref float pressure,
            ref float chase,
            ref float retreat,
            ref float keepDistance,
            ref float defend,
            ref float recover)
        {
            switch (role)
            {
                case EnemyAIRole.RangedSupport:
                    attack *= 0.85f;
                    punish *= 0.9f;
                    counter *= 0.75f;
                    pressure *= 1.15f;
                    chase *= 0.75f;
                    retreat *= 1.15f;
                    keepDistance *= 1.35f;
                    defend *= 1.15f;
                    recover *= 1.05f;
                    break;

                case EnemyAIRole.RangedMain:
                    attack *= 1.1f;
                    punish *= 0.95f;
                    counter *= 0.75f;
                    pressure *= 1.05f;
                    chase *= 0.8f;
                    retreat *= 1.2f;
                    keepDistance *= 1.45f;
                    defend *= 1.0f;
                    recover *= 0.95f;
                    break;

                case EnemyAIRole.Healer:
                    attack *= 0.65f;
                    punish *= 0.7f;
                    counter *= 0.65f;
                    pressure *= 0.9f;
                    chase *= 0.65f;
                    retreat *= 1.25f;
                    keepDistance *= 1.25f;
                    defend *= 1.25f;
                    recover *= 1.55f;
                    break;

                case EnemyAIRole.Summoner:
                    attack *= 0.9f;
                    punish *= 0.8f;
                    counter *= 0.65f;
                    pressure *= 1.35f;
                    chase *= 0.75f;
                    retreat *= 1.05f;
                    keepDistance *= 1.25f;
                    defend *= 1.05f;
                    recover *= 1.05f;
                    break;
            }
        }

        private float GetScore(CombatIntent intent)
        {
            for (var i = 0; i < _scores.Length; i++)
            {
                if (_scores[i].Intent == intent)
                    return _scores[i].Score;
            }

            return 0f;
        }

        private static void ApplyLastIntentPenalty(Blackboard blackboard, ref float score, CombatIntent intent)
        {
            if (!blackboard.TryGetString(EnemyBlackboardKeys.DecisionLastIntent, out var lastIntent)
                || lastIntent != intent.ToString())
                return;

            var repeatCount = blackboard.TryGetInt(EnemyBlackboardKeys.DecisionConsecutiveIntentCount, out var count)
                ? Mathf.Clamp(count, 1, 3)
                : 1;
            score *= Mathf.Pow(0.85f, repeatCount);
        }

        private static string ResolveRhythmPhase(bool actionDelayElapsed, float attackScore, float pressureScore, float retreatScore, bool overPreferredRange)
        {
            if (!actionDelayElapsed)
                return "Observe";
            if (retreatScore >= 0.55f)
                return "Disengage";
            if (overPreferredRange)
                return "ReEnter";
            return attackScore >= pressureScore ? "CommitAttack" : "Pressure";
        }

        private static string BuildReason(CombatIntent intent, float score, float distance, float hitAccuracy, BehaviorPhase phase, EnemyAIRole role, string playerReadSummary)
        {
            var phaseName = string.IsNullOrWhiteSpace(phase?.phaseName) ? "None" : phase.phaseName;
            return $"{intent} score={score:0.00}, distance={distance:0.00}, hitAccuracy={hitAccuracy:0.00}, phase={phaseName}, role={role}, read[{playerReadSummary}]";
        }

        private static float ReadFloat(Blackboard blackboard, string key, float fallback)
            => blackboard.TryGetFloat(key, out var value) ? value : fallback;

        private static float Read01(Blackboard blackboard, string key, float fallback)
            => Mathf.Clamp01(ReadFloat(blackboard, key, fallback));

        private readonly struct IntentScore
        {
            public IntentScore(CombatIntent intent, float score)
            {
                Intent = intent;
                Score = score;
            }

            public CombatIntent Intent { get; }
            public float Score { get; }
        }
    }
}
