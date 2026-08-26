using UnityEngine;

namespace UPlayGround.TriggerSystem
{
    /// <summary>지정한 조건을 모두 만족할 때만 통과한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/트리거/조건/And")]
    public sealed class AndTriggerConditionSO : TriggerConditionSO
    {
        [SerializeField] private TriggerConditionSO[] _children;

        public override bool Evaluate(TriggerContext context)
        {
            if (_children == null || _children.Length == 0)
                return true;

            foreach (TriggerConditionSO child in _children)
            {
                if (child != null && !child.Evaluate(context))
                    return false;
            }

            return true;
        }
    }
}
