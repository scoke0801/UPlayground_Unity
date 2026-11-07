using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 다른 BehaviorTreeAsset을 자식처럼 실행하는 Action 노드.
    /// 보스 페이즈, 그룹 행동 패턴 등 재사용 가능한 BT를 분리해 관리하기 위함.
    /// 부모 트리의 Blackboard를 공유하므로 키 이름 약속이 필요하다.
    /// 순환 참조는 Asset Validator에서 별도 검사한다.
    /// </summary>
    public class SubtreeNode : BTActionNode
    {
        [SerializeField] private BehaviorTreeAsset _subtreeAsset;

        private BehaviorTreeAsset _runtimeSubtree;
        private BTNode _runtimeSubRoot;

        public BehaviorTreeAsset SubtreeAsset
        {
            get => _subtreeAsset;
            set => _subtreeAsset = value;
        }

        protected override void OnInitialize()
        {
            DisposeRuntimeSubtree();

            if (_subtreeAsset == null || _subtreeAsset.RootNode == null || Context == null)
                return;

            _runtimeSubtree = _subtreeAsset.CloneRuntime(Context.Blackboard, shareBlackboardOverride: true);
            _runtimeSubRoot = _runtimeSubtree?.RootNode;
            _runtimeSubRoot?.Initialize(Context);
        }

        protected override BTStatus OnUpdate()
        {
            if (_runtimeSubRoot == null)
                return BTStatus.Failure;

            return _runtimeSubRoot.Tick();
        }

        protected override void OnAbort()
        {
            _runtimeSubRoot?.Abort();
        }

        protected override void OnReset()
        {
            _runtimeSubRoot?.ResetNode();
        }

        private void OnDestroy()
        {
            DisposeRuntimeSubtree();
        }

        private void DisposeRuntimeSubtree()
        {
            if (_runtimeSubtree == null)
                return;

            BehaviorTreeAsset.DisposeRuntime(_runtimeSubtree);
            _runtimeSubtree = null;
            _runtimeSubRoot = null;
        }
    }
}
