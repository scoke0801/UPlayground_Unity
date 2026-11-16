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
        [Tooltip("접촉 이벤트로 집고 내려놓는 현장 소품. 지정하면 임시 소품을 생성하지 않는다.")]
        [SerializeField] private Transform _sceneProp;
        private Transform _propParent;
        private Vector3 _propPosition;
        private Quaternion _propRotation;
        private UnityEngine.Object _propHolder;
        private static readonly HashSet<NpcWorkPoint> Points = new();
        private UnityEngine.Object _owner;
        private ulong _generation;

        public string MapId => _mapId;
        public string PointId => _pointId;
        public bool IsSheltered => _isSheltered;
        public MotionSetAsset Motion => _motion;
        public GameObject HeldPropPrefab => _heldPropPrefab;
        public bool HasSceneProp => _sceneProp != null;
        public bool IsScenePropHeldBy(UnityEngine.Object owner) => owner != null && _propHolder == owner;

        private void OnEnable()
        {
            Points.Add(this);
            if (_sceneProp == null) return;
            _propParent = _sceneProp.parent;
            _propPosition = _sceneProp.localPosition;
            _propRotation = _sceneProp.localRotation;
        }
        private void OnDisable() { RestoreSceneProp(); _owner = null; _generation++; Points.Remove(this); }

        /// <summary>예약한 실행만 같은 실물 소품을 손으로 인계한다. 이미 들고 재개한 경우 중복 부착하지 않는다.</summary>
        public bool TryTakeSceneProp(UnityEngine.Object owner, ulong generation, Transform hand)
        {
            if (!HasSceneProp || hand == null || !IsOwnedBy(owner, generation)
                || _propHolder != null && _propHolder != owner) return false;
            if (_propHolder == owner) return true;
            _sceneProp.SetParent(hand, true);
            _propHolder = owner;
            return true;
        }

        /// <summary>놓기 접촉에서 월드 자세를 보존하며 손의 소유권만 반환한다.</summary>
        public bool TryPlaceSceneProp(UnityEngine.Object owner, ulong generation)
        {
            if (!HasSceneProp || _propHolder != owner || !IsOwnedBy(owner, generation)) return false;
            _sceneProp.SetParent(_propParent, true);
            _propHolder = null;
            return true;
        }

        /// <summary>몸의 제거·씬 해제 때만 저작 위치를 복구해 소품이 몸과 함께 사라지지 않게 한다.</summary>
        public void ReleaseScenePropOnUnbind(UnityEngine.Object owner)
        {
            if (_propHolder == owner) RestoreSceneProp();
        }

        private void RestoreSceneProp()
        {
            if (_sceneProp != null && _propHolder != null)
            {
                _sceneProp.SetParent(_propParent, false);
                _sceneProp.SetLocalPositionAndRotation(_propPosition, _propRotation);
            }
            _propHolder = null;
        }

        /// <summary>같은 맵·ID의 중복 배치와 자리 경쟁을 거부하고 실행 토큰을 발급한다.</summary>
        public bool TryReserve(UnityEngine.Object owner, out ulong generation)
        {
            generation = 0;
            if (!isActiveAndEnabled || owner == null || _owner != null
                || _propHolder != null && _propHolder != owner
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
