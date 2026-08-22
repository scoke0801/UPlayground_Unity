using NUnit.Framework;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Combat.Tests
{
    public sealed class CombatTuningPolicyTests
    {
        private CombatDefensePolicySO _policy;

        [SetUp]
        public void SetUp()
        {
            _policy = ScriptableObject.CreateInstance<CombatDefensePolicySO>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_policy != null)
                Object.DestroyImmediate(_policy);
        }

        [Test]
        public void 미설정_오버라이드는_기존값을_보존한다()
        {
            Assert.That(_policy.ResolvePerfectGuardWindow(0.3f), Is.EqualTo(0.3f));
            Assert.That(_policy.ResolvePerfectDodgeWindow(0.25f), Is.EqualTo(0.25f));
            Assert.That(_policy.ResolveMaxGuardCount(3), Is.EqualTo(3));
            Assert.That(_policy.ResolveGuardResetDelay(3f), Is.EqualTo(3f));
            Assert.That(_policy.ResolveAssistParryWindow(0.4f), Is.EqualTo(0.4f));
        }

        [Test]
        public void 설정된_오버라이드만_방어튜닝값을_교체한다()
        {
            _policy.perfectGuardWindowSeconds = 0.18f;
            _policy.perfectDodgeWindowSeconds = 0.12f;
            _policy.maxGuardCount = 5;
            _policy.guardResetDelaySeconds = 1.75f;
            _policy.assistParryWindowSeconds = 0.55f;

            Assert.That(_policy.ResolvePerfectGuardWindow(0.3f), Is.EqualTo(0.18f));
            Assert.That(_policy.ResolvePerfectDodgeWindow(0.25f), Is.EqualTo(0.12f));
            Assert.That(_policy.ResolveMaxGuardCount(3), Is.EqualTo(5));
            Assert.That(_policy.ResolveGuardResetDelay(3f), Is.EqualTo(1.75f));
            Assert.That(_policy.ResolveAssistParryWindow(0.4f), Is.EqualTo(0.55f));
        }

        [Test]
        public void 성공유형에_맞는_피드백프로필을_반환한다()
        {
            var parry = DefenseSuccessFeedbackProfile.CreateDefault(DefenseSuccessType.Parry);
            var guard = DefenseSuccessFeedbackProfile.CreateDefault(DefenseSuccessType.PerfectGuard);
            var dodge = DefenseSuccessFeedbackProfile.CreateDefault(DefenseSuccessType.PerfectDodge);
            _policy.parryFeedback = parry;
            _policy.perfectGuardFeedback = guard;
            _policy.perfectDodgeFeedback = dodge;

            Assert.That(_policy.GetFeedbackProfile(DefenseSuccessType.Parry), Is.SameAs(parry));
            Assert.That(_policy.GetFeedbackProfile(DefenseSuccessType.PerfectGuard), Is.SameAs(guard));
            Assert.That(_policy.GetFeedbackProfile(DefenseSuccessType.PerfectDodge), Is.SameAs(dodge));
        }

        [Test]
        public void 공격출처는_CombatResult입력까지_보존된다()
        {
            var attack = new AttackData
            {
                abilityId = "Boss.Golem.Smash",
                abilityVariantId = "Phase2",
                motionKey = "Golem.Smash",
                attackKind = AttackKind.SkillAttack,
                damage = 42f,
            };

            HitRequest request = HitRequest.FromAttackData(attack);
            HitContext context = HitContext.Create(request, null);

            Assert.That(context.AbilityId, Is.EqualTo("Boss.Golem.Smash"));
            Assert.That(context.AbilityVariantId, Is.EqualTo("Phase2"));
            Assert.That(context.MotionKey, Is.EqualTo("Golem.Smash"));
            Assert.That(context.AttackKind, Is.EqualTo(AttackKind.SkillAttack));
        }

        [Test]
        public void 피니시_MotionEvent는_일반_브레이크공격과_구분되는_요청을_만든다()
        {
            HitRequest request = HitRequest.CreateFinishAttack(
                null,
                null,
                Vector3.forward);

            Assert.That(request.AttackKind, Is.EqualTo(AttackKind.FinishAttack));
            Assert.That(request.IsSpecialBreak, Is.False);
            Assert.That(request.AttackDirection, Is.EqualTo(Vector3.forward));
        }
    }
}
