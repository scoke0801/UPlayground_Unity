using UnityEngine;
using UPlayGround.Story;

namespace UPlayGround.Group
{
    /// <summary>
    /// MonsterGroupController와 StoryManager를 연결한다.
    /// 그룹 전멸 시 지정된 StoryEntry를 트리거하고,
    /// afterProgress > 0 이면 스토리 진행도도 올린다.
    ///
    /// 사용법: MonsterGroup GameObject에 붙이고
    ///   - targetGroup : 감시할 MonsterGroupController
    ///   - storyEntry  : 전멸 후 재생할 StoryEntrySO
    ///   - afterProgress: 트리거 후 세팅할 진행도 (0 = 변경 안 함)
    /// </summary>
    public class GroupStoryTrigger : MonoBehaviour
    {
        [SerializeField] private MonsterGroupController _targetGroup;
        [SerializeField] private StoryEntrySO           _storyEntry;

        [Tooltip("전멸 후 올릴 스토리 진행도. 0이면 변경하지 않는다.")]
        [SerializeField] private int _afterProgress = 0;

        private void Awake()
        {
            if (_targetGroup == null)
                _targetGroup = GetComponent<MonsterGroupController>();
        }

        private void OnEnable()
        {
            if (_targetGroup != null)
                _targetGroup.OnGroupDefeated += HandleGroupDefeated;
        }

        private void OnDisable()
        {
            if (_targetGroup != null)
                _targetGroup.OnGroupDefeated -= HandleGroupDefeated;
        }

        private void HandleGroupDefeated()
        {
            // 진행도를 먼저 올려야 StoryEntry의 requiredProgress 조건을 통과할 수 있다.
            if (_afterProgress > 0)
                StoryManager.Instance.SetProgress(_afterProgress);

            if (_storyEntry != null)
                StoryManager.Instance.TryTriggerStory(_storyEntry);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_targetGroup == null)
                _targetGroup = GetComponent<MonsterGroupController>();
        }
#endif
    }
}
