#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Editor.Ability.Production;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;

namespace UPlayGround.Ability.Tests
{
    public sealed class AbilityProductionPlannerTests
    {
        private const string FactoryTestRoot =
            "Assets/Tests/EditMode/Ability/__GeneratedFactoryTests";

        private AbilitySetSO _set;
        private ActorAnimationMotionSet _motionOwner;
        private MotionSetAsset _motion;

        [SetUp]
        public void SetUp()
        {
            _set = ScriptableObject.CreateInstance<AbilitySetSO>();
            _motionOwner =
                ScriptableObject.CreateInstance<ActorAnimationMotionSet>();
            _motion = ScriptableObject.CreateInstance<MotionSetAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_set);
            Object.DestroyImmediate(_motionOwner);
            Object.DestroyImmediate(_motion);
            if (AssetDatabase.IsValidFolder(FactoryTestRoot))
            {
                AssetDatabase.DeleteAsset(FactoryTestRoot);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void MonsterBasicMelee_공용MotionTaskGraph를사용한다()
        {
            AbilityRecipeDefinition recipe =
                AbilityRecipeCatalog.MonsterBasicMelee;

            Assert.That(recipe.RecipeId, Is.EqualTo("Monster.Basic.Melee"));
            Assert.That(recipe.AiSelectable, Is.True);
            Assert.That(
                recipe.DefaultTaskGraphPath,
                Is.EqualTo(AbilityRecipeCatalog.SharedMotionTaskGraphPath));
        }

        [Test]
        public void RecipeCatalog_초기필수레시피6종을제공한다()
        {
            Assert.That(AbilityRecipeCatalog.All.Count, Is.EqualTo(6));
            Assert.That(
                AbilityRecipeCatalog.All.Select(x => x.RecipeId),
                Is.EquivalentTo(new[]
                {
                    "Player.Basic.Melee",
                    "Player.Skill.Projectile",
                    "Monster.Basic.Melee",
                    "Monster.Heavy.Telegraph",
                    "Combat.AreaAttack",
                    "Support.HealOrBuff",
                }));
            Assert.That(
                AbilityRecipeCatalog.SupportHealOrBuff.RequiresEffect,
                Is.True);
        }

        [Test]
        public void Build_같은입력이면결정적경로를만든다()
        {
            AbilityCreationRequest request = CreateValidRequest(
                "Ability.Tests.Production.Deterministic");

            AbilityCreationPlan first = AbilityCreationPlanner.Build(request);
            AbilityCreationPlan second = AbilityCreationPlanner.Build(request);

            Assert.That(first.AbilityPath, Is.EqualTo(second.AbilityPath));
            Assert.That(first.PayloadPath, Is.EqualTo(second.PayloadPath));
            Assert.That(
                first.AbilityPath,
                Is.EqualTo(
                    "Assets/Temp/AbilityProductionTests/Abilities/"
                    + "GA_TestAttack.asset"));
            Assert.That(
                first.PayloadPath,
                Is.EqualTo(
                    "Assets/Temp/AbilityProductionTests/Payloads/"
                    + "AbilityPayload_TestAttack.asset"));
        }

        [Test]
        public void Build_Motion이없는요청은적용을차단한다()
        {
            AbilityCreationRequest request = CreateValidRequest(
                "Ability.Tests.Production.MissingMotion");
            request.Motion = null;

            AbilityCreationPlan plan = AbilityCreationPlanner.Build(request);

            Assert.That(plan.CanApply, Is.False);
            Assert.That(
                plan.Issues.Any(x =>
                    x.Code == "REQUEST.MOTION"
                    && x.Severity == AbilityProductionSeverity.Error),
                Is.True);
        }

        [Test]
        public void Build_거리순서가잘못되면적용을차단한다()
        {
            AbilityCreationRequest request = CreateValidRequest(
                "Ability.Tests.Production.Distance");
            request.MinDistance = 5f;
            request.MaxDistance = 2f;

            AbilityCreationPlan plan = AbilityCreationPlanner.Build(request);

            Assert.That(plan.CanApply, Is.False);
            Assert.That(
                plan.Issues.Any(x => x.Code == "REQUEST.DISTANCE_ORDER"),
                Is.True);
        }

        [Test]
        public void Factory_Ability와Payload를생성하고Set에연결한다()
        {
            AbilityCreationRequest request = CreatePersistentRequest(
                "Ability.Tests.Production.FactorySuccess",
                "FactorySuccess");
            AbilityCreationPlan plan = AbilityCreationPlanner.Build(request);

            AbilityProductionResult result = AbilityAssetFactory.Apply(plan);

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(
                    plan.AbilityPath),
                Is.SameAs(result.Ability));
            Assert.That(
                AssetDatabase.LoadMainAssetAtPath(plan.PayloadPath),
                Is.SameAs(result.Payload));
            Assert.That(request.TargetSet.Contains(result.Ability), Is.True);
            Assert.That(result.Ability.taskGraph.Root, Is.Not.Null);
            Assert.That(
                result.Payload.attackInfo.baseInfo.motionKey,
                Is.EqualTo(new AbilityMotionKey(request.AbilityId, "Default")));
            Assert.That(
                request.MotionOwner.GetAbilityMotionAsset(
                    result.Payload.attackInfo.baseInfo.motionKey),
                Is.SameAs(request.Motion));
        }

