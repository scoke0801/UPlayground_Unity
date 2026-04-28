using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    public class BehaviorTreeContext
    {
        private readonly Dictionary<System.Type, UnityEngine.Component> _componentCache = new();

        public BehaviorTreeContext(GameObject owner, Blackboard blackboard)
        {
            Owner = owner;
            Transform = owner != null ? owner.transform : null;
            Blackboard = blackboard;
        }

        public GameObject Owner { get; }
        public Transform Transform { get; }
        public Blackboard Blackboard { get; }

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
