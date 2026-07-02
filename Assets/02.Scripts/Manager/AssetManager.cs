using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UPlayGround.Manager
{
    public enum AssetLifetime
    {
        Global,
        Scene,
    }

    public class AssetManager : BaseManager<AssetManager>, IManager, IAsyncInitializableManager
    {
        // 에디터 첫 임포트/번들 빌드가 느린 환경의 오탐(타임아웃)을 피하려 에디터에서는 더 길게 둔다.
#if UNITY_EDITOR
        private const int LOAD_TIMEOUT_SECONDS = 60;
#else
        private const int LOAD_TIMEOUT_SECONDS = 15;
#endif

        private sealed class AssetHandleRecord
        {
            public AsyncOperationHandle Handle;
            public readonly HashSet<string> Owners = new();
        }

        private readonly Dictionary<(Type Type, string Key), AssetHandleRecord> _globalHandles = new();
        private readonly Dictionary<(Type Type, string Key), AssetHandleRecord> _sceneHandles = new();

        public void Init()
        {
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            IsLoaded = true;
            return UniTask.CompletedTask;
        }

        public void AfterInit()
        {
            
        }
        
        public void Dispose()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            LogHandleStatistics();
#endif
            ReleaseAll(_sceneHandles, AssetLifetime.Scene);
            ReleaseAll(_globalHandles, AssetLifetime.Global);
            IsLoaded = false;
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }

        public void OnSceneChanged(string sceneType) { }

        public bool IsLoaded { get; private set; } = false;

        /// <summary>
        /// 애플리케이션 수명 동안 공유할 Addressable 에셋을 타입+키 기준으로 한 번만 로드한다.
        /// 모든 핸들은 AssetManager가 소유하며 Dispose에서 일괄 해제한다.
        /// </summary>
        public async UniTask<T> LoadGlobalAsync<T>(
            string key,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            return await LoadAsync<T>(
                key,
                AssetLifetime.Global,
                owner: null,
                cancellationToken);
        }

        public async UniTask<T> LoadGlobalAsync<T>(
            string key,
            string owner,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            return await LoadAsync<T>(
                key,
                AssetLifetime.Global,
                owner,
                cancellationToken);
        }

        public async UniTask<T> LoadSceneAsync<T>(
            string key,
            string owner,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            return await LoadAsync<T>(
                key,
                AssetLifetime.Scene,
                owner,
                cancellationToken);
        }

        public async UniTask<T> LoadAsync<T>(
            string key,
            AssetLifetime lifetime,
            string owner,
            CancellationToken cancellationToken = default)
            where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Addressable 키가 비어 있습니다.", nameof(key));

            var cacheKey = (typeof(T), key);
            Dictionary<(Type Type, string Key), AssetHandleRecord> handles =
                GetHandleTable(lifetime);

            if (handles.TryGetValue(cacheKey, out AssetHandleRecord cachedRecord))
            {
                bool cachedOwnerAdded = AddOwner(cachedRecord, owner);

                if (!cachedRecord.Handle.IsDone)
                {
                    using var cachedTimeoutCancellation =
                        CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cachedTimeoutCancellation.CancelAfter(
                        TimeSpan.FromSeconds(LOAD_TIMEOUT_SECONDS));

                    try
                    {
                        await cachedRecord.Handle.ToUniTask(
                            cancellationToken: cachedTimeoutCancellation.Token);
                    }
                    catch (OperationCanceledException)
                        when (!cancellationToken.IsCancellationRequested)
                    {
                        // 타임아웃으로 에셋을 받지 못했으므로 호출자는 ReleaseAsset을 호출하지 않는다.
                        // 이번 호출로 추가한 소유자를 되돌리지 않으면 refcount가 0에 도달하지 못해
                        // 해당 에셋이 영구히 해제되지 않는다(신규 로드 경로와 대칭 정리).
                        if (cachedOwnerAdded)
                            RemoveOwner(cachedRecord, owner);

                        throw new TimeoutException(
                            $"[AssetManager] 캐시된 '{key}' ({typeof(T).Name}) 로드가 " +
                            $"{LOAD_TIMEOUT_SECONDS}초를 초과했습니다. " +
                            $"수명={lifetime}, 소유자={FormatOwners(cachedRecord)}");
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                T cachedResult = cachedRecord.Handle.Result as T;
                if (cachedResult == null)
                    throw new InvalidOperationException(
                        $"[AssetManager] 캐시된 '{key}' ({typeof(T).Name}) 결과가 null입니다.");

                return cachedResult;
            }

            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
            var record = new AssetHandleRecord { Handle = handle };
            AddOwner(record, owner);
            handles.Add(cacheKey, record);

            using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);
            timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(LOAD_TIMEOUT_SECONDS));

            try
            {
                T result = await handle.ToUniTask(
                    cancellationToken: timeoutCancellation.Token);
                cancellationToken.ThrowIfCancellationRequested();

                if (result == null)
                    throw new InvalidOperationException(
                        $"[AssetManager] '{key}' ({typeof(T).Name}) 로드 결과가 null입니다.");

                Debug.Log(
                    $"[AssetManager] 로드 완료: {lifetime} / {typeof(T).Name} / {key} / " +
                    $"소유자={FormatOwners(record)}");
                return result;
            }
            catch (OperationCanceledException)
            {
                bool timedOut = !cancellationToken.IsCancellationRequested;

                if (record.Owners.Count <= 1)
                {
                    handles.Remove(cacheKey);
                    if (handle.IsValid())
                        Addressables.Release(handle);
                }

                if (timedOut)
                {
                    throw new TimeoutException(
                        $"[AssetManager] '{key}' ({typeof(T).Name}) 로드가 " +
                        $"{LOAD_TIMEOUT_SECONDS}초를 초과했습니다. " +
                        $"수명={lifetime}, 소유자={FormatOwners(record)}");
                }

                throw;
            }
            catch
            {
                handles.Remove(cacheKey);
                if (handle.IsValid())
                    Addressables.Release(handle);
                throw;
            }
        }

        public void ReleaseSceneAssets()
        {
            ReleaseAll(_sceneHandles, AssetLifetime.Scene);
        }

        public void LogHandleStatistics()
        {
            Debug.Log(
                $"[AssetManager] 활성 핸들: Global={_globalHandles.Count}, " +
                $"Scene={_sceneHandles.Count}");
            LogHandleTable(_globalHandles, AssetLifetime.Global);
            LogHandleTable(_sceneHandles, AssetLifetime.Scene);
        }

        private Dictionary<(Type Type, string Key), AssetHandleRecord> GetHandleTable(
            AssetLifetime lifetime) =>
            lifetime == AssetLifetime.Scene ? _sceneHandles : _globalHandles;

        // 반환값: 이번 호출로 새로 추가된 소유자면 true, 이미 등록돼 있던 소유자면 false.
        // 실패 경로에서 "내가 추가한 소유자"만 정확히 되돌리기 위해 사용한다.
        private static bool AddOwner(AssetHandleRecord record, string owner)
        {
            return record.Owners.Add(string.IsNullOrWhiteSpace(owner) ? "Unknown" : owner);
        }

        private static void RemoveOwner(AssetHandleRecord record, string owner)
        {
            record.Owners.Remove(string.IsNullOrWhiteSpace(owner) ? "Unknown" : owner);
        }

        private static string FormatOwners(AssetHandleRecord record) =>
            string.Join(", ", record.Owners);

        private static void ReleaseAll(
            Dictionary<(Type Type, string Key), AssetHandleRecord> handles,
            AssetLifetime lifetime)
        {
            foreach (var pair in handles)
            {
                if (pair.Value.Handle.IsValid())
                    Addressables.Release(pair.Value.Handle);

                Debug.Log(
                    $"[AssetManager] 해제: {lifetime} / {pair.Key.Type.Name} / " +
                    $"{pair.Key.Key} / 소유자={FormatOwners(pair.Value)}");
            }

            handles.Clear();
        }

        private static void LogHandleTable(
            Dictionary<(Type Type, string Key), AssetHandleRecord> handles,
            AssetLifetime lifetime)
        {
            foreach (var pair in handles)
            {
                Debug.Log(
                    $"[AssetManager] [{lifetime}] {pair.Key.Type.Name} / {pair.Key.Key} / " +
                    $"상태={pair.Value.Handle.Status} / 소유자={FormatOwners(pair.Value)}");
            }
        }

    }
}
