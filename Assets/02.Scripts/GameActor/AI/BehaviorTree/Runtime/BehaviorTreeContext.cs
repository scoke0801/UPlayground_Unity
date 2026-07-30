using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class BehaviorTreeContext
    {
        private readonly Dictionary<System.Type, UnityEngine.Component> _componentCache = new();
        private readonly Dictionary<string, float> _actionCooldownReadyTimes = new();

        public BehaviorTreeContext(GameObject owner, Blackboard blackboard, BehaviorTreeRunner runner = null)
        {
            Owner = owner;
            Transform = owner != null ? owner.transform : null;
            Blackboard = blackboard;
            Runner = runner;
        }

        public GameObject Owner { get; }
        public Transform Transform { get; }
        public Blackboard Blackboard { get; }
        public BehaviorTreeRunner Runner { get; }
        public BehaviorTreeDebugTrace DebugTrace => Runner != null && Runner.DebugMode ? Runner.DebugTrace : null;
        public string ResolverFailureReason { get; private set; } = string.Empty;

        public void SetResolverFailure(string reason)
        {
            ResolverFailureReason = reason ?? string.Empty;
        }

        public bool IsActionCooldownReady(string cooldownId, float currentTime)
        {
            return string.IsNullOrWhiteSpace(cooldownId)
                   || !_actionCooldownReadyTimes.TryGetValue(cooldownId, out float readyTime)
                   || currentTime >= readyTime;
        }

        public void RecordActionCooldown(
            string cooldownId,
            float readyTime)
        {
            if (string.IsNullOrWhiteSpace(cooldownId))
                return;

            _actionCooldownReadyTimes[cooldownId] = readyTime;
        }

        public void RequestPause(BTNode node)
        {
            Runner?.RequestPauseFromNode(node);
        }

        public T GetComponentCached<T>() where T : UnityEngine.Component
        {
            if (Owner == null)
                return null;

            var type = typeof(T);
            if (_componentCache.TryGetValue(type, out var cached))
                return cached as T;

            var component = Owner.GetComponent<T>();
            _componentCache[type] = component;
            return component;
        }
    }
}
