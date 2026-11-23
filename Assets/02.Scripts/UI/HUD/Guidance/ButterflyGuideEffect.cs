using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.UI;

namespace UPlayGround.UI.Guidance
{
    /// <summary>
    /// 나비가 플레이어 앞에서 길을 따라 먼저 날며 목표를 알려 주는 연출. 두 층으로 이뤄진다.
    /// 군집: 실제 나비 프리팹 몇 마리가 경로의 "플레이어 앞쪽 지점"을 따라붙어, 플레이어가 느리면 기다리고 빠르면 앞서 나간다.
    /// 행렬(<see cref="ButterflyStream"/>): 수십 마리의 가벼운 나비가 플레이어 앞에서 목표 쪽으로 끊임없이 흘러 길의 굽이 전체를 보여 준다.
    /// 풀링한 개체를 재사용하므로 재생 중·재생 후에 할당이 생기지 않는다.
    /// </summary>
    public sealed class ButterflyGuideEffect : IGuideEffect
    {
        private const float AppearDuration = 0.25f;
        private const float HeadingSharpness = 10f;
        private const float MinHeadingSqrSpeed = 0.0004f;
        private const float VerticalFlutterRatio = 0.5f;
        private const float VerticalFlutterFrequencyRatio = 1.7f;

        private enum Mode
        {
            Idle,
            Guide,
        }

        private sealed class Agent
        {
            public Transform Transform;
            public TrailRenderer Trail;
            public Vector3 BaseScale;
            public Vector3 Position;
            public Vector3 SpawnPosition;
            public float PathDistance;
            public float LeadOffset;
            public float Phase;
            public Vector2 Lateral;
        }

        private const float RibbonStep = 1.5f;
        private const float RibbonSurfaceSearchDistance = 2f;

        private readonly List<Agent> _pool = new();
        private readonly ButterflyStream _stream = new();
        private LineRenderer _ribbon;
        private Material _ribbonMaterial;
        private float _ribbonBaseWidth;
        private Vector3[] _ribbonPositions;
        private GuideProfileSO _profile;
        private GuidePath _path;
        private Transform _player;
        private Transform _root;
        private Camera _camera;
        private Mode _mode;
        private float _elapsed;
        private float _totalDuration;
        private float _projectedDistance;
        private int _activeCount;

        public bool IsPlaying => _mode != Mode.Idle;

        /// <summary>경로를 따라 흐르는 행렬. 테스트·디버그에서 흐름 상태를 읽는 용도다.</summary>
        public ButterflyStream Stream => _stream;

        public void Begin(in GuideContext context)
        {
            if (!PrepareAgents(context, context.Profile.agentCount))
                return;

            _mode = Mode.Guide;
            _path = context.Path;
            _totalDuration = context.Profile.duration;
            _projectedDistance = 0f;

            Vector3 spawnOrigin = ResolveSpawnOrigin();
            for (int i = 0; i < _activeCount; i++)
            {
                Agent agent = _pool[i];
                ResetAgent(agent, spawnOrigin);
                agent.PathDistance = 0f;
                agent.LeadOffset = UnityEngine.Random.Range(-_profile.leadSpread, _profile.leadSpread);
                agent.Lateral = new Vector2(
                    UnityEngine.Random.Range(-_profile.lateralSpread, _profile.lateralSpread),
                    UnityEngine.Random.Range(-_profile.lateralSpread, _profile.lateralSpread) * VerticalFlutterRatio);
            }

            EnsureRibbon();
            _stream.Begin(_profile, _path, _root, _camera);
        }

        public void ReplacePath(GuidePath path)
        {
            if (_mode != Mode.Guide || path == null || !path.IsComplete)
                return;

            _path = path;
            _projectedDistance = 0f;
            for (int i = 0; i < _activeCount; i++)
                _pool[i].PathDistance = path.ProjectDistance(_pool[i].Position, 0f, out _);
            _stream.ReplacePath(path);
        }

