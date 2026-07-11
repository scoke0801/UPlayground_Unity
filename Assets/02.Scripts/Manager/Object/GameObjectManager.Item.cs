using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.Item;

namespace UPlayGround.Manager
{
    public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
    {
        private const string ITEM_ACTOR_PREFAB_PATH = "ItemActor";

        private ItemActor _itemActorPrefab;

        // 프리팹 로드 완료 전 SpawnItem 요청이 들어오면 대기열에 보관
        private readonly List<(ItemInstance instance, Vector3 position)> _pendingItems = new();

        private async UniTask LoadItemActorPrefab(CancellationToken cancellationToken)
        {
            try
            {
                GameObject go = await AssetManager.Instance.LoadGlobalAsync<GameObject>(
                    ITEM_ACTOR_PREFAB_PATH,
                    nameof(GameObjectManager),
                    cancellationToken);

                _itemActorPrefab = go.GetComponent<ItemActor>();

                if (_itemActorPrefab == null)
                {
                    Debug.LogError($"[GameObjectManager] '{ITEM_ACTOR_PREFAB_PATH}' 프리팹에 ItemActor 컴포넌트가 없습니다.");
                    return;
                }

                Debug.Log("[GameObjectManager] ItemActor 프리팹 로드 완료");
                FlushPendingItems();
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameObjectManager] ItemActor 프리팹 로드 실패: {e.Message}");
                throw;
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
            _itemActorPrefab = null;
        }
    }
}
