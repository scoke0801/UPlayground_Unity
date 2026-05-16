using UnityEngine;
using UnityEngine.Events;
using UPlayGround.Data;
using UPlayGround.Manager;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 맵에 배치해서 플레이어 진입 시 CameraSnapshotProfile을 재생하는 트리거.
    /// Collider(IsTrigger=true)와 함께 사용한다.
    /// </summary>
    [AddComponentMenu("UPlayGround/Camera/Camera Snapshot Sequence Trigger")]
    [RequireComponent(typeof(Collider))]
    public class CameraSnapshotSequenceTrigger : MonoBehaviour
    {
        [SerializeField] private CameraSnapshotProfile _profile;
        [SerializeField] private bool _overrideActorAnchor = false;
        [SerializeField] private CameraSnapshotActorReference _actorAnchor = CameraSnapshotActorReference.ActivePlayer();
        [SerializeField] private bool _overrideLookAtTarget = false;
        [SerializeField] private CameraSnapshotActorReference _lookAtTarget = CameraSnapshotActorReference.None();
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private bool _disableColliderAfterTrigger = false;
        [SerializeField] private UnityEvent _onSequenceStarted;
        [SerializeField] private UnityEvent _onSequenceCompleted;

        private bool _triggered;
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
                _collider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered && _triggerOnce) return;
            if (!other.CompareTag(_playerTag)) return;

            Play();
        }

        public void Play()
        {
            if (_profile == null || CameraManager.Instance == null)
                return;

            bool played = CameraManager.Instance.PushCameraSnapshotSequence(
                _profile,
                _overrideActorAnchor ? _actorAnchor : null,
                _overrideLookAtTarget ? _lookAtTarget : null,
                HandleSequenceCompleted);

            if (!played)
                return;

            _triggered = true;
            if (_disableColliderAfterTrigger && _collider != null)
                _collider.enabled = false;

            _onSequenceStarted?.Invoke();
        }

        private void HandleSequenceCompleted()
        {
            _onSequenceCompleted?.Invoke();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Collider triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
                triggerCollider.isTrigger = true;
        }

        private void OnDrawGizmosSelected()
        {
            Transform actorAnchor = _overrideActorAnchor
                ? CameraSnapshotActorReferenceResolver.Resolve(_actorAnchor)
                : null;
            if (actorAnchor != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, actorAnchor.position);
            }

            Transform lookAtTarget = _overrideLookAtTarget
                ? CameraSnapshotActorReferenceResolver.Resolve(_lookAtTarget)
                : null;
            if (lookAtTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, lookAtTarget.position);
            }
        }
#endif
    }
}
