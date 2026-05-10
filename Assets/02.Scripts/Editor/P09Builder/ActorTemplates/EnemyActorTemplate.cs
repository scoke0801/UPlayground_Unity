using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using KinematicCharacterController;
using UnityEditor;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data;
using UPlayGround.Component;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;
using UPlayGround.MovementController;

namespace Game.Editor.P09Builder
{
    /// <summary>
    /// Enemy(MonsterActor) 프리팹 빌드 템플릿.
    /// KCC + MotionWarp + EnemyMovementController + MonsterActor + 컴포넌트 일괄 부착 후
    /// Stats/Behavior/AttackData SO를 생성/연결한다.
    /// </summary>
    internal sealed class EnemyActorTemplate : IActorTemplate
    {
        public BuilderActorKind Kind => BuilderActorKind.Enemy;

        public void AttachComponents(GameObject root, CharacterBuildConfig config)
        {
            if (root == null)
            {
                Debug.LogWarning("[P09Builder] EnemyActorTemplate.AttachComponents: root is null");
                return;
            }

            LayerAssignmentUtil.ApplyActorLayer(root, "Enemy");

            // 물리 / 이동
            if (root.GetComponent<KinematicCharacterMotor>() == null)
                Undo.AddComponent<KinematicCharacterMotor>(root);

            if (root.GetComponent<EnemyMovementController>() == null)
                Undo.AddComponent<EnemyMovementController>(root);

            // Actor 본체 + 컴포넌트
            var actor   = GetOrAdd<MonsterActor>(root);
            var brain   = GetOrAdd<EnemyBrain>(root);
            var detect  = GetOrAdd<EnemyDetection>(root);
            var combat  = GetOrAdd<EnemyCombat>(root);
            var poise   = GetOrAdd<PoiseStat>(root);
            GetOrAdd<ActorColorChanger>(root);
            GetOrAdd<DissolveController>(root);

            if (root.GetComponent<Animator>() == null)
                Undo.AddComponent<Animator>(root);

            if (root.GetComponent<AnimancerComponent>() == null)
                Undo.AddComponent<AnimancerComponent>(root);

            if (root.GetComponent<CapsuleCollider>() == null)
            {
                var col = Undo.AddComponent<CapsuleCollider>(root);
                col.radius = 0.4f;
                col.height = 1.8f;
                col.center = Vector3.up * 0.9f;
            }

            // Actor에 컴포넌트 참조 주입 (private SerializeField)
            ReflectionUtil.SetField(actor, "_brain", brain);
            ReflectionUtil.SetField(actor, "_combat", combat);
            ReflectionUtil.SetField(actor, "_detection", detect);
            ReflectionUtil.SetField(actor, "_poiseStat", poise);

            if (config != null
                && config.Stats != null
                && config.Stats.recruitableOnDefeat
                && config.Stats.recruitableAs != CharacterActorType.None)
            {
                ReflectionUtil.SetField(actor, "_recruitableAs", (int)config.Stats.recruitableAs);
            }

            // ActorType 및 CharacterActorType 설정
            ReflectionUtil.SetField(actor, "_actorType", (int)(ActorType.Monster | ActorType.Combat));
            ReflectionUtil.SetField(actor, "_characterActorType", (int)CharacterActorType.None);
            ReflectionUtil.SetField(actor, "_actorId", root.name);
            ApplyMonsterSocketBindings(root, actor);

            ApplyDetectionDefaults(detect);
        }

        public IEnumerable<IDescDef> GetDescDefs(CharacterBuildConfig config)
        {
            // Stats: createNewStats=true일 때만 생성
            if (config != null && config.Stats != null && config.Stats.createNewStats)
                yield return new EnemyStatsDescDef();

            // 런타임 전투 스탯. MonsterActor는 EnemyStatsSO가 아니라 ActorDefinitionSO.statData를 사용한다.
            if (config != null && config.Stats != null)
                yield return new ActorStatDescDef();

            // Poise: createNewPoise=true일 때만 생성
            if (config != null && config.Stats != null && config.Stats.createNewPoise)
                yield return new PoiseDescDef();

            // Behavior: createNewBehavior=true일 때만 생성
            if (config != null && config.Stats != null && config.Stats.createNewBehavior)
                yield return new EnemyBehaviorDescDef();

            // AttackData: 기존 SO가 지정돼있으면 생성하지 않음
            if (config == null || config.Stats == null || config.Stats.attackDataSo == null)
                yield return new EnemyAttackDataDescDef();
        }

