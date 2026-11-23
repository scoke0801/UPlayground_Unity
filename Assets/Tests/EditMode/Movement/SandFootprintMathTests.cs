using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace UPlayGround.Movement.Tests
{
    public sealed class SandFootprintMathTests
    {
        private const BindingFlags StaticMethod = BindingFlags.NonPublic | BindingFlags.Static;
        private static Type World => Type.GetType("UPlayGround.Gameplay.World.SandFootprintWorld, Assembly-CSharp", true);

        [TestCase(-8.01f, -2, 1)]
        [TestCase(-8f, -1, 2)]
        [TestCase(-0.01f, -1, 2)]
        [TestCase(0f, 0, 0)]
        [TestCase(7.999f, 0, 0)]
        [TestCase(8f, 1, 1)]
        public void 좌표는_음수에서도_floor와_양수_슬롯을_사용한다(float value, int tile, int slot)
        {
            Assert.That(World.GetMethod("TileCoordinate", StaticMethod).Invoke(null, new object[] { value }), Is.EqualTo(tile));
            Assert.That(World.GetMethod("TileIndex", StaticMethod).Invoke(null, new object[] { tile, 3 }), Is.EqualTo(slot));
        }

        [TestCase(0, false)]
        [TestCase(30, false)]
        [TestCase(50, false)]
        [TestCase(30, true)]
        public void 경사에_투영한_발바닥을_브러시_좌표로_복원한다(float slope, bool left)
        {
            var normal = Quaternion.Euler(slope, 0, 0) * Vector3.up;
            var forward = Quaternion.Euler(0, 72, 0) * Vector3.forward;
            var size = new Vector2(0.13f, 0.29f);
            var args = new object[] { normal, forward, size, left, null, null };
            Assert.That(World.GetMethod("TryProject", StaticMethod).Invoke(null, args), Is.EqualTo(true));
            var inverse = (Vector4)args[4];
            var f = Vector3.ProjectOnPlane(forward, normal).normalized;
            var r = Vector3.Cross(normal, f) * (left ? -size.x : size.x);
            Vector3 point = r * 0.35f - f * size.y * 0.23f;
            Assert.That(inverse.x * point.x + inverse.y * point.z, Is.EqualTo(0.35f).Within(0.00001f));
            Assert.That(inverse.z * point.x + inverse.w * point.z, Is.EqualTo(-0.23f).Within(0.00001f));
        }

        [Test]
        public void 수직_표면의_퇴화_투영은_거부한다()
        {
            var args = new object[] { Vector3.right, Vector3.forward, new Vector2(0.13f, 0.29f), false, null, null };
            Assert.That(World.GetMethod("TryProject", StaticMethod).Invoke(null, args), Is.EqualTo(false));
        }

        [Test]
        public void 마스크는_비대칭_가중치의_픽셀_중심을_양선형_샘플링한다()
        {
            var root = new GameObject("모래 수신면 검사");
            root.SetActive(false);
            var texture = new Texture2D(2, 3, TextureFormat.R8, false, true);
            try
            {
                var world = root.AddComponent(World);
                texture.LoadRawTextureData(new byte[] { 0, 64, 128, 192, 224, 255 });
                texture.Apply(false, false);
                World.GetField("receiverMask").SetValue(world, texture);
                World.GetField("_receiverBounds", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(world, new Vector4(12, -24, 0.5f, 0.25f));
                float Sample(float x, float z) => (float)World.GetMethod("Weight").Invoke(world, new object[] { new Vector3(x, 0, z) });
                Assert.That(Sample(12, -22), Is.EqualTo(128 / 255f).Within(0.00001f));
                Assert.That(Sample(13, -23), Is.EqualTo(96 / 255f).Within(0.00001f));
                Assert.That(Sample(14, -20), Is.EqualTo(1));
                Assert.That(Sample(14.01f, -20), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void 저장_epoch에_따른_감쇠와_max_합성은_가시_값을_유지한다()
        {
            var method = World.GetMethod("Decay", StaticMethod);
            float Decay(double age) => (float)method.Invoke(null, new object[] { age });
            const double early = 0.75, later = 1.125, view = 1.25;
            float beforeEpoch = 0.85f * Decay(-early);
            // 저장식은 2^(stampClock-storageEpoch), 읽기는 2^-(viewClock-storageEpoch)다.
            float atEpoch = beforeEpoch * Decay(1);
            float second = 0.68f * Decay(1 - later);
            float visible = Mathf.Max(atEpoch, second) * Decay(view - 1);
            Assert.That(visible, Is.EqualTo(Mathf.Max(0.85f * Decay(view - early), 0.68f * Decay(view - later))).Within(0.00001f));
            Assert.That(Decay(1), Is.EqualTo(0.5f));
        }
    }
}
