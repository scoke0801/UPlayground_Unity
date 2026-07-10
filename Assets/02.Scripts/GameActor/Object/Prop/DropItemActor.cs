using Interaction.Enum;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 맵 배치와 런타임 스폰 양쪽에서 사용할 수 있는 수동 획득 아이템 액터.
    /// SceneEntityId가 있으면 획득 후 월드 소모 상태로 저장된다.
    /// 상호작용 감지를 위해 콜라이더가 필요하다 (Collider는 추상 타입이라 RequireComponent 자동 추가 불가 — OnValidate에서 경고).
    /// </summary>
    public class DropItemActor : GameActor, IInteractable
    {
        [SerializeField] private ItemSO _itemData;
        [Min(1)]
        [SerializeField] private int _count = 1;
        [SerializeField] private InteractableActorSO _interactionData;
        [SerializeField] private GameObject _getParticle;
        [SerializeField] private bool _showAcquisitionUI = true;
        [SerializeField] private bool _showPickupFX = true;

        private bool _isInteracting;
        private bool _isConsumed;

        protected override void Awake()
        {
            base.Awake();
            _actorType = ActorType.Obstacle;
        }

        public void Init(ItemSO itemData, int count = 1)
        {
            _itemData = itemData;
            _count = Mathf.Max(1, count);
            _isInteracting = false;
            _isConsumed = false;
        }

        public void Init(ItemInstance itemInstance)
        {
            if (itemInstance == null)
            {
                Init(null, 1);
                return;
            }

            Init(itemInstance.data, itemInstance.count);
        }

        public void Interact(GameActor interactor)
        {
            if (!CanInteract()) return;

            _isInteracting = true;
            Collect();
        }

        public void StopInteract()
        {
            _isInteracting = false;
        }

        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data)
            where TData : IEventData
        {
        }

        public bool CanInteract()
        {
            return !_isInteracting && !_isConsumed && _itemData != null && _count > 0;
        }

        public bool IsInteracting()
        {
            return _isInteracting;
        }

        public GameActor GetActor()
        {
            return this;
        }

        public InteractableActorSO GetData()
        {
            return _interactionData;
        }

        public void ResetForRespawn()
        {
            _isInteracting = false;
            _isConsumed = false;
        }

        public void ApplyConsumedState()
        {
            _isInteracting = false;
            _isConsumed = true;
            gameObject.SetActive(false);
        }

        private void Collect()
        {
            if (_itemData == null)
            {
                _isInteracting = false;
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning(
                    $"[{nameof(DropItemActor)}] 인벤토리 매니저가 없어 '{_itemData.name}' 획득을 중단합니다.",
                    this);
                _isInteracting = false;
                return;
            }

            var itemInstance = new ItemInstance
            {
                data = _itemData,
                count = Mathf.Max(1, _count),
            };

            InventoryManager.Instance.AddItem(_itemData.itemId, itemInstance);
            ShowAcquisitionUI();
            ShowCollectEffects();

            // Destroy 폴백 경로에서 _isInteracting이 true로 남으면 핸들러의
            // IsInteracting 게이트가 파괴된 참조를 계속 붙들 수 있으므로 명시 해제한다.
            _isInteracting = false;
            ConsumeOrDestroy();
        }

        private void ShowAcquisitionUI()
        {
            if (!_showAcquisitionUI || UIManager.Instance == null || _itemData == null) return;

            var ui = UIManager.Instance.ShowUI(UIKeyType.ItemAcquisitionList);
            if (ui != null)
            {
                ui.GetComponent<UI_ItemAcquisitionList>()?.SetItem(_itemData);
            }

            UI_Inventory inventory = UIManager.Instance.GetActiveUI(UIKeyType.Inventory)?.GetComponent<UI_Inventory>();
            if (inventory != null && inventory.IsVisible)
            {
                inventory.Show();
            }
        }

        private void ShowCollectEffects()
        {
            if (_getParticle != null)
            {
                Instantiate(_getParticle, transform.position, Quaternion.identity);
            }

            if (_showPickupFX)
            {
                GameObjectManager.Instance?.ShowFX(FXKeyType.ItemArrivedToPlayerPos, transform.position);
            }
        }

        private void ConsumeOrDestroy()
        {
            if (InteractionRespawnManager.Instance != null
                && InteractionRespawnManager.Instance.TryConsume(this))
            {
                return;
            }

            Destroy(gameObject);
        }

        private void OnValidate()
        {
            _count = Mathf.Max(1, _count);

            if (GetComponentInChildren<Collider>(true) == null)
            {
                Debug.LogWarning(
                    $"[{nameof(DropItemActor)}] '{name}'에 상호작용 감지용 Collider가 없습니다.",
                    this);
            }

            if (_interactionData != null
                && _interactionData.interactionObjectType != InteractionObjectType.DROP_ITEM)
            {
                Debug.LogWarning(
                    $"[{nameof(DropItemActor)}] '{name}'의 InteractionData 타입이 DROP_ITEM이 아닙니다.",
                    this);
            }
        }
    }
}
