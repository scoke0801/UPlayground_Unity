using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.FlowGraph
{
    /// <summary>출력 포트들("1".."N")로 토큰을 순서대로 즉시 방출한다. 하나의 Out에 다중 연결해도 병렬 방출이 되므로, 순서가 중요할 때 사용.</summary>
    [FlowNodeMenu("코어/Sequence")]
    [Serializable]
    public sealed class SequenceNode : FlowNode
    {
        [Range(2, 8)] public int outputCount = 2;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                for (int i = 1; i <= outputCount; i++)
                    yield return FlowPortDef.Output(i.ToString());
            }
        }

        public override IEnumerator Execute(FlowToken token)
        {
            for (int i = 1; i <= outputCount; i++)
                token.Emit(i.ToString());
            yield break;
        }
    }

    /// <summary>조건 평가 후 True/False 포트 중 하나로 분기.</summary>
    [FlowNodeMenu("코어/Branch")]
    [Serializable]
    public sealed class BranchNode : FlowNode
    {
        [SerializeReference] public FlowCondition condition;

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
            bool result = condition != null && condition.Evaluate(token.Context);
            token.Emit(result ? FlowPort.True : FlowPort.False);
            yield break;
        }
    }

    /// <summary>WaitTimeNode의 대기 진행 상태 — 에디터가 TryPeekNodeState로 읽어 진행 바를 그린다.</summary>
    public sealed class WaitTimeProgressState
    {
        public float StartTime;
        public float Duration;
        public bool Unscaled;

        public float Progress01
        {
            get
            {
                if (Duration <= 0f)
                    return 1f;
                float now = Unscaled ? Time.unscaledTime : Time.time;
                return Mathf.Clamp01((now - StartTime) / Duration);
            }
        }
    }

    /// <summary>지정 시간만큼 토큰을 보류한 뒤 통과.</summary>
    [FlowNodeMenu("코어/Wait (Time)")]
    [Serializable]
    public sealed class WaitTimeNode : FlowNode
    {
        [Min(0f)] public float seconds = 1f;
        public bool useUnscaledTime;

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
            // WaitForSeconds 대신 수동 루프 — 에디터 진행 바가 상태를 읽을 수 있게 한다.
            WaitTimeProgressState state = token.Context.GetNodeState<WaitTimeProgressState>(this);
            state.Duration = seconds;
            state.Unscaled = useUnscaledTime;
            state.StartTime = useUnscaledTime ? Time.unscaledTime : Time.time;

            float end = state.StartTime + seconds;
            while ((useUnscaledTime ? Time.unscaledTime : Time.time) < end)
                yield return null;

            token.Emit(FlowPort.Out);
        }
    }

    /// <summary>조건이 충족될 때까지 폴링 대기 후 통과.</summary>
    [FlowNodeMenu("코어/Wait (Condition)")]
    [Serializable]
    public sealed class WaitConditionNode : FlowNode
    {
        [SerializeReference] public FlowCondition condition;

        [Tooltip("조건 폴링 간격(초). 0이면 매 프레임.")]
        [Min(0f)] public float pollInterval = 0.1f;

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
            if (condition == null)
            {
                token.Emit(FlowPort.Out);
                yield break;
            }

            while (!condition.Evaluate(token.Context))
            {
                if (pollInterval > 0f)
                    yield return new WaitForSeconds(pollInterval);
                else
                    yield return null;
            }
            token.Emit(FlowPort.Out);
        }
    }

    public enum FlowJoinMode
    {
        /// <summary>들어오는 연결 수만큼 토큰이 모두 도착하면 1회 통과.</summary>
        All = 0,
        /// <summary>첫 토큰 도착 시 통과, 나머지는 소멸.</summary>
        Any = 1,
    }

    /// <summary>Parallel(다중 방출)로 갈라진 토큰을 합류시킨다. 같은 FlowContext 내에서만 합류한다.</summary>
    [FlowNodeMenu("코어/Join")]
    [Serializable]
    public sealed class JoinNode : FlowNode
    {
        public FlowJoinMode mode = FlowJoinMode.All;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
            }
        }

        private sealed class JoinState
        {
            public int Arrivals;
            public bool Fired;
        }

        public override IEnumerator Execute(FlowToken token)
        {
            JoinState state = token.Context.GetNodeState<JoinState>(this);
            state.Arrivals++;

            switch (mode)
            {
                case FlowJoinMode.Any:
                    if (!state.Fired)
                    {
                        state.Fired = true;
                        token.Emit(FlowPort.Out);
                    }
                    break;

                case FlowJoinMode.All:
                    // SubGraph 중첩 실행에서도 올바르게 세도록 러너 루트가 아닌 토큰의 그래프를 본다.
                    int expected = token.Graph.CountConnectionsTo(id);
                    if (state.Arrivals >= Mathf.Max(1, expected))
                    {
                        state.Arrivals = 0;
                        token.Emit(FlowPort.Out);
                    }
                    break;
            }
            yield break;
        }
    }

    /// <summary>재진입 정책 게이트. 진입점이 아닌 그래프 중간에서 통과 횟수를 제한한다. (컨텍스트 단위가 아닌 러너 수명 단위)</summary>
    [FlowNodeMenu("코어/Gate (RepeatPolicy)")]
    [Serializable]
    public sealed class GateNode : FlowNode
    {
        public FlowRepeatPolicy policy = FlowRepeatPolicy.Once;
        [Min(0f)] public float cooldownSeconds = 1f;

        public override IEnumerable<FlowPortDef> Ports
        {
            get
            {
                yield return FlowPortDef.Input();
                yield return FlowPortDef.Output();
            }
        }

        // 러너 수명 게이트 상태 — 러너별로 격리돼야 하므로 (runnerInstanceId, nodeId) 키의 세션 저장소를 쓴다.
        public override IEnumerator Execute(FlowToken token)
        {
            var runner = token.Context.Runner;
            string key = $"gate:{runner.GetInstanceID()}:{token.Graph.GetInstanceID()}:{id}";

            switch (policy)
            {
                case FlowRepeatPolicy.Once:
                case FlowRepeatPolicy.OncePerSession:
                    if (!FlowSessionState.TryMarkFired(policy == FlowRepeatPolicy.Once
                            ? key
                            : $"gate:{token.Graph.ResolvedGraphId}:{id}"))
                        yield break;
                    break;

                case FlowRepeatPolicy.Cooldown:
                    // 쿨다운은 발화(컨텍스트)를 넘어 유지돼야 하므로 러너 스코프 상태를 쓴다.
                    var state = runner.GetRunnerNodeState<GateCooldownState>(token.Graph, this);
                    if (Time.time - state.LastPassTime < cooldownSeconds)
                        yield break;
                    state.LastPassTime = Time.time;
                    break;
            }
            token.Emit(FlowPort.Out);
        }

        private sealed class GateCooldownState
        {
            public float LastPassTime = float.NegativeInfinity;
        }
    }

    /// <summary>디버그/테스트용 로그 출력.</summary>
    [FlowNodeMenu("코어/Log")]
    [Serializable]
    public sealed class LogNode : FlowNode
    {
        [TextArea] public string message;

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
            Debug.Log($"[FlowGraph] {message}");
            token.Emit(FlowPort.Out);
            yield break;
        }
    }
}
