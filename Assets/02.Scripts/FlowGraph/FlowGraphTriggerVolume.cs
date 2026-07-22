using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;

namespace UPlayGround.FlowGraph
{
    public enum FlowVolumePhase
    {
        Enter = 0,
        Exit = 1,
    }

    /// <summary>씬 콜라이더 볼륨 진입/이탈에 반응하는 진입점. FlowGraphTriggerVolume 프록시가 발화한다.</summary>
    [FlowNodeMenu("진입점/OnTriggerVolume")]
    [Serializable]
    public sealed class OnTriggerVolumeEntryNode : EntryNode
    {
        [Tooltip("씬의 FlowGraphTriggerVolume.volumeId와 매칭.")]
        public string volumeId;

        public FlowVolumePhase phase = FlowVolumePhase.Enter;

        public override string DisplayName => $"Entry: Volume [{volumeId}] {phase}";
    }

    /// <summary>
    /// 씬 콜라이더 이벤트를 그래프 진입점(OnTriggerVolumeEntryNode)에 라우팅하는 프록시.
    /// 그래프 에셋이 씬 참조를 직접 갖지 않게 하는 씬 바인딩 지점 (TriggerComposer와 동일 원리).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class FlowGraphTriggerVolume : MonoBehaviour
    {
        [SerializeField] private FlowGraphRunner _runner;
        [SerializeField] private string _volumeId;

        [Tooltip("0(None)이면 필터 없음. 지정 시 IWorldActor.ActorType과 겹치는 대상만 발화.")]
        [SerializeField] private ActorType _actorFilter = ActorType.Player;

        private void Reset()
        {
            _runner = GetComponentInParent<FlowGraphRunner>();
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other) => Route(other, FlowVolumePhase.Enter);
        private void OnTriggerExit(Collider other) => Route(other, FlowVolumePhase.Exit);

        private void Route(Collider other, FlowVolumePhase phase)
        {
            if (_runner == null || _runner.Graph == null)
                return;

            IWorldActor actor = other.GetComponentInParent<IWorldActor>();
            if (_actorFilter != 0 && (actor == null || (actor.ActorType & _actorFilter) == 0))
                return;

            _runner.FireEntries<OnTriggerVolumeEntryNode>(
                entry => entry.volumeId == _volumeId && entry.phase == phase,
                context =>
                {
                    context.Collider = other;
                    context.Actor = actor;
                });
        }
    }
}
