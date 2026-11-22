using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>풀과 나무가 한 타일의 런타임 TerrainData를 공유한다.</summary>
    internal sealed class ReactiveTerrainData
    {
        private static readonly Dictionary<Terrain, ReactiveTerrainData> Leases = new();
        private readonly Terrain _terrain;
        private readonly TerrainCollider _collider;
        private readonly TerrainData _originalColliderData;
        private int _users;
        public TerrainData Source { get; }
        public TerrainData Runtime { get; }

        private ReactiveTerrainData(Terrain terrain)
        {
            _terrain = terrain;
            _collider = terrain.GetComponent<TerrainCollider>();
            Source = terrain.terrainData;
            _originalColliderData = _collider != null ? _collider.terrainData : null;
            Runtime = Object.Instantiate(Source);
            Runtime.name = Source.name + " (Reactive Runtime)";
            Runtime.hideFlags = HideFlags.DontSave;
            terrain.terrainData = Runtime;
            RefreshCollider();
        }

        public static ReactiveTerrainData Acquire(Terrain terrain)
        {
            if (!Leases.TryGetValue(terrain, out var lease))
                Leases.Add(terrain, lease = new ReactiveTerrainData(terrain));
            lease._users++;
            return lease;
        }

        public void RefreshCollider()
        {
            if (_collider != null) _collider.terrainData = Runtime;
        }

        public void Release()
        {
            if (--_users != 0) return;
            Leases.Remove(_terrain);
            if (_terrain != null && _terrain.terrainData == Runtime) _terrain.terrainData = Source;
            if (_collider != null && _collider.terrainData == Runtime) _collider.terrainData = _originalColliderData;
            if (Application.isPlaying) Object.Destroy(Runtime);
            else Object.DestroyImmediate(Runtime);
        }
    }
}
