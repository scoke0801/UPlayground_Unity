using System;
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
        [SerializeField] private List<BehaviorTreeEditorGroup> _editorGroups = new();

        public BTNode RootNode
        {
            get => _rootNode;
            set => _rootNode = value;
        }

        public List<BTNode> Nodes => _nodes;
        public Blackboard Blackboard => _blackboard;
        public List<BehaviorTreeEditorGroup> EditorGroups => _editorGroups;

        public BehaviorTreeAsset CloneRuntime(Blackboard blackboardOverride = null, bool shareBlackboardOverride = false)
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
                if (clone is BTCompositeNode compositeClone)
                    compositeClone.Services.Clear();
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

                if (node is BTCompositeNode sourceComposite && clone is BTCompositeNode compositeClone)
                {
                    foreach (var service in sourceComposite.Services)
                    {
                        if (service != null && nodeMap.TryGetValue(service, out var serviceClone) && serviceClone is BTServiceNode serviceCloneTyped)
                            compositeClone.Services.Add(serviceCloneTyped);
                    }
                }
            }

            tree._rootNode = _rootNode != null && nodeMap.TryGetValue(_rootNode, out var rootClone)
                ? rootClone
                : null;
            if (blackboardOverride != null)
                tree._blackboard = shareBlackboardOverride ? blackboardOverride : blackboardOverride.Clone();
            else
                tree._blackboard = _blackboard?.Clone() ?? new Blackboard();
            return tree;
        }

        /// <summary>
        /// CloneRuntime으로 만들어진 런타임 인스턴스를 명시적으로 해제한다.
        /// 인스펙터/원본 에셋이 아닌 클론 트리에서만 호출해야 한다 (원본 에셋에 호출 시 데이터 소실 위험).
        /// </summary>
        public static void DisposeRuntime(BehaviorTreeAsset runtimeTree)
        {
            if (runtimeTree == null)
                return;

            foreach (var node in runtimeTree._nodes)
            {
                if (node == null)
                    continue;

                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(node);
                else
                    UnityEngine.Object.DestroyImmediate(node);
            }

            runtimeTree._nodes.Clear();
            runtimeTree._rootNode = null;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(runtimeTree);
            else
                UnityEngine.Object.DestroyImmediate(runtimeTree);
        }
    }

    [Serializable]
    public class BehaviorTreeEditorGroup
    {
        [SerializeField] private string _guid = System.Guid.NewGuid().ToString("N");
        [SerializeField] private string _title = "Group";
        [SerializeField] private Rect _rect = new(0f, 0f, 420f, 280f);
        [SerializeField] private Color _color = new(0.12f, 0.30f, 0.12f, 0.38f);

        public string Guid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_guid))
                    _guid = System.Guid.NewGuid().ToString("N");

                return _guid;
            }
            set => _guid = value;
        }

        public string Title
        {
            get => string.IsNullOrWhiteSpace(_title) ? "Group" : _title;
            set => _title = value;
        }

        public Rect Rect
        {
            get => _rect;
            set => _rect = value;
        }

        public Color Color
        {
            get => _color;
            set => _color = value;
        }
    }
}