        [Test]
        public void Factory_생성후검증실패시모든변경을롤백한다()
        {
            AbilityCreationRequest request = CreatePersistentRequest(
                "Ability.Tests.Production.FactoryRollback",
                "FactoryRollback");
            AbilityCreationPlan plan = AbilityCreationPlanner.Build(request);

            AbilityProductionResult result = AbilityAssetFactory.ApplyForTests(
                plan,
                AbilityProductionStage.AbilityCreated);

            Assert.That(result.Success, Is.False);
            Assert.That(
                AssetDatabase.LoadMainAssetAtPath(plan.AbilityPath),
                Is.Null);
            Assert.That(
                AssetDatabase.LoadMainAssetAtPath(plan.PayloadPath),
                Is.Null);
            Assert.That(request.TargetSet.additionalAbilities, Is.Empty);
        }

        [Test]
        public void Factory_Preview이후경로충돌은기존에셋을덮어쓰지않는다()
        {
            AbilityCreationRequest request = CreatePersistentRequest(
                "Ability.Tests.Production.PathRace",
                "PathRace");
            AbilityCreationPlan plan = AbilityCreationPlanner.Build(request);
            EnsureFolder(
                System.IO.Path.GetDirectoryName(plan.AbilityPath)
                    ?.Replace('\\', '/'));
            var sentinel = ScriptableObject.CreateInstance<GameplayAbilitySO>();
            sentinel.abilityId = "Ability.Tests.Production.Sentinel";
            AssetDatabase.CreateAsset(sentinel, plan.AbilityPath);
            AssetDatabase.SaveAssets();

            AbilityProductionResult result = AbilityAssetFactory.Apply(plan);

            Assert.That(result.Success, Is.False);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(
                    plan.AbilityPath),
                Is.SameAs(sentinel));
            Assert.That(
                AssetDatabase.LoadMainAssetAtPath(plan.PayloadPath),
                Is.Null);
            Assert.That(request.TargetSet.additionalAbilities, Is.Empty);
        }

        [Test]
        public void Factory_PlayerSkillSlot에연결한다()
        {
            AbilityCreationRequest request = CreatePersistentRequest(
                "Ability.Tests.Production.PlayerSlot",
                "PlayerSlot");
            request.Recipe = AbilityRecipeCatalog.PlayerSkillProjectile;
            request.BindingMode = AbilitySetBindingMode.PlayerSkillSlot;
            request.PlayerSkillSlot = PlayerSkillSlot.Ultimate;

            AbilityProductionResult result = AbilityAssetFactory.Apply(
                AbilityCreationPlanner.Build(request));

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(
                request.TargetSet.GetPlayerAbility(PlayerSkillSlot.Ultimate),
                Is.SameAs(result.Ability));
            Assert.That(request.TargetSet.additionalAbilities, Is.Empty);
        }

