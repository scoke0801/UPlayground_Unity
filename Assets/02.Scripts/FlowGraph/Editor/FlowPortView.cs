using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>
    /// 커스텀 EdgeConnector 리스너를 붙일 수 있는 실행 포트.
    /// 기본 Port.Create는 드롭 아웃사이드 콜백을 노출하지 않아 서브클래스로 구성한다.
    /// </summary>
    public sealed class FlowPortView : Port
    {
        private FlowPortView(
            Orientation orientation,
            Direction direction,
            Capacity capacity,
            Type portType)
            : base(orientation, direction, capacity, portType)
        {
        }

        public FlowPortDef Definition { get; private set; }

        public static FlowPortView Create(
            FlowPortDef definition,
            IEdgeConnectorListener connectorListener)
        {
            Direction direction = definition.Direction == FlowPortDirection.Input
                ? Direction.Input
                : Direction.Output;
            Capacity capacity = definition.Capacity == FlowPortCapacity.Single
                ? Capacity.Single
                : Capacity.Multi;
            Type portType = definition.Kind == FlowPortKind.Data
                ? definition.ValueType ?? typeof(object)
                : typeof(FlowExecutionPort);

            var port = new FlowPortView(Orientation.Horizontal, direction, capacity, portType)
            {
                Definition = definition,
                m_EdgeConnector = new EdgeConnector<Edge>(connectorListener),
            };
            port.AddManipulator(port.m_EdgeConnector);
            port.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0 || !evt.altKey || !port.connected)
                    return;

                FlowGraphView graphView = port.GetFirstAncestorOfType<FlowGraphView>();
                graphView?.DeleteElements(port.connections.ToList());
                evt.StopImmediatePropagation();
            });
            port.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
            {
                if (!port.connected)
                    return;
                evt.menu.AppendAction(
                    "모든 연결 해제",
                    _ => port.GetFirstAncestorOfType<FlowGraphView>()
                        ?.DeleteElements(port.connections.ToList()));
                evt.menu.AppendSeparator();
            });
            return port;
        }

        /// <summary>GraphView의 타입 비교에서 실행 포트를 데이터 포트와 확실히 분리하는 마커.</summary>
        private sealed class FlowExecutionPort
        {
        }
    }

    /// <summary>
    /// 포트 드래그를 빈 캔버스에 드롭하면 노드 검색창을 열고, 생성된 노드에 자동 연결한다 (FlowCanvas 참조).
    /// 포트 위 드롭은 일반 연결로 처리한다.
    /// </summary>
    public sealed class FlowEdgeConnectorListener : IEdgeConnectorListener
    {
        private readonly FlowGraphView _view;

        public FlowEdgeConnectorListener(FlowGraphView view)
        {
            _view = view;
        }

        public void OnDrop(GraphView graphView, Edge edge)
        {
            _view.ConnectPorts(edge.output, edge.input);
        }

        public void OnDropOutsidePort(Edge edge, Vector2 position)
        {
            _view.OpenSearchForPendingConnection(edge, position);
        }
    }
}
