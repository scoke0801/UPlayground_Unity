using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.Story
{
    /// <summary>
    /// 스토리 이벤트 하나의 데이터 묶음.
    /// storyId로 완료 여부를 추적하고, requiredProgress 이상일 때만 트리거됩니다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/Story/Entry", fileName = "Story_")]
    public class StoryEntrySO : ScriptableObject
    {
        [Tooltip("저장/식별에 사용하는 고유 ID. 한번 정하면 변경 금지.")]
        public string storyId;

        [Tooltip("이 스토리가 재생될 최소 게임 진행도")]
        public int requiredProgress;

        [Tooltip("실행할 대화 그래프")]
        public DialogueGraphSO dialogueGraph;

        [Tooltip("같은 위치에서 진행도에 따라 다른 대화를 쓸 때 이 배열에 추가.\n requiredProgress 내림차순으로 가장 조건에 맞는 것이 선택됩니다.")]
        public StoryVariant[] variants;
    }

    /// <summary>
    /// 진행도별 대체 대화. StoryEntrySO의 기본 graph 대신 사용됩니다.
    /// </summary>
    [System.Serializable]
    public class StoryVariant
    {
        [Tooltip("이 변형이 사용될 최소 진행도")]
        public int requiredProgress;
        public DialogueGraphSO dialogueGraph;
    }
}
