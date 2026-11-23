using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Data.UI;
using UPlayGround.UI.Guidance;

namespace UPlayGround.UI.Tests
{
    /// <summary>길안내의 경로 자료형·NavMesh 경로 제공자·나비 연출이 설계한 계약을 지키는지 검증한다.</summary>
    public sealed class GuidanceTests
    {
        private readonly List<NavMeshDataInstance> _navMeshes = new();
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (NavMeshDataInstance instance in _navMeshes)
                NavMesh.RemoveNavMeshData(instance);
            _navMeshes.Clear();

            foreach (Object created in _created)
                if (created != null)
                    Object.DestroyImmediate(created);
            _created.Clear();
        }

        [Test]
        public void 경로는_누적_거리로_위치와_방향을_표본한다()
        {
            GuidePath path = MakeLPath();

            Assert.That(path.IsComplete, Is.True);
            Assert.That(path.TotalLength, Is.EqualTo(20f).Within(0.001f));

            path.Sample(15f, out Vector3 position, out Vector3 tangent);
            Assert.That(Vector3.Distance(position, new Vector3(10f, 0f, 5f)), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(tangent, Vector3.forward), Is.LessThan(0.001f));
        }

        [Test]
        public void 경로는_같은_위치의_점을_무시하고_범위_밖_거리는_끝으로_고정한다()
        {
            var path = new GuidePath();
            path.AddPoint(Vector3.zero);
            path.AddPoint(Vector3.zero);
            path.AddPoint(new Vector3(0f, 0f, 4f));
            path.Seal();

            Assert.That(path.PointCount, Is.EqualTo(2));
            path.Sample(99f, out Vector3 position, out _);
            Assert.That(position, Is.EqualTo(new Vector3(0f, 0f, 4f)));
        }

        [Test]
        public void 한_점짜리_경로는_완성되지_않는다()
        {
            var path = new GuidePath();
            path.AddPoint(Vector3.zero);
            path.Seal();

            Assert.That(path.IsComplete, Is.False);
            Assert.That(path.Status, Is.EqualTo(GuidePathStatus.None));
        }

