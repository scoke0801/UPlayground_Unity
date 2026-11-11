using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>탈것의 탑승·양손 접촉 기준과 외형 충돌 범위를 제공한다.</summary>
    public sealed class PlayerGliderView : MonoBehaviour
    {
        [SerializeField] private Transform _riderAnchor;
        [SerializeField] private Transform _gripLeft;
        [SerializeField] private Transform _gripRight;
        [SerializeField] private Transform _canopy;
        [SerializeField] private Transform _petSeatAnchor;
        public Transform PetSeatAnchor => _petSeatAnchor;
        public event System.Action ReleasingPassengers;

        /// <summary>탈것을 비활성화하거나 파괴하기 전에 동승자의 부모를 해제한다.</summary>
        public void ReleasePassengers() => ReleasingPassengers?.Invoke();
        private void OnDisable() => ReleasePassengers();
        [SerializeField] private Vector3 _collisionCenter;
        [SerializeField] private Vector3 _collisionHalfExtents;

        private Quaternion _canopyRestRotation;

        public Transform RiderAnchor => _riderAnchor;
        public Transform GripLeft => _gripLeft;
        public Transform GripRight => _gripRight;
        public Vector3 CollisionCenter => _collisionCenter;
        public Vector3 CollisionHalfExtents => _collisionHalfExtents;
        public bool HasValidBinding => _riderAnchor != null && _gripLeft != null && _gripRight != null
            && _riderAnchor.IsChildOf(transform) && _gripLeft.IsChildOf(transform)
            && _gripRight.IsChildOf(transform) && transform.localScale == Vector3.one
            && _collisionHalfExtents.x > 0f && _collisionHalfExtents.y > 0f
            && _collisionHalfExtents.z > 0f;

        private void Awake()
        {
            if (_canopy != null) _canopyRestRotation = _canopy.localRotation;
        }

        /// <summary>손잡이를 고정한 채 날개만 선회 방향으로 기울인다.</summary>
        public void SetBank(float angle)
        {
            if (_canopy != null)
                _canopy.localRotation = _canopyRestRotation * Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}
