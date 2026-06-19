using UnityEngine;
using UPlayGround.Story;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/트리거/조건/Story Progress")]
    public sealed class StoryProgressTriggerConditionSO : TriggerConditionSO
    {
        [SerializeField] private int _minProgress = 0;
        [SerializeField] private int _maxProgress = int.MaxValue;

        public override bool Evaluate(TriggerContext context)
        {
            if (StoryManager.Instance == null)
                return false;

            int progress = StoryManager.Instance.CurrentProgress;
            return progress >= _minProgress && progress <= _maxProgress;
        }
    }
}
