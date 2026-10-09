using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        [Serializable] private sealed class MonsterAudit
        {
            public string prefab;
            public string definition;
            public int attacks;
            public bool flying;
            public List<string> issues = new();
        }

        [Serializable] private sealed class WorldAudit
        {
            public List<MonsterAudit> monsters = new();
        }

        /// <summary>배치 후보의 실행 데이터를 검사하고 사용할 수 없는 이유를 기록한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 월드 몬스터 조사")]
        public static void InspectWorldMonsters()
        {
            RequireEditMode();
            var report = new WorldAudit();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/03.Prefabs/Actor/Monster" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                MonsterActor actor = prefab.GetComponent<MonsterActor>();
                var entry = new MonsterAudit { prefab = path };
                report.monsters.Add(entry);
                if (actor == null) { entry.issues.Add("MonsterActor 누락"); continue; }
                ActorDefinitionSO definition = actor.Definition;
                entry.definition = AssetDatabase.GetAssetPath(definition);
                var controller = prefab.GetComponent<EnemyAIController>();
                var flying = prefab.GetComponent<EnemyFlyingAIController>();
                entry.flying = flying != null;
                if (flying != null)
                {
                    if (!flying.enabled) entry.issues.Add("비행 AI 비활성화");
                    if (new SerializedObject(flying).FindProperty("_behaviorTree").objectReferenceValue == null)
                        entry.issues.Add("비행 BehaviorTree 누락");
                }
                else
                {
                    if (controller == null || !controller.enabled) entry.issues.Add("활성 AIController 누락");
                    var behavior = definition != null ? definition.EffectiveBehaviorData : controller?.BehaviorData;
                    if (behavior?.behaviorTree == null) entry.issues.Add("BehaviorTree 누락");
                }
                var animator = prefab.GetComponentInChildren<ActorAnimator>(true);
                if (animator == null || animator.MotionSet == null) entry.issues.Add("MotionSet 누락");
                var abilitySet = definition != null ? definition.EffectiveAbilitySet : prefab.GetComponent<EnemyCombat>()?.AbilitySet;
                if (abilitySet == null) { entry.issues.Add("AbilitySet 누락"); continue; }
                foreach (var ability in abilitySet.EnumerateAll())
                {
                    if (ability?.variants == null) continue;
                    foreach (var variant in ability.variants)
                    {
                        if (!UPlayGroundAbilityPayloadResolver.TryResolveAttackInfo(variant, out var attack) || !attack.aiSelectable) continue;
                        entry.attacks++;
                        if (!attack.motionKey.IsValid || animator?.MotionSet == null || animator.MotionSet.GetAbilityMotionAsset(attack.motionKey) == null)
                            entry.issues.Add(ability.name + ": Motion Key 매핑 누락");
                        if (attack.baseInfo?.hitPhases == null || attack.baseInfo.hitPhases.Count == 0)
                            entry.issues.Add(ability.name + ": HitPhase 누락");
                    }
                }
                if (entry.attacks == 0) entry.issues.Add("AI 공격 없음");
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                        entry.issues.Add("Missing Script: " + child.name);
            }
            Directory.CreateDirectory(ReportDirectory);
            File.WriteAllText(ReportDirectory + "/WorldMonsterAudit.json", JsonUtility.ToJson(report, true));
            Debug.Log("[ScenarioMap] 몬스터 조사: " + report.monsters.Count);
        }
    }
}
