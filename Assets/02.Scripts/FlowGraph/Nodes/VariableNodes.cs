using System;
using System.Collections;
using System.Collections.Generic;

namespace UPlayGround.FlowGraph
{
    /// <summary>그래프 블랙보드 변수에 값을 대입한다 (변수는 그래프 에셋의 Blackboard 패널에서 선언).</summary>
    [FlowNodeMenu("변수/SetVariable")]
    [Serializable]
    public sealed class SetVariableNode : FlowNode
    {
        [FlowVariableName] public string variableName;
        public FlowVariableValue value = new();

        public override string DisplayName => $"Set [{variableName}] = {value}";

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
            if (!string.IsNullOrEmpty(variableName))
                token.Context.Set(variableName, value.Get());
            token.Emit(FlowPort.Out);
            yield break;
        }
    }

    /// <summary>그래프 블랙보드 변수 값으로 True/False 분기.</summary>
    [FlowNodeMenu("변수/CheckVariable (Branch)")]
    [Serializable]
    public sealed class CheckVariableNode : FlowNode
    {
        [FlowVariableName] public string variableName;
        public FlowVariableValue expected = new();

        public override string DisplayName => $"Check [{variableName}] == {expected}";

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
            bool result = !string.IsNullOrEmpty(variableName)
                && token.Context.TryGet(variableName, out object raw)
                && expected.Matches(raw);
            token.Emit(result ? FlowPort.True : FlowPort.False);
            yield break;
        }
    }

    /// <summary>Branch/Wait(Condition) 노드에서 쓰는 블랙보드 변수 비교 조건.</summary>
    [Serializable]
    public sealed class VariableCondition : FlowCondition
    {
        [FlowVariableName] public string variableName;
        public FlowVariableValue expected = new();

        public override bool Evaluate(FlowContext context)
        {
            return !string.IsNullOrEmpty(variableName)
                && context.TryGet(variableName, out object raw)
                && expected.Matches(raw);
        }
    }
}
