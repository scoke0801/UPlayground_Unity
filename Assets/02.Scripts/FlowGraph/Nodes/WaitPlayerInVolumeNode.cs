using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>저장 복원으로 이미 볼륨 안에 있는 경우에도 플레이어의 도착을 확인한다.</summary>
    [Serializable]
    [FlowNodeMenu("월드/Wait Player In Volume", Summary = "조작을 유지하며 맵에 저작된 목적지 도착을 기다립니다.")]
    public sealed class WaitPlayerInVolumeNode : FlowNode
    {
        public string volumeId;
        [Tooltip("대화를 취소한 직후 재시도 창이 뜨지 않도록 한 번 나갔다가 돌아오는 경우에만 통과합니다.")]
        public bool requireExitBeforeArrival;
        [Min(0.1f)] public float pollSeconds = 0.25f;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
                yield return FlowPortDef.Output("Failed");
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            FlowGraphTriggerVolume target = null;
            var volumes = token.Context.Runner.GetComponentsInChildren<FlowGraphTriggerVolume>(true);
            for (int i = 0; i < volumes.Length; i++)
            {
                if (!string.Equals(volumes[i].VolumeId, volumeId, StringComparison.Ordinal))
                    continue;
                if (target != null)
                {
                    token.Emit("Failed");
                    yield break;
                }
                target = volumes[i];
            }
            if (target == null)
            {
                token.Emit("Failed");
                yield break;
            }

            var wait = new WaitForSeconds(Mathf.Max(0.1f, pollSeconds));
            bool hasLeft = !requireExitBeforeArrival;
            while (!token.Context.Cancelled && target != null)
            {
                bool isInside = target.ContainsActor(Svc.ActorQuery?.Player);
                if (!isInside && Svc.ActorQuery?.Player != null)
                    hasLeft = true;
                if (hasLeft && isInside
                    && Svc.Dialogue?.IsDialogueActive != true)
                {
                    token.Emit(FlowPort.Out);
                    yield break;
                }
                yield return wait;
            }
            if (!token.Context.Cancelled)
                token.Emit("Failed");
        }
    }
}
