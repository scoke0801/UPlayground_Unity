using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Animation;

namespace UPlayGround.Components.LivingWorld
{
    /// <summary>한 자리 작업점의 예약과 실행 세대를 소유한다. 반복 작업은 저장 사실을 만들지 않는다.</summary>
    [DisallowMultipleComponent]
    public sealed class NpcWorkPoint : MonoBehaviour
    {
        [SerializeField] private string _mapId;
        [SerializeField] private string _pointId;
        [SerializeField] private bool _isSheltered;
        [SerializeField] private MotionSetAsset _motion;
        [SerializeField] private GameObject _heldPropPrefab;
        private static readonly HashSet<NpcWorkPoint> Points = new();
        private UnityEngine.Object _owner;
        private ulong _generation;

        public string MapId => _mapId;
        public string PointId => _pointId;
        public bool IsSheltered => _isSheltered;
        public MotionSetAsset Motion => _motion;
        public GameObject HeldPropPrefab => _heldPropPrefab;

        private void OnEnable() => Points.Add(this);
        private void OnDisable() { _owner = null; _generation++; Points.Remove(this); }

        /// <summary>같은 맵·ID의 중복 배치와 자리 경쟁을 거부하고 실행 토큰을 발급한다.</summary>
        public bool TryReserve(UnityEngine.Object owner, out ulong generation)
        {
            generation = 0;
            if (!isActiveAndEnabled || owner == null || _owner != null
                || string.IsNullOrWhiteSpace(_mapId) || string.IsNullOrWhiteSpace(_pointId)) return false;
            foreach (NpcWorkPoint point in Points)
                if (point != null && point != this && point._mapId == _mapId && point._pointId == _pointId)
                    return false;
            _owner = owner;
            generation = ++_generation;
            return true;
        }

        /// <summary>중단 이전 실행이 새 예약의 소품이나 모션을 변경하지 못하게 한다.</summary>
        public bool IsOwnedBy(UnityEngine.Object owner, ulong generation)
            => isActiveAndEnabled && owner != null && _owner == owner && _generation == generation;

        /// <summary>자신의 실행 세대만 해제하며 중복 해제는 무시한다.</summary>
        public void Release(UnityEngine.Object owner, ulong generation)
        {
            if (!IsOwnedBy(owner, generation)) return;
            _owner = null;
            _generation++;
        }
    }
}
