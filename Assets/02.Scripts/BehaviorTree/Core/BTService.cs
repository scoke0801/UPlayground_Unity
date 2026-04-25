using UnityEngine;

namespace UPlayGround.BehaviorTree
{
    /// <summary>
    /// Service 런타임 인스턴스. SO는 공유되므로 타이머 상태는 이 클래스에 보관한다.
    /// BTSelector / BTSequence가 Tick될 때마다 tickInterval 간격으로 OnTick을 호출한다.
    /// </summary>
    public abstract class BTServiceRuntime
    {
        private float _lastTick = -999f;

        protected readonly string ServiceName;
        protected readonly float  TickInterval;

        protected BTServiceRuntime(string name, float tickInterval)
        {
            ServiceName  = name;
            TickInterval = tickInterval;
        }

        public void TryTick(RuntimeBlackboard bb)
        {
            if (Time.time - _lastTick < TickInterval) return;
            _lastTick = Time.time;
            OnTick(bb);
        }

        protected abstract void OnTick(RuntimeBlackboard bb);
    }
}
