using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data;

namespace UPlayGround.CameraSystem
{
    /// <summary>
    /// 카메라→대상 시야선을 가리는 Fade 장애물을 찾아 디더로 흐리게 하고, 벗어나면 복원한다.
    /// 카메라 암 충돌에서 Fade 장애물을 제외하는 분류기 역할도 겸한다.
    /// 렌더링은 RendererFeature가 맡고 이 클래스는 상태(슬롯별 가시 단계)만 발행한다.
    /// </summary>
    public sealed class CameraOcclusionFader : ICameraObstacleClassifier, System.IDisposable
    {
        private const int QueryCapacity = 32;
        private const int CoverRayThreshold = 2;

        private static readonly Vector3[] CoverOffsets =
        {
            Vector3.zero, Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
        };

        private readonly CameraSettings _settings;
        private readonly LayerMask _queryLayers;
        private readonly OccluderClassifier _classifier;
        private readonly int _proxyLayer;
        private readonly RaycastHit[] _hits = new RaycastHit[QueryCapacity];
        private readonly Collider[] _overlaps = new Collider[QueryCapacity];
        private readonly HashSet<OccluderGroup> _desired = new();
        private readonly List<OccluderGroup> _active = new();
        private readonly List<OccluderGroup> _releaseBuffer = new();
        private Transform _ignoredRoot;
        private int _usedSlotMask;

        /// <summary>현재 흐려지고 있거나 복귀 중인 덩어리 수.</summary>
        public int ActiveGroupCount => _active.Count;

        /// <summary>프록시가 놓이는 레이어의 비트마스크. 메인 카메라 컬링 마스크에 포함돼야 한다.</summary>
        public int ProxyLayerMask => _proxyLayer >= 0 ? 1 << _proxyLayer : 0;

        private bool IsOperational =>
            _settings.enableOcclusionFade && _proxyLayer >= 0 && CameraOccluderFadeState.IsFeatureRegistered;

        /// <summary>페이드 판정에 필요한 설정과 질의 레이어를 받아 초기화한다.</summary>
        /// <param name="ignoredRoot">플레이어 등 판정에서 제외할 계층의 루트.</param>
        /// <param name="solidLayerMask">자동 판정에서 항상 Solid로 취급할 레이어.</param>
        /// <param name="proxyLayer">프록시를 놓을 물리 레이어. 정의되지 않았으면(-1) 페이드를 끄고 기존 충돌만 쓴다.</param>
        public CameraOcclusionFader(CameraSettings settings, Transform ignoredRoot, LayerMask queryLayers,
            int solidLayerMask, int proxyLayer)
        {
            _settings = settings;
            _ignoredRoot = ignoredRoot;
            _queryLayers = queryLayers;
            _proxyLayer = proxyLayer;
            _classifier = new OccluderClassifier(settings, solidLayerMask, proxyLayer);
            CameraOccluderFadeState.ProxyLayerMask = ProxyLayerMask;
        }

        /// <inheritdoc/>
        public bool IsBlocking(Collider collider)
        {
            if (!IsOperational) return true;
            return _classifier.Classify(collider).Kind == OccluderKind.Solid;
        }

