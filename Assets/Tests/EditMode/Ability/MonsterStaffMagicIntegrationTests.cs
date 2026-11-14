using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Event;

namespace UPlayGround.Ability.Tests
{
    /// <summary>실제 스태프 몬스터의 GAS·모션·발사 소켓과 몬스터용 피해 스냅샷을 검증한다.</summary>
    public sealed class MonsterStaffMagicIntegrationTests
    {
        private const string Root = "Assets/10.Datas/Ability/Actor/P09Staff/";

        [Test]
        public void 스태프몬스터14종의_프로필과_AI공격이_자기모션에서_해석된다()
        {
            var set = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(Root + "AbilitySet_P09Staff.asset");
            Assert.That(set, Is.Not.Null);
            var definitions = AssetDatabase.FindAssets("t:ActorDefinitionSO",
                    new[] { "Assets/10.Datas/Actor/P09Progression/Generated" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>)
                .Where(d => d.EffectiveAbilitySet == set).ToArray();
            Assert.That(definitions.Length, Is.EqualTo(14));
            foreach (var definition in definitions)
            {
                Assert.That(definition.monsterProfile.abilitySet, Is.SameAs(set), definition.actorId);
                var prefab = definition.prefab;
                Assert.That(prefab.GetComponent<EnemyCombat>().AbilitySet, Is.SameAs(set), definition.actorId);
                var mapping = prefab.GetComponentInChildren<ActorAnimator>(true).MotionSet;
                Assert.That(mapping.attackAbilitySet, Is.SameAs(set));
                var hand = prefab.GetComponentInChildren<Animator>(true).GetBoneTransform(HumanBodyBones.RightHand);
                Assert.That(hand.Find("StaffMagicMuzzle"), Is.Not.Null, definition.actorId);
                int count = 0;
                foreach (var ability in set.EnumerateAll())
                {
                    if (ability.variants[0].executionPayload is not UPlayGroundMotionAbilityPayloadSO payload
                        || !payload.attackInfo.aiSelectable) continue;
                    count++;
                    Assert.That(ability.abilityId, Does.StartWith("Monster.P09Staff."));
                    Assert.That(global::UPlayGround.AI.BehaviorTree.EnemyAbilityTriggerTags.TryResolveAttackTrigger(
                        set, payload.attackInfo.attackCategory, out _, out _, out _), Is.True);
                    var motion = mapping.GetAbilityMotionAsset(payload.attackInfo.motionKey);
                    Assert.That(motion, Is.Not.Null, ability.abilityId);
                    Assert.That(SerializationUtility.HasManagedReferencesWithMissingTypes(motion), Is.False);
                    var events = motion.motionSet.motions.SelectMany(m => m.events)
                        .Concat(motion.motionSet.globalEvents).ToArray();
                    Assert.That(events.Any(e => e is BeginCollisionEvent || e is SpawnProjectileEvent), Is.True);
                    foreach (var shot in events.OfType<SpawnProjectileEvent>())
                    {
                        Assert.That(shot.targetMode, Is.EqualTo(ProjectileTargetMode.EnemySkillTarget));
                        Assert.That(shot.spawnPointName, Is.EqualTo("StaffMagicMuzzle"));
                        Assert.That(shot.hitPhaseIndex, Is.InRange(0, payload.attackInfo.baseInfo.hitPhases.Count - 1));
                        Assert.That(payload.attackInfo.baseInfo.hitPhases[shot.hitPhaseIndex].projectileDefinition, Is.Not.Null);
                        Assert.That(events.Any(e => e is BeginCollisionEvent), Is.False,
                            "원거리 공격에 근접 판정이 중복되면 안 됩니다.");
                    }
                    foreach (var collision in events.OfType<BeginCollisionEvent>())
                        Assert.That(collision.hitPhaseIndex, Is.InRange(0, payload.attackInfo.baseInfo.hitPhases.Count - 1));
                    foreach (var vfx in events.OfType<BeginParticleEvent>())
                        Assert.That(vfx.particlePrefab, Is.Not.Null);
                }
                Assert.That(count, Is.EqualTo(10), definition.actorId);
            }
        }

        [TestCase("Bolt")]
        [TestCase("Beam")]
        [TestCase("Fan")]
        [TestCase("ChargedBeam")]
        [TestCase("Burst")]
        [TestCase("Homing")]
        [TestCase("DoubleBeam")]
        [TestCase("FanBeam")]
        public void 몬스터투사체는_자기Ability의_피해와_발사정의를_사용한다(string key)
        {
            var ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(Root + "GA_P09Staff_" + key + ".asset");
            Assert.That(ability, Is.Not.Null);
            var payload = (UPlayGroundMotionAbilityPayloadSO)ability.variants[0].executionPayload;
            var owner = new GameObject("몬스터 마법 스냅샷 검증");
            owner.SetActive(false);
            try
            {
                var combat = owner.AddComponent<EnemyCombat>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(EnemyCombat).GetField("_currentSkill", flags).SetValue(combat, payload.attackInfo);
                typeof(EnemyCombat).GetField("_currentAbility", flags).SetValue(combat, ability);
                var phase = payload.attackInfo.baseInfo.hitPhases[0];
                Assert.That(combat.GetProjectileDefinition(0), Is.SameAs(phase.projectileDefinition));
                var snapshot = combat.CreateProjectileAttackData(0);
                Assert.That(snapshot, Is.Not.Null);
                Assert.That(snapshot.abilityId, Is.EqualTo(ability.abilityId));
                Assert.That(snapshot.isProjectile, Is.True);
                Assert.That(snapshot.damage, Is.InRange(phase.damage * .8f, phase.damage * 1.2f));
                Assert.That(snapshot.criticalMultiplier, Is.EqualTo(1f));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [TestCase("Bolt")]
        [TestCase("Fan")]
        [TestCase("Beam")]
        [TestCase("ChargedBeam")]
        [TestCase("Burst")]
        [TestCase("Homing")]
        [TestCase("DoubleBeam")]
        [TestCase("FanBeam")]
        public void 몬스터주문은_저작사거리까지_도달하고_벽에_차단된다(string key)
        {
            var ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(Root + "GA_P09Staff_" + key + ".asset");
            var payload = (UPlayGroundMotionAbilityPayloadSO)ability.variants[0].executionPayload;
            var projectile = payload.attackInfo.baseInfo.hitPhases[0].projectileDefinition;
            Assert.That(AssetDatabase.GetAssetPath(projectile), Does.StartWith(Root), "플레이어 공유 정의를 변경하지 않는다.");
            Assert.That(projectile.obstructionLayers.value, Is.Not.Zero, "벽 레이어 누락은 벽 너머 피해를 만든다.");
            if (projectile.motion is global::UPlayGround.Data.Projectile.LinearProjectileMotion linear)
                Assert.That(linear.speed * projectile.lifetime, Is.GreaterThanOrEqualTo(ability.activation.maxDistance));
            if (projectile.motion is global::UPlayGround.Data.Projectile.HitscanProjectileMotion hitscan)
                Assert.That(hitscan.range, Is.GreaterThanOrEqualTo(ability.activation.maxDistance));
            if (projectile.motion is global::UPlayGround.Data.Projectile.HomingProjectileMotion homing)
                Assert.That(homing.speed * projectile.lifetime, Is.GreaterThanOrEqualTo(ability.activation.maxDistance));
        }

        [TestCase("Bolt", 1)]
        [TestCase("Sweep", 1)]
        [TestCase("Fan", 10)]
        [TestCase("Beam", 15)]
        [TestCase("Repulse", 15)]
        [TestCase("Burst", 20)]
        [TestCase("ChargedBeam", 25)]
        [TestCase("Homing", 30)]
        [TestCase("DoubleBeam", 40)]
        [TestCase("FanBeam", 50)]
        public void 해금_직전에는_실제_AI후보에서_잠기고_해금레벨부터_허용된다(string key, int level)
        {
            var ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(Root + "GA_P09Staff_" + key + ".asset");
            var payload = (UPlayGroundMotionAbilityPayloadSO)ability.variants[0].executionPayload;
            Assert.That(payload.attackInfo.requiredLevel, Is.EqualTo(level));
            var context = new global::UPlayGround.Data.Combat.SkillConditionContext
            {
                CurrentLevel = level - 1, HasTarget = true, CurrentHealth = 100, MaxHealth = 100,
                DistanceToTarget = (ability.activation.minDistance + ability.activation.maxDistance) * .5f
            };
            var locked = EnemyAbilitySelectionPolicy.Evaluate(payload.attackInfo, in context,
                payload.attackInfo.attackCategory, aerialOnly: false, diveOnly: false);
            Assert.That(locked.HasFlag(EnemyAbilityRejectReason.LevelLocked), Is.True, key);
            context.CurrentLevel = level;
            var unlocked = EnemyAbilitySelectionPolicy.Evaluate(payload.attackInfo, in context,
                payload.attackInfo.attackCategory, aerialOnly: false, diveOnly: false);
            Assert.That(unlocked, Is.EqualTo(EnemyAbilityRejectReason.None), key);
        }

        [TestCase(10, 3)]
        [TestCase(15, 5)]
        [TestCase(20, 6)]
        [TestCase(25, 7)]
        [TestCase(30, 8)]
        [TestCase(40, 9)]
        [TestCase(50, 10)]
        public void 성장단계별_실제몬스터가_해금목록을_공유한다(int level, int expected)
        {
            var set = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(Root + "AbilitySet_P09Staff.asset");
            var definitions = AssetDatabase.FindAssets("t:ActorDefinitionSO",
                    new[] { "Assets/10.Datas/Actor/P09Progression/Generated" })
                .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>)
                .Where(d => d.EffectiveAbilitySet == set && d.EffectiveLevel == level).ToArray();
            Assert.That(definitions.Length, Is.EqualTo(2), "각 성장 단계의 남녀 몬스터를 모두 확인한다.");
            foreach (var definition in definitions)
            {
                var context = new global::UPlayGround.Data.Combat.SkillConditionContext
                    { CurrentLevel = definition.EffectiveLevel, HasTarget = true };
                int unlocked = set.EnumerateAll().Count(ability =>
                    ability.variants[0].executionPayload is UPlayGroundMotionAbilityPayloadSO payload
                    && payload.attackInfo.aiSelectable
                    && !EnemyAbilitySelectionPolicy.Evaluate(payload.attackInfo, in context,
                        payload.attackInfo.attackCategory, aerialOnly: false, diveOnly: false)
                        .HasFlag(EnemyAbilityRejectReason.LevelLocked));
                Assert.That(unlocked, Is.EqualTo(expected), definition.actorId);
            }
        }
    }
}
