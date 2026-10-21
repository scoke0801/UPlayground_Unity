using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph.NarrativePlayTests
{
    /// <summary>본편 진입 전에 부팅 완료·실패·종료와 시간 배율 경계를 검사한다.</summary>
    public sealed class BootInitializationTests
    {
        private GameObject _root;
        private Component _manager;
        private Component _loader;
        private float _originalTimeScale;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
            // 실제 서비스·Addressables를 시작하지 않고 부팅 경계만 검사한다.
            _root = new GameObject(nameof(BootInitializationTests));
            _root.SetActive(false);
            _manager = _root.AddComponent(
                Type.GetType("UPlayGround.Manager.GameManager, Assembly-CSharp", true));
            _loader = _root.AddComponent(
                Type.GetType("UPlayGround.Manager.BootLoader, Assembly-CSharp", true));
            SetField(_loader, "_minimumLoadingTime", 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _originalTimeScale;
            UnityEngine.Object.DestroyImmediate(_root);
        }

        [Test]
        public void 일부_매니저가_준비되어도_전체_부팅_완료를_기다린다()
        {
            var readyManager = new TestManager();
            var pendingManager = new TestManager();
            GetField<List<IManager>>(_manager, "_registeredManagers").AddRange(
                new IManager[] { readyManager, pendingManager });
            GetField<HashSet<IManager>>(_manager, "_runtimeReadyManagers").Add(readyManager);
            GetField<HashSet<IManager>>(_manager, "_afterInitializedManagers").Add(readyManager);
            SetBootState(GameBootState.Initializing);

            IEnumerator wait = CreateWait();
            Assert.That(wait.MoveNext(), Is.True);
            Assert.That(GetProgress(), Is.InRange(0f, 0.99f));

            GetField<HashSet<IManager>>(_manager, "_runtimeReadyManagers").Add(pendingManager);
            Assert.That(wait.MoveNext(), Is.True, "비동기 작업 뒤의 후처리도 기다려야 한다.");
            GetField<HashSet<IManager>>(_manager, "_afterInitializedManagers").Add(pendingManager);
            Assert.That(wait.MoveNext(), Is.True, "전환의 단일 기준은 Ready 상태다.");

            SetBootState(GameBootState.Ready);
            Assert.That(wait.MoveNext(), Is.False);
            Assert.That(GetProgress(), Is.EqualTo(1f));
        }

        [Test]
        public void 초기화_실패는_무한_대기하지_않는다()
        {
            SetBootState(GameBootState.Initializing);
            IEnumerator wait = CreateWait();
            Assert.That(wait.MoveNext(), Is.True);
            SetField(_manager, "<InitializationFailure>k__BackingField", "DB 로드 실패");
            SetBootState(GameBootState.Failed);

            LogAssert.Expect(LogType.Error,
                "[BootLoader] GameManager 초기화 실패로 Boot을 중단합니다: DB 로드 실패");
            Assert.That(wait.MoveNext(), Is.False);
        }

        [Test]
        public void 종료_중에는_부팅_대기를_중단한다()
        {
            SetBootState(GameBootState.Initializing);
            IEnumerator wait = CreateWait();
            Assert.That(wait.MoveNext(), Is.True);
            SetBootState(GameBootState.Disposing);
            Assert.That(wait.MoveNext(), Is.False);
        }

        [UnityTest]
        public IEnumerator 일시정지_중에도_최소_로딩_시간이_끝난다()
        {
            Time.timeScale = 0f;
            SetField(_loader, "_minimumLoadingTime", 0.03f);
            SetBootState(GameBootState.Ready);
            IEnumerator wait = CreateWait();
            Assert.That(wait.MoveNext(), Is.True, "최소 연출 시간 전에는 완료되면 안 된다.");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (wait.MoveNext())
            {
                Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(2d),
                    "게임 시간 배율 때문에 부팅 대기가 멈췄다.");
                yield return null;
            }
        }

        [Test]
        public void 후처리_실패_매니저는_틱을_받지_않는다()
        {
            var manager = new TestManager { ShouldFailAfterInit = true };
            Invoke(_manager, "RegisterManager", manager);

            TargetInvocationException failure = Assert.Throws<TargetInvocationException>(
                () => Invoke(_manager, "RunAfterInit", manager));
            Assert.That(failure.InnerException, Is.TypeOf<InvalidOperationException>());
            Invoke(_manager, "Update");
            Assert.That(manager.UpdateCount, Is.Zero);
            Assert.That(GetProgress(), Is.LessThan(1f));

            manager.ShouldFailAfterInit = false;
            Invoke(_manager, "RunAfterInit", manager);
            Invoke(_manager, "RunAfterInit", manager);
            Invoke(_manager, "Update");
            Assert.That(manager.UpdateCount, Is.EqualTo(1));
            Assert.That(manager.AfterInitCount, Is.EqualTo(2),
                "실패 후 완료된 후처리를 다시 실행하면 안 된다.");
        }

        private IEnumerator CreateWait() =>
            (IEnumerator)Invoke(_loader, "WaitForInitialization", _manager);

        private void SetBootState(GameBootState state) =>
            SetField(_manager, "<BootState>k__BackingField", state);

        private float GetProgress() =>
            (float)_manager.GetType().GetProperty("InitializationProgress").GetValue(_manager);

        private static object Invoke(object target, string name, params object[] arguments) =>
            target.GetType().GetMethod(name, PrivateInstance | BindingFlags.Public)
                .Invoke(target, arguments);

        private static T GetField<T>(object target, string name) =>
            (T)target.GetType().GetField(name, PrivateInstance).GetValue(target);

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, PrivateInstance).SetValue(target, value);

        private sealed class TestManager : IManager, IUpdatableManager
        {
            public bool ShouldFailAfterInit;
            public int AfterInitCount;
            public int UpdateCount;

            public void Init() { }
            public void AfterInit()
            {
                AfterInitCount++;
                if (ShouldFailAfterInit)
                    throw new InvalidOperationException("후처리 실패");
            }
            public void Dispose() { }
            public void OnUpdate() => UpdateCount++;
            public void OnFixedUpdate() { }
            public void OnLateUpdate() { }
            public void OnSceneChanged(string sceneType) { }
        }
    }
}
