using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Movement.Tests
{
    /// <summary>폭포 영역의 접근 물살, 낙하 경계, 하류 해제와 속도 보존을 검증한다.</summary>
    public sealed class WaterfallCurrentTests
    {
        private GameObject _root;
        private WaterfallCurrent _current;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("폭포 물살 테스트");
            _root.transform.SetPositionAndRotation(new Vector3(10000f, 20f, 10000f), Quaternion.Euler(0f, 90f, 0f));
            _current = _root.AddComponent<WaterfallCurrent>();
            var data = new SerializedObject(_current);
            data.FindProperty("_region").boundsValue = new Bounds(Vector3.zero, new Vector3(10f, 14f, 12f));
            data.FindProperty("_releaseHeight").floatValue = -5f;
            data.ApplyModifiedPropertiesWithoutUndo();
            InvokeLifecycle("OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            InvokeLifecycle("OnDisable");
            Object.DestroyImmediate(_root);
        }

        [TestCase(-3f, 0f, true, false)]
        [TestCase(0f, 0f, true, true)]
        [TestCase(2f, -4.9f, true, true)]
        [TestCase(2f, -5f, false, false)]
        [TestCase(7f, 0f, false, false)]
        public void TrySample_회전된_영역에서도_낙구와_하류해제_높이를_구분한다(
            float along, float height, bool expectedFound, bool expectedFalling)
        {
            Vector3 position = _root.transform.TransformPoint(new Vector3(0f, height, along));
            Assert.That(_current.TrySample(position, out _, out bool falling), Is.EqualTo(expectedFound));
            Assert.That(falling, Is.EqualTo(expectedFalling));
        }

        [Test]
        public void 접근할수록_하류물살이_강해지고_상류입구에서는_거의_영이다()
        {
            float previous = -1f;
            // 회전·큰 월드 좌표의 반올림이 경계 포함 여부 검사를 대신하지 않게 안쪽에서 표본을 잡는다.
            foreach (float along in new[] { -5.99f, -4f, -2f, 0f })
            {
                Assert.That(_current.TrySample(_root.transform.TransformPoint(new Vector3(0f, 0f, along)), out var velocity, out _), Is.True);
                float speed = Vector3.Dot(velocity, _root.transform.forward);
                if (previous < 0f) Assert.That(speed, Is.Zero.Within(0.001f));
                Assert.That(speed, Is.GreaterThan(previous));
                Assert.That(Vector3.Dot(velocity, _root.transform.up), Is.Zero.Within(0.001f));
                previous = speed;
            }
            Assert.That(previous, Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void 낙하물살은_역방향과_상승을_누르되_빠른낙하와_옆속도는_보존한다()
        {
            Vector3 velocity = -_root.transform.forward * 4f + Vector3.up * 2f + _root.transform.right * 3f;
            _current.ConstrainFallVelocity(ref velocity);
            Assert.That(Vector3.Dot(velocity, _root.transform.forward), Is.EqualTo(6f).Within(0.001f));
            Assert.That(velocity.y, Is.EqualTo(-3f).Within(0.001f));
            Assert.That(Vector3.Dot(velocity, _root.transform.right), Is.EqualTo(3f).Within(0.001f));
            velocity = _root.transform.forward * 10f + Vector3.down * 20f;
            Vector3 before = velocity;
            _current.ConstrainFallVelocity(ref velocity);
            Assert.That((velocity - before).sqrMagnitude, Is.LessThan(0.0001f));
        }

        [Test]
        public void 비활성화한_물살은_등록부에서_조회되지_않는다()
        {
            Assert.That(WaterfallCurrent.TryResolve(_root.transform.position, out _, out _, out _), Is.True);
            InvokeLifecycle("OnDisable");
            Assert.That(WaterfallCurrent.TryResolve(_root.transform.position, out _, out _, out _), Is.False);
        }

        private void InvokeLifecycle(string name) => typeof(WaterfallCurrent)
            .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_current, null);
    }
}
