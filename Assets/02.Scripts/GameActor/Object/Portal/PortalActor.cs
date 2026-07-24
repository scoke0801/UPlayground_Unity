using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Manager;
using UPlayGround.State;

namespace UPlayGround
{
    public enum PortalType
    {
        SceneTransition,  // 씬 전환
        InMapTeleport,    // 맵 내 위치 이동
    }

    /// <summary>
    /// 플레이어가 트리거 영역에 진입하면 씬 전환 또는 맵 내 텔레포트를 수행하는 포탈.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PortalActor : MonoBehaviour, IInteractable
    {
        [SerializeField] private PortalType _portalType = PortalType.SceneTransition;

        [Header("씬 전환 설정")]
        [Tooltip("전환할 씬 이름. SceneName 상수와 일치해야 한다. (PortalType.SceneTransition 전용)")]
        [SerializeField] private string _targetSceneName;

        [Header("맵 내 텔레포트 설정")]
        [Tooltip("이동할 목적지 Transform. (PortalType.InMapTeleport 전용)")]
        [SerializeField] private Transform _destinationPoint;

        [Header("맵 클릭 텔레포트 설정")]
        [Tooltip("맵 UI에서 포탈 아이콘 클릭 시 플레이어가 도착할 위치. 미설정 시 포탈 자체 위치 사용.")]
        [SerializeField] private Transform _mapArrivalPoint;

        [Tooltip("false로 설정하면 플레이어가 진입해도 포탈이 동작하지 않는다.")]
        [SerializeField] private bool _isActive = true;

        [Tooltip("중앙 보스 처치 후 활성화되고, 진입 시 사이클 정산을 먼저 요청하는 탈출 포탈.")]
        [SerializeField] private bool _isCycleExitPortal;

        [Header("방문 활성화")]
        [Tooltip("활성화 전에는 이용할 수 없으며, 플레이어가 현장에서 상호작용해야 해금된다. 사이클 탈출 포탈은 적용하지 않는다.")]
        [SerializeField] private bool _requiresActivation = true;

        [Tooltip("새 게임에서도 처음부터 활성화된 포탈. 시작 거점처럼 방문 절차를 생략할 포탈에 사용한다.")]
        [SerializeField] private bool _startsActivated;

        [Tooltip("세이브에 기록되는 포탈 고유 ID. 비우면 씬/계층 경로로 임시 ID를 만들지만, 배치 변경에도 해금을 유지하려면 명시적으로 입력한다.")]
        [SerializeField] private string _activationId;

        [Tooltip("활성화 상태일 때 켤 오브젝트(VFX, 라이트 등).")]
        [SerializeField] private GameObject[] _activeVisuals;

        [Tooltip("비활성화 상태일 때 켤 오브젝트(봉인 VFX, 꺼진 장치 등).")]
        [SerializeField] private GameObject[] _inactiveVisuals;

        [SerializeField] private UnityEvent<bool> _onActivationChanged;

        [Header("맵 UI 동기화")]
        [Tooltip("이 포탈을 맵(RegionInfo)에 동기화할지 여부. 숨김 포탈은 false.")]
        [SerializeField] private bool _showOnMap = true;

        [Tooltip("파스트트래블 시 대상 씬에서 도착할 SceneArrivalPoint.Id. 비우면 씬 기본 스폰.")]
        [SerializeField] private string _targetArrivalId;

        [Tooltip("맵에 표시할 이름. 비우면 오브젝트 이름 사용.")]
        [SerializeField] private string _mapLabel;

        private bool _isActivating;
        private bool _sessionActivated;
        private int _originalLayer;
        private string _cachedActivationId;
        private bool _hasPresentedActivationState;
        private bool _presentedActivated;
        private IGlobalFlagService _boundFlags;
        private Coroutine _bindFlagsCoroutine;

        // ── 맵 UI 동기화용 읽기 전용 접근자 ──────────────────────
        public PortalType Type            => _portalType;
        public string     TargetSceneName => _targetSceneName;
        public string     TargetArrivalId => _targetArrivalId;
        public bool       ShowOnMap       => _showOnMap;
        public string     MapLabel        => string.IsNullOrEmpty(_mapLabel) ? gameObject.name : _mapLabel;
        public bool       IsCycleExitPortal => _isCycleExitPortal;
        public bool       RequiresActivation => _requiresActivation && !_isCycleExitPortal;
        public bool       StartsActivated => _startsActivated;
        public string     ActivationId => _cachedActivationId ??= ResolveActivationId();
        public bool       IsActivated =>
            !RequiresActivation
            || _startsActivated
            || _sessionActivated
            || PortalActivationState.IsActivated(ActivationId);

        private void Awake()
        {
            _originalLayer = gameObject.layer;
            GetComponent<Collider>().isTrigger = true;
            RefreshActivationPresentation(false);
        }

        private void OnEnable()
        {
            if (!TryBindFlagService())
                _bindFlagsCoroutine = StartCoroutine(BindFlagServiceWhenAvailable());
        }

        private void OnDisable()
        {
            if (_bindFlagsCoroutine != null)
            {
                StopCoroutine(_bindFlagsCoroutine);
                _bindFlagsCoroutine = null;
            }

            UnbindFlagService();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!CanUsePortal() || _isActivating) return;

            var actor = other.GetComponent<GameActor>();
            if (actor == null || !actor.HasActorType(ActorType.Player)) return;

            // 정산이 실패하면 씬 전환도 시작하지 않는다. 중복 트리거는 매니저 단계 검사에서 거부된다.
            if (_isCycleExitPortal && !(ActorSvc.CycleExit?.RequestExit() ?? false))
                return;

            switch (_portalType)
            {
                case PortalType.SceneTransition:
                    ActivateSceneTransition();
                    break;
                case PortalType.InMapTeleport:
                    ActivateInMapTeleport(actor);
                    break;
            }
        }

