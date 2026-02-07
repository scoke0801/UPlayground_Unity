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
        
        private int _currentHits = 0;
        private bool _isGathering = false;
        private GameActor _this;
        private int _currentHp = 0;

        protected override void Awake()
        {
            base.Awake();
            _this = GetComponent<GameActor>();
            _currentHp = _interactableData.hp;
        }
        
        public void Interact(GameActor user)
        {
            if (_isGathering) return;

            _isGathering = true;

            UI_InteractionHPBoard ui = UIManager.Instance.ShowUI("InteractionHPBoard")?.GetComponent<UI_InteractionHPBoard>();
            if (ui != null)
            {
                ui.BoardFill(_currentHp,_interactableData.hp);
                ui.SetInteractionData(_interactableData);
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
        }

        private void OnHitEvent(PlayerInteractionEvent eventData)
        {
            UI_InteractionHPBoard ui = UIManager.Instance.GetUI<UI_InteractionHPBoard>("InteractionHPBoard");
            if (ui == null)
            {
                return;
            }
            
            _currentHp = Mathf.Max(0, _currentHp - eventData.value);
            
            ui.BoardFill(_currentHp, _interactableData.hp);

            if (_currentHp == 0)
            {
                // [TODO] 파괴 이벤트를 발생 시켜야 겠다.
            }
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

        private void OnGatheringComplete()
        {
            // 채집 완료 로직 (아이템 드랍, 오브젝트 파괴 등)
            Destroy(gameObject);
        }
    }
}   