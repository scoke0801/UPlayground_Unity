using Interaction.Enum;
using UnityEngine;
using UPlayGround.Dialogue;

/// <summary>
/// NPC 한 명의 설정 데이터.
/// dialogueGraph: 기본 대화 / progressDialogues: 진행도별 대화 (StoryManager 연동 시 사용)
/// </summary>
[CreateAssetMenu(fileName = "NPC_", menuName = "UPlayGround/ActorData/NpcActorSO")]
public class NpcActorSO : InteractableActorSO
{
    [Header("NPC 설정")]
    public DialogueGraphSO dialogueGraph;

    private void OnEnable()
    {
        // NPC는 인터랙션 타입을 항상 NPC로 고정
        interactionObjectType = InteractionObjectType.NPC;
        showInfoUI = false;
        showShakeEffect = false;
    }
}
