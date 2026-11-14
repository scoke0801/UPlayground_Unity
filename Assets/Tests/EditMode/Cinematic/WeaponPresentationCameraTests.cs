using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Cinematic.Tests
{
    public sealed class WeaponPresentationCameraTests
    {
        [Test]
        public void 유효하지_않은_프로필은_카메라_저장전에_거절한다()
        {
            var profile = ScriptableObject.CreateInstance<WeaponPresentationProfileSO>();
            profile.weaponType = WeaponType.Sword;
            try
            {
                Type builder = Type.GetType("UPlayGround.Data.Editor.WeaponPresentationCameraBuilder, Assembly-CSharp-Editor");
                Assert.That(builder, Is.Not.Null);
                Assert.Throws<TargetInvocationException>(() => builder.GetMethod("Create").Invoke(null, new object[] { profile, 4f }));
                Assert.That(profile.cameraPresets, Is.Empty);
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void 무기_십종의_카메라는_실제_다섯_샷과_전투_시계를_쓴다()
        {
            string[] guids = AssetDatabase.FindAssets("t:WeaponPresentationProfileSO",
                new[] { "Assets/10.Datas/Combat/WeaponPresentation" });
            Assert.That(guids.Length, Is.EqualTo(10));
            foreach (string guid in guids)
            {
                var profile = AssetDatabase.LoadAssetAtPath<WeaponPresentationProfileSO>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(profile.cameraPresets, Is.Not.Empty);
                CameraSnapshotProfile camera = profile.cameraPresets[0];
                Assert.That(camera, Is.Not.Null);
                Assert.That(camera.shots.Count, Is.EqualTo(5));
                Assert.That(camera.useUnscaledTime, Is.False);
                Assert.That(camera.restorePreviousModeOnFinish, Is.True);
                foreach (CameraSnapshotShot shot in camera.shots)
                {
                    Assert.That(shot.duration, Is.GreaterThan(0f));
                    Assert.That(float.IsNaN(shot.duration) || float.IsInfinity(shot.duration), Is.False);
                }
            }
        }

        [TestCase("Raon")]
        [TestCase("Hwarin")]
        public void 구도_미검증_카메라_초안은_기존_궁극기를_덮지_않는다(string name)
        {
            var sequence = AssetDatabase.LoadAssetAtPath<UltimateSequenceAsset>(
                $"Assets/10.Datas/Actor/Ultimate/UltimateSequence_{name}.asset");
            Assert.That(sequence.weaponProfile, Is.Not.Null);
            Assert.That(sequence.weaponProfile.cameraPresets[0], Is.Not.Null);
            Assert.That(sequence.cameraProfile, Is.Not.SameAs(sequence.weaponProfile.cameraPresets[0]));
        }

        [TestCase("Raon", "e058822f7534df34ca58fca059ce759c")]
        [TestCase("Hwarin", "69e9e4d16dc3b384fa868a8ab6f2943a")]
        public void 보관한_카메라_후보의_길이는_실제_궁극기_매핑_모션과_같다(string name, string motionGuid)
        {
            var sequence = AssetDatabase.LoadAssetAtPath<UltimateSequenceAsset>(
                $"Assets/10.Datas/Actor/Ultimate/UltimateSequence_{name}.asset");
            ScriptableObject motionAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(motionGuid));
            Assert.That(motionAsset, Is.Not.Null);
            object motionSet = motionAsset.GetType().GetField("motionSet").GetValue(motionAsset);
            float duration = (float)motionSet.GetType().GetProperty("TotalDuration").GetValue(motionSet);
            Assert.That(sequence.weaponProfile.cameraPresets[0].TotalDuration, Is.EqualTo(duration).Within(0.001f));
        }
    }
}
