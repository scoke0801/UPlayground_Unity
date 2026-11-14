using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UPlayGround.Manager;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>로드 결과를 받지 못한 호출자의 소유권 회수와 다른 사용자의 핸들 보존을 검사한다.</summary>
    public sealed class AssetManagerLifetimeTests
    {
        private sealed class PendingOperation : AsyncOperationBase<Texture2D>
        {
            protected override void Execute() { }
            public void Finish(Texture2D result) => Complete(result, true, string.Empty);
        }

        private const string Key = "수명 검사";
        private GameObject _root;
        private AssetManager _manager;
        private Texture2D _asset;
        private ResourceManager _resources;
        private AsyncOperationHandle<Texture2D> _handle;
        private IDictionary _table;
        private HashSet<string> _owners;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("에셋 수명 검사");
            _root.SetActive(false);
            _manager = _root.AddComponent<AssetManager>();
            _asset = new Texture2D(1, 1);
            _resources = new ResourceManager();
            _table = (IDictionary)typeof(AssetManager).GetField("_globalHandles", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_manager);
        }

        [TearDown]
        public void TearDown()
        {
            _table.Clear();
            UpdateResources();
            if (_handle.IsValid()) _resources.Release(_handle);
            _resources.Dispose();
            UnityEngine.Object.DestroyImmediate(_asset);
            UnityEngine.Object.DestroyImmediate(_root);
        }

        [Test]
        public void 이미_취소된_호출자는_기존_캐시에_소유권을_추가하지_않는다()
        {
            AddRecord(false, "기존 사용자");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Assert.Throws<OperationCanceledException>(() => _manager.LoadGlobalAsync<Texture2D>(Key, "취소된 사용자", cancellation.Token).GetAwaiter().GetResult());
            Assert.That(_owners, Is.EquivalentTo(new[] { "기존 사용자" }));
            Assert.That(_handle.IsValid(), Is.True);
            Assert.That(_table.Count, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void 캐시_결과_실패는_이번_소유권만_회수하고_다른_사용자를_보존한다(bool sameOwner)
        {
            AddRecord(true, "기존 사용자");
            Assert.Throws<InvalidOperationException>(() => _manager.LoadGlobalAsync<Texture2D>(Key, sameOwner ? "기존 사용자" : "실패한 사용자").GetAwaiter().GetResult());
            Assert.That(_owners, Is.EquivalentTo(new[] { "기존 사용자" }));
            Assert.That(_handle.IsValid(), Is.True);
            Assert.That(_table.Count, Is.EqualTo(1));
        }

        [Test]
        public void 대기_중_취소된_사용자는_완료_뒤에도_다른_소유권을_남긴다()
        {
            var operation = new PendingOperation();
            _handle = _resources.StartOperation(operation, default(AsyncOperationHandle));
            RegisterRecord("기존 사용자");
            using var cancellation = new CancellationTokenSource();
            var request = _manager.LoadGlobalAsync<Texture2D>(Key, "취소된 사용자", cancellation.Token);
            Assert.That(_owners.Count, Is.EqualTo(2));
            cancellation.Cancel();
            operation.Finish(_asset);
            UpdateResources();
            Assert.That(request.GetAwaiter().IsCompleted, Is.True);
            Assert.Throws<OperationCanceledException>(() => request.GetAwaiter().GetResult());
            Assert.That(_owners, Is.EquivalentTo(new[] { "기존 사용자" }));
            Assert.That(_manager.LoadGlobalAsync<Texture2D>(Key, "기존 사용자").GetAwaiter().GetResult(), Is.SameAs(_asset));
            Assert.That(_handle.IsValid(), Is.True);
        }

        [Test]
        public void 같은_소유자의_요청_중_하나만_취소해도_다른_요청은_완료된다()
        {
            var operation = new PendingOperation();
            _handle = _resources.StartOperation(operation, default(AsyncOperationHandle));
            RegisterRecord();
            using var cancellation = new CancellationTokenSource();
            var canceled = _manager.LoadGlobalAsync<Texture2D>(Key, "동일 사용자", cancellation.Token);
            var waiting = _manager.LoadGlobalAsync<Texture2D>(Key, "동일 사용자");
            cancellation.Cancel();
            operation.Finish(_asset);
            UpdateResources();
            Assert.Throws<OperationCanceledException>(() => canceled.GetAwaiter().GetResult());
            Assert.That(waiting.GetAwaiter().GetResult(), Is.SameAs(_asset));
            Assert.That(_owners, Is.EquivalentTo(new[] { "동일 사용자" }));
            Assert.That(_handle.IsValid(), Is.True);
            Assert.That(_table.Count, Is.EqualTo(1));
        }

        [Test]
        public void 실패한_마지막_소유자를_회수하면_캐시와_핸들을_해제한다()
        {
            AddRecord(true);
            Assert.Throws<InvalidOperationException>(() => _manager.LoadGlobalAsync<Texture2D>(Key, "실패한 사용자").GetAwaiter().GetResult());
            Assert.That(_owners, Is.Empty);
            Assert.That(_table.Count, Is.Zero);
            // 실패 작업의 지연 완료 콜백이 보유한 참조까지 반환된 뒤 최종 수명을 검사한다.
            UpdateResources();
            Assert.That(_handle.IsValid(), Is.False);
            _manager.Dispose();
        }

        [Test]
        public void 성공한_동일_소유자의_중복_조회와_반복_종료는_중복_해제하지_않는다()
        {
            AddRecord(false, "기존 사용자");
            Assert.That(_manager.LoadGlobalAsync<Texture2D>(Key, "기존 사용자").GetAwaiter().GetResult(), Is.SameAs(_asset));
            Assert.That(_owners.Count, Is.EqualTo(1));
            _manager.Dispose();
            _manager.Dispose();
            Assert.That(_table.Count, Is.Zero);
            Assert.That(_handle.IsValid(), Is.False);
        }

        private void AddRecord(bool failed, params string[] owners)
        {
            if (failed)
            {
                var expectedFailure = new Exception("의도된 검사 실패");
                var previous = ResourceManager.ExceptionHandler;
                try
                {
                    // 부팅 여부와 관계없이 이 검사가 만든 실패만 로깅에서 격리한다.
                    ResourceManager.ExceptionHandler = (operation, exception) =>
                    {
                        if (ReferenceEquals(exception, expectedFailure)) return;
                        if (previous != null) previous(operation, exception);
                        else Debug.LogException(exception);
                    };
                    _handle = _resources.CreateCompletedOperationWithException<Texture2D>(null, expectedFailure);
                }
                finally { ResourceManager.ExceptionHandler = previous; }
                Assert.That(_handle.OperationException, Is.SameAs(expectedFailure));
                Assert.That(ResourceManager.ExceptionHandler, Is.SameAs(previous));
            }
            else
            {
                _handle = _resources.CreateCompletedOperation<Texture2D>(_asset, null);
            }
            RegisterRecord(owners);
        }

        private void RegisterRecord(params string[] owners)
        {
            Type recordType = typeof(AssetManager).GetNestedType("AssetHandleRecord", BindingFlags.NonPublic);
            object record = Activator.CreateInstance(recordType);
            recordType.GetField("Handle").SetValue(record, (AsyncOperationHandle)_handle);
            _owners = (HashSet<string>)recordType.GetField("Owners").GetValue(record);
            foreach (string owner in owners) _owners.Add(owner);
            _table.Add((typeof(Texture2D), Key), record);
        }

        private void UpdateResources() => typeof(ResourceManager)
            .GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(_resources, new object[] { 0f });
    }
}
