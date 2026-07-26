using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>전역 플래그가 특정 값으로 변경될 때 발화하는 진입점.</summary>
    [FlowNodeMenu("진입점/OnFlagChanged", Summary = "Global Flag가 지정 값으로 바뀔 때 시작합니다.", Keywords = new[] { "flag", "entry", "변경" })]
    [Serializable]
    public sealed class OnFlagChangedEntryNode : EntryNode
    {
        public string flagKey;
        public bool requiredValue = true;

        public override string DisplayName => $"Entry: Flag [{flagKey}]={requiredValue}";

        public override void Arm(FlowGraphRunner runner)
        {
            IGlobalFlagService flags = Svc.Flags;
            if (flags == null)
            {
                Debug.LogWarning($"[FlowGraph] IGlobalFlagService 미등록 — OnFlagChanged 진입점({flagKey}) 무장 실패");
                return;
            }

            Action<string, bool> handler = (key, value) =>
            {
                if (key == flagKey && value == requiredValue)
                    runner.FireEntry(this);
            };
            flags.OnFlagChanged += handler;
            runner.StoreEntryTeardown(this, () => flags.OnFlagChanged -= handler);
        }
    }

    /// <summary>전역 플래그를 설정한다.</summary>
    [FlowNodeMenu("플래그/SetFlag", Summary = "Global Flag 값을 설정합니다.", Keywords = new[] { "flag", "bool", "설정" })]
    [Serializable]
    public sealed class SetFlagNode : FlowNode
    {
        public string flagKey;
        public bool value = true;

        public override string DisplayName => $"SetFlag [{flagKey}]={value}";

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
            Svc.Flags?.SetFlag(flagKey, value);
            token.Emit(FlowPort.Out);
            yield break;
        }
    }

    /// <summary>플래그 값으로 True/False 분기. (BranchNode + FlagCondition의 단축형)</summary>
    [FlowNodeMenu("플래그/CheckFlag (Branch)", Summary = "Global Flag 값을 비교해 분기합니다.", Keywords = new[] { "flag", "if", "조건", "비교" })]
    [Serializable]
    public sealed class CheckFlagNode : FlowNode
    {
        public string flagKey;
        public bool expectedValue = true;

        public override string DisplayName => $"CheckFlag [{flagKey}]";

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output(FlowPort.True);
                yield return FlowPortDef.Output(FlowPort.False);
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            IGlobalFlagService flags = Svc.Flags;
            bool result = flags != null && flags.GetFlag(flagKey) == expectedValue;
            token.Emit(result ? FlowPort.True : FlowPort.False);
            yield break;
        }
    }
}
