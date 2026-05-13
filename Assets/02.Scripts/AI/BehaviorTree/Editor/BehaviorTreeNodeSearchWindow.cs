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
        public const string RecentNodesPrefKey = "UPlayGround.BT.RecentNodes";
        public const int RecentLimit = 6;

        private BehaviorTreeEditorWindow _window;
        private BehaviorTreeGraphView _graphView;
        private Action<Type, Vector2> _onCreateNode;
        private Action<Type, Vector2> _onCreateNodeFromPort;
        private Direction? _portDirectionFilter;

        public void Initialize(
            BehaviorTreeEditorWindow window,
            BehaviorTreeGraphView graphView,
            Action<Type, Vector2> onCreateNode,
            Action<Type, Vector2> onCreateNodeFromPort)
        {
            _window = window;
            _graphView = graphView;
            _onCreateNode = onCreateNode;
            _onCreateNodeFromPort = onCreateNodeFromPort;
        }

        public void SetPortFilter(Direction? direction)
        {
            _portDirectionFilter = direction;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Behavior Tree Node"))
            };

            var nodeTypes = GetNodeTypes(_portDirectionFilter).ToList();
            var typeByFullName = nodeTypes.ToDictionary(t => t.FullName ?? t.Name, t => t);

            var recents = LoadRecentNodes()
                .Where(name => typeByFullName.ContainsKey(name))
                .Select(name => typeByFullName[name])
                .Take(RecentLimit)
                .ToList();

            if (recents.Count > 0)
            {
                entries.Add(new SearchTreeGroupEntry(new GUIContent("Recent"), 1));
                foreach (var type in recents)
                    entries.Add(new SearchTreeEntry(new GUIContent($"★ {GetDisplayName(type)}")) { level = 2, userData = type });
            }

            foreach (var group in nodeTypes.GroupBy(GetCategory).OrderBy(g => g.Key))
            {
                entries.Add(new SearchTreeGroupEntry(new GUIContent(group.Key), 1));
                foreach (var type in group.OrderBy(GetDisplayName))
                    entries.Add(new SearchTreeEntry(new GUIContent(GetDisplayName(type))) { level = 2, userData = type });
            }

            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (_window == null || _graphView == null || searchTreeEntry.userData is not Type type)
                return false;

            RecordRecentNode(type);

            if (_portDirectionFilter.HasValue && _onCreateNodeFromPort != null)
            {
                _onCreateNodeFromPort.Invoke(type, context.screenMousePosition);
                _portDirectionFilter = null;
                return true;
            }

            _onCreateNode?.Invoke(type, context.screenMousePosition);
            return true;
        }

        public static void RecordRecentNode(Type type)
        {
            if (type == null)
                return;

            var name = type.FullName ?? type.Name;
            var current = LoadRecentNodes();
            current.RemoveAll(existing => string.Equals(existing, name, StringComparison.Ordinal));
            current.Insert(0, name);
            if (current.Count > RecentLimit)
                current.RemoveRange(RecentLimit, current.Count - RecentLimit);

            EditorPrefs.SetString(RecentNodesPrefKey, string.Join("|", current));
        }

        public static List<string> LoadRecentNodes()
        {
            var raw = EditorPrefs.GetString(RecentNodesPrefKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
                return new List<string>();

            return raw.Split('|').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        private static IEnumerable<Type> GetNodeTypes(Direction? portDirection)
        {
            var query = TypeCache.GetTypesDerivedFrom<BTNode>()
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .Where(type => !typeof(BTServiceNode).IsAssignableFrom(type));

            // 포트 드래그: Input 포트(자식 측)에 드롭하면 부모가 될 수 있는 타입만 노출.
            if (portDirection == Direction.Input)
                query = query.Where(type =>
                    typeof(BTCompositeNode).IsAssignableFrom(type) ||
                    typeof(BTDecoratorNode).IsAssignableFrom(type));

            return query.OrderBy(GetCategory).ThenBy(GetDisplayName);
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
