using System;
using System.Collections;
using System.Collections.Generic;
using UPlayGround.Data.EnumType;
using UPlayGround.Dialogue;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>비전투 동행 의사의 정상 종료 뒤에만 보유 명단과 사건 완료를 확정한다.</summary>
    [Serializable]
    [FlowNodeMenu("영입/비전투 동의", Summary = "전투 승리 없이 동의 대화와 실제 영입을 확인합니다. 취소하면 재시도할 수 있습니다.")]
    public sealed class RecruitCharacterByConsentNode : FlowNode
    {
        public CharacterActorType recruitCharacter;
        public DialogueGraphSO dialogue;
        public DialogueGraphSO alreadyOwnedDialogue;
        public string partnerActorId;
        public string partnerSpeakerId;
        public string requiredFlagKey;
        public string completedFlagKey;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output("Completed");
                yield return FlowPortDef.Output("Cancelled", optional: true);
                yield return FlowPortDef.Output("Failed", optional: true);
            }
        }

        /// <summary>대화 요청 수명 안에서 정상 완료를 증명하고, 실패한 영입은 완료로 소비하지 않는다.</summary>
        public override IEnumerator Execute(FlowToken token)
        {
            if (!Services.TryGet<IPartyService>(out var party)
                || !Services.TryGet<IGlobalFlagService>(out var flags)
                || !Services.TryGet<IDialogueService>(out var conversations)
                || string.IsNullOrWhiteSpace(completedFlagKey)
                || recruitCharacter == CharacterActorType.None
                || !Enum.IsDefined(typeof(CharacterActorType), recruitCharacter))
            {
                token.Emit("Failed");
                yield break;
            }

            yield return null;
            while (conversations.IsDialogueActive && !token.Context.Cancelled)
                yield return null;
            if (token.Context.Cancelled)
                yield break;
            if (flags.GetFlag(completedFlagKey))
            {
                token.Emit(party.IsCharacterUnlocked(recruitCharacter) ? "Completed" : "Failed");
                yield break;
            }
            if (!HasRequiredFact(flags))
            {
                token.Emit("Failed");
                yield break;
            }

            bool isOwned = party.IsCharacterUnlocked(recruitCharacter);
            DialogueGraphSO selected = isOwned ? alreadyOwnedDialogue : dialogue;
            if (selected == null)
            {
                token.Emit("Failed");
                yield break;
            }
            IWorldActor partner = string.IsNullOrWhiteSpace(partnerActorId)
                ? null : Svc.ActorQuery?.FindActor(partnerActorId);
            bool completed = false;
            bool cancelled = false;
            IDisposable request = conversations.TryStartDialogueTracked(
                selected, () => completed = true, partner,
                () => cancelled = true, partnerSpeakerId: partnerSpeakerId);
            if (request == null)
            {
                token.Emit("Failed");
                yield break;
            }
            token.Context.RegisterTeardown(request);
            try
            {
                while (!completed && !cancelled && !token.Context.Cancelled)
                    yield return null;
            }
            finally
            {
                request.Dispose();
            }
            if (token.Context.Cancelled)
                yield break;
            if (cancelled)
            {
                token.Emit("Cancelled");
                yield break;
            }
            if (!completed || !HasRequiredFact(flags))
            {
                token.Emit("Failed");
                yield break;
            }

            CharacterUnlockResult result = party.EnsureCharacterUnlocked(recruitCharacter);
            if (result is not CharacterUnlockResult.AddedToBattle
                and not CharacterUnlockResult.PreparingBattle
                and not CharacterUnlockResult.AddedToRoster
                and not CharacterUnlockResult.AlreadyOwned
                || !party.IsCharacterUnlocked(recruitCharacter))
            {
                token.Emit("Failed");
                yield break;
            }
            flags.SetFlag(completedFlagKey, true);
            token.Emit("Completed");
        }

        private bool HasRequiredFact(IGlobalFlagService flags) =>
            string.IsNullOrWhiteSpace(requiredFlagKey) || flags.GetFlag(requiredFlagKey);
    }
}
