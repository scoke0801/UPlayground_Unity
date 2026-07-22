using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.FlowGraph
{
    /// <summary>
    /// 다른 FlowGraphSO의 Manual 진입점을 중첩 실행한다. 하위 그래프의 모든 토큰이 소진되면 Out으로 통과.
    /// 하위 컨텍스트는 발화 원인(Collider/Actor)을 상속하고, 진입점의 재진입 정책은 그대로 존중된다.
    /// </summary>
    [FlowNodeMenu("코어/SubGraph")]
    [Serializable]
    public sealed class SubGraphNode : FlowNode
    {
        private const int MaxDepth = 8;

        public FlowGraphSO subGraph;

        [Tooltip("발화할 하위 그래프 Manual 진입점의 entryId. 비우면 모든 Manual 진입점.")]
        public string entryId;

        [Tooltip("true면 하위 그래프의 모든 토큰이 끝날 때까지 대기 후 Out, false면 발화 직후 통과.")]
        public bool waitForCompletion = true;

        public override string DisplayName =>
            subGraph != null ? $"SubGraph [{subGraph.name}]" : "SubGraph";

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
            if (subGraph == null)
            {
                Debug.LogWarning("[FlowGraph] SubGraph: 하위 그래프 미지정 — 통과");
                token.Emit(FlowPort.Out);
                yield break;
            }

            if (subGraph == token.Graph || token.Context.Depth >= MaxDepth)
            {
                Debug.LogError($"[FlowGraph] SubGraph: 자기 참조 또는 중첩 깊이 초과({token.Context.Depth}) — {subGraph.name} 실행 거부");
                token.Emit(FlowPort.Out);
                yield break;
            }

            var children = new List<FlowContext>();
            foreach (FlowNode node in subGraph.nodes)
            {
                if (node is ManualEntryNode entry
                    && (string.IsNullOrEmpty(entryId) || entry.entryId == entryId))
                {
                    FlowContext child = token.Context.Runner
                        .FireEntryInGraph(subGraph, entry, token.Context);
                    if (child != null)
                        children.Add(child);
                }
            }

            if (waitForCompletion)
            {
                bool AnyRunning()
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        if (children[i].ActiveTokenCount > 0)
                            return true;
                    }
                    return false;
                }

                while (!token.Context.Cancelled && AnyRunning())
                    yield return null;

                // 부모가 취소되면 하위 플로우도 함께 취소한다.
                if (token.Context.Cancelled)
                {
                    for (int i = 0; i < children.Count; i++)
                        children[i].Cancelled = true;
                    yield break;
                }
            }

            token.Emit(FlowPort.Out);
        }
    }
}
