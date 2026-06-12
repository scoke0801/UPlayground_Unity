#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Balance
{
    public static class BalanceDataAutoGenerator
    {
        private const string StatPath = "Assets/10.Datas/Stat/Generated";
        private const string AttackPath = "Assets/10.Datas/Actor/Enemy/AttackData/Generated";
        private const string BehaviorPath = "Assets/10.Datas/Actor/Enemy/BehaviorData/Generated";
        private const string BehaviorTreePath = "Assets/10.Datas/AI/BehaviorTree/Generated";
        private static MonsterScalingSO _cachedScaling;
        private static bool _didSearchScaling;
        private static bool _didWarnMultipleScaling;

        public static bool HasMissingData(ActorDefinitionSO actor)
        {
            if (actor == null)
                return false;

            bool isMonster = (actor.actorType & ActorType.Monster) != 0;
            if (actor.statData == null)
                return true;
            if (isMonster && actor.monsterScaling == null)
                return true;
            if (isMonster && actor.attackData == null)
                return true;
            if (isMonster && actor.behaviorData == null)
                return true;
            if (isMonster && actor.behaviorData != null && actor.behaviorData.behaviorTree == null)
                return true;

            return false;
        }

        public static GenerationSummary GenerateMissing(
            ActorDefinitionSO actor,
            BalanceScenarioAsset scenario = null,
            BalanceScenarioInput fallbackInput = default)
        {
            var summary = new GenerationSummary();
            if (actor == null)
                return summary;

            EnsureFolder(StatPath);
            EnsureFolder(AttackPath);
            EnsureFolder(BehaviorPath);
            EnsureFolder(BehaviorTreePath);

            Undo.RecordObject(actor, "Generate Missing Balance Data");
            var serializedActor = new SerializedObject(actor);
            ActorStatSO statForAttackGeneration = actor.statData;
            bool isMonster = (actor.actorType & ActorType.Monster) != 0;

            if (isMonster && (actor.statData == null || actor.monsterScaling == null))
            {
                MonsterStatBakeService.Result result = MonsterStatBakeService.Bake(actor, new MonsterStatBakeService.Options
                {
                    StatSavePath = StatPath,
                    CreateMissingStat = true,
                    ForceRegenerate = false,
                    LinkMissingScaling = true,
                    RecordUndo = true,
                    UndoLabel = "Generate Missing Balance Stat",
                });
                if (result.Stat != null)
                    statForAttackGeneration = result.Stat;
                if (result.CreatedStat)
                {
                    summary.StatDataPath = result.StatPath;
                    summary.CreatedCount++;
                }
            }
            else if (actor.statData == null)
            {
                ActorStatSO stat = CreateStatData(actor);
                string path = CreateAsset(stat, StatPath, $"ActorStat_{GetSafeId(actor)}");
                serializedActor.FindProperty("statData").objectReferenceValue = stat;
                statForAttackGeneration = stat;
                summary.StatDataPath = path;
                summary.CreatedCount++;
            }

            if (isMonster && actor.attackData == null)
            {
                EnemyAttackDataSO attackData = CreateEnemyAttackData(actor, statForAttackGeneration, summary, scenario, fallbackInput);
                string path = CreateAsset(attackData, AttackPath, $"EnemyAttackData_{GetSafeId(actor)}");
                serializedActor.FindProperty("attackData").objectReferenceValue = attackData;
                summary.AttackDataPath = path;
                summary.CreatedCount++;
            }

            if (isMonster && actor.behaviorData == null)
            {
                EnemyBehaviorSO behavior = CreateBehaviorData(actor);
                BehaviorTreeAsset tree = CreateBehaviorTree(actor);
                string treePath = CreateAsset(tree, BehaviorTreePath, $"BT_{GetSafeId(actor)}");
                behavior.behaviorTree = tree;
                string behaviorPath = CreateAsset(behavior, BehaviorPath, $"BehaviorData_{GetSafeId(actor)}");
                serializedActor.FindProperty("behaviorData").objectReferenceValue = behavior;
                summary.BehaviorTreePath = treePath;
                summary.BehaviorDataPath = behaviorPath;
                summary.CreatedCount += 2;
            }
            else if (isMonster && actor.behaviorData != null && actor.behaviorData.behaviorTree == null)
            {
                Undo.RecordObject(actor.behaviorData, "Generate Missing Behavior Tree");
                BehaviorTreeAsset tree = CreateBehaviorTree(actor);
                string treePath = CreateAsset(tree, BehaviorTreePath, $"BT_{GetSafeId(actor)}");
                actor.behaviorData.behaviorTree = tree;
                EditorUtility.SetDirty(actor.behaviorData);
                summary.BehaviorTreePath = treePath;
                summary.CreatedCount++;
            }

            serializedActor.ApplyModifiedProperties();
            EditorUtility.SetDirty(actor);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return summary;
        }

        private static ActorStatSO CreateStatData(ActorDefinitionSO actor)
        {
            if (actor != null && (actor.actorType & ActorType.Monster) != 0)
            {
                var monsterStat = ScriptableObject.CreateInstance<ActorStatSO>();
                MonsterStatBakeService.WriteMonsterStatValues(monsterStat, actor, new MonsterStatBakeService.Options
                {
                    PreferredScaling = actor.monsterScaling != null ? actor.monsterScaling : FindOrCreateScaling(),
                    StatSavePath = StatPath,
                });
                monsterStat.EditorFillMissing();
                return monsterStat;
            }

            var stat = ScriptableObject.CreateInstance<ActorStatSO>();
            MonsterScalingSO scaling = actor != null && actor.monsterScaling != null
                ? actor.monsterScaling
                : FindOrCreateScaling();

            if (scaling != null)
            {
                Dictionary<StatType, float> values = MonsterStatCalculator.Calculate(scaling, actor);
                foreach (KeyValuePair<StatType, float> pair in values)
                    stat.EditorSet(pair.Key, pair.Value);
            }
            else
            {
                ApplyStatTemplate(stat, actor != null ? actor.grade : MonsterActorGrade.Normal);
            }

            // 스케일링이 있으면 등급 배율이 반영된 계산값을 유지한다(statData 단일 소스).
            // PoiseSO로 덮으면 보스 강인도가 일반 몹 값으로 되돌아가므로 스케일링 없는 레거시 액터에만 적용.
            if (scaling == null && actor?.poiseData != null)
            {
                stat.EditorSet(StatType.MaxPoise, actor.poiseData.maxPoise);
                stat.EditorSet(StatType.PoiseRecoveryRate, actor.poiseData.recoveryRate);
                stat.EditorSet(StatType.PoiseRecoveryDelay, actor.poiseData.recoveryDelay);
            }

            stat.EditorFillMissing();
            return stat;
        }

        private static EnemyAttackDataSO CreateEnemyAttackData(
            ActorDefinitionSO actor,
            ActorStatSO statData,
            GenerationSummary summary,
            BalanceScenarioAsset scenario,
            BalanceScenarioInput fallbackInput)
        {
            var data = ScriptableObject.CreateInstance<EnemyAttackDataSO>();
            data.globalCooldown = actor != null && actor.grade == MonsterActorGrade.Boss ? 0.8f : 1f;
            GenerationCombatContext context = BuildGenerationContext(actor, statData, scenario, fallbackInput);

            if (TryFindMotionSet(actor, out ActorAnimationMotionSet motionSet, out string source) &&
                TryPopulateEnemyAttacksFromMotionSet(data, actor, statData, motionSet, context, out int generatedCount))
            {
                summary.MotionSetSource = context.UsesScenarioTarget
                    ? $"{source} / 플레이어 기준 피해 {context.BaseAttackDamage:F1}"
                    : source;
                summary.GeneratedAttackSkillCount = generatedCount;
                return data;
            }

            summary.MotionSetSource = string.IsNullOrEmpty(source) ? "MotionSet 없음 - 기본 공격 1개 생성" : $"{source} - 유효 공격 없음";
            data.skills.Add(new EnemyAttackInfo
            {
                baseInfo = new AttackInfoBase
                {
                    animKey = AnimKey.Attack_1,
                    attackType = AttackType.Melee,
                    hitPhases = new List<HitPhaseData>
                    {
                        new HitPhaseData { damage = CalculateStoredDamage(actor, statData, context.BaseAttackDamage), poiseDamage = 30f, breakDamage = 0f }
                    }
                },
                skillType = SkillType.Attack,
                attackCategory = EnemyAttackCategory.Basic,
                requiredLevel = 1,
                selectionWeight = 10f,
                minRange = 0f,
                maxRange = 2.5f,
                cooldown = 2f,
                defenseType = AttackDefenseType.Parryable
            });
            summary.GeneratedAttackSkillCount = 1;
            return data;
        }

        private static bool TryFindMotionSet(
            ActorDefinitionSO actor,
            out ActorAnimationMotionSet motionSet,
            out string source)
        {
            motionSet = null;
            source = "";

            if (actor == null)
                return false;

            if (actor.prefab != null)
            {
                var animator = actor.prefab.GetComponentInChildren<ActorAnimator>(true);
                if (animator != null && animator.MotionSet != null)
                {
                    motionSet = animator.MotionSet;
                    source = $"Prefab ActorAnimator: {AssetDatabase.GetAssetPath(actor.prefab)}";
                    return true;
                }
            }

            foreach (string token in GetMotionSearchTokens(actor))
            {
                string[] guids = AssetDatabase.FindAssets($"{token} t:ActorAnimationMotionSet");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var candidate = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(path);
                    if (candidate == null)
                        continue;

                    motionSet = candidate;
                    source = $"Asset search: {path}";
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> GetMotionSearchTokens(ActorDefinitionSO actor)
        {
            if (!string.IsNullOrWhiteSpace(actor.actorId))
                yield return actor.actorId;
            if (!string.IsNullOrWhiteSpace(actor.name))
                yield return actor.name.Replace("ActorDef_", "");
            if (!string.IsNullOrWhiteSpace(actor.displayName))
                yield return actor.displayName;
        }

        private static bool TryPopulateEnemyAttacksFromMotionSet(
            EnemyAttackDataSO data,
            ActorDefinitionSO actor,
            ActorStatSO statData,
            ActorAnimationMotionSet motionSet,
            GenerationCombatContext context,
            out int generatedCount)
        {
            generatedCount = 0;
            data.skills ??= new List<EnemyAttackInfo>();

            List<MotionScanEntry> entries = CollectMotionScanEntries(motionSet);
            context = ResolveMotionContext(actor, statData, data.globalCooldown, entries, context);
            foreach (MotionScanEntry entry in entries)
            {
                data.skills.Add(CreateEnemyAttackFromMotion(actor, statData, entry, context));
                generatedCount++;
            }

            return generatedCount > 0;
        }

        private static List<MotionScanEntry> CollectMotionScanEntries(ActorAnimationMotionSet root)
        {
            var result = new List<MotionScanEntry>();
            var seen = new HashSet<AnimKey>();

            foreach (ActorAnimationMotionSet set in EnumerateMotionSets(root))
            {
                if (set?.motionSets == null)
                    continue;

                foreach (KeyValuePair<AnimKey, MotionSetAsset> pair in set.motionSets)
                {
                    if (!seen.Add(pair.Key))
                        continue;
                    if (!TryGetEnemyAttackCategory(pair.Key, out AttackCategory category))
                        continue;
                    if (pair.Value == null || pair.Value.motionSet == null)
                        continue;

                    List<BeginCollisionEvent> collisions = CollectCollisionEvents(pair.Value.motionSet);
                    List<SpawnProjectileEvent> projectiles = CollectProjectileEvents(pair.Value.motionSet);
                    if (collisions.Count == 0 && projectiles.Count == 0)
                        continue;

                    result.Add(new MotionScanEntry(
                        pair.Key,
                        category,
                        CalculatePhaseCount(collisions, projectiles),
                        collisions.Count == 0 && projectiles.Count > 0,
                        pair.Value.motionSet.TotalDuration));
                }
            }

            return result.OrderBy(x => (int)x.Key).ToList();
        }

        private static IEnumerable<ActorAnimationMotionSet> EnumerateMotionSets(ActorAnimationMotionSet root)
        {
            var visited = new HashSet<ActorAnimationMotionSet>();
            ActorAnimationMotionSet current = root;
            int depth = 0;
            while (current != null && visited.Add(current) && depth++ < 8)
            {
                yield return current;
                current = current.fallbackMotionSet;
            }
        }

        private static List<BeginCollisionEvent> CollectCollisionEvents(MotionSet motionSet)
        {
            var result = new List<(float Time, BeginCollisionEvent Event)>();

            if (motionSet.globalEvents != null)
            {
                foreach (MotionEventBase evt in motionSet.globalEvents)
                {
                    if (evt is BeginCollisionEvent collision)
                        result.Add((collision.startTime, collision));
                }
            }

            float offset = 0f;
            if (motionSet.motions != null)
            {
                foreach (UPlayGround.Animation.Motion motion in motionSet.motions)
                {
                    if (motion?.events != null)
                    {
                        foreach (MotionEventBase evt in motion.events)
                        {
                            if (evt is BeginCollisionEvent collision)
                                result.Add((offset + collision.startTime, collision));
                        }
                    }

                    offset += motion?.Duration ?? 0f;
                }
            }

            return result.OrderBy(x => x.Time).Select(x => x.Event).ToList();
        }

        private static List<SpawnProjectileEvent> CollectProjectileEvents(MotionSet motionSet)
        {
            var result = new List<(float Time, SpawnProjectileEvent Event)>();

            if (motionSet.globalEvents != null)
            {
                foreach (MotionEventBase evt in motionSet.globalEvents)
                {
                    if (evt is SpawnProjectileEvent projectile)
                        result.Add((projectile.startTime, projectile));
                }
            }

            float offset = 0f;
            if (motionSet.motions != null)
            {
                foreach (UPlayGround.Animation.Motion motion in motionSet.motions)
                {
                    if (motion?.events != null)
                    {
                        foreach (MotionEventBase evt in motion.events)
                        {
                            if (evt is SpawnProjectileEvent projectile)
                                result.Add((offset + projectile.startTime, projectile));
                        }
                    }

                    offset += motion?.Duration ?? 0f;
                }
            }

            return result.OrderBy(x => x.Time).Select(x => x.Event).ToList();
        }

        private static int CalculatePhaseCount(List<BeginCollisionEvent> collisions, List<SpawnProjectileEvent> projectiles)
        {
            if (collisions == null || collisions.Count == 0)
                return Mathf.Max(1, projectiles?.Count ?? 0);

            int maxIndex = collisions.Max(x => Mathf.Max(0, x.hitPhaseIndex));
            return Mathf.Max(collisions.Count, maxIndex + 1);
        }

        private static EnemyAttackInfo CreateEnemyAttackFromMotion(
            ActorDefinitionSO actor,
            ActorStatSO statData,
            MotionScanEntry entry,
            GenerationCombatContext context)
        {
            return new EnemyAttackInfo
            {
                baseInfo = new AttackInfoBase
                {
                    animKey = entry.Key,
                    attackType = entry.IsProjectileOnly ? AttackType.Ranged : AttackType.Melee,
                    hitPhases = CreateHitPhases(actor, statData, entry, context)
                },
                skillType = SkillType.Attack,
                attackCategory = ToEnemyAttackCategory(entry.Category),
                requiredLevel = 1,
                selectionWeight = GetEnemySelectionWeight(actor, entry.Category),
                minRange = 0f,
                maxRange = entry.Category == AttackCategory.Dash ? 4f : 2.5f,
                cooldown = GetEnemyCooldown(entry.Category),
                isAerialSkill = entry.Key == AnimKey.Fly_Attack,
                useDangerRing = IsStrongEnemyAttack(entry.Category),
                dangerRingDuration = 0f,
                defenseType = AttackDefenseType.Parryable
            };
        }

        private static List<HitPhaseData> CreateHitPhases(
            ActorDefinitionSO actor,
            ActorStatSO statData,
            MotionScanEntry entry,
            GenerationCombatContext context)
        {
            int phaseCount = Mathf.Max(1, entry.PhaseCount);
            float totalDamage = CalculateMotionDamage(actor, statData, entry, context);
            float totalWeight = 0f;
            var weights = new float[phaseCount];

            for (int i = 0; i < phaseCount; i++)
            {
                weights[i] = Mathf.Lerp(1f, 1.25f, phaseCount <= 1 ? 0f : (float)i / (phaseCount - 1));
                totalWeight += weights[i];
            }

            var phases = new List<HitPhaseData>(phaseCount);
            for (int i = 0; i < phaseCount; i++)
            {
                float damage = totalWeight > 0f ? totalDamage * weights[i] / totalWeight : totalDamage;
                phases.Add(new HitPhaseData
                {
                    damage = Mathf.Round(damage),
                    poiseDamage = Mathf.Round(damage * 3f),
                    breakDamage = 0f
                });
            }

            return phases;
        }

        private static float CalculateMotionDamage(
            ActorDefinitionSO actor,
            ActorStatSO statData,
            MotionScanEntry entry,
            GenerationCombatContext context)
        {
            float baseDamage = context.BaseAttackDamage > 0f ? context.BaseAttackDamage : GetDefaultAttackDamage(actor);
            float motionUnit = CalculateMotionDamageUnit(entry);

            return Mathf.Max(1f, baseDamage * motionUnit);
        }

        private static float CalculateMotionDamageUnit(MotionScanEntry entry)
        {
            float categoryMultiplier = entry.Category switch
            {
                AttackCategory.Heavy => 1.55f,
                AttackCategory.Dash => 1.25f,
                AttackCategory.Jump => 1.20f,
                AttackCategory.Skill => 2.10f,
                AttackCategory.Counter => 1.75f,
                _ => 1.00f,
            };
            float comboMultiplier = 1f + GetComboStep(entry.Key, entry.Category) * 0.08f;
            float durationMultiplier = 1f + Mathf.Max(0f, entry.Duration - 1f) * 0.15f;
            float multiHitCompensation = 1f + Mathf.Max(0, entry.PhaseCount - 1) * 0.18f;
            return categoryMultiplier * comboMultiplier * durationMultiplier * multiHitCompensation;
        }

        private static GenerationCombatContext BuildGenerationContext(
            ActorDefinitionSO actor,
            ActorStatSO statData,
            BalanceScenarioAsset scenario,
            BalanceScenarioInput fallbackInput)
        {
            float targetDuration = scenario != null
                ? Mathf.Max(1f, scenario.targetDuration)
                : Mathf.Max(1f, fallbackInput.TargetDuration > 0f ? fallbackInput.TargetDuration : 30f);

            float playerHealth = scenario?.playerStatData != null
                ? Mathf.Max(1f, scenario.playerStatData.GetBase(StatType.MaxHealth))
                : Mathf.Max(1f, ActorStatSO.GetDefault(StatType.MaxHealth));

            float playerDefense = scenario?.playerStatData != null
                ? Mathf.Clamp01(scenario.playerStatData.GetBase(StatType.Defense))
                : Mathf.Clamp01(ActorStatSO.GetDefault(StatType.Defense));

            return new GenerationCombatContext
            {
                BaseAttackDamage = GetDefaultAttackDamage(actor),
                TargetEnemyDps = playerHealth / targetDuration,
                PlayerDefense = playerDefense,
                HitReceiveRate = scenario != null ? scenario.hitReceiveRate : 0.45f,
                DodgeSuccessRate = scenario != null ? scenario.dodgeSuccessRate : 0.15f,
                ParrySuccessRate = scenario != null ? scenario.parrySuccessRate : 0.05f,
                UsesScenarioTarget = scenario != null || fallbackInput.TargetDuration > 0f,
            };
        }

        private static GenerationCombatContext ResolveMotionContext(
            ActorDefinitionSO actor,
            ActorStatSO statData,
            float globalCooldown,
            List<MotionScanEntry> entries,
            GenerationCombatContext context)
        {
            if (!context.UsesScenarioTarget || entries == null || entries.Count == 0 || context.TargetEnemyDps <= 0f)
                return context;

            float totalWeight = 0f;
            for (int i = 0; i < entries.Count; i++)
                totalWeight += Mathf.Max(0f, GetEnemySelectionWeight(actor, entries[i].Category));

            if (totalWeight <= 0f)
                return context;

            float attackPower = statData != null
                ? Mathf.Max(0.01f, statData.GetBase(StatType.AttackPower))
                : ActorStatSO.GetDefault(StatType.AttackPower);
            float defenseMultiplier = 1f - Mathf.Clamp01(context.PlayerDefense);
            float avoidMultiplier = Mathf.Clamp01(context.HitReceiveRate) * (1f - Mathf.Clamp01(context.DodgeSuccessRate));
            float parryMultiplier = 1f - Mathf.Clamp01(context.ParrySuccessRate);
            float targetFactor = 0f;

            for (int i = 0; i < entries.Count; i++)
            {
                MotionScanEntry entry = entries[i];
                float chance = Mathf.Max(0f, GetEnemySelectionWeight(actor, entry.Category)) / totalWeight;
                float cooldown = Mathf.Max(0.05f, Mathf.Max(GetEnemyCooldown(entry.Category), globalCooldown));
                targetFactor += chance * CalculateMotionDamageUnit(entry) * attackPower * defenseMultiplier * avoidMultiplier * parryMultiplier / cooldown;
            }

            if (targetFactor <= 0f)
                return context;

            context.BaseAttackDamage = Mathf.Max(1f, context.TargetEnemyDps / targetFactor);
            return context;
        }

        private static float CalculateStoredDamage(ActorDefinitionSO actor, ActorStatSO statData, float targetDamage)
        {
            return Mathf.Round(Mathf.Max(1f, targetDamage));
        }

        private static int GetComboStep(AnimKey key, AttackCategory category)
        {
            int value = (int)key;
            return category switch
            {
                AttackCategory.Light => Mathf.Max(0, value - (int)AnimKey.Attack_1),
                AttackCategory.Heavy => Mathf.Max(0, value - (int)AnimKey.HeavyAttack_1),
                AttackCategory.Dash => key == AnimKey.JumpDashAttack_1 ? 1 : Mathf.Max(0, value - (int)AnimKey.DashAttack_1),
                AttackCategory.Jump => Mathf.Max(0, value - (int)AnimKey.JumpAttack_1),
                AttackCategory.Skill => Mathf.Max(0, value - (int)AnimKey.Skill_1),
                AttackCategory.Counter => Mathf.Max(0, value - (int)AnimKey.Counter_Attack_1),
                _ => 0,
            };
        }

        private static EnemyAttackCategory ToEnemyAttackCategory(AttackCategory category)
        {
            return category switch
            {
                AttackCategory.Heavy => EnemyAttackCategory.Heavy,
                AttackCategory.Skill or AttackCategory.Counter => EnemyAttackCategory.Skill,
                _ => EnemyAttackCategory.Basic,
            };
        }

        private static float GetEnemySelectionWeight(ActorDefinitionSO actor, AttackCategory category)
        {
            MonsterActorGrade grade = actor != null ? actor.grade : MonsterActorGrade.Normal;

            if (category == AttackCategory.Heavy)
            {
                return grade switch
                {
                    MonsterActorGrade.Boss => 7f,
                    MonsterActorGrade.Elite => 5f,
                    MonsterActorGrade.Normal => 3f,
                    _ => 3f,
                };
            }

            if (category is AttackCategory.Skill or AttackCategory.Counter)
            {
                return grade switch
                {
                    MonsterActorGrade.Boss => 7f,
                    MonsterActorGrade.Elite => 4f,
                    MonsterActorGrade.Normal => 1f,
                    _ => 1f,
                };
            }

            return category switch
            {
                _ => 10f,
            };
        }

        private static float GetEnemyCooldown(AttackCategory category)
        {
            return category switch
            {
                AttackCategory.Heavy or AttackCategory.Counter => 3f,
                AttackCategory.Skill => 4f,
                _ => 2f,
            };
        }

        private static bool IsStrongEnemyAttack(AttackCategory category)
            => category is AttackCategory.Heavy or AttackCategory.Skill or AttackCategory.Counter;

        private static bool TryGetEnemyAttackCategory(AnimKey key, out AttackCategory category)
        {
            int value = (int)key;
            category = AttackCategory.Unknown;

            if (key == AnimKey.Fly_Attack)
                category = AttackCategory.Skill;
            else if (value >= (int)AnimKey.Attack_1 && value <= (int)AnimKey.Attack_10)
                category = AttackCategory.Light;
            else if (value >= (int)AnimKey.HeavyAttack_1 && value <= (int)AnimKey.HeavyAttack_10)
                category = AttackCategory.Heavy;
            else if (value >= (int)AnimKey.DashAttack_1 && value <= (int)AnimKey.DashAttack_5)
                category = AttackCategory.Dash;
            else if (key == AnimKey.JumpDashAttack_1)
                category = AttackCategory.Dash;
            else if (value >= (int)AnimKey.JumpAttack_1 && value <= (int)AnimKey.JumpAttack_7)
                category = AttackCategory.Jump;
            else if (value >= (int)AnimKey.Skill_1 && value <= (int)AnimKey.Skill_9)
                category = AttackCategory.Skill;
            else if (value >= (int)AnimKey.Counter_Attack_1 && value <= (int)AnimKey.Counter_Attack_2)
                category = AttackCategory.Counter;

            return category != AttackCategory.Unknown;
        }

        private enum AttackCategory
        {
            Unknown,
            Light,
            Heavy,
            Dash,
            Jump,
            Skill,
            Counter,
        }

        private readonly struct MotionScanEntry
        {
            public readonly AnimKey Key;
            public readonly AttackCategory Category;
            public readonly int PhaseCount;
            public readonly bool IsProjectileOnly;
            public readonly float Duration;

            public MotionScanEntry(AnimKey key, AttackCategory category, int phaseCount, bool isProjectileOnly, float duration)
            {
                Key = key;
                Category = category;
                PhaseCount = Mathf.Max(1, phaseCount);
                IsProjectileOnly = isProjectileOnly;
                Duration = Mathf.Max(0f, duration);
            }
        }

        private struct GenerationCombatContext
        {
            public float BaseAttackDamage;
            public float TargetEnemyDps;
            public float PlayerDefense;
            public float HitReceiveRate;
            public float DodgeSuccessRate;
            public float ParrySuccessRate;
            public bool UsesScenarioTarget;
        }

        private static EnemyBehaviorSO CreateBehaviorData(ActorDefinitionSO actor)
        {
            var behavior = ScriptableObject.CreateInstance<EnemyBehaviorSO>();
            behavior.optimalCombatDistance = 2.5f;
            behavior.minCombatDistance = 1.5f;
            behavior.chaseStopDistance = 2.0f;
            behavior.personalSpaceDistance = 0.8f;
            behavior.continueAttackChance = actor != null && actor.grade == MonsterActorGrade.Boss ? 0.45f : 0.3f;
            behavior.guardChance = 0.2f;
            behavior.retreatChance = 0.2f;
            behavior.chaseSpeedMultiplier = 1.2f;
            behavior.circleDuration = 2.5f;
            behavior.guardDuration = 1.5f;
            behavior.retreatDistance = 3.0f;
            behavior.enablePatrol = true;
            behavior.patrolRadius = 5f;
            behavior.patrolWaitTime = 2f;
            return behavior;
        }

        private static BehaviorTreeAsset CreateBehaviorTree(ActorDefinitionSO actor)
        {
            var tree = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            tree.name = $"BT_{GetSafeId(actor)}";
            return tree;
        }

        private static void ApplyStatTemplate(ActorStatSO stat, MonsterActorGrade grade)
        {
            switch (grade)
            {
                case MonsterActorGrade.Weak:
                    SetStats(stat, 216f, 0.82f, 0.01f, 55f, 30f, 1.7f, 1f);
                    break;
                case MonsterActorGrade.Elite:
                    SetStats(stat, 1100f, 1.3f, 0.1f, 220f, 25f, 2.5f, 1.1f);
                    break;
                case MonsterActorGrade.Boss:
                    SetStats(stat, 4500f, 1.5f, 0.2f, 700f, 20f, 3f, 1f);
                    break;
                default:
                    SetStats(stat, 540f, 1f, 0f, 100f, 30f, 2f, 1f);
                    break;
            }
        }

        private static void SetStats(
            ActorStatSO stat,
            float maxHealth,
            float attackPower,
            float defense,
            float maxPoise,
            float poiseRecoveryRate,
            float poiseRecoveryDelay,
            float moveSpeed)
        {
            stat.EditorSet(StatType.MaxHealth, maxHealth);
            stat.EditorSet(StatType.AttackPower, attackPower);
            stat.EditorSet(StatType.Defense, defense);
            stat.EditorSet(StatType.MaxPoise, maxPoise);
            stat.EditorSet(StatType.PoiseRecoveryRate, poiseRecoveryRate);
            stat.EditorSet(StatType.PoiseRecoveryDelay, poiseRecoveryDelay);
            stat.EditorSet(StatType.MoveSpeed, moveSpeed);
        }

        private static float GetDefaultAttackDamage(ActorDefinitionSO actor)
        {
            // MonsterScalingSO 커브가 있으면 스탯과 동일한 소스에서 등급별 base 피해를 가져온다.
            // (레벨 보정과 AttackPower 역보정은 CalculateMotionDamage/CalculateStoredDamage가 유지한다.)
            MonsterScalingSO scaling = actor != null && actor.monsterScaling != null
                ? actor.monsterScaling
                : FindOrCreateScaling();
            if (scaling != null)
                return scaling.GetBaseAttackDamage(actor != null ? actor.grade : MonsterActorGrade.Normal);

            return actor != null && actor.grade == MonsterActorGrade.Boss ? 18f
                : actor != null && actor.grade == MonsterActorGrade.Elite ? 12f
                : 8f;
        }

        private static MonsterScalingSO FindFirstScaling()
        {
            if (_didSearchScaling)
                return _cachedScaling;

            _didSearchScaling = true;
            string[] paths = AssetDatabase.FindAssets("t:MonsterScalingSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .OrderBy(path => path)
                .ToArray();

            if (paths.Length > 1 && !_didWarnMultipleScaling)
            {
                _didWarnMultipleScaling = true;
                Debug.LogWarning(
                    $"MonsterScalingSO가 {paths.Length}개 발견되어 '{paths[0]}'을 사용합니다. " +
                    "공격 기본 피해 자동 생성 기준을 명확히 하려면 MonsterScalingSO를 하나만 유지하거나 생성 전 정리하세요.");
            }

            _cachedScaling = paths.Length > 0
                ? AssetDatabase.LoadAssetAtPath<MonsterScalingSO>(paths[0])
                : null;
            return _cachedScaling;
        }

        private static MonsterScalingSO FindOrCreateScaling()
        {
            MonsterScalingSO scaling = FindFirstScaling();
            if (scaling != null)
                return scaling;

            EnsureFolder(StatPath);
            scaling = ScriptableObject.CreateInstance<MonsterScalingSO>();
            scaling.FillDefaults();
            string path = CreateAsset(scaling, StatPath, "MonsterScaling_Default");
            _cachedScaling = scaling;
            _didSearchScaling = true;
            Debug.Log($"[BalanceDataAutoGenerator] 기본 MonsterScalingSO 생성: {path}");
            return scaling;
        }

        private static string CreateAsset(UnityEngine.Object asset, string folder, string rawName)
        {
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{SanitizeFileName(rawName)}.asset");
            AssetDatabase.CreateAsset(asset, assetPath);
            return assetPath;
        }

        private static string GetSafeId(ActorDefinitionSO actor)
        {
            if (actor == null)
                return "Unknown";

            string value = !string.IsNullOrWhiteSpace(actor.actorId) ? actor.actorId : actor.name;
            return SanitizeFileName(value);
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unnamed";

            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');

            return value.Replace('/', '_').Replace('\\', '_').Replace(' ', '_');
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public struct GenerationSummary
        {
            public int CreatedCount;
            public string StatDataPath;
            public string AttackDataPath;
            public string BehaviorDataPath;
            public string BehaviorTreePath;
            public string MotionSetSource;
            public int GeneratedAttackSkillCount;

            public bool CreatedAny => CreatedCount > 0;
        }
    }
}
#endif
