using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Runtime
{
    /// <summary>준비된 surface를 등록하고 양쪽 준비가 유지되는 동안만 링크를 연다.</summary>
    public sealed class WorldBakedNavigation : MonoBehaviour
    {
        public WorldBakeCatalog catalog;
        public WorldBridgeAccessGate[] crossingGates;
        [SerializeField] private bool registerLoadedScene = true;
        private readonly Dictionary<string, NavMeshDataInstance> _surfaces = new(StringComparer.Ordinal);
        private readonly HashSet<string> _readySurfaces = new(StringComparer.Ordinal);
        private NavMeshLinkInstance[] _links;
        private BakedCrossing[] _streamCrossings;
        private BakedCrossing[] Crossings => _streamCrossings ?? catalog.crossings;

        public void ConfigureStreaming(WorldBakeCatalog selected, BakedCrossing[] boundaries)
        {
            if (_links != null) OnDisable();
            catalog = selected;
            _streamCrossings = boundaries == null ? selected.crossings : selected.crossings.Concat(boundaries).ToArray();
            registerLoadedScene = false;
            if (isActiveAndEnabled) OnEnable();
        }

        internal void ClearStreaming()
        {
            OnDisable();
            _streamCrossings = null;
            catalog = null;
        }

        private void OnEnable()
        {
            if (catalog == null) return;
            _links = new NavMeshLinkInstance[Crossings.Length];
            if (!registerLoadedScene) return;
            foreach (var surface in catalog.surfaces) SetSurfaceReady(surface.id, true, true);
        }

        public bool IsLinkReady(int index) => _links != null && _links[index].valid;

        public void SetSurfaceReady(string id, bool navigationReady, bool collidersReady)
        {
            if (_links == null) throw new InvalidOperationException("Navigation revision이 활성 상태여야 합니다.");
            bool ready = navigationReady && collidersReady;
            if (ready)
            {
                PrepareSurface(id);
                _readySurfaces.Add(id);
            }
            else if (_surfaces.Remove(id, out var handle))
            {
                _readySurfaces.Remove(id);
                RefreshLinks();
                handle.Remove();
                return;
            }
            RefreshLinks();
        }

        internal void PrepareSurface(string id)
        {
            if (_surfaces.ContainsKey(id)) return;
            int index = Array.FindIndex(catalog.surfaces, surface => surface.id == id);
            if (index < 0) throw new ArgumentException("알 수 없는 surface ID", nameof(id));
            var handle = NavMesh.AddNavMeshData(catalog.surfaces[index].data);
            if (!handle.valid) throw new InvalidOperationException("NavMesh surface 등록 실패 · " + id);
            _surfaces.Add(id, handle);
        }

        private void RefreshLinks()
        {
            for (int i = 0; i < Crossings.Length; i++)
            {
                var link = Crossings[i];
                bool first = _readySurfaces.Contains(link.fromSurface), second = _readySurfaces.Contains(link.toSurface);
                if (!(first && second) && _links[i].valid) _links[i].Remove();
                if (first && second && !_links[i].valid) _links[i] = NavMesh.AddLink(LinkData(link, catalog.agentTypeId));
                var gate = crossingGates != null && i < crossingGates.Length ? crossingGates[i] : null;
                if (gate == null) continue;
                gate.SetEndpointReady(0, first && _links[i].valid);
                gate.SetEndpointReady(1, second && _links[i].valid);
            }
        }

        public static NavMeshLinkData LinkData(BakedCrossing link, int agentType) => new()
        {
            startPosition = link.from, endPosition = link.to, width = link.width,
            agentTypeID = agentType, area = 0, costModifier = -1, bidirectional = true
        };

        private void OnDisable()
        {
            if (_links == null) return;
            foreach (var link in _links) if (link.valid) link.Remove();
            foreach (var handle in _surfaces.Values) if (handle.valid) handle.Remove();
            _surfaces.Clear();
            _readySurfaces.Clear();
            if (crossingGates != null) foreach (var gate in crossingGates)
            {
                if (gate == null || !gate.IsConfigured) continue;
                gate.SetEndpointReady(0, false);
                gate.SetEndpointReady(1, false);
            }
            _links = null;
        }
    }
}


