using System.Collections;
using UPlayGround.Data.EnumType;
using UnityEngine;
using UPlayGround.Data.Event;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.Data.Actor;

namespace UPlayGround
{
    public class GatheringActor : GameActor, IInteractable
    {
        protected override bool RequiresCombatVisuals => false;

        [SerializeField] private InteractableActorSO _interactableData;
        [SerializeField] private float _shakeAmount = 5.0f;
        [SerializeField] private float _shakeDuration = 0.5f;
        private Quaternion _originalRotation = Quaternion.identity;

        private bool _isGathering = false;
        private bool _isInteractionDepleted = false;
        private GameActor _this;
        private IReactiveGatheringTarget _reactiveTarget;
        private bool _hasResolvedReactiveOwner;
        public bool HasReactiveOwner
        {
            get
            {
                if (!_hasResolvedReactiveOwner)
                {
                    _reactiveTarget = GetComponent<IReactiveGatheringTarget>();
                    _hasResolvedReactiveOwner = true;
                }
                return _reactiveTarget != null && _reactiveTarget.OwnsGathering;
            }
        }
        private int _currentHp = 0;
        private int _currentInteractionCount = 0;
        public event System.Action<Vector3> ImpactOccurred;

        protected override void Awake()
        {
            base.Awake();
            _reactiveTarget = GetComponent<IReactiveGatheringTarget>();
            _hasResolvedReactiveOwner = true;
            _actorType = ActorType.Obstacle;
            _this = GetComponent<GameActor>();
            _currentHp = _interactableData != null ? _interactableData.hp : 0;
            _originalRotation = transform.rotation;
        }

        public void Interact(GameActor user)
        {
            if (!CanInteract()) return;

            _isGathering = true;

            if (_interactableData != null && _interactableData.showInfoUI)
            {
                ShowInteractionBoard();
            }
        }

        public void StopInteract()
        {
            _isGathering = false;
        }

        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data) where TData : IEventData
        {
            PlayerInteractionEvent eventData = data as PlayerInteractionEvent;
            if (animEvent == InteractionAnimEvent.OnHit)
            {
                OnHitEvent(eventData);
            }
            else if (animEvent == InteractionAnimEvent.CatchFish)
            {
                OnCatchFishEvent(eventData);
            }
        }

        private void OnHitEvent(PlayerInteractionEvent eventData)
        {
            if (_interactableData == null || eventData == null) return;
            if (HasReactiveOwner)
            {
                if (!_reactiveTarget.TryGather(eventData.value)) return;
                ImpactOccurred?.Invoke(transform.position);
                if (_interactableData.showInfoUI) ActorSvc.UI?.UpdateInteractionBoard(Mathf.CeilToInt(_reactiveTarget.GatheringHealth),
                    Mathf.CeilToInt(_reactiveTarget.GatheringMaximumHealth));
                if (!_reactiveTarget.CanGather)
                {
                    _isGathering = false;
                    SendInteractionFinishedEvent();
                }
                return;
            }
            ImpactOccurred?.Invoke(transform.position);

            PlayerActor player = ActorSvc.Objects.Player;
            if (player != null && _interactableData.showShakeEffect)
            {
                Shake(transform.position - player.transform.position);
            }

            _currentHp = Mathf.Max(0, _currentHp - eventData.value);

            if (_interactableData.showInfoUI)
            {
                ActorSvc.UI?.UpdateInteractionBoard(_currentHp, _interactableData.hp);
            }

            if (_currentHp == 0)
            {
                SendInteractionFinishedEvent();

                var items = Svc.Item.GetDropItemList(_interactableData.dropItems);
                Services.Get<IGroundDropService>().SpawnItems(items, transform.position, source: transform);

                ActorSvc.Objects.ShowFX(FXKeyType.ItemArrivedToPlayerPos, transform.position);

                ConsumeOrDestroy();
            }
        }

        private void OnCatchFishEvent(PlayerInteractionEvent eventData)
        {
            if (_interactableData == null) return;

            var items = Svc.Item.GetDropItemList(_interactableData.dropItems);
            Services.Get<IGroundDropService>().SpawnItems(items, transform.position, source: transform);

            ActorSvc.Objects.ShowFX(FXKeyType.ItemArrivedToPlayerPos, transform.position);

            if (!ShouldDepleteFishingZone()) return;

            _currentInteractionCount++;
            if (_currentInteractionCount < _interactableData.fishingDepleteCatchCount) return;

            SendInteractionFinishedEvent();
            ConsumeOrDestroy();
        }

