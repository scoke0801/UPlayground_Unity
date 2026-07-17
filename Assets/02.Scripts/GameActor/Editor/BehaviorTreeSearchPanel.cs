#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    /// <summary>
    /// 그래프가 커진 뒤 노드/Blackboard Key를 찾기 위한 검색 패널.
    /// DisplayName, Comment, BlackboardKeySelector, 레거시 string Key를 한 번에 훑는다.
    /// </summary>
    public sealed class BehaviorTreeSearchPanel : VisualElement
    {
        private readonly Action<BTNode> _onNodeSelected;
        private readonly TextField _queryField;
        private readonly VisualElement _resultsContainer;
        private readonly Label _summaryLabel;
        private BehaviorTreeAsset _tree;
        private string _lastQuery = string.Empty;

        public BehaviorTreeSearchPanel(Action<BTNode> onNodeSelected)
        {
            _onNodeSelected = onNodeSelected;
            style.flexGrow = 1;

            _queryField = new TextField("검색")
            {
                value = string.Empty
            };
            _queryField.style.marginLeft = 6f;
            _queryField.style.marginRight = 6f;
            _queryField.style.marginTop = 6f;
            _queryField.RegisterValueChangedCallback(evt => Refresh(evt.newValue));
            Add(_queryField);

            _summaryLabel = new Label("검색어를 입력하세요. DisplayName, Comment, Blackboard Key를 검색합니다.");
            _summaryLabel.style.color = new Color(0.62f, 0.62f, 0.72f);
            _summaryLabel.style.marginLeft = 8f;
            _summaryLabel.style.marginRight = 8f;
            _summaryLabel.style.marginTop = 4f;
            _summaryLabel.style.marginBottom = 6f;
            _summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            Add(_summaryLabel);

            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;
            _resultsContainer = new VisualElement();
            _resultsContainer.style.flexGrow = 1;
            scroll.Add(_resultsContainer);
            Add(scroll);
        }

        public void Bind(BehaviorTreeAsset tree)
        {
            _tree = tree;
            Refresh(_queryField.value);
        }

        public void Refresh()
        {
            Refresh(_queryField.value);
        }

        private void Refresh(string rawQuery)
        {
            _lastQuery = rawQuery ?? string.Empty;
            _resultsContainer.Clear();

            if (_tree == null)
            {
                _summaryLabel.text = "BT Asset을 선택하세요.";
                return;
            }

            var query = _lastQuery.Trim();
            if (string.IsNullOrEmpty(query))
            {
                _summaryLabel.text = $"노드 {_tree.Nodes.Count}개, Blackboard Entry {_tree.Blackboard?.Entries.Count ?? 0}개. 검색어를 입력하세요.";
                return;
            }

            var results = Collect(query);
            _summaryLabel.text = $"'{query}' 검색 결과 {results.Count}건";

            foreach (var result in results)
                _resultsContainer.Add(BuildResultRow(result));
        }

        private List<SearchHit> Collect(string query)
        {
            var hits = new List<SearchHit>();
            var blackboard = _tree.Blackboard;
            if (blackboard != null)
            {
                foreach (var entry in blackboard.Entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                        continue;

                    if (entry.Key.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var referenceCount = BehaviorTreeBlackboardKeyRenamer.CountReferences(_tree, entry.Key);
                        hits.Add(new SearchHit
                        {
                            Kind = HitKind.BlackboardKey,
                            Node = FindFirstNodeReferencingKey(entry.Key),
                            DisplayTitle = $"[Blackboard] {entry.Key}",
                            DisplayDetail = $"Type: {entry.ValueType}, 참조 {referenceCount}개" +
                                            (referenceCount > 0 ? ", 클릭 시 첫 참조 노드로 이동" : string.Empty),
                        });
                    }
                }
            }

            foreach (var node in _tree.Nodes)
            {
                if (node == null)
                    continue;

                if (Matches(node.DisplayName, query))
                {
                    hits.Add(new SearchHit
                    {
                        Kind = HitKind.NodeName,
                        Node = node,
                        DisplayTitle = $"[Node] {node.DisplayName}",
                        DisplayDetail = node.GetType().Name,
                    });
                }
                else if (Matches(node.Comment, query))
                {
                    hits.Add(new SearchHit
                    {
                        Kind = HitKind.NodeComment,
                        Node = node,
                        DisplayTitle = $"[Comment] {node.DisplayName}",
                        DisplayDetail = node.Comment,
                    });
                }

                foreach (var field in GetSerializableFields(node.GetType()))
                {
                    if (field.FieldType == typeof(BlackboardKeySelector))
                    {
                        var selector = (BlackboardKeySelector)field.GetValue(node);
                        if (selector.HasKey && Matches(selector.Key, query))
                        {
                            hits.Add(new SearchHit
                            {
                                Kind = HitKind.KeyReference,
                                Node = node,
                                DisplayTitle = $"[Key] {node.DisplayName}.{field.Name}",
                                DisplayDetail = $"{selector.Key} ({selector.ExpectedType})",
                            });
                        }
                    }
                    else if (field.FieldType == typeof(string) && IsBlackboardKeyField(field))
                    {
                        var key = field.GetValue(node) as string;
                        if (Matches(key, query))
                        {
                            hits.Add(new SearchHit
                            {
                                Kind = HitKind.KeyReference,
                                Node = node,
                                DisplayTitle = $"[Key·legacy] {node.DisplayName}.{field.Name}",
                                DisplayDetail = key,
                            });
                        }
                    }
                }
            }

            return hits;
        }

        private VisualElement BuildResultRow(SearchHit hit)
        {
            var row = new VisualElement();
            row.style.marginLeft = 6f;
            row.style.marginRight = 6f;
            row.style.marginBottom = 3f;
            row.style.paddingLeft = 6f;
            row.style.paddingRight = 6f;
            row.style.paddingTop = 4f;
            row.style.paddingBottom = 4f;
            row.style.backgroundColor = new Color(0.09f, 0.09f, 0.11f);
            row.style.borderTopLeftRadius = 5f;
            row.style.borderTopRightRadius = 5f;
            row.style.borderBottomLeftRadius = 5f;
            row.style.borderBottomRightRadius = 5f;

            var title = new Label(hit.DisplayTitle);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = hit.Kind switch
            {
                HitKind.BlackboardKey => new Color(0.95f, 0.72f, 0.24f),
                HitKind.KeyReference => new Color(0.85f, 0.62f, 0.95f),
                HitKind.NodeComment => new Color(0.62f, 0.86f, 0.96f),
                _ => new Color(0.84f, 0.92f, 0.84f),
            };
            row.Add(title);

            if (!string.IsNullOrWhiteSpace(hit.DisplayDetail))
            {
                var detail = new Label(hit.DisplayDetail);
                detail.style.color = new Color(0.66f, 0.66f, 0.72f);
                detail.style.fontSize = 10f;
                detail.style.whiteSpace = WhiteSpace.Normal;
                row.Add(detail);
            }

            if (hit.Node != null)
            {
                row.tooltip = "클릭하면 해당 노드로 이동합니다.";
                row.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button != 0)
                        return;
                    _onNodeSelected?.Invoke(hit.Node);
                    evt.StopPropagation();
                });
            }

            return row;
        }

        private static bool Matches(string source, string query)
        {
            return !string.IsNullOrEmpty(source) && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsBlackboardKeyField(FieldInfo field)
        {
            return string.Equals(field.Name, "_key", StringComparison.Ordinal) ||
                   string.Equals(field.Name, "key", StringComparison.Ordinal) ||
                   field.Name.EndsWith("Key", StringComparison.Ordinal) ||
                   field.Name.EndsWith("_key", StringComparison.Ordinal);
        }

        private BTNode FindFirstNodeReferencingKey(string key)
        {
            if (_tree == null || string.IsNullOrWhiteSpace(key))
                return null;

            foreach (var node in _tree.Nodes)
            {
                if (node == null)
                    continue;

                foreach (var field in GetSerializableFields(node.GetType()))
                {
                    if (field.FieldType == typeof(BlackboardKeySelector))
                    {
                        var selector = (BlackboardKeySelector)field.GetValue(node);
                        if (selector.HasKey && string.Equals(selector.Key, key, StringComparison.Ordinal))
                            return node;
                    }
                    else if (field.FieldType == typeof(string) && IsBlackboardKeyField(field))
                    {
                        var legacyKey = field.GetValue(node) as string;
                        if (string.Equals(legacyKey, key, StringComparison.Ordinal))
                            return node;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<FieldInfo> GetSerializableFields(Type type)
        {
            while (type != null && type != typeof(BTNode))
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (field.IsNotSerialized)
                        continue;

                    if (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null)
                        yield return field;
                }

                type = type.BaseType;
            }
        }

        private enum HitKind
        {
            NodeName,
            NodeComment,
            KeyReference,
            BlackboardKey,
        }

        private struct SearchHit
        {
            public HitKind Kind;
            public BTNode Node;
            public string DisplayTitle;
            public string DisplayDetail;
        }
    }
}
#endif
