using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>FlowNode 파생 타입을 FlowNodeMenu 경로 기준으로 나열하는 노드 생성 검색창.</summary>
    public sealed class FlowNodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private FlowGraphView _graphView;
        private Texture2D _transparentIcon;

        /// <summary>아이콘 유무가 섞여도 들여쓰기가 흔들리지 않게 하는 투명 플레이스홀더.</summary>
        private Texture2D TransparentIcon
        {
            get
            {
                if (_transparentIcon == null)
                {
                    _transparentIcon = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    _transparentIcon.SetPixel(0, 0, Color.clear);
                    _transparentIcon.Apply();
                    _transparentIcon.hideFlags = HideFlags.HideAndDontSave;
                }
                return _transparentIcon;
            }
        }

        public void Initialize(FlowGraphView graphView)
        {
            _graphView = graphView;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var entries = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("노드 생성"), 0),
            };

            var byCategory = new SortedDictionary<string, List<(string label, Type type)>>();
            foreach (Type type in FlowNodeCatalog.GetNodeTypes())
            {
                if (!_graphView.CanCreateForPendingConnection(type))
                    continue;

                string category = FlowNodeCatalog.GetCategory(type);
                string label = FlowNodeCatalog.GetSearchLabel(type);

                if (!byCategory.TryGetValue(category, out var list))
                {
                    list = new List<(string, Type)>();
                    byCategory[category] = list;
                }
                list.Add((label, type));
            }

            foreach (var pair in byCategory)
            {
                entries.Add(new SearchTreeGroupEntry(new GUIContent(pair.Key), 1));
                foreach ((string label, Type type) in pair.Value.OrderBy(v => v.label))
                {
                    Texture2D icon = FlowNodeCatalog.GetIcon(type);
                    entries.Add(new SearchTreeEntry(new GUIContent(label, icon != null ? icon : TransparentIcon))
                    {
                        level = 2,
                        userData = type,
                    });
                }
            }

            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is not Type type)
                return false;
            FlowNodeUsageStore.RecordRecent(type);
            _graphView.CreateNodeAtScreenPosition(type, context.screenMousePosition);
            return true;
        }
    }
}
