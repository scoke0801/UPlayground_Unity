using System;
using System.Collections;
using System.Collections.Generic;
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

        [Header("유효 구간 노출")]
        [Tooltip("유효 조건을 만족하는 동안에만 켤 연출 오브젝트(모델, VFX, 상호작용 표식 등). " +
                 "조건을 만족하지 못했거나 이미 발화된 뒤에는 꺼둡니다.")]
        [SerializeField] private GameObject[] _availableVisuals;

        [Tooltip("켜면 유효 조건을 만족하기 전까지 볼륨 물리 진입 발화도 막습니다. " +
                 "조건을 만족하는 순간 이미 볼륨 안에 서 있었다면 그 시점에 진입이 재생됩니다.")]
        [SerializeField] private bool _shouldGateVolumeRouting = true;

        private readonly List<IDisposable> _questSubscriptions = new();

        private IQuestFlowService _questFlow;
        private IGlobalFlagService _flags;
        private IGlobalFlagService _boundFlags;
        private Coroutine _bindServicesCoroutine;
        private bool _hasTriggered;
        private int _originalLayer;
        private bool _hasPresentedAvailability;
        private bool _presentedAvailable;

        private void Reset()
        {
            _flowVolume = GetComponent<FlowGraphTriggerVolume>();

            int interactableLayer = LayerMask.NameToLayer("InteractableObject");
            if (interactableLayer >= 0)
                gameObject.layer = interactableLayer;
        }

        private void Awake()
        {
            _originalLayer = gameObject.layer;

            // 서비스 등록 전에는 조건을 판정할 수 없다. 노출된 채로 시작하지 않도록 닫힌 상태로 먼저 표시한다.
            RefreshAvailabilityPresentation();
        }

        private void OnEnable()
        {
            if (!TryBindConditionSources())
                _bindServicesCoroutine = StartCoroutine(BindConditionSourcesWhenAvailable());
        }

        private void OnDisable()
        {
            if (_bindServicesCoroutine != null)
            {
                StopCoroutine(_bindServicesCoroutine);
                _bindServicesCoroutine = null;
            }

            UnbindConditionSources();
        }

        public bool CanInteract()
        {
            if (!isActiveAndEnabled || _hasTriggered || _flowVolume == null)
                return false;

            return IsConditionSatisfied();
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
            else
                RefreshAvailabilityPresentation();
        }

        public void StopInteract()
        {
        }

        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data)
            where TData : IEventData
        {
        }

        /// <summary>퀘스트/플래그 조건상 이 연출이 지금 유효한지. 발화 여부는 보지 않는다.</summary>
        private bool IsConditionSatisfied()
        {
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

        /// <summary>
        /// 노출과 발화 경로를 현재 유효 여부에 맞춘다.
        /// 연출 오브젝트, 상호작용 탐색용 레이어, 볼륨 물리 진입을 한 지점에서 함께 여닫는다.
        /// </summary>
        private void RefreshAvailabilityPresentation()
        {
            bool isAvailable = !_hasTriggered && IsConditionSatisfied();
            if (_hasPresentedAvailability && _presentedAvailable == isAvailable)
                return;

            _hasPresentedAvailability = true;
            _presentedAvailable = isAvailable;

            if (_availableVisuals != null)
            {
                for (int i = 0; i < _availableVisuals.Length; i++)
                {
                    GameObject visual = _availableVisuals[i];
                    if (visual != null)
                        visual.SetActive(isAvailable);
                }
            }

            int interactableLayer = LayerMask.NameToLayer("InteractableObject");
            if (interactableLayer >= 0)
                gameObject.layer = isAvailable ? interactableLayer : _originalLayer;

            if (_shouldGateVolumeRouting && _flowVolume != null)
            {
                // 조건을 만족하는 순간 이미 볼륨 안에 서 있던 대상의 진입은 여기서 재생된다.
                _flowVolume.SetRoutingEnabled(isAvailable);
            }
        }

        private IEnumerator BindConditionSourcesWhenAvailable()
        {
            while (isActiveAndEnabled && !TryBindConditionSources())
                yield return null;

            _bindServicesCoroutine = null;
        }

        /// <summary>조건 소스(플래그/퀘스트)의 변경 통지에 연결한다. 매 프레임 폴링 대신 변경 시점에만 갱신한다.</summary>
        private bool TryBindConditionSources()
        {
            if (!TryResolveServices())
                return false;

            IGameEventObservable events = Svc.Events;
            if (events == null)
                return false;

            if (!ReferenceEquals(_boundFlags, _flags))
            {
                UnbindFlags();
                _boundFlags = _flags;
                _boundFlags.OnFlagChanged += OnFlagChanged;
                _boundFlags.OnFlagsReloaded += OnConditionSourceReloaded;
            }

            if (_questSubscriptions.Count == 0 && !string.IsNullOrEmpty(_requiredQuestId))
            {
                SubscribeQuestEvent(events, QuestEvent.QuestAccepted);
                SubscribeQuestEvent(events, QuestEvent.QuestCompleted);
                SubscribeQuestEvent(events, QuestEvent.QuestFailed);
            }

            RefreshAvailabilityPresentation();
            return true;
        }

        private void SubscribeQuestEvent(IGameEventObservable events, QuestEvent eventType)
        {
            IDisposable subscription = events.Subscribe<QuestEvent, QuestStateEventData>(
                eventType, OnQuestStateChanged);
            if (subscription != null)
                _questSubscriptions.Add(subscription);
        }

        private void UnbindConditionSources()
        {
            UnbindFlags();

            for (int i = 0; i < _questSubscriptions.Count; i++)
                _questSubscriptions[i]?.Dispose();
            _questSubscriptions.Clear();
        }

        private void UnbindFlags()
        {
            if (_boundFlags == null)
                return;

            _boundFlags.OnFlagChanged -= OnFlagChanged;
            _boundFlags.OnFlagsReloaded -= OnConditionSourceReloaded;
            _boundFlags = null;
        }

        private void OnFlagChanged(string key, bool value)
        {
            if (key != _requiredFlagKey && key != _completedFlagKey)
                return;

            RefreshAvailabilityPresentation();
        }

        private void OnConditionSourceReloaded() => RefreshAvailabilityPresentation();

        private void OnQuestStateChanged(QuestStateEventData data)
        {
            if (data == null || data.QuestId != _requiredQuestId)
                return;

            RefreshAvailabilityPresentation();
        }

        private bool TryResolveServices()
        {
            _questFlow ??= Svc.QuestFlow;
            _flags ??= Svc.Flags;
            return _questFlow != null && _flags != null;
        }
    }
}
