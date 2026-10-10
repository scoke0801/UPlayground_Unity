using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Party;
using UPlayGround.Data.Projectile;

namespace UPlayGround.Ability.Tests
{
    /// <summary>Staff 실사용 연결과 즉발 레이저의 벽 차단·단일 피해·풀 재사용을 검증한다.</summary>
    public sealed class StaffMagicIntegrationTests
    {
        private const string Root = "Assets/10.Datas/Ability/Player/Staff/";
        private readonly List<UnityEngine.Object> _objects = new();
        private readonly Vector3 _origin = new(10000, 10000, 10000);

        [TearDown]
        public void Cleanup()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i] != null) UnityEngine.Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }

        [TestCase("Myomyo")]
        [TestCase("YeonHoa")]
        public void Staff캐릭터의_실제Set과_모든공격_모션이_연결된다(string characterName)
        {
            var character = AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinitionSO>(
                "Assets/10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_" + characterName + ".asset");
            var set = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(Root + "AbilitySet_PlayerStaff.asset");
            Assert.That(set, Is.Not.Null);
            Assert.That(character.abilitySet, Is.SameAs(set));
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_" + characterName + ".prefab");
            Assert.That(model.GetComponentInChildren<CharacterModelData>(true).abilitySet, Is.SameAs(set));
            var animator = model.GetComponentInChildren<Animator>(true);
            Assert.That(animator.GetBoneTransform(HumanBodyBones.RightHand).Find("StaffMagicMuzzle"), Is.Not.Null,
                "발사 소켓은 실제 오른손 본을 따라야 합니다.");
            var mapping = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(
                "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Humanoid_Player_StaffAnimationSet.asset");
            Assert.That(mapping.attackAbilitySet, Is.SameAs(set));
            Assert.That(mapping.attackWeaponType, Is.EqualTo(WeaponType.Staff));
            Assert.That(set.GetCombatSequence(PlayerCombatAbilitySlot.LightCombo).Count, Is.EqualTo(10));
            Assert.That(set.GetCombatSequence(PlayerCombatAbilitySlot.JumpCombo).Count, Is.EqualTo(3));
            var abilities = set.EnumerateAll().Where(a => a.abilityId.StartsWith("Player.Staff.")).ToArray();
            Assert.That(abilities.Length, Is.EqualTo(20));
            foreach (var ability in abilities)
            {
                var payload = ability.variants[0].executionPayload as UPlayGroundMotionAbilityPayloadSO;
                Assert.That(payload, Is.Not.Null, ability.name);
                var motion = mapping.GetAbilityMotionAsset(payload.attackInfo.motionKey);
                Assert.That(motion, Is.Not.Null, ability.name);
                var events = motion.motionSet.motions.SelectMany(m => m.events).ToArray();
                Assert.That(events.Any(e => e is CancelWindowEvent), Is.True, motion.name);
                Assert.That(events.Any(e => e is BeginCollisionEvent || e is SpawnProjectileEvent), Is.True, motion.name);
                foreach (var particle in events.OfType<BeginParticleEvent>())
                    Assert.That(particle.particlePrefab, Is.Not.Null, motion.name);
                foreach (var shot in events.OfType<SpawnProjectileEvent>())
                {
                    Assert.That(shot.hitPhaseIndex, Is.InRange(0, payload.attackInfo.baseInfo.hitPhases.Count - 1));
                    Assert.That(payload.attackInfo.baseInfo.hitPhases[shot.hitPhaseIndex].projectileDefinition, Is.Not.Null);
                    Assert.That(shot.projectilePrefab, Is.Null);
                }
            }
        }

        [Test]
        public void 차지공격의_잔류페이즈에서_투사체정의와_배율을_보존한다()
        {
            var owner = new GameObject("ChargeSnapshotTest");
            owner.SetActive(false);
            _objects.Add(owner);
            var combat = owner.AddComponent<PlayerCombat>();
            var definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            _objects.Add(definition);
            var phases = new List<HitPhaseData>
            {
                new() { damage = 100, projectileDefinition = definition }
            };
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(PlayerCombat).GetField("_currentResidualHitPhases", flags).SetValue(combat, phases);
            typeof(PlayerCombat).GetField("_currentAttackData", flags).SetValue(combat,
                new AttackData { damage = 150, damageMultiplier = 1.5f });
            Assert.That(combat.GetProjectileDefinition(0), Is.SameAs(definition));
            var snapshot = combat.CreateProjectileAttackData(0);
            Assert.That(snapshot, Is.Not.Null);
            Assert.That(snapshot.damageMultiplier, Is.EqualTo(1.5f));
            Assert.That(snapshot.damage, Is.InRange(120f, 180f));
        }

        [Test]
        public void 레이저는_벽에서_멈추고_뒤의적에게_피해를주지않는다()
        {
            var runtime = CreateRuntime(out var definition, out var line);
            CreateBox(3, 0);
            var target = CreateBox(6, 29).AddComponent<StaffMagicDamageTarget>();
            Fire(runtime, definition);
            runtime.Tick(0.016f);
            Assert.That(target.HitCount, Is.Zero);
            Assert.That(line.GetPosition(1).z - _origin.z, Is.InRange(2.3f, 3f));
            Assert.That(runtime.IsActive, Is.True);
        }

        [Test]
        public void 레이저는_표시중_한번만_타격하고_시간후_반환된다()
        {
            var runtime = CreateRuntime(out var definition, out var line);
            var target = CreateBox(4, 29).AddComponent<StaffMagicDamageTarget>();
            Fire(runtime, definition);
            runtime.Tick(0.016f);
            runtime.Tick(0.05f);
            Assert.That(target.HitCount, Is.EqualTo(1));
            Assert.That(target.LastDamage, Is.EqualTo(42));
            Assert.That(line.GetPosition(0), Is.EqualTo(_origin));
            Assert.That(runtime.transform.position, Is.EqualTo(_origin));
            Assert.That(line.GetPosition(1).z - _origin.z, Is.InRange(3.3f, 4f));
            runtime.Tick(0.25f);
            Assert.That(runtime.IsReset, Is.True);
        }

        [Test]
        public void 레이저를_재사용하면_이전끝점과_명중대상기록이_초기화된다()
        {
            var runtime = CreateRuntime(out var definition, out var line);
            var target = CreateBox(4, 29).AddComponent<StaffMagicDamageTarget>();
            Fire(runtime, definition);
            runtime.Tick(0.016f);
            runtime.Tick(0.25f);
            Fire(runtime, definition);
            Assert.That(line.GetPosition(1), Is.EqualTo(_origin));
            runtime.Tick(0.016f);
            Assert.That(target.HitCount, Is.EqualTo(2));
        }

        private ProjectileRuntime CreateRuntime(out ProjectileDefinitionSO definition, out LineRenderer line)
        {
            var visual = new GameObject("StaffBeamTest");
            _objects.Add(visual);
            line = visual.AddComponent<LineRenderer>();
            var beam = visual.AddComponent<ProjectileBeamVisual>();
            var serialized = new SerializedObject(beam);
            var lines = serialized.FindProperty("_lines");
            lines.arraySize = 1;
            lines.GetArrayElementAtIndex(0).objectReferenceValue = line;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            _objects.Add(definition);
            definition.motion = new HitscanProjectileMotion { range = 10, visualDuration = 0.2f };
            definition.obstructionLayers = 1;
            definition.inheritOwnerTimeScale = false;
            definition.detachTrailOnReturn = false;
            var runtime = visual.AddComponent<ProjectileRuntime>();
            typeof(ProjectileRuntime).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(runtime, null);
            return runtime;
        }

        private void Fire(ProjectileRuntime runtime, ProjectileDefinitionSO definition)
        {
            Physics.SyncTransforms();
            runtime.Initialize(definition, new ProjectileSpawnRequest
            {
                origin = _origin, logicalOrigin = _origin, direction = Vector3.forward,
                definition = definition, hitLayers = 1 << 29
            }, new AttackData { damage = 42, hitParticleName = string.Empty },
                (projectile, expired) => projectile.OnReturnedToPool(), null);
        }

        private GameObject CreateBox(float distance, int layer)
        {
            var target = new GameObject("StaffTarget") { layer = layer };
            _objects.Add(target);
            target.transform.position = _origin + Vector3.forward * distance;
            target.AddComponent<BoxCollider>();
            return target;
        }
    }

    public sealed class StaffMagicDamageTarget : MonoBehaviour, IDamageable
    {
        public int HitCount;
        public float LastDamage;
        public CombatResult ReceiveHit(in HitRequest request)
        {
            HitCount++;
            LastDamage = request.Damage;
            return default;
        }
        public bool IsAlive() => true;
        public bool CanTakeDamage() => true;
        public Transform GetTransform() => transform;
        public void LockOn() { }
        public void UnLockOn() { }
        public float GetHealthPercent() => 1;
        public float GetCurrentHealth() => 100;
        public void ApplyHealingEffect(float healAmount) { }
    }
}
