using System;
using System.Collections.Generic;
using UPlayGround.Manager.World;

namespace UPlayGround.Manager
{
    public partial class MonsterRespawnManager
    {
        private readonly Dictionary<(string mapId, string guid), RuntimePlacementLoader> _proximityOwners = new();

        /// <summary>실체가 없는 배치도 로더 소유로 등록해 원거리 자동 재스폰을 막는다.</summary>
        public void RegisterProximityPlacement(string mapId, string guid, RuntimePlacementLoader owner)
        {
            if (string.IsNullOrEmpty(mapId) || string.IsNullOrEmpty(guid) || owner == null)
                throw new ArgumentException("인접 스폰에는 mapId, GUID, 로더가 필요합니다.");
            var key = (mapId, guid);
            if (_proximityOwners.TryGetValue(key, out var existing) && existing != null && existing != owner)
                throw new InvalidOperationException($"인접 스폰 GUID '{guid}'를 여러 로더가 소유합니다.");
            _proximityOwners[key] = owner;
        }

        /// <summary>언로드·초기화 시 소유권과 실체 추적만 해제하며 저장된 처치 사실은 보존한다.</summary>
        public void UnregisterProximityPlacements(RuntimePlacementLoader owner)
        {
            var keys = new List<(string mapId, string guid)>();
            foreach (var pair in _proximityOwners)
                if (ReferenceEquals(pair.Value, owner)) keys.Add(pair.Key);
            foreach (var key in keys)
            {
                _proximityOwners.Remove(key);
                _placements.Remove(key);
            }
        }

        private bool HasProximityOwner(string mapId, string guid) =>
            _proximityOwners.TryGetValue((mapId, guid), out var owner) && owner != null;
    }
}
