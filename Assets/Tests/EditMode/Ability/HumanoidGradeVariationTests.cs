using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Ability.Tests
{
    /// <summary>실사용 등급 Set의 레벨 경계와 역할·등급 격리를 검사한다.</summary>
    public sealed class HumanoidGradeVariationTests
    {
        private const string Root = "Assets/10.Datas/Ability/Actor/GradeVariation";
        private static readonly int[] Levels = { 1, 9, 10, 19, 20, 29, 30, 40, 50 };
        private static readonly AbilityAIRole[] Roles = { AbilityAIRole.Opener, AbilityAIRole.Punish, AbilityAIRole.Counter, AbilityAIRole.GapCloser, AbilityAIRole.Finisher };
        private static AbilityAttackInfo Info(GameplayAbilitySO ability) => (ability.variants[0].executionPayload as UPlayGroundMotionAbilityPayloadSO)?.attackInfo;
        private static AbilitySetSO[] LoadSets() => AssetDatabase.FindAssets("t:AbilitySetSO", new[] { Root }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<AbilitySetSO>).ToArray();

        [Test]
        public void 등급별_필수역할은_모든_레벨에서_실제_도달거리와_선택후보를_가진다()
        {
            var sets = LoadSets();
            Assert.That(sets.Length, Is.GreaterThanOrEqualTo(3));
            var failures = new List<string>();
            foreach (var set in sets)
            foreach (int level in Levels)
            foreach (var role in set.name.EndsWith("Normal") ? Roles : Roles.Append(AbilityAIRole.Signature))
            {
                var attacks = set.EnumerateAll().Select(a => (ability: a, info: Info(a))).Where(x => x.info != null && x.info.aiSelectable && x.info.IsUnlockedForLevel(level) && (x.info.aiRoles & role) != 0).ToArray();
                bool isBow = set.name.StartsWith("AbilitySet_Bow_");
                var category = role == AbilityAIRole.Opener || role == AbilityAIRole.Finisher ? AbilityAttackCategory.Basic : role == AbilityAIRole.Punish && !isBow ? AbilityAttackCategory.Heavy : AbilityAttackCategory.Skill;
                bool available = false;
                for (float distance = 0.05f; distance <= 25f && !available; distance += 0.05f)
                    available = EnemyAttackRangePolicy.HasAttackInRange(set, distance, level, category, useMeleeApproachRange: true, personalSpaceDistance: isBow ? 2f : 0.75f, abilityRole: role);
                if (attacks.Length == 0 || !available) failures.Add($"{set.name}/Lv{level}/{role}: 유효 거리 후보 없음");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void 대검의_누적_해금수와_일반_전용기_격리가_유지된다()
        {
            foreach (var grade in new[] { "Normal", "Elite", "Boss" })
            {
                var set = AssetDatabase.LoadAssetAtPath<AbilitySetSO>($"{Root}/GreatSword/AbilitySet_GreatSword_{grade}.asset");
                Assert.That(set, Is.Not.Null);
                foreach (int level in Levels)
                {
                    int expected = 7 + (grade == "Normal" ? 0 : 1) + (level >= 10 ? 1 : 0) + (level >= 20 ? 1 : 0) + (level >= 30 ? 1 : 0) + (grade == "Boss" && level >= 30 ? 1 : 0);
                    Assert.That(set.EnumerateAll().Select(Info).Count(i => i != null && i.aiSelectable && i.IsUnlockedForLevel(level)), Is.EqualTo(expected), $"{grade}/Lv{level}");
                }
            }
            foreach (var normal in LoadSets().Where(s => s.name.EndsWith("Normal")))
                Assert.That(normal.EnumerateAll().Select(Info).Where(i => i != null).Any(i => (i.aiRoles & AbilityAIRole.Signature) != 0), Is.False, normal.name);
        }

        [Test]
        public void 후반기는_레벨과_HP를_모두_요구하고_Lv1_대표기는_유지된다()
        {
            var late = LoadSets().Where(s => s.name.EndsWith("Boss")).SelectMany(s => s.EnumerateAll()).Distinct().Select(Info).Where(i => i != null && i.requiredLevel == 30 && i.conditionGroup.conditions.Any(c => c.type == ConditionType.SelfHealthBased)).ToArray();
            Assert.That(late, Is.Not.Empty);
            foreach (var info in late)
            foreach (int level in new[] { 29, 30, 50 })
            foreach (float health in new[] { 49f, 50f, 51f })
            {
                bool available = info.IsUnlockedForLevel(level) && info.CheckCondition(new SkillConditionContext { CurrentLevel = level, CurrentHealth = health, MaxHealth = 100f, HasTarget = true, DistanceToTarget = 1f });
                Assert.That(available, Is.EqualTo(level >= 30 && health <= 50f), $"{info.motionKey}/Lv{level}/HP{health}");
            }
        }

        [Test]
        public void 궁수는_Lv1부터_근접_반격과_8m_사격을_유지한다()
        {
            foreach (var set in LoadSets().Where(s => s.name.StartsWith("AbilitySet_Bow_")))
            foreach (int level in Levels)
            {
                Assert.That(EnemyAttackRangePolicy.HasAttackInRange(set, 8f, level, useMeleeApproachRange: true, personalSpaceDistance: 2f), Is.True, set.name);
                Assert.That(EnemyAttackRangePolicy.HasAttackInRange(set, 0.5f, level, AbilityAttackCategory.Skill, useMeleeApproachRange: true, personalSpaceDistance: 2f, abilityRole: AbilityAIRole.Counter), Is.True, set.name);
            }
        }

        [Test]
        public void 실제_소유자의_Profile과_Definition은_동일한_등급_Set을_사용한다()
        {
            var definitions = AssetDatabase.FindAssets("t:ActorDefinitionSO").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>).Where(d => d.EffectiveAbilitySet != null && AssetDatabase.GetAssetPath(d.EffectiveAbilitySet).StartsWith(Root)).ToArray();
            Assert.That(definitions.Length, Is.GreaterThanOrEqualTo(10));
            foreach (var definition in definitions)
            {
                string suffix = definition.EffectiveGrade == MonsterActorGrade.Weak ? "Normal" : definition.EffectiveGrade.ToString();
                Assert.That(definition.EffectiveAbilitySet.name.EndsWith(suffix), Is.True, definition.actorId);
                Assert.That(definition.monsterProfile.abilitySet, Is.SameAs(definition.EffectiveAbilitySet), definition.actorId);
            }
        }

        [Test]
        public void 신규_Fork는_피해_메타데이터가_일치하고_공유_Payload와_분리된다()
        {
            foreach (var ability in LoadSets().SelectMany(s => s.EnumerateAll()).Distinct().Where(a => AssetDatabase.GetAssetPath(a).StartsWith(Root)))
            {
                var info = Info(ability);
                Assert.That(info, Is.Not.Null, ability.name);
                Assert.That(ability.balance.expectedDamage, Is.EqualTo(info.baseInfo.hitPhases.Sum(h => h.damage)).Within(0.001f), ability.name);
                Assert.That(AssetDatabase.GetAssetPath(ability.variants[0].executionPayload).StartsWith(Root), Is.True, ability.name);
            }
        }

        [Test]
        public void 등급_Set은_BT_요청_라우터를_상속한다()
        {
            foreach (var set in LoadSets())
            foreach (var category in new[] { AbilityAttackCategory.Basic, AbilityAttackCategory.Heavy, AbilityAttackCategory.Skill })
                Assert.That(global::UPlayGround.AI.BehaviorTree.EnemyAbilityTriggerTags.TryResolveAttackTrigger(set, category, out _, out _, out _), Is.True, $"{set.name}/{category}");
        }

        [Test]
        public void 추가_강공격은_기존_응징과_쿨다운을_공유한다()
        {
            foreach (var set in LoadSets().Where(s => s.name.EndsWith("Normal")))
            {
                var attacks = set.EnumerateAll().Where(a => Info(a)?.aiSelectable == true).ToArray();
                foreach (var extra in attacks.Where(a => Info(a).requiredLevel == 30 && Info(a).attackCategory == AbilityAttackCategory.Heavy))
                {
                    var original = attacks.First(a => Info(a).requiredLevel == 1 && (Info(a).aiRoles & AbilityAIRole.Punish) != 0);
                    Assert.That(extra.cooldown.cooldownGroupId, Is.EqualTo(original.cooldown.cooldownGroupId).And.Not.Empty, extra.name);
                    Assert.That(extra.cooldown.durationSeconds, Is.GreaterThanOrEqualTo(original.cooldown.durationSeconds), extra.name);
                }
            }
        }

        [TestCase("Katana", "16_HeavyAttack_6", 17f, 1)]
        [TestCase("Whip", "09_HeavyAttack_3", 28f, 5)]
        public void 후속_강공격은_Lv30에서만_추가되고_상위등급에_상속된다(string weapon, string suffix, float damage, int phases)
        {
            foreach (string grade in new[] { "Normal", "Elite", "Boss" })
            {
                var set = AssetDatabase.LoadAssetAtPath<AbilitySetSO>($"{Root}/{weapon}/AbilitySet_{weapon}_{grade}.asset");
                var attack = set.EnumerateAll().Single(a => a.name.EndsWith(suffix));
                var info = Info(attack);
                Assert.That(info.IsUnlockedForLevel(29), Is.False, attack.name);
                Assert.That(info.IsUnlockedForLevel(30), Is.True, attack.name);
                Assert.That(info.aiRoles, Is.EqualTo(AbilityAIRole.Punish));
                Assert.That(info.attackCategory, Is.EqualTo(AbilityAttackCategory.Heavy));
                Assert.That(info.baseInfo.hitPhases.Count, Is.EqualTo(phases));
                Assert.That(info.baseInfo.hitPhases.Sum(h => h.damage), Is.EqualTo(damage));
                Assert.That(info.baseInfo.hitPhases.All(h => h.projectileDefinition == null), Is.True);
            }
        }

        [Test]
        public void 쌍검_후반기는_일반정예와_분리되고_기본대표기와_다른_모션을_사용한다()
        {
            var sets = new[] { "Normal", "Elite", "Boss" }.Select(g => AssetDatabase.LoadAssetAtPath<AbilitySetSO>($"{Root}/DualBlade/AbilitySet_DualBlade_{g}.asset")).ToArray();
            var late = sets[2].EnumerateAll().Single(a => a.name.EndsWith("12_HeavyAttack_2"));
            Assert.That(sets.Take(2).Any(s => s.EnumerateAll().Contains(late)), Is.False);
            Assert.That(Info(late).requiredLevel, Is.EqualTo(30));
            Assert.That(Info(late).conditionGroup.conditionOperator, Is.EqualTo(ConditionOperator.And));
            Assert.That(Info(late).conditionGroup.conditions.Any(c => c.type == ConditionType.SelfHealthBased && c.maxHealthPercent == 0.5f), Is.True);
            Assert.That(Info(late).baseInfo.hitPhases.Select(h => h.damage), Is.EqualTo(new[] { 8f, 9f }));
            Assert.That(sets[2].EnumerateAll().Select(Info).Where(i => i?.requiredLevel == 1 && (i.aiRoles & AbilityAIRole.Signature) != 0).All(i => i.motionKey != Info(late).motionKey), Is.True);
            Assert.That(late.cooldown.durationSeconds, Is.EqualTo(14f));
        }

        [TestCase("MonsterSera", "GreatSword")]
        [TestCase("MonsterSeolA", "Bow")]
        public void 보스는_자기_무기_모션과_실제_부착형_판정을_사용한다(string file, string weapon)
        {
            var d = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>($"Assets/10.Datas/Actor/DataBase/Humanoid/{file}.asset");
            var expected = AssetDatabase.LoadAssetAtPath<AbilitySetSO>($"{Root}/{weapon}/AbilitySet_{weapon}_Boss.asset");
            Assert.That(d.EffectiveAbilitySet, Is.SameAs(expected));
            Assert.That(d.monsterProfile.abilitySet, Is.SameAs(expected));
            var animator = d.prefab.GetComponentInChildren<global::UPlayGround.Animation.ActorAnimator>(true);
            var expectedMotionSet = AssetDatabase.LoadAssetAtPath<global::UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>($"Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/Humanoid_{weapon}AnimationSet.asset");
            Assert.That(animator.MotionSet, Is.SameAs(expectedMotionSet));
            var groups = d.prefab.GetComponentsInChildren<global::UPlayGround.Combat.CombatHitbox>(true)
                .Where(h => h.IsSupported && h.gameObject.activeSelf).Select(h => h.GroupId).ToHashSet();
            Assert.That(groups.Contains("MainWeapon"), Is.True, d.actorId);
            foreach (var ability in expected.EnumerateAll().Where(a => Info(a)?.aiSelectable == true))
            {
                var info = Info(ability);
                var motion = animator.MotionSet.GetAbilityMotionAsset(info.motionKey);
                Assert.That(motion, Is.Not.Null, ability.name);
                foreach (var hit in motion.motionSet.globalEvents.Concat(motion.motionSet.motions.SelectMany(m => m.events)).OfType<global::UPlayGround.Data.Event.BeginCollisionEvent>())
                {
                    Assert.That(hit.hitPhaseIndex, Is.InRange(0, info.baseInfo.hitPhases.Count - 1), ability.name);
                    if (hit.collisionSource == global::UPlayGround.Combat.CollisionSourceType.AttachedHitboxGroup)
                        Assert.That(groups.Contains(hit.hitboxGroupId), Is.True, ability.name + "/" + hit.hitboxGroupId);
                }
            }
        }

        [Test]
        public void 별도_스토리_분신보스의_고유_Set은_Profile_교체로_가려지지_않는다()
        {
            var story = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>("Assets/10.Datas/Actor/DataBase/Boss/MonsterBossAlternateSelfNenmir.asset");
            var humanoid = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>("Assets/10.Datas/Actor/DataBase/Humanoid/MonsterSeolA.asset");
            Assert.That(story.actorId, Is.EqualTo("BossAlternateSelfNenmir"));
            Assert.That(humanoid.actorId, Is.EqualTo("MonsterNenmir"));
            Assert.That(story.monsterProfile, Is.Not.SameAs(humanoid.monsterProfile));
            Assert.That(story.EffectiveAbilitySet, Is.SameAs(story.abilitySet));
            Assert.That(story.EffectiveAbilitySet, Is.Not.SameAs(humanoid.EffectiveAbilitySet));
        }
    }
}