        /// <summary>
        /// 이번 프레임의 시야선을 훑어 흐려져야 할 덩어리를 등록한다.
        /// 한 프레임에 여러 번 호출하면 결과가 합쳐진다.
        /// </summary>
        public void Probe(Vector3 cameraPosition, Vector3 aimPoint)
        {
            if (!IsOperational) return;

            float radius = Mathf.Max(0.05f, _settings.occlusionProbeRadius);
            CollectCameraOverlaps(cameraPosition, Mathf.Max(0.05f, _settings.cameraRadius));

            Vector3 toAim = aimPoint - cameraPosition;
            float distance = toAim.magnitude - Mathf.Max(0f, _settings.occlusionTargetMargin);
            if (distance <= 0.01f) return;

            int count = Physics.SphereCastNonAlloc(cameraPosition, radius, toAim.normalized, _hits,
                distance, _queryLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                RegisterDesired(_hits[i].collider);
        }

        /// <summary>
        /// 플레이어가 지붕 밖에 있는데 카메라 끝점이 지붕 아래(건물 내부)에 들어가면 암을 지붕 밖까지 줄인다.
        /// 벽을 통과해 건물 뒤의 열린 공간으로 나가는 것은 허용하고, 실내로 파고드는 것만 막는다.
        /// </summary>
        /// <returns>제한된 암 길이. 가드가 꺼져 있거나 제한이 필요 없으면 요청 길이 그대로.</returns>
        public float LimitInterior(Vector3 pivot, Vector3 direction, float desiredDistance)
        {
            if (!IsOperational || !_settings.enableOcclusionInteriorGuard || desiredDistance <= 0f)
                return desiredDistance;
            if (IsCovered(pivot)) return desiredDistance;

            float step = Mathf.Max(0.1f, _settings.occlusionInteriorStep);
            float minDistance = Mathf.Max(0f, _settings.minDistance);
            for (float distance = desiredDistance; distance > minDistance; distance -= step)
                if (!IsCovered(pivot + direction * distance)) return distance;
            return Mathf.Min(desiredDistance, minDistance);
        }

        /// <summary>등록된 시야선 결과로 가시율을 진행하고 슬롯 상태를 발행한다. 프레임당 한 번 호출한다.</summary>
        public void Advance(float deltaTime)
        {
            if (!IsOperational)
            {
                ReleaseAll();
                return;
            }

            deltaTime = Mathf.Max(0f, deltaTime);
            EngageDesiredGroups();
            StepActiveGroups(deltaTime);
            _desired.Clear();
            Publish();
        }

        /// <summary>모든 덩어리를 즉시 원상 복구하고 발행 상태를 비운다.</summary>
        public void ReleaseAll()
        {
            for (int i = 0; i < _active.Count; i++)
                _active[i].Release();
            _active.Clear();
            _desired.Clear();
            _usedSlotMask = 0;
            CameraOccluderFadeState.Clear();
        }

        /// <summary>프록시를 모두 제거하고 분류 캐시를 해제한다.</summary>
        public void Dispose()
        {
            ReleaseAll();
            _classifier.Clear();
            _ignoredRoot = null;
        }

        /// <summary>
        /// 지점 위쪽에 '지붕 제공'으로 표시된 콜라이더가 있으면 true. 사방 5개 레이 중 2개 이상이 맞아야 한다.
        /// 나무 캐노피처럼 표시되지 않은 콜라이더는 지붕으로 보지 않는다.
        /// </summary>
        private bool IsCovered(Vector3 point)
        {
            float height = Mathf.Max(1f, _settings.occlusionCoverCheckHeight);
            float spread = Mathf.Max(0f, _settings.occlusionCoverSpread);
            int coveredCount = 0;
            for (int i = 0; i < CoverOffsets.Length; i++)
            {
                Vector3 origin = point + CoverOffsets[i] * spread + Vector3.up * 0.1f;
                if (HasCeiling(origin, height)) coveredCount++;
            }
            return coveredCount >= CoverRayThreshold;
        }

        private bool HasCeiling(Vector3 origin, float height)
        {
            int count = Physics.RaycastNonAlloc(origin, Vector3.up, _hits, height,
                _queryLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider collider = _hits[i].collider;
                if (collider == null) continue;
                if (_ignoredRoot != null && (collider.transform == _ignoredRoot || collider.transform.IsChildOf(_ignoredRoot)))
                    continue;
                if (_classifier.Classify(collider).ProvidesCover) return true;
            }
            return false;
        }

        private void CollectCameraOverlaps(Vector3 cameraPosition, float radius)
        {
            // 카메라가 벽 내부에 있으면 SphereCast는 그 콜라이더를 보고하지 않는다.
            int count = Physics.OverlapSphereNonAlloc(cameraPosition, radius, _overlaps,
                _queryLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                RegisterDesired(_overlaps[i]);
        }

        private void RegisterDesired(Collider collider)
        {
            if (collider == null) return;
            if (_ignoredRoot != null && (collider.transform == _ignoredRoot || collider.transform.IsChildOf(_ignoredRoot)))
                return;

            OccluderClassifier.Entry entry = _classifier.Classify(collider);
            if (entry.Kind == OccluderKind.Fade && entry.Group != null)
                _desired.Add(entry.Group);
        }

        private void EngageDesiredGroups()
        {
            int maxGroups = Mathf.Clamp(_settings.occlusionMaxGroups, 1, CameraOccluderFadeState.MaxSlots);
            foreach (OccluderGroup group in _desired)
            {
                group.HoldTimer = Mathf.Max(0f, _settings.occlusionReleaseDelay);
                if (group.IsEngaged) continue;
                if (_active.Count >= maxGroups) continue;

                int slot = FindFreeSlot();
                if (slot < 0) continue;
                _usedSlotMask |= 1 << slot;
                group.Engage(slot, _proxyLayer);
                _active.Add(group);
            }
        }

        private int FindFreeSlot()
        {
            for (int slot = 0; slot < CameraOccluderFadeState.MaxSlots; slot++)
                if ((_usedSlotMask & (1 << slot)) == 0) return slot;
            return -1;
        }

        private void StepActiveGroups(float deltaTime)
        {
            float minVisibility = Mathf.Clamp(_settings.occlusionMinVisibility, 0f, 0.95f);
            float span = 1f - minVisibility;
            float fadeOutStep = span / Mathf.Max(0.01f, _settings.occlusionFadeOutTime) * deltaTime;
            float fadeInStep = span / Mathf.Max(0.01f, _settings.occlusionFadeInTime) * deltaTime;

            _releaseBuffer.Clear();
            for (int i = 0; i < _active.Count; i++)
            {
                OccluderGroup group = _active[i];
                group.SyncRenderers();
                bool isWanted = _desired.Contains(group);
                if (!isWanted) group.HoldTimer -= deltaTime;

                bool shouldHide = isWanted || group.HoldTimer > 0f;
                group.Visibility = shouldHide
                    ? Mathf.MoveTowards(group.Visibility, minVisibility, fadeOutStep)
                    : Mathf.MoveTowards(group.Visibility, 1f, fadeInStep);

                if (!shouldHide && group.Visibility >= 0.999f)
                    _releaseBuffer.Add(group);
            }

            for (int i = 0; i < _releaseBuffer.Count; i++)
            {
                OccluderGroup group = _releaseBuffer[i];
                _usedSlotMask &= ~(1 << group.Slot);
                group.Release();
                _active.Remove(group);
            }
        }

        private void Publish()
        {
            CameraOccluderFadeState.BeginPublish(_settings.occlusionDitherPixelScale);
            for (int i = 0; i < _active.Count; i++)
            {
                OccluderGroup group = _active[i];
                int level = Mathf.RoundToInt(group.Visibility * CameraOccluderFadeState.Levels);
                CameraOccluderFadeState.SetSlot(group.Slot, level);
            }
        }
    }
}