        [Test]
        public void Factory_새Effect를생성하고Commit에연결한다()
        {
            AbilityCreationRequest request = CreatePersistentRequest(
                "Ability.Tests.Production.Effect",
                "Effect");
            request.Recipe = AbilityRecipeCatalog.SupportHealOrBuff;
            request.BindingMode = AbilitySetBindingMode.PlayerSkillSlot;
            request.PlayerSkillSlot = PlayerSkillSlot.Ability;
            request.CreateCommitEffect = true;
            request.EffectId = "Effect.Tests.Production.New";
            request.EffectAssetName = "ProductionNew";
            request.EffectDurationType =
                GameplayEffectDurationType.Duration;
            request.EffectDurationSeconds = 1f;
            request.EffectAttributeId = "Vital.Health";
            request.EffectModifierValue = 5f;
            AbilityCreationPlan plan = AbilityCreationPlanner.Build(request);

            AbilityProductionResult result = AbilityAssetFactory.Apply(plan);

            Assert.That(result.Success, Is.True, result.Message);
            Assert.That(result.Effect, Is.Not.Null);
            Assert.That(
                AssetDatabase.LoadAssetAtPath<GameplayEffectSO>(
                    plan.EffectPath),
                Is.SameAs(result.Effect));
            Assert.That(
                result.Ability.commitEffects.Single(),
                Is.SameAs(result.Effect));
        }

        [Test]
        public void MotionAnalyzer_Collision인덱스로필요HitPhase를계산한다()
        {
            var payload =
                ScriptableObject.CreateInstance<
                    UPlayGroundMotionAbilityPayloadSO>();
            payload.attackInfo = new AbilityAttackInfo
            {
                baseInfo = new AttackInfoBase
                {
                    motionKey = new AbilityMotionKey(
                        "Ability.Tests.MotionAnalyzer",
                        "Default"),
                    hitPhases = new System.Collections.Generic.List<
                        HitPhaseData> { new() },
                },
            };
            _motionOwner.SetAbilityMotionAsset(
                payload.attackInfo.baseInfo.motionKey,
                _motion);
            _motion.motionSet.motions.Add(new global::UPlayGround.Animation.Motion
            {
                events = new System.Collections.Generic.List<
                    MotionEventBase>
                {
                    new BeginCollisionEvent { hitPhaseIndex = 2 },
                    new SpawnProjectileEvent(),
                },
            });

            AbilityMotionReport report =
                AbilityMotionAnalyzer.Analyze(payload, _motionOwner);

            Assert.That(report.RequiredHitPhaseCount, Is.EqualTo(3));
            Assert.That(report.HasProjectileEvent, Is.True);
            Assert.That(
                report.Issues.Any(x =>
                    x.Code == "MOTION.HIT_PHASE_SHORTAGE"),
                Is.True);
            Assert.That(
                AbilityMotionAnalyzer.ExpandHitPhasesToMatch(
                    payload,
                    report),
                Is.True);
            Assert.That(
                payload.attackInfo.baseInfo.hitPhases,
                Has.Count.EqualTo(3));
            Object.DestroyImmediate(payload);
        }

        [Test]
        public void TaskGraphValidator_공용Graph는순환오류가없다()
        {
            AbilityTaskGraphSO graph =
                AssetDatabase.LoadAssetAtPath<AbilityTaskGraphSO>(
                    AbilityRecipeCatalog.SharedMotionTaskGraphPath);

            var issues = AbilityTaskGraphValidator.Validate(graph);

            Assert.That(
                issues.Any(x =>
                    x.Severity == AbilityProductionSeverity.Error),
                Is.False,
                string.Join("\n", issues.Select(x => x.Message)));
        }

