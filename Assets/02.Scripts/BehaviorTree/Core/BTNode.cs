namespace UPlayGround.BehaviorTree
{
    public enum NodeStatus { Success, Failure, Running }

    public abstract class BTNode
    {
        public string     NodeName   { get; protected set; }
        public NodeStatus LastStatus { get; private set; } = NodeStatus.Failure;

        /// <summary> 에디터 런타임 하이라이트용 — 이 노드를 생성한 SO 역참조 </summary>
        public BTNodeSO SourceSO { get; set; }

        /// <summary> 외부에서 호출하는 진입점. LastStatus를 갱신한 뒤 반환. </summary>
        public NodeStatus Tick(RuntimeBlackboard bb)
        {
            LastStatus = TickInternal(bb);
            return LastStatus;
        }

        /// <summary> 서브클래스에서 실행 로직을 구현 </summary>
        protected abstract NodeStatus TickInternal(RuntimeBlackboard bb);

        public virtual void OnEnter(RuntimeBlackboard bb) { }
        public virtual void OnExit(RuntimeBlackboard bb)  { }
    }
}
