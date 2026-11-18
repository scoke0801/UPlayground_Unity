using System;
using UnityEngine;
using UnityEngine.AI;

namespace UPlayGround.WorldBuilder.Runtime
{
    /// <summary>양쪽 소켓이 준비되기 전 교량 진입을 물리 장벽과 NavMesh carving으로 막는다.</summary>
    public sealed class WorldBridgeAccessGate : MonoBehaviour
    {
        [SerializeField] private Collider[] _entryBarriers;
        [SerializeField] private NavMeshObstacle[] _entryObstacles;
        private bool _firstReady, _secondReady;
        public bool CanEnter => isActiveAndEnabled && IsConfigured && _firstReady && _secondReady;
        public bool IsConfigured
        {
            get
            {
                if (_entryBarriers == null || _entryBarriers.Length != 2 || _entryObstacles == null || _entryObstacles.Length != 2) return false;
                for (int i = 0; i < 2; i++) if (_entryBarriers[i] == null || _entryObstacles[i] == null) return false;
                return true;
            }
        }
        public Collider[] EntryBarriers => _entryBarriers;
        public NavMeshObstacle[] EntryObstacles => _entryObstacles;
        private void Awake() => ApplyState();
        private void OnDisable() { _firstReady = _secondReady = false; ApplyState(); }
        /// <summary>이전 청크 상태를 추정하지 않고 소유자가 전달한 준비 상태만 반영한다.</summary>
        public void SetEndpointReady(int endpoint, bool isReady)
        {
            if (endpoint < 0 || endpoint > 1) throw new ArgumentOutOfRangeException(nameof(endpoint));
            if (!IsConfigured) throw new InvalidOperationException("교량 양쪽 진입 장벽과 NavMesh 장애물 참조가 필요합니다.");
            if (endpoint == 0) _firstReady = isReady; else _secondReady = isReady;
            ApplyState();
        }
        private void ApplyState()
        {
            bool blocked = !CanEnter;
            if (_entryBarriers != null) foreach (var barrier in _entryBarriers) if (barrier != null) barrier.enabled = blocked;
            if (_entryObstacles != null) foreach (var obstacle in _entryObstacles) if (obstacle != null) obstacle.enabled = blocked;
        }
    }
}
