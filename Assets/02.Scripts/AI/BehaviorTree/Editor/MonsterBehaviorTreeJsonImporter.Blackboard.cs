#if UNITY_EDITOR
using System.Globalization;
using System.Reflection;
using UPlayGround.Data.Enemy;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static partial class MonsterBehaviorTreeJsonImporter
    {
        private static void AddDefaultBlackboard(BehaviorTreeAsset tree, MonsterBehaviorTreeJson data, EnemyBehaviorSO sourceBehavior)
        {
            var blackboard = tree.Blackboard;
            blackboard.SetBool(EnemyBlackboardKeys.HasTarget, false);
            blackboard.SetObject(EnemyBlackboardKeys.Target, null);
            blackboard.SetFloat(EnemyBlackboardKeys.DistanceToTarget, float.MaxValue);
            blackboard.SetString(EnemyBlackboardKeys.CurrentState, "");
            blackboard.SetFloat(EnemyBlackboardKeys.HpPercent, 1f);
            blackboard.SetString(EnemyBlackboardKeys.CurrentPhaseName, "");
            blackboard.SetInt(EnemyBlackboardKeys.PhaseIndex, -1);
            blackboard.SetBool(EnemyBlackboardKeys.AllowCharge, false);
            blackboard.SetBool(EnemyBlackboardKeys.AllowFlank, false);
            blackboard.SetInt(EnemyBlackboardKeys.MaxConsecutiveAttacks, 3);
            blackboard.SetFloat(EnemyBlackboardKeys.ContinueAttackChance, sourceBehavior?.continueAttackChance ?? 0.3f);
            blackboard.SetFloat(EnemyBlackboardKeys.GuardChance, sourceBehavior?.guardChance ?? 0.25f);
            blackboard.SetFloat(EnemyBlackboardKeys.RetreatChance, sourceBehavior?.retreatChance ?? 0.2f);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerAttacking, false);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerGuarding, false);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerStaggered, false);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerRecovering, false);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerDodgingFrequently, false);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerAttackingFrequently, false);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerGuardingFrequently, false);
            blackboard.SetBool(EnemyBlackboardKeys.IsPlayerRecoveringFrequently, false);
            blackboard.SetInt(EnemyBlackboardKeys.PlayerDodgeCount, 0);
            blackboard.SetInt(EnemyBlackboardKeys.PlayerGuardCount, 0);
            blackboard.SetInt(EnemyBlackboardKeys.PlayerAttackCount, 0);
            blackboard.SetInt(EnemyBlackboardKeys.PlayerRecoverCount, 0);
            blackboard.SetBool(EnemyBlackboardKeys.CanUseSkill, false);
            blackboard.SetBool(EnemyBlackboardKeys.HasAttackSlot, false);
            blackboard.SetFloat(EnemyBlackboardKeys.NextActionAllowedTime, 0f);
            blackboard.SetFloat(EnemyBlackboardKeys.Aggression, Mathf.Clamp01(data.blackboard.aggression));
            blackboard.SetFloat(EnemyBlackboardKeys.ReactionChance, Mathf.Clamp01(data.blackboard.reactionChance));
            blackboard.SetFloat(EnemyBlackboardKeys.CounterChance, Mathf.Clamp01(data.blackboard.counterChance));
            blackboard.SetFloat(EnemyBlackboardKeys.DodgeChance, Mathf.Clamp01(data.blackboard.dodgeChance));
            blackboard.SetFloat(EnemyBlackboardKeys.PunishRecoveryChance, Mathf.Clamp01(data.blackboard.punishRecoveryChance));
            blackboard.SetFloat(EnemyBlackboardKeys.AntiGuardChance, Mathf.Clamp01(data.blackboard.antiGuardChance));
            blackboard.SetFloat(EnemyBlackboardKeys.MinRetreatCooldown, Mathf.Max(0f, data.blackboard.minRetreatCooldown));
            blackboard.SetInt(EnemyBlackboardKeys.MaxComboPressureCount, Mathf.Max(0, data.blackboard.maxComboPressureCount));
            blackboard.SetFloat(EnemyBlackboardKeys.PreferredRange, ResolveBlackboardValue(data.blackboard.preferredRange, data.blackboard.optimalCombatDistance >= 0f ? data.blackboard.optimalCombatDistance : sourceBehavior?.optimalCombatDistance ?? 2.5f));
            blackboard.SetBool(EnemyBlackboardKeys.RecentlyHitByPlayer, false);
            blackboard.SetInt(EnemyBlackboardKeys.RecentHitCount, Mathf.Max(0, data.blackboard.recentHitCount));
            blackboard.SetString(EnemyBlackboardKeys.LastHitReactionType, data.blackboard.lastHitReactionType ?? "");
            blackboard.SetFloat(EnemyBlackboardKeys.PoiseRatio, Mathf.Clamp01(data.blackboard.poiseRatio));
            blackboard.SetBool(EnemyBlackboardKeys.IsPoiseBroken, data.blackboard.isPoiseBroken);
            blackboard.SetFloat(EnemyBlackboardKeys.HitReactionLockTime, Mathf.Max(0f, data.blackboard.hitReactionLockTime));
            blackboard.SetFloat(EnemyBlackboardKeys.RevengeChance, Mathf.Clamp01(data.blackboard.revengeChance));
            blackboard.SetString(EnemyBlackboardKeys.SelectedIntent, "");
            blackboard.SetString(EnemyBlackboardKeys.LastIntent, "");
            blackboard.SetInt(EnemyBlackboardKeys.ConsecutiveIntentCount, 0);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreAttack, 0f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentScorePunish, 0f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreCounter, 0f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentScorePressure, 0f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreChase, 0f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreRetreat, 0f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreKeepDistance, 0f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreDefend, 0f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentScoreRecover, 0f);
            blackboard.SetString(EnemyBlackboardKeys.CombatRhythmPhase, "");
            blackboard.SetString(EnemyBlackboardKeys.EnemyAIRole, sourceBehavior?.aiRole.ToString() ?? "Melee");
            blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightAttack, 1f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightPunish, 1f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightCounter, 1f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightPressure, 1f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightChase, 1f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightRetreat, 1f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightKeepDistance, 1f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightDefend, 1f);
            blackboard.SetFloat(EnemyBlackboardKeys.IntentWeightRecover, 1f);

            blackboard.SetBool("enablePatrol", data.blackboard.enablePatrol);
            blackboard.SetFloat("optimalCombatDistance", ResolveBlackboardValue(data.blackboard.optimalCombatDistance, sourceBehavior?.optimalCombatDistance ?? 2.5f));
            blackboard.SetFloat("minCombatDistance", ResolveBlackboardValue(data.blackboard.minCombatDistance, sourceBehavior?.minCombatDistance ?? 1.5f));
            blackboard.SetFloat("personalSpaceDistance", ResolveBlackboardValue(data.blackboard.personalSpaceDistance, sourceBehavior?.personalSpaceDistance ?? 0.8f));
            blackboard.SetFloat("guardChance", ResolveBlackboardValue(data.blackboard.guardChance, sourceBehavior?.guardChance ?? 0.25f));
            blackboard.SetFloat("retreatChance", ResolveBlackboardValue(data.blackboard.retreatChance, sourceBehavior?.retreatChance ?? 0.2f));
            blackboard.SetFloat("circleWeight", Mathf.Max(0f, data.blackboard.circleWeight));
        }

        private static float ResolveBlackboardValue(float value, float fallback)
        {
            return value >= 0f ? value : fallback;
        }

        private static float ResolveFloat(string keyOrValue, EnemyBehaviorSO sourceBehavior, float fallback)
        {
            if (string.IsNullOrWhiteSpace(keyOrValue))
                return fallback;

            if (float.TryParse(keyOrValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
                return numeric;

            if (sourceBehavior != null)
            {
                var field = typeof(EnemyBehaviorSO).GetField(keyOrValue, BindingFlags.Instance | BindingFlags.Public);
                if (field != null && field.FieldType == typeof(float))
                    return (float)field.GetValue(sourceBehavior);
            }

            return fallback;
        }

        private static float ResolveWeight(MonsterBehaviorChoiceJson choice, EnemyBehaviorSO sourceBehavior, MonsterBehaviorBlackboardJson blackboard)
        {
            return string.IsNullOrWhiteSpace(choice.weightKey)
                ? Mathf.Max(0f, choice.weight)
                : Mathf.Max(0f, ResolveFloat(choice.weightKey, sourceBehavior, blackboard, 1f));
        }

        private static float ResolveFloat(string keyOrValue, EnemyBehaviorSO sourceBehavior, MonsterBehaviorBlackboardJson blackboard, float fallback)
        {
            if (string.IsNullOrWhiteSpace(keyOrValue))
                return fallback;

            if (float.TryParse(keyOrValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var numeric))
                return numeric;

            if (blackboard != null)
            {
                var blackboardField = typeof(MonsterBehaviorBlackboardJson).GetField(keyOrValue, BindingFlags.Instance | BindingFlags.Public);
                if (blackboardField != null)
                {
                    if (blackboardField.FieldType == typeof(float))
                        return (float)blackboardField.GetValue(blackboard);
                    if (blackboardField.FieldType == typeof(int))
                        return (int)blackboardField.GetValue(blackboard);
                }
            }

            return ResolveFloat(keyOrValue, sourceBehavior, fallback);
        }
    }
}
#endif
