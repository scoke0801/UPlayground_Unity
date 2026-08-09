using System;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UPlayGround.Data.Cycle;
using UPlayGround.World.Generation;

namespace UPlayGround.Cycle
{
    /// <summary>
    /// 프리팹에 직렬화된 KCC 캡슐과 안정 지면 한계를 배치 검증용 값으로 복사한 프로필.
    /// 배치 시점에 액터를 미리 생성하지 않고도 실제 충돌 크기를 검사하기 위해 사용한다.
    /// </summary>
    internal readonly struct KccPlacementProfile
    {
        public KccPlacementProfile(
            float radius,
            float height,
            float yOffset,
            float maxSlopeAngle,
            float maxStepHeight)
        {
            Radius = Mathf.Max(0.01f, radius);
            Height = Mathf.Max(Radius * 2f, height);
            YOffset = yOffset;
            MaxSlopeAngle = Mathf.Clamp(maxSlopeAngle, 0f, 89f);
            MaxStepHeight = Mathf.Max(0f, maxStepHeight);
        }

        public float Radius { get; }
        public float Height { get; }
        public float YOffset { get; }
        public float MaxSlopeAngle { get; }
        public float MaxStepHeight { get; }

        public static bool TryCreateFromPrefab(
            GameObject prefab,
            out KccPlacementProfile profile,
            out string error)
        {
            profile = default;
            error = null;
            if (prefab == null)
            {
                error = "KCC 배치 프로필을 읽을 프리팹이 없습니다.";
                return false;
            }

            KinematicCharacterMotor motor = prefab.GetComponentInChildren<KinematicCharacterMotor>(true);
            if (motor == null)
            {
                error = $"'{prefab.name}' 프리팹에 KinematicCharacterMotor가 없습니다.";
                return false;
            }

            CapsuleCollider capsule = motor.Capsule != null
                ? motor.Capsule
                : motor.GetComponent<CapsuleCollider>();
            if (capsule == null)
            {
                error = $"'{prefab.name}' 프리팹의 KinematicCharacterMotor에 CapsuleCollider가 없습니다.";
                return false;
            }

            if (capsule.direction != 1)
            {
                error = $"'{prefab.name}' 프리팹의 KCC 캡슐이 Y축 방향이 아닙니다.";
                return false;
            }

            Vector3 scale = capsule.transform.lossyScale;
            float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float heightScale = Mathf.Abs(scale.y);
            Vector3 worldCenter = capsule.transform.TransformPoint(capsule.center);
            float rootY = prefab.transform.position.y;
            profile = new KccPlacementProfile(
                capsule.radius * radiusScale,
                capsule.height * heightScale,
                worldCenter.y - rootY,
                motor.MaxStableSlopeAngle,
                motor.MaxStepHeight);
            return true;
        }
    }

    internal readonly struct CycleGroundSample
    {
        public CycleGroundSample(Vector3 position, Vector3 normal, TerrainCollider surface)
        {
            Position = position;
            Normal = normal;
            Surface = surface;
        }

        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public TerrainCollider Surface { get; }
    }

    /// <summary>
    /// XZ 경로를 Physics 지면에 투영한 연속 경로.
    /// Evaluate의 인자는 표본 인덱스가 아니라 누적 경로 길이의 정규화 값이다.
    /// </summary>
    internal sealed class CycleGroundRoute
    {
        private readonly CycleGroundSample[] _samples;
        private readonly float[] _cumulativeDistances;

        internal CycleGroundRoute(IReadOnlyList<CycleGroundSample> samples)
        {
            if (samples == null || samples.Count == 0)
                throw new ArgumentException("지면 경로 표본이 비어 있습니다.", nameof(samples));

            _samples = new CycleGroundSample[samples.Count];
            _cumulativeDistances = new float[samples.Count];
            for (int i = 0; i < samples.Count; i++)
            {
                _samples[i] = samples[i];
                if (i > 0)
                {
                    _cumulativeDistances[i] = _cumulativeDistances[i - 1] +
                                              Vector3.Distance(_samples[i - 1].Position, _samples[i].Position);
                }
            }
        }

        public int SampleCount => _samples.Length;
        public float Length => _cumulativeDistances[^1];
        public IReadOnlyList<CycleGroundSample> Samples => _samples;

        public Vector3 Evaluate(float normalizedDistance)
        {
            if (_samples.Length == 1 || Length <= 0.0001f)
                return _samples[0].Position;

            float target = Mathf.Clamp01(normalizedDistance) * Length;
            int upper = Array.BinarySearch(_cumulativeDistances, target);
            if (upper >= 0) return _samples[upper].Position;

            upper = ~upper;
            if (upper <= 0) return _samples[0].Position;
            if (upper >= _samples.Length) return _samples[^1].Position;

            int lower = upper - 1;
            float segmentLength = _cumulativeDistances[upper] - _cumulativeDistances[lower];
            float t = segmentLength <= 0.0001f
                ? 0f
                : (target - _cumulativeDistances[lower]) / segmentLength;
            return Vector3.Lerp(_samples[lower].Position, _samples[upper].Position, t);
        }
    }

    internal readonly struct CycleGroundMemberRequest
    {
        public CycleGroundMemberRequest(string stableId, Vector3 localOffset, KccPlacementProfile profile)
        {
            StableId = stableId;
            LocalOffset = localOffset;
            Profile = profile;
        }

        public string StableId { get; }
        public Vector3 LocalOffset { get; }
        public KccPlacementProfile Profile { get; }
    }

    internal sealed class CycleGroundGroupRequest
    {
        public string stableId;
        public Vector3 desiredAnchor;
        public CycleGroundRoute route;
        public float routeProgress;
        public IReadOnlyList<CycleGroundMemberRequest> members;
        public IReadOnlyList<Vector3> bossPositions;
        public bool allowRelocation = true;
    }

    internal readonly struct CycleGroundMemberPlacement
    {
        public CycleGroundMemberPlacement(string stableId, Vector3 position, KccPlacementProfile profile)
        {
            StableId = stableId;
            Position = position;
            Profile = profile;
        }

        public string StableId { get; }
        public Vector3 Position { get; }
        public KccPlacementProfile Profile { get; }
    }

