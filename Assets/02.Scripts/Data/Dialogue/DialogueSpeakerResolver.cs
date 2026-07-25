using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 화자 표시명·초상화 해석을 한곳에 모은 유틸.
    /// 뷰(UI_Dialogue)와 대화 이력 기록(DialogueManager)이 동일한 해석 결과를 쓰도록 공용화했습니다.
    /// 파티 서비스 계약이 모듈마다 다르므로(UISvc.Party / PartyManager) 필요한 데이터만 인자로 받습니다.
    /// </summary>
    public static class DialogueSpeakerResolver
    {
        public const string PlayerSpeakerId = "당신";
        public const string PlayerActorId = "Player";

        public static bool IsPlayerSpeaker(DialogueNodeSO node)
        {
            return node != null && (node.speakerId == PlayerSpeakerId || node.speakerId == PlayerActorId);
        }

        /// <summary>
        /// 플레이어 화자면 현재 활성 캐릭터 이름으로, 아니면 노드의 speakerId를 그대로 반환합니다.
        /// </summary>
        public static string ResolveSpeakerName(
            DialogueNodeSO node,
            PartyMemberDataSO memberData,
            CharacterActorType activeType)
        {
            if (node == null)
                return string.Empty;

            if (!IsPlayerSpeaker(node))
                return node.speakerId;

            string activeName = memberData != null ? memberData.GetName(activeType) : string.Empty;
            return string.IsNullOrEmpty(activeName) ? node.speakerId : activeName;
        }

        /// <summary>
        /// 플레이어 화자면 활성 캐릭터 전신 스프라이트를, 없으면 노드 초상화로 폴백합니다.
        /// </summary>
        public static Sprite ResolvePortrait(
            DialogueNodeSO node,
            PartyMemberDataSO memberData,
            CharacterActorType activeType)
        {
            if (node == null)
                return null;

            if (!IsPlayerSpeaker(node))
                return node.portrait;

            Sprite activePortrait = memberData != null ? memberData.GetFullBodySprite(activeType) : null;
            return activePortrait != null ? activePortrait : node.portrait;
        }
    }
}
