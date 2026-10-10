using System;
using System.Collections;
using System.Collections.Generic;

namespace UPlayGround.FlowGraph
{
    /// <summary>같은 흐름에서 생성 시 보류했던 액터의 전투를 공개 대화 이후 허용한다.</summary>
    [Serializable]
    [FlowNodeMenu("스토리/Release Story Combat", Summary = "공개가 끝난 액터의 전투 보류를 해제합니다.")]
    public sealed class ReleaseStoryCombatNode : FlowNode
    {
        public string actorId;
        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.DataInput<string>(SpawnStoryActorNode.ActorIdPort);
                yield return FlowPortDef.Output();
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            string resolved = token.Graph.TryEvaluateDataInput(
                token.Context, this, SpawnStoryActorNode.ActorIdPort, out string connected)
                && !string.IsNullOrWhiteSpace(connected) ? connected : actorId;
            string key = "storyCombatHold:" + resolved;
            if (token.Context.TryGet<IDisposable>(key, out var lease))
            {
                lease?.Dispose();
                token.Context.Set(key, null);
            }
            token.Emit(FlowPort.Out);
            yield break;
        }
    }
}
