using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UPlayGround.Manager
{
    public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
    {
        private const string ITEM_ACTOR_PREFAB_PATH = "ItemActor";

        private AsyncOperationHandle<GameObject> _itemActorHandle;
        private ItemActor _itemActorPrefab;

        // 프리팹 로드 완료 전 SpawnItem 요청이 들어오면 대기열에 보관
        private readonly List<(ItemInstance instance, Vector3 position)> _pendingItems = new();

        private async void LoadItemActorPrefab()
        {
            _itemActorHandle = Addressables.LoadAssetAsync<GameObject>(ITEM_ACTOR_PREFAB_PATH);

            try
            {
                var go = await _itemActorHandle.Task;

                if (go == null)
                {
                    Debug.LogError($"[GameObjectManager] ItemActor 프리팹을 '{ITEM_ACTOR_PREFAB_PATH}' 경로에서 찾을 수 없습니다.");
                    return;
                }

                _itemActorPrefab = go.GetComponent<ItemActor>();

                if (_itemActorPrefab == null)
                {
                    Debug.LogError($"[GameObjectManager] '{ITEM_ACTOR_PREFAB_PATH}' 프리팹에 ItemActor 컴포넌트가 없습니다.");
                    return;
                }

                Debug.Log("[GameObjectManager] ItemActor 프리팹 로드 완료");
                FlushPendingItems();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameObjectManager] ItemActor 프리팹 로드 실패: {e.Message}");
            }
        }

        private void FlushPendingItems()
        {
            foreach (var (instance, position) in _pendingItems)
            {
                SpawnItemInternal(instance, position);
            }
            _pendingItems.Clear();
        }

        public void SpawnItem(ItemInstance itemInstance, Vector3 position)
        {
            if (_itemActorPrefab == null)
            {
                _pendingItems.Add((itemInstance, position));
                return;
            }

            SpawnItemInternal(itemInstance, position);
        }

        private void SpawnItemInternal(ItemInstance itemInstance, Vector3 position)
        {
            var actor = Instantiate(_itemActorPrefab, position, Quaternion.identity);
            actor.Init(itemInstance);
        }

        private void DisposeItemActorPrefab()
        {
            _pendingItems.Clear();
            if (_itemActorHandle.IsValid())
                Addressables.Release(_itemActorHandle);
        }
    }
}
