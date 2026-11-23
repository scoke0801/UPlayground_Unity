using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.UI;

namespace UPlayGround.UI.Guidance
{
    /// <summary>
    /// 바람결 여러 가닥이 플레이어 앞에서 길을 따라 흘러가며 방향을 알려 주는 연출.
    /// 나비와 같은 <see cref="IGuideEffect"/> 계약만 구현해 프로필 교체만으로 바꿔 쓸 수 있다는 것을 증명하는 두 번째 구현이다.
    /// 프리팹은 <see cref="TrailRenderer"/>(필수)와 선택적 <see cref="ParticleSystem"/>을 가진 한 가닥이다.
    /// </summary>
    public sealed class WindGuideEffect : IGuideEffect
    {
        private const float SpeedRatio = 0.7f;
        private const float HeightRatio = 0.6f;
        private const float MinTrailTime = 0.05f;

        private enum Mode
        {
            Idle,
            Guide,
        }

        private sealed class Gust
        {
            public Transform Transform;
            public TrailRenderer Trail;
            public float PathDistance;
            public float StartDelay;
            public float Phase;
            public float FinishedAt = -1f;
            public bool IsStarted;
        }

        private readonly List<Gust> _pool = new();
        private GuideProfileSO _profile;
        private GuidePath _path;
        private Transform _player;
        private Transform _root;
        private Mode _mode;
        private float _elapsed;
        private float _totalDuration;
        private float _projectedDistance;
        private int _activeCount;
        private bool _isStopRequested;

        public bool IsPlaying => _mode != Mode.Idle;

        public void Begin(in GuideContext context)
        {
            if (!PrepareGusts(context, context.Profile.agentCount))
                return;

            _mode = Mode.Guide;
            _path = context.Path;
            _totalDuration = context.Profile.duration;
            _projectedDistance = 0f;

            float stagger = _activeCount > 1 ? _profile.gatherDuration / (_activeCount - 1) : 0f;
            for (int i = 0; i < _activeCount; i++)
            {
                _pool[i].StartDelay = stagger * i;
                _pool[i].PathDistance = 0f;
            }
        }

        public void ReplacePath(GuidePath path)
        {
            if (_mode != Mode.Guide || path == null || !path.IsComplete)
                return;

            _path = path;
            _projectedDistance = 0f;
            for (int i = 0; i < _activeCount; i++)
                _pool[i].PathDistance = path.ProjectDistance(_pool[i].Transform.position, 0f, out _);
        }

        public void Tick(float deltaTime)
        {
            if (_mode == Mode.Idle)
                return;

            _elapsed += deltaTime;
            if (_elapsed >= _totalDuration)
                _isStopRequested = true;

            bool anyAlive = false;
            for (int i = 0; i < _activeCount; i++)
                anyAlive |= TickGust(_pool[i], deltaTime);

            if (!anyAlive && _isStopRequested)
                Stop(true);
        }

        public void Stop(bool immediate)
        {
            if (_mode == Mode.Idle)
                return;

            if (!immediate)
            {
                // 가닥은 더 나아가지 않고 꼬리가 사라지면 끝난다.
                _isStopRequested = true;
                return;
            }

            for (int i = 0; i < _pool.Count; i++)
                _pool[i].Transform.gameObject.SetActive(false);

            _activeCount = 0;
            _mode = Mode.Idle;
            _path = null;
        }

        public void Dispose()
        {
            Stop(true);
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].Transform != null)
                    UnityEngine.Object.Destroy(_pool[i].Transform.gameObject);
            }

            _pool.Clear();
        }

        #region 재생

        // 살아 있으면 true. 도착하거나 멈춤 요청을 받으면 꼬리가 사라질 때까지만 남긴다.
        private bool TickGust(Gust gust, float deltaTime)
        {
            if (gust.FinishedAt >= 0f)
                return _elapsed < gust.FinishedAt;

            if (_isStopRequested)
            {
                FinishGust(gust);
                return true;
            }

            if (!gust.IsStarted)
            {
                if (_elapsed < gust.StartDelay)
                    return true;

                _projectedDistance = Mathf.Max(
                    _projectedDistance,
                    _path.ProjectDistance(_player.position, _projectedDistance, out _));
                gust.PathDistance = _projectedDistance;
                StartGust(gust, SamplePosition(gust));
            }

            AdvanceGust(gust, deltaTime);
            return true;
        }

        private void AdvanceGust(Gust gust, float deltaTime)
        {
            _projectedDistance = Mathf.Max(
                _projectedDistance,
                _path.ProjectDistance(_player.position, _projectedDistance, out _));

            float limit = Mathf.Min(_path.TotalLength, _projectedDistance + _profile.leadDistanceEnd);
            gust.PathDistance = Mathf.Min(gust.PathDistance + _profile.maxSpeed * SpeedRatio * deltaTime, limit);
            gust.Transform.position = SamplePosition(gust);

            if (gust.PathDistance >= limit - 0.01f)
                FinishGust(gust);
        }

        private Vector3 SamplePosition(Gust gust)
        {
            _path.Sample(gust.PathDistance, out Vector3 position, out Vector3 tangent);
            Vector3 right = Vector3.Cross(Vector3.up, tangent);
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;

            float wave = _elapsed * _profile.flutterFrequency * Mathf.PI * 2f + gust.Phase;
            return position
                   + right * (Mathf.Sin(wave) * _profile.flutterAmplitude)
                   + Vector3.up * (_profile.flightHeight * HeightRatio + Mathf.Sin(wave * 0.7f) * _profile.flutterAmplitude * 0.5f);
        }

        private void StartGust(Gust gust, Vector3 position)
        {
            gust.IsStarted = true;
            gust.FinishedAt = -1f;
            gust.Transform.position = position;
            gust.Transform.gameObject.SetActive(true);
            gust.Trail.Clear();
        }

        private void FinishGust(Gust gust)
        {
            gust.FinishedAt = _elapsed + Mathf.Max(MinTrailTime, gust.Trail.time);
        }

        #endregion

        #region 준비

        private bool PrepareGusts(in GuideContext context, int count)
        {
            Stop(true);

            if (context.Profile == null || context.Profile.agentPrefab == null || context.Player == null)
                return false;

            _profile = context.Profile;
            _player = context.Player;
            _root = context.EffectRoot;
            _elapsed = 0f;
            _isStopRequested = false;

            while (_pool.Count < count)
            {
                Gust gust = CreateGust();
                if (gust == null)
                    return false;
                _pool.Add(gust);
            }

            _activeCount = count;
            for (int i = 0; i < _activeCount; i++)
            {
                _pool[i].IsStarted = false;
                _pool[i].FinishedAt = -1f;
                _pool[i].Phase = UnityEngine.Random.value * Mathf.PI * 2f;
            }

            return true;
        }

        private Gust CreateGust()
        {
            if (_profile.agentPrefab.GetComponentInChildren<TrailRenderer>(true) == null)
            {
                Debug.LogWarning("[WindGuideEffect] 바람 프리팹에 TrailRenderer가 없어 연출을 시작하지 못했습니다.");
                return null;
            }

            GameObject instance = UnityEngine.Object.Instantiate(_profile.agentPrefab, _root);
            instance.SetActive(false);
            return new Gust
            {
                Transform = instance.transform,
                Trail = instance.GetComponentInChildren<TrailRenderer>(true),
            };
        }

        #endregion
    }
}
