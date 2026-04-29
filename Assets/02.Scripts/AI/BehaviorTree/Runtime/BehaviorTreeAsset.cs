using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    [CreateAssetMenu(fileName = "BT_", menuName = "UPlayGround/AI/Behavior Tree")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        [SerializeField] private BTNode _rootNode;
        [SerializeField] private List<BTNode> _nodes = new();
        [SerializeField] private Blackboard _blackboard = new();

        public BTNode RootNode
        {
            get => _rootNode;
            set => _rootNode = value;
        }

        public List<BTNode> Nodes => _nodes;
        public Blackboard Blackboard => _blackboard;

        public BehaviorTreeAsset CloneRuntime(Blackboard blackboardOverride = null)
        {
            var tree = Instantiate(this);
            var nodeMap = new Dictionary<BTNode, BTNode>();

            tree._nodes = new List<BTNode>();
            foreach (var node in _nodes)
            {
                if (node == null)
                    continue;

                var clone = Instantiate(node);
                clone.name = node.name;
                clone.Children.Clear();
                nodeMap[node] = clone;
                tree._nodes.Add(clone);
            }

            foreach (var node in _nodes)
            {
                if (node == null || !nodeMap.TryGetValue(node, out var clone))
                    continue;

                foreach (var child in node.Children)
                {
                    if (child != null && nodeMap.TryGetValue(child, out var childClone))
                        clone.Children.Add(childClone);
                }
            }

            tree._rootNode = _rootNode != null && nodeMap.TryGetValue(_rootNode, out var rootClone)
                ? rootClone
                : null;
            tree._blackboard = blackboardOverride?.Clone() ?? _blackboard?.Clone() ?? new Blackboard();
            return tree;
        }
    }
}
