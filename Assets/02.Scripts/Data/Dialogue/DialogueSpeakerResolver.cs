using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

namespace UPlayGround.Dialogue
{
    /// <summary>
    /// 화자 표시명·초상화 해석을 한곳에 모은 유틸.
    /// 뷰(UI_Scene_Dialogue)와 대화 이력 기록(DialogueManager)이 동일한 해석 결과를 쓰도록 공용화했습니다.
    /// 파티 서비스 계약이 모듈마다 다르므로(UISvc.Party / PartyManager) 필요한 데이터만 인자로 받습니다.
    /// </summary>
    public static class DialogueSpeakerResolver
    {
        public const string PlayerSpeakerId = "당신";
        public const string PlayerActorId = "Player";
        public const string ProtagonistSpeakerId = "Protagonist";

        private static bool s_warnedMissingProtagonist;

        public static bool IsPlayerSpeaker(DialogueNodeSO node)
        {
            return node != null && IsActivePlayerSpeaker(node.speakerId);
        }

        public static bool IsActivePlayerSpeaker(DialogueNodeSO node) =>
            node != null && IsActivePlayerSpeaker(node.speakerId);

        public static bool IsActivePlayerSpeaker(string speakerId) =>
            speakerId == PlayerSpeakerId || speakerId == PlayerActorId;

        public static bool IsProtagonistSpeaker(DialogueNodeSO node) =>
            node != null && IsProtagonistSpeaker(node.speakerId);

        public static bool IsProtagonistSpeaker(string speakerId) =>
            speakerId == ProtagonistSpeakerId;

        /// <summary>
        /// 플레이어 화자면 현재 활성 캐릭터 이름으로, 아니면 노드의 speakerId를 그대로 반환합니다.
        /// </summary>
        public static string ResolveSpeakerName(
            DialogueNodeSO node,
            PartyMemberDataSO memberData,
            CharacterActorType activeType,
            CharacterActorType protagonistType)
        {
            if (node == null)
                return string.Empty;

            CharacterActorType resolvedType;
            if (IsActivePlayerSpeaker(node))
                resolvedType = activeType;
            else if (IsProtagonistSpeaker(node))
                resolvedType = protagonistType;
            else
                return node.speakerId;

            WarnIfMissingProtagonist(node, protagonistType);
            string resolvedName = memberData != null ? memberData.GetName(resolvedType) : string.Empty;
            return string.IsNullOrEmpty(resolvedName) ? node.speakerId : resolvedName;
        }

        /// <summary>
        /// 플레이어 화자면 활성 캐릭터 전신 스프라이트를, 없으면 노드 초상화로 폴백합니다.
        /// </summary>
        public static Sprite ResolvePortrait(
            DialogueNodeSO node,
            PartyMemberDataSO memberData,
            CharacterActorType activeType,
            CharacterActorType protagonistType)
        {
            if (node == null)
                return null;

            CharacterActorType resolvedType;
            if (IsActivePlayerSpeaker(node))
                resolvedType = activeType;
            else if (IsProtagonistSpeaker(node))
                resolvedType = protagonistType;
            else
                return node.portrait;

            WarnIfMissingProtagonist(node, protagonistType);
            Sprite resolvedPortrait = memberData != null ? memberData.GetFullBodySprite(resolvedType) : null;
            return resolvedPortrait != null ? resolvedPortrait : node.portrait;
        }

        private static void WarnIfMissingProtagonist(
            DialogueNodeSO node,
            CharacterActorType protagonistType)
        {
            if (!IsProtagonistSpeaker(node)
                || protagonistType != CharacterActorType.None
                || s_warnedMissingProtagonist)
                return;

            s_warnedMissingProtagonist = true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning("[DialogueSpeakerResolver] Protagonist 화자를 해석할 서사 주인공이 없습니다.");
#endif
        }
    }

    /// <summary>대화 본문과 선택지에서 예약된 플레이어 이름 토큰만 치환한다.</summary>
    public static class DialogueTextResolver
    {
        public const string PlayerNameToken = "{PlayerName}";
        public const string ProtagonistNameToken = "{ProtagonistName}";

        private static bool s_warnedMissingPlayerName;
        private static bool s_warnedMissingProtagonistName;

        public static string Resolve(
            string source,
            string activePlayerName,
            string protagonistName)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            string resolved = ReplaceKnownToken(
                source,
                PlayerNameToken,
                activePlayerName,
                ref s_warnedMissingPlayerName);
            return ReplaceKnownToken(
                resolved,
                ProtagonistNameToken,
                protagonistName,
                ref s_warnedMissingProtagonistName);
        }

        private static string ReplaceKnownToken(
            string source,
            string token,
            string value,
            ref bool warned)
        {
            if (!source.Contains(token))
                return source;

            if (!string.IsNullOrEmpty(value))
                return source.Replace(token, value);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!warned)
            {
                warned = true;
                Debug.LogWarning($"[DialogueTextResolver] {token} 토큰의 치환 값을 찾지 못했습니다.");
            }
#endif
            return source;
        }
    }
}