        [Test]
        public void 경로_투영은_진행_거리와_수평_이탈을_돌려준다()
        {
            GuidePath path = MakeLPath();

            float distance = path.ProjectDistance(new Vector3(13f, 5f, 5f), 0f, out float deviation);

            Assert.That(distance, Is.EqualTo(15f).Within(0.001f));
            Assert.That(deviation, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void 경로_투영은_이미_지나온_구간으로_되돌아가지_않는다()
        {
            GuidePath path = MakeLPath();

            float distance = path.ProjectDistance(new Vector3(5f, 0f, 0f), 12f, out _);

            Assert.That(distance, Is.GreaterThanOrEqualTo(12f));
        }

        [Test]
        public void NavMesh_위의_두_지점은_완성_경로가_된다()
        {
            AddNavMeshBox(Vector3.zero, new Vector3(40f, 0.2f, 40f));
            var provider = new NavMeshGuidePathProvider(3f, 4);
            var path = new GuidePath();

            bool found = provider.TryBuild(new Vector3(-10f, 0f, -10f), new Vector3(10f, 0f, 10f), path);

            Assert.That(found, Is.True);
            Assert.That(path.Status, Is.EqualTo(GuidePathStatus.Complete));
            Assert.That(path.TotalLength, Is.EqualTo(28.3f).Within(1.5f));
        }

        [Test]
        public void 서로_끊어진_섬_사이는_부분_경로여도_길_없음이다()
        {
            AddNavMeshBox(new Vector3(-30f, 0f, 0f), new Vector3(20f, 0.2f, 20f));
            AddNavMeshBox(new Vector3(30f, 0f, 0f), new Vector3(20f, 0.2f, 20f));
            var provider = new NavMeshGuidePathProvider(3f, 4);
            var path = new GuidePath();

            bool found = provider.TryBuild(new Vector3(-30f, 0f, 0f), new Vector3(30f, 0f, 0f), path);

            Assert.That(found, Is.False);
            Assert.That(path.Status, Is.EqualTo(GuidePathStatus.None));
        }

        [Test]
        public void NavMesh에서_멀리_떨어진_목표는_직선으로_대신하지_않고_길_없음이다()
        {
            AddNavMeshBox(Vector3.zero, new Vector3(20f, 0.2f, 20f));
            var provider = new NavMeshGuidePathProvider(3f, 4);
            var path = new GuidePath();

            bool found = provider.TryBuild(Vector3.zero, new Vector3(500f, 0f, 500f), path);

            Assert.That(found, Is.False);
            Assert.That(path.IsComplete, Is.False);
        }

        [Test]
        public void 나비는_경로를_따라_앞서_날다가_재생이_끝나면_모두_사라진다()
        {
            GuideProfileSO profile = MakeProfile();
            Transform player = MakeTransform("플레이어");
            Transform root = MakeTransform("루트");
            GuidePath path = MakeStraightPath(60f);
            var effect = new ButterflyGuideEffect();

            effect.Begin(new GuideContext(player, root, path, profile));
            Assert.That(effect.IsPlaying, Is.True);

            float farthest = 0f;
            for (float t = 0f; t < profile.duration * 0.5f; t += 0.02f)
                effect.Tick(0.02f);
            for (int i = 0; i < root.childCount; i++)
            {
                Transform agent = root.GetChild(i);
                if (agent.gameObject.activeSelf)
                    farthest = Mathf.Max(farthest, agent.position.z);
            }

            Assert.That(farthest, Is.GreaterThan(3f), "나비가 경로 앞쪽으로 나아가지 않았다");

            for (float t = 0f; t < profile.duration; t += 0.02f)
                effect.Tick(0.02f);

            Assert.That(effect.IsPlaying, Is.False);
            for (int i = 0; i < root.childCount; i++)
                Assert.That(root.GetChild(i).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void 즉시_정지하면_개체가_모두_비활성화된다()
        {
            GuideProfileSO profile = MakeProfile();
            Transform root = MakeTransform("루트");
            var effect = new ButterflyGuideEffect();

            effect.Begin(new GuideContext(MakeTransform("플레이어"), root, MakeStraightPath(40f), profile));
            effect.Tick(0.1f);
            effect.Stop(true);

            Assert.That(effect.IsPlaying, Is.False);
            for (int i = 0; i < root.childCount; i++)
                Assert.That(root.GetChild(i).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void 경로를_갈아_끼워도_개체는_이어서_난다()
        {
            GuideProfileSO profile = MakeProfile();
            Transform root = MakeTransform("루트");
            var effect = new ButterflyGuideEffect();

            effect.Begin(new GuideContext(MakeTransform("플레이어"), root, MakeStraightPath(60f), profile));
            for (int i = 0; i < 50; i++)
                effect.Tick(0.02f);

            effect.ReplacePath(MakeStraightPath(80f));
            for (int i = 0; i < 10; i++)
                effect.Tick(0.02f);

            Assert.That(effect.IsPlaying, Is.True);
        }

        [Test]
        public void 행렬_나비는_플레이어_앞부터_행렬_끝까지_경로_전체에_흩어져_흐른다()
        {
            GuideProfileSO profile = MakeStreamProfile(32);
            var effect = new ButterflyGuideEffect();

            effect.Begin(new GuideContext(MakeTransform("플레이어"), MakeTransform("루트"), MakeStraightPath(80f), profile));
            TickFor(effect, 8f);

            Assert.That(effect.Stream.AliveCount, Is.EqualTo(32), "모든 행렬 나비가 날고 있어야 한다");
            Assert.That(effect.Stream.TryGetDistanceRange(out float nearest, out float farthest), Is.True);
            Assert.That(nearest, Is.LessThan(8f), "플레이어 가까이에 나비가 없어 길의 시작이 끊겨 보인다");
            Assert.That(farthest, Is.GreaterThan(profile.streamLength * 0.7f), "행렬이 길 앞쪽까지 이어지지 않는다");
        }

        [Test]
        public void 행렬_끝에_닿은_나비는_플레이어_앞에서_다시_날아올라_행렬이_끊기지_않는다()
        {
            GuideProfileSO profile = MakeStreamProfile(24);
            var effect = new ButterflyGuideEffect();

            effect.Begin(new GuideContext(MakeTransform("플레이어"), MakeTransform("루트"), MakeStraightPath(200f), profile));
            // 가장 느린 나비도 행렬 끝까지 여러 번 오갈 만큼 진행한다.
            TickFor(effect, 25f);

            Assert.That(effect.Stream.AliveCount, Is.EqualTo(24));
            effect.Stream.TryGetDistanceRange(out float nearest, out float farthest);
            Assert.That(farthest, Is.LessThanOrEqualTo(profile.streamLength + profile.streamSpeed * 0.1f), "행렬 끝을 지나서도 계속 날아간다");
            Assert.That(nearest, Is.LessThan(8f));
        }

        [Test]
        public void 페이드아웃에_들어서면_행렬은_새로_날아오르지_않고_끝나면_모두_비워진다()
        {
            GuideProfileSO profile = MakeStreamProfile(24);
            profile.duration = 12f;
            profile.fadeOutDuration = 3f;
            var effect = new ButterflyGuideEffect();

            effect.Begin(new GuideContext(MakeTransform("플레이어"), MakeTransform("루트"), MakeStraightPath(80f), profile));
            TickFor(effect, 5f);
            effect.Stop(false);

            int previous = effect.Stream.AliveCount;
            effect.Stream.TryGetDistanceRange(out float nearestAtStop, out _);
            for (int i = 0; i < 100 && effect.IsPlaying; i++)
            {
                effect.Tick(0.02f);
                Assert.That(effect.Stream.AliveCount, Is.LessThanOrEqualTo(previous), "페이드아웃 중에 나비가 다시 늘어났다");
                previous = effect.Stream.AliveCount;
                if (effect.Stream.TryGetDistanceRange(out float nearest, out _))
                    Assert.That(nearest, Is.GreaterThanOrEqualTo(nearestAtStop), "페이드아웃 중에 플레이어 앞에서 다시 날아올랐다");
            }

            TickFor(effect, profile.duration);
            Assert.That(effect.IsPlaying, Is.False);
            Assert.That(effect.Stream.AliveCount, Is.EqualTo(0));
        }

        [Test]
        public void 다시_재생해도_행렬_표시_개체를_새로_만들지_않고_재사용한다()
        {
            GuideProfileSO profile = MakeStreamProfile(16);
            profile.streamPrefab = MakeParticlePrefab("행렬 프리팹");
            profile.afterglowPrefab = MakeParticlePrefab("잔광 프리팹");
            Transform root = MakeTransform("루트");
            var effect = new ButterflyGuideEffect();

            effect.Begin(new GuideContext(MakeTransform("플레이어"), root, MakeStraightPath(60f), profile));
            TickFor(effect, 1f);
            int childCount = root.childCount;
            effect.Stop(true);

            effect.Begin(new GuideContext(MakeTransform("플레이어"), root, MakeStraightPath(60f), profile));
            TickFor(effect, 1f);

            Assert.That(root.childCount, Is.EqualTo(childCount), "재생할 때마다 표시 개체가 늘어난다");
            Assert.That(effect.Stream.AliveCount, Is.GreaterThan(0));
        }

        [Test]
        public void 행렬_재생_중_프레임_진행은_관리_힙을_할당하지_않는다()
        {
            GuideProfileSO profile = MakeStreamProfile(64);
            profile.streamPrefab = MakeParticlePrefab("행렬 프리팹");
            profile.afterglowPrefab = MakeParticlePrefab("잔광 프리팹");
            var effect = new ButterflyGuideEffect();

            effect.Begin(new GuideContext(MakeTransform("플레이어"), MakeTransform("루트"), MakeStraightPath(80f), profile));
            TickFor(effect, 4f);

            // NUnit의 Is와 이름이 겹치지 않도록 확장 메서드를 직접 호출한다.
            Assert.That(() => effect.Tick(0.02f), UnityEngine.TestTools.Constraints.ConstraintExtensions.AllocatingGCMemory(Is.Not));
        }

        [Test]
        public void 경로를_갈아_끼우면_행렬_나비는_새_경로_위에서_이어서_난다()
        {
            GuideProfileSO profile = MakeStreamProfile(20);
            var effect = new ButterflyGuideEffect();

            effect.Begin(new GuideContext(MakeTransform("플레이어"), MakeTransform("루트"), MakeStraightPath(60f), profile));
            TickFor(effect, 6f);
            int aliveBefore = effect.Stream.AliveCount;

            GuidePath shortPath = MakeStraightPath(20f);
            effect.ReplacePath(shortPath);
            effect.Stream.TryGetDistanceRange(out _, out float farthest);

            Assert.That(effect.Stream.AliveCount, Is.EqualTo(aliveBefore));
            Assert.That(farthest, Is.LessThanOrEqualTo(shortPath.TotalLength + 0.001f), "새 경로 길이를 넘는 위치에 남아 있다");
            TickFor(effect, 1f);
            Assert.That(effect.IsPlaying, Is.True);
        }

        [Test]
        public void 연출_종류에_맞는_구현을_만든다()
        {
            Assert.That(GuideEffectFactory.Create(GuideEffectKind.Butterfly), Is.TypeOf<ButterflyGuideEffect>());
            Assert.That(GuideEffectFactory.Create(GuideEffectKind.Wind), Is.TypeOf<WindGuideEffect>());
        }

        [Test]
        public void 바람은_같은_계약으로_길을_따라_흐르고_끝나면_모두_사라진다()
        {
            GuideProfileSO profile = MakeWindProfile();
            Transform root = MakeTransform("루트");
            IGuideEffect effect = GuideEffectFactory.Create(GuideEffectKind.Wind);

            effect.Begin(new GuideContext(MakeTransform("플레이어"), root, MakeStraightPath(60f), profile));
            Assert.That(effect.IsPlaying, Is.True);

            for (float t = 0f; t < 1.5f; t += 0.02f)
                effect.Tick(0.02f);

            float farthest = 0f;
            for (int i = 0; i < root.childCount; i++)
                if (root.GetChild(i).gameObject.activeSelf)
                    farthest = Mathf.Max(farthest, root.GetChild(i).position.z);
            Assert.That(farthest, Is.GreaterThan(3f), "바람이 경로 앞쪽으로 흐르지 않았다");

            for (float t = 0f; t < 20f && effect.IsPlaying; t += 0.02f)
                effect.Tick(0.02f);

            Assert.That(effect.IsPlaying, Is.False);
            for (int i = 0; i < root.childCount; i++)
                Assert.That(root.GetChild(i).gameObject.activeSelf, Is.False);
        }

        [Test]
        public void 바람_프리팹에_꼬리가_없으면_시작하지_않는다()
        {
            GuideProfileSO profile = MakeProfile();
            IGuideEffect effect = GuideEffectFactory.Create(GuideEffectKind.Wind);
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("TrailRenderer"));

            effect.Begin(new GuideContext(MakeTransform("플레이어"), MakeTransform("루트"), MakeStraightPath(20f), profile));

            Assert.That(effect.IsPlaying, Is.False);
        }

        #region 도우미

        // 행렬은 개체마다 속도·흩어짐을 무작위로 정하므로, 분포를 검사하는 테스트가 실행마다 흔들리지 않게 시드를 고정한다.
        private GuideProfileSO MakeStreamProfile(int count)
        {
            UnityEngine.Random.InitState(20261122);
            GuideProfileSO profile = MakeProfile();
            profile.streamCount = count;
            profile.duration = 60f;
            return profile;
        }

        private GameObject MakeParticlePrefab(string name)
        {
            var prefab = new GameObject(name);
            prefab.AddComponent<ParticleSystem>();
            _created.Add(prefab);
            return prefab;
        }

        private static void TickFor(IGuideEffect effect, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.02f)
                effect.Tick(0.02f);
        }

        private GuideProfileSO MakeWindProfile()
        {
            GuideProfileSO profile = MakeProfile();
            profile.effectKind = GuideEffectKind.Wind;
            var trailPrefab = new GameObject("바람 프리팹");
            trailPrefab.AddComponent<TrailRenderer>().time = 0.5f;
            profile.agentPrefab = trailPrefab;
            _created.Add(trailPrefab);
            return profile;
        }

        private static GuidePath MakeLPath()
        {
            var path = new GuidePath();
            path.AddPoint(new Vector3(0f, 0f, 0f));
            path.AddPoint(new Vector3(10f, 0f, 0f));
            path.AddPoint(new Vector3(10f, 0f, 10f));
            path.Seal();
            return path;
        }

        private static GuidePath MakeStraightPath(float length)
        {
            var path = new GuidePath();
            for (float z = 0f; z <= length; z += 2f)
                path.AddPoint(new Vector3(0f, 0f, z));
            path.Seal();
            return path;
        }

        private GuideProfileSO MakeProfile()
        {
            var profile = ScriptableObject.CreateInstance<GuideProfileSO>();
            profile.agentPrefab = new GameObject("나비 프리팹");
            _created.Add(profile);
            _created.Add(profile.agentPrefab);
            return profile;
        }

        private Transform MakeTransform(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.transform;
        }

        private void AddNavMeshBox(Vector3 center, Vector3 size)
        {
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = size,
                transform = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one),
                area = 0,
            };
            var bounds = new Bounds(center, size + new Vector3(10f, 10f, 10f));
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(
                NavMesh.GetSettingsByID(0), new List<NavMeshBuildSource> { source }, bounds, Vector3.zero, Quaternion.identity);
            _created.Add(data);
            _navMeshes.Add(NavMesh.AddNavMeshData(data));
        }

        #endregion
    }
}
