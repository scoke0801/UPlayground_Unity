using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.FlowGraph;

namespace UPlayGround.TriggerSystem
{
    /// <summary>
    /// 기존 TriggerActionSO 에셋을 FlowGraph에서 재활용하는 범용 실행 노드.
    /// 전용 노드가 없는 액션(카메라 스냅샷, 그룹 활성화, 가이드 팝업 등)을 자산 재생성 없이 커버한다.
    /// 주의: Composer/Source 없이 발화되므로 TriggerContext의 해당 필드는 null이다 —
    /// Composer에 의존하는 액션은 전용 노드로 승격할 것.
    /// </summary>
    [FlowNodeMenu("트리거 브릿지/RunTriggerAction")]
    [Serializable]
    public sealed class RunTriggerActionNode : FlowNode
    {
        public TriggerActionSO action;

        public override string DisplayName =>
            action != null ? $"RunAction [{action.name}]" : "RunTriggerAction";

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
            if (action == null)
            {
                Debug.LogWarning("[FlowGraph] RunTriggerAction: 액션 미지정 — 통과");
                token.Emit(FlowPort.Out);
                yield break;
            }

            var triggerContext = new TriggerContext(null, null);
            if (token.Context.Collider != null && token.Context.Actor is GameActor gameActor)
                triggerContext.WithCollider(token.Context.Collider, gameActor);

            if (action.CanExecute(triggerContext))
                yield return action.Execute(triggerContext);

            token.Emit(FlowPort.Out);
        }
    }

    /// <summary>기존 TriggerConditionSO 에셋으로 True/False 분기하는 범용 노드.</summary>
    [FlowNodeMenu("트리거 브릿지/EvaluateTriggerCondition (Branch)")]
    [Serializable]
    public sealed class EvaluateTriggerConditionNode : FlowNode
    {
        public TriggerConditionSO condition;

        public override string DisplayName =>
            condition != null ? $"Condition [{condition.name}]" : "EvaluateTriggerCondition";

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
            var triggerContext = new TriggerContext(null, null);
            if (token.Context.Collider != null && token.Context.Actor is GameActor gameActor)
                triggerContext.WithCollider(token.Context.Collider, gameActor);

            bool result = condition != null && condition.Evaluate(triggerContext);
            token.Emit(result ? FlowPort.True : FlowPort.False);
            yield break;
        }
    }
}