        [Test]
        public void BalanceAnalyzer_HitPhase합계와Replay거리를비교한다()
        {
            var ability = ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.abilityId = "Ability.Tests.Balance";
            ability.activation.minDistance = 1f;
            ability.activation.maxDistance = 3f;
            var payload =
                ScriptableObject.CreateInstance<
                    UPlayGroundMotionAbilityPayloadSO>();
            payload.attackInfo = new AbilityAttackInfo
            {
                baseInfo = new AttackInfoBase
                {
                    motionKey = new AbilityMotionKey(
                        ability.abilityId,
                        "Default"),
                },
            };
            payload.attackInfo.baseInfo.hitPhases =
                new System.Collections.Generic.List<HitPhaseData>
                {
                    new() { damage = 10f },
                    new() { damage = 15f },
                };
            payload.attackInfo.selectionWeight = 10f;
            ability.variants.Add(new AbilityVariantDefinition
            {
                variantId = "Default",
                executionPayload = payload,
            });
            var replay = new AbilityReplayData
            {
                frames = new System.Collections.Generic.List<
                    AbilityReplayFrame>
                {
                    new() { distance = 5f, hasAttackSlot = false },
                    new() { distance = 5f, hasAttackSlot = false },
                },
            };

            AbilityStaticBalanceSummary summary =
                AbilityBalanceAnalyzer.Summarize(ability);
            AbilityReplayComparison comparison =
                BalanceReplayComparator.Compare(ability, payload, replay);

            Assert.That(summary.TotalDamage, Is.EqualTo(25f));
            Assert.That(summary.HitPhaseCount, Is.EqualTo(2));
            Assert.That(
                comparison.Findings.Any(x =>
                    x.Contains("활성화 거리 밖")),
                Is.True);
            Object.DestroyImmediate(payload);
            Object.DestroyImmediate(ability);
        }

        private AbilityCreationRequest CreateValidRequest(string abilityId)
        {
            AbilityTaskGraphSO taskGraph =
                AssetDatabase.LoadAssetAtPath<AbilityTaskGraphSO>(
                    AbilityRecipeCatalog.SharedMotionTaskGraphPath);
            Assert.That(taskGraph, Is.Not.Null);
            Assert.That(taskGraph.Root, Is.Not.Null);

            return new AbilityCreationRequest
            {
                Recipe = AbilityRecipeCatalog.MonsterBasicMelee,
                DisplayName = "테스트 공격",
                AbilityId = abilityId,
                AssetName = "TestAttack",
                SaveRoot = "Assets/Temp/AbilityProductionTests",
                TargetSet = _set,
                MotionOwner = _motionOwner,
                Motion = _motion,
                TaskGraph = taskGraph,
                RequiredLevel = 1,
                SelectionWeight = 10f,
                MinDistance = 0f,
                MaxDistance = 3f,
            };
        }

        private AbilityCreationRequest CreatePersistentRequest(
            string abilityId,
            string assetName)
        {
            if (AssetDatabase.IsValidFolder(FactoryTestRoot))
                AssetDatabase.DeleteAsset(FactoryTestRoot);
            EnsureFolder(FactoryTestRoot);

            var set = ScriptableObject.CreateInstance<AbilitySetSO>();
            AssetDatabase.CreateAsset(
                set,
                $"{FactoryTestRoot}/AbilitySet_Test.asset");

            var motion = ScriptableObject.CreateInstance<MotionSetAsset>();
            AssetDatabase.CreateAsset(
                motion,
                $"{FactoryTestRoot}/Motion_Test.asset");

            var motionOwner =
                ScriptableObject.CreateInstance<ActorAnimationMotionSet>();
            AssetDatabase.CreateAsset(
                motionOwner,
                $"{FactoryTestRoot}/ActorMotionSet_Test.asset");
            AssetDatabase.SaveAssets();

            return new AbilityCreationRequest
            {
                Recipe = AbilityRecipeCatalog.MonsterBasicMelee,
                DisplayName = "Factory 테스트 공격",
                AbilityId = abilityId,
                AssetName = assetName,
                SaveRoot = FactoryTestRoot,
                TargetSet = set,
                MotionOwner = motionOwner,
                Motion = motion,
                TaskGraph = AssetDatabase.LoadAssetAtPath<AbilityTaskGraphSO>(
                    AbilityRecipeCatalog.SharedMotionTaskGraphPath),
                RequiredLevel = 1,
                SelectionWeight = 10f,
                MinDistance = 0f,
                MaxDistance = 3f,
            };
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = System.IO.Path.GetDirectoryName(path)
                ?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
