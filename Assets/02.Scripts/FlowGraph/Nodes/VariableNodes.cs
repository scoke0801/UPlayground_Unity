using System;
using System.Collections;
using System.Collections.Generic;

namespace UPlayGround.FlowGraph
{
    /// <summary>그래프 블랙보드 변수에 값을 대입한다 (변수는 그래프 에셋의 Blackboard 패널에서 선언).</summary>
    [FlowNodeMenu("변수/SetVariable", Summary = "현재 실행 Blackboard 변수 값을 설정합니다.", Keywords = new[] { "variable", "blackboard", "set", "변수" })]
    [Serializable]
    public sealed class SetVariableNode : FlowNode
    {
        [FlowVariableName] public string variableName;
        [UnityEngine.HideInInspector] public string variableId;
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
            string resolvedName = token.Graph.ResolveVariableName(variableId, variableName);
            if (!string.IsNullOrEmpty(resolvedName))
                token.Context.Set(resolvedName, value.Get());
            token.Emit(FlowPort.Out);
            yield break;
        }
    }

    /// <summary>그래프 블랙보드 변수 값으로 True/False 분기.</summary>
    [FlowNodeMenu("변수/CheckVariable (Branch)", Summary = "Blackboard 변수 값을 비교해 분기합니다.", Keywords = new[] { "variable", "blackboard", "if", "compare" })]
    [Serializable]
    public sealed class CheckVariableNode : FlowNode
    {
        [FlowVariableName] public string variableName;
        [UnityEngine.HideInInspector] public string variableId;
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
            string resolvedName = token.Graph.ResolveVariableName(variableId, variableName);
            bool result = !string.IsNullOrEmpty(resolvedName)
                && token.Context.TryGet(resolvedName, out object raw)
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
        [UnityEngine.HideInInspector] public string variableId;
        public FlowVariableValue expected = new();

        public override bool Evaluate(FlowContext context)
        {
            string resolvedName = context.Graph?.ResolveVariableName(variableId, variableName)
                                  ?? variableName;
            return !string.IsNullOrEmpty(resolvedName)
                && context.TryGet(resolvedName, out object raw)
                && expected.Matches(raw);
        }
    }
}
