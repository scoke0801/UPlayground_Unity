using System.Collections.Generic;
using NUnit.Framework;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Gameplay.Tag;
using UnityEngine;

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

        [Test]
        public void None_공격카테고리는_사용가능한_라우터를_운반경로로_선택한다()
        {
            AbilitySetSO set = ScriptableObject.CreateInstance<AbilitySetSO>();
            GameplayAbilitySO router = CreateRequestRouter(
                GameplayTags.Trigger_Monster_Attack_Heavy);
            set.additionalAbilities.Add(router);
            set.RebuildRuntimeIndex();

            try
            {
                bool resolved = EnemyAbilityTriggerTags.TryResolveAttackTrigger(
                    set,
                    AbilityAttackCategory.None,
                    out AbilityAttackCategory category,
                    out GameplayAbilitySO ability,
                    out GameplayTag tag);

                Assert.That(resolved, Is.True);
                Assert.That(category, Is.EqualTo(AbilityAttackCategory.Heavy));
                Assert.That(ability, Is.SameAs(router));
                Assert.That(tag, Is.EqualTo(GameplayTags.Trigger_Monster_Attack_Heavy));
            }
            finally
            {
                Object.DestroyImmediate(router);
                Object.DestroyImmediate(set);
            }
        }

        [Test]
        public void 명시적_공격카테고리는_다른_라우터로_폴백하지않는다()
        {
            AbilitySetSO set = ScriptableObject.CreateInstance<AbilitySetSO>();
            GameplayAbilitySO router = CreateRequestRouter(
                GameplayTags.Trigger_Monster_Attack_Heavy);
            set.additionalAbilities.Add(router);
            set.RebuildRuntimeIndex();

            try
            {
                Assert.That(
                    EnemyAbilityTriggerTags.TryResolveAttackTrigger(
                        set,
                        AbilityAttackCategory.Basic,
                        out _,
                        out _,
                        out _),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(router);
                Object.DestroyImmediate(set);
            }
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

        private static GameplayAbilitySO CreateRequestRouter(GameplayTag triggerTag)
        {
            GameplayAbilitySO ability =
                ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = triggerTag,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
            });
            return ability;
        }
    }
}
