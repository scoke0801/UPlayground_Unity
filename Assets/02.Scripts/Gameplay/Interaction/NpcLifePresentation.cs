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
        [SerializeField] private GameObject[] _beforeVisuals = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject[] _afterVisuals = System.Array.Empty<GameObject>();
        [Min(0f), SerializeField] private float _transitionDistance = 18f;
        [Min(0.05f), SerializeField] private float _retrySeconds = 0.25f;

        private IGlobalFlagService _flags;
        private Coroutine _pending;
        private Camera _camera;
        private bool _hasApplied;
        private bool _appliedCompleted;
        private bool _restoreRequested;

        /// <summary>현재 화면에 반영된 사건 결과. 저장 사실 자체는 전역 플래그가 소유한다.</summary>
        public bool IsCompletedVisible => _hasApplied && _appliedCompleted;

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

        private void OnFlagsReloaded() => RequestRefresh(true);

        private void RequestRefresh(bool restore)
        {
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
            _pending = null;
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
                var controller = _actor.GetComponent<NpcMovementController>();
                if (controller == null || controller.Motor == null) return false;
                controller.Motor.SetPositionAndRotation(anchor.position, anchor.rotation);
                _actor.GetComponent<NpcBrain>()?.SetHomePose(anchor.position, anchor.rotation);
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
