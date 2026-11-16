#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Event;
using UPlayGround.Data.Party;
using UPlayGround.Data.Projectile;
using UPlayGround.Data.Stat;
using UPlayGround.Gameplay.Tag;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.Ability.Tests
{
    /// <summary>실제 플레이어 정의·모델의 궁극기 연결과 공유 원본 보존을 검사한다.</summary>
    public sealed class SignatureAbilityDataTests
    {
        private const string AbilityRoot = "Assets/10.Datas/Ability/Player/Signature/";
        private const string MotionRoot = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Signature/";
        private const string RedesignedMotionRoot = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/UltimateRedesign/";
        private const string DefaultSetPath = "Assets/10.Datas/Ability/Migrated/PlayerDefaultAttackData/AbilitySet_PlayerDefaultAttackData.asset";
        private const string DefaultAbilityPath = "Assets/10.Datas/Ability/Migrated/PlayerDefaultAttackData/GA_PlayerDefaultAttackData_Ultimate.asset";
        private static readonly string[] Characters =
            { "Raon", "Arin", "Hwarin", "Reine", "Lian", "SeolA", "Sera", "Lili", "YeonHoa", "Myomyo", "Yura", "MyoRyeong" };
        private static readonly string[] Weapons =
            { "Katana", "Bow", "Spear", "DoubleAxe", "DualBlade", "Staff", "GreatSword", "Whip", "SwordShield" };

        [Test]
        public void 정규12명은_자기정의와_실제모델의_일반형궁극기와_컷인으로_연결된다()
        {
            foreach (string name in Characters)
            {
                PlayerCharacterDefinitionSO definition = Definition(name);
                var model = Load<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_" + name + ".prefab");
                Assert.That(model.GetComponent<CharacterModelData>()?.Definition, Is.SameAs(definition), name);
                var animator = model.GetComponentInChildren<PlayerActorAnimator>(true);
                Assert.That(animator, Is.Not.Null, name);
                var ability = definition.abilitySet.GetPlayerAbility(PlayerSkillSlot.Ultimate);
                Assert.That(ability, Is.Not.Null, name);
                var payload = ability.variants.Single().executionPayload;
                Assert.That(payload, Is.TypeOf<UPlayGroundMotionAbilityPayloadSO>(), name);
                var attack = ((UPlayGroundMotionAbilityPayloadSO)payload).attackInfo;
                MotionSetAsset motion = animator.PlayerMotionSet.GetAbilityMotionAsset(definition.defaultWeaponType, attack.motionKey);
                Assert.That(motion, Is.Not.Null, name);

                Assert.That(AssetDatabase.GetAssetPath(motion), Is.EqualTo(RedesignedMotionRoot + name + "_Ultimate.asset"), name);
                Assert.That(motion.motionSet.globalEvents.OfType<UltimateFaceStripMotionEvent>().Count(), Is.EqualTo(1), name);
                Assert.That(definition.ultimateFaceSprite, Is.Not.Null, name + " 얼굴 Sprite 누락");
            }
        }

        [Test]
        public void 같은무기의_공유궁극기는_Ability를공유하고_모션과컷인은_각자의것을쓴다()
        {
            foreach (string[] pair in new[] { new[] { "Sera", "Lili" }, new[] { "YeonHoa", "Myomyo" } })
            {
                PlayerCharacterDefinitionSO first = Definition(pair[0]), second = Definition(pair[1]);
                Assert.That(first.abilitySet.GetPlayerAbility(PlayerSkillSlot.Ultimate),
                    Is.SameAs(second.abilitySet.GetPlayerAbility(PlayerSkillSlot.Ultimate)), string.Join("/", pair));
                Assert.That(LiveMotion(pair[0]), Is.Not.SameAs(LiveMotion(pair[1])));
                Assert.That(first.ultimateFaceSprite, Is.Not.Null);
                Assert.That(second.ultimateFaceSprite, Is.Not.Null);
                Assert.That(second.ultimateFaceSprite, Is.Not.SameAs(first.ultimateFaceSprite));
            }
        }

        [Test]
        public void 기존궁극기9개의_GUID와_새모션11개의_독립성을_보존한다()
        {
            foreach (string weapon in Weapons)
            {
                string path = weapon == "Spear" ? DefaultAbilityPath : SourceAbilityPath(weapon);
                AssertBaselineHash(path + ".meta");
            }
            MotionSetAsset[] motions = AssetDatabase.FindAssets("t:MotionSetAsset", new[] { MotionRoot.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath).Select(Load<MotionSetAsset>).ToArray();
            Assert.That(motions, Has.Length.EqualTo(11));
            Assert.That(motions.Select(AssetDatabase.GetAssetPath).Select(AssetDatabase.AssetPathToGUID).Distinct().Count(), Is.EqualTo(11));
            foreach (MotionSetAsset motion in motions)
                Assert.That(motion.motionSet.motions.All(m => m.motionClip != null && m.Duration > 0f), Is.True, motion.name);
        }

        [Test]
        public void 창_분리는_공유Default와_지팡이기반과_공통모션을_변경하지않는다()
        {
            var original = Load<AbilitySetSO>(DefaultSetPath);
            var derived = Load<AbilitySetSO>(AbilityRoot + "AbilitySet_PlayerSpear.asset");
            Assert.That(derived.baseSet, Is.SameAs(original));
            Assert.That(Definition("Reine").abilitySet, Is.SameAs(derived));
            Assert.That(derived.GetPlayerAbility(PlayerSkillSlot.Ultimate), Is.Not.SameAs(original.GetPlayerAbility(PlayerSkillSlot.Ultimate)));
            Assert.That(Load<AbilitySetSO>("Assets/10.Datas/Ability/Player/Staff/AbilitySet_PlayerStaff.asset").baseSet, Is.SameAs(original));
            const string commonPath = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Humanoid_Player_CommonAnimationSet.asset";
            var common = Load<ActorAnimationMotionSet>(commonPath);
            Assert.That(common.attackAbilitySet, Is.SameAs(original));
            Assert.That(AssetDatabase.GetAssetPath(common.GetAbilityMotionAsset(new MotionKey("Default.Ultimate"))),
                Is.EqualTo("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Hwarin/DoubleAxe/DoubleAxe_Skill_WhirlWind.asset"));
            AssertBaselineHash(DefaultSetPath);
            AssertBaselineHash(DefaultAbilityPath);
            AssertBaselineHash(commonPath);
        }

        [Test]
        public void 창_착탄은_비어있던Default대신_대검기준의_한번짜리_피해를쓴다()
        {
            var original = Load<UPlayGroundMotionAbilityPayloadSO>(AbilityRoot + "SourcePayload_Spear.asset");
            Assert.That(original.attackInfo.baseInfo.hitPhases, Is.Empty, "기존 Default 궁극기의 빈 페이즈를 보존해야 합니다.");
            var reference = Load<UPlayGroundMotionAbilityPayloadSO>(AbilityRoot + "SourcePayload_GreatSword.asset");
            var spear = Payload("Spear");
            Assert.That(spear.attackInfo.baseInfo.hitPhases, Has.Count.EqualTo(1));
            Assert.That(spear.attackInfo.baseInfo.hitPhases[0].damage, Is.EqualTo(141f).Within(.001f));
            AssertVector(Totals(spear.attackInfo.baseInfo.hitPhases), Totals(reference.attackInfo.baseInfo.hitPhases), "Spear");
            Assert.That(spear.attackInfo.motionKey, Is.EqualTo(new MotionKey("Spear.Ultimate")));
        }

        [Test]
        public void 화살비와_토네이도는_독립된지면장판이며_최대틱수와_사망회수를_가진다()
        {
            foreach (var expected in new[]
            {
                (weapon: "Bow", file: "Projectile_BowSignatureRain", ticks: 6, radius: 4f, interval: .3f, delay: .6f, lifetime: 2.4f),
                (weapon: "DualBlade", file: "Projectile_DualBladeSignatureTornado", ticks: 12, radius: 3.5f, interval: .25f, delay: 0f, lifetime: 3f)
            })
            {
                var definition = Projectile(expected.file);
                Assert.That(definition.motion, Is.TypeOf<StationaryProjectileMotion>(), expected.weapon);
                Assert.That(((StationaryProjectileMotion)definition.motion).attachToGround, Is.True);
                Assert.That(((StationaryProjectileMotion)definition.motion).groundLayers.value, Is.EqualTo((1 << 0) | (1 << 3)), "적 캡슐을 지면으로 다시 선택하면 안 된다");
                AreaTickProjectileBehavior tick = definition.GetBehavior<AreaTickProjectileBehavior>();
                Assert.That(tick, Is.Not.Null);
                Assert.That(tick.maxHitsPerTarget, Is.EqualTo(expected.ticks));
                Assert.That(tick.radius, Is.EqualTo(expected.radius).Within(.001f));
                Assert.That(tick.interval, Is.EqualTo(expected.interval).Within(.001f));
                Assert.That(tick.activationDelay, Is.EqualTo(expected.delay).Within(.001f));
                Assert.That(definition.lifetime, Is.EqualTo(expected.lifetime).Within(.001f));
                Assert.That(definition.returnOnOwnerDeath, Is.True);
                Assert.That(definition.inheritOwnerTimeScale, Is.False);
                Assert.That(definition.destroyOnHit, Is.False);
                Assert.That(definition.visualPrefab, Is.Not.Null);
            }
        }

        [Test]
        public void 창과방패는_비행접촉으로_중복피해없이_지정시간에_한번폭발한다()
        {
            foreach (var expected in new[] { (weapon: "Spear", duration: .3f, distance: 8f), (weapon: "SwordShield", duration: .9f, distance: 6f) })
            {
                var definition = Projectile("Projectile_" + expected.weapon + "SignatureFall");
                Assert.That(definition.motion, Is.TypeOf<ArcProjectileMotion>());
                var arc = (ArcProjectileMotion)definition.motion;
                Assert.That(arc.flightTimeMode, Is.EqualTo(ProjectileArcFlightTimeMode.Fixed));
                Assert.That(arc.fixedFlightTime, Is.EqualTo(expected.duration).Within(.001f));
                Assert.That(definition.ignoreFlightContacts, Is.True);
                Assert.That(definition.returnOnOwnerDeath, Is.True);
                var detonate = definition.GetBehavior<DetonateProjectileBehavior>();
                Assert.That(detonate, Is.Not.Null);
                Assert.That(detonate.onHit, Is.False);
                Assert.That(detonate.onExpire, Is.True);
                if (expected.weapon == "SwordShield") Assert.That(definition.landingTelegraphPrefab, Is.Not.Null);
                var spawn = AllEvents(LiveMotion(expected.weapon == "Spear" ? "Reine" : "MyoRyeong").motionSet).OfType<SpawnProjectileEvent>().Single();
                Assert.That(spawn.projectileDefinition, Is.SameAs(definition));
                Assert.That(spawn.projectTargetToGround, Is.True);
                Assert.That(spawn.playerTargetFallbackDistance, Is.EqualTo(expected.distance));
                Assert.That(spawn.targetMode, Is.EqualTo(ProjectileTargetMode.TargetPosition));
            }
        }

        [Test]
        public void 실제타격횟수와_장판틱을합산하면_각궁극기의_기준수치를_보존한다()
        {
            foreach (string character in Characters)
            {
                var definition = Definition(character);
                string weapon = definition.defaultWeaponType.ToString();
                var payload = (UPlayGroundMotionAbilityPayloadSO)definition.abilitySet.GetPlayerAbility(PlayerSkillSlot.Ultimate).variants.Single().executionPayload;
                MotionEventBase[] events = AllEvents(LiveMotion(character).motionSet).ToArray();
                var source = Load<UPlayGroundMotionAbilityPayloadSO>(AbilityRoot + "SourcePayload_" + (weapon == "Spear" ? "GreatSword" : weapon) + ".asset");
                var counts = new int[payload.attackInfo.baseInfo.hitPhases.Count];
                foreach (BeginCollisionEvent collision in events.OfType<BeginCollisionEvent>()) counts[collision.hitPhaseIndex]++;
                foreach (SpawnProjectileEvent spawn in events.OfType<SpawnProjectileEvent>())
                    counts[spawn.hitPhaseIndex] += spawn.projectileDefinition.GetBehavior<AreaTickProjectileBehavior>()?.maxHitsPerTarget ?? 1;
                Vector3 actual = Vector3.zero;
                for (int i = 0; i < counts.Length; i++)
                {
                    Assert.That(counts[i], Is.GreaterThan(0), weapon + " 실행되지 않는 페이즈 " + i);
                    HitPhaseData phase = payload.attackInfo.baseInfo.hitPhases[i];
                    actual += new Vector3(phase.damage, phase.poiseDamage, phase.breakDamage) * counts[i];
                }
                AssertVector(actual, Totals(source.attackInfo.baseInfo.hitPhases), weapon);
            }
        }

        [Test]
        public void 방패없는_궁극기동안은_반격을막고_피해감소를_한번적용한다()
        {
            var set = Definition("MyoRyeong").abilitySet;
            var ultimate = set.GetPlayerAbility(PlayerSkillSlot.Ultimate);
            GameplayTag absent = GameplayTagRegistry.GetRequired("State.Combat.SignatureShieldAbsent");
            Assert.That(ultimate.activation.executionGrantedTagIds.Count(tag => tag == absent), Is.EqualTo(1));
            foreach (var slot in new[] { PlayerCombatAbilitySlot.CounterAttack, PlayerCombatAbilitySlot.ParryCounterAttack })
                Assert.That(set.GetCombatAbility(slot).activation.blockedTagIds, Does.Contain(absent), slot.ToString());
            var effect = ultimate.variants.Single().ownerEffects.Single(value => value.effectId == "Effect.SwordShield.SignatureProtection");
            var reduction = effect.modifiers.Single(modifier => modifier.attributeId == Attributes.Combat.DamageTakenMultiplier.Value);
            Assert.That(reduction.modifierType, Is.EqualTo(ModifierType.Multiply));
            Assert.That(reduction.value, Is.EqualTo(.5f));
            Assert.That(effect.durationType, Is.EqualTo(GameplayEffectDurationType.Duration));
            Assert.That(effect.durationSeconds, Is.EqualTo(55f / 30f).Within(.001f));
        }

        [Test]
        public void 거대검은_본타시각에_지면에닿고_채찍은_몸과무기의_재생길이를_맞춘다()
        {
            foreach (string character in new[] { "Sera", "Lili" })
            {
                MotionSet greatSword = LiveMotion(character).motionSet;
                var prop = AllEvents(greatSword).OfType<TimedPropEvent>().Single();
                var collision = AllEvents(greatSword).OfType<BeginCollisionEvent>().Single(value => value.hitPhaseIndex == 0);
                float time = Mathf.InverseLerp(EventStart(greatSword, prop), EventStart(greatSword, prop) + prop.endTime - prop.startTime, EventStart(greatSword, collision));
                Assert.That(prop.positionProgress.Evaluate(time), Is.EqualTo(1f).Within(.001f));
                Assert.That(prop.endOffset.y, Is.EqualTo(0f));
                Assert.That(prop.endOffset.z, Is.EqualTo(7f));
                Assert.That(prop.dissolveAmount, Is.Not.Null);
                Assert.That(prop.dissolveAmount.Evaluate(0), Is.EqualTo(1).Within(.001f));
                Assert.That(prop.dissolveAmount.Evaluate(time), Is.EqualTo(0).Within(.001f));
                Assert.That(prop.dissolveAmount.Evaluate(1), Is.EqualTo(1).Within(.001f));
                var material = prop.prefab.GetComponentInChildren<Renderer>().sharedMaterial;
                Assert.That(material.HasProperty(prop.dissolveProperty), Is.True);
                Assert.That(material.GetColor("_EdgeColor").maxColorComponent, Is.GreaterThan(1f));
            }
            var weaponSet = Load<ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/WeaponMotion/Whip/WhipMotionSet.asset");
            var weaponMotion = weaponSet.GetAbilityMotionAsset(new MotionKey("Whip.Ultimate"));
            Assert.That(weaponMotion, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(weaponMotion), Is.EqualTo(RedesignedMotionRoot + "Lian_Weapon_Ultimate.asset"));
            Assert.That(LiveMotion("Lian").motionSet.TotalDuration,
                Is.EqualTo(weaponMotion.motionSet.TotalDuration).Within(.001f));
        }

        [Test]
        public void 새궁극기12개는_독립된모션과_유효한전체타임라인을_가진다()
        {
            var assets = Characters.Select(LiveMotion).ToArray();
            Assert.That(assets.Distinct().Count(), Is.EqualTo(Characters.Length));
            Assert.That(assets.Select(AssetDatabase.GetAssetPath).Select(AssetDatabase.AssetPathToGUID).Distinct().Count(), Is.EqualTo(Characters.Length));
            foreach (MotionSetAsset asset in assets.Append(Load<MotionSetAsset>(RedesignedMotionRoot + "Lian_Weapon_Ultimate.asset")))
            {
                MotionSet set = asset.motionSet;
                AssertFinite(set.TotalDuration, asset.name);
                Assert.That(set.TotalDuration, Is.GreaterThan(0f), asset.name);
                AssertTimeline(set.motions, set.globalEvents, set.TotalDuration, asset.name);
                foreach (MotionLayer layer in set.layers)
                {
                    Assert.That(layer, Is.Not.Null, asset.name);
                    AssertTimeline(layer.motions, layer.globalEvents, set.TotalDuration, asset.name + "/" + layer.layerName);
                }
            }
        }

        [Test]
        public void 새궁극기는_원본의_타격페이즈별횟수와_투사체정의를_보존한다()
        {
            foreach (string character in Characters)
            {
                string weapon = Definition(character).defaultWeaponType.ToString();
                MotionSet source = Motion(character == "Raon" ? "Katana_Raon" : weapon).motionSet;
                MotionEventBase[] before = AllEvents(source).ToArray();
                MotionEventBase[] after = AllEvents(LiveMotion(character).motionSet).ToArray();
                CollectionAssert.AreEquivalent(before.OfType<BeginCollisionEvent>().Select(e => e.hitPhaseIndex),
                    after.OfType<BeginCollisionEvent>().Select(e => e.hitPhaseIndex), character + " 충돌 페이즈/횟수");
                CollectionAssert.AreEquivalent(before.OfType<SpawnProjectileEvent>().Select(e => (e.hitPhaseIndex, e.projectileDefinition)),
                    after.OfType<SpawnProjectileEvent>().Select(e => (e.hitPhaseIndex, e.projectileDefinition)), character + " 투사체 페이즈/횟수/정의");
            }
        }

        [Test]
        public void 캔슬은_마지막타격또는발사후에열리고_중력구간은_겹치지않는다()
        {
            foreach (string character in Characters)
            {
                MotionSet set = LiveMotion(character).motionSet;
                MotionEventBase[] events = AllEvents(set).ToArray();
                var impacts = events.Where(e => e is BeginCollisionEvent || e is SpawnProjectileEvent).ToArray();
                Assert.That(impacts, Is.Not.Empty, character);
                float lastImpact = impacts.Max(e => EventStart(set, e));
                var cancels = events.OfType<CancelWindowEvent>().ToArray();
                Assert.That(cancels, Is.Not.Empty, character);
                foreach (var cancel in cancels)
                    Assert.That(EventStart(set, cancel), Is.GreaterThanOrEqualTo(lastImpact - .001f), character + " 타격 전 캔슬");
                var gravity = events.OfType<AttackGravityEvent>().OrderBy(e => EventStart(set, e)).ToArray();
                for (int i = 1; i < gravity.Length; i++)
                {
                    float previousEnd = EventStart(set, gravity[i - 1]) + gravity[i - 1].endTime - gravity[i - 1].startTime;
                    Assert.That(EventStart(set, gravity[i]), Is.GreaterThanOrEqualTo(previousEnd - .001f), character + " 중력 구간 중첩");
                }
            }
            MotionSet spear = LiveMotion("Reine").motionSet;
            var spawn = AllEvents(spear).OfType<SpawnProjectileEvent>().Single();
            var visibility = AllEvents(spear).OfType<WeaponSlotVisibilityEvent>().Single();
            Assert.That(EventStart(spear, visibility), Is.EqualTo(EventStart(spear, spawn)).Within(.001f), "창은 발사 순간에 손에서 사라져야 합니다.");
        }

        private static MotionSetAsset LiveMotion(string character)
        {
            var definition = Definition(character);
            var model = Load<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_" + character + ".prefab");
            var animator = model.GetComponentInChildren<PlayerActorAnimator>(true);
            Assert.That(animator, Is.Not.Null, character);
            var payload = (UPlayGroundMotionAbilityPayloadSO)definition.abilitySet.GetPlayerAbility(PlayerSkillSlot.Ultimate).variants.Single().executionPayload;
            var motion = animator.PlayerMotionSet.GetAbilityMotionAsset(definition.defaultWeaponType, payload.attackInfo.motionKey);
            Assert.That(motion, Is.Not.Null, character);
            return motion;
        }

        private static IEnumerable<MotionEventBase> AllEvents(MotionSet set)
            => set.globalEvents.Concat(set.motions.SelectMany(m => m.events))
                .Concat(set.layers.Where(l => l.enabled).SelectMany(l => l.globalEvents.Concat(l.motions.SelectMany(m => m.events))));

        private static float EventStart(MotionSet set, MotionEventBase motionEvent)
        {
            Assert.That(set.TryGetEventGlobalStart(motionEvent, out float start), Is.True);
            return start;
        }

        private static void AssertTimeline(IEnumerable<Motion> motions, IEnumerable<MotionEventBase> events, float duration, string context)
        {
            foreach (Motion motion in motions)
            {
                Assert.That(motion, Is.Not.Null, context);
                Assert.That(motion.motionClip, Is.Not.Null, context);
                AssertFinite(motion.clipStartTime, context);
                AssertFinite(motion.clipEndTime, context);
                AssertFinite(motion.playbackSpeed, context);
                AssertFinite(motion.Duration, context);
                Assert.That(motion.playbackSpeed, Is.GreaterThan(0f), context);
                Assert.That(motion.Duration, Is.GreaterThan(0f), context);
                AssertEvents(motion.events, motion.Duration, context);
            }
            AssertEvents(events, duration, context);
        }

        private static void AssertEvents(IEnumerable<MotionEventBase> events, float duration, string context)
        {
            foreach (MotionEventBase motionEvent in events)
            {
                Assert.That(motionEvent, Is.Not.Null, context + " 이벤트 역직렬화 누락");
                AssertFinite(motionEvent.startTime, context);
                AssertFinite(motionEvent.endTime, context);
                Assert.That(motionEvent.startTime, Is.GreaterThanOrEqualTo(0f), context);
                Assert.That(motionEvent.endTime, Is.GreaterThanOrEqualTo(motionEvent.startTime), context);
                Assert.That(motionEvent.endTime, Is.LessThanOrEqualTo(duration + .001f), context + "/" + motionEvent.GetType().Name);
            }
        }

        private static void AssertFinite(float value, string context)
            => Assert.That(float.IsNaN(value) || float.IsInfinity(value), Is.False, context);

        private static PlayerCharacterDefinitionSO Definition(string character)
            => Load<PlayerCharacterDefinitionSO>("Assets/10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_" + character + ".asset");
        private static MotionSetAsset Motion(string weapon) => Load<MotionSetAsset>(MotionRoot + weapon + "_Signature.asset");
        private static ProjectileDefinitionSO Projectile(string file) => Load<ProjectileDefinitionSO>("Assets/10.Datas/Projectile/Signature/" + file + ".asset");
        private static UPlayGroundMotionAbilityPayloadSO Payload(string weapon)
            => (UPlayGroundMotionAbilityPayloadSO)Load<GameplayAbilitySO>(weapon == "Spear" ? AbilityRoot + "GA_PlayerSpear_Ultimate.asset" : SourceAbilityPath(weapon)).variants.Single().executionPayload;
        private static string SourceAbilityPath(string weapon) => weapon == "Staff"
            ? "Assets/10.Datas/Ability/Player/Staff/GA_PlayerStaff_Ultimate.asset"
            : "Assets/10.Datas/Ability/Migrated/Player" + weapon + "AttackData/GA_Player" + weapon + "AttackData_Ultimate.asset";

        private static T Load<T>(string path) where T : UnityEngine.Object
        { T value = AssetDatabase.LoadAssetAtPath<T>(path); Assert.That(value, Is.Not.Null, path); return value; }
        private static Vector3 Totals(IEnumerable<HitPhaseData> phases)
        { Vector3 result = Vector3.zero; foreach (HitPhaseData phase in phases) result += new Vector3(phase.damage, phase.poiseDamage, phase.breakDamage); return result; }
        private static void AssertVector(Vector3 actual, Vector3 expected, string context)
        { Assert.That(Vector3.Distance(actual, expected), Is.LessThan(.01f), context + ": " + actual + " / " + expected); }

        [Serializable] private sealed class Baseline { public Entry[] entries; }
        [Serializable] private sealed class Entry { public string path; public string hash; }
        private static void AssertBaselineHash(string path)
        {
            string json = File.ReadAllText("docs/work-status/weapon-signature/BASELINE_HASHES.json");
            Entry expected = JsonUtility.FromJson<Baseline>("{\"entries\":" + json + "}").entries.Single(value => value.path == path);
            using SHA256 hash = SHA256.Create();
            using FileStream input = File.OpenRead(path);
            string actual = BitConverter.ToString(hash.ComputeHash(input)).Replace("-", "");
            Assert.That(actual, Is.EqualTo(expected.hash).IgnoreCase, path + " 원본 변경");
        }
    }
}
#endif
