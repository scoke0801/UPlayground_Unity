using NUnit.Framework;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Projectile;
using UPlayGround.Data.Event;
using UPlayGround.Animation;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UPlayGround.Ability.Tests
{
    public sealed class ProjectileAttackDataTests
    {
#if UNITY_EDITOR
        [TestCase(false)]
        [TestCase(true)]
        public void 실제_적_단일탄이_GAS_모션_발사_표시까지_연결된다(bool magic)
        {
            string abilityPath = magic ? "Assets/10.Datas/Ability/Actor/P09Staff/GA_P09Staff_Bolt.asset"
                : "Assets/10.Datas/Ability/Actor/Humanoid_BowAttackData/GA_Humanoid_BowAttackData_01_Attack_1.asset";
            string actorPath = magic
                ? "Assets/03.Prefabs/Characters/Enemy/Armor_06/Enemy_P09_Armor06_Staff_F/Enemy_P09_Armor06_Staff_F.prefab"
                : "Assets/03.Prefabs/Characters/Enemy/Armor_06/Enemy_P09_Armor06_Bow/Enemy_P09_Armor06_Bow.prefab";
            var ability = AssetDatabase.LoadAssetAtPath<global::UPlayGround.Data.Ability.GameplayAbilitySO>(abilityPath);
            var payload = ability.variants[0].executionPayload as global::UPlayGround.Ability.UPlayGround.UPlayGroundMotionAbilityPayloadSO;
            Assert.That(payload, Is.Not.Null);
            var definition = payload.attackInfo.baseInfo.hitPhases[0].projectileDefinition;
            Assert.That(payload.attackInfo.defenseType, Is.EqualTo(AttackDefenseType.Parryable));
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.SupportsReflection(), Is.True);
            Assert.That(definition.inheritOwnerTimeScale, Is.False);
            var errors = new List<string>(); definition.CollectValidationErrors(errors);
            Assert.That(errors, Is.Empty);
            var cue = definition.GetBehavior<ReflectableProjectileBehavior>();
            Assert.That(cue.flightCuePrefab.GetComponentsInChildren<TrailRenderer>(true).Length, Is.EqualTo(2));
            Assert.That(cue.returnTrailPrefab.GetComponentsInChildren<TrailRenderer>(true).Length, Is.GreaterThan(0));
            Assert.That(cue.launchCue.samples, Is.GreaterThan(0));
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(actorPath);
            var mapping = prefab.GetComponentInChildren<ActorAnimator>(true).MotionSet;
            var motion = mapping.GetAbilityMotionAsset(payload.attackInfo.motionKey);
            Assert.That(motion, Is.Not.Null);
            Assert.That(SerializationUtility.HasManagedReferencesWithMissingTypes(motion), Is.False);
            int shots = 0;
            foreach (var part in motion.motionSet.motions)
                foreach (var evt in part.events)
                    if (evt is SpawnProjectileEvent shot)
                    {
                        shots++;
                        Assert.That(shot.hitPhaseIndex, Is.Zero);
                        Assert.That(shot.projectileDefinition == null || shot.projectileDefinition == definition, Is.True,
                            "Motion 직접 지정이 GAS Definition을 가리면 안 됩니다.");
                        Assert.That(shot.targetMode, Is.EqualTo(ProjectileTargetMode.EnemySkillTarget));
                    }
            Assert.That(shots, Is.EqualTo(1));
        }

        [TestCase("Area")]
        [TestCase("Split")]
        [TestCase("Detonate")]
        [TestCase("Pierce")]
        [TestCase("Bounce")]
        [TestCase("Arc")]
        [TestCase("Homing")]
        [TestCase("Hitscan")]
        [TestCase("Acceleration")]
        [TestCase("Curve")]
        public void 반사_미지원_조합은_런타임과_저작검사에서_거부한다(string mode)
        {
            var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            try
            {
                definition.behaviors.Add(new ReflectableProjectileBehavior());
                switch (mode)
                {
                    case "Area": definition.behaviors.Add(new AreaTickProjectileBehavior()); break;
                    case "Split": definition.behaviors.Add(new SplitProjectileBehavior()); break;
                    case "Detonate": definition.behaviors.Add(new DetonateProjectileBehavior()); break;
                    case "Pierce": definition.behaviors.Add(new PierceProjectileBehavior()); break;
                    case "Bounce": definition.behaviors.Add(new BounceProjectileBehavior()); break;
                    case "Arc": definition.motion = new ArcProjectileMotion(); break;
                    case "Homing": definition.motion = new HomingProjectileMotion(); break;
                    case "Hitscan": definition.motion = new HitscanProjectileMotion(); break;
                    case "Acceleration": ((LinearProjectileMotion)definition.motion).acceleration = 1f; break;
                    case "Curve": ((LinearProjectileMotion)definition.motion).speedCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f); break;
                }
                Assert.That(definition.SupportsReflection(), Is.False);
                var errors = new List<string>(); definition.CollectValidationErrors(errors);
                Assert.That(errors, Has.Some.Contains("반사 미지원"));
            }
            finally { Object.DestroyImmediate(definition); }
        }

