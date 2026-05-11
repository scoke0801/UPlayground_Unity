#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    public sealed class BehaviorTreeNodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private BehaviorTreeEditorWindow _window;
        private BehaviorTreeGraphView _graphView;
        private Action<Type, Vector2> _onCreateNode;

        public void Initialize(BehaviorTreeEditorWindow window, BehaviorTreeGraphView graphView, Action<Type, Vector2> onCreateNode)
        {
            _window = window;
            _graphView = graphView;
            _onCreateNode = onCreateNode;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Behavior Tree Node"))
            };

            foreach (var group in GetNodeTypes().GroupBy(GetCategory))
            {
                entries.Add(new SearchTreeGroupEntry(new GUIContent(group.Key), 1));
                foreach (var type in group)
                    entries.Add(new SearchTreeEntry(new GUIContent(GetDisplayName(type))) { level = 2, userData = type });
            }

            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (_window == null || _graphView == null || _onCreateNode == null || searchTreeEntry.userData is not Type type)
                return false;

            _onCreateNode.Invoke(type, context.screenMousePosition);
            return true;
        }

        private static IEnumerable<Type> GetNodeTypes()
        {
            return TypeCache.GetTypesDerivedFrom<BTNode>()
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .Where(type => !typeof(BTServiceNode).IsAssignableFrom(type))
                .OrderBy(GetCategory)
                .ThenBy(GetDisplayName);
        }

        private static string GetCategory(Type type)
        {
            if (typeof(BTCompositeNode).IsAssignableFrom(type))
                return "Composite";
            if (typeof(BTDecoratorNode).IsAssignableFrom(type))
                return "Decorator";
            if (typeof(BTConditionNode).IsAssignableFrom(type))
                return "Condition";
            return "Action";
        }

        private static string GetDisplayName(Type type)
        {
            var name = type.Name;
            return name.EndsWith("Node", StringComparison.Ordinal) ? name[..^4] : name;
        }
    }
}
#endif
