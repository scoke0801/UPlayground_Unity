using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    [CreateAssetMenu(menuName = "BehaviorTree/BehaviorTree", fileName = "BehaviorTree")]
    public class BehaviorTreeSO : ScriptableObject
    {
        [Tooltip("트리의 루트 노드")]
        public BTNodeSO       rootNode;
        [Tooltip("연결된 블랙보드 키 정의")]
        public BTBlackboardSO blackboard;

        /// <summary>
        /// 적 한 마리당 독립된 런타임 트리를 생성한다.
        /// SourceSO 바인딩이 재귀적으로 모든 노드에 적용된다.
        /// </summary>
        public BTNode CreateRuntimeTree(RuntimeBlackboard bb)
        {
            if (rootNode == null)
            {
                Debug.LogError($"[BehaviorTreeSO] {name}: rootNode가 null입니다.");
                return new BTLeaf("Empty", _ => NodeStatus.Failure);
            }

            return rootNode.CreateAndBindNode(bb);
        }
    }
}
