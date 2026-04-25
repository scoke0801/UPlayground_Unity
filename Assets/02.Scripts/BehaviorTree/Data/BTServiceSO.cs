using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    /// <summary>
    /// Service ScriptableObject 기반 클래스.
    /// 적마다 독립된 BTServiceRuntime 인스턴스를 생성한다 (BTNodeSO → BTNode 패턴과 동일).
    /// </summary>
    public abstract class BTServiceSO : ScriptableObject
    {
        [Tooltip("에디터 표시용 이름")]
        public string serviceName = "Service";
        [Tooltip("OnTick 호출 최소 간격 (초)"), Min(0.05f)]
        public float  tickInterval = 0.1f;

        public abstract BTServiceRuntime CreateRuntime();
    }
}
