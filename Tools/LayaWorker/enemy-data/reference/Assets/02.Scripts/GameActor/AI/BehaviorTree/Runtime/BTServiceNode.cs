using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 부착된 Composite가 실행 중일 때 지정한 주기로 OnTick이 호출되는 노드.
    /// 자식 노드 흐름에 들어가지 않고, Blackboard 폴링/주기 갱신 같은 백그라운드 작업에 쓰인다.
    /// Composite.OnStart 시 OnServiceEnter, Composite가 Running인 동안 _interval마다 OnServiceTick,
    /// Composite.OnStop 시 OnServiceExit이 호출된다.
    /// </summary>
    public abstract class BTServiceNode : BTNode
    {
        [SerializeField] private float _interval = 0.5f;
        [SerializeField] private bool _tickOnEnter = true;

        private float _timer;

        public float Interval
        {
            get => _interval;
            set => _interval = Mathf.Max(0f, value);
        }

        public bool TickOnEnter
        {
            get => _tickOnEnter;
            set => _tickOnEnter = value;
        }

        protected override BTStatus OnUpdate()
        {
            // Service는 Composite에서 직접 호출되므로 자식 Tick 흐름에 끼지 않는다.
            return BTStatus.Success;
        }

        internal void ServiceEnter()
        {
            _timer = _tickOnEnter ? _interval : 0f;
            OnServiceEnter();
            Context?.DebugTrace?.Record(this, "ServiceEnter", BTStatus.Running);
        }

        internal void ServiceTick(float deltaTime)
        {
            _timer += deltaTime;
            if (_timer < _interval)
                return;

            _timer = 0f;
            OnServiceTick();
            Context?.DebugTrace?.Record(this, "ServiceTick", BTStatus.Running);
        }

        internal void ServiceExit()
        {
            OnServiceExit();
            Context?.DebugTrace?.Record(this, "ServiceExit", BTStatus.Success);
            _timer = 0f;
        }

        protected virtual void OnServiceEnter() { }
        protected abstract void OnServiceTick();
        protected virtual void OnServiceExit() { }
    }
}
