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
        [TestCase(-0.001f, false)]
        [TestCase(0f, true)]
        [TestCase(0.1799f, true)]
        [TestCase(0.18f, true)]
        [TestCase(0.1801f, false)]
        public void 투사체_가드창은_접촉시각을_사용한다(float contactTime, bool expected)
        {
            var defense = new PlayerDefenseController(null, 3, 3f, 1f, 1f, 1f, 1f, 0.3f, 0.25f);
            defense.SetProjectileGuardInput(true);
            defense.BeginProjectileGuard(0f, _policy);
            Assert.That(defense.IsProjectileGuardWindow(contactTime), Is.EqualTo(expected));
        }

        [Test]
        public void 유지_재진입_조기입력은_창을_갱신하지_않는다()
        {
            var defense = new PlayerDefenseController(null, 3, 3f, 1f, 1f, 1f, 1f, 0.3f, 0.25f);
            defense.SetProjectileGuardInput(true);
            defense.BeginProjectileGuard(0f, _policy);
            ulong first = defense.ProjectileGuardActionId;
            Assert.That(defense.ConsumeProjectileReward(first), Is.True);
            Assert.That(defense.ConsumeProjectileReward(first), Is.False);
            defense.BeginProjectileGuard(0.1f, _policy);
            Assert.That(defense.IsProjectileGuardWindow(0.1f), Is.False);
            defense.SetProjectileGuardInput(false);
            defense.SetProjectileGuardInput(true);
            defense.BeginProjectileGuard(0.349f, _policy);
            Assert.That(defense.IsProjectileGuardWindow(0.349f), Is.False);
            defense.BeginProjectileGuard(0.35f, _policy);
            Assert.That(defense.IsProjectileGuardWindow(0.35f), Is.False);
            defense.SetProjectileGuardInput(false);
            defense.SetProjectileGuardInput(true);
            defense.BeginProjectileGuard(0.35f, _policy);
            Assert.That(defense.IsProjectileGuardWindow(0.35f), Is.True);
        }

        [TestCase(0f, true)]
        [TestCase(69.99f, true)]
        [TestCase(70.01f, false)]
        [TestCase(180f, false)]
        public void 투사체_정면은_몸과_입사방향의_수평각이다(float angle, bool expected)
        {
            Vector3 incoming = Quaternion.Euler(0f, angle, 0f) * Vector3.back;
            Assert.That(DefenseResolver.IsProjectileInFront(Vector3.forward, incoming, 140f), Is.EqualTo(expected));
            Assert.That(DefenseResolver.IsProjectileInFront(Vector3.forward, Vector3.down, 140f), Is.False);
        }

        private CombatDefensePolicySO _policy;

        [Test]
        public void 반사피해는_발사공격력을_고정하고_현재_대상방어를_사용한다()
        {
            var sourceObject = new GameObject("발사 수치 검사"); sourceObject.SetActive(false);
            var reflectorObject = new GameObject("반사 수치 검사"); reflectorObject.SetActive(false);
            var targetObject = new GameObject("현재 방어 검사"); targetObject.SetActive(false);
            try
            {
                var source = sourceObject.AddComponent<MonsterActor>();
                var reflector = reflectorObject.AddComponent<PlayerActor>();
                var target = targetObject.AddComponent<MonsterActor>();
                foreach (var actor in new GameActor[] { source, reflector, target })
                {
                    var ability = actor.gameObject.AddComponent<UPlayGround.Gameplay.Ability.AbilitySystemComponent>();
                    ability.EnsureInitialized();
                    typeof(GameActor).GetProperty(nameof(GameActor.AbilitySystem)).SetValue(actor, ability);
                    ability.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, 3f);
                }
                var attack = new AttackData { attacker = source, damage = 10f, criticalMultiplier = 2f };
                var snapshot = new ProjectileDamageSource(attack).ReflectedBy(reflector);
                attack.attacker = reflector; attack.projectileDamageSource = snapshot;
                source.AbilitySystem.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, 99f);
                reflector.AbilitySystem.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower, 999f);
                target.AbilitySystem.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Combat.Defense, 0.5f);
                UnityEngine.Object.DestroyImmediate(sourceObject);
                HitRequest request = HitRequest.FromAttackData(attack);
                DamageResult result = DamageResolver.ResolveMonsterDamage(target, HitContext.Create(request, target), null);
                Assert.That(result.AttackerPower, Is.EqualTo(3f));
                Assert.That(result.FinalDamage, Is.EqualTo(30f).Within(0.001f));
                target.AbilitySystem.Attributes.SetBase(global::UPlayGround.Data.Stat.Attributes.Combat.Defense, 0f);
                result = DamageResolver.ResolveMonsterDamage(target, HitContext.Create(request, target), null);
                Assert.That(result.FinalDamage, Is.EqualTo(60f).Within(0.001f));
                UnityEngine.Object.DestroyImmediate(reflectorObject);
                var ledger = new CombatContributionLedger(); ledger.Record(request, result.FinalDamage);
                Assert.That(ledger.CreateKillContext(target).TotalRecordedDamage, Is.EqualTo(60f));
            }
            finally
            {
                if (sourceObject != null) UnityEngine.Object.DestroyImmediate(sourceObject);
                if (reflectorObject != null) UnityEngine.Object.DestroyImmediate(reflectorObject);
                UnityEngine.Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void 투사체_세경로와_방어타입은_배타적이고_마지막내구도도_소모하지않는다()
        {
            var go = new GameObject("투사체 방어 후보");
            try
            {
                var runtime = go.AddComponent<ProjectileRuntime>();
                for (int source = 0; source < 3; source++)
                for (int type = 0; type < 3; type++)
                for (int eligible = 0; eligible < 2; eligible++)
                {
                    var attack = new AttackData
                    {
                        isProjectile = true, isReflectableProjectile = eligible == 1,
                        defenseType = (AttackDefenseType)type, attackDirection = Vector3.back,
                        projectileContact = new ProjectileContact(runtime, 1, 1, 1, 0f, Vector3.zero),
                    };
                    var query = new PlayerDefenseQuery(source == 0, source == 0, true, true, true,
                        false, false, true, true, source == 1, DefenseOutcome.GuardBreak, _policy,
                        true, false, Vector3.forward, 7, 8, 9);
                    DefenseResult result = DefenseResolver.ResolvePlayerDefense(query,
                        HitContext.Create(HitRequest.FromAttackData(attack), null));
                    bool success = type == 0 && eligible == 1;
                    Assert.That(result.ProjectileSource != ProjectileDefenseSource.None, Is.EqualTo(success));
                    if (success)
                    {
                        Assert.That(result.ShouldApplyDamage, Is.False);
                        Assert.That(result.Outcome, Is.EqualTo(source == 0 ? DefenseOutcome.PerfectGuard : DefenseOutcome.AttackClash));
                        Assert.That(result.ProjectileSource, Is.EqualTo(source == 0 ? ProjectileDefenseSource.Guard
                            : source == 1 ? ProjectileDefenseSource.Assist : ProjectileDefenseSource.Attack));
                        Assert.That(result.DefenseActionId, Is.EqualTo(source == 0 ? 7ul : source == 1 ? 9ul : 8ul));
                    }
                    else if (source == 0 && type != 2) Assert.That(result.Outcome, Is.EqualTo(DefenseOutcome.GuardBreak));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

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
