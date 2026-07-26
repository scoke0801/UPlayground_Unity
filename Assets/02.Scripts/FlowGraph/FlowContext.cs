using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>
    /// 한 번의 진입점 발화(플로우 실행)를 대표하는 컨텍스트.
    /// TriggerContext 설계를 계승 — 공유 SO 에셋에 가변 상태를 두지 않기 위해 발화마다 새로 만든다.
    /// </summary>
    public sealed class FlowContext
    {
        private Dictionary<string, object> _blackboard;
        private Dictionary<string, object> _nodeStates;
        private HashSet<string> _dataEvaluationStack;
        private int _executionBudgetFrame = -1;
        private int _executionsThisFrame;

        public FlowContext(FlowGraphRunner runner, EntryNode entry)
        {
            Runner = runner;
            Entry = entry;
        }

        public FlowGraphRunner Runner { get; }
        public EntryNode Entry { get; }
        public FlowGraphSO Graph { get; internal set; }
        public long ContextId { get; internal set; }
        public long ParentContextId { get; internal set; }

        /// <summary>발화 원인(있다면). 트리거 볼륨 진입 등에서 채워진다.</summary>
        public Collider Collider { get; set; }
        public IWorldActor Actor { get; set; }

        /// <summary>씬 전환/러너 비활성화 시 true — 진행 중 토큰은 다음 재개 시점에 중단된다.</summary>
        public bool Cancelled { get; internal set; }

        /// <summary>SubGraph 중첩 깊이. 순환 호출 가드에 사용.</summary>
        public int Depth { get; internal set; }

        /// <summary>이 컨텍스트에서 실행 중인 토큰 수. 0이면 플로우 완료 — SubGraph 합류 대기에 사용.</summary>
        public int ActiveTokenCount { get; internal set; }

        /// <summary>
        /// 같은 프레임에 대기 없이 이어지는 노드 실행 수를 제한한다.
        /// 일반 엣지 사이클이 코루틴 재진입으로 스택을 소진하는 것을 막기 위한 컨텍스트별 예산이다.
        /// </summary>
        internal bool TryConsumeExecutionBudget(int maxExecutionsPerFrame)
        {
            int frame = Time.frameCount;
            if (_executionBudgetFrame != frame)
            {
                _executionBudgetFrame = frame;
                _executionsThisFrame = 0;
            }

            _executionsThisFrame++;
            return _executionsThisFrame <= maxExecutionsPerFrame;
        }

        /// <summary>
        /// 노드 상태를 생성 없이 조회한다 (에디터 디버그 뷰용 — GetNodeState와 달리 부수효과 없음).
        /// </summary>
        public bool TryPeekNodeState<T>(string nodeId, out T state) where T : class
        {
            if (_nodeStates != null
                && _nodeStates.TryGetValue(nodeId, out object raw)
                && raw is T typed)
            {
                state = typed;
                return true;
            }
            state = null;
            return false;
        }

        /// <summary>블랙보드 전체 열람 (에디터 디버그 뷰용).</summary>
        public IEnumerable<KeyValuePair<string, object>> BlackboardEntries
        {
            get
            {
                if (_blackboard == null)
                    yield break;
                foreach (KeyValuePair<string, object> pair in _blackboard)
                    yield return pair;
            }
        }

        /// <summary>플로우 스코프 공유 데이터 채널(경량 블랙보드).</summary>
        public void Set(string key, object value)
        {
            _blackboard ??= new Dictionary<string, object>();
            _blackboard[key] = value;
            Runner.RecordBlackboardChange(this, key, value);
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_blackboard != null && _blackboard.TryGetValue(key, out object raw) && raw is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// 노드별·컨텍스트별 실행 상태(예: Join 도착 카운트). 노드 인스턴스는 에셋 공유이므로
        /// 상태는 반드시 여기서 관리한다.
        /// </summary>
        public T GetNodeState<T>(FlowNode node) where T : class, new()
        {
            _nodeStates ??= new Dictionary<string, object>();
            if (!_nodeStates.TryGetValue(node.id, out object state) || state is not T typed)
            {
                typed = new T();
                _nodeStates[node.id] = typed;
            }
            return typed;
        }

        internal bool TryBeginDataEvaluation(FlowGraphSO graph, string nodeId, string portId)
        {
            _dataEvaluationStack ??= new HashSet<string>();
            return _dataEvaluationStack.Add(
                $"{graph.GetInstanceID()}\u001f{nodeId}\u001f{portId}");
        }

        internal void EndDataEvaluation(FlowGraphSO graph, string nodeId, string portId)
        {
            _dataEvaluationStack?.Remove(
                $"{graph.GetInstanceID()}\u001f{nodeId}\u001f{portId}");
        }
    }

    /// <summary>노드에 도착한 실행 토큰. Emit으로 출력 포트에 연결된 다음 노드들로 전파한다.</summary>
    public sealed class FlowToken
    {
        internal FlowToken(FlowContext context, FlowGraphSO graph, FlowNode node)
        {
            Context = context;
            Graph = graph;
            Node = node;
        }

        public FlowContext Context { get; }

        /// <summary>토큰이 흐르는 그래프. SubGraph 중첩 실행 시 러너의 루트 그래프와 다를 수 있다.</summary>
        public FlowGraphSO Graph { get; }

        public FlowNode Node { get; }

        public void Emit(string port)
        {
            if (Context.Cancelled)
                return;
            Context.Runner.EmitToken(Context, Graph, Node, port);
        }
    }
}
