using UPlayGround.AI.BehaviorTree;
using UPlayGround.AI.CombatDecision;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Group;
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

            var optimalDistance = ReadFloat(blackboard, EnemyBlackboardKeys.OptimalCombatDistance, _context?.OptimalCombatDistance ?? behavior?.optimalCombatDistance ?? 2.5f);
            var minDistance = ReadFloat(blackboard, EnemyBlackboardKeys.MinCombatDistance, _context?.MinCombatDistance ?? behavior?.minCombatDistance ?? 1.5f);
            var personalSpace = ReadFloat(blackboard, EnemyBlackboardKeys.PersonalSpaceDistance, _context?.PersonalSpaceDistance ?? behavior?.personalSpaceDistance ?? 0.8f);
            var preferredRange = ReadFloat(blackboard, EnemyBlackboardKeys.AIPreferredRange, optimalDistance);
            var aggression = Read01(blackboard, EnemyBlackboardKeys.AIAggression, DefaultAggression);
            var reactionChance = Read01(blackboard, EnemyBlackboardKeys.AIReactionChance, DefaultReactionChance);
            var counterChance = Read01(blackboard, EnemyBlackboardKeys.AICounterChance, DefaultCounterChance);
            var punishChance = Read01(blackboard, EnemyBlackboardKeys.AIPunishRecoveryChance, DefaultPunishChance);
            var retreatChance = Read01(blackboard, EnemyBlackboardKeys.RetreatChance, behavior?.retreatChance ?? DefaultRetreatChance);
            var guardChance = Read01(blackboard, EnemyBlackboardKeys.GuardChance, behavior?.guardChance ?? DefaultGuardChance);
            var circleWeight = ReadFloat(blackboard, EnemyBlackboardKeys.CircleWeight, DefaultCircleWeight);
            var minRetreatCooldown = ReadFloat(blackboard, EnemyBlackboardKeys.AIMinRetreatCooldown, 1.5f);
            var groupMemory = _context?.CurrentGroupMemory;

            var isPlayerAttacking = groupMemory != null ? groupMemory.IsPlayerAttacking : _memory != null && _memory.IsPlayerAttacking();
            var isPlayerGuarding = groupMemory != null ? groupMemory.IsPlayerGuarding : _memory != null && _memory.IsPlayerGuarding();
            var isPlayerStaggered = groupMemory != null ? groupMemory.IsPlayerStaggered : _memory != null && _memory.IsPlayerStaggered();
            var isPlayerRecovering = groupMemory != null ? groupMemory.IsPlayerRecovering : _memory != null && _memory.IsPlayerRecovering();
            var isPlayerDodgingFrequently = groupMemory != null ? groupMemory.IsPlayerDodgingFrequently() : _memory != null && _memory.IsPlayerDodgingFrequently();
            var isPlayerAttackingFrequently = groupMemory != null ? groupMemory.IsPlayerAttackingFrequently() : _memory != null && _memory.IsPlayerAttackingFrequently();
            var isPlayerGuardingFrequently = groupMemory != null ? groupMemory.IsPlayerGuardingFrequently() : _memory != null && _memory.IsPlayerGuardingFrequently();
            var isPlayerRecoveringFrequently = groupMemory != null ? groupMemory.IsPlayerRecoveringFrequently() : _memory != null && _memory.IsPlayerRecoveringFrequently();
            var wasHitRecently = _memory != null && _memory.WasHitRecently();
            var timeSinceRetreat = _memory?.TimeSinceLastRetreat() ?? 999f;
            var hitAccuracy = groupMemory != null ? groupMemory.HitAccuracyAgainstPlayer : _memory?.GetHitAccuracy() ?? 0.5f;
            var playerReadSummary = groupMemory != null ? groupMemory.BuildPlayerReadSummary() : _memory?.BuildPlayerReadSummary() ?? "Dodge=0, Guard=0, Attack=0, Recover=0";
            var isPoiseBroken = _poise != null && _poise.IsPoiseBroken;
            var hpPercent = _context?.HealthPercent ?? 1f;
            var canUseSkill = _context?.CanUseSkill() ?? false;
            var hasAvailableAttack = _combat != null && _combat.HasAvailableSkillAtDistance(distance);
            var actionDelayElapsed = !blackboard.TryGetFloat(EnemyBlackboardKeys.NextActionAllowedTime, out var nextActionTime)
                                     || Time.time >= nextActionTime;

            var overPreferredRange = distance > preferredRange + 0.75f;
            var hasGuardMotion = _context != null && _context.HasGuardMotion;

            var ctx = new IntentEvaluationContext
            {
                Distance = distance,
                OptimalDistance = optimalDistance,
                MinDistance = minDistance,
                PersonalSpace = personalSpace,
                PreferredRange = preferredRange,
                HealthPercent = hpPercent,
                Aggression = aggression,
                ReactionChance = reactionChance,
                PunishChance = punishChance,
                CounterChance = counterChance,
                RetreatChance = retreatChance,
                GuardChance = guardChance,
                CircleWeight = circleWeight,
                MinRetreatCooldown = minRetreatCooldown,
                TimeSinceRetreat = timeSinceRetreat,
                ActionDelayElapsed = actionDelayElapsed,
                CanUseSkill = canUseSkill,
                HasAvailableAttack = hasAvailableAttack,
                HasGuardMotion = hasGuardMotion,
                IsPlayerAttacking = isPlayerAttacking,
                IsPlayerGuarding = isPlayerGuarding,
                IsPlayerStaggered = isPlayerStaggered,
                IsPlayerRecovering = isPlayerRecovering,
                IsPlayerDodgingFrequently = isPlayerDodgingFrequently,
                IsPlayerAttackingFrequently = isPlayerAttackingFrequently,
                IsPlayerGuardingFrequently = isPlayerGuardingFrequently,
                IsPlayerRecoveringFrequently = isPlayerRecoveringFrequently,
                WasHitRecently = wasHitRecently,
                IsPoiseBroken = isPoiseBroken
            };

            var weightsSO = ResolveIntentWeights(phase, behavior);

            float attackScore, punishScore, counterScore, pressureScore, chaseScore;
            float retreatScore, keepDistanceScore, defendScore, recoverScore;

            if (weightsSO != null)
            {
                attackScore       = IntentScoreComputer.Compute(weightsSO.attack,       in ctx);
                punishScore       = IntentScoreComputer.Compute(weightsSO.punish,       in ctx);
                counterScore      = IntentScoreComputer.Compute(weightsSO.counter,      in ctx);
                pressureScore     = IntentScoreComputer.Compute(weightsSO.pressure,     in ctx);
                chaseScore        = IntentScoreComputer.Compute(weightsSO.chase,        in ctx);
                retreatScore      = IntentScoreComputer.Compute(weightsSO.retreat,      in ctx);
                keepDistanceScore = IntentScoreComputer.Compute(weightsSO.keepDistance, in ctx);
                defendScore       = IntentScoreComputer.Compute(weightsSO.defend,       in ctx);
                recoverScore      = IntentScoreComputer.Compute(weightsSO.recover,      in ctx);
            }
            else
            {
                var s = LegacyIntentScoring.Compute(in ctx);
                attackScore       = s.attack;
                punishScore       = s.punish;
                counterScore      = s.counter;
                pressureScore     = s.pressure;
                chaseScore        = s.chase;
                retreatScore      = s.retreat;
                keepDistanceScore = s.keepDistance;
                defendScore       = s.defend;
                recoverScore      = s.recover;
            }

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

            var groupBias = _context != null ? _context.CurrentGroupIntentBias : GroupIntentBias.Neutral;
            WriteGroupDebugBlackboard(blackboard, groupBias);

            ApplyGroupIntentBias(
                groupBias,
                ref attackScore,
                ref punishScore,
                ref counterScore,
                ref pressureScore,
                ref retreatScore,
                ref keepDistanceScore);

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

        private static EnemyIntentWeightsSO ResolveIntentWeights(BehaviorPhase phase, EnemyBehaviorSO behavior)
        {
            if (phase != null && phase.intentWeightsOverride != null)
                return phase.intentWeightsOverride;
            return behavior != null ? behavior.intentWeights : null;
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

        private static void ApplyGroupIntentBias(
            GroupIntentBias bias,
            ref float attack,
            ref float punish,
            ref float counter,
            ref float pressure,
            ref float retreat,
            ref float keepDistance)
        {
            attack *= Mathf.Max(0f, bias.AttackMultiplier);
            punish *= Mathf.Max(0f, bias.PunishMultiplier);
            counter *= Mathf.Max(0f, bias.CounterMultiplier);
            pressure += bias.PressureBonus;
            retreat += bias.RetreatBonus;
            keepDistance += bias.KeepDistanceBonus;
        }

        private static void WriteGroupDebugBlackboard(Blackboard blackboard, GroupIntentBias bias)
        {
            blackboard.SetFloat(EnemyBlackboardKeys.GroupIntentAttackMultiplier, bias.AttackMultiplier);
            blackboard.SetFloat(EnemyBlackboardKeys.GroupIntentPunishMultiplier, bias.PunishMultiplier);
            blackboard.SetFloat(EnemyBlackboardKeys.GroupIntentCounterMultiplier, bias.CounterMultiplier);
            blackboard.SetFloat(EnemyBlackboardKeys.GroupIntentPressureBonus, bias.PressureBonus);
            blackboard.SetFloat(EnemyBlackboardKeys.GroupIntentKeepDistanceBonus, bias.KeepDistanceBonus);
            blackboard.SetFloat(EnemyBlackboardKeys.GroupIntentRetreatBonus, bias.RetreatBonus);
            blackboard.SetFloat(EnemyBlackboardKeys.GroupBreatherRemainingTime, bias.BreatherRemainingTime);
            blackboard.SetInt(EnemyBlackboardKeys.GroupFormationSlotIndex, bias.FormationSlotIndex);
            blackboard.SetFloat(EnemyBlackboardKeys.GroupAggroFitness, bias.AggroFitness);
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

            var maxRepeatPenaltyCount = intent is CombatIntent.Pressure or CombatIntent.KeepDistance or CombatIntent.Chase
                ? 4
                : 3;
            var repeatBase = intent is CombatIntent.Pressure or CombatIntent.KeepDistance or CombatIntent.Chase
                ? 0.65f
                : 0.85f;
            var repeatCount = blackboard.TryGetInt(EnemyBlackboardKeys.DecisionConsecutiveIntentCount, out var count)
                ? Mathf.Clamp(count, 1, maxRepeatPenaltyCount)
                : 1;
            score *= Mathf.Pow(repeatBase, repeatCount);
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
