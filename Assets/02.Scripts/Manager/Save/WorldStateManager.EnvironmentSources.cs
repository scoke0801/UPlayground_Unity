using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.Manager
{
    public partial class WorldStateManager
    {
        private struct EnvironmentSource
        {
            public Transform root;
            public Vector3 position;
            public string map;
            public int layer, blockers;
            public float radius, heat, water, windInfluence;
            public Vector2 wind;
        }
        private const float EnvironmentCellSize = 4;
        private readonly List<EnvironmentElementEmitter> _environmentEmitters = new();
        private readonly Dictionary<Vector3Int, List<EnvironmentSource>> _environmentSourceCells = new();
        private readonly Stack<List<EnvironmentSource>> _environmentCellPool = new();
        private readonly RaycastHit[] _environmentOcclusionHits = new RaycastHit[32];
        private float _environmentSourceRadius;

        /// <summary>지속 공급은 VFX 활성 개수와 무관하게 논리 공급원을 등록한다.</summary>
        public void RegisterEnvironmentEmitter(EnvironmentElementEmitter emitter)
        {
            if (emitter != null && !_environmentEmitters.Contains(emitter)) _environmentEmitters.Add(emitter);
        }
        /// <summary>순회 중 제거는 tombstone으로 표시한다.</summary>
        public void UnregisterEnvironmentEmitter(EnvironmentElementEmitter emitter)
        {
            int index = _environmentEmitters.IndexOf(emitter);
            if (index >= 0) _environmentEmitters[index] = null;
        }

        private void PrepareEnvironmentSources(float deltaTime)
        {
            foreach (var cell in _environmentSourceCells.Values) { cell.Clear(); _environmentCellPool.Push(cell); }
            _environmentSourceCells.Clear();
            _environmentSourceRadius = 0;
            // 이번 커밋에서 새로 점화될 대상은 이 스냅샷에 없으므로 다음 tick부터만 전파한다.
            foreach (var binding in _environmentBindings)
            {
                if (binding.target == null || binding.reaction.State.combustion != EnvironmentCombustion.Burning) continue;
                var definition = binding.target.Definition;
                if (definition.spreadRadius <= 0 || definition.spreadHeatPerSecond <= 0) continue;
                AddEnvironmentSource(new EnvironmentSource
                {
                    root = binding.target.transform, position = binding.target.ReactionPosition,
                    map = binding.record.mapId, layer = definition.surfaceLayerId,
                    radius = definition.spreadRadius, heat = definition.spreadHeatPerSecond,
                    blockers = definition.fireBlockerLayers.value, wind = binding.target.Wind,
                    windInfluence = definition.windInfluence
                });
            }
            for (int i = 0; i < _environmentEmitters.Count; i++)
            {
                var emitter = _environmentEmitters[i];
                if (emitter == null || !emitter.IsEmitting) continue;
                if (!EnvironmentReaction.IsNonNegativeFinite(emitter.radius) || emitter.radius == 0
                    || !EnvironmentReaction.IsNonNegativeFinite(emitter.heatPerSecond)
                    || !EnvironmentReaction.IsNonNegativeFinite(emitter.waterPerSecond)) continue;
                float fraction = emitter.AdvanceLifetime(deltaTime);
                AddEnvironmentSource(new EnvironmentSource
                {
                    root = emitter.transform, position = emitter.transform.position,
                    map = World.RegionOwnership.Resolve(emitter), layer = emitter.surfaceLayerId,
                    radius = emitter.radius, heat = emitter.heatPerSecond * fraction, water = emitter.waterPerSecond * fraction,
                    blockers = emitter.blockerLayers.value
                });
            }
            _environmentEmitters.RemoveAll(emitter => emitter == null);
        }

        private void AddEnvironmentSource(EnvironmentSource source)
        {
            Vector3Int cell = GetEnvironmentCell(source.position);
            if (!_environmentSourceCells.TryGetValue(cell, out var sources))
            {
                sources = _environmentCellPool.Count > 0 ? _environmentCellPool.Pop() : new List<EnvironmentSource>();
                _environmentSourceCells.Add(cell, sources);
            }
            sources.Add(source);
            _environmentSourceRadius = Mathf.Max(_environmentSourceRadius, source.radius);
        }

        private void ApplyEnvironmentSources(EnvironmentBinding binding, float seconds)
        {
            if (_environmentSourceCells.Count == 0) return;
            Vector3 position = binding.target.ReactionPosition;
            Vector3Int cell = GetEnvironmentCell(position);
            int radius = Mathf.CeilToInt(_environmentSourceRadius / EnvironmentCellSize);
            double heat = 0, water = 0;
            for (int z = -radius; z <= radius; z++)
            for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++)
            {
                if (!_environmentSourceCells.TryGetValue(cell + new Vector3Int(x, y, z), out var sources)) continue;
                foreach (var source in sources)
                {
                    if (source.root == null || source.root == binding.target.transform
                        || source.map != binding.record.mapId || source.layer != binding.target.Definition.surfaceLayerId) continue;
                    Vector3 direction = position - source.position;
                    float distance = direction.magnitude;
                    if (distance >= source.radius || IsEnvironmentSourceBlocked(source, binding.target.transform, direction, distance)) continue;
                    float falloff = 1 - distance / source.radius;
                    Vector2 horizontal = new(direction.x, direction.z);
                    float wind = 1 + Vector2.Dot(source.wind, horizontal.normalized) * source.windInfluence;
                    heat += source.heat * falloff * Mathf.Clamp(wind, 0.5f, 1.5f);
                    water += source.water * falloff;
                }
            }
            binding.reaction.AddEmitterInput((float)System.Math.Min(float.MaxValue, heat),
                (float)System.Math.Min(float.MaxValue, water), seconds);
        }

        private bool IsEnvironmentSourceBlocked(EnvironmentSource source, Transform target, Vector3 direction, float distance)
        {
            if (source.blockers == 0 || distance <= 0) return false;
            int count = Physics.RaycastNonAlloc(source.position, direction / distance, _environmentOcclusionHits,
                distance, source.blockers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform hit = _environmentOcclusionHits[i].transform;
                if (!hit.IsChildOf(source.root) && !hit.IsChildOf(target)) return true;
            }
            return count == _environmentOcclusionHits.Length;
        }

        private static Vector3Int GetEnvironmentCell(Vector3 position) => new(
            Mathf.FloorToInt(position.x / EnvironmentCellSize), Mathf.FloorToInt(position.y / EnvironmentCellSize),
            Mathf.FloorToInt(position.z / EnvironmentCellSize));
    }
}
