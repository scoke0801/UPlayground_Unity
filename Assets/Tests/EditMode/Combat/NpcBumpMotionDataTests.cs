using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.Combat.Tests
{
    /// <summary>NPC 충돌 반응 모션 데이터가 남/여 NPC 모션셋에 빠짐없이, 안전한 클립으로 연결됐는지 검사한다.</summary>
    public sealed class NpcBumpMotionDataTests
    {
        private const string MotionSetFolder = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/";
        private const float RootDriftTolerance = 0.05f;

        private static readonly string[] NpcMotionSets = { "MotionSet_Npc_Male", "MotionSet_Npc_Female" };

        private static System.Collections.Generic.IEnumerable<(NpcBumpTier tier, NpcBumpMotionDirection direction)> AllSlots()
        {
            foreach (var tier in new[] { NpcBumpTier.Stagger, NpcBumpTier.Shove })
                foreach (NpcBumpMotionDirection direction in System.Enum.GetValues(typeof(NpcBumpMotionDirection)))
                    yield return (tier, direction);
        }

        [TestCaseSource(nameof(NpcMotionSets))]
        public void 모든_단계와_방향의_전용_모션이_연결되어_있다(string setName)
        {
            var set = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(MotionSetFolder + setName + ".asset");
            Assert.IsNotNull(set, setName);
            foreach (var (tier, direction) in AllSlots())
            {
                var tag = NpcBumpResolver.ResolveMotionTag(tier, direction);
                var asset = set.GetMotionSetAsset(tag);
                Assert.IsNotNull(asset, $"{setName} 슬롯 누락: {tag.TagName}");
                Assert.IsTrue(asset.motionSet.IsValid(), $"{tag.TagName} 모션 비정상");
                Assert.IsTrue(MotionTimelineResolver.TryValidateSectionLayout(asset.motionSet, out string error), $"{tag.TagName} 구간 배치: {error}");
            }
        }

        [Test]
        public void 연결된_클립은_휴머노이드이고_루트가_제자리다()
        {
            var set = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(MotionSetFolder + NpcMotionSets[0] + ".asset");
            foreach (var (tier, direction) in AllSlots())
            {
                var tag = NpcBumpResolver.ResolveMotionTag(tier, direction);
                foreach (var motion in set.GetMotionSetAsset(tag).motionSet.motions)
                {
                    Assert.IsNotNull(motion.motionClip, tag.TagName);
                    Assert.IsTrue(motion.motionClip.isHumanMotion, $"{tag.TagName} 휴머노이드 아님");
                    Assert.Less(MeasureRootDrift(motion.motionClip), RootDriftTolerance, $"{tag.TagName} 루트가 이동함");
                }
            }
        }

        [Test]
        public void 남녀_모션셋이_같은_모션_에셋을_공유한다()
        {
            var male = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(MotionSetFolder + NpcMotionSets[0] + ".asset");
            var female = AssetDatabase.LoadAssetAtPath<ActorAnimationMotionSet>(MotionSetFolder + NpcMotionSets[1] + ".asset");
            foreach (var (tier, direction) in AllSlots())
            {
                var tag = NpcBumpResolver.ResolveMotionTag(tier, direction);
                Assert.AreSame(male.GetMotionSetAsset(tag), female.GetMotionSetAsset(tag), tag.TagName);
            }
        }

        [Test]
        public void 데이터가_없는_씬_배치_NPC가_쓸_기본_프로필이_Resources에_있다()
        {
            var profile = NpcBumpProfileSO.LoadDefault();
            Assert.IsNotNull(profile, "Resources/Config/NpcBumpDefault.asset 누락");
            Assert.Greater(profile.staggerDistance, 0f);
        }

        [Test]
        public void 모든_NPC_데이터가_충돌_반응_프로필을_갖는다()
        {
            var missing = new System.Collections.Generic.List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:NpcActorSO"))
            {
                var data = AssetDatabase.LoadAssetAtPath<NpcActorSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (data != null && data.bumpProfile == null) missing.Add(data.name);
            }
            Assert.IsEmpty(missing, string.Join(", ", missing));
        }

        private static float MeasureRootDrift(AnimationClip clip)
        {
            float drift = 0f;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.propertyName != "RootT.x" && binding.propertyName != "RootT.z") continue;
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                drift = Mathf.Max(drift, Mathf.Abs(curve.Evaluate(clip.length) - curve.Evaluate(0f)));
            }
            return drift;
        }
    }
}
