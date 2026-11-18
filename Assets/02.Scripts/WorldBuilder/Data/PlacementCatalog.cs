using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.WorldBuilder.Core;

namespace UPlayGround.WorldBuilder.Data
{
    /// <summary>참조 프리팹과 바닥 pivot·footprint 계약의 단일 원본.</summary>
    [CreateAssetMenu(fileName = "PlacementCatalog", menuName = "UPlayGround/WorldBuilder/Placement Catalog")]
    public sealed class PlacementCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string prefabGuid;
            public GameObject prefab;
            public Vector2 halfExtents = Vector2.one;
            public float pivotHeight;
            public bool supportsNonUniformScale;
            public PlacementPrefab ToCore() => new PlacementPrefab { Guid = prefabGuid, HalfX = halfExtents.x, HalfZ = halfExtents.y, PivotHeight = pivotHeight, SupportsNonUniformScale = supportsNonUniformScale };
        }
        [SerializeField] private List<Entry> _entries = new List<Entry>();
        public IReadOnlyList<Entry> Entries => _entries;
        public List<PlacementPrefab> ReadPrefabs()
        {
            var result = new List<PlacementPrefab>();
            foreach (var entry in _entries) { if (entry == null || entry.prefab == null) throw new InvalidOperationException("MissingPrefab: catalog 참조가 누락됐습니다."); result.Add(entry.ToCore()); }
            return result;
        }
        public GameObject Resolve(string guid)
        {
            foreach (var entry in _entries) if (entry.prefabGuid == guid) return entry.prefab;
            throw new InvalidOperationException("MissingPrefab: " + guid);
        }
    }
}
