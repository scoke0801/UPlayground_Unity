using UnityEngine;
using UPlayGround.Dialogue;

namespace UPlayGround.Story
{
    /// <summary>
    /// 스토리가 어떤 경로로 재생되는지를 지정한다.
    /// 기본값(NpcTalk)이 0인 것은 의도적이다 — 트리거를 지정하지 않은 엔트리가
    /// 자동 재생 큐로 흘러들어 화자 없이 재생되는 사고를 막는다.
    /// </summary>
    public enum StoryTriggerMode
    {
        [Tooltip("NPC와 상호작용할 때 재생. NpcActorSO.storyEntries에 연결해야 한다.")]
        NpcTalk = 0,

        [Tooltip("StoryTriggerZone 진입 등 배치된 트리거가 재생. 독백/연출용.")]
        Zone = 1,

        [Tooltip("진행도 조건만 맞으면 게임플레이 씬에서 자동 재생. 화자 없는 나레이션에만 사용.")]
        Auto = 2,
    }

    /// <summary>
    /// 스토리 이벤트 하나의 데이터 묶음.
    /// storyId로 완료 여부를 추적하고, requiredProgress 이상이면서
    /// maxProgressExclusive가 0이거나 그 값 미만일 때만 트리거됩니다.
    /// </summary>
    [CreateAssetMenu(menuName = "UPlayGround/스토리/Entry", fileName = "Story_")]
    public class StoryEntrySO : ScriptableObject
    {
        [Tooltip("저장/식별에 사용하는 고유 ID. 한번 정하면 변경 금지.")]
        public string storyId;

        [Tooltip("이 스토리가 재생될 최소 게임 진행도")]
        public int requiredProgress;

        [Tooltip("0이면 상한 없음. 양수면 현재 진행도가 이 값 미만일 때만 재생됩니다. 지나간 회차의 NPC 대사가 뒤늦게 재생되는 것을 막습니다.")]
        public int maxProgressExclusive;

        [Tooltip("재생 경로. Auto만 자동 재생 큐에 올라간다.")]
        public StoryTriggerMode triggerMode = StoryTriggerMode.NpcTalk;

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