        public bool CanInteract()
        {
            return !_isGathering && !_isInteractionDepleted && (!HasReactiveOwner || _reactiveTarget.CanGather);
        }

        public bool IsInteracting()
        {
            return _isGathering && (!HasReactiveOwner || _reactiveTarget.CanGather);
        }

        public Transform GetInteractionTransform()
        {
            return transform;
        }

        public GameActor GetActor()
        {
            return _this;
        }

        public InteractableActorSO GetData()
        {
            return _interactableData;
        }

        public void Init(InteractableActorSO interactableData)
        {
            _interactableData = interactableData;
            _isGathering = false;
            _isInteractionDepleted = false;
            _currentInteractionCount = 0;
            _currentHp = _interactableData != null ? _interactableData.hp : 0;
        }

        private void ShowInteractionBoard()
        {
            ActorSvc.UI?.ShowInteractionBoard(_interactableData,
                HasReactiveOwner ? Mathf.CeilToInt(_reactiveTarget.GatheringHealth) : _currentHp,
                HasReactiveOwner ? Mathf.CeilToInt(_reactiveTarget.GatheringMaximumHealth) : _interactableData.hp);
        }

        private void OnGatheringComplete()
        {
            // 채집 완료 로직 (아이템 드랍, 오브젝트 파괴 등)
            ConsumeOrDestroy();
        }

        public void ResetForRespawn()
        {
            StopAllCoroutines();
            _isGathering = false;
            _isInteractionDepleted = false;
            _currentInteractionCount = 0;
            _currentHp = _interactableData != null ? _interactableData.hp : 0;
            transform.rotation = _originalRotation;
        }

        public void ApplyConsumedState()
        {
            StopAllCoroutines();
            _isGathering = false;

            if (IsFishingZone())
            {
                _isInteractionDepleted = true;
                _currentInteractionCount = Mathf.Max(
                    _currentInteractionCount,
                    _interactableData != null ? _interactableData.fishingDepleteCatchCount : 0);
                return;
            }

            gameObject.SetActive(false);
        }

        private void ConsumeOrDestroy()
        {
            if (ActorSvc.InteractionPersistence?.TryConsume(this) == true)
                return;

            if (IsFishingZone())
            {
                ApplyConsumedState();
                return;
            }

            Destroy(gameObject);
        }

        private bool ShouldDepleteFishingZone()
        {
            return IsFishingZone()
                   && _interactableData != null
                   && _interactableData.fishingDepleteCatchCount > 0;
        }

        private bool IsFishingZone()
        {
            return _interactableData != null
                   && _interactableData.interactionObjectType == InteractionObjectType.FISHING_ZONE;
        }

        private static void SendInteractionFinishedEvent()
        {
            Svc.EventPublisher?.Send(PlayerEvent.InteractionTargetDestroy, new EmptyEventData());
        }

        private void Shake(Vector3 attackDirection)
        {
            Vector3 oppositeDirection = attackDirection.normalized;

            Quaternion targetRotation = Quaternion.Euler(
                _originalRotation.eulerAngles.x + oppositeDirection.z * _shakeAmount,
                _originalRotation.eulerAngles.y,
                _originalRotation.eulerAngles.z + oppositeDirection.x * _shakeAmount);

            StopAllCoroutines();
            StartCoroutine(ShakeAnimation(targetRotation));
        }

        private IEnumerator ShakeAnimation(Quaternion targetRotationQuaternion)
        {
            float elapsedTime = 0.0f;

            float shakeDuration = _shakeDuration * 0.5f;
            while (elapsedTime < shakeDuration)
            {
                transform.rotation = Quaternion.Slerp(_originalRotation, targetRotationQuaternion,
                    elapsedTime / shakeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            elapsedTime = 0.0f;
            while (elapsedTime < shakeDuration)
            {
                transform.rotation = Quaternion.Slerp(targetRotationQuaternion, _originalRotation, elapsedTime / shakeDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }
    }
}