        // ── IInteractable ─────────────────────────────────────────

        public bool CanInteract()
        {
            return _isActive
                   && !_isActivating
                   && RequiresActivation
                   && !IsActivated
                   && !string.IsNullOrEmpty(ActivationId);
        }

        public bool IsInteracting() => false;

        public Transform GetInteractionTransform() => transform;

        public GameActor GetActor() => null;

        public InteractableActorSO GetData() => null;

        public void Interact(GameActor interactor)
        {
            if (!CanInteract())
                return;

            if (!PortalActivationState.Activate(ActivationId))
            {
                Debug.LogWarning(
                    $"[{nameof(PortalActor)}] 전역 플래그 서비스를 사용할 수 없어 '{MapLabel}' 포탈을 활성화하지 못했습니다.",
                    this);
                return;
            }

            _sessionActivated = true;
            RefreshActivationPresentation(true);
            Debug.Log($"[{nameof(PortalActor)}] 포탈 활성화: {MapLabel} ({ActivationId})", this);
        }

        public void StopInteract()
        {
        }

        public void OnAnimationEvent<TData>(InteractionAnimEvent animEvent, TData data)
            where TData : IEventData
        {
        }

        private void ActivateSceneTransition()
        {
            if (string.IsNullOrEmpty(_targetSceneName)) return;

            _isActivating = true;
            // 걸어서 통과할 때도 맵 파스트트래블과 동일한 도착 지점을 사용해 일관성을 맞춘다.
            ActorSvc.SceneTransition?.LoadScene(_targetSceneName, _targetArrivalId);
        }

        private void ActivateInMapTeleport(GameActor actor)
        {
            if (_destinationPoint == null) return;

            _isActivating = true;

            var motor = actor.ActorController?.Motor;
            if (motor != null)
                motor.SetPositionAndRotation(_destinationPoint.position, _destinationPoint.rotation);
            else
                actor.transform.SetPositionAndRotation(_destinationPoint.position, _destinationPoint.rotation);

            // 이전 애니메이션 이어짐 방지: 상태를 Idle로 강제 전환
            actor.ActorController?.TransitionToState(new PlayerIdleState(actor.ActorController));

            // 카메라 튀는 현상 방지: SmoothDamp 속도를 초기화하고 목적지로 즉시 스냅
            CameraManager.Instance?.SnapToTarget(_destinationPoint.position);

            StartCoroutine(ResetActivatingAfterDelay());
        }