    internal sealed class CycleGroundGroupPlacement
    {
        public CycleGroundGroupPlacement(
            Vector3 anchorPosition,
            IReadOnlyList<CycleGroundMemberPlacement> members)
        {
            AnchorPosition = anchorPosition;
            Members = members;
        }

        public Vector3 AnchorPosition { get; }
        public IReadOnlyList<CycleGroundMemberPlacement> Members { get; }
    }

    /// <summary>
    /// NavMesh에 의존하지 않는 사이클 자동 콘텐츠 배치 검증기.
    /// 최초 생성은 안정적인 ring 후보를 탐색할 수 있지만 복원 검증은 저장 좌표를 변경하지 않는다.
    /// </summary>
    internal sealed class CycleGroundPlacementResolver
    {
        private const float MinProbeDistance = 0.01f;
        private const float MinSampleSpacing = 0.1f;
        private const float CapsuleSkin = 0.02f;
        private const float SurfaceTieTolerance = 0.01f;
        private const float ExactGroundTolerance = 0.1f;
        private const float ContinuousSlopeTolerance = 2f;
        private const float TreeObstacleCellSize = 8f;
        private const uint FnvOffsetBasis = 2166136261u;
        private const uint FnvPrime = 16777619u;
        private const float GoldenAngle = 2.39996323f;

        private readonly CycleWorldAutoGenerationSettings _settings;
        private readonly Dictionary<TerrainCollider, TerrainTreeObstacleCache> _terrainTreeObstacleCaches = new();

