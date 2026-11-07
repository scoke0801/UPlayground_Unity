using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;

namespace UPlayGround.Ability.Tests
{
    /// <summary>실제 창병 소비 경로·판정과 추가 본을 포함한 독립 클립을 검사한다.</summary>
    public sealed class HumanoidSpearContentTests
    {
        private const string Root = "Assets/10.Datas/Actor/HumanoidSpear";
        private static ActorDefinitionSO Definition => AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(Root + "/Enemy_Field_Spear_001.asset");
        private static AbilityAttackInfo Info(GameplayAbilitySO ability) => (ability.variants[0].executionPayload as UPlayGroundMotionAbilityPayloadSO)?.attackInfo;
        private static GameplayAbilitySO[] Attacks => Definition.EffectiveAbilitySet.EnumerateAll().Where(a => Info(a)?.aiSelectable == true).ToArray();

        [Test]
        public void 신규_일반_창병의_실제_소비경로는_독립되어_있다()
        {
            var d = Definition;
            Assert.That(d, Is.Not.Null);
            Assert.That(d.actorId, Is.EqualTo("Enemy_Field_Spear_001"));
            Assert.That(d.EffectiveGrade, Is.EqualTo(MonsterActorGrade.Normal));
            Assert.That(d.monsterProfile.abilitySet, Is.SameAs(d.abilitySet));
            Assert.That(d.EffectiveAbilitySet, Is.SameAs(d.abilitySet));
            var serialized = new SerializedObject(d.prefab.GetComponent<MonsterActor>());
            Assert.That(serialized.FindProperty("_definition").objectReferenceValue, Is.SameAs(d));
            var map = d.prefab.GetComponent<ActorAnimator>().MotionSet;
            Assert.That(AssetDatabase.GetAssetPath(map).StartsWith(Root), Is.True);
            Assert.That(map.attackWeaponType, Is.EqualTo(WeaponType.Spear));
            Assert.That(map.attackAbilitySet, Is.SameAs(d.EffectiveAbilitySet));
            Assert.That(Attacks.Length, Is.EqualTo(8));
            Assert.That(Attacks.All(a => AssetDatabase.GetAssetPath(a).StartsWith(Root)), Is.True);
            Assert.That(Attacks.All(a => AssetDatabase.GetAssetPath(a.variants[0].executionPayload).StartsWith(Root)), Is.True);
        }

        [TestCase(1, 5)] [TestCase(9, 5)] [TestCase(10, 6)]
        [TestCase(19, 6)] [TestCase(20, 7)] [TestCase(29, 7)]
        [TestCase(30, 8)] [TestCase(40, 8)] [TestCase(50, 8)]
        public void 레벨_경계에서_다섯_역할과_일반_등급_격리가_유지된다(int level, int count)
        {
            var infos = Attacks.Select(Info).Where(i => i.IsUnlockedForLevel(level)).ToArray();
            Assert.That(infos.Length, Is.EqualTo(count));
            foreach (var role in new[] { AbilityAIRole.Opener, AbilityAIRole.Finisher, AbilityAIRole.Punish, AbilityAIRole.Counter, AbilityAIRole.GapCloser })
                Assert.That(infos.Any(i => (i.aiRoles & role) != 0), Is.True, role.ToString());
            Assert.That(infos.Any(i => (i.aiRoles & AbilityAIRole.Signature) != 0), Is.False);
        }

        [Test]
        public void 모든_공격은_고유_동작과_Phase별_실물_창_판정을_갖는다()
        {
            var map = Definition.prefab.GetComponent<ActorAnimator>().MotionSet;
            var clips = Attacks.Select(a => map.GetAbilityMotionAsset(Info(a).motionKey).motionSet.motions.Single().motionClip).ToArray();
            Assert.That(clips.Distinct().Count(), Is.EqualTo(8));
            foreach (var ability in Attacks)
            {
                var info = Info(ability);
                var motion = map.GetAbilityMotionAsset(info.motionKey).motionSet;
                var events = motion.motions.Single().events.OfType<BeginCollisionEvent>().ToArray();
                Assert.That(events.Length, Is.EqualTo(info.baseInfo.hitPhases.Count), ability.name);
                Assert.That(events.Select(e => e.hitPhaseIndex), Is.EqualTo(Enumerable.Range(0, events.Length)), ability.name);
                Assert.That(events.All(e => e.hitboxGroupId == "MainWeapon" && e.collisionSource == CollisionSourceType.AttachedHitboxGroup), Is.True);
                Assert.That(info.baseInfo.hitPhases.All(p => p.projectileDefinition == null && p.hitboxGroupId == "MainWeapon"), Is.True);
                Assert.That(ability.balance.expectedDamage, Is.EqualTo(info.baseInfo.hitPhases.Sum(p => p.damage)));
                Assert.That(events.Min(e => e.startTime), Is.GreaterThanOrEqualTo(.17f));
                Assert.That(motion.TotalDuration - events.Max(e => e.endTime), Is.GreaterThan(.25f), ability.name + " 후딜");
            }
        }

