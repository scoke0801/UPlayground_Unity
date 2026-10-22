using System;
using System.Collections;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Interaction
{
    /// <summary>생활 소품 인계와 주민의 실제 도착을 저장 가능한 배치 변화로 연결한다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Collider))]
    public sealed class NpcLifeInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private NpcLifePresentation _presentation;
        [SerializeField] private string _requiredFlag;
        [SerializeField] private InteractableActorSO _data;
        [Min(0.1f), SerializeField] private float _interactionDistance = 3f;
        [Min(0.1f), SerializeField] private float _arrivalDistance = 0.6f;
        [Min(0.1f), SerializeField] private float _walkSpeedMultiplier = 0.6f;
        [Min(0.1f), SerializeField] private float _timeoutSeconds = 20f;

        private IGlobalFlagService _flags;
        private Coroutine _action;
        private IDisposable _movementLease;
        private NpcActor _movingActor;
        private GameActor _interactor;
        private bool _requiresArrival;

        /// <summary>미완료 행동과 선행 사건을 만족한 경우에만 인계를 허용한다.</summary>
        public bool CanInteract()
        {
            // 플레이어 상태가 인계 완료까지 기다릴 수 있는 상호작용 계약이 필요하다.
            if (!isActiveAndEnabled || _action != null || _data == null || _data.interactionObjectType != InteractionObjectType.DROP_ITEM
                || _presentation == null || !_presentation.isActiveAndEnabled
                || !_presentation.CanPresentAction() || !Services.TryGet<IGlobalFlagService>(out var flags)) return false;
            return !flags.GetFlag(_presentation.CompletedFlag)
                && (string.IsNullOrEmpty(_requiredFlag) || flags.GetFlag(_requiredFlag))
                && Svc.Dialogue?.IsDialogueActive != true;
        }

        public bool IsInteracting() => _action != null;
        public Transform GetInteractionTransform() => transform;
        public GameActor GetActor() => null;
        public InteractableActorSO GetData() => _data;

        /// <summary>근처의 살아 있는 플레이어 입력으로 인계하거나 주민 보행을 시작한다.</summary>
        public void Interact(GameActor interactor)
        {
            if (!CanInteract() || interactor == null || !((IWorldActor)interactor).IsAlive
                || !ReferenceEquals(Svc.ActorQuery?.Player, interactor)
                || (interactor.transform.position - transform.position).sqrMagnitude > _interactionDistance * _interactionDistance) return;
            _interactor = interactor;
            Services.TryGet(out _flags);
            _flags.OnFlagsReloaded += StopInteract;
            _flags.OnFlagChanged += OnFlagChanged;
            _action = StartCoroutine(PerformAction());
        }

        private IEnumerator PerformAction()
        {
            // 즉시 완료되는 소품 인계에서도 코루틴 핸들이 남지 않게 한다.
            yield return null;
            _movingActor = _presentation.IsActorControlledByPlayer ? null : _presentation.Actor;
            _requiresArrival = _movingActor != null;
            if (_movingActor != null)
            {
                if (_movingActor.IsInteracting() || _movingActor.IsDialogueStaged) { FinishAction(); yield break; }
                _movementLease = _movingActor.BeginDialogueStage(_presentation.Destination);
                if (!_movingActor.TryBeginDialogueStep(_presentation.Destination, _walkSpeedMultiplier,
                    _arrivalDistance * 0.5f, faceMovement: true)) { FinishAction(); yield break; }
                float deadline = Time.unscaledTime + _timeoutSeconds;
                while (_movingActor != null && _presentation.Destination != null
                    && (_movingActor.transform.position - _presentation.Destination.position).sqrMagnitude > _arrivalDistance * _arrivalDistance)
                {
                    if (!CanContinue() || Time.unscaledTime >= deadline) { FinishAction(); yield break; }
                    yield return null;
                }
            }
            if (CanContinue())
            {
                var delivery = _presentation.PresentDelivery();
                while (CanContinue() && delivery.MoveNext()) yield return delivery.Current;
                _presentation.CancelDelivery();
            }
            if (CanContinue()) _presentation.TryCompleteAction(
                _presentation.IsActorControlledByPlayer ? _interactionDistance : _arrivalDistance);
            FinishAction();
        }

        private bool CanContinue() => _interactor != null && ((IWorldActor)_interactor).IsAlive
            && _presentation != null && _presentation.isActiveAndEnabled
            // NPC는 IDamageable이 아니므로 IWorldActor.IsAlive로 생존을 판정하지 않는다.
            && (!_requiresArrival || _movingActor != null && _movingActor.isActiveAndEnabled)
            && (string.IsNullOrEmpty(_requiredFlag) || _flags.GetFlag(_requiredFlag))
            && Svc.Dialogue?.IsDialogueActive != true;

        private void OnFlagChanged(string key, bool value)
        {
            if (key == _requiredFlag && !value) StopInteract();
        }

        /// <summary>취소·로드·씬 이탈은 이동 홀드만 정리하고 완료 사실을 만들지 않는다.</summary>
        public void StopInteract()
        {
            if (_action != null) StopCoroutine(_action);
            FinishAction();
        }

        private void FinishAction()
        {
            _presentation?.CancelDelivery();
            _movingActor?.StopDialogueStep();
            _movementLease?.Dispose();
            _movementLease = null;
            _movingActor = null;
            _requiresArrival = false;
            _interactor = null;
            _action = null;
            if (_flags == null) return;
            _flags.OnFlagsReloaded -= StopInteract;
            _flags.OnFlagChanged -= OnFlagChanged;
            _flags = null;
        }

        private void OnDisable() => StopInteract();

        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data) where TData : IEventData { }
    }
}
