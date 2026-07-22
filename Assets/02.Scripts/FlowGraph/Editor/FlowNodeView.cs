using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>FlowNode 하나를 그리는 노드 뷰. 포트는 노드의 Ports 선언에서 생성한다.</summary>
    public sealed class FlowNodeView : Node
    {
        private static readonly Color ActiveBorderColor = new(0.35f, 0.95f, 0.45f);
        private static readonly Color AfterglowColor = new(0.98f, 0.75f, 0.25f);
        private static readonly Color BreakpointColor = new(0.90f, 0.25f, 0.25f);
        private static readonly Color TruePortColor = new(0.40f, 0.90f, 0.45f);
        private static readonly Color FalsePortColor = new(0.95f, 0.40f, 0.40f);

        private readonly FlowGraphView _owner;
        private readonly Action<FlowNodeView> _onSelected;
        private readonly VisualElement _breakpointMarker;
        private readonly VisualElement _summaryContainer;
        private readonly Label _commentBubble;
        private readonly Label _validationBadge;
        private readonly VisualElement _waitProgressBar;
        private bool _debugActive;
        private float _afterglow;

        public FlowNodeView(FlowGraphView owner, FlowNode node, Action<FlowNodeView> onSelected)
        {
            _owner = owner;
            FlowNode = node;
            _onSelected = onSelected;
            title = MakeTitle(node);
            viewDataKey = node.id;

            foreach (FlowPortDef def in node.Ports)
            {
                Port port = FlowPortView.Create(
                    def.Direction == FlowPortDirection.Input ? Direction.Input : Direction.Output,
                    owner.ConnectorListener);
                port.portName = def.Name;

                // 분기 포트 색 구분 — 엣지가 포트 색을 상속해 와이어도 함께 구분된다
                if (def.Name == FlowPort.True)
                    port.portColor = TruePortColor;
                else if (def.Name == FlowPort.False)
                    port.portColor = FalsePortColor;

                if (def.Direction == FlowPortDirection.Input)
                    inputContainer.Add(port);
                else
                    outputContainer.Add(port);
            }

            // 시안: 카테고리별 헤더 컬러 (진입점=녹색, 코어=보라, 액션=파랑, 이벤트=주황)
            // 커스텀 노드는 [FlowNodeStyle]/[assembly: FlowNodeCategoryStyle]로 재정의 가능
            Color categoryColor = FlowNodeCatalog.GetCategoryColor(node.GetType());
            titleContainer.style.backgroundColor = new StyleColor(categoryColor);

            // 진입점 실루엣 차별화 — 색뿐 아니라 좌측 액센트 바로도 구분 (색약 접근성)
            if (node is EntryNode)
            {
                titleContainer.style.borderLeftWidth = 4;
                titleContainer.style.borderLeftColor = new Color(
                    Mathf.Min(1f, categoryColor.r + 0.35f),
                    Mathf.Min(1f, categoryColor.g + 0.35f),
                    Mathf.Min(1f, categoryColor.b + 0.35f));
            }

            // 타입별 아이콘 (FlowNodeCatalog: 노드 스타일 → 카테고리 기본 순)
            Texture2D icon = FlowNodeCatalog.GetIcon(node.GetType());
            if (icon != null)
            {
                titleContainer.Insert(0, new Image
                {
                    image = icon,
                    scaleMode = ScaleMode.ScaleToFit,
                    style =
                    {
                        width = 16,
                        height = 16,
                        marginLeft = 6,
                        flexShrink = 0,
                        alignSelf = Align.Center,
                    },
                });
            }

            // 본문 파라미터 요약 (BT Summary 이식) — 캔버스에서 설정을 바로 읽을 수 있게
            _summaryContainer = new VisualElement
            {
                style =
                {
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 3,
                    paddingBottom = 3,
                    backgroundColor = new Color(
                        categoryColor.r * 0.30f, categoryColor.g * 0.30f, categoryColor.b * 0.30f),
                },
            };
            extensionContainer.Add(_summaryContainer);
            RebuildSummary();

            // 서브그래프 더블클릭 진입
            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && FlowNode is SubGraphNode sub && sub.subGraph != null)
                {
                    _owner.RequestOpenSubGraph(sub.subGraph);
                    evt.StopPropagation();
                }
            });

            // 브레이크포인트 마커 (FlowCanvas 참조 — 타이틀 우측 빨간 점)
            _breakpointMarker = new VisualElement
            {
                style =
                {
                    width = 10,
                    height = 10,
                    borderTopLeftRadius = 5,
                    borderTopRightRadius = 5,
                    borderBottomLeftRadius = 5,
                    borderBottomRightRadius = 5,
                    marginRight = 6,
                    alignSelf = Align.Center,
                    backgroundColor = BreakpointColor,
                },
            };
            titleContainer.Add(_breakpointMarker);
            RefreshBreakpointMarker();

            // 저작 메모 말풍선 (Blueprint comment bubble) — 노드 위 절대 배치
            _commentBubble = new Label
            {
                style =
                {
                    position = Position.Absolute,
                    bottom = Length.Percent(100),
                    left = 0,
                    marginBottom = 4,
                    maxWidth = 240,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 4,
                    paddingBottom = 4,
                    whiteSpace = WhiteSpace.Normal,
                    fontSize = 10,
                    color = new Color(0.92f, 0.90f, 0.75f),
                    backgroundColor = new Color(0.16f, 0.15f, 0.10f, 0.92f),
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                },
                pickingMode = PickingMode.Ignore,
            };
            Add(_commentBubble);

            // 검증 배지 — 우상단 (검증 패널 안 봐도 캔버스에서 문제 노드가 보이게)
            _validationBadge = new Label
            {
                style =
                {
                    position = Position.Absolute,
                    top = -8,
                    right = -8,
                    width = 18,
                    height = 18,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.white,
                    borderTopLeftRadius = 9,
                    borderTopRightRadius = 9,
                    borderBottomLeftRadius = 9,
                    borderBottomRightRadius = 9,
                    display = DisplayStyle.None,
                },
                pickingMode = PickingMode.Ignore,
            };
            Add(_validationBadge);

            // Wait 진행 바 — 노드 하단 3px
            _waitProgressBar = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    bottom = 0,
                    left = 0,
                    height = 3,
                    width = Length.Percent(0),
                    backgroundColor = new Color(0.35f, 0.80f, 0.95f),
                    display = DisplayStyle.None,
                },
                pickingMode = PickingMode.Ignore,
            };
            Add(_waitProgressBar);

            RefreshCommentBubble();

            SetPosition(new Rect(node.editorPosition, Vector2.zero));
            RefreshExpandedState();
            RefreshPorts();
        }

        public FlowNode FlowNode { get; }

        public Port FindPort(Direction direction, string portName)
        {
            VisualElement container = direction == Direction.Input ? inputContainer : outputContainer;
            foreach (VisualElement child in container.Children())
            {
                if (child is Port port && port.portName == portName)
                    return port;
            }
            return null;
        }

        /// <summary>포트 드래그 자동 연결용 — 해당 방향의 첫 포트.</summary>
        public Port FirstPort(Direction direction)
        {
            VisualElement container = direction == Direction.Input ? inputContainer : outputContainer;
            foreach (VisualElement child in container.Children())
            {
                if (child is Port port)
                    return port;
            }
            return null;
        }

        public void RefreshTitle() => title = MakeTitle(FlowNode);

        // ──────────────────────────────────────────────────────────
        #region 파라미터 요약 (BT GetNodeSummaryRows 이식)

        private const int MaxSummaryRows = 6;

        public void RebuildSummary()
        {
            _summaryContainer.Clear();

            int count = 0;
            foreach ((string key, string value) in GetSummaryRows(FlowNode))
            {
                if (++count > MaxSummaryRows)
                {
                    _summaryContainer.Add(new Label("…") { style = { opacity = 0.5f, fontSize = 10 } });
                    break;
                }

                var row = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween },
                };
                row.Add(new Label(key)
                {
                    style = { opacity = 0.55f, fontSize = 10, marginRight = 8 },
                });
                row.Add(new Label(value)
                {
                    style = { fontSize = 10, unityTextAlign = TextAnchor.MiddleRight },
                });
                _summaryContainer.Add(row);
            }

            RefreshCommentBubble(); // 인스펙터 편집 경로에서 메모도 함께 갱신
            RefreshSummaryVisibility(count);
        }

        private static IEnumerable<(string Key, string Value)> GetSummaryRows(FlowNode node)
        {
            Type type = node.GetType();
            while (type != null && type != typeof(FlowNode))
            {
                foreach (FieldInfo field in type.GetFields(
                             BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (field.IsNotSerialized)
                        continue;
                    if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                        continue;
                    if (TryFormatField(field, node, out string key, out string value))
                        yield return (key, value);
                }
                type = type.BaseType;
            }
        }

        private static bool TryFormatField(FieldInfo field, FlowNode node, out string key, out string value)
        {
            key = field.Name.TrimStart('_');
            value = string.Empty;

            Type fieldType = field.FieldType;
            object rawValue = field.GetValue(node);

            if (fieldType == typeof(bool))
                value = (bool)rawValue ? "true" : "false";
            else if (fieldType == typeof(int))
                value = rawValue.ToString();
            else if (fieldType == typeof(float))
                value = $"{(float)rawValue:0.###}";
            else if (fieldType == typeof(string))
                value = string.IsNullOrWhiteSpace(rawValue as string) ? "<empty>" : (string)rawValue;
            else if (fieldType.IsEnum)
                value = rawValue.ToString();
            else if (fieldType == typeof(FlowVariableValue))
                value = rawValue?.ToString() ?? "null";
            else if (fieldType == typeof(GameEventRef))
                value = rawValue?.ToString() ?? "null";
            else if (typeof(FlowCondition).IsAssignableFrom(fieldType))
                value = rawValue == null ? "<조건 없음>" : rawValue.GetType().Name;
            else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                value = rawValue is UnityEngine.Object obj && obj != null ? obj.name : "null";
            else if (typeof(IList).IsAssignableFrom(fieldType) && rawValue is IList list)
                value = $"count:{list.Count}";
            else
                return false;

            if (value.Length > 24)
                value = value.Substring(0, 23) + "…";
            return true;
        }

        #endregion

        /// <summary>사용자 라벨(editorLabel) 우선, 시작(진입점) 노드는 ▶ 표식으로 구분한다.</summary>
        private static string MakeTitle(FlowNode node)
        {
            string name = string.IsNullOrWhiteSpace(node.editorLabel)
                ? node.DisplayName
                : node.editorLabel;
            return node is EntryNode ? $"▶ {name}" : name;
        }

        /// <summary>editorComment가 있으면 노드 위 말풍선으로 표시한다.</summary>
        public void RefreshCommentBubble()
        {
            if (_commentBubble == null)
                return; // 생성자 초기 RebuildSummary 시점 가드
            bool hasComment = !string.IsNullOrWhiteSpace(FlowNode.editorComment);
            _commentBubble.text = hasComment ? FlowNode.editorComment : string.Empty;
            _commentBubble.style.display = hasComment ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>검증 이슈 배지. null이면 숨김, Error=✕(빨강), Warning=!(노랑).</summary>
        public void SetValidationBadge(FlowIssueSeverity? severity)
        {
            if (severity == null)
            {
                _validationBadge.style.display = DisplayStyle.None;
                return;
            }

            _validationBadge.style.display = DisplayStyle.Flex;
            if (severity == FlowIssueSeverity.Error)
            {
                _validationBadge.text = "✕";
                _validationBadge.style.backgroundColor = new Color(0.80f, 0.22f, 0.22f);
            }
            else
            {
                _validationBadge.text = "!";
                _validationBadge.style.backgroundColor = new Color(0.85f, 0.62f, 0.10f);
            }
        }

        private bool _compact;

        /// <summary>컴팩트 모드 — 본문 요약을 숨기고 타이틀만 남긴다 (FlowCanvas compact 참조).</summary>
        public void SetCompact(bool compact)
        {
            _compact = compact;
            RefreshSummaryVisibility(_summaryContainer.childCount);
        }

        private void RefreshSummaryVisibility(int rowCount)
        {
            _summaryContainer.style.display =
                _compact || rowCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            RefreshExpandedState();
        }

        /// <summary>Wait 진행 바 (0~1). 음수면 숨김.</summary>
        public void SetWaitProgress(float progress01)
        {
            if (progress01 < 0f)
            {
                _waitProgressBar.style.display = DisplayStyle.None;
                return;
            }

            _waitProgressBar.style.display = DisplayStyle.Flex;
            _waitProgressBar.style.width = Length.Percent(Mathf.Clamp01(progress01) * 100f);
        }

        public void RefreshBreakpointMarker()
        {
            _breakpointMarker.style.display =
                FlowNode.breakpoint ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction(
                FlowNode.breakpoint ? "브레이크포인트 해제" : "브레이크포인트 설정",
                _ => _owner.ToggleBreakpoint(this));
            evt.menu.AppendSeparator();
            base.BuildContextualMenu(evt);
        }

        /// <summary>런타임 활성 토큰 하이라이트. 증분 diff 대상만 호출된다 — 매 갱신 전체 재스타일 금지.</summary>
        public void SetDebugActive(bool active)
        {
            if (_debugActive == active)
                return;
            _debugActive = active;
            _afterglow = -1f; // 활성 해제 시 잔광이 즉시 재적용되도록 강제

            SetBorder(ActiveBorderColor, active ? 2f : 0f);
        }

        /// <summary>
        /// "최근 실행" 잔광 (0~1, 페이드아웃). 순간 통과 노드도 실행 경로가 보이게 한다.
        /// 활성 보더가 켜져 있으면 무시된다.
        /// </summary>
        public void SetAfterglow(float intensity01)
        {
            if (_debugActive)
                return;
            if (Mathf.Abs(intensity01 - _afterglow) < 0.04f)
                return;
            _afterglow = intensity01;

            if (intensity01 <= 0f)
            {
                SetBorder(AfterglowColor, 0f);
                return;
            }

            Color color = AfterglowColor;
            color.a = Mathf.Clamp01(intensity01);
            SetBorder(color, 2f);
        }

        private void SetBorder(Color color, float width)
        {
            style.borderTopColor = style.borderBottomColor = color;
            style.borderLeftColor = style.borderRightColor = color;
            style.borderTopWidth = style.borderBottomWidth = width;
            style.borderLeftWidth = style.borderRightWidth = width;
        }

        public override void OnSelected()
        {
            base.OnSelected();
            _onSelected?.Invoke(this);
        }
    }
}
