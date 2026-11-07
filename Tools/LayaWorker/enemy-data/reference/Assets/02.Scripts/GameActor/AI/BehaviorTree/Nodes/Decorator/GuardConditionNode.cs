using UnityEngine;

namespace UPlayGround.AI.BehaviorTree
{
    /// <summary>
    /// 지정한 Blackboard bool 키가 기대값과 같을 때만 자식을 실행한다.
    /// 자식이 Running 중이라도 매 Tick 키 값이 바뀌면 자식을 Abort하고 Failure를 반환한다.
    /// UE Decorator의 Observer 패턴 중 가장 단순한 형태.
    /// </summary>
    public class GuardConditionNode : BTDecoratorNode
    {
        [SerializeField] private BlackboardKeySelector _key = new("Guard", BlackboardValueType.Bool);
        [SerializeField] private bool _expectedValue = true;

        public BlackboardKeySelector Key
        {
            get => _key;
            set => _key = value;
        }

        public bool ExpectedValue
        {
            get => _expectedValue;
            set => _expectedValue = value;
        }

        protected override BTStatus OnUpdate()
        {
            if (Child == null || Context?.Blackboard == null)
                return BTStatus.Failure;

            var actual = Context.Blackboard.TryGetBool(_key, out var value) && value;
            if (actual != _expectedValue)
            {
                AbortChild();
                return BTStatus.Failure;
            }

            return Child.Tick();
        }

        protected override void OnAbort()
        {
            AbortChild();
        }

        protected override void OnReset()
        {
            ResetChild();
        }
    }
}
