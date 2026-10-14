using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Content.Tests
{
    /// <summary>생활 모션은 레거시 클립 콜백 없이 타임라인의 획득 이벤트를 유지한다.</summary>
    public sealed class LegacyInteractionEventTests
    {
        [TestCase("f09eaec882904a44b84523a123d8f503")]
        [TestCase("1f80dfbf2e2e7904195c7dcbf7cdfd55")]
        [TestCase("1cfacc13c5002b4438e146bf504e7bcf")]
        [TestCase("9debf8d00fcb33d45913ee37b75fbe34")]
        [TestCase("f49d3433f41d3a148a2259659e51b73e")]
        [TestCase("c143fe553160d0549b17755dcc09fbb8")]
        public void ImportedClips_DoNotDispatchLegacyHit(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().ToArray();
            Assert.That(clips, Is.Not.Empty, path);
            foreach (var clip in clips)
                Assert.That(AnimationUtility.GetAnimationEvents(clip).Any(e => e.functionName == "Hit"),
                    Is.False, path + "/" + clip.name);
        }

        [TestCase("HandGathering")]
        [TestCase("WoodCutting")]
        [TestCase("Mining_Wall")]
        [TestCase("Mining_Ground")]
        public void GatheringMotions_KeepTimelineHit(string motionName)
        {
            string path = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Common/Raon_Common_" + motionName + ".asset";
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            Assert.That(asset, Is.Not.Null, path);
            var motions = new SerializedObject(asset).FindProperty("motionSet.motions");
            bool hasTimelineHit = false;
            for (int i = 0; i < motions.arraySize; i++)
            {
                var events = motions.GetArrayElementAtIndex(i).FindPropertyRelative("events");
                for (int j = 0; j < events.arraySize; j++)
                {
                    var entry = events.GetArrayElementAtIndex(j);
                    if (!entry.managedReferenceFullTypename.EndsWith("UPlayGround.Data.Event.InteractionEvent"))
                        continue;
                    hasTimelineHit |= entry.FindPropertyRelative("interactionEvent").enumValueIndex == 0;
                }
            }
            Assert.That(hasTimelineHit, Is.True, path);
        }
    }
}