        [Test]
        public void 판정은_세_스킨본의_실제_메시_크기이며_추가_강공격은_쿨다운을_공유한다()
        {
            var prefab = Definition.prefab;
            var weapon = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == "Weapon_Spear");
            Assert.That(weapon.bones.Select(b => b.name), Is.EqualTo(new[] { "Spear_Root", "Spear_Bone01", "Spear_Bone02" }));
            var hits = prefab.GetComponentsInChildren<CombatHitbox>(true).Where(h => h.GroupId == "MainWeapon").ToArray();
            Assert.That(hits.Length, Is.EqualTo(3));
            Assert.That(hits.All(h => h.IsSupported && weapon.bones.Contains(h.transform.parent)), Is.True);
            Assert.That(hits.Select(h => (BoxCollider)h.ShapeCollider).All(b => b.size.magnitude < 1.1f), Is.True);
            var heavy = Attacks.Where(a => Info(a).attackCategory == AbilityAttackCategory.Heavy).ToArray();
            Assert.That(heavy.Length, Is.EqualTo(2));
            Assert.That(heavy.Select(a => a.cooldown.cooldownGroupId).Distinct().Count(), Is.EqualTo(1));
            Assert.That(heavy.All(a => a.cooldown.durationSeconds == 6f), Is.True);
        }

        [Test]
        public void 독립_클립의_추가본_경로는_프리팹에_실재하고_원본_바디곡선은_보존된다()
        {
            var prefab = Definition.prefab;
            var paths = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/07.Animations/Monster/FieldSpear" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            Assert.That(paths, Is.Not.Empty);
            var sourceGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/ExternalAssets" });
            foreach (var path in paths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                string guidPrefix = System.IO.Path.GetFileNameWithoutExtension(path).Split('_').Last();
                var sourcePaths = sourceGuids.Where(g => g.StartsWith(guidPrefix)).Select(AssetDatabase.GUIDToAssetPath).Distinct().ToArray();
                Assert.That(sourcePaths.Length, Is.EqualTo(1), path);
                var originals = AssetDatabase.LoadAllAssetsAtPath(sourcePaths[0]).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__") && clip.name.EndsWith("_" + c.name.Replace(" ", "_") + "_" + guidPrefix)).ToArray();
                Assert.That(originals.Length, Is.EqualTo(1), path + " 원본 클립 식별");
                var original = originals.Single();
                Assert.That(clip.length, Is.EqualTo(original.length).Within(.0001f), path);
                foreach (var binding in AnimationUtility.GetCurveBindings(original).Where(b => b.type == typeof(Animator)))
                {
                    var expected = AnimationUtility.GetEditorCurve(original, binding).keys;
                    var actual = AnimationUtility.GetEditorCurve(clip, binding).keys;
                    Assert.That(actual.Select(k => new Vector4(k.time, k.value, k.inTangent, k.outTangent)), Is.EqualTo(expected.Select(k => new Vector4(k.time, k.value, k.inTangent, k.outTangent))), path + "/" + binding.propertyName);
                }
                var extra = AnimationUtility.GetCurveBindings(clip).Where(b => b.type == typeof(Transform) && b.path.Contains("/R_Hand_Weapon")).ToArray();
                Assert.That(extra, Is.Not.Empty, path);
                foreach (var binding in extra) Assert.That(prefab.transform.Find(binding.path), Is.Not.Null, path + "/" + binding.path);
            }
        }
    }
}
