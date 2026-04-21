using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    public abstract class BTNodeSO : ScriptableObject
    {
        [Tooltip("에디터/디버그용 노드 이름")]
        public string nodeName = "Node";

        [HideInInspector] public Vector2 editorPosition;

        /// <summary>
        /// 런타임 노드 생성. 상태(쿨다운 등)는 인스턴스에 저장되므로 SO 공유에 안전하다.
        /// 직접 호출하지 말 것 — <see cref="CreateAndBindNode"/> 를 사용할 것.
        /// </summary>
        protected abstract BTNode CreateRuntimeNode(RuntimeBlackboard bb);

        /// <summary>
        /// 런타임 노드를 생성하고 SourceSO를 바인딩한다.
        /// 에디터 런타임 하이라이트가 이 바인딩을 통해 SO ↔ 런타임 노드를 매핑한다.
        /// </summary>
        public BTNode CreateAndBindNode(RuntimeBlackboard bb)
        {
            var node = CreateRuntimeNode(bb);
            node.SourceSO = this;
            return node;
        }
    }
}
