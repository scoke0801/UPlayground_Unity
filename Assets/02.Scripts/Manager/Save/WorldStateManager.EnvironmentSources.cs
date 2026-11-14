using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.Manager
{
    public partial class WorldStateManager
    {
        public int EnvironmentSourceCount { get; private set; }
        public int EnvironmentOcclusionQueries { get; private set; }
        private struct EnvironmentSource
        {
            public Transform root, ignoreRoot;
            public IEnvironmentReactionTarget owner;
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
        private Vector3Int _environmentSourceMinCell, _environmentSourceMaxCell;

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
            EnvironmentSourceCount = 0; EnvironmentOcclusionQueries = 0;
            // 이번 커밋에서 새로 점화될 대상은 이 스냅샷에 없으므로 다음 tick부터만 전파한다.
            foreach (var binding in _environmentBindings)
            {
                if ((binding.target == null || !binding.target.IsAvailable) || binding.reaction.State.combustion != EnvironmentCombustion.Burning) continue;
                var definition = binding.target.Definition;
                if (definition.spreadRadius <= 0 || definition.spreadHeatPerSecond <= 0) continue;
                AddEnvironmentSource(new EnvironmentSource
                {
                    owner = binding.target, root = binding.target.transform, ignoreRoot = binding.target.OcclusionRoot, position = binding.target.ReactionPosition,
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
                    root = emitter.transform, ignoreRoot = emitter.transform, position = emitter.transform.position,
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
            EnvironmentSourceCount++;
            if (EnvironmentSourceCount == 1) _environmentSourceMinCell = _environmentSourceMaxCell = cell;
            else
            {
                _environmentSourceMinCell = Vector3Int.Min(_environmentSourceMinCell, cell);
                _environmentSourceMaxCell = Vector3Int.Max(_environmentSourceMaxCell, cell);
            }
            _environmentSourceRadius = Mathf.Max(_environmentSourceRadius, source.radius);
        }

        private void ApplyEnvironmentSources(EnvironmentBinding binding, float seconds)
        {
            binding.sourceHeat = binding.sourceWater = 0;
            if (_environmentSourceCells.Count == 0) return;
            Vector3 position = binding.target.ReactionPosition;
            Vector3Int cell = GetEnvironmentCell(position);
            int radius = Mathf.CeilToInt(_environmentSourceRadius / EnvironmentCellSize);
            double heat = 0, water = 0;
            // 공급원이 없는 바깥 격자는 건너뛰되 기존 비어 있지 않은 셀 순서는 유지한다.
            Vector3Int minimum = Vector3Int.Max(cell - Vector3Int.one * radius, _environmentSourceMinCell);
            Vector3Int maximum = Vector3Int.Min(cell + Vector3Int.one * radius, _environmentSourceMaxCell);
            for (int z = minimum.z; z <= maximum.z; z++)
            for (int y = minimum.y; y <= maximum.y; y++)
            for (int x = minimum.x; x <= maximum.x; x++)
            {
                if (!_environmentSourceCells.TryGetValue(new Vector3Int(x, y, z), out var sources)) continue;
                foreach (var source in sources)
                {
                    if (source.root == null || source.owner == binding.target
                        || source.owner == null && binding.target is ReactiveEnvironmentObject && source.root == binding.target.transform
                        || source.map != binding.record.mapId || source.layer != binding.target.Definition.surfaceLayerId) continue;
                    Vector3 direction = position - source.position;
                    float distance = direction.magnitude;
                    if (distance >= source.radius || IsEnvironmentSourceBlocked(source, binding.target.OcclusionRoot, direction, distance)) continue;
                    float falloff = 1 - distance / source.radius;
                    Vector2 horizontal = new(direction.x, direction.z);
                    float wind = 1 + Vector2.Dot(source.wind, horizontal.normalized) * source.windInfluence;
                    heat += source.heat * falloff * Mathf.Clamp(wind, 0.5f, 1.5f);
                    water += source.water * falloff;
                }
            }
            binding.sourceHeat = (float)System.Math.Min(float.MaxValue, heat);
            binding.sourceWater = (float)System.Math.Min(float.MaxValue, water);
            binding.reaction.AddEmitterInput(binding.sourceHeat, binding.sourceWater, seconds);
        }

        /// <summary>최근 tick 시작 공급원의 거리·바람·차폐·구획을 진단할 때만 열거한다.</summary>
        public string DescribeEnvironmentSources(IEnvironmentReactionTarget target)
        {
            if (!_environmentLookup.TryGetValue(target, out var binding)) return "등록되지 않음";
            var text = new System.Text.StringBuilder();
            foreach (var cell in _environmentSourceCells.Values)
            foreach (var source in cell)
            {
                if (source.owner == target || source.root == null) continue;
                var direction = target.ReactionPosition - source.position;
                float distance = direction.magnitude;
                if (distance > source.radius) continue;
                bool compatible = source.map == binding.record.mapId && source.layer == target.Definition.surfaceLayerId;
                bool blocked = IsEnvironmentSourceBlocked(source, target.OcclusionRoot, direction, distance, false);
                text.AppendLine($"{source.root.name} d={distance:F2}/{source.radius:F2} map={source.map} layer={source.layer} compatible={compatible} blocked={blocked} heat/s={source.heat:F3} wind={source.wind}");
            }
            return text.Length == 0 ? "최근 공급원 스냅샷의 반경 내 이웃 없음" : text.ToString();
        }

        private bool IsEnvironmentSourceBlocked(EnvironmentSource source, Transform target, Vector3 direction, float distance, bool countQuery = true)
        {
            if (source.blockers == 0 || distance <= 0) return false;
            if (countQuery) EnvironmentOcclusionQueries++;
            int count = Physics.RaycastNonAlloc(source.position, direction / distance, _environmentOcclusionHits,
                distance, source.blockers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform hit = _environmentOcclusionHits[i].transform;
                if ((source.ignoreRoot == null || !hit.IsChildOf(source.ignoreRoot))
                    && (target == null || !hit.IsChildOf(target))) return true;
            }
            return count == _environmentOcclusionHits.Length;
        }

        private static Vector3Int GetEnvironmentCell(Vector3 position) => new(
            Mathf.FloorToInt(position.x / EnvironmentCellSize), Mathf.FloorToInt(position.y / EnvironmentCellSize),
            Mathf.FloorToInt(position.z / EnvironmentCellSize));
    }
}
