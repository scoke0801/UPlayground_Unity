using System.Collections;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.Data.Sound;
using UPlayGround.Data.Actor;

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
        private Coroutine _interactionCoroutine;

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

            float completeDuration = _data != null ? Mathf.Max(0f, _data.interactionCompleteDuration) : 0f;
            if (completeDuration <= 0f)
            {
                CompleteInteraction();
                return;
            }

            _interactionCoroutine = StartCoroutine(CompleteInteractionAfterDelay(completeDuration));
        }

        public void StopInteract()
        {
            if (_interactionCoroutine != null)
            {
                StopCoroutine(_interactionCoroutine);
                _interactionCoroutine = null;
            }

            _isInteracting = false;
        }

        private IEnumerator CompleteInteractionAfterDelay(float duration)
        {
            yield return new WaitForSeconds(duration);
            _interactionCoroutine = null;
            CompleteInteraction();
        }

        private void CompleteInteraction()
        {
            if (!_isInteracting) return;

            Svc.Party?.HealAllParty(_data != null && _data.reviveDowned);

            // 연출: FX (기존 키 재사용)
            ActorSvc.Objects.ShowFX(FXKeyType.ItemArrivedToPlayerPos, transform.position);
            Svc.Sound?.PlaySfx(GameSoundKey.RestPointHeal, transform.position);
            ActorSvc.UI?.ShowRestGrowth();
            _isInteracting = false;
        }

        public bool CanInteract()   => true;     // 무제한 재사용 → 항상 가능
        public bool IsInteracting() => _isInteracting;

        public Transform GetInteractionTransform() => transform;
        public GameActor GetActor()          => _this;
        public InteractableActorSO GetData() => _data;

        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data)
            where TData : IEventData { }
    }
}
