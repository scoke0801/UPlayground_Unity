using UPlayGround.Data.EnumType;
using UnityEngine;
using UPlayGround.Dialogue;
using UPlayGround.Story;

namespace UPlayGround.Data.Actor
{
    /// <summary>
    /// NPC 한 명의 설정 데이터.
    /// storyEntries: 이 NPC가 말하는 스토리(우선) / dialogueGraph: 스토리가 없을 때의 기본 대화
    /// </summary>
    [CreateAssetMenu(fileName = "NPC_", menuName = "UPlayGround/액터/NPC")]
    public class NpcActorSO : InteractableActorSO
    {
        [Header("NPC 설정")]
        [Tooltip("이 NPC가 담당하는 스토리. 위에서부터 조건이 맞는 첫 항목을 재생하고, " +
                 "없으면 dialogueGraph로 넘어간다. triggerMode가 NpcTalk인 엔트리를 연결한다.")]
        public StoryEntrySO[] storyEntries;

        [Tooltip("담당 스토리가 모두 소진됐을 때 반복 재생할 기본 대화")]
        public DialogueGraphSO dialogueGraph;

        private void OnEnable()
        {
            // NPC는 인터랙션 타입을 항상 NPC로 고정
            interactionObjectType = InteractionObjectType.NPC;
            showInfoUI = false;
            showShakeEffect = false;
        }
    }
}
