using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.Tests
{
    public sealed class EnemyAttackRangePolicyTests
    {
        private GameplayAbilitySO _ability;

        [SetUp]
        public void SetUp()
        {
            _ability = ScriptableObject.CreateInstance<GameplayAbilitySO>();
            _ability.activation.minDistance = 1f;
            _ability.activation.maxDistance = 5f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_ability);
        }

        [TestCase(0.9f, false)]
        [TestCase(1f, true)]
        [TestCase(3f, true)]
        [TestCase(5f, true)]
        [TestCase(5.1f, false)]
        public void Ability_활성화_사거리만으로_정적_커버리지를_판정한다(
            float distance,
            bool expected)
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();

            bool result = EnemyAttackRangePolicy.CoversDistance(
                _ability,
                attackInfo,
                distance,
                currentLevel: 1);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void RangeBased_AND_조건은_정적_사거리와_함께_적용한다()
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();
            attackInfo.conditionGroup = new SkillConditionGroup
            {
                conditionOperator = ConditionOperator.And,
                conditions = new List<SkillCondition>
                {
                    new()
                    {
                        type = ConditionType.RangeBased,
                        minRange = 2f,
                        maxRange = 4f
                    }
                }
            };

            Assert.That(
                EnemyAttackRangePolicy.CoversDistance(
                    _ability,
                    attackInfo,
                    1.5f,
                    currentLevel: 1),
                Is.False);
            Assert.That(
                EnemyAttackRangePolicy.CoversDistance(
                    _ability,
                    attackInfo,
                    3f,
                    currentLevel: 1),
                Is.True);
        }

        [Test]
        public void OR_그룹의_동적_조건은_정적_사거리_판정에서_차단하지_않는다()
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();
            attackInfo.conditionGroup = new SkillConditionGroup
            {
                conditionOperator = ConditionOperator.Or,
                conditions = new List<SkillCondition>
                {
                    new()
                    {
                        type = ConditionType.RangeBased,
                        minRange = 4f,
                        maxRange = 5f
                    },
                    new()
                    {
                        type = ConditionType.SelfHealthBased,
                        minHealthPercent = 0f,
                        maxHealthPercent = 0.5f
                    }
                }
            };

            bool result = EnemyAttackRangePolicy.CoversDistance(
                _ability,
                attackInfo,
                2f,
                currentLevel: 1);

            Assert.That(result, Is.True);
        }

        [Test]
        public void AI_선택_불가_공격은_정적_커버리지에서_제외한다()
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();
            attackInfo.aiSelectable = false;

            bool result = EnemyAttackRangePolicy.CoversDistance(
                _ability,
                attackInfo,
                3f,
                currentLevel: 1);

            Assert.That(result, Is.False);
        }

        [TestCase(0.5f, EnemyAttackDistanceRelation.TooClose)]
        [TestCase(3f, EnemyAttackDistanceRelation.InRange)]
        [TestCase(6f, EnemyAttackDistanceRelation.TooFar)]
        public void 공격_거리_관계는_최소와_최대_범위를_구분한다(
            float distance,
            EnemyAttackDistanceRelation expected)
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();

            EnemyAttackDistanceRelation result =
                EnemyAttackRangePolicy.EvaluateAttackDistance(
                    _ability,
                    attackInfo,
                    distance,
                    currentLevel: 1);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void 요청_카테고리_후보가_없으면_접근하지_않는다()
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();
            attackInfo.attackCategory = AbilityAttackCategory.Basic;

            EnemyAttackDistanceRelation result =
                EnemyAttackRangePolicy.EvaluateAttackDistance(
                    _ability,
                    attackInfo,
                    distanceToTarget: 6f,
                    currentLevel: 1,
                    attackCategory: AbilityAttackCategory.Heavy);

            Assert.That(result, Is.EqualTo(EnemyAttackDistanceRelation.Unavailable));
        }

        [Test]
        public void 데이터_None_카테고리는_정적_커버리지에서도_제외한다()
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();
            attackInfo.attackCategory = AbilityAttackCategory.None;

            bool result = EnemyAttackRangePolicy.CoversDistance(
                _ability,
                attackInfo,
                distanceToTarget: 3f,
                currentLevel: 1,
                attackCategory: AbilityAttackCategory.None);

            Assert.That(result, Is.False);
        }

        [Test]
        public void 데이터_Any_카테고리는_구체_요청의_정적_커버리지에_참여한다()
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();
            attackInfo.attackCategory = AbilityAttackCategory.Any;

            bool result = EnemyAttackRangePolicy.CoversDistance(
                _ability,
                attackInfo,
                distanceToTarget: 3f,
                currentLevel: 1,
                attackCategory: AbilityAttackCategory.Skill);

            Assert.That(result, Is.True);
        }

        [Test]
        public void 요청_AI역할이_없는_공격은_정적_커버리지에서_제외한다()
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();
            attackInfo.aiRoles = AbilityAIRole.Opener;

            bool result = EnemyAttackRangePolicy.CoversDistance(
                _ability,
                attackInfo,
                distanceToTarget: 3f,
                currentLevel: 1,
                abilityRole: AbilityAIRole.Counter);

            Assert.That(result, Is.False);
        }

        [Test]
        public void AbilitySet_판정도_너무가까움과_접근필요를_구분한다()
        {
            var payload = ScriptableObject.CreateInstance<UPlayGroundMotionAbilityPayloadSO>();
            var abilitySet = ScriptableObject.CreateInstance<AbilitySetSO>();
            try
            {
                payload.attackInfo = CreateAttackInfo();
                _ability.variants.Add(new AbilityVariantDefinition
                {
                    variantId = "Default",
                    executionPayload = payload,
                });
                abilitySet.additionalAbilities.Add(_ability);
                abilitySet.RebuildRuntimeIndex();

                Assert.That(
                    EnemyAttackRangePolicy.EvaluateAttackDistance(
                        abilitySet,
                        distanceToTarget: 0.5f,
                        currentLevel: 1),
                    Is.EqualTo(EnemyAttackDistanceRelation.TooClose));
                Assert.That(
                    EnemyAttackRangePolicy.EvaluateAttackDistance(
                        abilitySet,
                        distanceToTarget: 6f,
                        currentLevel: 1),
                    Is.EqualTo(EnemyAttackDistanceRelation.TooFar));
            }
            finally
            {
                Object.DestroyImmediate(abilitySet);
                Object.DestroyImmediate(payload);
            }
        }

        [Test]
        public void 근접_안전접근은_HitPhase와_PersonalSpace로_공격_시작거리를_좁힌다()
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();
            attackInfo.baseInfo.hitPhases[0].targetingRange = 1.5f;

            Assert.That(
                EnemyAttackRangePolicy.CoversDistance(
                    _ability,
                    attackInfo,
                    1.3f,
                    currentLevel: 1,
                    useMeleeApproachRange: true,
                    personalSpaceDistance: 0.8f),
                Is.True);
            Assert.That(
                EnemyAttackRangePolicy.CoversDistance(
                    _ability,
                    attackInfo,
                    1.5f,
                    currentLevel: 1,
                    useMeleeApproachRange: true,
                    personalSpaceDistance: 0.8f),
                Is.False);
        }

        [Test]
        public void 대형_근접_몬스터는_PersonalSpace만큼_접근거리를_확보한다()
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();
            attackInfo.baseInfo.hitPhases[0].targetingRange = 1.5f;

            float effectiveMax = EnemyAttackRangePolicy.ResolveEffectiveMaxDistance(
                _ability,
                attackInfo,
                personalSpaceDistance: 1.5f);

            Assert.That(effectiveMax, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void PersonalSpace_하한은_베이크된_Activation_최대거리를_넘지_않는다()
        {
            AbilityAttackInfo attackInfo = CreateAttackInfo();
            attackInfo.baseInfo.hitPhases[0].targetingRange = 0.8f;
            _ability.activation.minDistance = 0f;
            _ability.activation.maxDistance = 0.8f;

            float effectiveMax = EnemyAttackRangePolicy.ResolveEffectiveMaxDistance(
                _ability,
                attackInfo,
                personalSpaceDistance: 0.8f);

            Assert.That(effectiveMax, Is.EqualTo(0.8f).Within(0.001f));
        }

        [Test]
        public void 실제_활_몬스터의_일반공격과_카운터_사거리_차이를_보존한다()
        {
            AbilitySetSO skeletonBow = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(
                "Assets/10.Datas/Ability/Actor/Skeleton_Bow/AbilitySet_Skeleton_Bow.asset");
            AbilitySetSO femaleBow = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(
                "Assets/10.Datas/Ability/Actor/Humanoid_BowAttackData/AbilitySet_Humanoid_BowAttackData.asset");

            Assert.That(skeletonBow, Is.Not.Null);
            Assert.That(femaleBow, Is.Not.Null);
            Assert.That(
                EnemyAttackRangePolicy.HasAttackInRange(
                    skeletonBow,
                    distanceToTarget: 8f,
                    currentLevel: 1),
                Is.True);
            Assert.That(
                EnemyAttackRangePolicy.HasAttackInRange(
                    femaleBow,
                    distanceToTarget: 2f,
                    currentLevel: 3,
                    abilityRole: AbilityAIRole.Counter),
                Is.True);
            Assert.That(
                EnemyAttackRangePolicy.HasAttackInRange(
                    femaleBow,
                    distanceToTarget: 3f,
                    currentLevel: 3,
                    abilityRole: AbilityAIRole.Counter),
                Is.False);
        }

        private static AbilityAttackInfo CreateAttackInfo()
        {
            return new AbilityAttackInfo
            {
                aiSelectable = true,
                attackCategory = AbilityAttackCategory.Basic,
                requiredLevel = 1,
                baseInfo = new AttackInfoBase
                {
                    hitPhases = new List<HitPhaseData> { new() }
                }
            };
        }
    }
}
