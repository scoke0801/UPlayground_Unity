using NUnit.Framework;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Components;

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

        [TestCase(DefenseOutcome.Block, false)]
        [TestCase(DefenseOutcome.PerfectGuard, false)]
        [TestCase(DefenseOutcome.GuardBreak, true)]
        public void 가드_세부결과가_최종_방어결과로_보존된다(
            DefenseOutcome guardOutcome,
            bool shouldApplyDamage)
        {
            PlayerDefenseQuery query = CreateDefenseQuery(
                isGuarding: true,
                isGuardState: true,
                guardOutcome: guardOutcome);

            DefenseResult result = ResolveDefense(query, AttackDefenseType.Parryable);

            Assert.That(result.Outcome, Is.EqualTo(guardOutcome));
            Assert.That(result.ShouldApplyDamage, Is.EqualTo(shouldApplyDamage));
        }

        [Test]
        public void 공격중_충돌방어는_공격쳐내기로_구분된다()
        {
            PlayerDefenseQuery query = CreateDefenseQuery(
                isAttackState: true,
                isAttackCollisionActive: true,
                isCurrentAttackParryCapable: true);

            DefenseResult result = ResolveDefense(query, AttackDefenseType.Parryable);

            Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.AttackClash));
            Assert.That(result.ShouldApplyDamage, Is.False);
        }

        [Test]
        public void 퍼펙트도지창의_무적피격은_퍼펙트도지로_확정된다()
        {
            PlayerDefenseQuery query = CreateDefenseQuery(
                isPerfectDodgeState: true,
                isPerfectDodgeWindow: true,
                canTakeDamage: false);

            DefenseResult result = ResolveDefense(query, AttackDefenseType.Parryable);

            Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.PerfectDodge));
            Assert.That(result.ShouldApplyDamage, Is.False);
        }

        [Test]
        public void 일반투사체는_강제쳐내기설정이어도_공격쳐내기가_성립하지않는다()
        {
            PlayerDefenseQuery query = CreateDefenseQuery(alwaysParry: true);

            DefenseResult result = ResolveDefense(
                query,
                AttackDefenseType.Parryable,
                isProjectile: true,
                isReflectableProjectile: false);

            Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.None));
            Assert.That(result.ShouldApplyDamage, Is.True);
        }

        [Test]
        public void 언블로커블은_가드보다_우선해_피해결과가_된다()
        {
            PlayerDefenseQuery query = CreateDefenseQuery(
                isGuarding: true,
                isGuardState: true,
                guardOutcome: DefenseOutcome.PerfectGuard);

            DefenseResult result = ResolveDefense(query, AttackDefenseType.Unblockable);

            Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.UnblockableHit));
            Assert.That(result.ShouldApplyDamage, Is.True);
        }

        [Test]
        public void 가드내구도_미리보기는_상태를_바꾸지않고_브레이크를_예측한다()
        {
            var controller = new PlayerDefenseController(
                owner: null,
                maxGuardCount: 2,
                guardResetDelay: 3f,
                perfectGuardCounterWindow: 1f,
                parryCounterWindow: 1f,
                dodgeCounterWindow: 1f,
                assistParryWindow: 1f,
                perfectGuardWindow: 0.3f,
                perfectDodgeWindow: 0.25f);

            Assert.That(controller.PreviewGuardOutcome(false), Is.EqualTo(DefenseOutcome.Block));
            Assert.That(controller.GuardHitCount, Is.Zero);

            controller.CommitGuardOutcome(DefenseOutcome.Block);

            Assert.That(controller.GuardHitCount, Is.EqualTo(1));
            Assert.That(controller.PreviewGuardOutcome(true), Is.EqualTo(DefenseOutcome.GuardBreak));
            Assert.That(controller.GuardHitCount, Is.EqualTo(1));

            controller.CommitGuardOutcome(DefenseOutcome.GuardBreak);
            controller.ConfirmGuardBreak();

            Assert.That(controller.GuardHitCount, Is.Zero);
            Assert.That(controller.CanGuard(), Is.False);
        }

        private PlayerDefenseQuery CreateDefenseQuery(
            bool isGuarding = false,
            bool isGuardState = false,
            bool isAttackState = false,
            bool isAttackCollisionActive = false,
            bool isCurrentAttackParryCapable = false,
            bool isPerfectDodgeState = false,
            bool isPerfectDodgeWindow = false,
            bool canTakeDamage = true,
            bool alwaysParry = false,
            DefenseOutcome guardOutcome = DefenseOutcome.Block)
        {
            return new PlayerDefenseQuery(
                isGuarding,
                isGuardState,
                isAttackState,
                isAttackCollisionActive,
                isCurrentAttackParryCapable,
                isPerfectDodgeState,
                isPerfectDodgeWindow,
                canTakeDamage,
                alwaysParry,
                guardOutcome: guardOutcome,
                policy: _policy);
        }

        private static DefenseResult ResolveDefense(
            in PlayerDefenseQuery query,
            AttackDefenseType defenseType,
            bool isProjectile = false,
            bool isReflectableProjectile = false)
        {
            var attack = new AttackData
            {
                defenseType = defenseType,
                isProjectile = isProjectile,
                isReflectableProjectile = isReflectableProjectile,
            };
            HitRequest request = HitRequest.FromAttackData(attack);
            HitContext hit = HitContext.Create(request, null);
            return DefenseResolver.ResolvePlayerDefense(query, hit);
        }
    }
}
