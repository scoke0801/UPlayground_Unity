#if UNITY_EDITOR
using System.IO;
using UPlayGround.Data.Enemy;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public static class EnemyBehaviorJsonExporter
    {
        [MenuItem("UPlayGround/Character/AI/Monster Behavior Json/Export From Selected BehaviorSO")]
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
                actorKind = "Ground",
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
                        when = { new MonsterBehaviorConditionJson { condition = "IsBlockedEnemyState" } },
                        @do = { new MonsterBehaviorActionJson { action = "KeepCurrentState" } }
                    },
                    new MonsterBehaviorRuleJson
                    {
                        name = "NoTarget",
                        priority = 900,
                        when = { new MonsterBehaviorConditionJson { condition = "HasTarget", invert = true } },
                        @do = { new MonsterBehaviorActionJson { action = "PatrolOrIdle" } }
                    },
                    new MonsterBehaviorRuleJson
                    {
                        name = "TooClose",
                        priority = 800,
                        when =
                        {
                            new MonsterBehaviorConditionJson { condition = "HasTarget" },
                            new MonsterBehaviorConditionJson { condition = "DistanceLessOrEqual", value = "personalSpaceDistance" }
                        },
                        @do = { new MonsterBehaviorActionJson { action = "Transition", state = "Retreat" } }
                    },
                    new MonsterBehaviorRuleJson
                    {
                        name = "Attack",
                        priority = 700,
                        when =
                        {
                            new MonsterBehaviorConditionJson { condition = "HasTarget" },
                            new MonsterBehaviorConditionJson { condition = "ActionDelayElapsed" },
                            new MonsterBehaviorConditionJson { condition = "CanUseSkill" }
                        },
                        @do =
                        {
                            new MonsterBehaviorActionJson { action = "RequestAttackSlot" },
                            new MonsterBehaviorActionJson { action = "ExecuteAttack" }
                        }
                    },
                    new MonsterBehaviorRuleJson
                    {
                        name = "OutOfRange",
                        priority = 500,
                        when =
                        {
                            new MonsterBehaviorConditionJson { condition = "HasTarget" },
                            new MonsterBehaviorConditionJson { condition = "DistanceGreater", value = "optimalCombatDistance" }
                        },
                        @do = { new MonsterBehaviorActionJson { action = "Transition", state = "Chase" } }
                    },
                    new MonsterBehaviorRuleJson
                    {
                        name = "CombatIdle",
                        priority = 100,
                        select = "WeightedRandom",
                        choices =
                        {
                            new MonsterBehaviorChoiceJson { weightKey = "guardChance", action = "Transition", state = "Guard" },
                            new MonsterBehaviorChoiceJson { weightKey = "retreatChance", action = "Transition", state = "Retreat" },
                            new MonsterBehaviorChoiceJson { weightKey = "circleWeight", action = "Transition", state = "Circle" }
                        }
                    }
                }
            };
        }
    }
}
#endif
