using System.Collections;
using Interaction.Enum;
using Mono.Cecil;
using UnityEngine;
using UPlayGround.Data.Enum;
using UPlayGround.Data.Event;
using UPlayGround.Manager;

namespace UPlayGround
{
    public class GatheringActor : GameActor, IInteractable
    {
        [SerializeField] private InteractableActorSO _interactableData;
        [SerializeField] private float _shakeAmount = 5.0f;
        [SerializeField] private float _shakeDuration = 0.5f;
        [SerializeField] private ItemActor _itemActorPrefab;
        
        private Quaternion _originalRotation = Quaternion.identity;
        
        private bool _isGathering = false;
        private GameActor _this;
        private int _currentHp = 0;

        protected override void Awake()
        {
            base.Awake();
            _actorType = ActorType.Obstacle;
            _this = GetComponent<GameActor>();
            _currentHp = _interactableData.hp;
        }
        
        public void Interact(GameActor user)
        {
            if (_isGathering) return;

            _isGathering = true;

            if (_interactableData.showInfoUI)
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
            PlayerActor player = GameObjectManager.Instance.Player;
            if (player != null && _interactableData.showShakeEffect)
            {
                Shake(transform.position - player.transform.position);
            }
            
            _currentHp = Mathf.Max(0, _currentHp - eventData.value);

            if (_interactableData.showInfoUI)
            { 
                UI_InteractionHPBoard ui = UIManager.Instance.GetUI<UI_InteractionHPBoard>("InteractionHPBoard");
                if (ui != null)
                {
                    ui.BoardFill(_currentHp, _interactableData.hp);
                }
            }

            if (_currentHp == 0)
            {
                if (EventManager.Instance != null)
                {
                    EventManager.Instance.Send(PlayerEvent.InteractionTargetDestroy, new EmptyEventData());
                }
                
                var items = ItemManager.Instance.GetDropItemList(_interactableData.dropItems);
                for (int i = 0; i <items.Count; ++i)
                {
                    var go = Instantiate(_itemActorPrefab, transform.position, Quaternion.identity);
                
                    go.Init(itemInstance: items[i]);
                }
            
                GameObjectManager.Instance.ShowFX("ItemArrivedToPlayerPos", transform.position);
            
                Destroy(gameObject);
            }
        }

        private void OnCatchFishEvent(PlayerInteractionEvent eventData)
        {
            var items = ItemManager.Instance.GetDropItemList(_interactableData.dropItems);
            for (int i = 0; i <items.Count; ++i)
            {
                var go = Instantiate(_itemActorPrefab, transform.position, Quaternion.identity);
                
                go.Init(itemInstance: items[i]);
            }
            
            GameObjectManager.Instance.ShowFX("ItemArrivedToPlayerPos", transform.position);
        }
        
        public bool CanInteract()
        {
            return !_isGathering;
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
            UI_InteractionHPBoard ui = UIManager.Instance.ShowUI("InteractionHPBoard")?.GetComponent<UI_InteractionHPBoard>();
            if (ui != null)
            {
                ui.BoardFill(_currentHp,_interactableData.hp);
                ui.SetInteractionData(_interactableData);
            }
        }
        
        private void OnGatheringComplete()
        {
            // 채집 완료 로직 (아이템 드랍, 오브젝트 파괴 등)
            Destroy(gameObject);
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