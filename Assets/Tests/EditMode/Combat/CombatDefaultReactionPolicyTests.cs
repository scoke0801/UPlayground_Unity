using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Actor;
using UnityEditor;

namespace UPlayGround.Combat.Tests
{
    public sealed class CombatDefaultReactionPolicyTests
    {
        private CombatReactionPolicySO _fallback;
        private CombatReactionPolicySO _explicit;

        [SetUp]
        public void SetUp()
        {
            _fallback = ScriptableObject.CreateInstance<CombatReactionPolicySO>();
            _explicit = ScriptableObject.CreateInstance<CombatReactionPolicySO>();
            _fallback.monsterGradeRules.Add(new CombatReactionPolicySO.GradeRule
            {
                grade = MonsterActorGrade.Boss, requirePoiseBreakForState = true,
                allowForceReaction = false, allowHit = false,
            });
        }

        [TearDown]
        public void TearDown()
        {
            CombatPolicyResolver.SetDefaultPolicy(null);
            UnityEngine.Object.DestroyImmediate(_explicit);
            UnityEngine.Object.DestroyImmediate(_fallback);
        }

        [Test]
        public void 정책이_없으면_기본_등급규칙을_쓴다()
        {
            CombatPolicyResolver.SetDefaultPolicy(_fallback);
            Assert.That(CombatPolicyResolver.RequiresPoiseBreakForMonsterState(null, MonsterActorGrade.Boss), Is.True);
            Assert.That(CombatPolicyResolver.AllowsMonsterForceReaction(null, MonsterActorGrade.Boss), Is.False);
            Assert.That(CombatPolicyResolver.AllowsMonsterReactionState(null, MonsterActorGrade.Boss, CombatReactionState.Hit), Is.False);
        }

        [Test]
        public void 다른_등급의_정책만_있어도_기본값을_쓴다()
        {
            _explicit.monsterGradeRules.Add(new CombatReactionPolicySO.GradeRule { grade = MonsterActorGrade.Elite });
            Assert.That(CombatPolicyResolver.ResolveRule(_explicit, MonsterActorGrade.Boss, _fallback),
                Is.SameAs(_fallback.GetRule(MonsterActorGrade.Boss)));
        }

        [Test]
        public void 명시한_규칙은_기본값보다_우선한다()
        {
            var rule = new CombatReactionPolicySO.GradeRule { grade = MonsterActorGrade.Boss };
            _explicit.monsterGradeRules.Add(rule);
            Assert.That(CombatPolicyResolver.ResolveRule(_explicit, MonsterActorGrade.Boss, _fallback), Is.SameAs(rule));
        }

        [Test]
        public void 모든_몬스터_정의에_유효한_등급규칙이_연결된다()
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if ((actor.actorType & ActorType.Monster) == 0) continue;
                count++;
                Assert.That(actor.EffectiveCombatReactionPolicy, Is.Not.Null, actor.actorId);
                Assert.That(actor.EffectiveCombatReactionPolicy.GetRule(actor.EffectiveGrade), Is.Not.Null, actor.actorId);
            }
            Assert.That(count, Is.GreaterThan(0), "검사할 몬스터 정의가 없습니다.");
        }

        [Test]
        public void 프로젝트_기본정책은_모든_등급을_포함한다()
        {
            var policy = Resources.Load<CombatReactionPolicySO>(CombatPolicyResolver.DefaultPolicyResourceKey);
            Assert.That(policy, Is.Not.Null);
            foreach (MonsterActorGrade grade in System.Enum.GetValues(typeof(MonsterActorGrade)))
                Assert.That(policy.GetRule(grade), Is.Not.Null, grade.ToString());
        }
    }
}
