using UnityEngine;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Condition/And")]
    public sealed class AndTriggerConditionSO : TriggerConditionSO
    {
        [SerializeField] private TriggerConditionSO[] _children;

        public override bool Evaluate(TriggerContext context)
        {
            if (_children == null || _children.Length == 0)
                return true;

            foreach (var child in _children)
            {
                if (child != null && !child.Evaluate(context))
                    return false;
            }

            return true;
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Condition/Or")]
    public sealed class OrTriggerConditionSO : TriggerConditionSO
    {
        [SerializeField] private TriggerConditionSO[] _children;

        public override bool Evaluate(TriggerContext context)
        {
            // 빈 논리합(OR)은 거짓. 자식 없는 Or를 "조건 없음"으로 끼워넣었을 때
            // 의도치 않게 항상 통과하는 함정을 막는다. (And의 vacuous-true와 대비)
            if (_children == null || _children.Length == 0)
                return false;

            foreach (var child in _children)
            {
                if (child != null && child.Evaluate(context))
                    return true;
            }

            return false;
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/Trigger/Condition/Not")]
    public sealed class NotTriggerConditionSO : TriggerConditionSO
    {
        [SerializeField] private TriggerConditionSO _child;

        public override bool Evaluate(TriggerContext context)
        {
            return _child == null || !_child.Evaluate(context);
        }
    }
}
