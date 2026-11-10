using UPlayGround.Data.EnumType;
using System.Collections.Generic;
using UPlayGround.Components;
using UnityEngine;
using UPlayGround.Data.Event;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Item;

namespace UPlayGround
{
    /// <summary>
    /// 맵 배치와 런타임 스폰 양쪽에서 사용할 수 있는 수동 획득 아이템 액터.
    /// SceneEntityId가 있으면 획득 후 월드 소모 상태로 저장된다.
    /// 상호작용 감지를 위해 콜라이더가 필요하다 (Collider는 추상 타입이라 RequireComponent 자동 추가 불가 — OnValidate에서 경고).
    /// </summary>
    public class DropItemActor : GameActor, IInteractable
    {
        protected override bool RequiresCombatVisuals => false;

        [SerializeField] private ItemSO _itemData;
        [Min(1)]
        [SerializeField] private int _count = 1;
        [SerializeField] private InteractableActorSO _interactionData;
        [SerializeField] private GameObject _getParticle;
        [SerializeField] private bool _showAcquisitionUI = true;
        [SerializeField] private bool _showPickupFX = true;

        private bool _isConsumed;
        private ItemInstance _instance;
        private string _dropId;
        private SceneEntityId _entityId;
        private static readonly List<DropItemActor> s_activeDrops = new();
        public static IReadOnlyList<DropItemActor> ActiveDrops => s_activeDrops;

        public ItemSO ItemData => _itemData;
        public int Count => Mathf.Max(1, _count);
        public InteractableActorSO InteractionData => _interactionData;
        public bool ShowsAcquisitionUI => _showAcquisitionUI;

        protected override void Awake()
        {
            base.Awake();
            _actorType = ActorType.Obstacle;
            _entityId = GetComponent<SceneEntityId>();
        }

        private void OnEnable() { if (!s_activeDrops.Contains(this)) s_activeDrops.Add(this); }
        private void OnDisable() { s_activeDrops.Remove(this); StopInteract(); }

        /// <summary>씬 준비 뒤 등록해 초기화 순서와 월드 복원에 안전하게 연결한다.</summary>
        public void RegisterGroundDrop(IGroundDropService drops)
        {
            if (_itemData == null || _isConsumed || _entityId == null || !_entityId.HasGuid) return;
            _instance ??= new ItemInstance { data = _itemData, count = Count };
            _dropId = drops.RegisterPlaced(_entityId.Guid, _instance, transform);
        }

        public void Init(ItemSO itemData, int count = 1)
        {
            Init(itemData, count, _interactionData);
        }

        public void Init(ItemSO itemData, int count, InteractableActorSO interactionData)
        {
            _itemData = itemData;
            _count = Mathf.Max(1, count);
            _interactionData = interactionData;
            _isConsumed = false;
            _instance = new ItemInstance { data = itemData, count = _count };
        }

        public void Init(ItemInstance itemInstance)
        {
            if (itemInstance == null)
            {
                Init(null, 1);
                return;
            }

            Init(itemInstance.data, itemInstance.count);
            _instance = itemInstance;
        }

        public void Interact(GameActor interactor)
        {
            if (!CanInteract()) return;

            Collect();
        }

        public void StopInteract()
        {
        }

        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data)
            where TData : IEventData
        {
        }

        public bool CanInteract()
        {
            return !_isConsumed && _dropId != null && Services.TryGet<IGroundDropService>(out var drops)
                && drops.CanPlayerCollect(_dropId);
        }

        public bool IsInteracting()
        {
            return false;
        }

        public Transform GetInteractionTransform()
        {
            return transform;
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
            if (Services.TryGet<IGroundDropService>(out var drops)) drops.ResetPlaced(_dropId);
            _dropId = null;
            StopInteract();
            _isConsumed = false;
        }

        public void ApplyConsumedState()
        {
            StopInteract();
            _isConsumed = true;
            gameObject.SetActive(false);
        }

        private void Collect()
        {
            if (_dropId == null || !Services.TryGet<IGroundDropService>(out var drops)) return;
            drops.TryPlayerCollect(_dropId);
        }

        /// <summary>클릭과 펫 회수 모두 같은 획득 연출을 사용한다.</summary>
        public void PlayCollectEffects()
        {
            if (_getParticle != null)
            {
                Instantiate(_getParticle, transform.position, Quaternion.identity);
            }

            if (_showPickupFX)
            {
                ActorSvc.Objects?.ShowFX(FXKeyType.ItemArrivedToPlayerPos, transform.position);
            }
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
