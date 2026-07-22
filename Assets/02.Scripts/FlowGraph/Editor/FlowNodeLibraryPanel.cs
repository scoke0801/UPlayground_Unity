using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>
    /// 좌측 노드 라이브러리 패널 (시안: Node Library) — 검색 필터 + 카테고리별 노드 목록.
    /// 항목 클릭으로 캔버스 중앙에 노드를 생성한다. 우클릭 검색창과 병행 사용.
    /// </summary>
    public sealed class FlowNodeLibraryPanel : VisualElement
    {
        private readonly Action<Type> _onCreateNode;
        private readonly ScrollView _listView;
        private string _filter = string.Empty;

        public FlowNodeLibraryPanel(Action<Type> onCreateNode)
        {
            _onCreateNode = onCreateNode;

            style.width = 220;
            style.flexShrink = 0;
            style.borderRightWidth = 1;
            style.borderRightColor = new Color(0.1f, 0.1f, 0.1f);

            var header = new Label("Node Library")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingLeft = 8,
                    paddingTop = 6,
                    paddingBottom = 4,
                },
            };
            Add(header);

            var searchField = new TextField { style = { marginLeft = 4, marginRight = 4 } };
            searchField.textEdition.placeholder = "Search nodes...";
            searchField.RegisterValueChangedCallback(evt =>
            {
                _filter = evt.newValue ?? string.Empty;
                Rebuild();
            });
            Add(searchField);

            _listView = new ScrollView { style = { flexGrow = 1, marginTop = 4 } };
            Add(_listView);

            Rebuild();
        }

        private void Rebuild()
        {
            _listView.Clear();

            foreach (var pair in FlowNodeCatalog.GetNodeTypesByCategory())
            {
                var matching = new List<Type>();
                foreach (Type type in pair.Value)
                {
                    string label = FlowNodeCatalog.GetLabel(type);
                    if (string.IsNullOrEmpty(_filter)
                        || label.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0
                        || type.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matching.Add(type);
                    }
                }

                if (matching.Count == 0)
                    continue;

                var foldout = new Foldout { text = pair.Key, value = true };
                foldout.style.marginLeft = 2;
                foreach (Type type in matching)
                    foldout.Add(CreateItem(type));
                _listView.Add(foldout);
            }
        }

        private VisualElement CreateItem(Type nodeType)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = 2,
                    paddingBottom = 2,
                    paddingLeft = 4,
                },
            };

            // 타입 아이콘 우선, 없으면 카테고리 컬러 스와치
            Texture2D icon = FlowNodeCatalog.GetIcon(nodeType);
            if (icon != null)
            {
                row.Add(new Image
                {
                    image = icon,
                    scaleMode = ScaleMode.ScaleToFit,
                    style = { width = 14, height = 14, marginRight = 6, flexShrink = 0 },
                });
            }
            else
            {
                row.Add(new VisualElement
                {
                    style =
                    {
                        width = 8,
                        height = 8,
                        borderTopLeftRadius = 4,
                        borderTopRightRadius = 4,
                        borderBottomLeftRadius = 4,
                        borderBottomRightRadius = 4,
                        marginRight = 6,
                        backgroundColor = FlowNodeCatalog.GetCategoryColor(nodeType),
                    },
                });
            }
            row.Add(new Label(FlowNodeCatalog.GetLabel(nodeType)));

            row.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    _onCreateNode?.Invoke(nodeType);
                    evt.StopPropagation();
                }
            });
            row.RegisterCallback<MouseEnterEvent>(_ =>
                row.style.backgroundColor = new Color(1f, 1f, 1f, 0.06f));
            row.RegisterCallback<MouseLeaveEvent>(_ =>
                row.style.backgroundColor = Color.clear);

            return row;
        }
    }
}
