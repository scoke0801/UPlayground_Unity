using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>프로젝트의 FlowGraph/노드/변수/SubGraph 참조를 평면 검색하는 Explorer.</summary>
    public sealed class FlowGraphExplorerWindow : EditorWindow
    {
        private enum ItemKind
        {
            Graph,
            Node,
            Variable,
            Problem,
        }

        private sealed class ExplorerItem
        {
            public ItemKind Kind;
            public FlowGraphSO Graph;
            public string NodeId;
            public string Title;
            public string Path;
            public string Detail;
            public string SearchText;
        }

        private readonly List<ExplorerItem> _allItems = new();
        private readonly List<ExplorerItem> _visibleItems = new();
        private ToolbarSearchField _searchField;
        private ListView _list;
        private Label _status;

        [MenuItem("UPlayGround/Flow Graph Explorer")]
        public static void Open()
        {
            GetWindow<FlowGraphExplorerWindow>("FlowGraph Explorer").Show();
        }

        private void CreateGUI()
        {
            var toolbar = new Toolbar();
            _searchField = new ToolbarSearchField { style = { flexGrow = 1 } };
            _searchField.RegisterValueChangedCallback(_ => ApplyFilter());
            toolbar.Add(_searchField);
            toolbar.Add(new ToolbarButton(RebuildIndex) { text = "새로고침" });
            rootVisualElement.Add(toolbar);

            _status = new Label
            {
                style =
                {
                    paddingLeft = 6,
                    paddingTop = 3,
                    paddingBottom = 3,
                    color = new Color(0.7f, 0.7f, 0.7f),
                },
            };
            rootVisualElement.Add(_status);

            _list = new ListView(_visibleItems, 42, MakeRow, BindRow)
            {
                selectionType = SelectionType.Single,
                style = { flexGrow = 1 },
            };
            _list.itemsChosen += OnItemsChosen;
            rootVisualElement.Add(_list);
            RebuildIndex();
        }

        private static VisualElement MakeRow()
        {
            var root = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    paddingLeft = 6,
                    paddingTop = 3,
                    paddingBottom = 3,
                },
            };
            root.Add(new Label { name = "title", style = { unityFontStyleAndWeight = FontStyle.Bold } });
            root.Add(new Label
            {
                name = "detail",
                style = { fontSize = 10, color = new Color(0.65f, 0.65f, 0.65f) },
            });
            return root;
        }

        private void BindRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _visibleItems.Count)
                return;

            ExplorerItem item = _visibleItems[index];
            string prefix = item.Kind switch
            {
                ItemKind.Graph => "GRAPH",
                ItemKind.Node => "NODE",
                ItemKind.Variable => "VAR",
                ItemKind.Problem => "ERROR",
                _ => string.Empty,
            };
            row.Q<Label>("title").text = $"[{prefix}] {item.Title}";
            row.Q<Label>("detail").text = string.IsNullOrEmpty(item.Detail)
                ? item.Path
                : $"{item.Path}  ·  {item.Detail}";
        }

        private void OnItemsChosen(IEnumerable<object> chosen)
        {
            ExplorerItem item = chosen.OfType<ExplorerItem>().FirstOrDefault();
            if (item?.Graph != null)
                FlowGraphEditorWindow.OpenGraph(item.Graph, item.NodeId);
        }

        private void RebuildIndex()
        {
            _allItems.Clear();
            string[] guids = AssetDatabase.FindAssets("t:FlowGraphSO");
            var graphs = new List<(FlowGraphSO graph, string path)>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                FlowGraphSO graph = AssetDatabase.LoadAssetAtPath<FlowGraphSO>(path);
                if (graph != null)
                    graphs.Add((graph, path));
            }

            foreach ((FlowGraphSO graph, string path) in graphs.OrderBy(item => item.path))
                AddGraphItems(graph, path);

            foreach (IGrouping<string, (FlowGraphSO graph, string path)> duplicate
                     in graphs.GroupBy(item => item.graph.ResolvedGraphId)
                         .Where(group => group.Count() > 1))
            {
                foreach ((FlowGraphSO graph, string path) in duplicate)
                {
                    AddItem(
                        ItemKind.Problem,
                        graph,
                        null,
                        $"graphId 중복: {duplicate.Key}",
                        path,
                        $"{duplicate.Count()}개 에셋이 같은 ID를 사용");
                }
            }

            ApplyFilter();
        }

        private void AddGraphItems(FlowGraphSO graph, string path)
        {
            AddItem(
                ItemKind.Graph,
                graph,
                null,
                graph.ResolvedGraphId,
                path,
                $"Nodes {graph.nodes.Count} · Connections {graph.connections.Count}");

            foreach (FlowNode node in graph.nodes)
            {
                if (node == null)
                    continue;

                string detail = FlowNodeCatalog.GetMenuPath(node.GetType());
                if (!string.IsNullOrEmpty(node.editorComment))
                    detail += $" · {node.editorComment}";
                if (node is SubGraphNode sub && sub.subGraph != null)
                {
                    string subPath = AssetDatabase.GetAssetPath(sub.subGraph);
                    detail += $" · SubGraph → {sub.subGraph.ResolvedGraphId} ({subPath})";
                }

                AddItem(
                    ItemKind.Node,
                    graph,
                    node.id,
                    node.DisplayName,
                    path,
                    detail,
                    JsonUtility.ToJson(node),
                    FlowNodeCatalog.GetSummary(node.GetType()),
                    string.Join(" ", FlowNodeCatalog.GetKeywords(node.GetType())));
            }

            foreach (FlowVariableDef variable in graph.variables)
            {
                if (variable == null)
                    continue;
                AddItem(
                    ItemKind.Variable,
                    graph,
                    null,
                    variable.name,
                    path,
                    $"{variable.type} · 기본값 {variable.GetDefaultValue()}");
            }
        }

        private void AddItem(
            ItemKind kind,
            FlowGraphSO graph,
            string nodeId,
            string title,
            string path,
            string detail,
            params string[] extraSearchText)
        {
            string searchText = string.Join(
                "\n",
                new[] { title, path, detail, graph?.ResolvedGraphId }
                    .Concat(extraSearchText ?? Array.Empty<string>())
                    .Where(value => !string.IsNullOrEmpty(value)));
            _allItems.Add(new ExplorerItem
            {
                Kind = kind,
                Graph = graph,
                NodeId = nodeId,
                Title = title,
                Path = path,
                Detail = detail,
                SearchText = searchText,
            });
        }

        private void ApplyFilter()
        {
            if (_list == null)
                return;

            string[] terms = (_searchField?.value ?? string.Empty)
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            _visibleItems.Clear();
            foreach (ExplorerItem item in _allItems)
            {
                bool matches = true;
                foreach (string term in terms)
                {
                    if (item.SearchText.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches)
                    _visibleItems.Add(item);
            }

            _status.text = $"결과 {_visibleItems.Count} / 색인 {_allItems.Count}";
            _list.RefreshItems();
        }
    }
}