        public void WireDescAssets(GameObject root, List<ScriptableObject> generatedDescs, CharacterBuildConfig config)
        {
            if (root == null) return;

            var actor  = root.GetComponent<MonsterActor>();
            var brain  = root.GetComponent<EnemyBrain>();
            var combat = root.GetComponent<EnemyCombat>();

            var stats = FindFirst<EnemyStatsSO>(generatedDescs)
                        ?? (config?.Stats?.existingStatsSo as EnemyStatsSO);

            var behavior = FindFirst<EnemyBehaviorSO>(generatedDescs)
                           ?? (config?.Stats?.existingBehaviorSo as EnemyBehaviorSO);

            var attackData = FindFirst<EnemyAttackDataSO>(generatedDescs)
                             ?? (config?.Stats?.attackDataSo as EnemyAttackDataSO);

            var poiseData = FindFirst<PoiseSO>(generatedDescs)
                            ?? (config?.Stats?.existingPoiseSo as PoiseSO);

            if (actor != null && stats != null)
                ReflectionUtil.SetField(actor, "_stats", stats);

            var poise = root.GetComponent<PoiseStat>();
            if (poise != null && poiseData != null)
                ReflectionUtil.SetField(poise, "_data", poiseData);

            if (brain != null && behavior != null)
                ReflectionUtil.SetField(brain, "_behaviorData", behavior);

            if (combat != null && attackData != null)
                ReflectionUtil.SetField(combat, "_attackData", attackData);
        }

        // ---------- helpers ----------
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(go);
            return c;
        }

