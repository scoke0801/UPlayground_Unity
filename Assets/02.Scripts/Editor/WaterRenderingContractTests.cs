using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.World;
using UPlayGround.Gameplay.World;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>실제 GPU 함수로 체적 구간·건조 차집합·겹친 수역·CPU 파도의 계약을 확인한다.</summary>
    public sealed class WaterRenderingContractTests
    {
        private Material _material;
        private RenderTexture _target;
        private Texture2D _readback;
        private GraphicsBuffer _bodies, _cells, _dry;

        [SetUp]
        public void SetUp()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/06.Shaders/Water/WaterContractProbe.shader");
            Assert.That(shader, Is.Not.Null);
            _material = new Material(shader);
            _target = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGBFloat);
            _target.Create();
            _readback = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
        }

        [TearDown]
        public void TearDown()
        {
            _bodies?.Dispose(); _cells?.Dispose(); _dry?.Dispose();
            _target.Release();
            UnityEngine.Object.DestroyImmediate(_target);
            UnityEngine.Object.DestroyImmediate(_readback);
            UnityEngine.Object.DestroyImmediate(_material);
        }

        [Test]
        public void Layout_셰이더와_동일한_레코드크기를_유지한다()
        {
            Assert.That(Marshal.SizeOf<WaterGpuRecords.Body>(), Is.EqualTo(352));
            Assert.That(Marshal.OffsetOf<WaterGpuRecords.Body>("Range").ToInt32(), Is.EqualTo(304));
            Assert.That(Marshal.SizeOf<WaterGpuRecords.Cell>(), Is.EqualTo(48));
            Assert.That(Marshal.SizeOf<WaterGpuRecords.DryVolume>(), Is.EqualTo(32));
            Assert.That(Marshal.SizeOf<WaterGpuRecords.Stamp>(), Is.EqualTo(32));
        }

        [Test]
        public void Weather_잠수하면_강수를중단하고_남은입자를지우며_재부상하면복구한다()
        {
            var root = new GameObject("수중 날씨 검사");
            root.transform.position = new Vector3(100000f, 0f, 100000f);
            var mesh = new Mesh { vertices = new[] { new Vector3(-5, 0, -5), new Vector3(-5, 0, 5), new Vector3(5, 0, -5), new Vector3(5, 0, 5) }, triangles = new[] { 0, 1, 2, 2, 1, 3 } };
            try
            {
                var filter = root.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
                var surface = root.AddComponent<UPlayGround.Components.FootstepWaterSurface>(); surface.surfaceMesh = filter; surface.RefreshMesh();
                var effects = root.AddComponent<UPlayGround.Gameplay.World.LivingWorld.EnvironmentEffects>();
                var rainObject = new GameObject("검사 강수"); rainObject.transform.SetParent(root.transform);
                var rain = rainObject.AddComponent<ParticleSystem>();
                typeof(UPlayGround.Gameplay.World.LivingWorld.EnvironmentEffects).GetField("_rain", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(effects, rain);
                effects.BindShelters(System.Array.Empty<BoxCollider>(), 1);
                var sample = new UPlayGround.Gameplay.World.LivingWorld.EnvironmentSchedule.Sample { Rain = 1f };
                effects.Apply(sample, root.transform.position + Vector3.up, true, false);
                Assert.That(rain.emission.rateOverTime.constant, Is.GreaterThan(0));
                rain.Pause();
                rain.SetParticles(new[] { new ParticleSystem.Particle { position = root.transform.position, startLifetime = 10, remainingLifetime = 10, startSize = 1 } }, 1);
                Assert.That(rain.particleCount, Is.EqualTo(1));
                effects.Apply(sample, root.transform.position + Vector3.down, true, false);
                Assert.That(rain.emission.rateOverTime.constant, Is.Zero);
                Assert.That(rain.particleCount, Is.Zero);
                effects.Apply(sample, root.transform.position + Vector3.up, true, false);
                Assert.That(rain.emission.rateOverTime.constant, Is.GreaterThan(0));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(3.2f, -7.5f, 11.3f)]
        [TestCase(10000f, 10000f, 30f)]
        public void Waves_CPU와GPU가_같은_좌표와시간을_사용한다(float x, float z, float time)
        {
            var profile = ScriptableObject.CreateInstance<WaterDynamicsProfileSO>();
            try
            {
                Bind(new[] { Body(0, 0f, 0.2f) }, Cells(0f), new WaterGpuRecords.DryVolume[1]);
                _material.SetFloat("_ProbeMode", 1);
                _material.SetVector("_ProbeOrigin", new Vector4(x, 0f, z, 0f));
                _material.SetFloat("_ProbeTime", time);
                _material.SetVector("_ProbeWave0", profile.firstWave.GetParameters());
                _material.SetVector("_ProbeWave1", profile.secondWave.GetParameters());
                Assert.That(Read().r, Is.EqualTo(profile.EvaluateHeight(new Vector2(x, z), time)).Within(0.002f));
            }
            finally { UnityEngine.Object.DestroyImmediate(profile); }
        }

        [Test]
        public void Medium_수면아래의_실제거리만_한번_감쇠한다()
        {
            Bind(new[] { Body(0, 0f, 0.2f) }, Cells(0f), new WaterGpuRecords.DryVolume[1]);
            Color result = Probe(new Vector3(0f, 2f, 0f), Vector3.down, 5f);
            Assert.That(result.a, Is.EqualTo(3f).Within(0.001f));
            Assert.That(result.r, Is.EqualTo(Mathf.Exp(-0.2f * 3f)).Within(0.001f));
        }

        [Test]
        public void Medium_건조구간은_광학길이에서_뺀다()
        {
            var body = Body(0, 0f, 0.2f); body.Range.w = 1;
            Bind(new[] { body }, Cells(0f), new[] { new WaterGpuRecords.DryVolume { Min = new Vector4(-1f, -2f, -1f, 0f), Max = new Vector4(1f, -1f, 1f, 0f) } });
            Color result = Probe(new Vector3(0f, -0.5f, 0f), Vector3.down, 3.5f);
            Assert.That(result.a, Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(result.r, Is.EqualTo(Mathf.Exp(-0.2f * 2.5f)).Within(0.001f));
        }

        [Test]
        public void Medium_겹친수역은_우선순위로_선택하고_이중감쇠하지않는다()
        {
            var first = Body(0, 0f, 0.2f);
            var second = Body(2, 0f, 0.8f); second.Authority.x = 1;
            var cells = new WaterGpuRecords.Cell[4];
            Cells(0f).CopyTo(cells, 0); Cells(0f).CopyTo(cells, 2);
            Bind(new[] { first, second }, cells, new WaterGpuRecords.DryVolume[1]);
            Color result = Probe(new Vector3(0f, 1f, 0f), Vector3.down, 4f);
            Assert.That(result.a, Is.EqualTo(3f).Within(0.001f));
            Assert.That(result.r, Is.EqualTo(Mathf.Exp(-0.8f * 3f)).Within(0.001f));
        }

        [Test]
        public void Medium_같은우선순위의_다른높이는_높은수역을_선택한다()
        {
            var cells = new WaterGpuRecords.Cell[4];
            Cells(0f).CopyTo(cells, 0); Cells(2f).CopyTo(cells, 2);
            Bind(new[] { Body(0, 0f, 0.2f), Body(2, 2f, 0.8f) }, cells, new WaterGpuRecords.DryVolume[1]);
            Color result = Probe(new Vector3(0f, 3f, 0f), Vector3.down, 6f);
            Assert.That(result.a, Is.EqualTo(5f).Within(0.001f));
            Assert.That(result.r, Is.EqualTo(Mathf.Exp(-0.8f * 5f)).Within(0.001f));
        }

        [Test]
        public void Prisms_평면볼록그리드는_원래영역의_두삼각형으로_축약한다()
        {
            var vertices = new Vector3[9];
            for (int z = 0; z < 3; z++)
            for (int x = 0; x < 3; x++) vertices[z * 3 + x] = new Vector3(x, 0f, z);
            var triangles = new int[24];
            for (int z = 0; z < 2; z++)
            for (int x = 0; x < 2; x++)
            {
                int i = (z * 2 + x) * 6, v = z * 3 + x;
                triangles[i] = v; triangles[i + 1] = v + 3; triangles[i + 2] = v + 1;
                triangles[i + 3] = v + 1; triangles[i + 4] = v + 3; triangles[i + 5] = v + 4;
            }
            AssertPrismCount(vertices, triangles, 2);
        }

        [Test]
        public void Prisms_오목영역은_볼록화해서_빈공간을_물로_채우지않는다()
        {
            var vertices = new[] { new Vector3(0,0,0), new Vector3(2,0,0), new Vector3(2,0,1),
                new Vector3(1,0,1), new Vector3(1,0,2), new Vector3(0,0,2) };
            AssertPrismCount(vertices, new[] { 0,2,1, 0,3,2, 0,5,3, 3,5,4 }, 4);
        }

        [Test]
        public void Medium_큰월드의_세분화된볼록수면은_구간예산을_초과하지않는다()
        {
            const int boundaryCount = 96;
            var vertices = new Vector3[boundaryCount];
            for (int i = 0; i < boundaryCount; i++)
            {
                float angle = i * Mathf.PI * 2f / boundaryCount;
                vertices[i] = new Vector3(10000f + Mathf.Cos(angle) * 400f, 0f, 10000f + Mathf.Sin(angle) * 400f);
            }
            var cells = new WaterGpuRecords.Cell[boundaryCount - 2];
            for (int i = 0; i < cells.Length; i++)
                cells[i] = new WaterGpuRecords.Cell { A = vertices[0], B = vertices[i + 1], C = vertices[i + 2] };
            var body = Body(0, 0f, 0.2f); body.Range.y = cells.Length; body.Authority.y = 0f;
            Bind(new[] { body }, cells, new WaterGpuRecords.DryVolume[1]);
            _material.SetFloat("_ProbeReportOverflow", 1f);
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI * 2f / 32f;
                Color result = Probe(new Vector3(10060f, -2f, 9980f), new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)), 800f);
                Assert.That(result.g, Is.Zero, "볼록 수면이 인접 삼각형마다 별도 구간을 소모합니다: " + i);
                Assert.That(result.r, Is.GreaterThan(250f));
            }
        }

        [Test]
        public void Region_같은수면의_시각메시대상을_바꾸면_렌더러캐시도_바뀐다()
        {
            var root = new GameObject("Water renderer rebind test"); root.SetActive(false);
            var profile = ScriptableObject.CreateInstance<UnderwaterProfileSO>();
            try
            {
                var first = new GameObject("First"); first.transform.SetParent(root.transform);
                var second = new GameObject("Second"); second.transform.SetParent(root.transform);
                var firstFilter = first.AddComponent<MeshFilter>(); first.AddComponent<MeshRenderer>();
                var secondFilter = second.AddComponent<MeshFilter>(); var secondRenderer = second.AddComponent<MeshRenderer>();
                var surface = root.AddComponent<UPlayGround.Components.FootstepWaterSurface>(); surface.surfaceMesh = firstFilter;
                var region = root.AddComponent<UnderwaterRegion>(); region.Bind(surface, profile);
                surface.surfaceMesh = secondFilter; region.RefreshBinding();
                Assert.That(region.SurfaceRenderer, Is.SameAs(secondRenderer));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(profile); }
        }

        private static void AssertPrismCount(Vector3[] vertices, int[] triangles, int expected)
        {
            var root = new GameObject("Prism geometry test"); root.SetActive(false);
            var mesh = new Mesh { vertices = vertices, triangles = triangles };
            var profile = ScriptableObject.CreateInstance<UnderwaterProfileSO>();
            try
            {
                var filter = root.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
                var surface = root.AddComponent<UPlayGround.Components.FootstepWaterSurface>(); surface.surfaceMesh = filter;
                var region = root.AddComponent<UnderwaterRegion>(); region.Bind(surface, profile);
                surface.RefreshMesh();
                Assert.That(region.PrismTriangleCount, Is.EqualTo(expected));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void Medium_수역바깥과_완전한건조공간은_감쇠하지않는다()
        {
            var body = Body(0, 0f, 0.2f); body.Range.w = 1;
            Bind(new[] { body }, Cells(0f), new[] { new WaterGpuRecords.DryVolume { Min = new Vector4(-2f, -4f, -2f, 0f), Max = new Vector4(2f, 0f, 2f, 0f) } });
            Assert.That(Probe(new Vector3(0f, -1f, 0f), Vector3.down, 2f).r, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Probe(new Vector3(10f, -1f, 0f), Vector3.down, 2f).r, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void Fog_공기구간과_물구간은_자기길이만_감쇠한다()
        {
            Bind(new[] { Body(0, 0f, 0.2f) }, Cells(0f), new WaterGpuRecords.DryVolume[1]);
            _material.SetFloat("_ProbeApplyAir", 1f);
            _material.SetVector("_WaterAirFog", new Vector4(2f, 0.3f, 0f, 100f));
            Color result = Probe(new Vector3(0f, -0.5f, -7f), Vector3.forward, 12f);
            Assert.That(result.a, Is.EqualTo(10f).Within(0.001f));
            Assert.That(result.r, Is.EqualTo(Mathf.Exp(-0.2f * 10f - 0.3f * 2f)).Within(0.001f));
        }

        [Test]
        public void Frame_중복합성과_다른카메라와_다음프레임을_구분한다()
        {
            using var first = new UnityEngine.Rendering.ContextContainer();
            using var other = new UnityEngine.Rendering.ContextContainer();
            Assert.That(first.GetOrCreate<WaterOpticsFrameData>().TryClaim(), Is.True);
            Assert.That(first.GetOrCreate<WaterOpticsFrameData>().TryClaim(), Is.False);
            Assert.That(other.GetOrCreate<WaterOpticsFrameData>().TryClaim(), Is.True);
            first.Dispose();
            Assert.That(first.GetOrCreate<WaterOpticsFrameData>().TryClaim(), Is.True);
        }

        private static WaterGpuRecords.Body Body(int start, float height, float extinction)
        {
            return new WaterGpuRecords.Body
            {
                WorldToLocal = Matrix4x4.identity,
                VolumeMin = new Vector4(-5f, -4f, -5f, 0f), VolumeMax = new Vector4(5f, height + 0.5f, 5f, 0f),
                Scattering = new Vector4(0.1f, 0.2f, 0.3f, 0.1f), Extinction = new Vector4(extinction, 0.1f, 0.05f, 65f),
                Range = new Vector4(start, 2f, 0f, 0f), Authority = new Vector4(0f, 1f, 0f, 0f)
            };
        }

        private static WaterGpuRecords.Cell[] Cells(float height) => new[]
        {
            new WaterGpuRecords.Cell { A = new Vector4(-5f,height,-5f,0f), B = new Vector4(-5f,height,5f,0f), C = new Vector4(5f,height,-5f,0f) },
            new WaterGpuRecords.Cell { A = new Vector4(5f,height,-5f,0f), B = new Vector4(-5f,height,5f,0f), C = new Vector4(5f,height,5f,0f) }
        };

        private void Bind(WaterGpuRecords.Body[] bodies, WaterGpuRecords.Cell[] cells, WaterGpuRecords.DryVolume[] dry)
        {
            _bodies = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bodies.Length, Marshal.SizeOf<WaterGpuRecords.Body>());
            _cells = new GraphicsBuffer(GraphicsBuffer.Target.Structured, cells.Length, 48);
            _dry = new GraphicsBuffer(GraphicsBuffer.Target.Structured, dry.Length, 32);
            _bodies.SetData(bodies); _cells.SetData(cells); _dry.SetData(dry);
            _material.SetBuffer("_WaterBodies", _bodies); _material.SetBuffer("_WaterCells", _cells); _material.SetBuffer("_WaterDryVolumes", _dry);
            _material.SetInt("_WaterBodyCount", bodies.Length);
        }

        private Color Probe(Vector3 origin, Vector3 direction, float length)
        {
            _material.SetVector("_ProbeOrigin", origin);
            _material.SetVector("_ProbeDirection", new Vector4(direction.x, direction.y, direction.z, length));
            return Read();
        }

        private Color Read()
        {
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(Texture2D.blackTexture, _target, _material);
                RenderTexture.active = _target;
                _readback.ReadPixels(new Rect(0, 0, 1, 1), 0, 0); _readback.Apply();
                return _readback.GetPixel(0, 0);
            }
            finally { RenderTexture.active = previous; }
        }
    }
}
