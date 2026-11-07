using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Simulation;

namespace UPlayGround.Manager
{
    /// <summary>개별 MonoBehaviour.Update 대신 매니저가 일괄 호출하는 틱 계약.</summary>
    public interface IManagedTick
    {
        void ManagedTick(float deltaTime);
    }

    /// <summary>Suspended에서 Active로 복귀한 프레임의 캐시/타이머 재기준화 계약.</summary>
    public interface IActorSimulationResumeHandler
    {
        void OnActorSimulationResumed();
    }

    /// <summary>
    /// 소유 액터별로 틱을 그룹화한다. Suspended 그룹은 활성 그룹 목록에서 제거하므로
    /// 원거리 액터 수가 늘어도 OnUpdate 순회량이 함께 늘지 않는다.
    /// </summary>
    public class AgentTickManager : BaseManager<AgentTickManager>, IManager, IUpdatableManager
    {
        private sealed class TickGroup
        {
            public GameActor Owner;
            public readonly List<IManagedTick> Ticks = new();
            public bool IsActive = true;
            public bool NeedsCompact;
        }

        private readonly Dictionary<GameActor, TickGroup> _groups = new();
        private readonly List<TickGroup> _activeGroups = new();
        private readonly List<IManagedTick> _ownerlessTicks = new();
        private readonly List<GameActor> _staleOwners = new();
        private readonly HashSet<TickGroup> _dirtyGroups = new();
        private bool _ownerlessNeedsCompact;
        private bool _isUpdating;

        public void Init()
        {
            ActorSimulationParticipant.AnyStateChanged += HandleSimulationStateChanged;
        }

        public void AfterInit() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void Dispose()
        {
            ActorSimulationParticipant.AnyStateChanged -= HandleSimulationStateChanged;
            _groups.Clear();
            _activeGroups.Clear();
            _ownerlessTicks.Clear();
            _staleOwners.Clear();
            _dirtyGroups.Clear();
            _ownerlessNeedsCompact = false;
            _isUpdating = false;
        }

        public void OnSceneChanged(string sceneType)
        {
            _staleOwners.Clear();
            foreach (KeyValuePair<GameActor, TickGroup> pair in _groups)
            {
                if (pair.Key == null)
                    _staleOwners.Add(pair.Key);
            }
            for (int i = 0; i < _staleOwners.Count; i++)
            {
                GameActor owner = _staleOwners[i];
                if (_groups.TryGetValue(owner, out TickGroup group))
                    RemoveGroup(group);
            }
            _staleOwners.Clear();
            _ownerlessTicks.RemoveAll(IsDead);
        }

        public void Register(GameActor owner, IManagedTick tick)
        {
            if (tick == null)
                return;
            if (owner == null)
            {
                if (!_ownerlessTicks.Contains(tick))
                    _ownerlessTicks.Add(tick);
                return;
            }

            if (!_groups.TryGetValue(owner, out TickGroup group))
            {
                group = new TickGroup { Owner = owner };
                ActorSimulationParticipant participant =
                    owner.GetComponent<ActorSimulationParticipant>();
                group.IsActive = participant == null || !participant.IsSuspended;
                _groups.Add(owner, group);
                if (group.IsActive)
                    _activeGroups.Add(group);
            }
            if (!group.Ticks.Contains(tick))
                group.Ticks.Add(tick);
        }

        public void Register(IManagedTick tick)
        {
            GameActor owner = tick is Component component
                ? component.GetComponent<GameActor>()
                : null;
            Register(owner, tick);
        }

        public void Unregister(IManagedTick tick)
        {
            if (tick == null)
                return;

            foreach (TickGroup group in _groups.Values)
            {
                if (MarkUnregistered(group, tick))
                    return;
            }

            int ownerlessIndex = _ownerlessTicks.IndexOf(tick);
            if (ownerlessIndex >= 0)
            {
                _ownerlessTicks[ownerlessIndex] = null;
                _ownerlessNeedsCompact = true;
            }
        }

        public void Unregister(GameActor owner, IManagedTick tick)
        {
            if (tick == null)
                return;
            if (owner != null && _groups.TryGetValue(owner, out TickGroup group) &&
                MarkUnregistered(group, tick))
            {
                return;
            }

            Unregister(tick);
        }

        public void OnUpdate()
        {
            float deltaTime = Time.deltaTime;
            _isUpdating = true;
            try
            {
                for (int groupIndex = 0; groupIndex < _activeGroups.Count; groupIndex++)
                {
                    TickGroup group = _activeGroups[groupIndex];
                    for (int tickIndex = 0; tickIndex < group.Ticks.Count; tickIndex++)
                        group.Ticks[tickIndex]?.ManagedTick(deltaTime);
                }

                for (int i = 0; i < _ownerlessTicks.Count; i++)
                    _ownerlessTicks[i]?.ManagedTick(deltaTime);
            }
            finally
            {
                _isUpdating = false;
                FlushDirtyGroups();
                if (_ownerlessNeedsCompact)
                {
                    _ownerlessTicks.RemoveAll(tick => tick == null);
                    _ownerlessNeedsCompact = false;
                }
            }
        }

        private void HandleSimulationStateChanged(GameActor owner, ActorSimulationState state)
        {
            if (owner == null || !_groups.TryGetValue(owner, out TickGroup group))
                return;

            bool active = state == ActorSimulationState.Active;
            if (group.IsActive == active)
                return;
            group.IsActive = active;
            if (active)
                _activeGroups.Add(group);
            else
                _activeGroups.Remove(group);
        }

        private static void Compact(TickGroup group)
        {
            if (!group.NeedsCompact)
                return;
            group.Ticks.RemoveAll(IsDead);
            group.NeedsCompact = false;
        }

        private bool MarkUnregistered(TickGroup group, IManagedTick tick)
        {
            int index = group.Ticks.IndexOf(tick);
            if (index < 0)
                return false;

            group.Ticks[index] = null;
            group.NeedsCompact = true;
            if (_isUpdating)
            {
                _dirtyGroups.Add(group);
            }
            else
            {
                Compact(group);
                if (group.Ticks.Count == 0)
                    RemoveGroup(group);
            }
            return true;
        }

        private void FlushDirtyGroups()
        {
            foreach (TickGroup group in _dirtyGroups)
            {
                Compact(group);
                if (group.Ticks.Count == 0)
                    RemoveGroup(group);
            }
            _dirtyGroups.Clear();
        }

        private void RemoveGroup(TickGroup group)
        {
            if (group == null)
                return;
            _activeGroups.Remove(group);
            if (!ReferenceEquals(group.Owner, null))
                _groups.Remove(group.Owner);
            group.Ticks.Clear();
            group.NeedsCompact = false;
        }

        private static bool IsDead(IManagedTick tick) =>
            tick == null || (tick is global::UnityEngine.Object obj && obj == null);
    }
}
