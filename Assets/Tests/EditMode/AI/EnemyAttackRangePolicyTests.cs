using System.Collections.Generic;
using NUnit.Framework;
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

        [Test]
        public void 실제_활_몬스터_AbilitySet의_사거리_차이를_보존한다()
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
                    currentLevel: 3),
                Is.True);
            Assert.That(
                EnemyAttackRangePolicy.HasAttackInRange(
                    femaleBow,
                    distanceToTarget: 3f,
                    currentLevel: 3),
                Is.False);
        }

        private static AbilityAttackInfo CreateAttackInfo()
        {
            return new AbilityAttackInfo
            {
                aiSelectable = true,
                requiredLevel = 1,
                baseInfo = new AttackInfoBase
                {
                    hitPhases = new List<HitPhaseData> { new() }
                }
            };
        }
    }
}