        public CycleGroundPlacementResolver(CycleWorldAutoGenerationSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// 직선 경로를 먼저 검사하고, 막힌 경우 제한된 측면 격자에서 결정론적 우회 경로를 찾는다.
        /// 모든 간선은 직선 경로와 동일한 Ground/KCC 검사를 통과해야 한다.
        /// </summary>
        public bool TryBuildRoute(
            Vector3 startMarkerPosition,
            Vector3 endMarkerPosition,
            out CycleGroundRoute route,
            out string error)
        {
            route = null;
            if (!TrySampleRouteAnchor(startMarkerPosition, out CycleGroundSample startGround, out string startError))
            {
                error = $"경로 시작 앵커의 지면이 유효하지 않습니다: {startError}";
                return false;
            }

            if (!TrySampleRouteAnchor(endMarkerPosition, out CycleGroundSample endGround, out string endError))
            {
                error = $"경로 도착 앵커의 지면이 유효하지 않습니다: {endError}";
                return false;
            }

            KccPlacementProfile routeProfile = new(
                _settings.routeClearanceRadius,
                _settings.routeClearanceHeight,
                Mathf.Max(_settings.routeClearanceRadius, _settings.routeClearanceHeight * 0.5f),
                _settings.maxGroundSlopeAngle,
                _settings.maxGroundStepHeight);
            if (TryBuildGroundChain(startGround.Position, endGround.Position, routeProfile, out route, out error))
                return true;

            string directError = error;
            if (TryBuildDetourRoute(
                    startGround,
                    endGround,
                    routeProfile,
                    out route,
                    out string detourError))
            {
                error = null;
                return true;
            }

            error = $"직선 경로 실패: {directError} 우회 경로 실패: {detourError}";
            return false;
        }

        private bool TryBuildDetourRoute(
            CycleGroundSample startGround,
            CycleGroundSample endGround,
            KccPlacementProfile profile,
            out CycleGroundRoute route,
            out string error)
        {
            route = null;
            error = null;
            Vector3 start = startGround.Position;
            Vector3 end = endGround.Position;
            Vector3 horizontal = end - start;
            horizontal.y = 0f;
            float distance = horizontal.magnitude;
            if (distance <= MinSampleSpacing)
            {
                error = "시작점과 도착점의 수평 거리가 너무 짧아 우회할 수 없습니다.";
                return false;
            }

            float step = Mathf.Max(MinSampleSpacing, _settings.routeDetourStep);
            int longitudinalSegmentCount = Mathf.Max(1, Mathf.CeilToInt(distance / step));
            int lateralStepCount = Mathf.FloorToInt(Mathf.Max(0f, _settings.routeDetourMaxOffset) / step);
            if (lateralStepCount <= 0)
            {
                error = "routeDetourMaxOffset이 routeDetourStep보다 작아 측면 우회 후보가 없습니다.";
                return false;
            }

            Vector3 forward = horizontal / distance;
            Vector3 lateralDirection = Vector3.Cross(Vector3.up, forward);
            Dictionary<CycleRouteLatticeEdge, CycleGroundRoute> validEdges = new();
            Dictionary<CycleRouteLatticeNode, CycleGroundSample> nodeGround = new()
            {
                [new CycleRouteLatticeNode(0, 0)] = startGround,
                [new CycleRouteLatticeNode(longitudinalSegmentCount, 0)] = endGround,
            };
            string lastEdgeError = null;
            int testedEdgeCount = 0;

            bool CanTraverse(CycleRouteLatticeEdge edge)
            {
                testedEdgeCount++;
                Vector3 edgeStart = ResolveLatticePosition(
                    start,
                    end,
                    lateralDirection,
                    step,
                    longitudinalSegmentCount,
                    edge.From);
                if (nodeGround.TryGetValue(edge.From, out CycleGroundSample resolvedStartGround))
                    edgeStart.y = resolvedStartGround.Position.y;

                Vector3 edgeEnd = ResolveLatticePosition(
                    start,
                    end,
                    lateralDirection,
                    step,
                    longitudinalSegmentCount,
                    edge.To);
                if (nodeGround.TryGetValue(edge.To, out CycleGroundSample resolvedEndGround))
                    edgeEnd.y = resolvedEndGround.Position.y;
                else
                    edgeEnd.y = edgeStart.y;

                if (!TryBuildGroundChain(edgeStart, edgeEnd, profile, out CycleGroundRoute edgeRoute, out string edgeError))
                {
                    lastEdgeError = $"{edge.From.LongitudinalIndex}/{edge.From.LateralIndex}→" +
                                    $"{edge.To.LongitudinalIndex}/{edge.To.LateralIndex}: {edgeError}";
                    return false;
                }

                validEdges.Add(edge, edgeRoute);
                if (!nodeGround.ContainsKey(edge.To))
                    nodeGround.Add(edge.To, edgeRoute.Samples[^1]);
                return true;
            }

            if (!CycleRouteLatticePlanner.TryBuild(
                    longitudinalSegmentCount,
                    lateralStepCount,
                    CanTraverse,
                    out IReadOnlyList<CycleRouteLatticeNode> latticePath,
                    out string planError))
            {
                error = $"{planError} 검사 간선 {testedEdgeCount}개, 마지막 실패: {lastEdgeError ?? "없음"}";
                return false;
            }

            List<CycleGroundSample> samples = new();
            for (int i = 1; i < latticePath.Count; i++)
            {
                CycleRouteLatticeEdge edge = new(latticePath[i - 1], latticePath[i]);
                if (!validEdges.TryGetValue(edge, out CycleGroundRoute edgeRoute))
                {
                    error = $"선택된 우회 경로 간선의 지면 표본이 없습니다: " +
                            $"{edge.From.LongitudinalIndex}/{edge.From.LateralIndex}→" +
                            $"{edge.To.LongitudinalIndex}/{edge.To.LateralIndex}";
                    return false;
                }

                int sampleStartIndex = samples.Count == 0 ? 0 : 1;
                for (int sampleIndex = sampleStartIndex; sampleIndex < edgeRoute.Samples.Count; sampleIndex++)
                    samples.Add(edgeRoute.Samples[sampleIndex]);
            }

            if (samples.Count == 0)
            {
                error = "우회 경로에서 결합할 지면 표본이 없습니다.";
                return false;
            }

            route = new CycleGroundRoute(samples);
            return true;
        }

        private static Vector3 ResolveLatticePosition(
            Vector3 start,
            Vector3 end,
            Vector3 lateralDirection,
            float lateralStep,
            int longitudinalSegmentCount,
            CycleRouteLatticeNode node)
        {
            float progress = node.LongitudinalIndex / (float)longitudinalSegmentCount;
            return Vector3.Lerp(start, end, progress) +
                   lateralDirection * (node.LateralIndex * lateralStep);
        }

        /// <summary>
        /// 논리 앵커와 그 상대 위치에 있는 모든 멤버를 한 후보 단위로 검증한다.
        /// 한 멤버라도 실패하면 후보 전체를 버리며 부분 결과를 반환하지 않는다.
        /// </summary>
        public bool TryResolveGroup(
            CycleGroundGroupRequest request,
            out CycleGroundGroupPlacement placement,
            out string error)
        {
            placement = null;
            error = null;
            if (request == null)
            {
                error = "지면 그룹 배치 요청이 없습니다.";
                return false;
            }

            if (request.members == null || request.members.Count == 0)
            {
                error = $"그룹 '{request.stableId}'에 배치할 멤버가 없습니다.";
                return false;
            }

            string lastError = null;
            foreach (Vector3 anchorCandidate in EnumerateCandidates(
                         request.stableId,
                         request.desiredAnchor,
                         request.allowRelocation))
            {
                if (TryEvaluateGroupCandidate(request, anchorCandidate, out placement, out lastError))
                {
                    // 복원은 표면을 다시 조사하더라도 결과 좌표를 절대 보정하지 않는다.
                    if (!request.allowRelocation && placement.AnchorPosition != request.desiredAnchor)
                    {
                        placement = null;
                        error = $"그룹 '{request.stableId}'의 exact 복원 검증이 좌표를 변경했습니다.";
                        return false;
                    }

                    error = null;
                    return true;
                }
            }

            error = request.allowRelocation
                ? $"그룹 '{request.stableId}'을 {_settings.placementSearchRadius:0.##}m 탐색 반경 안에 배치하지 못했습니다. 마지막 오류: {lastError}"
                : $"그룹 '{request.stableId}'의 저장 좌표가 더 이상 유효하지 않습니다. {lastError}";
            return false;
        }

        /// <summary>
        /// 루팅/상호작용처럼 단일 캡슐로 표현 가능한 콘텐츠를 배치한다.
        /// allowRelocation=false이면 성공해도 desiredPosition을 그대로 반환한다.
        /// </summary>
        public bool TryResolvePoint(
            string stableId,
            Vector3 desiredPosition,
            KccPlacementProfile profile,
            CycleGroundRoute route,
            float routeProgress,
            IReadOnlyList<Vector3> bossPositions,
            bool allowRelocation,
            out Vector3 resolvedPosition,
            out string error)
        {
            CycleGroundGroupRequest request = new()
            {
                stableId = stableId,
                desiredAnchor = desiredPosition,
                route = route,
                routeProgress = routeProgress,
                bossPositions = bossPositions,
                allowRelocation = allowRelocation,
                members = new[]
                {
                    new CycleGroundMemberRequest(stableId, Vector3.zero, profile),
                },
            };

            if (TryResolveGroup(request, out CycleGroundGroupPlacement placement, out error))
            {
                resolvedPosition = allowRelocation ? placement.Members[0].Position : desiredPosition;
                return true;
            }

            resolvedPosition = default;
            return false;
        }

        /// <summary>
        /// 자동 생성 콘텐츠의 배치 후보를 검사한다.
        /// 설정된 최대 수직 투영 거리를 넘는 좌표는 저작/계획 오류로 간주해 거부한다.
        /// </summary>
        public bool TrySampleGround(Vector3 desiredPosition, out CycleGroundSample sample, out string error)
        {
            return TrySampleGroundInternal(
                desiredPosition,
                enforceProjectionDistance: true,
                out sample,
                out error);
        }

        /// <summary>
        /// CycleSpawnPoint 같은 경로 앵커는 XZ 목표를 소유하고 Y는 액터 스폰 피벗일 수 있다.
        /// 따라서 배치 후보용 투영 거리 제한은 적용하지 않고, 설정된 probe 범위 안에서
        /// 허용 Terrain 표면·경사만 검증해 경로용 지면 표본으로 변환한다.
        /// </summary>
        private bool TrySampleRouteAnchor(Vector3 markerPosition, out CycleGroundSample sample, out string error)
        {
            return TrySampleGroundInternal(
                markerPosition,
                enforceProjectionDistance: false,
                out sample,
                out error);
        }

        /// <summary>
        /// 이미 유효한 지면 표본에서 이어지는 경로 전용 검사다.
        /// 절대 마커 Y와의 거리는 비교하지 않고 표면 종류·경사·연속성 검사는 그대로 유지한다.
        /// </summary>
        private bool TrySampleRouteGround(Vector3 desiredPosition, out CycleGroundSample sample, out string error)
        {
            return TrySampleGroundInternal(
                desiredPosition,
                enforceProjectionDistance: false,
                out sample,
                out error);
        }

        private bool TrySampleGroundInternal(
            Vector3 desiredPosition,
            bool enforceProjectionDistance,
            out CycleGroundSample sample,
            out string error)
        {
            sample = default;
            error = null;
            if (_settings.placementSurfaceLayers.value == 0)
            {
                error = "placementSurfaceLayers가 비어 있어 물/장애물을 포함한 최상단 표면을 판정할 수 없습니다.";
                return false;
            }

            if (_settings.placementGroundLayers.value == 0)
            {
                error = "placementGroundLayers가 비어 있습니다.";
                return false;
            }

            float upDistance = Mathf.Max(MinProbeDistance, _settings.groundProbeUpDistance);
            float downDistance = Mathf.Max(MinProbeDistance, _settings.groundProbeDownDistance);
            Vector3 origin = desiredPosition + Vector3.up * upDistance;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                upDistance + downDistance,
                _settings.placementSurfaceLayers,
                QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
            {
                error = $"({desiredPosition.x:0.##}, {desiredPosition.z:0.##})에서 배치 표면을 찾지 못했습니다.";
                return false;
            }

            RaycastHit top = hits[0];
            for (int i = 1; i < hits.Length; i++)
            {
                if (hits[i].distance < top.distance)
                    top = hits[i];
            }

            if (top.collider is not TerrainCollider terrain ||
                !ContainsLayer(_settings.placementGroundLayers, top.collider.gameObject.layer))
            {
                string colliderName = top.collider != null ? top.collider.name : "<없음>";
                int layer = top.collider != null ? top.collider.gameObject.layer : -1;
                error = $"최상단 표면 '{colliderName}'(layer {layer})이 허용된 Ground TerrainCollider가 아닙니다.";
                return false;
            }

            // RaycastAll의 반환 순서는 보장되지 않는다. 물 proxy와 Terrain이 사실상 같은
            // 높이에 있을 때 우연히 Terrain이 먼저 왔다는 이유로 수면을 승인하지 않는다.
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit tiedHit = hits[i];
                if (tiedHit.distance > top.distance + SurfaceTieTolerance) continue;
                if (tiedHit.collider is TerrainCollider &&
                    ContainsLayer(_settings.placementGroundLayers, tiedHit.collider.gameObject.layer))
                {
                    continue;
                }

                string tiedName = tiedHit.collider != null ? tiedHit.collider.name : "<없음>";
                int tiedLayer = tiedHit.collider != null ? tiedHit.collider.gameObject.layer : -1;
                error = $"최상단 동률 표면 '{tiedName}'(layer {tiedLayer})이 허용된 Ground TerrainCollider가 아닙니다.";
                return false;
            }

            float projection = Mathf.Abs(top.point.y - desiredPosition.y);
            if (enforceProjectionDistance &&
                projection > Mathf.Max(0f, _settings.maxGroundProjectionDistance))
            {
                error = $"지면 투영 거리 {projection:0.###}m가 허용값 {_settings.maxGroundProjectionDistance:0.###}m를 초과했습니다.";
                return false;
            }

            float slope = Vector3.Angle(Vector3.up, top.normal);
            if (slope > Mathf.Clamp(_settings.maxGroundSlopeAngle, 0f, 89f) + 0.01f)
            {
                error = $"지면 경사 {slope:0.##}도가 허용값 {_settings.maxGroundSlopeAngle:0.##}도를 초과했습니다.";
                return false;
            }

            sample = new CycleGroundSample(top.point, top.normal, terrain);
            return true;
        }

