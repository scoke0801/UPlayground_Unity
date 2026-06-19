using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

namespace UPlayGround.Manager
{
    public enum AssetLifetime
    {
        Global,
        Scene,
    }

    public class AssetManager : BaseManager<AssetManager>, IManager, IAsyncInitializableManager
    {
        private sealed class AssetHandleRecord
        {
            public AsyncOperationHandle Handle;
            public readonly HashSet<string> Owners = new();
        }

        private SpriteAtlas _itemAtlas;
        private readonly Dictionary<(Type Type, string Key), AssetHandleRecord> _globalHandles = new();
        private readonly Dictionary<(Type Type, string Key), AssetHandleRecord> _sceneHandles = new();

        public void Init()
        {
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken) =>
            LoadItemAtlasAsync(cancellationToken);

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
            _itemAtlas = null;
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

        public Sprite GetAtlas(string key)
        {
            if (_itemAtlas == null)
            {
                Debug.LogError("[AssetManager] ItemAtlas가 준비되지 않았습니다.");
                return null;
            }

            return _itemAtlas.GetSprite(key);
        }

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
                AddOwner(cachedRecord, owner);

                if (!cachedRecord.Handle.IsDone)
                    await cachedRecord.Handle.Task;

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

            try
            {
                T result = await handle.Task;
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
                if (record.Owners.Count <= 1)
                {
                    handles.Remove(cacheKey);
                    if (handle.IsValid())
                        Addressables.Release(handle);
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

        private static void AddOwner(AssetHandleRecord record, string owner)
        {
            record.Owners.Add(string.IsNullOrWhiteSpace(owner) ? "Unknown" : owner);
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

        private async UniTask LoadItemAtlasAsync(CancellationToken cancellationToken)
        {
            const string path = "ItemAtlas";

            try
            {
                _itemAtlas = await LoadGlobalAsync<SpriteAtlas>(
                    path,
                    nameof(AssetManager),
                    cancellationToken);

                IsLoaded = true;
                Debug.Log($"[AssetManager] {path} 로드 완료");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AssetManager] {path} 로드 실패: {e.Message}");
                throw;
            }
        }
    }
}
