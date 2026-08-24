using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;

namespace UPlayGround.Manager
{
    public partial class GameObjectManager : BaseManager<GameObjectManager>, IManager
    {
        private const string ITEM_ACTOR_PREFAB_PATH = "ItemActor";

        private ItemActor _itemActorPrefab;

        private readonly List<LootPresentationRequest> _pendingPresentations = new();

        private readonly struct LootPresentationRequest
        {
            public LootPresentationRequest(
                ItemInstance item,
                Vector3 position,
                int launchOrder,
                bool playsArrivalAccent)
            {
                Item = item;
                Position = position;
                LaunchOrder = launchOrder;
                PlaysArrivalAccent = playsArrivalAccent;
            }

            public ItemInstance Item { get; }
            public Vector3 Position { get; }
            public int LaunchOrder { get; }
            public bool PlaysArrivalAccent { get; }
        }

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
                FlushPendingPresentations();
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

        private void FlushPendingPresentations()
        {
            for (int i = 0; i < _pendingPresentations.Count; i++)
            {
                LootPresentationRequest request = _pendingPresentations[i];
                SpawnPresentationInternal(request);
            }

            _pendingPresentations.Clear();
        }

        /// <summary>드랍 보상을 즉시 확정하고 희귀도 순서로 획득 연출을 재생한다.</summary>
        public void GrantAndPresentItems(IReadOnlyList<ItemInstance> items, Vector3 position)
        {
            if (items == null || items.Count == 0)
                return;

            var inventory = Svc.Inventory;
            if (inventory == null)
            {
                Debug.LogWarning("[GameObjectManager] 인벤토리 서비스가 없어 드랍 보상을 지급하지 못했습니다.");
                return;
            }

            var presentationItems = new List<ItemInstance>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                ItemInstance item = items[i];
                if (item?.data == null || item.count <= 0)
                    continue;

                var presentationItem = new ItemInstance
                {
                    data = item.data,
                    count = item.count,
                };
                inventory.AddItem(item.data.itemId, item);
                presentationItems.Add(presentationItem);
            }

            if (presentationItems.Count == 0)
                return;

            presentationItems.Sort(ComparePresentationOrder);
            for (int i = 0; i < presentationItems.Count; i++)
            {
                var request = new LootPresentationRequest(
                    presentationItems[i],
                    position,
                    i,
                    i == presentationItems.Count - 1);

                if (_itemActorPrefab == null)
                    _pendingPresentations.Add(request);
                else
                    SpawnPresentationInternal(request);
            }

            ActorSvc.UI?.RefreshInventoryIfVisible();
        }

        private static int ComparePresentationOrder(ItemInstance left, ItemInstance right)
        {
            ItemRarity leftRarity = left?.data != null ? left.data.itemRarity : ItemRarity.NONE;
            ItemRarity rightRarity = right?.data != null ? right.data.itemRarity : ItemRarity.NONE;
            return leftRarity.CompareTo(rightRarity);
        }

        private void SpawnPresentationInternal(in LootPresentationRequest request)
        {
            var actor = Instantiate(_itemActorPrefab, request.Position, Quaternion.identity);
            actor.Init(request.Item, request.LaunchOrder, request.PlaysArrivalAccent);
        }

        private void DisposeItemActorPrefab()
        {
            _pendingPresentations.Clear();
            _itemActorPrefab = null;
        }
    }
}