        private IEnumerator ResetActivatingAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            _isActivating = false;
        }

        /// <summary>
        /// 플레이어가 이 포탈에서 스폰될 때 사용할 위치/회전을 반환한다.
        /// _mapArrivalPoint가 설정되어 있으면 그 위치를, 없으면 포탈 자체 위치를 반환한다.
        /// </summary>
        public (Vector3 position, Quaternion rotation) GetArrivalPoint()
        {
            if (_mapArrivalPoint != null)
                return (_mapArrivalPoint.position, _mapArrivalPoint.rotation);
            return (transform.position, transform.rotation);
        }

        /// <summary>
        /// 외부(이벤트, 스토리 트리거 등)에서 포탈 활성 상태를 제어한다.
        /// </summary>
        public void SetPortalActive(bool active)
        {
            _isActive = active;
            RefreshActivationPresentation(false);
        }

        /// <summary>
        /// 맵 UI에서 포탈 아이콘 클릭 시 _mapArrivalPoint(없으면 포탈 자체 위치)로 플레이어를 이동한다.
        /// </summary>
        public void TeleportPlayerHere(GameActor actor)
        {
            if (!CanUsePortal() || actor == null)
            {
                Debug.LogWarning($"[{nameof(PortalActor)}] 활성화되지 않은 포탈로의 이동을 거부했습니다.", this);
                return;
            }

            _isActivating = true;

            Vector3    targetPos = _mapArrivalPoint != null ? _mapArrivalPoint.position : transform.position;
            Quaternion targetRot = _mapArrivalPoint != null ? _mapArrivalPoint.rotation : transform.rotation;

            var motor = actor.ActorController?.Motor;
            if (motor != null)
                motor.SetPositionAndRotation(targetPos, targetRot);
            else
                actor.transform.SetPositionAndRotation(targetPos, targetRot);

            actor.ActorController?.TransitionToState(new PlayerIdleState(actor.ActorController));
            CameraManager.Instance?.SnapToTarget(targetPos);

            StartCoroutine(ResetActivatingAfterDelay());
        }

        private bool CanUsePortal()
        {
            return _isActive && IsActivated;
        }

        private IEnumerator BindFlagServiceWhenAvailable()
        {
            while (isActiveAndEnabled && !TryBindFlagService())
                yield return null;

            _bindFlagsCoroutine = null;
        }

        private bool TryBindFlagService()
        {
            IGlobalFlagService flags = Svc.Flags;
            if (flags == null)
                return false;
            if (ReferenceEquals(_boundFlags, flags))
                return true;

            UnbindFlagService();
            _boundFlags = flags;
            _boundFlags.OnFlagChanged += OnFlagChanged;
            _boundFlags.OnFlagsReloaded += OnFlagsReloaded;

            // 서비스 등록 전 Awake에서 표시한 임시 상태를 실제 저장 플래그로 다시 맞춘다.
            _sessionActivated = false;
            RefreshActivationPresentation(true);
            return true;
        }

        private void UnbindFlagService()
        {
            if (_boundFlags == null)
                return;

            _boundFlags.OnFlagChanged -= OnFlagChanged;
            _boundFlags.OnFlagsReloaded -= OnFlagsReloaded;
            _boundFlags = null;
        }

        private void OnFlagChanged(string key, bool value)
        {
            if (key != PortalActivationState.GetFlagKey(ActivationId))
                return;

            _sessionActivated = value;
            RefreshActivationPresentation(true);
        }

        private void OnFlagsReloaded()
        {
            _sessionActivated = false;
            RefreshActivationPresentation(true);
        }

        private void RefreshActivationPresentation(bool notify)
        {
            bool activated = IsActivated;
            bool changed = !_hasPresentedActivationState || _presentedActivated != activated;

            SetVisuals(_activeVisuals, activated);
            SetVisuals(_inactiveVisuals, !activated);

            int interactableLayer = LayerMask.NameToLayer("InteractableObject");
            if (interactableLayer >= 0)
                gameObject.layer = CanInteract() ? interactableLayer : _originalLayer;

            _hasPresentedActivationState = true;
            _presentedActivated = activated;

            if (notify && changed)
                _onActivationChanged?.Invoke(activated);
        }

        private static void SetVisuals(GameObject[] targets, bool active)
        {
            if (targets == null)
                return;

            foreach (GameObject target in targets)
                if (target != null)
                    target.SetActive(active);
        }

        private string ResolveActivationId()
        {
            if (!string.IsNullOrWhiteSpace(_activationId))
                return _activationId.Trim();

            if (!gameObject.scene.IsValid())
                return string.Empty;

            var path = new StringBuilder(gameObject.scene.name);
            var hierarchy = new System.Collections.Generic.Stack<Transform>();
            Transform current = transform;
            while (current != null)
            {
                hierarchy.Push(current);
                current = current.parent;
            }

            while (hierarchy.Count > 0)
            {
                Transform item = hierarchy.Pop();
                path.Append('/').Append(item.name).Append('[').Append(item.GetSiblingIndex()).Append(']');
            }

            return path.ToString();
        }

#if UNITY_EDITOR
        public void EditorEnsureActivationId()
        {
            if (!string.IsNullOrWhiteSpace(_activationId))
                return;

            UnityEditor.Undo.RecordObject(this, "포탈 활성화 ID 생성");
            _activationId = System.Guid.NewGuid().ToString("N");
            _cachedActivationId = _activationId;
            UnityEditor.EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
    }
}
