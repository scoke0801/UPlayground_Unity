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
            var preferredRange = ResolveBlackboardValue(data.blackboard.preferredRange, data.blackboard.optimalCombatDistance >= 0f ? data.blackboard.optimalCombatDistance : sourceBehavior?.optimalCombatDistance ?? 2.5f);

            EnemyBlackboardDefaultEntryRegistry.ApplyDefaults(blackboard);

            blackboard.SetFloat(EnemyBlackboardKeys.ContinueAttackChance, sourceBehavior?.continueAttackChance ?? 0.3f);
            blackboard.SetFloat(EnemyBlackboardKeys.GuardChance, sourceBehavior?.guardChance ?? 0.25f);
            blackboard.SetFloat(EnemyBlackboardKeys.RetreatChance, sourceBehavior?.retreatChance ?? 0.2f);
            blackboard.SetFloat(EnemyBlackboardKeys.AIAggression, Mathf.Clamp01(data.blackboard.aggression));
            blackboard.SetFloat(EnemyBlackboardKeys.AIReactionChance, Mathf.Clamp01(data.blackboard.reactionChance));
            blackboard.SetFloat(EnemyBlackboardKeys.AICounterChance, Mathf.Clamp01(data.blackboard.counterChance));
            blackboard.SetFloat(EnemyBlackboardKeys.AIDodgeChance, Mathf.Clamp01(data.blackboard.dodgeChance));
            blackboard.SetFloat(EnemyBlackboardKeys.AIPunishRecoveryChance, Mathf.Clamp01(data.blackboard.punishRecoveryChance));
            blackboard.SetFloat(EnemyBlackboardKeys.AIAntiGuardChance, Mathf.Clamp01(data.blackboard.antiGuardChance));
            blackboard.SetFloat(EnemyBlackboardKeys.AIMinRetreatCooldown, Mathf.Max(0f, data.blackboard.minRetreatCooldown));
            blackboard.SetInt(EnemyBlackboardKeys.AIMaxComboPressureCount, Mathf.Max(0, data.blackboard.maxComboPressureCount));
            blackboard.SetFloat(EnemyBlackboardKeys.AIPreferredRange, preferredRange);
            blackboard.SetFloat(EnemyBlackboardKeys.HitReactionLockTime, Mathf.Max(0f, data.blackboard.hitReactionLockTime));
            blackboard.SetFloat(EnemyBlackboardKeys.RevengeChance, Mathf.Clamp01(data.blackboard.revengeChance));
            blackboard.SetInt(EnemyBlackboardKeys.MemoryHitRecentCount, Mathf.Max(0, data.blackboard.recentHitCount));
            blackboard.SetString(EnemyBlackboardKeys.MemoryHitLastReactionType, data.blackboard.lastHitReactionType ?? "");
            blackboard.SetString(EnemyBlackboardKeys.PredictedNextPlayerAction, "None");
            blackboard.SetFloat(EnemyBlackboardKeys.PredictionConfidence, 0f);
            blackboard.SetString(EnemyBlackboardKeys.PlayerActionLastToken, "None");
            blackboard.SetFloat(EnemyBlackboardKeys.PlayerActionTimeSinceLast, 0f);
            blackboard.SetFloat(EnemyBlackboardKeys.SelfPoiseRatio, Mathf.Clamp01(data.blackboard.poiseRatio));
            blackboard.SetBool(EnemyBlackboardKeys.SelfIsPoiseBroken, data.blackboard.isPoiseBroken);
            blackboard.SetString(EnemyBlackboardKeys.EnemyAIRole, sourceBehavior?.aiRole.ToString() ?? "Melee");

            blackboard.SetBool(EnemyBlackboardKeys.EnablePatrol, data.blackboard.enablePatrol);
            blackboard.SetFloat(EnemyBlackboardKeys.OptimalCombatDistance, ResolveBlackboardValue(data.blackboard.optimalCombatDistance, sourceBehavior?.optimalCombatDistance ?? 2.5f));
            blackboard.SetFloat(EnemyBlackboardKeys.MinCombatDistance, ResolveBlackboardValue(data.blackboard.minCombatDistance, sourceBehavior?.minCombatDistance ?? 1.5f));
            blackboard.SetFloat(EnemyBlackboardKeys.PersonalSpaceDistance, ResolveBlackboardValue(data.blackboard.personalSpaceDistance, sourceBehavior?.personalSpaceDistance ?? 0.8f));
            blackboard.SetFloat(EnemyBlackboardKeys.GuardChance, ResolveBlackboardValue(data.blackboard.guardChance, sourceBehavior?.guardChance ?? 0.25f));
            blackboard.SetFloat(EnemyBlackboardKeys.RetreatChance, ResolveBlackboardValue(data.blackboard.retreatChance, sourceBehavior?.retreatChance ?? 0.2f));
            blackboard.SetFloat(EnemyBlackboardKeys.CircleWeight, Mathf.Max(0f, data.blackboard.circleWeight));
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
