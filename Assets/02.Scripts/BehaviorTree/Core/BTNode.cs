namespace UPlayGround.BehaviorTree
{
    public enum NodeStatus { Success, Failure, Running }

    public abstract class BTNode
    {
        public string     NodeName          { get; protected set; }
        public NodeStatus LastStatus        { get; private set; } = NodeStatus.Failure;
        public bool       BreakpointEnabled { get; set; }

        /// <summary> 에디터 런타임 하이라이트용 — 이 노드를 생성한 SO 역참조 </summary>
        public BTNodeSO SourceSO { get; set; }

        /// <summary>
        /// 외부에서 호출하는 진입점.
        /// Running이 아닌 상태에서 새로 진입할 때 OnEnter, Running이 끝날 때 OnExit을 자동 호출한다.
        /// </summary>
        public NodeStatus Tick(RuntimeBlackboard bb)
        {
#if UNITY_EDITOR
            if (BreakpointEnabled && UnityEngine.Application.isPlaying)
            {
                UnityEngine.Debug.Log($"[BT Breakpoint] {NodeName}  (이전 상태: {LastStatus})");
                UnityEngine.Debug.Break();
            }
#endif
            if (LastStatus != NodeStatus.Running)
                OnEnter(bb);

            LastStatus = TickInternal(bb);

            if (LastStatus != NodeStatus.Running)
                OnExit(bb);

            return LastStatus;
        }

        /// <summary> 서브클래스에서 실행 로직을 구현 </summary>
        protected abstract NodeStatus TickInternal(RuntimeBlackboard bb);

        public virtual void OnEnter(RuntimeBlackboard bb) { }
        public virtual void OnExit(RuntimeBlackboard bb)  { }
    }
}
