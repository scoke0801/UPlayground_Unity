using System;
using System.Collections.Generic;
using System.Linq;
using KinematicCharacterController;
using UnityEditor;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data;
using UPlayGround.Component;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
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
        }

        public IEnumerable<IDescDef> GetDescDefs(CharacterBuildConfig config)
        {
            // Stats: createNewStats=true일 때만 생성
            if (config != null && config.Stats != null && config.Stats.createNewStats)
                yield return new EnemyStatsDescDef();

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

            if (actor != null && stats != null)
                ReflectionUtil.SetField(actor, "_stats", stats);

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
