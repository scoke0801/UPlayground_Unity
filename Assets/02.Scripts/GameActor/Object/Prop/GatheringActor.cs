using System.Collections;
using Interaction.Enum;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround
{
    public class GatheringActor : GameActor, IInteractable
    {
        [SerializeField] private InteractableActorSO _interactableData;
        [SerializeField] private float _shakeAmount = 5.0f;
        [SerializeField] private float _shakeDuration = 0.5f;
        private Quaternion _originalRotation = Quaternion.identity;

        private bool _isGathering = false;
        private bool _isInteractionDepleted = false;
        private GameActor _this;
        private int _currentHp = 0;
        private int _currentInteractionCount = 0;

        protected override void Awake()
        {
            base.Awake();
            _actorType = ActorType.Obstacle;
            _this = GetComponent<GameActor>();
            _currentHp = _interactableData != null ? _interactableData.hp : 0;
            _originalRotation = transform.rotation;
        }

        public void Interact(GameActor user)
        {
            if (_isGathering || _isInteractionDepleted) return;

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

            PlayerActor player = GameObjectManager.Instance.Player;
            if (player != null && _interactableData.showShakeEffect)
            {
                Shake(transform.position - player.transform.position);
            }

            _currentHp = Mathf.Max(0, _currentHp - eventData.value);

            if (_interactableData.showInfoUI)
            {
                UI_InteractionHPBoard ui = UIManager.Instance.GetUI<UI_InteractionHPBoard>(UIKeyType.InteractionHPBoard);
                if (ui != null)
                {
                    ui.BoardFill(_currentHp, _interactableData.hp);
                }
            }

            if (_currentHp == 0)
            {
                SendInteractionFinishedEvent();

                var items = ItemManager.Instance.GetDropItemList(_interactableData.dropItems);
                for (int i = 0; i < items.Count; ++i)
                {
                    GameObjectManager.Instance.SpawnItem(items[i], transform.position);
                }

                GameObjectManager.Instance.ShowFX(FXKeyType.ItemArrivedToPlayerPos, transform.position);

                ConsumeOrDestroy();
            }
        }

        private void OnCatchFishEvent(PlayerInteractionEvent eventData)
        {
            if (_interactableData == null) return;

            var items = ItemManager.Instance.GetDropItemList(_interactableData.dropItems);
            for (int i = 0; i < items.Count; ++i)
            {
                GameObjectManager.Instance.SpawnItem(items[i], transform.position);
            }

            GameObjectManager.Instance.ShowFX(FXKeyType.ItemArrivedToPlayerPos, transform.position);

            if (!ShouldDepleteFishingZone()) return;

            _currentInteractionCount++;
            if (_currentInteractionCount < _interactableData.fishingDepleteCatchCount) return;

            SendInteractionFinishedEvent();
            ConsumeOrDestroy();
        }

        public bool CanInteract()
        {
            return !_isGathering && !_isInteractionDepleted;
        }

        public bool IsInteracting()
        {
            return _isGathering;
        }

        public GameActor GetActor()
        {
            return _this;
        }

        public InteractableActorSO GetData()
        {
            return _interactableData;
        }

        private void ShowInteractionBoard()
        {
            UI_InteractionHPBoard ui = UIManager.Instance.ShowUI(UIKeyType.InteractionHPBoard)?.GetComponent<UI_InteractionHPBoard>();
            if (ui != null)
            {
                ui.BoardFill(_currentHp, _interactableData.hp);
                ui.SetInteractionData(_interactableData);
            }
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
            if (InteractionRespawnManager.Instance != null
                && InteractionRespawnManager.Instance.TryConsume(this))
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
            EventManager.Instance?.Send(PlayerEvent.InteractionTargetDestroy, new EmptyEventData());
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