#endif

        [TestCase(-4f)]
        [TestCase(0f)]
        [TestCase(4f)]
        public void 직접사격은_대상_고저차와_스케일을_반영한_캡슐_중심을_조준한다(float elevation)
        {
            var target = new GameObject("화살 조준 대상");
            try
            {
                target.transform.position = new Vector3(2f, elevation, 10f);
                target.transform.localScale = new Vector3(1f, 1.5f, 1f);
                var capsule = target.AddComponent<CapsuleCollider>();
                capsule.center = Vector3.up;
                capsule.height = 2f;
                var resolve = typeof(SpawnProjectileEvent).GetMethod(
                    "GetActorTargetPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                Assert.That(resolve, Is.Not.Null);
                Vector3 point = (Vector3)resolve.Invoke(null, new object[] { target.transform, true });
                Assert.That(point, Is.EqualTo(new Vector3(2f, elevation + 1.5f, 10f)));
                Vector3 groundPoint = (Vector3)resolve.Invoke(null, new object[] { target.transform, false });
                Assert.That(groundPoint, Is.EqualTo(target.transform.position), "지점 지정 공격의 높이를 바꾸면 안 됩니다.");
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void 캡슐이_없는_대상은_기존_대상_좌표를_사용한다()
        {
            var target = new GameObject("캡슐 없는 대상");
            try
            {
                target.transform.position = new Vector3(2f, 3f, 10f);
                var resolve = typeof(SpawnProjectileEvent).GetMethod(
                    "GetActorTargetPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                Assert.That(resolve, Is.Not.Null);
                Assert.That((Vector3)resolve.Invoke(null, new object[] { target.transform, true }),
                    Is.EqualTo(target.transform.position));
            }
            finally
            {
                Object.DestroyImmediate(target);
            }
        }

#if UNITY_EDITOR
        [Test]
        public void 몬스터_화살_모션_전체가_지면보다_높게_발사하고_직선탄은_대상을_조준한다()
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:MotionSetAsset"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Player/")) continue;
                var asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                var serialized = new SerializedObject(asset);
                var iterator = serialized.GetIterator();
                while (iterator.Next(true))
                {
                    if (iterator.propertyType != SerializedPropertyType.ManagedReference
                        || iterator.managedReferenceValue is not SpawnProjectileEvent shot
                        || shot.projectilePrefab == null
                        || (shot.projectilePrefab.name != "DefaultArrow" && shot.projectilePrefab.name != "SeolA_Arcing_Arrow"))
                        continue;
                    count++;
                    Assert.That(shot.spawnOffset.y, Is.GreaterThanOrEqualTo(1f), path);
                    Assert.That(shot.projectTargetToGround, Is.False, path);
                    if (shot.projectilePrefab is LinearProjectile)
                        Assert.That(shot.targetMode, Is.EqualTo(ProjectileTargetMode.EnemySkillTarget), path);
                }
            }
            Assert.That(count, Is.GreaterThan(0), "몬스터 화살 모션이 한 건도 검사되지 않았습니다.");
        }
