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
        public void 데이터_None_카테고리는_명시적_오류로_거부한다()
        {
            var attackInfo = CreateAttackInfo(aiSelectable: true);
            attackInfo.attackCategory = AbilityAttackCategory.None;
            var context = new SkillConditionContext { CurrentLevel = 1 };

            EnemyAbilityRejectReason result = EnemyAbilitySelectionPolicy.Evaluate(
                attackInfo,
                in context,
                AbilityAttackCategory.None,
                aerialOnly: false,
                diveOnly: false);

            Assert.That(
                result.HasFlag(EnemyAbilityRejectReason.MissingAttackCategory),
                Is.True);
        }

        [Test]
        public void 데이터_Any_카테고리는_구체_요청에_매칭한다()
        {
            var attackInfo = CreateAttackInfo(aiSelectable: true);
            attackInfo.attackCategory = AbilityAttackCategory.Any;
            var context = new SkillConditionContext { CurrentLevel = 1 };

            EnemyAbilityRejectReason result = EnemyAbilitySelectionPolicy.Evaluate(
                attackInfo,
                in context,
                AbilityAttackCategory.Skill,
                aerialOnly: false,
                diveOnly: false);

            Assert.That(result, Is.EqualTo(EnemyAbilityRejectReason.None));
        }

        [Test]
        public void 요청_None은_필터없음이고_요청_Any는_유효한_필터가_아니다()
        {
            var attackInfo = CreateAttackInfo(aiSelectable: true);

            Assert.That(
                EnemyAbilitySelectionPolicy.MatchesCategory(
                    attackInfo,
                    AbilityAttackCategory.None),
                Is.True);
            Assert.That(
                EnemyAbilitySelectionPolicy.MatchesCategory(
                    attackInfo,
                    AbilityAttackCategory.Any),
                Is.False);

            attackInfo.attackCategory = AbilityAttackCategory.None;
            Assert.That(
                EnemyAbilitySelectionPolicy.MatchesCategory(
                    attackInfo,
                    AbilityAttackCategory.None),
                Is.False);
        }

        [Test]
        public void 요청한_AI역할이_없으면_후보에서_제외한다()
        {
            var attackInfo = CreateAttackInfo(aiSelectable: true);
            attackInfo.aiRoles = AbilityAIRole.Opener | AbilityAIRole.GapCloser;
            var context = new SkillConditionContext { CurrentLevel = 1 };

            EnemyAbilityRejectReason result = EnemyAbilitySelectionPolicy.Evaluate(
                attackInfo,
                in context,
                AbilityAttackCategory.Basic,
                aerialOnly: false,
                diveOnly: false,
                AbilityAIRole.Counter);

            Assert.That(
                result.HasFlag(EnemyAbilityRejectReason.RoleMismatch),
                Is.True);
        }

        [Test]
        public void 체력조건은_경계포함_설정을_구분한다()
        {
            var upperPhase = new SkillCondition
            {
                minHealthPercent = 0.6f,
                maxHealthPercent = 1f,
                includeMinHealth = false,
                includeMaxHealth = true,
            };
            var lowerPhase = new SkillCondition
            {
                minHealthPercent = 0.3f,
                maxHealthPercent = 0.6f,
                includeMinHealth = false,
                includeMaxHealth = true,
            };

            Assert.That(upperPhase.MatchesHealthPercent(0.6f), Is.False);
            Assert.That(lowerPhase.MatchesHealthPercent(0.6f), Is.True);
            Assert.That(lowerPhase.MatchesHealthPercent(0.3f), Is.False);
        }

        [Test]
        public void 명시적_선택_오버로드는_이전_카테고리와_역할_예약을_폐기한다()
        {
            var gameObject = new GameObject("EnemyCombatReservationTest");
            try
            {
                EnemyCombat combat = gameObject.AddComponent<EnemyCombat>();
                combat.ReserveAttackSelection(
                    AbilityAttackCategory.Heavy,
                    AbilityAIRole.Counter);

                combat.SelectAndExecuteSkill(
                    distanceToTarget: 1f,
                    attackCategory: AbilityAttackCategory.Basic,
                    abilityRole: AbilityAIRole.Opener);

                Assert.That(
                    combat.ReservedAttackCategory,
                    Is.EqualTo(AbilityAttackCategory.None));
                Assert.That(
                    combat.ReservedAbilityRole,
                    Is.EqualTo(AbilityAIRole.None));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void 기본_선택_오버로드는_카테고리와_역할_예약을_함께_소비한다()
        {
            var gameObject = new GameObject("EnemyCombatReservationTest");
            try
            {
                EnemyCombat combat = gameObject.AddComponent<EnemyCombat>();
                combat.ReserveAttackSelection(
                    AbilityAttackCategory.Heavy,
                    AbilityAIRole.Counter);

                combat.SelectAndExecuteSkill(distanceToTarget: 1f);

                Assert.That(
                    combat.ReservedAttackCategory,
                    Is.EqualTo(AbilityAttackCategory.None));
                Assert.That(
                    combat.ReservedAbilityRole,
                    Is.EqualTo(AbilityAIRole.None));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
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
                attackCategory = AbilityAttackCategory.Basic,
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