        private bool TryEvaluateGroupCandidate(
            CycleGroundGroupRequest request,
            Vector3 anchorCandidate,
            out CycleGroundGroupPlacement placement,
            out string error)
        {
            placement = null;
            if (!TryResolveCandidatePosition(
                    anchorCandidate,
                    request.allowRelocation,
                    out CycleGroundSample anchorSample,
                    out Vector3 resolvedAnchor,
                    out error))
            {
                return false;
            }

            if (!IsOutsideBossRadius(resolvedAnchor, request.bossPositions))
            {
                error = "그룹 앵커가 보스 제외 반경 안에 있습니다.";
                return false;
            }

            if (request.route != null)
            {
                Vector3 routePosition = request.route.Evaluate(request.routeProgress);
                KccPlacementProfile routeProfile = new(
                    _settings.routeClearanceRadius,
                    _settings.routeClearanceHeight,
                    Mathf.Max(_settings.routeClearanceRadius, _settings.routeClearanceHeight * 0.5f),
                    _settings.maxGroundSlopeAngle,
                    _settings.maxGroundStepHeight);
                if (!TryBuildGroundChain(routePosition, resolvedAnchor, routeProfile, out _, out string routeError))
                {
                    error = $"route→앵커 로컬 경로가 유효하지 않습니다: {routeError}";
                    return false;
                }
            }

            List<CycleGroundMemberPlacement> members = new(request.members.Count);
            for (int i = 0; i < request.members.Count; i++)
            {
                CycleGroundMemberRequest member = request.members[i];
                if (!TryResolveMember(
                        request,
                        resolvedAnchor,
                        member,
                        i,
                        members,
                        out CycleGroundMemberPlacement memberPlacement,
                        out error))
                    return false;

                members.Add(memberPlacement);
            }

            // exact 복원은 입력 앵커와 입력 localOffset으로 계산한 좌표를 결과에도 그대로 보존한다.
            if (!request.allowRelocation)
            {
                for (int i = 0; i < members.Count; i++)
                {
                    CycleGroundMemberRequest requested = request.members[i];
                    members[i] = new CycleGroundMemberPlacement(
                        requested.StableId,
                        request.desiredAnchor + requested.LocalOffset,
                        requested.Profile);
                }
                resolvedAnchor = request.desiredAnchor;
            }

            placement = new CycleGroundGroupPlacement(resolvedAnchor, members);
            return true;
        }

