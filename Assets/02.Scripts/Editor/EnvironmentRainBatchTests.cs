using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Gameplay.World.LivingWorld;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>지붕 묶음 조회의 자기 충돌 제외·포화·볼륨 이동·수명 경계를 기존 동기 조회와 대조한다.</summary>
    public sealed class EnvironmentRainBatchTests
    {
        private readonly List<GameObject> _created = new();
        private EnvironmentEffects _effects;
        private static readonly Vector3 Origin = new(5000, 2000, 5000);

        [SetUp]
        public void SetUp()
        {
            // Test Runner가 소유한 물리 씬에서 이번 fixture의 오브젝트만 만들고 제거한다.
            _effects = Create("날씨", Origin).AddComponent<EnvironmentEffects>();
            _effects.BindShelters(Array.Empty<BoxCollider>(), 1);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var target in _created) if (target != null) UnityEngine.Object.DestroyImmediate(target);
            _created.Clear();
        }

        [Test]
        public void BatchedPositionsFollowParentTransformAfterSwapRemovalAndSlotReuse()
        {
            var parent = Create("강수 앵커 부모", Origin).transform;
            var firstPoint = Create("첫 앵커", Origin).transform;
            var secondPoint = Create("둘째 앵커", Origin).transform;
            firstPoint.SetParent(parent, false); firstPoint.localPosition = new Vector3(1, 2, 3);
            secondPoint.SetParent(parent, false); secondPoint.localPosition = new Vector3(-2, 1, 4);
            using var batch = new EnvironmentRainBatch();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var register = typeof(EnvironmentRainBatch).GetMethod("RegisterPositions", flags);
            var release = typeof(EnvironmentRainBatch).GetMethod("ReleasePositions", flags);
            var read = typeof(EnvironmentRainBatch).GetMethod("TryReadPosition", flags);
            object first = register.Invoke(batch, new object[] { new[] { firstPoint } });
            object second = register.Invoke(batch, new object[] { new[] { secondPoint } });
            Action<object, Vector3> check = (registration, expected) =>
            {
                object[] args = { registration, 0, Vector3.zero };
                Assert.That(read.Invoke(batch, args), Is.True);
                Assert.That(Vector3.Distance((Vector3)args[2], expected), Is.LessThan(0.0001f));
            };
            using (batch.Begin()) { check(first, firstPoint.position); check(second, secondPoint.position); batch.Complete(); }
            release.Invoke(batch, new[] { first });
            parent.SetPositionAndRotation(Origin + Vector3.up * 5, Quaternion.Euler(13, 42, 7));
            parent.localScale = new Vector3(2, 0.5f, 1.5f);
            object reused = register.Invoke(batch, new object[] { new[] { firstPoint } });
            using (batch.Begin())
            {
                object[] old = { first, 0, Vector3.zero };
                Assert.That(read.Invoke(batch, old), Is.False, "반환된 등록은 슬롯이 재사용되어도 읽지 못한다.");
                check(second, secondPoint.position); check(reused, firstPoint.position); batch.Complete();
            }
        }

        [Test]
        public void SharedPositionReferencesAndDisposedRegistrationsDoNotStealNewOwners()
        {
            var point = Create("공유 앵커", Origin).transform;
            using var batch = new EnvironmentRainBatch();
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var register = typeof(EnvironmentRainBatch).GetMethod("RegisterPositions", flags);
            var release = typeof(EnvironmentRainBatch).GetMethod("ReleasePositions", flags);
            var read = typeof(EnvironmentRainBatch).GetMethod("TryReadPosition", flags);
            object first = register.Invoke(batch, new object[] { new[] { point, point } });
            object second = register.Invoke(batch, new object[] { new[] { point } });
            release.Invoke(batch, new[] { first }); release.Invoke(batch, new[] { first });
            point.position += Vector3.up * 7;
            using (batch.Begin())
            {
                object[] args = { second, 0, Vector3.zero };
                Assert.That(read.Invoke(batch, args), Is.True);
                Assert.That((Vector3)args[2], Is.EqualTo(point.position)); batch.Complete();
            }
            batch.Dispose();
            object replacement = register.Invoke(batch, new object[] { new[] { point } });
            using (batch.Begin())
            {
                object[] old = { second, 0, Vector3.zero }, current = { replacement, 0, Vector3.zero };
                Assert.That(read.Invoke(batch, old), Is.False);
                Assert.That(read.Invoke(batch, current), Is.True);
                Assert.That((Vector3)current[2], Is.EqualTo(point.position)); batch.Complete();
            }
            UnityEngine.Object.DestroyImmediate(point.gameObject);
            using (batch.Begin())
            {
                object[] removed = { replacement, 0, Vector3.zero };
                Assert.That(read.Invoke(batch, removed), Is.False, "파괴된 앵커는 이전 작업 위치를 재사용하지 않는다.");
                batch.Complete();
            }
        }

        [Test]
        public void WeatherValueStaysFrozenWithinBatchAndRefreshesInAnotherBatch()
        {
            var root = Create("배치 날씨", Origin); root.SetActive(false);
            var weather = root.AddComponent<EnvironmentController>();
            var ready = typeof(EnvironmentController).GetProperty("IsReady").GetSetMethod(true);
            var current = typeof(EnvironmentController).GetProperty("Current").GetSetMethod(true);
            var read = typeof(EnvironmentRainBatch).GetMethod("GetRain",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            ready.Invoke(weather, new object[] { true });
            current.Invoke(weather, new object[] { new EnvironmentSchedule.Sample { Rain = 0.25f } });
            using var first = new EnvironmentRainBatch();
            using var second = new EnvironmentRainBatch();
            try
            {
                using (first.Begin())
                {
                    Assert.That(read.Invoke(first, new object[] { weather }), Is.EqualTo(0.25f));
                    current.Invoke(weather, new object[] { new EnvironmentSchedule.Sample { Rain = 0.75f } });
                    Assert.That(read.Invoke(first, new object[] { weather }), Is.EqualTo(0.25f));
                    first.Complete();
                }
                using (second.Begin())
                {
                    Assert.That(read.Invoke(second, new object[] { weather }), Is.EqualTo(0.75f));
                    second.Complete();
                }
                ready.Invoke(weather, new object[] { false });
                using (first.Begin())
                {
                    Assert.That(read.Invoke(first, new object[] { weather }), Is.EqualTo(0));
                    first.Complete();
                }
            }
            finally { ready.Invoke(weather, new object[] { false }); }
        }

        [Test]
        public void MultipleWeatherSheltersKeepSeparateRangesAndRefreshDisabledVolumes()
        {
            var first = Create("첫 보호 볼륨", Origin).AddComponent<BoxCollider>();
            first.isTrigger = true; first.size = Vector3.one * 4;
            var secondPosition = Origin + Vector3.right * 10;
            var second = Create("둘째 보호 볼륨", secondPosition).AddComponent<BoxCollider>();
            second.isTrigger = true; second.size = Vector3.one * 4;
            var other = Create("둘째 날씨", Origin).AddComponent<EnvironmentEffects>();
            _effects.BindShelters(new[] { first }, 0);
            other.BindShelters(new[] { second }, 0);
            using var batch = new EnvironmentRainBatch(); batch.Reserve(4);
            for (int run = 0; run < 2; run++)
            {
                using (batch.Begin())
                {
                    var own = batch.Collect(_effects, Origin);
                    var crossed = batch.Collect(other, Origin);
                    var outside = batch.Collect(_effects, secondPosition);
                    var otherOwn = batch.Collect(other, secondPosition);
                    batch.Complete();
                    Assert.That(batch.IsSheltered(own), Is.EqualTo(run == 0));
                    Assert.That(batch.IsSheltered(crossed), Is.False);
                    Assert.That(batch.IsSheltered(outside), Is.False);
                    Assert.That(batch.IsSheltered(otherOwn), Is.True);
                }
                first.enabled = false;
            }
        }

        [Test]
        public void MovedShelterBroadPhaseUsesTransformBeforePhysicsSync()
        {
            var volume = Create("이동 처마", Origin).AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = new Vector3(4, 3, 2);
            volume.transform.rotation = Quaternion.Euler(13, 37, 19);
            volume.transform.localScale = new Vector3(2, 0.7f, 1.5f);
            _effects.BindShelters(new[] { volume }, 0);
            Physics.SyncTransforms();
            volume.transform.position += Vector3.right * 50;
            Vector3 point = volume.transform.TransformPoint(new Vector3(1.9f, 1.4f, 0.9f));
            Assert.That(_effects.IsSheltered(point), Is.True);
            using var batch = new EnvironmentRainBatch(); batch.Reserve(1);
            using (batch.Begin())
            {
                var sample = batch.Collect(_effects, point);
                batch.Complete();
                Assert.That(batch.IsSheltered(sample), Is.True);
            }
        }

        [Test]
        public void OwnFirstHitStillFindsExternalRoofBehindIt()
        {
            var owner = Create("자기 소유", Origin);
            var own = Create("자기 면", Origin + Vector3.up * 2).AddComponent<BoxCollider>();
            own.transform.SetParent(owner.transform, true);
            own.size = new Vector3(2, 0.1f, 2);
            var other = Create("외부 지붕", Origin + Vector3.up * 4).AddComponent<BoxCollider>();
            other.size = new Vector3(2, 0.1f, 2);
            using var batch = new EnvironmentRainBatch(); batch.Reserve(1);
            CompareDirect(batch, new[] { Origin }, new[] { owner.transform });
            Assert.That(_effects.IsRainSheltered(Origin, owner.transform), Is.True);
            other.enabled = false;
            CompareDirect(batch, new[] { Origin }, new[] { owner.transform });
            Assert.That(_effects.IsRainSheltered(Origin, owner.transform), Is.False);
        }

        [Test]
        public void BatchedRoofsMatchDirectQueriesAndRefreshAfterMovement()
        {
            var roof = Create("지붕", Origin + Vector3.up * 3).AddComponent<BoxCollider>();
            roof.size = new Vector3(4, 1, 4);
            var volume = Create("회전 처마", Origin + Vector3.right * 10).AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = new Vector3(4, 3, 2);
            volume.transform.rotation = Quaternion.Euler(0, 37, 0);
            volume.transform.localScale = new Vector3(2, 0.7f, 1.5f);
            _effects.BindShelters(new[] { volume }, 1);
            var points = new[] { Origin, Origin, Origin + Vector3.right * 7,
                volume.transform.TransformPoint(new Vector3(1.5f, 0, 0)), Origin + Vector3.right * 30 };
            var receivers = new[] { roof.transform, null, null, null, null };
            using var batch = new EnvironmentRainBatch();
            batch.Reserve(points.Length);
            CompareDirect(batch, points, receivers);
            roof.transform.position += Vector3.right * 20;
            volume.transform.position += Vector3.right * 20;
            CompareDirect(batch, points, receivers);
            Assert.That(_effects.IsRainSheltered(points[1]), Is.False);
            _effects.BindShelters(Array.Empty<BoxCollider>(), 0);
            CompareDirect(batch, points, receivers);
        }

        [Test]
        public void UnpreparedQueryFallsBackAndOutsideScopeSeesCurrentVolume()
        {
            var volume = Create("처마", Origin).AddComponent<BoxCollider>();
            volume.isTrigger = true;
            _effects.BindShelters(new[] { volume }, 1);
            using var batch = new EnvironmentRainBatch();
            batch.Reserve(1);
            EnvironmentRainBatch.Sample oldSample;
            using (batch.Begin())
            {
                oldSample = batch.Collect(_effects, Origin);
                var outside = batch.Collect(_effects, Origin + Vector3.right * 4);
                Assert.Throws<InvalidOperationException>(() => batch.IsSheltered(outside));
                batch.Complete();
                Assert.That(batch.IsSheltered(oldSample), Is.True);
                Assert.That(batch.IsSheltered(outside), Is.False);
            }
            Assert.Throws<InvalidOperationException>(() => batch.IsSheltered(oldSample));
            using (batch.Begin())
            {
                batch.Complete();
                Assert.Throws<InvalidOperationException>(() => batch.IsSheltered(oldSample));
            }
            volume.transform.position += Vector3.right * 10;
            Assert.That(_effects.IsSheltered(Origin), Is.False);
            Assert.That(_effects.IsSheltered(volume.transform.position), Is.True);
        }

        [Test]
        public void FullOwnColliderBufferKeepsConservativeShelterAndClearsOldHits()
        {
            var owner = Create("자기 충돌", Origin);
            for (int i = 0; i < 33; i++)
            {
                var roof = Create("겹친 면", Origin + Vector3.up * (1 + i * 0.2f));
                roof.transform.SetParent(owner.transform, true);
                roof.AddComponent<BoxCollider>().size = new Vector3(1, 0.1f, 1);
            }
            using var batch = new EnvironmentRainBatch();
            batch.Reserve(2);
            var points = new[] { Origin, Origin + Vector3.right * 20 };
            var receivers = new[] { owner.transform, null };
            Physics.SyncTransforms();
            Assert.That(_effects.IsRainSheltered(Origin, owner.transform), Is.True);
            CompareDirect(batch, points, receivers);
            owner.SetActive(false);
            CompareDirect(batch, points, receivers);
            Assert.That(_effects.IsRainSheltered(Origin), Is.False);
        }

        [Test]
        public void CachedActorRainMatchesDirectSamplesAndRefreshesNextTick()
        {
            var roof = Create("지붕", Origin + Vector3.up * 3).AddComponent<BoxCollider>();
            roof.size = new Vector3(4, 1, 4);
            var weatherRoot = Create("비 상태 fixture", Origin);
            weatherRoot.SetActive(false);
            var weather = weatherRoot.AddComponent<EnvironmentController>();
            var weatherType = typeof(EnvironmentController);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            weatherType.GetField("_effects", flags).SetValue(weather, _effects);
            weatherType.GetProperty("IsReady").SetValue(weather, true);
            weatherType.GetProperty("Current").SetValue(weather, new EnvironmentSchedule.Sample { Rain = 1 });
            var root = Create("식생", Origin);
            var first = Create("지붕 아래 샘플", Origin).transform;
            var second = Create("야외 샘플", Origin + Vector3.right * 10).transform;
            first.SetParent(root.transform, true); second.SetParent(root.transform, true);
            var actor = root.AddComponent<ReactiveEnvironmentObject>();
            var actorType = typeof(ReactiveEnvironmentObject);
            actorType.GetField("_weather", flags).SetValue(actor, weather);
            actorType.GetField("_rainSamples", flags).SetValue(actor, new[] { first, second });
            actorType.GetMethod("Awake", flags).Invoke(actor, null);
            Physics.SyncTransforms();
            actor.SampleRain(out float directRain, out float directExposure);
            using var batch = new EnvironmentRainBatch(); batch.Reserve(2);
            using (batch.Begin())
            {
                actor.CollectRainSamples(batch); batch.Complete();
                actor.SampleRain(out float rain, out float exposure);
                Assert.That(rain, Is.EqualTo(directRain));
                Assert.That(exposure, Is.EqualTo(directExposure).And.EqualTo(0.5f));
            }
            second.position = Origin;
            Physics.SyncTransforms();
            actor.SampleRain(out _, out float movedExposure);
            using (batch.Begin())
            {
                actor.CollectRainSamples(batch); batch.Complete();
                actor.SampleRain(out _, out float exposure);
                Assert.That(exposure, Is.EqualTo(movedExposure).And.Zero);
            }
        }

        [Test]
        public void ScopeExceptionRestoresOrdinaryQueriesAndRejectsNesting()
        {
            using var batch = new EnvironmentRainBatch();
            Assert.Throws<InvalidOperationException>(() =>
            {
                using var scope = batch.Begin();
                Assert.Throws<InvalidOperationException>(() => batch.Begin());
                throw new InvalidOperationException("중단");
            });
            using var next = new EnvironmentRainBatch();
            using (next.Begin()) next.Complete();
            Assert.That(_effects.IsRainSheltered(Origin), Is.False);
        }

        private void CompareDirect(EnvironmentRainBatch batch, Vector3[] points, Transform[] receivers)
        {
            Physics.SyncTransforms();
            var expected = new bool[points.Length];
            var samples = new EnvironmentRainBatch.Sample[points.Length];
            for (int i = 0; i < points.Length; i++) expected[i] = _effects.IsRainSheltered(points[i], receivers[i]);
            using (batch.Begin())
            {
                for (int i = 0; i < points.Length; i++) samples[i] = batch.Collect(_effects, points[i], receivers[i]);
                batch.Complete();
                for (int i = 0; i < points.Length; i++)
                    Assert.That(batch.IsSheltered(samples[i]), Is.EqualTo(expected[i]), "샘플 " + i);
            }
        }

        private GameObject Create(string name, Vector3 position)
        {
            var result = new GameObject(name);
            _created.Add(result);
            result.transform.position = position;
            return result;
        }
    }
}
