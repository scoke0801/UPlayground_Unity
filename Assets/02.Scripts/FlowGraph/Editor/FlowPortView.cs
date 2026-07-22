using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.FlowGraph.Editor
{
    /// <summary>
    /// 커스텀 EdgeConnector 리스너를 붙일 수 있는 실행 포트.
    /// 기본 Port.Create는 드롭 아웃사이드 콜백을 노출하지 않아 서브클래스로 구성한다.
    /// </summary>
    public sealed class FlowPortView : Port
    {
        private FlowPortView(Orientation orientation, Direction direction, Capacity capacity)
            : base(orientation, direction, capacity, typeof(bool))
        {
        }

        public static FlowPortView Create(
            Direction direction,
            IEdgeConnectorListener connectorListener)
        {
            var port = new FlowPortView(Orientation.Horizontal, direction, Capacity.Multi)
            {
                m_EdgeConnector = new EdgeConnector<Edge>(connectorListener),
            };
            port.AddManipulator(port.m_EdgeConnector);
            return port;
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
