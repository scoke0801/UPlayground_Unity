using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    /// <summary>저장 플래그 일괄 복원 뒤 다른 서비스의 복원까지 마치고 흐름을 재개한다.</summary>
    [FlowNodeMenu("진입점/OnFlagsReloaded", Summary = "저장 불러오기와 새 게임의 플래그 일괄 교체 후 시작합니다.", Keywords = new[] { "save", "load", "복원" })]
    [Serializable]
    public sealed class OnFlagsReloadedEntryNode : EntryNode
    {
        [Tooltip("이전 저장에서 진행 중이던 전투·대화 대기를 취소한 뒤 복원 흐름을 시작합니다.")]
        public bool cancelRunningContexts;

        public override void Arm(FlowGraphRunner runner)
        {
            IGlobalFlagService flags = Svc.Flags;
            if (flags == null) return;
            Coroutine pending = null;
            IEnumerator Resume()
            {
                // ImportSaveData 디스패치 안에서는 퀘스트 서비스가 아직 이전 저장일 수 있다.
                yield return null;
                pending = null;
                if (runner.isActiveAndEnabled) runner.FireEntry(this);
            }
            Action handler = () =>
            {
                if (cancelRunningContexts) runner.CancelAll();
                if (pending != null) runner.StopCoroutine(pending);
                pending = runner.StartCoroutine(Resume());
            };
            flags.OnFlagsReloaded += handler;
            runner.StoreEntryTeardown(this, () =>
            {
                flags.OnFlagsReloaded -= handler;
                if (pending != null) runner.StopCoroutine(pending);
            });
        }
    }

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
                yield return FlowPortDef.Output(FlowPort.True, optional: true);
                yield return FlowPortDef.Output(FlowPort.False, optional: true);
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