        private bool TryResolveMember(
            CycleGroundGroupRequest request,
            Vector3 resolvedAnchor,
            CycleGroundMemberRequest member,
            int memberIndex,
            IReadOnlyList<CycleGroundMemberPlacement> acceptedMembers,
            out CycleGroundMemberPlacement placement,
            out string error)
        {
            placement = default;
            error = null;
            string lastError = null;
            Vector3 desiredMember = resolvedAnchor + member.LocalOffset;
            string stableCandidateId = $"{request.stableId}|{member.StableId}";
            foreach (Vector3 memberCandidate in EnumerateCandidates(
                         stableCandidateId,
                         desiredMember,
                         request.allowRelocation))
            {
                if (!TryResolveCandidatePosition(
                        memberCandidate,
                        request.allowRelocation,
                        out CycleGroundSample memberSample,
                        out Vector3 resolvedMember,
                        out string candidateError))
                {
                    lastError = $"지면: {candidateError}";
                    continue;
                }

                float memberSlope = Vector3.Angle(Vector3.up, memberSample.Normal);
                float allowedSlope = Mathf.Min(
                    Mathf.Clamp(_settings.maxGroundSlopeAngle, 0f, 89f),
                    member.Profile.MaxSlopeAngle);
                if (memberSlope > allowedSlope + 0.01f)
                {
                    lastError = $"경사 {memberSlope:0.##}도가 KCC 한계 {allowedSlope:0.##}도를 초과했습니다.";
                    continue;
                }

                if (!IsOutsideBossRadius(
                        resolvedMember,
                        request.bossPositions,
                        member.Profile.Radius + CapsuleSkin))
                {
                    lastError = "KCC 캡슐이 보스 제외 반경을 침범합니다.";
                    continue;
                }

                if (!TryCapsuleClear(
                        resolvedMember,
                        member.Profile,
                        memberSample.Surface,
                        out string clearanceError))
                {
                    lastError = $"캡슐 여유 공간이 없습니다: {clearanceError}";
                    continue;
                }

                if (!TryBuildGroundChain(
                        resolvedAnchor,
                        resolvedMember,
                        member.Profile,
                        out _,
                        out string chainError))
                {
                    lastError = $"앵커 로컬 경로가 유효하지 않습니다: {chainError}";
                    continue;
                }

                bool overlapsMember = false;
                for (int acceptedIndex = 0; acceptedIndex < acceptedMembers.Count; acceptedIndex++)
                {
                    CycleGroundMemberPlacement accepted = acceptedMembers[acceptedIndex];
                    if (!CapsulesOverlap(
                            resolvedMember,
                            member.Profile,
                            accepted.Position,
                            accepted.Profile,
                            _settings.memberClearanceGap))
                        continue;

                    lastError = $"멤버 {acceptedIndex}('{accepted.StableId}')의 가상 캡슐과 겹칩니다.";
                    overlapsMember = true;
                    break;
                }
                if (overlapsMember) continue;

                placement = new CycleGroundMemberPlacement(member.StableId, resolvedMember, member.Profile);
                return true;
            }

            error = request.allowRelocation
                ? $"멤버 {memberIndex}('{member.StableId}')를 결정론적 탐색 반경 안에 배치하지 못했습니다. 마지막 오류: {lastError}"
                : $"멤버 {memberIndex}('{member.StableId}')의 저장 좌표가 유효하지 않습니다. {lastError}";
            return false;
        }

        private bool TryResolveCandidatePosition(
            Vector3 desired,
            bool allowProjection,
            out CycleGroundSample sample,
            out Vector3 resolved,
            out string error)
        {
            if (!TrySampleGround(desired, out sample, out error))
            {
                resolved = default;
                return false;
            }

            if (!allowProjection && Mathf.Abs(sample.Position.y - desired.y) > ExactGroundTolerance)
            {
                resolved = default;
                error = $"exact 복원 좌표의 지면 오차 {Mathf.Abs(sample.Position.y - desired.y):0.###}m가 허용값 {ExactGroundTolerance:0.###}m를 초과했습니다.";
                return false;
            }

            resolved = allowProjection ? sample.Position : desired;
            return true;
        }

        private bool TryBuildGroundChain(
            Vector3 start,
            Vector3 end,
            KccPlacementProfile profile,
            out CycleGroundRoute route,
            out string error)
        {
            route = null;
            error = null;
            Vector3 horizontal = end - start;
            horizontal.y = 0f;
            float spacing = Mathf.Max(MinSampleSpacing, _settings.pathSampleSpacing);
            int segmentCount = Mathf.Max(1, Mathf.CeilToInt(horizontal.magnitude / spacing));
            List<CycleGroundSample> samples = new(segmentCount + 1);
            float allowedSlope = Mathf.Min(
                Mathf.Clamp(_settings.maxGroundSlopeAngle, 0f, 89f),
                profile.MaxSlopeAngle);
            float allowedStep = Mathf.Min(
                Mathf.Max(0f, _settings.maxGroundStepHeight),
                profile.MaxStepHeight);

            for (int i = 0; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                Vector3 desired = Vector3.Lerp(start, end, t);
                if (samples.Count > 0)
                    desired.y = samples[^1].Position.y;

                if (!TrySampleRouteGround(desired, out CycleGroundSample sample, out error))
                {
                    error = $"경로 표본 {i}/{segmentCount}: {error}";
                    return false;
                }

                float surfaceSlope = Vector3.Angle(Vector3.up, sample.Normal);
                if (surfaceSlope > allowedSlope + 0.01f)
                {
                    error = $"경로 표본 {i}/{segmentCount} 경사 {surfaceSlope:0.##}도가 한계 {allowedSlope:0.##}도를 초과했습니다.";
                    return false;
                }

                if (!TryCapsuleClear(sample.Position, profile, sample.Surface, out string clearanceError))
                {
                    error = $"경로 표본 {i}/{segmentCount} 캡슐 장애물: {clearanceError}";
                    return false;
                }

                if (samples.Count > 0)
                {
                    CycleGroundSample previous = samples[^1];
                    Vector3 delta = sample.Position - previous.Position;
                    float horizontalDistance = new Vector2(delta.x, delta.z).magnitude;
                    float heightDelta = Mathf.Abs(delta.y);
                    float segmentSlope = Mathf.Atan2(heightDelta, Mathf.Max(0.0001f, horizontalDistance)) * Mathf.Rad2Deg;
                    if (heightDelta > allowedStep + 0.001f)
                    {
                        float previousNormalSlope = Vector3.Angle(Vector3.up, previous.Normal);
                        float currentNormalSlope = Vector3.Angle(Vector3.up, sample.Normal);
                        float explainingSurfaceSlope = Mathf.Max(previousNormalSlope, currentNormalSlope);
                        if (explainingSurfaceSlope + ContinuousSlopeTolerance < segmentSlope)
                        {
                            error = $"경로 표본 {i - 1}→{i} 높이 {heightDelta:0.###}m/구간 경사 {segmentSlope:0.##}도가 " +
                                    $"양끝 표면 경사 {explainingSurfaceSlope:0.##}도로 설명되지 않아 불연속 단차로 판정했습니다.";
                            return false;
                        }
                    }

                    if (!TryCapsuleSegmentClear(previous, sample, profile, out string segmentError))
                    {
                        error = $"경로 표본 {i - 1}→{i} 캡슐 이동이 막혔습니다: {segmentError}";
                        return false;
                    }
                }

                samples.Add(sample);
            }

            route = new CycleGroundRoute(samples);
            return true;
        }

