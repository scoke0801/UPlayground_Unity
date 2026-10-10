using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Dialogue;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>
    /// DialogueManager에 대화 재생을 위임하고 종료까지 대기한다.
    /// 대화 내부 분기는 Dialogue 그래프가, 대화 전후 매크로 흐름은 FlowGraph가 담당(대체 아님).
    /// </summary>
    [FlowNodeMenu("대화/PlayDialogue", Summary = "대화 그래프를 재생하고 종료까지 대기합니다.", Keywords = new[] { "dialogue", "conversation", "대사" })]
    [Serializable]
    public sealed class PlayDialogueNode : FlowNode
    {
        public const string PartnerActorIdPort = "PartnerActorId";
        public const string FailedPort = "Failed";
        public const string CancelledPort = "Cancelled";

        public DialogueGraphSO dialogue;
        [Tooltip("켜면 뒤로 가기로 대화를 닫은 경우도 명시적 건너뛰기로 보고 다음 흐름을 실행합니다.")]
        public bool continueWhenCancelled;
        [Tooltip("파트너 Actor ID 데이터 포트가 연결되지 않았을 때 사용할 Actor ID.")]
        public string partnerActorId;
        [Tooltip("파트너 인스턴스를 이 대화 그래프의 어떤 speakerId로 해석할지 지정합니다.")]
        public string partnerSpeakerId;

        public override string DisplayName =>
            dialogue != null ? $"PlayDialogue [{dialogue.name}]" : "PlayDialogue";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.DataInput<string>(
                    PartnerActorIdPort,
                    displayName: "파트너 Actor ID");
                yield return FlowPortDef.Output();
                yield return FlowPortDef.Output(FailedPort);
                yield return FlowPortDef.Output(CancelledPort, optional: true);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            IDialogueService service = Svc.Dialogue;
            if (service == null || dialogue == null)
            {
                Debug.LogWarning("[FlowGraph] PlayDialogue: 대화 서비스 또는 그래프 미지정");
                token.Emit(FailedPort);
                yield break;
            }

            // 같은 프레임에 끝난 대화의 카메라·UI 정리 후 다음 요청을 시작한다.
            yield return null;
            while (service.IsDialogueActive && !token.Context.Cancelled)
                yield return null;
            if (token.Context.Cancelled)
                yield break;

            bool done = false;
            bool cancelled = false;
            IWorldActor partnerOverride = ResolvePartnerActor(token);
            IDisposable request = service.TryStartDialogueTracked(
                dialogue,
                () => done = true,
                partnerOverride,
                onCancelled: () =>
                {
                    cancelled = true;
                    done = true;
                },
                partnerSpeakerId: partnerSpeakerId);
            if (request == null)
            {
                Debug.LogWarning($"[FlowGraph] PlayDialogue: 대화 시작이 거부됨 — {dialogue.name}");
                token.Emit(FailedPort);
                yield break;
            }

            try
            {
                while (!done && !token.Context.Cancelled)
                    yield return null;
            }
            finally
            {
                request.Dispose();
            }

            if ((!cancelled || continueWhenCancelled) && !token.Context.Cancelled)
                token.Emit(FlowPort.Out);
            else if (cancelled && !token.Context.Cancelled)
                token.Emit(CancelledPort);
        }

        private IWorldActor ResolvePartnerActor(FlowToken token)
        {
            string resolvedActorId = token.Graph.TryEvaluateDataInput(
                                         token.Context,
                                         this,
                                         PartnerActorIdPort,
                                         out string connectedActorId)
                                     && !string.IsNullOrWhiteSpace(connectedActorId)
                ? connectedActorId
                : partnerActorId;
            return string.IsNullOrWhiteSpace(resolvedActorId)
                ? null
                : Svc.ActorQuery?.FindActor(resolvedActorId);
        }
    }
}
