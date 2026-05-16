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
        [SerializeField] private Transform _actorAnchor;
        [SerializeField] private Transform _lookAtTarget;
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private bool _triggerOnce = true;
        [SerializeField] private bool _useEnteringPlayerAsAnchor = false;
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

            Transform anchor = _actorAnchor;
            if (_useEnteringPlayerAsAnchor || anchor == null)
                anchor = other.transform;

            Play(anchor);
        }

        public void Play()
        {
            Play(_actorAnchor);
        }

        public void Play(Transform actorAnchor)
        {
            if (_profile == null || CameraManager.Instance == null)
                return;

            bool played = CameraManager.Instance.PushCameraSnapshotSequence(
                _profile,
                actorAnchor,
                _lookAtTarget,
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
            if (_actorAnchor != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, _actorAnchor.position);
            }

            if (_lookAtTarget != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, _lookAtTarget.position);
            }
        }
#endif
    }
}