        private bool TryCapsuleClear(
            Vector3 basePosition,
            KccPlacementProfile profile,
            TerrainCollider surface,
            out string error)
        {
            GetCapsulePoints(basePosition, profile, out Vector3 point1, out Vector3 point2, out float radius);
            Collider[] overlaps = Physics.OverlapCapsule(
                point1,
                point2,
                radius,
                _settings.placementObstacleLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider overlap = overlaps[i];
                if (overlap == null || IsAllowedGroundTerrain(overlap)) continue;
                error = $"'{overlap.name}'(layer {overlap.gameObject.layer})";
                return false;
            }

            Bounds capsuleBounds = CreateCapsuleBounds(point1, point2, radius);
            if (TryFindTerrainTreeOverlap(surface, capsuleBounds, out TerrainTreeObstacle treeObstacle))
            {
                error = $"Terrain tree '{treeObstacle.PrototypeName}'(instance {treeObstacle.InstanceIndex})";
                return false;
            }

            error = null;
            return true;
        }

        private bool TryCapsuleSegmentClear(
            CycleGroundSample start,
            CycleGroundSample end,
            KccPlacementProfile profile,
            out string error)
        {
            GetCapsulePoints(start.Position, profile, out Vector3 point1, out Vector3 point2, out float radius);
            Vector3 displacement = end.Position - start.Position;
            float distance = displacement.magnitude;
            if (distance <= 0.0001f)
            {
                error = null;
                return true;
            }

            RaycastHit[] hits = Physics.CapsuleCastAll(
                point1,
                point2,
                radius,
                displacement / distance,
                distance,
                _settings.placementObstacleLayers,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || IsAllowedGroundTerrain(collider)) continue;
                error = $"'{collider.name}'(layer {collider.gameObject.layer})";
                return false;
            }

            GetCapsulePoints(end.Position, profile, out Vector3 endPoint1, out Vector3 endPoint2, out _);
            Bounds sweptBounds = CreateSweptCapsuleBounds(
                point1,
                point2,
                endPoint1,
                endPoint2,
                radius);
            if (TryFindTerrainTreeOverlap(start.Surface, sweptBounds, out TerrainTreeObstacle treeObstacle) ||
                (end.Surface != start.Surface &&
                 TryFindTerrainTreeOverlap(end.Surface, sweptBounds, out treeObstacle)))
            {
                error = $"Terrain tree '{treeObstacle.PrototypeName}'(instance {treeObstacle.InstanceIndex})";
                return false;
            }

            error = null;
            return true;
        }

        private bool TryFindTerrainTreeOverlap(
            TerrainCollider terrain,
            Bounds queryBounds,
            out TerrainTreeObstacle obstacle)
        {
            obstacle = default;
            if (terrain == null || terrain.terrainData == null) return false;

            if (!_terrainTreeObstacleCaches.TryGetValue(terrain, out TerrainTreeObstacleCache cache) ||
                cache.TerrainData != terrain.terrainData)
            {
                cache = new TerrainTreeObstacleCache(terrain, TreeObstacleCellSize);
                _terrainTreeObstacleCaches[terrain] = cache;
            }

            return cache.TryFindOverlap(queryBounds, out obstacle);
        }

        private static Bounds CreateCapsuleBounds(Vector3 point1, Vector3 point2, float radius)
        {
            Vector3 padding = Vector3.one * radius;
            Bounds bounds = new();
            bounds.SetMinMax(Vector3.Min(point1, point2) - padding, Vector3.Max(point1, point2) + padding);
            return bounds;
        }

        private static Bounds CreateSweptCapsuleBounds(
            Vector3 startPoint1,
            Vector3 startPoint2,
            Vector3 endPoint1,
            Vector3 endPoint2,
            float radius)
        {
            Vector3 minimum = Vector3.Min(
                Vector3.Min(startPoint1, startPoint2),
                Vector3.Min(endPoint1, endPoint2));
            Vector3 maximum = Vector3.Max(
                Vector3.Max(startPoint1, startPoint2),
                Vector3.Max(endPoint1, endPoint2));
            Vector3 padding = Vector3.one * radius;
            Bounds bounds = new();
            bounds.SetMinMax(minimum - padding, maximum + padding);
            return bounds;
        }

        private readonly struct TerrainTreeObstacle
        {
            public TerrainTreeObstacle(Bounds bounds, string prototypeName, int instanceIndex)
            {
                Bounds = bounds;
                PrototypeName = prototypeName;
                InstanceIndex = instanceIndex;
            }

            public Bounds Bounds { get; }
            public string PrototypeName { get; }
            public int InstanceIndex { get; }
        }

        /// <summary>
        /// Terrain tree collider는 TerrainCollider의 Ground 레이어로 합쳐져 Physics 장애물
        /// 마스크에서 분리할 수 없다. prototype collider의 보수적인 월드 AABB를 한 번 만들고
        /// XZ 셀로 색인해 Ground Terrain 본체를 허용하면서 나무만 별도로 거부한다.
        /// </summary>
        private sealed class TerrainTreeObstacleCache
        {
            private readonly float _cellSize;
            private readonly Dictionary<long, List<TerrainTreeObstacle>> _cells = new();

