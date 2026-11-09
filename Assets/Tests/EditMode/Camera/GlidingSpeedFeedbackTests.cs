using NUnit.Framework;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data;

namespace UPlayGround.Camera.Tests
{
    /// <summary>활공 속도감이 기본 이동 FOV와 중복되거나 종료 뒤 남는 회귀를 막는다.</summary>
    public sealed class GlidingSpeedFeedbackTests
    {
        [Test]
        public void 연출모드에서는_바람을즉시숨기고_종료하면메시를해제한다()
        {
            var settings = ScriptableObject.CreateInstance<CameraSettings>();
            var root = new GameObject("활공 효과 수명 검사");
            GlidingSpeedLines effect = null;
            try
            {
                settings.glidingSpeedLineMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/10.Datas/Camera/GlidingSpeedLines.mat");
                Assert.That(settings.glidingSpeedLineMaterial, Is.Not.Null);
                effect = new GlidingSpeedLines(root.AddComponent<UnityEngine.Camera>(), settings);
                effect.Advance(1f, Vector3.right * 18f, 1f, true);
                var view = root.transform.GetChild(0).gameObject;
                var mesh = view.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(view.activeSelf, Is.True);
                Assert.That(mesh.vertexCount, Is.EqualTo(settings.glidingSpeedLineCount * 4));
                effect.Advance(1f, Vector3.right * 18f, 0.016f, false);
                Assert.That(view.activeSelf, Is.False);
                effect.Dispose();
                Assert.That(mesh == null, Is.True);
                Assert.That(view == null, Is.True);
                effect = null;
            }
            finally
            {
                effect?.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void 활공FOV는_일반속도FOV와_중첩하지않는다(float intensity)
        {
            var settings = ScriptableObject.CreateInstance<CameraSettings>();
            try
            {
                settings.enableTraversalComposition = false;
                settings.enableSpeedFOV = true;
                var controller = new CameraDistanceController(settings, null, 0, 52f);
                controller.SetPlayerVelocityProvider(() => Vector3.forward * 18f);
                var motion = new CameraMotionContext(true, false, Vector3.forward * 18f,
                    Vector3.up, Vector3.up, true, intensity);
                controller.SnapFOV(false, false, motion);
                Assert.That(controller.BaseFOV, Is.EqualTo(settings.explore.fieldOfView
                    + settings.glidingFOVMaxAdd * intensity).Within(0.001f));
                controller.SnapFOV(false, false, new CameraMotionContext(true, true,
                    Vector3.zero, Vector3.up, Vector3.up));
                controller.SetPlayerVelocityProvider(() => Vector3.zero);
                controller.SnapFOV(false, false, default);
                Assert.That(controller.BaseFOV, Is.EqualTo(settings.explore.fieldOfView).Within(0.001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(settings); }
        }

        [Test]
        public void 이동정보가없는경우_속도효과는꺼져있다()
        {
            Assert.That(CameraMotionContext.Unavailable.IsGliding, Is.False);
            Assert.That(CameraMotionContext.Unavailable.SpeedFeedback, Is.Zero);
        }
    }
}
