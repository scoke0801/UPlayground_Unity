using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.Dialogue
{
    public partial class DialogueManager
    {
        private readonly DialogueExpressionSession _expressionSession = new();
        public DialoguePortraitPresentation CurrentPortraitPresentation { get; private set; }
        public DialoguePortraitPresentation CurrentForegroundPresentation { get; private set; }

        private void ResolveExpression(DialogueNodeSO node)
        {
            var party = PartyManager.Instance;
            var active = party != null ? party.ActiveCharacterType : CharacterActorType.None;
            var protagonist = party != null ? party.StoryProtagonistType : CharacterActorType.None;
            Sprite legacy = DialogueSpeakerResolver.ResolvePortrait(node, party?.PartyMemberDataSO,
                active, protagonist, PortraitTable);
            CurrentPortraitPresentation = _expressionSession.Resolve(node, active, protagonist, PortraitTable, legacy);
        }

        private void ResolveForegroundExpression(DialogueNodeSO node)
        {
            CurrentForegroundPresentation = new DialoguePortraitPresentation(CurrentLineForegroundIllustration);
            if (node == null || !node.applyExpressionToForeground) return;
            var party = PartyManager.Instance;
            var set = PortraitTable?.FindPresentation(node.speakerId,
                party != null ? party.ActiveCharacterType : CharacterActorType.None,
                party != null ? party.StoryProtagonistType : CharacterActorType.None);
            if (set == null || CurrentLineForegroundIllustration == null) return;
            bool matches = CurrentLineForegroundIllustration == set.defaultPortrait;
            foreach (var frame in set.expressions)
                if (frame != null && frame.portrait == CurrentLineForegroundIllustration) matches = true;
            // 배경이나 다른 인물의 삽화를 현재 화자 이미지로 바꾸지 않는다.
            if (matches) CurrentForegroundPresentation = CurrentPortraitPresentation;
        }

        private void ClearExpressionSession()
        {
            _expressionSession.Clear();
            CurrentPortraitPresentation = default;
            CurrentForegroundPresentation = default;
        }
    }
}
