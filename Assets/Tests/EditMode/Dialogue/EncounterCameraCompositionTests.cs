using NUnit.Framework;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data;

namespace UPlayGround.Camera.Tests
{
    public sealed class EncounterCameraCompositionTests
    {
        [Test]
        public void ExplorePreservesUserZoomOutsideProfileTransition()
        {
            var settings = ScriptableObject.CreateInstance<CameraSettings>();
            try
            {
                var controller = new CameraDistanceController(settings, null, 0, 55f);
                Assert.AreEqual(-1f, controller.EvaluateDistance(false, false, 6f, default, 0.1f));
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        [TestCase(1, 0f, 7f)]
        [TestCase(10, 0f, 9f)]
        [TestCase(2, 20f, 9f)]
        public void CrowdUsesContinuousPopulationAndSpread(int count, float radius, float expected)
            => Assert.AreEqual(expected, EncounterCameraComposition.CalculateCrowdDistance(count, radius, 20f, 10, new Vector2(7, 9)), 0.001f);

        [Test]
        public void DeadZoneDoesNotChatterAtCountBoundary()
        {
            Assert.AreEqual(8f, EncounterCameraComposition.ApplyDeadZone(8f, 8.1f, 0.15f));
            Assert.AreEqual(8.3f, EncounterCameraComposition.ApplyDeadZone(8f, 8.3f, 0.15f));
        }

        [TestCase(false, 9f, 5.8f)]
        [TestCase(true, 9f, 9f)]
        [TestCase(true, 4f, 5.8f)]
        public void LockDistanceUsesCrowdOnlyInSweep(bool sweep, float crowd, float expected)
            => Assert.AreEqual(expected, EncounterCameraComposition.ResolveLockDistance(5.8f, crowd, sweep));

        [Test]
        public void LegacyFieldsMigrateOnceWithoutLosingOffsets()
        {
            var settings = ScriptableObject.CreateInstance<CameraSettings>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"_legacyFovExplore\":63,\"_legacyCombatOffset\":{\"x\":0.4,\"y\":1.7,\"z\":0}}", settings);
                settings.OnAfterDeserialize();
                Assert.AreEqual(63f, settings.explore.fieldOfView);
                Assert.AreEqual(0.4f, settings.sweep.pivotOffset.x);
                settings.explore.fieldOfView = 61f;
                settings.OnAfterDeserialize();
                Assert.AreEqual(61f, settings.explore.fieldOfView);
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        [Test]
        public void NewProfileSurvivesFirstSerialization()
        {
            var source = ScriptableObject.CreateInstance<CameraSettings>();
            var loaded = ScriptableObject.CreateInstance<CameraSettings>();
            try
            {
                source.explore.fieldOfView = 62f;
                source.sweep.pivotOffset = new Vector3(0.7f, 1.6f, 0f);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), loaded);
                Assert.AreEqual(62f, loaded.explore.fieldOfView);
                Assert.AreEqual(source.sweep.pivotOffset, loaded.sweep.pivotOffset);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(loaded);
            }
        }
    }
}