        private static T FindFirst<T>(List<ScriptableObject> list) where T : ScriptableObject
        {
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is T match) return match;
            }
            return null;
        }

        private static void ApplyDetectionDefaults(EnemyDetection detection)
        {
            if (detection == null) return;

            ReflectionUtil.SetField(detection, "_detectionRadius", 10f);
            ReflectionUtil.SetField(detection, "_lostTargetRadius", 15f);
            ReflectionUtil.SetField(detection, "_fieldOfView", 120f);
            ReflectionUtil.SetField(detection, "_targetLayer", LayerMask.GetMask("Player"));
            ReflectionUtil.SetField(detection, "_obstacleLayer", LayerMask.GetMask("Default", "Water", "InteractableObject"));
            ReflectionUtil.SetField(detection, "_allyDetectionRadius", 10f);
            ReflectionUtil.SetField(detection, "_allyLayer", LayerMask.GetMask("Enemy"));
            ReflectionUtil.SetField(detection, "_detectionInterval", 0.2f);
        }

        private static void ApplyMonsterSocketBindings(GameObject root, MonsterActor actor)
        {
            if (root == null || actor == null) return;

            var chest = FindChild(root, "Chest");
            var hpBarSocket = FindChild(root, "HpBarSocket");
            var lockOn = FindChild(root, "LockOn");

            SetSocket(actor, ActorSocketType.Center, chest);
            SetSocket(actor, ActorSocketType.UI_HpBar, hpBarSocket);
            ReflectionUtil.SetField(actor, "_lockOnDecal", lockOn != null ? lockOn.gameObject : null);

            if (chest == null)
                Debug.LogWarning($"[P09Builder] MonsterActor Center 소켓 대상 'Chest'를 찾지 못했습니다: {root.name}");
            if (hpBarSocket == null)
                Debug.LogWarning($"[P09Builder] MonsterActor UI_HpBar 소켓 대상 'HpBarSocket'을 찾지 못했습니다: {root.name}");
            if (lockOn == null)
                Debug.LogWarning($"[P09Builder] MonsterActor LockOnDecal 대상 'LockOn'을 찾지 못했습니다: {root.name}");
        }

        private static Transform FindChild(GameObject root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName)) return null;

            var transforms = root.GetComponentsInChildren<Transform>(true);
            return transforms.FirstOrDefault(t =>
                string.Equals(t.name, childName, StringComparison.OrdinalIgnoreCase));
        }

        private static void SetSocket(MonsterActor actor, ActorSocketType socketType, Transform socket)
        {
            if (socket == null) return;

            var prop = ReflectionUtil.FindProperty(actor, "_socketDict", out var so);
            var list = prop?.FindPropertyRelative("_serializedList");
            if (so == null || list == null)
            {
                Debug.LogWarning($"[P09Builder] MonsterActor SocketDict 직렬화 필드를 찾지 못했습니다: {actor.name}");
                return;
            }

            var key = (int)socketType;
            for (int i = 0; i < list.arraySize; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("Key")?.intValue != key)
                    continue;

                element.FindPropertyRelative("Value").objectReferenceValue = socket;
                so.ApplyModifiedPropertiesWithoutUndo();
                return;
            }

            list.InsertArrayElementAtIndex(list.arraySize);
            var newElement = list.GetArrayElementAtIndex(list.arraySize - 1);
            newElement.FindPropertyRelative("Key").intValue = key;
            newElement.FindPropertyRelative("Value").objectReferenceValue = socket;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- DescDefs ----------
        private sealed class EnemyStatsDescDef : IDescDef
        {
            public Type DescType => typeof(EnemyStatsSO);
            public string Suffix => "_Stats";

            public void ApplyDefaults(ScriptableObject so, CharacterBuildConfig config)
            {
                if (so is not EnemyStatsSO stats) return;
                if (config?.Stats == null)
                {
                    EditorUtility.SetDirty(so);
                    return;
                }

                var tuning = EnemyStatTuningUtility.Calculate(config);

                stats.level            = Mathf.Max(1, config.Stats.level);
                stats.maxHealth        = config.Stats.defaultHp * tuning.HealthMultiplier;
                stats.walkSpeed        = config.Stats.defaultWalkSpeed * tuning.MoveSpeedMultiplier;
                stats.runSpeed         = config.Stats.defaultRunSpeed * tuning.MoveSpeedMultiplier;
                stats.detectionRadius  = config.Stats.defaultDetectionRadius;
                stats.grade            = config.Stats.grade;

                EditorUtility.SetDirty(so);
            }
        }

        private sealed class PoiseDescDef : IDescDef
        {
            public Type DescType => typeof(PoiseSO);
            public string Suffix => "_Poise";

            public void ApplyDefaults(ScriptableObject so, CharacterBuildConfig config)
            {
                if (so is not PoiseSO poise) return;
                if (config?.Stats == null)
                {
                    EditorUtility.SetDirty(so);
                    return;
                }

                poise.maxPoise = Mathf.Max(1f, config.Stats.defaultMaxPoise);
                poise.recoveryDelay = Mathf.Max(0f, config.Stats.defaultPoiseRecoveryDelay);
                poise.recoveryRate = Mathf.Max(0f, config.Stats.defaultPoiseRecoveryRate);
                poise.hasHyperArmor = config.Stats.defaultHasHyperArmor;

                EditorUtility.SetDirty(so);
            }
        }

        private sealed class ActorStatDescDef : IDescDef
        {
            public Type DescType => typeof(ActorStatSO);
            public string Suffix => "_ActorStat";

            public void ApplyDefaults(ScriptableObject so, CharacterBuildConfig config)
            {
                if (so is not ActorStatSO stat) return;

                stat.EditorFillMissing();

                var stats = config?.Stats;
                if (stats == null)
                {
                    EditorUtility.SetDirty(so);
                    return;
                }

                var tuning = EnemyStatTuningUtility.Calculate(config);
                var sourceStats = stats.existingStatsSo as EnemyStatsSO;
                var sourcePoise = stats.existingPoiseSo as PoiseSO;

                float maxHealth = stats.createNewStats || sourceStats == null
                    ? stats.defaultHp * tuning.HealthMultiplier
                    : sourceStats.maxHealth;
                float moveSpeed = stats.createNewStats || sourceStats == null
                    ? tuning.MoveSpeedMultiplier
                    : Mathf.Max(0.01f, sourceStats.runSpeed / Mathf.Max(0.01f, stats.defaultRunSpeed));

                stat.EditorSet(StatType.MaxHealth, maxHealth);
                stat.EditorSet(StatType.AttackPower, tuning.AttackMultiplier);
                stat.EditorSet(StatType.MoveSpeed, moveSpeed);

                stat.EditorSet(
                    StatType.MaxPoise,
                    stats.createNewPoise || sourcePoise == null ? stats.defaultMaxPoise : sourcePoise.maxPoise);
                stat.EditorSet(
                    StatType.PoiseRecoveryRate,
                    stats.createNewPoise || sourcePoise == null ? stats.defaultPoiseRecoveryRate : sourcePoise.recoveryRate);
                stat.EditorSet(
                    StatType.PoiseRecoveryDelay,
                    stats.createNewPoise || sourcePoise == null ? stats.defaultPoiseRecoveryDelay : sourcePoise.recoveryDelay);

                EditorUtility.SetDirty(so);
            }
        }

        private sealed class EnemyBehaviorDescDef : IDescDef
        {
            public Type DescType => typeof(EnemyBehaviorSO);
            public string Suffix => "_Behavior";

            public void ApplyDefaults(ScriptableObject so, CharacterBuildConfig config)
            {
                if (so is not EnemyBehaviorSO behavior) return;
                if (config?.Stats == null)
                {
                    EditorUtility.SetDirty(so);
                    return;
                }

                behavior.optimalCombatDistance = config.Stats.optimalCombatDistance;
                EditorUtility.SetDirty(so);
            }
        }

        private sealed class EnemyAttackDataDescDef : IDescDef
        {
            public Type DescType => typeof(EnemyAttackDataSO);
            public string Suffix => "_AttackData";

            public void ApplyDefaults(ScriptableObject so, CharacterBuildConfig config)
            {
                if (so is EnemyAttackDataSO attackData && config?.Stats != null)
                {
                    var tuning = EnemyStatTuningUtility.Calculate(config);
                    var skill = new EnemyAttackInfo
                    {
                        baseInfo = new AttackInfoBase
                        {
                            attackType = AttackType.Melee,
                            hitPhases = new List<HitPhaseData>
                            {
                                new HitPhaseData
                                {
                                    damage = config.Stats.defaultAttackDamage * tuning.AttackMultiplier,
                                    poiseDamage = 30f,
                                    reactionType = AttackReactionType.Hit,
                                }
                            }
                        },
                        skillType = SkillType.Attack,
                        selectionWeight = 10f,
                        minRange = 0f,
                        maxRange = Mathf.Max(1f, config.Stats.optimalCombatDistance + 0.5f),
                        cooldown = 2f,
                    };

                    attackData.skills.Clear();
                    attackData.skills.Add(skill);
                }

                EditorUtility.SetDirty(so);
            }
        }
    }
}
