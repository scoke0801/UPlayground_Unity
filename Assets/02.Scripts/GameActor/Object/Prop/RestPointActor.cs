using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 휴식지점(모닥불/회복 제단). 상호작용 시 파티 전원(액티브 + 벤치)을 풀 회복한다.
    /// GatheringActor 흐름을 모델로 하되 아이템 드랍/HP 파괴 대신 회복을 수행한다. 무제한 재사용.
    /// </summary>
    public class RestPointActor : GameActor, IInteractable
    {
        [SerializeField] private InteractableActorSO _data;

        private bool _isInteracting;
        private GameActor _this;

        protected override void Awake()
        {
            base.Awake();
            _actorType = ActorType.Obstacle;
            _this = GetComponent<GameActor>();
        }

        public void Interact(GameActor user)
        {
            if (_isInteracting) return;
            _isInteracting = true;

            // 즉시 회복 (풀 회복 + 무제한 재사용이므로 애니 이벤트 대기 불필요)
            PartyManager.Instance?.HealAllParty(_data != null && _data.reviveDowned);

            // 연출: FX (기존 키 재사용)
            GameObjectManager.Instance.ShowFX(FXKeyType.ItemArrivedToPlayerPos, transform.position);
        }

        public void StopInteract() => _isInteracting = false;

        public bool CanInteract()   => true;     // 무제한 재사용 → 항상 가능
        public bool IsInteracting() => _isInteracting;

        public GameActor GetActor()          => _this;
        public InteractableActorSO GetData() => _data;

        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data)
            where TData : IEventData { }
    }
}
