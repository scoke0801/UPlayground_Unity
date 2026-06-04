#if UNITY_EDITOR
using System.IO;
using UPlayGround.Data.Enemy;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static class EnemyBehaviorJsonExporter
    {
        [MenuItem("UPlayGround/비헤이비어 트리/JSON/선택 BehaviorSO에서 내보내기", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.BehaviorTreeJson + 3)]
        public static void ExportFromSelectedBehaviorSO()
        {
            if (Selection.activeObject is not EnemyBehaviorSO behavior)
            {
                EditorUtility.DisplayDialog("Monster Behavior Json Export", "EnemyBehaviorSO를 선택하세요.", "확인");
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(behavior);
            var fileName = "EnemyBehavior_" + behavior.name.Replace("BehaviorData_", "") + ".json";
            var savePath = EditorUtility.SaveFilePanel(
                "Monster Behavior Json Export",
                Application.dataPath,
                fileName,
                "json");

            if (string.IsNullOrWhiteSpace(savePath))
                return;

            var data = CreateDefaultJson(behavior, assetPath);
            File.WriteAllText(savePath, JsonUtility.ToJson(data, true));
            AssetDatabase.Refresh();
            Debug.Log($"[BT] Monster Behavior Json Export 완료: {savePath}");
        }

        private static MonsterBehaviorTreeJson CreateDefaultJson(EnemyBehaviorSO behavior, string sourcePath)
        {
            var id = "EnemyBehavior_" + behavior.name.Replace("BehaviorData_", "");
            return new MonsterBehaviorTreeJson
            {
                schemaVersion = 1,
                id = id,
                displayName = behavior.name,
                actorKind = MonsterBehaviorJsonNodeKeys.ActorKinds.Ground,
                sourceBehaviorSo = sourcePath,
                blackboard = new MonsterBehaviorBlackboardJson
                {
                    enablePatrol = behavior.enablePatrol,
                    optimalCombatDistance = behavior.optimalCombatDistance,
                    minCombatDistance = behavior.minCombatDistance,
                    personalSpaceDistance = behavior.personalSpaceDistance,
                    guardChance = behavior.guardChance,
                    retreatChance = behavior.retreatChance,
                    circleWeight = 1f
                },
                rules =
                {
                    new MonsterBehaviorRuleJson
                    {
                        name = "BlockedState",
                        priority = 1000,
                        when = { new MonsterBehaviorConditionJson { condition = MonsterBehaviorJsonNodeKeys.Conditions.IsBlockedEnemyState } },
                        @do = { new MonsterBehaviorActionJson { action = MonsterBehaviorJsonNodeKeys.Actions.KeepCurrentState } }
                    },
                    new MonsterBehaviorRuleJson
                    {
                        name = "NoTarget",
                        priority = 900,
                        when = { new MonsterBehaviorConditionJson { condition = MonsterBehaviorJsonNodeKeys.Conditions.HasTarget, invert = true } },
                        @do = { new MonsterBehaviorActionJson { action = MonsterBehaviorJsonNodeKeys.Actions.PatrolOrIdle } }
                    },
                    new MonsterBehaviorRuleJson
                    {
                        name = "TooClose",
                        priority = 800,
                        when =
                        {
                            new MonsterBehaviorConditionJson { condition = MonsterBehaviorJsonNodeKeys.Conditions.HasTarget },
                            new MonsterBehaviorConditionJson { condition = MonsterBehaviorJsonNodeKeys.Conditions.DistanceLessOrEqual, value = "personalSpaceDistance" }
                        },
                        @do = { new MonsterBehaviorActionJson { action = MonsterBehaviorJsonNodeKeys.Actions.Transition, state = nameof(EnemyTransitionStateType.Retreat) } }
                    },
                    new MonsterBehaviorRuleJson
                    {
                        name = "Attack",
                        priority = 700,
                        when =
                        {
                            new MonsterBehaviorConditionJson { condition = MonsterBehaviorJsonNodeKeys.Conditions.HasTarget },
                            new MonsterBehaviorConditionJson { condition = MonsterBehaviorJsonNodeKeys.Conditions.ActionDelayElapsed },
                            new MonsterBehaviorConditionJson { condition = MonsterBehaviorJsonNodeKeys.Conditions.CanUseSkill }
                        },
                        @do =
                        {
                            new MonsterBehaviorActionJson { action = MonsterBehaviorJsonNodeKeys.Actions.RequestAttackSlot },
                            new MonsterBehaviorActionJson { action = MonsterBehaviorJsonNodeKeys.Actions.ExecuteAttack }
                        }
                    },
                    new MonsterBehaviorRuleJson
                    {
                        name = "OutOfRange",
                        priority = 500,
                        when =
                        {
                            new MonsterBehaviorConditionJson { condition = MonsterBehaviorJsonNodeKeys.Conditions.HasTarget },
                            new MonsterBehaviorConditionJson { condition = MonsterBehaviorJsonNodeKeys.Conditions.DistanceGreater, value = "optimalCombatDistance" }
                        },
                        @do = { new MonsterBehaviorActionJson { action = MonsterBehaviorJsonNodeKeys.Actions.Transition, state = nameof(EnemyTransitionStateType.Chase) } }
                    },
                    new MonsterBehaviorRuleJson
                    {
                        name = "CombatIdle",
                        priority = 100,
                        select = MonsterBehaviorJsonNodeKeys.SelectKinds.WeightedRandom,
                        choices =
                        {
                            new MonsterBehaviorChoiceJson { weightKey = "guardChance", action = MonsterBehaviorJsonNodeKeys.Actions.Transition, state = nameof(EnemyTransitionStateType.Guard) },
                            new MonsterBehaviorChoiceJson { weightKey = "retreatChance", action = MonsterBehaviorJsonNodeKeys.Actions.Transition, state = nameof(EnemyTransitionStateType.Retreat) },
                            new MonsterBehaviorChoiceJson { weightKey = "circleWeight", action = MonsterBehaviorJsonNodeKeys.Actions.Transition, state = nameof(EnemyTransitionStateType.Circle) }
                        }
                    }
                }
            };
        }
    }
}
#endif
