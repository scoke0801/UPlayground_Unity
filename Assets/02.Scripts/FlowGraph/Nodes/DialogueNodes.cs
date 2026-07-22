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
    [FlowNodeMenu("대화/PlayDialogue")]
    [Serializable]
    public sealed class PlayDialogueNode : FlowNode
    {
        public DialogueGraphSO dialogue;

        public override string DisplayName =>
            dialogue != null ? $"PlayDialogue [{dialogue.name}]" : "PlayDialogue";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            IDialogueService service = Svc.Dialogue;
            if (service == null || dialogue == null)
            {
                Debug.LogWarning("[FlowGraph] PlayDialogue: 대화 서비스 또는 그래프 미지정 — 통과");
                token.Emit(FlowPort.Out);
                yield break;
            }

            bool done = false;
            IDisposable request = service.TryStartDialogueTracked(dialogue, () => done = true);
            if (request == null)
            {
                Debug.LogWarning($"[FlowGraph] PlayDialogue: 대화 시작이 거부됨 — {dialogue.name}");
                token.Emit(FlowPort.Out);
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

            token.Emit(FlowPort.Out);
        }
    }
}