#endif

        [Test]
        public void CreateFromAbility_지정한_히트페이즈와_방어타입을_복사한다()
        {
            var phase = new HitPhaseData
            {
                damage = 10f,
                poiseDamage = 17f,
                breakDamage = 23f,
                reactionType = AttackReactionType.KnockBack,
                reactionDuration = 0.75f,
                forceReaction = true,
                forceBreakExpose = true,
                hitParticleName = "ProjectileHeavyHit",
                pullForce = 4f,
                airborneForce = 5f,
                knockBackForce = 6f,
                knockBackDrag = 7f,
                grabDuration = 1.25f,
                guaranteedReaction = true,
            };
            var attackInfo = new AbilityAttackInfo
            {
                baseInfo = new AttackInfoBase(),
                defenseType = AttackDefenseType.Unblockable,
            };
            attackInfo.baseInfo.hitPhases.Clear();
            attackInfo.baseInfo.hitPhases.Add(new HitPhaseData());
            attackInfo.baseInfo.hitPhases.Add(phase);

            AttackData result = PlayerAttackController.CreateFromAbility(
                attackInfo,
                AttackKind.SkillAttack,
                1);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.hitPhaseIndex, Is.EqualTo(1));
            Assert.That(result.damage, Is.InRange(8f, 12f));
            Assert.That(result.poiseDamage, Is.EqualTo(17f));
            Assert.That(result.breakDamage, Is.EqualTo(23f));
            Assert.That(result.reactionType, Is.EqualTo(AttackReactionType.KnockBack));
            Assert.That(result.reactionDuration, Is.EqualTo(0.75f));
            Assert.That(result.forceReaction, Is.True);
            Assert.That(result.forceBreakExpose, Is.True);
            Assert.That(result.hitParticleName, Is.EqualTo("ProjectileHeavyHit"));
            Assert.That(result.pullForce, Is.EqualTo(4f));
            Assert.That(result.airborneForce, Is.EqualTo(5f));
            Assert.That(result.knockbackForce, Is.EqualTo(6f));
            Assert.That(result.knockbackDrag, Is.EqualTo(7f));
            Assert.That(result.grabDuration, Is.EqualTo(1.25f));
            Assert.That(result.guaranteedReaction, Is.True);
            Assert.That(result.defenseType, Is.EqualTo(AttackDefenseType.Unblockable));
            Assert.That(result.criticalMultiplier, Is.EqualTo(1f));
            Assert.That(result.attackKind, Is.EqualTo(AttackKind.SkillAttack));
        }

        [Test]
        public void ApplyHitPhase_런타임_배율을_유지해_스냅샷에_적용한다()
        {
            var data = new AttackData
            {
                damageMultiplier = 2f,
                poiseMultiplier = 3f,
                breakDamageMultiplier = 4f,
            };
            var phase = new HitPhaseData
            {
                damage = 10f,
                poiseDamage = 5f,
                breakDamage = 7f,
            };

            PlayerAttackController.ApplyHitPhase(data, phase, 2);

            Assert.That(data.hitPhaseIndex, Is.EqualTo(2));
            Assert.That(data.damage, Is.InRange(16f, 24f));
            Assert.That(data.poiseDamage, Is.EqualTo(15f));
            Assert.That(data.breakDamage, Is.EqualTo(84f));
        }

        [Test]
        public void Copy_투사체_스냅샷을_원본과_독립시킨다()
        {
            var source = new AttackData
            {
                damage = 12f,
                defenseType = AttackDefenseType.GuardableOnly,
                isProjectile = true,
            };

            AttackData copy = PlayerAttackController.Copy(source);
            copy.damage = 99f;

            Assert.That(source.damage, Is.EqualTo(12f));
            Assert.That(copy.defenseType, Is.EqualTo(AttackDefenseType.GuardableOnly));
            Assert.That(copy.isProjectile, Is.True);
        }

        [Test]
        public void Definition_Hitscan과_Bounce_조합을_거부한다()
        {
            ProjectileDefinitionSO definition =
                ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            definition.motion = new HitscanProjectileMotion();
            definition.behaviors = new List<ProjectileBehaviorData>
            {
                new BounceProjectileBehavior(),
            };
            var errors = new List<string>();

            definition.CollectValidationErrors(errors);

            Assert.That(errors, Has.Some.Contains("HitscanMotion"));
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Definition_프리웜이_최대풀보다_크면_오류다()
        {
            ProjectileDefinitionSO definition =
                ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            definition.prewarmCount = 8;
            definition.maxPoolSize = 4;
            var errors = new List<string>();

            definition.CollectValidationErrors(errors);

            Assert.That(errors, Has.Some.Contains("prewarmCount"));
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Definition_정지형과_충돌이동_Behavior_조합을_거부한다()
        {
            ProjectileDefinitionSO definition =
                ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            definition.motion = new StationaryProjectileMotion();
            definition.behaviors = new List<ProjectileBehaviorData>
            {
                new PierceProjectileBehavior(),
            };
            var errors = new List<string>();

            definition.CollectValidationErrors(errors);

            Assert.That(errors, Has.Some.Contains("StationaryMotion"));
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Definition_중복_Behavior와_자기참조_Split을_거부한다()
        {
            ProjectileDefinitionSO definition =
                ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            definition.behaviors = new List<ProjectileBehaviorData>
            {
                new PierceProjectileBehavior(),
                new PierceProjectileBehavior(),
                new SplitProjectileBehavior { childDefinition = definition },
            };
            var errors = new List<string>();

            definition.CollectValidationErrors(errors);

            Assert.That(errors, Has.Some.Contains("PierceProjectileBehavior이 중복"));
            Assert.That(errors, Has.Some.Contains("자기 자신"));
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void BurstPattern_간격에_맞춰_지연샷을_생성한다()
        {
            var pattern = new BurstShotPattern { count = 3, interval = 0.2f };
            var shots = new List<ProjectilePatternShot>();

            pattern.Build(Vector3.forward, shots);

            Assert.That(shots, Has.Count.EqualTo(3));
            Assert.That(shots[0].Delay, Is.Zero);
            Assert.That(shots[1].Delay, Is.EqualTo(0.2f));
            Assert.That(shots[2].Delay, Is.EqualTo(0.4f));
        }

        [Test]
        public void Runtime_풀_반환시_상태를_초기화한다()
        {
            var gameObject = new GameObject("ProjectileRuntimeTest");
            var runtime = gameObject.AddComponent<ProjectileRuntime>();
            ProjectileDefinitionSO definition =
                ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            var request = new ProjectileSpawnRequest
            {
                definition = definition,
                origin = Vector3.zero,
                logicalOrigin = Vector3.zero,
                direction = Vector3.forward,
                damageScale = 1f,
            };
            runtime.Initialize(definition, request, new AttackData(), null, null);

            runtime.OnReturnedToPool();

            Assert.That(runtime.IsReset, Is.True);
            Assert.That(runtime.HitTargetCount, Is.Zero);
            Object.DestroyImmediate(gameObject);
            Object.DestroyImmediate(definition);
        }

#if UNITY_EDITOR
        [Test]
        public void 프로젝트의_모든_ProjectileDefinition_조합이_유효하다()
        {
            string[] guids = AssetDatabase.FindAssets("t:ProjectileDefinitionSO");
            var errors = new List<string>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ProjectileDefinitionSO definition =
                    AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(path);
                definition?.CollectValidationErrors(errors);
            }

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }
#endif
    }
}
