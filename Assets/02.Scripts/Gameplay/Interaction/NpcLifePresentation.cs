using System.Collections;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Manager;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Gameplay.Interaction
{
    /// <summary>저장된 사건 플래그에서 주민의 작업 위치와 소품을 복원한다.</summary>
    [DisallowMultipleComponent]
    public sealed class NpcLifePresentation : MonoBehaviour
    {
        [SerializeField] private string _completedFlag;
        [SerializeField] private NpcActor _actor;
        [Tooltip("사건 현장에서 생성되는 주민은 기존 Actor ID로 찾아 재사용한다.")]
        [SerializeField] private string _spawnActorId;
        [SerializeField] private string _sceneEntityId;
        [SerializeField] private Transform _beforeAnchor;
        [SerializeField] private Transform _afterAnchor;
        [SerializeField] private UPlayGround.Animation.MotionSetAsset _beforeWorkMotion;
        [SerializeField] private UPlayGround.Animation.MotionSetAsset _afterWorkMotion;
        [SerializeField] private GameObject[] _beforeVisuals = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject[] _afterVisuals = System.Array.Empty<GameObject>();
        [Tooltip("인계 시 위에서 내려놓을 완료 소품. 그릇·탁자처럼 미리 놓인 물체는 제외한다.")]
        [SerializeField] private GameObject[] _deliveryVisuals = System.Array.Empty<GameObject>();
        [Min(0f), SerializeField] private float _deliveryHeight = 0.45f;
        [Min(0.01f), SerializeField] private float _deliveryDuration = 0.35f;
        [Min(0f), SerializeField] private float _transitionDistance = 18f;
        [Min(0.05f), SerializeField] private float _retrySeconds = 0.25f;

        private IGlobalFlagService _flags;
        private Coroutine _pending;
        private Camera _camera;
        private bool _hasApplied;
        private bool _appliedCompleted;
        private bool _restoreRequested;
        private NpcActor _actionActor;
        private NpcMovementController _actionController;
        private Vector3[] _deliveryPositions;
        private bool[] _deliveryVisibility;
        private bool _isDelivering;
        private ulong _deliveryGeneration;
        private System.IDisposable _routinePause;
        private NpcActor _pausedActor;

        /// <summary>현재 화면에 반영된 사건 결과. 저장 사실 자체는 전역 플래그가 소유한다.</summary>
        public bool IsCompletedVisible => _hasApplied && _appliedCompleted;

        /// <summary>행동 생산자와 저장 복원기가 공유하는 완료 사실.</summary>
        public string CompletedFlag => _completedFlag;
        public NpcActor Actor => _actor;
        public Transform Destination => _afterAnchor;

        /// <summary>동료 본인을 조작 중이면 숨겨진 대역 대신 현재 몸으로 인계한다.</summary>
        public bool IsActorControlledByPlayer => _actor != null
            && _actor.CharacterType != UPlayGround.Data.EnumType.CharacterActorType.None
            && Services.TryGet<IPartyService>(out var party)
            && party.ActiveCharacterType == _actor.CharacterType;

        /// <summary>인계할 소품 또는 이동할 주민이 실제로 연결되어 있는지 확인한다.</summary>
        public bool CanPresentAction()
        {
            if (string.IsNullOrWhiteSpace(_completedFlag) || !string.IsNullOrEmpty(_spawnActorId)) return false;
            if (_actor != null)
            {
                if (_actionActor != _actor)
                {
                    _actionActor = _actor;
                    _actionController = _actor.GetComponent<NpcMovementController>();
                }
                if (_afterAnchor == null || !IsActorControlledByPlayer &&
                    (_actionController == null || _actionController.Motor == null)) return false;
            }
            if (_actor == null && _beforeVisuals.Length == 0 && _afterVisuals.Length == 0) return false;
            for (int i = 0; i < _beforeVisuals.Length; i++)
                if (_beforeVisuals[i] == null || _beforeVisuals[i] == gameObject) return false;
            for (int i = 0; i < _afterVisuals.Length; i++)
                if (_afterVisuals[i] == null || _afterVisuals[i] == gameObject) return false;
            for (int i = 0; i < _deliveryVisuals.Length; i++)
                if (_deliveryVisuals[i] == null || System.Array.IndexOf(_afterVisuals, _deliveryVisuals[i]) < 0) return false;
            return true;
        }

        /// <summary>완료 소품을 착지 위치 위에서 내려놓는다. 저장 사실은 착지 후 호출자가 확정한다.</summary>
        public IEnumerator PresentDelivery()
        {
            if (_isDelivering || _deliveryVisuals.Length == 0 || !CanPresentAction()) yield break;
            _deliveryPositions = new Vector3[_deliveryVisuals.Length];
            _deliveryVisibility = new bool[_deliveryVisuals.Length];
            for (int i = 0; i < _deliveryVisuals.Length; i++)
            {
                _deliveryPositions[i] = _deliveryVisuals[i].transform.localPosition;
                _deliveryVisibility[i] = _deliveryVisuals[i].activeSelf;
            }
            _isDelivering = true;
            ulong generation = ++_deliveryGeneration;
            float elapsed = 0f;
            while (_isDelivering && generation == _deliveryGeneration && elapsed < _deliveryDuration)
            {
                float progress = Mathf.Clamp01(elapsed / _deliveryDuration);
                for (int i = 0; i < _deliveryVisuals.Length; i++)
                {
                    if (_deliveryVisuals[i] == null) continue;
                    Transform target = _deliveryVisuals[i].transform;
                    target.localPosition = _deliveryPositions[i];
                    target.position += Vector3.up * (_deliveryHeight * (1f - progress * progress));
                    _deliveryVisuals[i].SetActive(true);
                }
                yield return null;
                elapsed += Time.deltaTime;
            }
            // 취소된 실행의 늦은 재개가 다음 인계의 소품을 복원하지 못하게 한다.
            if (generation == _deliveryGeneration) CancelDelivery();
        }

        /// <summary>중단·로드·완료 직전 임시 소품 위치와 표시를 원래 배치로 돌린다.</summary>
        public void CancelDelivery()
        {
            if (!_isDelivering) return;
            _isDelivering = false;
            ++_deliveryGeneration;
            for (int i = 0; i < _deliveryVisuals.Length; i++)
            {
                if (_deliveryVisuals[i] == null) continue;
                _deliveryVisuals[i].transform.localPosition = _deliveryPositions[i];
                _deliveryVisuals[i].SetActive(_deliveryVisibility[i]);
            }
        }

        /// <summary>실제 인계 입력 또는 주민 도착 뒤 배치를 먼저 반영하고 저장 사실을 확정한다.</summary>
        public bool TryCompleteAction(float arrivalDistance)
        {
            if (!isActiveAndEnabled || !CanPresentAction() || !Services.TryGet<IGlobalFlagService>(out var flags)) return false;
            if (flags.GetFlag(_completedFlag)) return false;
            // 복원용 순간 이동으로 안전 도착을 만들지 않는다. 실제 보행 도착을 먼저 검사한다.
            Transform arrival = IsActorControlledByPlayer ? Svc.ActorQuery?.PlayerTransform : _actor?.transform;
            if (_actor != null && (arrival == null ||
                (arrival.position - _afterAnchor.position).sqrMagnitude > arrivalDistance * arrivalDistance)) return false;
            if (!TryApply(true)) return false;
            _hasApplied = true;
            _appliedCompleted = true;
            _restoreRequested = false;
            flags.SetFlag(_completedFlag, true);
            return true;
        }

        private void OnEnable() => _pending = StartCoroutine(BindAndRestore());

        private IEnumerator BindAndRestore()
        {
            while (!Services.TryGet(out _flags)) yield return null;
            _flags.OnFlagChanged += OnFlagChanged;
            _flags.OnFlagsReloaded += OnFlagsReloaded;
            _camera = Camera.main;
            _restoreRequested = true;
            yield return ApplyPending();
        }

        private void OnDisable()
        {
            ReleaseRoutinePause();
            CancelDelivery();
            if (_pending != null) StopCoroutine(_pending);
            _pending = null;
            if (_flags != null)
            {
                _flags.OnFlagChanged -= OnFlagChanged;
                _flags.OnFlagsReloaded -= OnFlagsReloaded;
            }
            _flags = null;
        }

        private void OnFlagChanged(string key, bool value)
        {
            if (key == _completedFlag) RequestRefresh(false);
        }

        private void OnFlagsReloaded()
        {
            CancelDelivery();
            RequestRefresh(true);
        }

        private void RequestRefresh(bool restore)
        {
            PauseRoutine();
            _restoreRequested |= restore;
            if (_pending == null) _pending = StartCoroutine(ApplyPending());
        }

        private IEnumerator ApplyPending()
        {
            // SaveManager가 같은 호출 안에서 나머지 참여자를 복원한 뒤 최신 사실을 읽는다.
            yield return null;
            var delay = new WaitForSecondsRealtime(_retrySeconds);
            while (isActiveAndEnabled)
            {
                PauseRoutine();
                bool completed = _flags.GetFlag(_completedFlag);
                if (_hasApplied && completed == _appliedCompleted && !_restoreRequested) break;
                bool restoring = _restoreRequested;
                if (!restoring && !CanChangePresentation()) { yield return delay; continue; }
                if (!TryApply(completed)) { yield return delay; continue; }
                _hasApplied = true;
                _appliedCompleted = completed;
                _restoreRequested = false;
                break;
            }
            ReleaseRoutinePause();
            _pending = null;
        }

        private void PauseRoutine()
        {
            if (_actor == null && !string.IsNullOrEmpty(_spawnActorId))
                _actor = Svc.ActorQuery?.FindActor(_spawnActorId) as NpcActor;
            if (_actor == _pausedActor) return;
            ReleaseRoutinePause();
            if (_actor == null) return;
            _pausedActor = _actor;
            _routinePause = _actor.PauseRoutine();
        }

        private void ReleaseRoutinePause()
        {
            _routinePause?.Dispose();
            _routinePause = null;
            _pausedActor = null;
        }

        private bool CanChangePresentation()
        {
            if (_actor == null && !string.IsNullOrEmpty(_spawnActorId))
                _actor = Svc.ActorQuery?.FindActor(_spawnActorId) as NpcActor;
            if (Svc.Dialogue?.IsDialogueActive == true || _actor != null &&
                (_actor.IsInteracting() || _actor.IsDialogueStaged)) return false;
            Transform player = Svc.ActorQuery?.PlayerTransform;
            if (player == null) return false;
            return IsOutsideView(_beforeAnchor, player) && IsOutsideView(_afterAnchor, player)
                && (_actor == null || IsOutsideView(_actor.transform, player));
        }

        private bool IsOutsideView(Transform point, Transform player)
        {
            if (point == null) return true;
            if ((point.position - player.position).sqrMagnitude < _transitionDistance * _transitionDistance) return false;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return false;
            Vector3 viewport = _camera.WorldToViewportPoint(point.position + Vector3.up);
            return viewport.z <= 0 || viewport.x < -0.1f || viewport.x > 1.1f || viewport.y < -0.1f || viewport.y > 1.1f;
        }

        private bool TryApply(bool completed)
        {
            Transform anchor = completed ? _afterAnchor : _beforeAnchor;
            if (!string.IsNullOrEmpty(_spawnActorId))
            {
                // 구조 현장의 액터가 있으면 같은 인물을 이동한다. 마을용 복제 인물을 만들지 않는다.
                _actor = Svc.ActorQuery?.FindActor(_spawnActorId) as NpcActor;
                if (_actor == null && completed)
                {
                    if (anchor == null || !Services.TryGet<IActorSpawnService>(out var spawn) || !spawn.IsReady) return false;
                    _actor = spawn.SpawnActor(_spawnActorId, anchor.position, anchor.rotation) as NpcActor;
                    if (_actor == null) return false;
                }
                if (_actor != null && !completed && anchor == null)
                {
                    // 직접 로드로 완료 이전으로 돌아가면 마을에 복원했던 인물만 제거한다.
                    var identity = _actor.GetComponent<SceneEntityId>();
                    if (identity != null && identity.Guid == _sceneEntityId) Destroy(_actor.gameObject);
                    _actor = null;
                }
            }
            if (_actor != null && anchor != null)
            {
                using var routinePause = _actor.PauseRoutine();
                var controller = _actor.GetComponent<NpcMovementController>();
                if (controller == null || controller.Motor == null)
                {
                    if (!IsActorControlledByPlayer) return false;
                    // 숨겨진 대역은 아직 Awake 전일 수 있다. 조작 중인 몸은 옮기지 않는다.
                    _actor.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                }
                else controller.Motor.SetPositionAndRotation(anchor.position, anchor.rotation);
                var brain = _actor.GetComponent<NpcBrain>();
                brain?.SetHomePose(anchor.position, anchor.rotation);
                if (_beforeWorkMotion != null || _afterWorkMotion != null)
                    brain?.SetWorkMotion(completed ? _afterWorkMotion : _beforeWorkMotion);
                if (controller != null && controller.Motor != null)
                    controller.TransitionToState(new NpcIdleState(controller));
                if (!string.IsNullOrEmpty(_sceneEntityId))
                {
                    var identity = _actor.GetComponent<SceneEntityId>();
                    if (identity == null) identity = _actor.gameObject.AddComponent<SceneEntityId>();
                    identity.RuntimeSetGuid(_sceneEntityId);
                }
            }
            SetVisuals(_beforeVisuals, !completed);
            SetVisuals(_afterVisuals, completed);
            return true;
        }

        private static void SetVisuals(GameObject[] visuals, bool visible)
        {
            for (int i = 0; i < visuals.Length; i++)
                if (visuals[i] != null) visuals[i].SetActive(visible);
        }
    }
}
