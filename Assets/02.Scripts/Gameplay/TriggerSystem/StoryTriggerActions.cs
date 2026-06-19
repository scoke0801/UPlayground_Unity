using System.Collections;
using UnityEngine;
using UPlayGround.Story;

namespace UPlayGround.TriggerSystem
{
    [CreateAssetMenu(menuName = "UPlayGround/트리거/액션/Trigger Story")]
    public sealed class TriggerStoryTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private StoryEntrySO _storyEntry;

        public override bool CanExecute(TriggerContext context)
        {
            return _storyEntry != null && StoryManager.Instance != null;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            StoryManager.Instance?.TryTriggerStory(_storyEntry);
            yield break;
        }
    }

    [CreateAssetMenu(menuName = "UPlayGround/트리거/액션/Set Story Progress")]
    public sealed class SetStoryProgressTriggerActionSO : TriggerActionSO
    {
        [SerializeField] private int _progress;

        public override bool CanExecute(TriggerContext context)
        {
            return StoryManager.Instance != null;
        }

        public override IEnumerator Execute(TriggerContext context)
        {
            StoryManager.Instance?.SetProgress(_progress);
            yield break;
        }
    }
}
