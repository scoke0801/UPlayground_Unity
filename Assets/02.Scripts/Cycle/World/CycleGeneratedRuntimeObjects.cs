using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Item;
using UPlayGround.Group;
using UPlayGround.Manager;

namespace UPlayGround.Cycle
{
    [DisallowMultipleComponent]
    public sealed class CycleEncounterRuntimeHandle : MonoBehaviour
    {
        private string _encounterId;
        private MonsterGroupController _group;

        public void Initialize(string encounterId, MonsterGroupController group)
        {
            if (_group != null) _group.OnGroupDefeated -= OnGroupDefeated;
            _encounterId = encounterId;
            _group = group;
            if (_group != null) _group.OnGroupDefeated += OnGroupDefeated;
        }

        private void OnDestroy()
        {
            if (_group != null) _group.OnGroupDefeated -= OnGroupDefeated;
        }

        private void OnGroupDefeated()
        {
            CycleRunManager.Instance?.ReportEncounterCleared(_encounterId);
        }
    }

    [DisallowMultipleComponent]
    public sealed class CycleLootPickup : MonoBehaviour, IInteractable
    {
        private string _lootId;
        private ItemSO _item;
        private int _count;
        private bool _collected;

        public void Initialize(string lootId, ItemSO item, int count)
        {
            _lootId = lootId;
            _item = item;
            _count = Mathf.Max(1, count);
        }

        public void Interact(GameActor interactor)
        {
            if (!CanInteract()) return;
            bool routedToCycleLedger = CycleRemainsManager.Instance?.TryAddUnsettledMaterial(_item.itemId, _count) == true;
            if (!routedToCycleLedger)
            {
                InventoryManager inventory = InventoryManager.Instance;
                if (inventory == null) return;
                inventory.AddItem(_item.itemId, _count);
            }

            _collected = true;
            CycleRunManager.Instance?.ReportCycleLootCollected(_lootId);
            gameObject.SetActive(false);
        }

        public void StopInteract() { }
        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data) where TData : IEventData { }
        public bool CanInteract() => !_collected && _item != null && _count > 0;
        public bool IsInteracting() => false;
        public Transform GetInteractionTransform() => transform;
        public GameActor GetActor() => null;
        public InteractableActorSO GetData() => null;
    }

    [DisallowMultipleComponent]
    public sealed class CycleInteractionTarget : MonoBehaviour, IInteractable
    {
        private string _interactionId;
        private bool _completed;

        public void Initialize(string interactionId)
        {
            _interactionId = interactionId;
        }

        public void Interact(GameActor interactor)
        {
            if (!CanInteract()) return;
            _completed = true;
            CycleRunManager.Instance?.ReportInteractionCompleted(_interactionId);
            gameObject.SetActive(false);
        }

        public void StopInteract() { }
        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data) where TData : IEventData { }
        public bool CanInteract() => !_completed && !string.IsNullOrWhiteSpace(_interactionId);
        public bool IsInteracting() => false;
        public Transform GetInteractionTransform() => transform;
        public GameActor GetActor() => null;
        public InteractableActorSO GetData() => null;
    }
}
