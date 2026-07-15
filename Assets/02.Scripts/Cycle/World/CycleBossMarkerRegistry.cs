using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Cycle
{
    public readonly struct CycleBossMarkerData
    {
        public readonly string spawnId;
        public readonly Vector3 worldPosition;
        public readonly bool discovered;
        public readonly bool isCentral;

        public CycleBossMarkerData(string spawnId, Vector3 worldPosition, bool discovered, bool isCentral)
        {
            this.spawnId = spawnId;
            this.worldPosition = worldPosition;
            this.discovered = discovered;
            this.isCentral = isCentral;
        }
    }

    public static class CycleBossMarkerRegistry
    {
        private static readonly Dictionary<string, CycleBossMarkerData> Markers = new();
        public static event Action<CycleBossMarkerData> OnMarkerAdded;
        public static event Action<CycleBossMarkerData> OnMarkerChanged;
        public static event Action<string> OnMarkerRemoved;

        public static IEnumerable<CycleBossMarkerData> GetAll() => Markers.Values;
        public static bool TryGet(string spawnId, out CycleBossMarkerData marker) => Markers.TryGetValue(spawnId, out marker);

        public static void Register(CycleBossMarkerData marker)
        {
            bool existed = Markers.ContainsKey(marker.spawnId);
            Markers[marker.spawnId] = marker;
            if (existed) OnMarkerChanged?.Invoke(marker); else OnMarkerAdded?.Invoke(marker);
        }

        public static void Remove(string spawnId)
        {
            if (Markers.Remove(spawnId)) OnMarkerRemoved?.Invoke(spawnId);
        }

        public static void Clear()
        {
            string[] ids = new string[Markers.Count];
            Markers.Keys.CopyTo(ids, 0);
            Markers.Clear();
            foreach (string id in ids) OnMarkerRemoved?.Invoke(id);
        }
    }
}
