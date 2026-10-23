using System;

namespace UPlayGround.FlowGraph
{
    /// <summary>공유 그래프의 현장 효과를 러너가 배치된 씬으로 제한한다.</summary>
    [Serializable]
    public sealed class RunnerSceneCondition : FlowCondition
    {
        public string sceneName;

        public override bool Evaluate(FlowContext context) =>
            context?.Runner != null && !string.IsNullOrWhiteSpace(sceneName)
            && string.Equals(context.Runner.gameObject.scene.name, sceneName, StringComparison.Ordinal);
    }
}
