using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.FlowGraph
{
    /// <summary>TriggerRepeatPolicy 개념을 진입점 노드에 계승한 재진입 정책.</summary>
    public enum FlowRepeatPolicy
    {
        /// <summary>러너 인스턴스 수명 동안 1회 (씬 재로드 시 리셋).</summary>
        Once = 0,
        /// <summary>플레이 세션 동안 1회 (그래프ID+노드ID 기준, 씬 재로드에도 유지).</summary>
        OncePerSession = 1,
        /// <summary>cooldownSeconds 간격 제한.</summary>
        Cooldown = 2,
        Always = 3,
    }

    /// <summary>
    /// 그래프 실행 진입점 베이스. 발화 상태(발화 횟수/시각)는 에셋 공유 문제를 피하기 위해
    /// 노드가 아니라 FlowGraphRunner가 노드 id 기준으로 소유한다.
    /// </summary>
    [System.Serializable]
    public abstract class EntryNode : FlowNode
    {
        [Tooltip("Manual 발화/디버그 식별용 ID. 비워도 된다.")]
        public string entryId;

        public FlowRepeatPolicy repeatPolicy = FlowRepeatPolicy.Always;

        [Tooltip("repeatPolicy가 Cooldown일 때의 최소 발화 간격(초).")]
        public float cooldownSeconds = 1f;

        public override IEnumerable<FlowPortDef> Ports
        {
            get { yield return FlowPortDef.Output(); }
        }

        public sealed override IEnumerator Execute(FlowToken token)
        {
            token.Emit(FlowPort.Out);
            yield break;
        }

        /// <summary>외부 신호 구독 등 발화 준비. 해제 동작은 runner.StoreEntryTeardown으로 맡긴다.</summary>
        public virtual void Arm(FlowGraphRunner runner)
        {
        }
    }

    /// <summary>코드/치트/트리거 볼륨 등 외부 API로만 발화되는 진입점.</summary>
    [FlowNodeMenu("진입점/Manual")]
    [System.Serializable]
    public sealed class ManualEntryNode : EntryNode
    {
        public override string DisplayName => string.IsNullOrEmpty(entryId) ? "Entry (Manual)" : $"Entry: {entryId}";
    }
}
