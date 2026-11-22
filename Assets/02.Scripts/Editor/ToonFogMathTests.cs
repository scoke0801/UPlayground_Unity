using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.EditorTools.Tests
{
    public sealed class ToonFogMathTests
    {
        private Material _probe;
        private RenderTexture _target;
        private Texture2D _readback;

        [SetUp]
        public void Prepare()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null
                || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat))
                Assert.Ignore("부동소수 GPU 렌더 타깃이 필요합니다.");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/06.Shaders/ToonFog/ToonFogMathProbe.shader");
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.isSupported, Is.True);
            _probe = new Material(shader);
            _target = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _target.Create();
            _readback = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
        }

        [TearDown]
        public void Release()
        {
            UnityEngine.Object.DestroyImmediate(_probe);
            UnityEngine.Object.DestroyImmediate(_target);
            UnityEngine.Object.DestroyImmediate(_readback);
        }

        [TestCase(-12, -12, 100, 0, 12, TestName = "M02_기준_아래_수평")]
        [TestCase(27, 27, 100, 4, 12, TestName = "M03_기준_위_수평")]
        [TestCase(27, 27.00001, 100, 4, 12, TestName = "M03_수평에_가까운_시선")]
        [TestCase(-6, 86, 130, 4, 7, TestName = "M04_교차_상향")]
        [TestCase(86, -6, 130, 4, 7, TestName = "M04_교차_하향")]
        [TestCase(8, 108, 100, 12, 14, TestName = "M13_보호_구간_원점_이동")]
        [TestCase(1000, 4900, 12000, 0, 0.01, TestName = "M14_극한_고도와_스케일")]
        public void HeightMatchesIndependentQuadrature(double start, double end, double length, double baseline, double scale)
        {
            double reference = IntegrateMidpoints(start, end, length, baseline, scale);
            Color result = Height(start, end, length, baseline, scale);
            Assert.That(result.r, Is.EqualTo(reference).Within(1e-5 + 1e-4 * reference));
            Assert.That(result.g, Is.EqualTo(Math.Exp(-0.012 * reference)).Within(0.001));
        }

        [Test]
        public void M06_TransmittanceMultipliesAcrossPureSegments()
        {
            float full = Height(-12, 54, 75, 2, 6).g;
            float first = Height(-12, 10, 25, 2, 6).g;
            float last = Height(10, 54, 50, 2, 6).g;
            Assert.That(full, Is.EqualTo(first * last).Within(0.001));
        }

        [Test]
        public void M08_BoxBetweenExternalEndpointsAndOccludedBox()
        {
            var start = new Vector3(0, 0, -10);
            Assert.That(Intersection(start, new Vector3(0, 0, 10), Matrix4x4.identity, false), Is.EqualTo(2).Within(1e-4));
            Assert.That(Intersection(start, new Vector3(0, 0, -2), Matrix4x4.identity, false), Is.Zero);
            Assert.That(Intersection(start + Vector3.right * 2, Vector3.forward * 10 + Vector3.right * 2,
                Matrix4x4.identity, false), Is.Zero);
        }

        [Test]
        public void M09_SphereInsideTangentAndThinVolume()
        {
            Assert.That(Intersection(Vector3.zero, Vector3.forward * 10, Matrix4x4.identity, true), Is.EqualTo(1).Within(1e-4));
            Assert.That(Intersection(new Vector3(1, 0, -10), new Vector3(1, 0, 10), Matrix4x4.identity, true), Is.Zero);
            var thin = Matrix4x4.Scale(new Vector3(2, 2, 0.001f));
            Assert.That(Intersection(Vector3.back * 2, Vector3.forward * 2, thin, true), Is.EqualTo(0.002f).Within(1e-5));
        }

        [Test]
        public void M10_RotationAndScalePreserveWorldMetres()
        {
            var transform = Matrix4x4.TRS(new Vector3(12, 3, -5), Quaternion.Euler(30, 48, -10), new Vector3(5, 2, 3));
            Vector3 a = transform.MultiplyPoint3x4(Vector3.left * 2);
            Vector3 b = transform.MultiplyPoint3x4(Vector3.right * 2);
            Assert.That(Intersection(a, b, transform, false), Is.EqualTo(10).Within(1e-4));
            Assert.That(Intersection(a, b, transform, true), Is.EqualTo(10).Within(1e-4));
        }

        [Test]
        public void M01_M05_M07_M11_M12_EvaluationAndOpacity()
        {
            try
            {
                Shader.SetGlobalTexture("_UPGToonFogPalette", Texture2D.whiteTexture);
                Shader.SetGlobalVector("_UPGToonFogColors", new Vector4(20, 500, 0, 0));
                Shader.SetGlobalVector("_UPGToonFogHeight", new Vector4(0, 0, 12, 0));
                Shader.SetGlobalVector("_UPGToonFogHeightTint", Vector4.one);
                Shader.SetGlobalVector("_UPGToonFogEnvironmentTint", Vector4.one);
                Shader.SetGlobalInt("_UPGToonFogLocalCount", 0);
                Shader.SetGlobalFloat("_UPGToonFogEnabled", 0);
                AssertFog(Evaluate(108, 1, 1), 1, "M01 disabled");
                Shader.SetGlobalFloat("_UPGToonFogEnabled", 1);
                AssertFog(Evaluate(108, 0, 1), 1, "M01 zero density");
                AssertFog(Evaluate(0, 1, 1), 1, "M01 zero length");
                float halfDensity = (float)Math.Log(2) / 100;
                AssertFog(Evaluate(108, halfDensity, 1), 0.5f, "M05 protected distance");
                Assert.That(Evaluate(250, halfDensity, 1).a, Is.LessThan(Evaluate(108, halfDensity, 1).a), "M07");
                Shader.SetGlobalVector("_UPGToonFogHeight", new Vector4(halfDensity, 0, 12, 0));
                AssertFog(Evaluate(108, halfDensity, 1), 0.25f, "M11 same color");
                AssertFog(Evaluate(108, halfDensity, 0.2f), 0.8f, "M12 cap");
                AssertFog(Evaluate(108, halfDensity, 0), 1, "M12 zero cap");
            }
            finally
            {
                Shader.SetGlobalFloat("_UPGToonFogEnabled", 0);
                Shader.SetGlobalInt("_UPGToonFogLocalCount", 0);
                Shader.SetGlobalTexture("_UPGToonFogPalette", Texture2D.blackTexture);
            }
        }

        private Color Evaluate(float distance, float density, float cap)
        {
            Shader.SetGlobalVector("_UPGToonFogDistance", new Vector4(density, 8, 2000, cap));
            return Read(Vector4.zero, new Vector4(0, 0, distance, 0), new Vector4(0, 0, 0, 3), Matrix4x4.identity);
        }

        private static void AssertFog(Color color, float transmission, string label)
        {
            Assert.That(color.a, Is.EqualTo(transmission).Within(0.001), label);
            Assert.That(color.r, Is.EqualTo(1 - transmission).Within(0.001), label);
        }

        private float Intersection(Vector3 a, Vector3 b, Matrix4x4 transform, bool sphere) =>
            Read(a, b, new Vector4(0, 0, 0, sphere ? 2 : 1), transform.inverse).r;

        private Color Height(double start, double end, double length, double baseline, double scale) =>
            Read(new Vector4((float)start, (float)end, (float)length, 0),
                new Vector4((float)baseline, (float)scale, 0.012f, 0), Vector4.zero, Matrix4x4.identity);

        private Color Read(Vector4 a, Vector4 b, Vector4 parameters, Matrix4x4 inverse)
        {
            _probe.SetVector("_ProbeA", a);
            _probe.SetVector("_ProbeB", b);
            _probe.SetVector("_ProbeParameters", parameters);
            _probe.SetMatrix("_ProbeInverse", inverse);
            var previous = RenderTexture.active;
            using var command = new CommandBuffer();
            try
            {
                command.SetRenderTarget(_target);
                command.DrawProcedural(Matrix4x4.identity, _probe, 0, MeshTopology.Triangles, 3, 1);
                Graphics.ExecuteCommandBuffer(command);
                RenderTexture.active = _target;
                _readback.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
                return _readback.GetPixel(0, 0);
            }
            finally { RenderTexture.active = previous; }
        }

        private static double IntegrateMidpoints(double start, double end, double length, double baseline, double scale)
        {
            const int steps = 65536;
            double integral = 0;
            for (int i = 0; i < steps; i++)
            {
                double height = start + (end - start) * (i + 0.5) / steps;
                integral += Math.Exp(-Math.Max(height - baseline, 0) / scale);
            }
            return integral * length / steps;
        }
    }
}
