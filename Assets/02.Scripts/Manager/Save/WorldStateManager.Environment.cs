using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.Save;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.Manager
{
    public partial class WorldStateManager
    {
        private static readonly Unity.Profiling.ProfilerMarker EnvironmentTickMarker = new("ReactiveEnvironment.Tick");
        private static readonly Unity.Profiling.ProfilerMarker EnvironmentSaveMarker = new("ReactiveEnvironment.Export");
        public int EnvironmentTargetCount => _environmentLookup.Count;
        public int EnvironmentRecordCount => _environmentStates.Count;
        private sealed class EnvironmentBinding
        {
            public IEnvironmentReactionTarget target;
            public ReactiveEnvironmentSaveEntry record;
            public EnvironmentReaction reaction;
            public float sourceHeat, sourceWater;
            public int rainSamples;
            public IEnvironmentRainBatchTarget rainTarget;
            public ReactiveEnvironmentDefinitionSO definition;
            public bool rewardFailureReported;
        }

        private Dictionary<(string, string), ReactiveEnvironmentSaveEntry> _environmentStates = new();
        private List<EnvironmentBinding> _environmentBindings = new();
        private readonly Dictionary<ReactiveEnvironmentDefinitionSO, int> _environmentDefinitions = new();

        private void RetainEnvironmentDefinition(EnvironmentBinding binding)
        {
            _environmentDefinitions.TryGetValue(binding.definition, out int count);
            _environmentDefinitions[binding.definition] = count + 1;
        }

        private void ReleaseEnvironmentDefinition(EnvironmentBinding binding)
        {
            if (ReferenceEquals(binding.definition, null)) return;
            if (_environmentDefinitions.TryGetValue(binding.definition, out int count))
            {
                if (count == 1) _environmentDefinitions.Remove(binding.definition);
                else _environmentDefinitions[binding.definition] = count - 1;
            }
            binding.definition = null;
        }
        private readonly EnvironmentRainBatch _environmentRain = new();
        private int _environmentRainCapacity;
        private readonly List<TerrainDetailReaction> _detailTerrains = new();
        /// <summary>셀을 소유하는 Terrain만 한 번 등록한다.</summary>
        public void RegisterDetailTerrain(TerrainDetailReaction terrain)
        { if (!_detailTerrains.Contains(terrain)) _detailTerrains.Add(terrain); }
        /// <summary>언로드된 Terrain의 범위 조회를 해제한다.</summary>
        public void UnregisterDetailTerrain(TerrainDetailReaction terrain) => _detailTerrains.Remove(terrain);
        /// <summary>소유 맵의 Terrain 셀을 기존 전투 중복 제거 목록으로 수집한다.</summary>
        public void CollectCellHits(Transform owner, in UPlayGround.Combat.CombatHitboxShape shape,
            ISet<IDamageable> ignored, ISet<IDamageable> collected, List<UPlayGround.Combat.CombatHit> results)
        {
            if (owner == null) return;
            string map = World.RegionOwnership.Resolve(owner);
            foreach (var terrain in _detailTerrains)
                if (terrain != null && terrain.isActiveAndEnabled && World.RegionOwnership.Resolve(terrain) == map)
                    terrain.CollectHits(shape, ignored, collected, results);
        }
        private void FlushEnvironmentMasks()
        {
            foreach (var terrain in _detailTerrains) if (terrain != null) terrain.FlushMask();
        }
        private Dictionary<IEnvironmentReactionTarget, EnvironmentBinding> _environmentLookup = new();

        /// <summary>진단 창에서 pending 입력과 최근 tick 공급 수지를 읽는다.</summary>
        public string DescribeEnvironmentInputs(IEnvironmentReactionTarget target)
        {
            if (!_environmentLookup.TryGetValue(target, out var binding)) return "등록되지 않음";
            binding.reaction.GetPendingInputs(out double heat, out double water, out double continuousWater);
            var p = target.Definition.reaction;
            return $"map={binding.record.mapId} layer={target.Definition.surfaceLayerId} step={target.Definition.tickSeconds:F3}\n"
                + $"pending heat={heat:F4} waterImpulse={water:F4} waterRateIntegrated={continuousWater:F4}\n"
                + $"최근 source heat/s={binding.sourceHeat:F4} water/s={binding.sourceWater:F4}\n"
                + $"self={p.selfHeat:F3} cooling={p.cooling:F3} rainCooling={p.rainCooling:F3} absorption={p.rainAbsorption:F3} evaporation={p.evaporation:F3}";
        }
        private Dictionary<(string, string), IEnvironmentReactionTarget> _environmentActiveKeys = new();
        private bool _isTickingEnvironment;
        private bool _environmentNeedsFixedTick;
        private bool _environmentRewardsPending;
        internal void RequestEnvironmentFixedTick() => _environmentNeedsFixedTick = true;
        private float _environmentRemainder;
        public uint EnvironmentCollisionRevision { get; private set; }
        public uint EnvironmentShelterRevision { get; private set; }
        public uint EnvironmentNavigationRevision { get; private set; }
        public event Action<Bounds> OnEnvironmentStructureChanged;

        /// <summary>물리·차폐·길찾기 캐시를 서로 독립된 revision으로 무효화한다.</summary>
        public void NotifyEnvironmentStructureChanged(IEnvironmentReactionTarget target, Bounds bounds)
        {
            EnvironmentCollisionRevision++;
            var recipe = target.Definition.destruction;
            if (recipe != null && recipe.changesShelter) EnvironmentShelterRevision++;
            if (recipe != null && recipe.changesNavigation) EnvironmentNavigationRevision++;
            OnEnvironmentStructureChanged?.Invoke(bounds);
        }

        private void FixedTickEnvironment(float seconds)
        {
            int count = _environmentBindings.Count;
            _isTickingEnvironment = true;
            try
            {
                if (_environmentNeedsFixedTick)
                {
                    _environmentNeedsFixedTick = false;
                    for (int i = 0; i < count; i++)
                    {
                        var binding = _environmentBindings[i];
                        var target = binding.target;
                        if (target == null || !target.HasPendingFixedTick || !target.IsAvailable) continue;
                        target.FixedTick(seconds);
                        AdvanceEnvironmentCompletion(binding, seconds);
                        _environmentNeedsFixedTick |= target.HasPendingFixedTick;
                    }
                }
                foreach (var terrain in _detailTerrains) if (terrain != null) terrain.TickPresentation(seconds);
            }
            finally
            {
                FlushEnvironmentMasks();
                _isTickingEnvironment = false;
                // 파괴된 Unity 참조의 전수 정리는 논리 tick에서 수행한다.
                _environmentBindings.RemoveAll(binding => binding.target == null);
            }
        }

        /// <summary>소유 지역+배치 GUID로 기존 저장 상태를 연결한다.</summary>
        public bool RegisterEnvironment(IEnvironmentReactionTarget target)
        {
            if (target == null || !target.IsAvailable || !target.IsAuthoringValid())
            {
                Debug.LogError("반응형 환경의 정의·ID·표현·날씨·채집 중복 구성을 확인하세요.", target?.transform);
                return false;
            }
            string map = World.RegionOwnership.Resolve(target.transform);
            if (string.IsNullOrEmpty(map))
            {
                Debug.LogError("반응형 환경의 소유 지역을 확인할 수 없습니다.", target?.transform);
                return false;
            }
            if (_environmentLookup.ContainsKey(target)) return true;
            if (_environmentActiveKeys.TryGetValue((map, target.StableId), out var existing) && existing.IsAvailable)
            {
                Debug.LogError("같은 지역의 반응형 환경 GUID가 중복되었습니다.", target.transform);
                return false;
            }
            var key = (map, target.StableId);
            if (!_environmentStates.TryGetValue(key, out var record))
            {
                record = CreateEnvironmentRecord(target, map, IsInteractableConsumed(map, target.StableId));
                _environmentStates.Add(key, record);
            }
            try
            {
                var binding = CreateEnvironmentBinding(target, record);
                if (Application.isPlaying) _environmentRain.Reserve(_environmentRainCapacity + binding.rainSamples);
                _environmentRainCapacity += binding.rainSamples;
                _environmentBindings.Add(binding);
                _environmentRewardsPending = true;
                RetainEnvironmentDefinition(binding);
                _environmentLookup.Add(target, binding);
                _environmentActiveKeys[key] = target;
                if (target is ReactiveEnvironmentObject sceneObject)
                {
                    sceneObject.AttachWorldState(this);
                    if (!HasEndedEnvironmentReaction(binding)) sceneObject.AttachRainPositions(_environmentRain);
                }
                target.ApplySnapshot(binding.reaction.State, binding.record.destruction);
                _environmentNeedsFixedTick |= target.HasPendingFixedTick;
                return true;
            }
            catch (ArgumentException error)
            {
                Debug.LogError(error.Message, target?.transform);
                return false;
            }
        }

        /// <summary>언로드 때 상태는 유지하고 tick 목록만 커밋 장벽에서 정리한다.</summary>
        public void UnregisterEnvironment(IEnvironmentReactionTarget target)
        {
            if (_environmentLookup.Remove(target, out var binding))
            {
                if (target is ReactiveEnvironmentObject sceneObject) sceneObject.DetachRainPositions(_environmentRain);
                CaptureEnvironment(binding);
                ReleaseEnvironmentDefinition(binding);
                _environmentRainCapacity -= binding.rainSamples;
                _environmentActiveKeys.Remove((binding.record.mapId, binding.record.guid));
                binding.target = null;
            }
            if (!_isTickingEnvironment)
                _environmentBindings.RemoveAll(binding =>
                {
                    if (binding.target != null && binding.target.IsAvailable) return false;
                    ReleaseEnvironmentDefinition(binding);
                    return true;
                });
        }

        /// <summary>환경 입력 키의 중복을 제거하고 구조 피해를 즉시 커밋한다.</summary>
        public bool ApplyEnvironmentImpact(IEnvironmentReactionTarget target, ulong attackId, uint sequence,
            float damage, float heat, float water)
        {
            if (target != null && target.IsAvailable && _environmentLookup.TryGetValue(target, out var binding))
            {

                if (binding.record.destruction.completion?.phase >= EnvironmentCompletionPhase.Settled
                    || !binding.reaction.TryApplyImpact(attackId, sequence, damage, heat, water)) return false;
                CaptureEnvironment(binding);
                target.ApplySnapshot(binding.reaction.State, binding.record.destruction);
                _environmentNeedsFixedTick |= target.HasPendingFixedTick;
                return true;
            }
            return false;
        }

        /// <summary>기존 검출기의 대상 중복 제거를 유지하며 원소 실행 키와 분리한다.</summary>
        public bool ApplyEnvironmentCombatHit(IEnvironmentReactionTarget target, float damage,
            UPlayGround.Data.ReactiveEnvironment.EnvironmentHitPayload payload = default)
        {
            if (_environmentLookup.TryGetValue(target, out var binding))
            {

                if (binding.record.destruction.completion?.phase >= EnvironmentCompletionPhase.Settled
                    || !payload.IsValid || !binding.reaction.TryApplyCombatDamage(damage,
                    payload.heatImpulse, payload.waterImpulse, payload.kind)) return false;
                CaptureEnvironment(binding);
                target.ApplySnapshot(binding.reaction.State, binding.record.destruction);
                _environmentNeedsFixedTick |= target.HasPendingFixedTick;
                return true;
            }
            return false;
        }

        private void TickEnvironment(float deltaTime)
        {
            if (deltaTime <= 0) return;
            using var profile = EnvironmentTickMarker.Auto();
            float domainStep = 0.2f;
            foreach (var entry in _environmentDefinitions)
                domainStep = Mathf.Min(domainStep, entry.Key.tickSeconds);
            _environmentRemainder += deltaTime;
            // 논리 스텝과 대기 보상이 없는 프레임은 전체 목록을 순회하지 않는다.
            if (_environmentRemainder < domainStep && !_environmentRewardsPending) return;
            _isTickingEnvironment = true;
            int count = _environmentBindings.Count;
            try
            {
                while (_environmentRemainder >= domainStep)
                {
                    _environmentRemainder -= domainStep;
                    PrepareEnvironmentSources(domainStep);
                    AdvanceEnvironmentReactions(domainStep, count);
                    // 모든 논리 결과를 계산한 뒤 표현·충돌 전이를 적용한다.
                    for (int i = 0; i < count; i++)
                        if (_environmentBindings[i].target != null && !HasEndedEnvironmentReaction(_environmentBindings[i]))
                        {
                            _environmentBindings[i].target.ApplySnapshot(_environmentBindings[i].record.state, _environmentBindings[i].record.destruction);
                            _environmentNeedsFixedTick |= _environmentBindings[i].target != null
                                && _environmentBindings[i].target.HasPendingFixedTick;
                        }
                }
                _environmentRewardsPending = false;
                for (int i = 0; i < count; i++)
                {
                    var binding = _environmentBindings[i];
                    if (!binding.record.state.isBroken || binding.record.rewardCommitted || !CanCommitEnvironmentReward(binding)) continue;
                    if ((binding.target == null || !binding.target.IsAvailable))
                    { _environmentRewardsPending = true; continue; }
                    CommitEnvironmentReward(binding);
                    _environmentRewardsPending |= !binding.record.rewardCommitted;
                }
            }
            finally
            {
                _isTickingEnvironment = false;
                _environmentBindings.RemoveAll(binding => binding.target == null);
            }
        }

        private void AdvanceEnvironmentReactions(float domainStep, int count)
        {
            using var rainScope = _environmentRain.Begin();
            // 순수 샘플만 먼저 모으고 동일 tick의 물리 결과를 논리 계산 전에 확정한다.
            for (int i = 0; i < count; i++)
            {
                var binding = _environmentBindings[i];
                if (binding.target == null) continue;
                // 한 논리 스텝에서만 생존을 확인한다. 이후 비활성/파괴는 Unregister가 tombstone으로 반영한다.
                if (!binding.target.IsAvailable)
                {
                    if (binding.target is ReactiveEnvironmentObject sceneObject) sceneObject.DetachRainPositions(_environmentRain);
                    ReleaseEnvironmentDefinition(binding);
                    binding.target = null;
                    continue;
                }
                if (!HasEndedEnvironmentReaction(binding) && binding.rainSamples > 0
                    && binding.record.tickRemainder + domainStep >= binding.target.Definition.tickSeconds)
                    binding.rainTarget.CollectRainSamples(_environmentRain);
            }
            _environmentRain.Complete();
            for (int i = 0; i < count; i++)
            {
                var binding = _environmentBindings[i];
                if (binding.target == null || HasEndedEnvironmentReaction(binding)) continue;
                float step = binding.target.Definition.tickSeconds;
                binding.record.tickRemainder += domainStep;
                ApplyEnvironmentSources(binding, domainStep);
                while (binding.record.tickRemainder >= step)
                {
                    binding.record.tickRemainder -= step;
                    binding.target.SampleRain(out float rain, out float exposure);
                    binding.reaction.Tick(step, rain, exposure);
                }
                CaptureEnvironment(binding);
            }
        }

        private void CaptureEnvironment(EnvironmentBinding binding)
        {
            binding.record.state = binding.reaction.State;
            if (ClearsEnvironment(binding) && binding.record.state.isBroken
                && binding.record.destruction.completion.phase == EnvironmentCompletionPhase.Standing)
                binding.record.destruction.completion.phase = EnvironmentCompletionPhase.Falling;
            _environmentRewardsPending |= binding.record.state.isBroken && !binding.record.rewardCommitted && CanCommitEnvironmentReward(binding);
            binding.reaction.GetPendingInputs(out binding.record.pendingHeat, out binding.record.pendingWater, out binding.record.pendingContinuousWater);
        }

        private static void CommitEnvironmentReward(EnvironmentBinding binding)
        {
            var record = binding.record;
            if (!record.state.isBroken || record.rewardCommitted || !CanCommitEnvironmentReward(binding)) return;
            if (!Services.TryGet<IItemService>(out var items) || !items.IsItemDBLoaded
                || !Services.TryGet<IGroundDropService>(out var drops)
                || drops.Settings == null) return;
            var table = record.state.breakCause == EnvironmentBreakCause.Fire
                ? binding.target.Definition.fireDrops : binding.target.Definition.impactDrops;
            if (record.state.breakCause == EnvironmentBreakCause.Impact
                && binding.target is ReactiveEnvironmentObject gathering && gathering.OwnsGathering)
                table = gathering.GatheringDrops;
            try
            {
                var rewards = items.GetDropItemList(table);
                // 동일 ID는 드롭의 생성·획득·로드 이후에도 기존 드롭 원장에 남는다.
                string sourceId = "environment:" + record.guid + ":0";
                var completion = record.destruction.completion;
                Vector3 position = completion != null && completion.hasRewardPosition
                    ? WorldCoordinates.ToWorld(record.mapId, completion.rewardPosition.ToVector3()) : binding.target.transform.position;
                drops.SpawnItems(rewards, position, sourceId, binding.target.transform);
                record.rewardCommitted = true;
                binding.rewardFailureReported = false;
            }
            catch (Exception error)
            {
                if (!binding.rewardFailureReported) Debug.LogException(error, binding.target.transform);
                binding.rewardFailureReported = true;
            }
        }

        private static ReactiveEnvironmentSaveEntry CreateEnvironmentRecord(IEnvironmentReactionTarget target, string map,
            bool previouslyConsumed = false)
        {
            var reaction = new EnvironmentReaction(target.Definition.reaction);
            bool consumed = previouslyConsumed && target is ReactiveEnvironmentObject gathering && gathering.OwnsGathering;
            if (consumed) reaction.TryApplyCombatDamage(target.Definition.reaction.maxDurability);
            var record = new ReactiveEnvironmentSaveEntry { mapId = map, guid = target.StableId, definitionId = target.Definition.definitionId,
                persistenceSignature = target.PersistenceSignature, state = reaction.State, rewardCommitted = consumed };
            PrepareEnvironmentCompletion(target, record, !consumed);
            return record;
        }

        private static EnvironmentBinding CreateEnvironmentBinding(IEnvironmentReactionTarget target, ReactiveEnvironmentSaveEntry record)
        {
            if (record.persistenceSignature != target.PersistenceSignature || record.schemaVersion != 1 || record.definitionId != target.Definition.definitionId
                || !EnvironmentReaction.IsNonNegativeFinite(record.tickRemainder)
                || record.tickRemainder >= target.Definition.tickSeconds)
                throw new ArgumentException("반응형 환경 저장의 정의·버전·tick 잔여 시간이 호환되지 않습니다.");
            var reaction = new EnvironmentReaction(target.Definition.reaction, record.state);
            record.destruction ??= new EnvironmentDestructionSaveState();
            if (!EnvironmentReaction.IsNonNegativeFinite(record.destruction.progress) || record.destruction.progress > 1
                || !IsFiniteDirection(record.destruction.direction.ToVector3()))
                throw new ArgumentException("저장된 파괴 포즈가 유효하지 않습니다.");
            reaction.RestorePendingInputs(record.pendingHeat, record.pendingWater, record.pendingContinuousWater);
            PrepareEnvironmentCompletion(target, record, false);
            if (record.destruction.completion?.phase >= EnvironmentCompletionPhase.Clearing)
            {
                reaction.RestorePendingInputs(0, 0);
                record.pendingHeat = record.pendingWater = record.pendingContinuousWater = 0;
            }
            var rainTarget = target as IEnvironmentRainBatchTarget;
            return new EnvironmentBinding { target = target, record = record, reaction = reaction, definition = target.Definition,
                rainTarget = rainTarget, rainSamples = rainTarget != null
                    && !(record.destruction.completion?.phase >= EnvironmentCompletionPhase.Clearing) ? rainTarget.RainSampleCapacity : 0 };
        }

        private void ExportEnvironment(WorldStateSaveData world)
        {
            using var profile = EnvironmentSaveMarker.Auto();
            ExportLightning(world);
            world.reactiveEnvironmentRemainder = _environmentRemainder;
            foreach (var binding in _environmentBindings) CaptureEnvironment(binding);
            world.reactiveEnvironment = new List<ReactiveEnvironmentSaveEntry>(_environmentStates.Count);
            foreach (var record in _environmentStates.Values) world.reactiveEnvironment.Add(CloneEnvironmentRecord(record));
        }

        private void ImportEnvironment(WorldStateSaveData world)
        {
            float remainder = world?.reactiveEnvironmentRemainder ?? 0;
            if (!EnvironmentReaction.IsNonNegativeFinite(remainder) || remainder >= 0.2f)
                throw new ArgumentException("환경 시계 잔여 시간이 유효하지 않습니다.");
            var lightningStates = PrepareLightningImport(world);
            var states = new Dictionary<(string, string), ReactiveEnvironmentSaveEntry>();
            var bindings = new List<EnvironmentBinding>();
            if (world?.reactiveEnvironment != null)
                foreach (var record in world.reactiveEnvironment)
                {
                    if (record == null || string.IsNullOrEmpty(record.mapId) || string.IsNullOrEmpty(record.guid)
                        || !states.TryAdd((record.mapId, record.guid), CloneEnvironmentRecord(record)))
                        throw new ArgumentException("반응형 환경 저장 키가 누락되거나 중복되었습니다.");
                }
            for (int i = 0; i < _environmentBindings.Count; i++)
            {
                var old = _environmentBindings[i];
                if ((old.target == null || !old.target.IsAvailable)) continue;
                var key = (old.record.mapId, old.record.guid);
                if (!states.TryGetValue(key, out var record))
                {
                    bool consumed = world?.consumedInteractables != null
                        && world.consumedInteractables.TryGetValue(old.record.mapId, out var guids)
                        && guids != null && guids.Contains(old.record.guid);
                    record = CreateEnvironmentRecord(old.target, old.record.mapId, consumed);
                    states.Add(key, record);
                }
                var binding = CreateEnvironmentBinding(old.target, record);
                bindings.Add(binding);
            }
            int rainCapacity = 0;
            foreach (var binding in bindings) rainCapacity += binding.rainSamples;
            if (Application.isPlaying) _environmentRain.Reserve(rainCapacity);
            // 모든 활성 배치를 검증한 뒤 한 번에 교체하여 실패한 로드가 기존 상태를 지우지 않게 한다.
            _lightningStates = lightningStates;
            _environmentStates = states;
            _environmentRemainder = remainder;
            _environmentBindings = bindings;
            _environmentRainCapacity = rainCapacity;
            _environmentRain.ResetPositionRegistrations();
            _environmentNeedsFixedTick = true;
            _environmentRewardsPending = true;
            _environmentDefinitions.Clear();
            foreach (var binding in bindings) RetainEnvironmentDefinition(binding);
            _environmentLookup = new Dictionary<IEnvironmentReactionTarget, EnvironmentBinding>();
            _environmentActiveKeys = new Dictionary<(string, string), IEnvironmentReactionTarget>();
            foreach (var binding in bindings)
            {
                if (binding.target is ReactiveEnvironmentObject sceneObject)
                {
                    if (HasEndedEnvironmentReaction(binding)) sceneObject.DetachRainPositions(_environmentRain);
                    else sceneObject.AttachRainPositions(_environmentRain);
                }
                _environmentLookup.Add(binding.target, binding);
                _environmentActiveKeys.Add((binding.record.mapId, binding.record.guid), binding.target);
            }
            int count = bindings.Count;
            for (int i = 0; i < count; i++)
                if (bindings[i].target != null && bindings[i].target.IsAvailable) bindings[i].target.ApplySnapshot(bindings[i].reaction.State, bindings[i].record.destruction);
            FlushEnvironmentMasks();
        }

        private static ReactiveEnvironmentSaveEntry CloneEnvironmentRecord(ReactiveEnvironmentSaveEntry record) =>
            new() { mapId = record.mapId, guid = record.guid, definitionId = record.definitionId,
                persistenceSignature = record.persistenceSignature, schemaVersion = record.schemaVersion, state = record.state, pendingHeat = record.pendingHeat,
                pendingContinuousWater = record.pendingContinuousWater,
                pendingWater = record.pendingWater, tickRemainder = record.tickRemainder,
                rewardCommitted = record.rewardCommitted, destruction = record.destruction?.Copy() ?? new EnvironmentDestructionSaveState() };

        private static bool IsFiniteDirection(Vector3 direction) =>
            !float.IsNaN(direction.x) && !float.IsInfinity(direction.x)
            && !float.IsNaN(direction.y) && !float.IsInfinity(direction.y)
            && !float.IsNaN(direction.z) && !float.IsInfinity(direction.z);
    }
}
