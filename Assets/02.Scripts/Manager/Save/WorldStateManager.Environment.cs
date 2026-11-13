using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.Save;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.Manager
{
    public partial class WorldStateManager
    {
        private sealed class EnvironmentBinding
        {
            public ReactiveEnvironmentObject target;
            public ReactiveEnvironmentSaveEntry record;
            public EnvironmentReaction reaction;
        }

        private Dictionary<(string, string), ReactiveEnvironmentSaveEntry> _environmentStates = new();
        private List<EnvironmentBinding> _environmentBindings = new();
        private bool _isTickingEnvironment;
        private float _environmentRemainder;
        public uint EnvironmentCollisionRevision { get; private set; }
        public uint EnvironmentShelterRevision { get; private set; }
        public uint EnvironmentNavigationRevision { get; private set; }
        public event Action<Bounds> OnEnvironmentStructureChanged;

        /// <summary>물리·차폐·길찾기 캐시를 서로 독립된 revision으로 무효화한다.</summary>
        public void NotifyEnvironmentStructureChanged(ReactiveEnvironmentObject target, Bounds bounds)
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
                for (int i = 0; i < count; i++)
                    if (_environmentBindings[i].target != null) _environmentBindings[i].target.FixedTick(seconds);
            }
            finally
            {
                _isTickingEnvironment = false;
                _environmentBindings.RemoveAll(binding => binding.target == null);
            }
        }

        /// <summary>소유 지역+배치 GUID로 기존 저장 상태를 연결한다.</summary>
        public bool RegisterEnvironment(ReactiveEnvironmentObject target)
        {
            if (target == null || !target.IsAuthoringValid())
            {
                Debug.LogError("반응형 환경의 정의·ID·표현·날씨·채집 중복 구성을 확인하세요.", target);
                return false;
            }
            string map = World.RegionOwnership.Resolve(target.transform);
            if (string.IsNullOrEmpty(map))
            {
                Debug.LogError("반응형 환경의 소유 지역을 확인할 수 없습니다.", target);
                return false;
            }
            foreach (var binding in _environmentBindings)
            {
                if (binding.target == target) return true;
                if (binding.target != null && binding.record.mapId == map && binding.record.guid == target.StableId)
                {
                    Debug.LogError("같은 지역의 반응형 환경 GUID가 중복되었습니다.", target);
                    return false;
                }
            }
            var key = (map, target.StableId);
            if (!_environmentStates.TryGetValue(key, out var record))
            {
                record = CreateEnvironmentRecord(target, map);
                _environmentStates.Add(key, record);
            }
            try
            {
                var binding = CreateEnvironmentBinding(target, record);
                _environmentBindings.Add(binding);
                target.ApplySnapshot(binding.reaction.State, binding.record.destruction);
                return true;
            }
            catch (ArgumentException error)
            {
                Debug.LogError(error.Message, target);
                return false;
            }
        }

        /// <summary>언로드 때 상태는 유지하고 tick 목록만 커밋 장벽에서 정리한다.</summary>
        public void UnregisterEnvironment(ReactiveEnvironmentObject target)
        {
            foreach (var binding in _environmentBindings)
                if (binding.target == target) { CaptureEnvironment(binding); binding.target = null; }
            if (!_isTickingEnvironment) _environmentBindings.RemoveAll(binding => binding.target == null);
        }

        /// <summary>환경 입력 키의 중복을 제거하고 구조 피해를 즉시 커밋한다.</summary>
        public bool ApplyEnvironmentImpact(ReactiveEnvironmentObject target, ulong attackId, uint sequence,
            float damage, float heat, float water)
        {
            foreach (var binding in _environmentBindings)
            {
                if (binding.target != target) continue;
                if (!binding.reaction.TryApplyImpact(attackId, sequence, damage, heat, water)) return false;
                CaptureEnvironment(binding);
                target.ApplySnapshot(binding.reaction.State, binding.record.destruction);
                return true;
            }
            return false;
        }

        /// <summary>기존 검출기의 대상 중복 제거를 유지하며 원소 실행 키와 분리한다.</summary>
        public bool ApplyEnvironmentCombatHit(ReactiveEnvironmentObject target, float damage,
            UPlayGround.Data.ReactiveEnvironment.EnvironmentHitPayload payload = default)
        {
            foreach (var binding in _environmentBindings)
            {
                if (binding.target != target) continue;
                if (!payload.IsValid || !binding.reaction.TryApplyCombatDamage(damage,
                    payload.heatImpulse, payload.waterImpulse, payload.kind)) return false;
                CaptureEnvironment(binding);
                target.ApplySnapshot(binding.reaction.State, binding.record.destruction);
                return true;
            }
            return false;
        }

        private void TickEnvironment(float deltaTime)
        {
            if (deltaTime <= 0) return;
            _isTickingEnvironment = true;
            int count = _environmentBindings.Count;
            try
            {
                float domainStep = 0.2f;
                for (int i = 0; i < count; i++)
                    if (_environmentBindings[i].target != null)
                        domainStep = Mathf.Min(domainStep, _environmentBindings[i].target.Definition.tickSeconds);
                _environmentRemainder += deltaTime;
                while (_environmentRemainder >= domainStep)
                {
                    _environmentRemainder -= domainStep;
                    PrepareEnvironmentSources(domainStep);
                    for (int i = 0; i < count; i++)
                    {
                        var binding = _environmentBindings[i];
                        if (binding.target == null) continue;
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
                    // 모든 논리 결과를 계산한 뒤 표현·충돌 전이를 적용한다.
                    for (int i = 0; i < count; i++)
                        if (_environmentBindings[i].target != null)
                            _environmentBindings[i].target.ApplySnapshot(_environmentBindings[i].record.state, _environmentBindings[i].record.destruction);
                }
                for (int i = 0; i < count; i++)
                {
                    var binding = _environmentBindings[i];
                    if (binding.target == null) continue;
                    CommitEnvironmentReward(binding);
                }
            }
            finally
            {
                _isTickingEnvironment = false;
                _environmentBindings.RemoveAll(binding => binding.target == null);
            }
        }

        private static void CaptureEnvironment(EnvironmentBinding binding)
        {
            binding.record.state = binding.reaction.State;
            binding.reaction.GetPendingInputs(out binding.record.pendingHeat, out binding.record.pendingWater, out binding.record.pendingContinuousWater);
        }

        private static void CommitEnvironmentReward(EnvironmentBinding binding)
        {
            var record = binding.record;
            if (!record.state.isBroken || record.rewardCommitted) return;
            if (!Services.TryGet<IItemService>(out var items) || !items.IsItemDBLoaded
                || !Services.TryGet<IGroundDropService>(out var drops)
                || drops.Settings == null) return;
            var table = record.state.breakCause == EnvironmentBreakCause.Fire
                ? binding.target.Definition.fireDrops : binding.target.Definition.impactDrops;
            var rewards = items.GetDropItemList(table);
            // 동일 ID는 드롭의 생성·획득·로드 이후에도 기존 드롭 원장에 남는다.
            string sourceId = "environment:" + record.guid + ":0";
            drops.SpawnItems(rewards, binding.target.transform.position, sourceId, binding.target.transform);
            record.rewardCommitted = true;
        }

        private static ReactiveEnvironmentSaveEntry CreateEnvironmentRecord(ReactiveEnvironmentObject target, string map) =>
            new() { mapId = map, guid = target.StableId, definitionId = target.Definition.definitionId,
                state = new EnvironmentReaction(target.Definition.reaction).State };

        private static EnvironmentBinding CreateEnvironmentBinding(ReactiveEnvironmentObject target, ReactiveEnvironmentSaveEntry record)
        {
            if (record.schemaVersion != 1 || record.definitionId != target.Definition.definitionId
                || !EnvironmentReaction.IsNonNegativeFinite(record.tickRemainder)
                || record.tickRemainder >= target.Definition.tickSeconds)
                throw new ArgumentException("반응형 환경 저장의 정의·버전·tick 잔여 시간이 호환되지 않습니다.");
            var reaction = new EnvironmentReaction(target.Definition.reaction, record.state);
            record.destruction ??= new EnvironmentDestructionSaveState();
            if (!EnvironmentReaction.IsNonNegativeFinite(record.destruction.progress) || record.destruction.progress > 1
                || !IsFiniteDirection(record.destruction.direction.ToVector3()))
                throw new ArgumentException("저장된 파괴 포즈가 유효하지 않습니다.");
            reaction.RestorePendingInputs(record.pendingHeat, record.pendingWater, record.pendingContinuousWater);
            return new EnvironmentBinding { target = target, record = record, reaction = reaction };
        }

        private void ExportEnvironment(WorldStateSaveData world)
        {
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
                if (old.target == null) continue;
                var key = (old.record.mapId, old.record.guid);
                if (!states.TryGetValue(key, out var record))
                {
                    record = CreateEnvironmentRecord(old.target, old.record.mapId);
                    states.Add(key, record);
                }
                var binding = CreateEnvironmentBinding(old.target, record);
                bindings.Add(binding);
            }
            // 모든 활성 배치를 검증한 뒤 한 번에 교체하여 실패한 로드가 기존 상태를 지우지 않게 한다.
            _environmentStates = states;
            _environmentRemainder = remainder;
            _environmentBindings = bindings;
            int count = bindings.Count;
            for (int i = 0; i < count; i++)
                if (bindings[i].target != null) bindings[i].target.ApplySnapshot(bindings[i].reaction.State, bindings[i].record.destruction);
        }

        private static ReactiveEnvironmentSaveEntry CloneEnvironmentRecord(ReactiveEnvironmentSaveEntry record) =>
            new() { mapId = record.mapId, guid = record.guid, definitionId = record.definitionId,
                schemaVersion = record.schemaVersion, state = record.state, pendingHeat = record.pendingHeat,
                pendingContinuousWater = record.pendingContinuousWater,
                pendingWater = record.pendingWater, tickRemainder = record.tickRemainder,
                rewardCommitted = record.rewardCommitted, destruction = record.destruction?.Copy() ?? new EnvironmentDestructionSaveState() };

        private static bool IsFiniteDirection(Vector3 direction) =>
            !float.IsNaN(direction.x) && !float.IsInfinity(direction.x)
            && !float.IsNaN(direction.y) && !float.IsInfinity(direction.y)
            && !float.IsNaN(direction.z) && !float.IsInfinity(direction.z);
    }
}
