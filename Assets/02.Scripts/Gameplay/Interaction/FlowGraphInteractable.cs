using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Quest;
using UPlayGround.FlowGraph;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.Interaction
{
    /// <summary>플레이어의 조사 입력으로 지정된 FlowGraph 볼륨 진입점을 발화하는 월드 상호작용 대상.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(FlowGraphTriggerVolume))]
    public sealed class FlowGraphInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private FlowGraphTriggerVolume _flowVolume;
        [SerializeField] private Transform _interactionTransform;
        [SerializeField] private string _requiredQuestId;
        [SerializeField] private string _requiredFlagKey;
        [SerializeField] private string _completedFlagKey;

        [Tooltip("켜면 FlowGraph 진입점 발화에 성공한 직후 이 GameObject를 비활성화합니다.")]
        [SerializeField] private bool _shouldDeactivateAfterTrigger;

        private IQuestFlowService _questFlow;
        private IGlobalFlagService _flags;
        private bool _hasTriggered;

        private void Reset()
        {
            _flowVolume = GetComponent<FlowGraphTriggerVolume>();

            int interactableLayer = LayerMask.NameToLayer("InteractableObject");
            if (interactableLayer >= 0)
                gameObject.layer = interactableLayer;
        }

        public bool CanInteract()
        {
            if (!isActiveAndEnabled || _hasTriggered || _flowVolume == null)
                return false;
            if (!TryResolveServices())
                return false;
            if (!string.IsNullOrEmpty(_requiredQuestId)
                && _questFlow.GetQuestStatus(_requiredQuestId) != QuestStatus.Active)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(_requiredFlagKey) && !_flags.GetFlag(_requiredFlagKey))
                return false;

            return string.IsNullOrEmpty(_completedFlagKey) || !_flags.GetFlag(_completedFlagKey);
        }

        public bool IsInteracting() => false;

        public Transform GetInteractionTransform()
            => _interactionTransform != null ? _interactionTransform : transform;

        public GameActor GetActor() => null;

        public InteractableActorSO GetData() => null;

        public void Interact(GameActor interactor)
        {
            if (!CanInteract() || interactor == null)
                return;

            if (!_flowVolume.TryRouteActor(interactor, out FlowVolumeRouteFailure failure))
            {
                Debug.LogWarning(
                    $"[{nameof(FlowGraphInteractable)}] '{name}' 조사 진입점을 발화하지 못했습니다. ({failure})",
                    this);
                return;
            }

            _hasTriggered = true;
            if (_shouldDeactivateAfterTrigger)
                gameObject.SetActive(false);
        }

        public void StopInteract()
        {
        }

        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data)
            where TData : IEventData
        {
        }

        private bool TryResolveServices()
        {
            _questFlow ??= Svc.QuestFlow;
            _flags ??= Svc.Flags;
            return _questFlow != null && _flags != null;
        }
    }
}
