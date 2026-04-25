using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    /// <summary>
    /// HasTarget / DistanceToTarget BB 키를 EnemyDetection에서 갱신한다.
    /// 루트 컴포짓에 붙이면 BTRunner.MakeDecision()에서 해당 키 갱신을 제거할 수 있다.
    /// </summary>
    [CreateAssetMenu(menuName = "BehaviorTree/Service/UpdatePerception", fileName = "BTService_UpdatePerception")]
    public class BTService_UpdatePerceptionSO : BTServiceSO
    {
        public override BTServiceRuntime CreateRuntime()
            => new UpdatePerceptionRuntime(serviceName, tickInterval);

        private class UpdatePerceptionRuntime : BTServiceRuntime
        {
            public UpdatePerceptionRuntime(string name, float interval) : base(name, interval) { }

            protected override void OnTick(RuntimeBlackboard bb)
            {
                bb.Set(BBKey.HasTarget,        bb.Detection?.HasTarget        ?? false);
                bb.Set(BBKey.DistanceToTarget, bb.Detection?.DistanceToTarget ?? float.MaxValue);
            }
        }
    }
}
