using NUnit.Framework;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Data.Actor;

namespace UPlayGround.Movement.Tests
{
    /// <summary>수영과 발소리가 공유하는 수역 선택·체적·등록 수명을 검증한다.</summary>
    public sealed class WaterSurfaceQueryTests
    {
        private GameObject _root;
        private Mesh _mesh;
        private FootstepSurfaceProfileSO _profile;
        private static readonly Vector3 Origin = new(100000f, 0f, 100000f);

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Water query test");
            _root.SetActive(false);
            _root.transform.position = Origin;
            _mesh = new Mesh
            {
                vertices = new[] { Vector3.zero, Vector3.right * 10f, Vector3.forward * 10f },
                triangles = new[] { 0, 2, 1 }
            };
            _profile = ScriptableObject.CreateInstance<FootstepSurfaceProfileSO>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            UnityEngine.Object.DestroyImmediate(_mesh);
            UnityEngine.Object.DestroyImmediate(_profile);
        }

        [Test]
        public void Query_동일우선순위는_높은수면을_고른다()
        {
            AddSurface(2f);
            var upper = AddSurface(10f);
            _root.SetActive(true);
            Assert.That(FootstepWaterSurface.TryResolveSurface(Origin + Vector3.one, out var selected, out var point), Is.True);
            Assert.That(selected, Is.SameAs(upper));
            Assert.That(point.y, Is.EqualTo(10f));
        }

        [Test]
        public void Query_우선순위는_높이보다_먼저_적용한다()
        {
            var lower = AddSurface(2f);
            lower.priority = 1;
            AddSurface(10f);
            _root.SetActive(true);
            Assert.That(FootstepWaterSurface.TryResolveSurface(Origin + Vector3.one, out var selected, out _), Is.True);
            Assert.That(selected, Is.SameAs(lower));
        }

        [Test]
        public void Query_상류체적아래에서는_하류수면을_선택한다()
        {
            AddSurface(2f);
            var upper = AddSurface(10f);
            upper.useVolumeBounds = true;
            upper.volumeBounds = new Bounds(new Vector3(5f, -1f, 5f), new Vector3(10f, 2f, 10f));
            _root.SetActive(true);
            Assert.That(FootstepWaterSurface.TryGetSurfaceHeight(Origin + Vector3.one, out float height), Is.True);
            Assert.That(height, Is.EqualTo(2f));
        }

        [Test]
        public void Query_건조공간에서는_발소리와수영을_모두제외한다()
        {
            var surface = AddSurface(2f);
            surface.dryVolumes = new[] { new Bounds(new Vector3(1f, -0.2f, 1f), Vector3.one) };
            _root.SetActive(true);
            Vector3 ground = Origin + new Vector3(1f, 1.8f, 1f);
            Assert.That(FootstepWaterSurface.TryGetSurfaceHeight(ground, out _), Is.False);
            Assert.That(FootstepWaterSurface.TryResolve(ground, out _, out _), Is.False);
        }

        [Test]
        public void Query_선택한깊은수역을_얕은수역발소리로_대체하지않는다()
        {
            AddSurface(2f);
            AddSurface(10f);
            _root.SetActive(true);
            Assert.That(FootstepWaterSurface.TryResolve(Origin + new Vector3(1f, 1.8f, 1f), out _, out _), Is.False);
        }

        [Test]
        public void Query_비활성화와재등록과늦은메시연결을_반영한다()
        {
            _root.SetActive(true);
            var surface = AddSurface(2f);
            surface.RefreshMesh();
            Vector3 point = Origin + Vector3.one;
            Assert.That(FootstepWaterSurface.TryGetSurfaceHeight(point, out _), Is.True);
            surface.enabled = false;
            Assert.That(FootstepWaterSurface.TryGetSurfaceHeight(point, out _), Is.False);
            surface.enabled = true;
            Assert.That(FootstepWaterSurface.TryGetSurfaceHeight(point, out _), Is.True);
            surface.gameObject.SetActive(false);
            Assert.That(FootstepWaterSurface.TryGetSurfaceHeight(point, out _), Is.False);
        }

        [Test]
        public void Query_수면삼각형밖에서는_바운드안이어도_물로보지않는다()
        {
            AddSurface(2f);
            _root.SetActive(true);
            Assert.That(FootstepWaterSurface.TryGetSurfaceHeight(Origin + new Vector3(9f, 1f, 9f), out _), Is.False);
        }

        [Test]
        public void DownwardQuery_첫수면은_발소리우선순위와_독립이다()
        {
            var lower = AddSurface(2f); lower.priority = 100;
            var upper = AddSurface(10f);
            upper.useVolumeBounds = true;
            upper.volumeBounds = new Bounds(new Vector3(5,-1,5), new Vector3(10,2,10));
            _root.SetActive(true);
            Assert.That(FootstepWaterSurface.TryRaycastDown(Origin + new Vector3(1,20,1), 30, out var selected, out var point), Is.True);
            Assert.That(selected, Is.SameAs(upper));
            Assert.That(point.y, Is.EqualTo(10f));
            upper.dryVolumes = new[] { new Bounds(new Vector3(1,0,1), Vector3.one) };
            Assert.That(FootstepWaterSurface.TryRaycastDown(Origin + new Vector3(1,20,1), 30, out selected, out _), Is.True);
            Assert.That(selected, Is.SameAs(lower));
            Assert.That(FootstepWaterSurface.TryRaycastDown(Origin + new Vector3(1,1,1), 30, out _, out _), Is.False);
            Assert.That(FootstepWaterSurface.TryRaycastDown(Origin + new Vector3(1,20,1), 5, out _, out _), Is.False);
            Assert.That(FootstepWaterSurface.TryRaycastDown(new Vector3(float.NaN,20,1), 30, out _, out _), Is.False);
        }

        private FootstepWaterSurface AddSurface(float height)
        {
            var child = new GameObject("Surface");
            child.transform.SetParent(_root.transform, false);
            child.transform.localPosition = Vector3.up * height;
            var filter = child.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;
            var surface = child.AddComponent<FootstepWaterSurface>();
            surface.surfaceMesh = filter;
            surface.profile = _profile;
            return surface;
        }
    }
}
