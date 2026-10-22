using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data.Event;
using UPlayGround.MovementController;

namespace UPlayGround.Movement.Tests
{
    /// <summary>플레이어의 전진 검격이 타격 전에 제한된 간격을 좁힐 수 있는지 검증한다.</summary>
    public class PlayerWeaponApproachTests
    {
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Skill_3_PlayerApproach.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/WhipCharacter_Attack_1.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/WhipCharacter_Attack_2.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/WhipCharacter_Attack_3.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/WhipCharacter_Attack_4.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/WhipCharacter_Attack_6.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/WhipCharacter_HeavyAttack_4.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Whip/WhipCharacter_HeavyAttack_5.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_Attack_1.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_Attack_2.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_Attack_3.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_Attack_4.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_Attack_5.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_Attack_6.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_Attack_7.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_Attack_8.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_Attack_9.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_HeavyAttack_1.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_HeavyAttack_2.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_HeavyAttack_3.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_HeavyAttack_4.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_HeavyAttack_6.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_CounterAttack_1_PlayerApproach.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DoubleAxe/Humanoid_DoubleAxeAxeAnimationSet_SwapAttack_1.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/Attack_1.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/Attack_2.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/Attack_3.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/Attack_4.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/Attack_7.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/Attack_9.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/HeavyAttack_2.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/HeavyAttack_5.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/DualBlade/HeavyAttack_7.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Spear/Humanoid_Spear_Attack_3.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_1.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_2.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_3.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_4.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_5.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_6.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_7.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_8.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_9.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_Attack_10.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_HeavyAttack_1.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_HeavyAttack_2.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_HeavyAttack_4.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_HeavyAttack_5.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_HeavyAttack_6.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_HeavyAttack_7.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/GreatSword/Humanoid_GreatSwordAnimationSet_SwapAttack_1.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_Attack_1.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_Attack_2.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_Attack_3.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_Attack_4.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_Attack_5.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_Attack_6.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_Attack_7.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_Attack_8.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_Attack_9.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_Attack_10.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_HeavyAttack_1.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_HeavyAttack_2.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_HeavyAttack_3.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_HeavyAttack_4.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_HeavyAttack_7.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_HeavyAttack_8.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_HeavyAttack_9.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/SwordShield/Humanoid_SwordShield_SwapAttack.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Combo_Attack_1_3_PlayerGrounded.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Combo_Attack_2_1_PlayerGrounded.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Combo_Attack_2_2_PlayerGrounded.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Combo_Attack_2_3_PlayerGrounded.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Combo_Attack_3_1_PlayerGrounded.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Combo_Attack_3_2_PlayerGrounded.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Heavy_Attack_3_PlayerGrounded.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Combo_Attack_5_1_PlayerGrounded.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Combo_Attack_1_1_PlayerGrounded.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Katana_Combo_Attack_1_2_PlayerGrounded.asset")]
        [TestCase("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Katana/Test/Bokuse_Katana_DashAttack_1.asset")]
        public void 전진_공격은_유효한_원본으로_일부_접근하고_타격_전에_끝낸다(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
            Assert.That(asset, Is.Not.Null, path);
            Assert.That(SerializationUtility.HasManagedReferencesWithMissingTypes(asset), Is.False);
            var events = asset.motionSet.motions.Single().events;
            var warp = events.OfType<MotionEvent_MotionWarp>().Single();
            var settings = warp.ResolveSettings();
            Assert.That(settings.correctionLimitPolicy, Is.EqualTo(MotionWarpCorrectionLimitPolicy.Clamp));
            Assert.That(settings.maxCorrectionDistance, Is.InRange(0.3f, 1f));
            Assert.That(warp.endTime - settings.translationEndLeadTime,
                Is.LessThan(events.OfType<BeginCollisionEvent>().Min(e => e.startTime)));
            Assert.That(warp.bakedProfiles, Is.Not.Empty);

            bool hasPartialApproach = false;
            foreach (var profile in warp.bakedProfiles)
            {
                Assert.That(MotionWarpTrajectory.MatchesSource(profile, asset.motionSet,
                    MotionWarpTrajectory.BuildSourceSignature(asset.motionSet)), Is.True, profile.avatar.name);
                MotionWarpTrajectory.SampleRawCumulative(profile,
                    1f - settings.translationEndLeadTime / settings.duration,
                    out var source, out float length, out _);
                if (source.z <= 0f || source.magnitude < settings.minimumSourceDisplacement
                    || length <= 0f || source.magnitude / length < settings.minimumNetToPathRatio)
                    continue;

                bool resolved = MotionWarpMath.TryResolveEndpointCorrection(source, length,
                    source + source.normalized * 2f, settings, out var correction);
                Assert.That(resolved, Is.True, profile.avatar.name);
                Assert.That(correction.magnitude, Is.LessThanOrEqualTo(settings.maxCorrectionDistance + .0001f));
                hasPartialApproach |= correction.magnitude >= .1f;
            }
            Assert.That(hasPartialApproach, Is.True, "실제 리그의 전진 구간에서 일부 접근이 가능해야 한다.");
        }
    }
}