        public void Tick(float deltaTime)
        {
            if (_mode == Mode.Idle)
                return;

            _elapsed += deltaTime;
            if (_elapsed >= _totalDuration)
            {
                Stop(true);
                return;
            }

            TickGuide(deltaTime);
        }

        public void Stop(bool immediate)
        {
            if (_mode == Mode.Idle)
                return;

            if (!immediate)
            {
                // 지금부터 페이드아웃 구간으로 건너뛰어 자연스럽게 사라진다.
                float fadeStart = Mathf.Max(0f, _totalDuration - _profile.fadeOutDuration);
                _elapsed = Mathf.Max(_elapsed, fadeStart);
                return;
            }

            for (int i = 0; i < _pool.Count; i++)
                _pool[i].Transform.gameObject.SetActive(false);

            _stream.Clear();
            HideRibbon();
            _activeCount = 0;
            _mode = Mode.Idle;
            _path = null;
        }

        public void Dispose()
        {
            Stop(true);
            _stream.Dispose();
            if (_ribbon != null)
                UnityEngine.Object.Destroy(_ribbon.gameObject);
            _ribbon = null;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].Transform != null)
                    UnityEngine.Object.Destroy(_pool[i].Transform.gameObject);
            }

            _pool.Clear();
        }

        #region 재생

        private void TickGuide(float deltaTime)
        {
            float pathLength = _path.TotalLength;
            _projectedDistance = Mathf.Max(
                _projectedDistance,
                _path.ProjectDistance(_player.position, _projectedDistance, out _));

            float leadProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _totalDuration - _profile.fadeOutDuration)));
            float lead = Mathf.Lerp(_profile.leadDistanceStart, _profile.leadDistanceEnd, leadProgress);
            float gather = _profile.gatherDuration > 0f
                ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / _profile.gatherDuration))
                : 1f;
            float followBlend = 1f - Mathf.Exp(-_profile.followSharpness * deltaTime);

            for (int i = 0; i < _activeCount; i++)
            {
                Agent agent = _pool[i];
                AdvanceAlongPath(agent, pathLength, lead, deltaTime);

                Vector3 desired = ComputeGuidePosition(agent, pathLength);
                desired = Vector3.Lerp(agent.SpawnPosition, desired, gather);
                MoveAgent(agent, Vector3.Lerp(agent.Position, desired, followBlend), deltaTime);
                ApplyScale(agent);
            }

            // 페이드아웃 구간에 들어서면 행렬은 새 나비를 띄우지 않고, 날던 나비가 끝까지 흘러가며 사라진다.
            if (_elapsed >= _totalDuration - _profile.fadeOutDuration)
                _stream.StopRespawning();
            _stream.Tick(deltaTime, _elapsed, _projectedDistance, ComputePresence());

            UpdateRibbon();
        }

        #region 경로 띠

        // 띠는 나비가 지나가는 길 자체를 보여 준다. 프리팹이 없으면 만들지 않는다.
        private void EnsureRibbon()
        {
            if (_ribbon != null || _profile.pathRibbonPrefab == null)
                return;

            GameObject instance = UnityEngine.Object.Instantiate(_profile.pathRibbonPrefab, _root);
            _ribbon = instance.GetComponentInChildren<LineRenderer>(true);
            if (_ribbon == null)
            {
                UnityEngine.Object.Destroy(instance);
                return;
            }

            _ribbon.useWorldSpace = true;
            _ribbonBaseWidth = _ribbon.widthMultiplier;
            _ribbonMaterial = _ribbon.material;
            _ribbonPositions = new Vector3[Mathf.CeilToInt(_profile.ribbonLength / RibbonStep) + 2];
            _ribbon.gameObject.SetActive(false);
        }

        private void UpdateRibbon()
        {
            if (_ribbon == null)
                return;

            float start = _projectedDistance;
            float end = Mathf.Min(_path.TotalLength, start + _profile.ribbonLength);
            if (end - start < RibbonStep)
            {
                _ribbon.gameObject.SetActive(false);
                return;
            }

            int count = Mathf.Min(_ribbonPositions.Length, Mathf.CeilToInt((end - start) / RibbonStep) + 1);
            for (int i = 0; i < count; i++)
            {
                _path.Sample(Mathf.Lerp(start, end, i / (float)(count - 1)), out Vector3 point, out _);
                _ribbonPositions[i] = SnapToSurface(point) + Vector3.up * _profile.ribbonHeight;
            }

            _ribbon.gameObject.SetActive(true);
            _ribbon.positionCount = count;
            _ribbon.SetPositions(_ribbonPositions);
            _ribbon.widthMultiplier = _ribbonBaseWidth * ComputePresence();
            _ribbonMaterial.mainTextureOffset = new Vector2(-_elapsed * _profile.ribbonScrollSpeed, 0f);
        }

        // 보간한 경로가 지형 굴곡 아래로 파묻히지 않도록 NavMesh 표면 높이를 따른다. 표면을 못 찾으면 경로 높이를 쓴다.
        private static Vector3 SnapToSurface(Vector3 point)
        {
            return UnityEngine.AI.NavMesh.SamplePosition(point, out UnityEngine.AI.NavMeshHit hit, RibbonSurfaceSearchDistance, UnityEngine.AI.NavMesh.AllAreas)
                ? new Vector3(point.x, hit.position.y, point.z)
                : point;
        }

        private void HideRibbon()
        {
            if (_ribbon != null)
                _ribbon.gameObject.SetActive(false);
        }

        #endregion

        private void AdvanceAlongPath(Agent agent, float pathLength, float lead, float deltaTime)
        {
            float target = Mathf.Clamp(_projectedDistance + lead + agent.LeadOffset, 0f, pathLength);
            float speed = Mathf.Clamp((target - agent.PathDistance) * _profile.catchUpGain, -_profile.maxSpeed, _profile.maxSpeed);
            agent.PathDistance = Mathf.Clamp(agent.PathDistance + speed * deltaTime, 0f, pathLength);
        }

        private Vector3 ComputeGuidePosition(Agent agent, float pathLength)
        {
            _path.Sample(agent.PathDistance, out Vector3 pathPosition, out Vector3 tangent);

            Vector3 right = Vector3.Cross(Vector3.up, tangent);
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;

            float wave = _elapsed * _profile.flutterFrequency * Mathf.PI * 2f + agent.Phase;
            float sideOffset = agent.Lateral.x + Mathf.Sin(wave) * _profile.flutterAmplitude;
            float heightOffset = _profile.flightHeight + agent.Lateral.y
                + Mathf.Sin(wave * VerticalFlutterFrequencyRatio) * _profile.flutterAmplitude * VerticalFlutterRatio;
            Vector3 onPath = pathPosition + right * sideOffset + Vector3.up * heightOffset;

            float arrive = 1f - Mathf.Clamp01((pathLength - agent.PathDistance) / _profile.arriveRadius);
            if (arrive <= 0f)
                return onPath;

            float orbitAngle = _elapsed * _profile.arriveOrbitSpeed + agent.Phase;
            Vector3 orbit = _path.EndPoint + Vector3.up * _profile.flightHeight
                + new Vector3(Mathf.Cos(orbitAngle), 0f, Mathf.Sin(orbitAngle)) * _profile.arriveOrbitRadius;
            return Vector3.Lerp(onPath, orbit, Mathf.SmoothStep(0f, 1f, arrive));
        }

        private void MoveAgent(Agent agent, Vector3 nextPosition, float deltaTime)
        {
            Vector3 delta = nextPosition - agent.Position;
            agent.Position = nextPosition;
            agent.Transform.position = nextPosition;

            if (delta.sqrMagnitude < MinHeadingSqrSpeed * deltaTime)
                return;

            Quaternion heading = Quaternion.LookRotation(delta.normalized, Vector3.up);
            agent.Transform.rotation = Quaternion.Slerp(agent.Transform.rotation, heading, 1f - Mathf.Exp(-HeadingSharpness * deltaTime));
        }

        // 등장은 짧게 커지며, 끝 구간에서는 작아지며 사라진다(머티리얼 투명도를 건드리지 않아 어떤 프리팹이든 동작).
        private void ApplyScale(Agent agent)
        {
            agent.Transform.localScale = agent.BaseScale * (ComputePresence() * ComputeDistanceBoost(agent));
        }

        // 0(보이지 않음)~1(완전히 나타남). 등장은 짧게, 끝 구간에서는 서서히 사라진다.
        private float ComputePresence()
        {
            float appear = Mathf.Clamp01(_elapsed / AppearDuration);
            float fadeDuration = _profile.fadeOutDuration;
            float vanish = fadeDuration > 0f ? Mathf.Clamp01((_totalDuration - _elapsed) / fadeDuration) : 1f;
            return Mathf.SmoothStep(0f, 1f, Mathf.Min(appear, vanish));
        }

        // 멀리 나는 개체가 점처럼 작아져 방향을 알아볼 수 없게 되는 것을 막는다.
        private float ComputeDistanceBoost(Agent agent)
        {
            if (_camera == null || _profile.distanceScaleMax <= 1f)
                return 1f;

            float distance = Vector3.Distance(_camera.transform.position, agent.Position);
            return Mathf.Clamp(distance / _profile.distanceScaleReference, 1f, _profile.distanceScaleMax);
        }

        #endregion

        #region 준비

        private bool PrepareAgents(in GuideContext context, int count)
        {
            Stop(true);

            if (context.Profile == null || context.Profile.agentPrefab == null || context.Player == null)
                return false;

            _profile = context.Profile;
            _player = context.Player;
            _root = context.EffectRoot;
            _camera = Camera.main;
            _elapsed = 0f;

            while (_pool.Count < count)
                _pool.Add(CreateAgent());

            _activeCount = count;
            return true;
        }

        private Agent CreateAgent()
        {
            GameObject instance = UnityEngine.Object.Instantiate(_profile.agentPrefab, _root);
            instance.SetActive(false);
            return new Agent
            {
                Transform = instance.transform,
                Trail = instance.GetComponentInChildren<TrailRenderer>(true),
                BaseScale = instance.transform.localScale,
            };
        }

        private void ResetAgent(Agent agent, Vector3 spawnOrigin)
        {
            agent.SpawnPosition = spawnOrigin + UnityEngine.Random.insideUnitSphere * _profile.spawnRadius;
            agent.Position = agent.SpawnPosition;
            agent.Phase = UnityEngine.Random.value * Mathf.PI * 2f;
            agent.Transform.SetPositionAndRotation(agent.SpawnPosition, Quaternion.LookRotation(PlanarForward(), Vector3.up));
            agent.Transform.localScale = Vector3.zero;
            agent.Transform.gameObject.SetActive(true);
            // 풀에서 꺼낸 개체가 이전 위치에서 새 위치까지 꼬리를 길게 끌지 않게 한다.
            if (agent.Trail != null)
                agent.Trail.Clear();
        }

        // 카메라가 보는 방향 앞에 나타나야 시야 안에서 먼저 눈에 들어온다. 카메라가 없으면 플레이어 정면을 쓴다.
        private Vector3 PlanarForward()
        {
            Vector3 forward = _camera != null ? _camera.transform.forward : _player.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        private Vector3 ResolveSpawnOrigin()
        {
            return _player.position + PlanarForward() * _profile.spawnForwardDistance + Vector3.up * _profile.spawnHeight;
        }

        #endregion
    }
}
