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

    /// <summary>위치 기반 진입 판정이 실패한 사유. 호출측 진단에만 사용한다.</summary>
    public enum FlowVolumeRouteFailure
    {
        None = 0,
        RoutingDisabled,
        ActorMissing,
        ActorFilterMismatch,
        ColliderMissing,
        OutsideVolume,
        EntryNotFired,
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
        [SerializeField] private Collider _volumeCollider;

        [Tooltip("0(None)이면 필터 없음. 지정 시 IWorldActor.ActorType과 겹치는 대상만 발화.")]
        [SerializeField] private ActorType _actorFilter = ActorType.Player;

        private readonly HashSet<Collider> _overlappingColliders = new();
        private bool _isRoutingEnabled = true;

        private void Awake()
        {
            ResolveVolumeCollider();
        }

        private void Reset()
        {
            _runner = GetComponentInParent<FlowGraphRunner>();
            _volumeCollider = GetComponent<Collider>();
            if (_volumeCollider != null)
                _volumeCollider.isTrigger = true;
        }

        private void OnDisable()
        {
            _overlappingColliders.Clear();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other == null || !_overlappingColliders.Add(other) || !_isRoutingEnabled)
                return;

            Route(other, FlowVolumePhase.Enter);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other == null || !_overlappingColliders.Remove(other) || !_isRoutingEnabled)
                return;

            Route(other, FlowVolumePhase.Exit);
        }

        /// <summary>외부 런타임이 준비될 때까지 진입을 보류하고, 준비 완료 시 현재 겹친 대상의 진입을 재생한다.</summary>
        public bool SetRoutingEnabled(bool isEnabled)
        {
            if (_isRoutingEnabled == isEnabled)
                return false;

            _isRoutingEnabled = isEnabled;
            if (!isEnabled)
                return false;

            bool fired = false;
            foreach (Collider overlappingCollider in _overlappingColliders)
            {
                if (overlappingCollider != null)
                    fired |= Route(overlappingCollider, FlowVolumePhase.Enter);
                if (!_isRoutingEnabled)
                    break;
            }
            return fired;
        }

        /// <summary>
        /// 물리 Trigger 콜백에 의존하지 않고 액터 위치만으로 같은 FlowGraph 진입점을 발화한다.
        /// KCC 액터는 트랜스폼을 직접 옮기고 볼륨 안에서 생성될 수도 있어 진입 콜백이 보장되지 않는다.
        /// </summary>
        public bool TryRouteActorIfInside(
            IWorldActor actor,
            out FlowVolumeRouteFailure failure,
            FlowVolumePhase phase = FlowVolumePhase.Enter)
        {
            Transform actorTransform = actor?.Transform;
            if (!_isRoutingEnabled)
                failure = FlowVolumeRouteFailure.RoutingDisabled;
            else if (actorTransform == null)
                failure = FlowVolumeRouteFailure.ActorMissing;
            else if (!MatchesActorFilter(actor))
                failure = FlowVolumeRouteFailure.ActorFilterMismatch;
            else if (ResolveVolumeCollider() == null)
                failure = FlowVolumeRouteFailure.ColliderMissing;
            else if (!ContainsWorldPoint(actorTransform.position))
                failure = FlowVolumeRouteFailure.OutsideVolume;
            else
            {
                failure = Route(null, actor, phase)
                    ? FlowVolumeRouteFailure.None
                    : FlowVolumeRouteFailure.EntryNotFired;
            }

            return failure == FlowVolumeRouteFailure.None;
        }

        /// <summary>
        /// 볼륨 콜라이더의 로컬 형상으로 포함 여부를 판정한다.
        /// <see cref="Collider.bounds"/>는 콜라이더가 비활성이거나 물리 씬에 등록되기 전이면
        /// 크기 0을 돌려주므로 진입 판정의 기준으로 쓰지 않는다.
        /// </summary>
        private bool ContainsWorldPoint(Vector3 worldPoint)
        {
            Vector3 localPoint = _volumeCollider.transform.InverseTransformPoint(worldPoint);
            switch (_volumeCollider)
            {
                case BoxCollider box:
                {
                    Vector3 offset = localPoint - box.center;
                    Vector3 extents = box.size * 0.5f;
                    return Mathf.Abs(offset.x) <= extents.x
                        && Mathf.Abs(offset.y) <= extents.y
                        && Mathf.Abs(offset.z) <= extents.z;
                }
                case SphereCollider sphere:
                    return (localPoint - sphere.center).sqrMagnitude <= sphere.radius * sphere.radius;
                default:
                    // 진입 볼륨은 Box/Sphere로 저작한다. 그 외 형상은 물리 씬에 의존하는 근사로만 판정한다.
                    return _volumeCollider.bounds.Contains(worldPoint);
            }
        }

        /// <summary>
        /// 콜라이더 참조를 지연 확보한다. GameObject가 비활성이면 <see cref="Awake"/>가 실행되지 않으므로
        /// 직렬화 값이 비어 있을 때 호출 시점에 다시 해석한다.
        /// </summary>
        private Collider ResolveVolumeCollider()
        {
            if (_volumeCollider == null)
                _volumeCollider = GetComponent<Collider>();
            return _volumeCollider;
        }

        private bool Route(Collider other, FlowVolumePhase phase)
        {
            IWorldActor actor = other != null ? other.GetComponentInParent<IWorldActor>() : null;
            if (!MatchesActorFilter(actor))
                return false;

            return Route(other, actor, phase);
        }

        private bool Route(Collider other, IWorldActor actor, FlowVolumePhase phase)
        {
            if (_runner == null || _runner.Graph == null)
            {
                Debug.LogError(
                    $"[FlowGraph] 볼륨 '{_volumeId}'에 유효한 Runner/Graph가 없어 진입을 처리할 수 없습니다.",
                    this);
                return false;
            }

            bool fired = _runner.FireEntries<OnTriggerVolumeEntryNode>(
                entry => entry.volumeId == _volumeId && entry.phase == phase,
                context =>
                {
                    context.Collider = other;
                    context.Actor = actor;
                });

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!fired)
            {
                Debug.LogWarning(
                    $"[FlowGraph] 볼륨 '{_volumeId}'의 {phase} 진입점이 발화되지 않았습니다. " +
                    "그래프 배선과 반복 정책을 확인하세요.",
                    this);
            }
#endif
            return fired;
        }

        private bool MatchesActorFilter(IWorldActor actor)
        {
            return _actorFilter == 0
                || (actor != null && (actor.ActorType & _actorFilter) != 0);
        }
    }
}
