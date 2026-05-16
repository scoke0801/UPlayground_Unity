using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 지정한 Blackboard bool 키 값이 트리거 값으로 변경되면 실행 중인 자식을 강제로 Abort한다.
    /// GuardCondition과 달리 매 Tick 비교가 아니라 "변화 이벤트" 기반으로 동작한다.
    /// 트리거가 잡힐 때까지는 자식 결과를 그대로 반환한다.
    /// </summary>
    public class ForceAbortNode : BTDecoratorNode
    {
        [SerializeField] private BlackboardKeySelector _key = new("AbortTrigger", BlackboardValueType.Bool);
        [SerializeField] private bool _triggerOn = true;

        private bool _hasLastValue;
        private bool _lastValue;

        public BlackboardKeySelector Key
        {
            get => _key;
            set => _key = value;
        }

        public bool TriggerOn
        {
            get => _triggerOn;
            set => _triggerOn = value;
        }

        protected override void OnStart()
        {
            _hasLastValue = false;
            _lastValue = false;
        }

        protected override BTStatus OnUpdate()
        {
            if (Child == null || Context?.Blackboard == null)
                return BTStatus.Failure;

            var current = Context.Blackboard.TryGetBool(_key, out var value) && value;

            if (_hasLastValue && current != _lastValue && current == _triggerOn)
            {
                AbortChild();
                _lastValue = current;
                Context?.DebugTrace?.Record(this, "ForceAbort", BTStatus.Failure, $"{_key.Key} -> {current}");
                return BTStatus.Failure;
            }

            _lastValue = current;
            _hasLastValue = true;

            return Child.Tick();
        }

        protected override void OnAbort()
        {
            AbortChild();
            _hasLastValue = false;
        }

        protected override void OnReset()
        {
            ResetChild();
            _hasLastValue = false;
        }
    }
}
