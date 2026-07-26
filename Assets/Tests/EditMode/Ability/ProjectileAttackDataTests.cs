using NUnit.Framework;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Projectile;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UPlayGround.Ability.Tests
{
    public sealed class ProjectileAttackDataTests
    {
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