            public TerrainTreeObstacleCache(TerrainCollider terrain, float cellSize)
            {
                TerrainData = terrain.terrainData;
                _cellSize = Mathf.Max(0.1f, cellSize);
                Build(terrain);
            }

            public TerrainData TerrainData { get; }

            public bool TryFindOverlap(Bounds queryBounds, out TerrainTreeObstacle obstacle)
            {
                GetCellRange(queryBounds, out int minX, out int maxX, out int minZ, out int maxZ);
                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (!_cells.TryGetValue(GetCellKey(x, z), out List<TerrainTreeObstacle> obstacles))
                            continue;

                        for (int i = 0; i < obstacles.Count; i++)
                        {
                            if (!obstacles[i].Bounds.Intersects(queryBounds)) continue;
                            obstacle = obstacles[i];
                            return true;
                        }
                    }
                }

                obstacle = default;
                return false;
            }

            private void Build(TerrainCollider terrain)
            {
                if (TerrainData == null) return;

                TreePrototype[] prototypes = TerrainData.treePrototypes;
                TreeInstance[] instances = TerrainData.treeInstances;
                if (prototypes == null || instances == null || prototypes.Length == 0 || instances.Length == 0)
                    return;

                Bounds[] prototypeBounds = new Bounds[prototypes.Length];
                bool[] hasPrototypeBounds = new bool[prototypes.Length];
                for (int i = 0; i < prototypes.Length; i++)
                {
                    GameObject prefab = prototypes[i].prefab;
                    hasPrototypeBounds[i] = TryGetPrototypeColliderBounds(prefab, out prototypeBounds[i]);
                }

                Vector3 terrainSize = TerrainData.size;
                Matrix4x4 terrainToWorld = terrain.transform.localToWorldMatrix;
                for (int instanceIndex = 0; instanceIndex < instances.Length; instanceIndex++)
                {
                    TreeInstance instance = instances[instanceIndex];
                    int prototypeIndex = instance.prototypeIndex;
                    if (prototypeIndex < 0 || prototypeIndex >= prototypes.Length ||
                        !hasPrototypeBounds[prototypeIndex])
                    {
                        continue;
                    }

                    Vector3 localPosition = Vector3.Scale(instance.position, terrainSize);
                    Vector3 instanceScale = new(
                        Mathf.Abs(instance.widthScale),
                        Mathf.Abs(instance.heightScale),
                        Mathf.Abs(instance.widthScale));
                    Quaternion instanceRotation = Quaternion.AngleAxis(
                        instance.rotation * Mathf.Rad2Deg,
                        Vector3.up);
                    Matrix4x4 prototypeToWorld = terrainToWorld * Matrix4x4.TRS(
                        localPosition,
                        instanceRotation,
                        instanceScale);
                    Bounds worldBounds = TransformBounds(prototypeBounds[prototypeIndex], prototypeToWorld);
                    string prototypeName = prototypes[prototypeIndex].prefab != null
                        ? prototypes[prototypeIndex].prefab.name
                        : $"prototype {prototypeIndex}";
                    Add(new TerrainTreeObstacle(worldBounds, prototypeName, instanceIndex));
                }
            }

            private void Add(TerrainTreeObstacle obstacle)
            {
                GetCellRange(obstacle.Bounds, out int minX, out int maxX, out int minZ, out int maxZ);
                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        long key = GetCellKey(x, z);
                        if (!_cells.TryGetValue(key, out List<TerrainTreeObstacle> obstacles))
                        {
                            obstacles = new List<TerrainTreeObstacle>();
                            _cells.Add(key, obstacles);
                        }

                        obstacles.Add(obstacle);
                    }
                }
            }

            private void GetCellRange(
                Bounds bounds,
                out int minX,
                out int maxX,
                out int minZ,
                out int maxZ)
            {
                minX = Mathf.FloorToInt(bounds.min.x / _cellSize);
                maxX = Mathf.FloorToInt(bounds.max.x / _cellSize);
                minZ = Mathf.FloorToInt(bounds.min.z / _cellSize);
                maxZ = Mathf.FloorToInt(bounds.max.z / _cellSize);
            }

            private static long GetCellKey(int x, int z)
            {
                return ((long)x << 32) ^ (uint)z;
            }

            private static bool TryGetPrototypeColliderBounds(GameObject prefab, out Bounds bounds)
            {
                bounds = default;
                if (prefab == null) return false;

                Transform root = prefab.transform;
                Matrix4x4 rootAuthoringTransform = Matrix4x4.TRS(
                    Vector3.zero,
                    root.localRotation,
                    root.localScale);
                Collider[] colliders = prefab.GetComponentsInChildren<Collider>(true);
                bool hasBounds = false;
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider collider = colliders[i];
                    if (collider == null || !collider.enabled || collider.isTrigger ||
                        !IsActiveUnderRoot(collider.transform, root))
                    {
                        continue;
                    }

                    if (TryGetColliderLocalBounds(collider, out Bounds colliderLocalBounds))
                    {
                        Matrix4x4 colliderToPrototype = rootAuthoringTransform *
                                                        root.worldToLocalMatrix *
                                                        collider.transform.localToWorldMatrix;
                        Encapsulate(
                            ref bounds,
                            ref hasBounds,
                            TransformBounds(colliderLocalBounds, colliderToPrototype));
                        continue;
                    }

                    Bounds fallbackBounds = collider.bounds;
                    if (fallbackBounds.size.sqrMagnitude <= 0.000001f) continue;
                    Matrix4x4 worldToPrototype = rootAuthoringTransform * root.worldToLocalMatrix;
                    Encapsulate(
                        ref bounds,
                        ref hasBounds,
                        TransformBounds(fallbackBounds, worldToPrototype));
                }

                return hasBounds;
            }

            private static bool TryGetColliderLocalBounds(Collider collider, out Bounds bounds)
            {
                switch (collider)
                {
                    case BoxCollider box:
                        bounds = new Bounds(box.center, Abs(box.size));
                        return true;
                    case SphereCollider sphere:
                        float sphereDiameter = Mathf.Max(0f, sphere.radius) * 2f;
                        bounds = new Bounds(sphere.center, Vector3.one * sphereDiameter);
                        return true;
                    case CapsuleCollider capsule:
                        float capsuleDiameter = Mathf.Max(0f, capsule.radius) * 2f;
                        Vector3 capsuleSize = Vector3.one * capsuleDiameter;
                        capsuleSize[capsule.direction] = Mathf.Max(capsuleDiameter, capsule.height);
                        bounds = new Bounds(capsule.center, capsuleSize);
                        return true;
                    case CharacterController controller:
                        float controllerDiameter = Mathf.Max(0f, controller.radius) * 2f;
                        bounds = new Bounds(
                            controller.center,
                            new Vector3(
                                controllerDiameter,
                                Mathf.Max(controllerDiameter, controller.height),
                                controllerDiameter));
                        return true;
                    case MeshCollider meshCollider when meshCollider.sharedMesh != null:
                        bounds = meshCollider.sharedMesh.bounds;
                        return true;
                    default:
                        bounds = default;
                        return false;
                }
            }

            private static Vector3 Abs(Vector3 value)
            {
                return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
            }

            private static bool IsActiveUnderRoot(Transform transform, Transform root)
            {
                Transform current = transform;
                while (current != null)
                {
                    if (!current.gameObject.activeSelf) return false;
                    if (current == root) return true;
                    current = current.parent;
                }

                return false;
            }

            private static void Encapsulate(ref Bounds aggregate, ref bool hasBounds, Bounds addition)
            {
                if (!hasBounds)
                {
                    aggregate = addition;
                    hasBounds = true;
                    return;
                }

                aggregate.Encapsulate(addition.min);
                aggregate.Encapsulate(addition.max);
            }

            private static Bounds TransformBounds(Bounds source, Matrix4x4 matrix)
            {
                Vector3 sourceMin = source.min;
                Vector3 sourceMax = source.max;
                Vector3 first = matrix.MultiplyPoint3x4(sourceMin);
                Bounds result = new(first, Vector3.zero);
                for (int x = 0; x < 2; x++)
                {
                    for (int y = 0; y < 2; y++)
                    {
                        for (int z = 0; z < 2; z++)
                        {
                            if (x == 0 && y == 0 && z == 0) continue;
                            result.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(
                                x == 0 ? sourceMin.x : sourceMax.x,
                                y == 0 ? sourceMin.y : sourceMax.y,
                                z == 0 ? sourceMin.z : sourceMax.z)));
                        }
                    }
                }

                return result;
            }
        }

        private static void GetCapsulePoints(
            Vector3 basePosition,
            KccPlacementProfile profile,
            out Vector3 point1,
            out Vector3 point2,
            out float radius)
        {
            // Ground Terrain은 obstacle 검사에서 명시적으로 제외한다. 접촉 오검출을 피하려고
            // 실제 KCC보다 캡슐을 줄일 이유가 없으므로 작은 안전 여유를 바깥으로 더한다.
            radius = Mathf.Max(0.01f, profile.Radius + CapsuleSkin);
            float height = Mathf.Max(radius * 2f, profile.Height);
            float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 center = basePosition + Vector3.up * profile.YOffset;
            point1 = center - Vector3.up * halfSegment;
            point2 = center + Vector3.up * halfSegment;
        }

        private bool IsAllowedGroundTerrain(Collider collider)
        {
            return collider is TerrainCollider &&
                   ContainsLayer(_settings.placementGroundLayers, collider.gameObject.layer);
        }

        private bool IsOutsideBossRadius(
            Vector3 position,
            IReadOnlyList<Vector3> bossPositions,
            float footprintRadius = 0f)
        {
            float radius = Mathf.Max(0f, _settings.bossExclusionRadius) + Mathf.Max(0f, footprintRadius);
            if (radius <= 0f || bossPositions == null) return true;

            float squaredRadius = radius * radius;
            for (int i = 0; i < bossPositions.Count; i++)
            {
                Vector3 delta = position - bossPositions[i];
                delta.y = 0f;
                if (delta.sqrMagnitude < squaredRadius)
                    return false;
            }
            return true;
        }

        private static bool CapsulesOverlap(
            Vector3 firstPosition,
            KccPlacementProfile first,
            Vector3 secondPosition,
            KccPlacementProfile second,
            float clearanceGap)
        {
            float horizontalDistance = new Vector2(
                firstPosition.x - secondPosition.x,
                firstPosition.z - secondPosition.z).magnitude;
            float requiredHorizontal = first.Radius + second.Radius + Mathf.Max(0f, clearanceGap);
            if (horizontalDistance >= requiredHorizontal) return false;

            float firstMin = firstPosition.y + first.YOffset - first.Height * 0.5f;
            float firstMax = firstPosition.y + first.YOffset + first.Height * 0.5f;
            float secondMin = secondPosition.y + second.YOffset - second.Height * 0.5f;
            float secondMax = secondPosition.y + second.YOffset + second.Height * 0.5f;
            return firstMin < secondMax && secondMin < firstMax;
        }

        private IEnumerable<Vector3> EnumerateCandidates(string stableId, Vector3 desired, bool allowRelocation)
        {
            yield return desired;
            if (!allowRelocation) yield break;

            float radiusLimit = Mathf.Max(0f, _settings.placementSearchRadius);
            float step = Mathf.Max(MinSampleSpacing, _settings.placementSearchStep);
            if (radiusLimit <= 0f) yield break;

            uint hash = StableFnv1A(stableId ?? string.Empty);
            float phase = (hash / (float)uint.MaxValue) * Mathf.PI * 2f;
            int ringCount = Mathf.CeilToInt(radiusLimit / step);
            for (int ring = 1; ring <= ringCount; ring++)
            {
                float radius = Mathf.Min(radiusLimit, ring * step);
                int count = Mathf.Max(6, Mathf.CeilToInt(Mathf.PI * 2f * radius / step));
                float ringPhase = phase + ring * GoldenAngle;
                for (int index = 0; index < count; index++)
                {
                    float angle = ringPhase + Mathf.PI * 2f * index / count;
                    yield return desired + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                }
            }
        }

        private static uint StableFnv1A(string value)
        {
            uint hash = FnvOffsetBasis;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                hash ^= (byte)(character & 0xFF);
                hash *= FnvPrime;
                hash ^= (byte)(character >> 8);
                hash *= FnvPrime;
            }
            return hash;
        }

        private static bool ContainsLayer(LayerMask mask, int layer)
        {
            return layer >= 0 && layer < 32 && (mask.value & (1 << layer)) != 0;
        }
    }
}
