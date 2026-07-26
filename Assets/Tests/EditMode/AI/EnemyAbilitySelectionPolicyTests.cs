using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.AI.Tests
{
    public sealed class EnemyAbilitySelectionPolicyTests
    {
        [Test]
        public void AiSelectable이_꺼진_공격은_후보에서_제외한다()
        {
            var attackInfo = CreateAttackInfo(aiSelectable: false);
            var context = new SkillConditionContext { CurrentLevel = 1 };

            EnemyAbilityRejectReason result =
                EnemyAbilitySelectionPolicy.Evaluate(
                    attackInfo,
                    in context,
                    AbilityAttackCategory.None,
                    aerialOnly: false,
                    diveOnly: false);

            Assert.That(
                result.HasFlag(EnemyAbilityRejectReason.NotAISelectable),
                Is.True);
        }

        [Test]
        public void 같은_Seed와_가중치는_같은_선택열을_만든다()
        {
            var weights = new List<float> { 1f, 3f, 2f };
            var first = new SeededEnemyAbilityRandomSource(9137);
            var second = new SeededEnemyAbilityRandomSource(9137);

            for (var i = 0; i < 32; i++)
            {
                Assert.That(
                    EnemyAbilitySelectionPolicy.SelectWeightedIndex(weights, first),
                    Is.EqualTo(
                        EnemyAbilitySelectionPolicy.SelectWeightedIndex(weights, second)));
            }
        }

        [Test]
        public void 타입과_카테고리_조건을_탈락사유로_누적한다()
        {
            var attackInfo = CreateAttackInfo(aiSelectable: true);
            attackInfo.isAerialSkill = true;
            attackInfo.attackCategory = AbilityAttackCategory.Heavy;
            var context = new SkillConditionContext { CurrentLevel = 1 };

            EnemyAbilityRejectReason result =
                EnemyAbilitySelectionPolicy.Evaluate(
                    attackInfo,
                    in context,
                    AbilityAttackCategory.Basic,
                    aerialOnly: false,
                    diveOnly: false);

            Assert.That(result.HasFlag(EnemyAbilityRejectReason.AerialMismatch), Is.True);
            Assert.That(result.HasFlag(EnemyAbilityRejectReason.CategoryMismatch), Is.True);
        }

        private static AbilityAttackInfo CreateAttackInfo(bool aiSelectable)
        {
            return new AbilityAttackInfo
            {
                aiSelectable = aiSelectable,
                requiredLevel = 1,
                baseInfo = new AttackInfoBase
                {
                    hitPhases = new List<HitPhaseData> { new() }
                }
            };
        }
    }
}
