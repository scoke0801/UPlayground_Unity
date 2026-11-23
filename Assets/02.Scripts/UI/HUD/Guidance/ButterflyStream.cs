using UnityEngine;
using UPlayGround.Data.UI;

namespace UPlayGround.UI.Guidance
{
    /// <summary>
    /// 플레이어 바로 앞에서 목표 쪽으로 경로를 따라 끊임없이 흘러가는 나비 행렬.
    /// 한 마리가 구간 끝에 닿으면 플레이어 앞에서 다시 날아올라, 경로 전체에 나비가 늘 흩어져 있게 한다.
    /// 그래서 나비 궤적만 봐도 길의 굽이가 읽힌다. 지나간 자리에는 잔광을 떨어뜨려 흐름이 오래 남는다.
    /// 시뮬레이션은 미리 할당한 배열로만 하고, 화면 표시는 파티클 시스템 두 개(본체·잔광)에 한 번에 쓴다.
    /// 표시 프리팹이 없어도 시뮬레이션은 돌아가므로 편집 모드 테스트로 흐름을 검증할 수 있다.
    /// </summary>
    public sealed class ButterflyStream
    {
        private const float AppearDuration = 0.35f;
        private const float ParticleLifetime = 1000f;
        private const float MinFlapWidth = 0.2f;
        private const float SwayDegrees = 18f;
        private const float RespawnJitter = 1f;
        private const float VerticalFlutterRatio = 0.5f;
        private const float VerticalFlutterFrequencyRatio = 1.7f;

        private struct Butterfly
        {
            public bool IsAlive;
            public bool IsReleased;
            public float ReleaseTime;
            public float Age;
            public float PathDistance;
            public float Speed;
            public float Phase;
            public float FlapFrequency;
            public Vector2 Lateral;
            public Vector3 Position;
            public Color32 Color;
            public float AfterglowTimer;
        }

        private Butterfly[] _butterflies = new Butterfly[0];
        private ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[0];
        private ParticleSystem _body;
        private ParticleSystem _afterglow;
        private ParticleSystem.EmitParams _afterglowEmit;
        private GuideProfileSO _profile;
        private GuidePath _path;
        private Camera _camera;
        private float _screenSizeFactor;
        private int _count;
        private bool _canRespawn;

        /// <summary>지금 날고 있는 나비 수.</summary>
        public int AliveCount { get; private set; }

        /// <summary>이번 재생에 쓰는 나비 수(아직 날아오르지 않은 개체 포함).</summary>
        public int Count => _count;

        /// <summary>날고 있는 나비의 경로상 거리 범위. 날고 있는 개체가 없으면 false.</summary>
        public bool TryGetDistanceRange(out float nearest, out float farthest)
        {
            nearest = float.MaxValue;
            farthest = float.MinValue;
            for (int i = 0; i < _count; i++)
            {
                if (!_butterflies[i].IsAlive)
                    continue;
                nearest = Mathf.Min(nearest, _butterflies[i].PathDistance);
                farthest = Mathf.Max(farthest, _butterflies[i].PathDistance);
            }

            return AliveCount > 0;
        }

        /// <summary>새 재생을 준비한다. 나비는 releaseDuration 동안 차례로 플레이어 앞에서 날아오른다.</summary>
        public void Begin(GuideProfileSO profile, GuidePath path, Transform root, Camera camera)
        {
            Clear();
            _profile = profile;
            _path = path;
            _camera = camera;
            _count = Mathf.Max(0, profile.streamCount);
            _canRespawn = true;
            _screenSizeFactor = camera != null ? 2f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) : 0f;

            EnsureCapacity(_count);
            EnsureRenderers(root);

            float releaseStep = _count > 0 ? profile.streamReleaseDuration / _count : 0f;
            for (int i = 0; i < _count; i++)
            {
                _butterflies[i] = default;
                _butterflies[i].ReleaseTime = i * releaseStep;
            }

            if (_body != null)
            {
                _body.gameObject.SetActive(true);
                _body.Play();
            }

            if (_afterglow != null)
            {
                _afterglow.gameObject.SetActive(true);
                _afterglow.Play();
            }
        }

        /// <summary>새 경로로 갈아 끼운다. 나는 중인 나비는 지금 위치에서 가장 가까운 새 경로 지점부터 이어서 난다.</summary>
        public void ReplacePath(GuidePath path)
        {
            _path = path;
            for (int i = 0; i < _count; i++)
            {
                if (_butterflies[i].IsAlive)
                    _butterflies[i].PathDistance = path.ProjectDistance(_butterflies[i].Position, 0f, out _);
            }
        }

        /// <summary>
        /// 구간 끝에 닿은 나비를 다시 띄우지 않는다. 이미 날고 있는 나비는 끝까지 흘러가며 사라진다.
        /// </summary>
        public void StopRespawning()
        {
            _canRespawn = false;
        }

        /// <summary>
        /// 한 프레임 진행한다. projectedDistance는 플레이어가 경로상 도달한 거리, presence는 전체 연출의 등장·퇴장 정도(0~1)다.
        /// </summary>
        public void Tick(float deltaTime, float elapsed, float projectedDistance, float presence)
        {
            if (_path == null || _count == 0)
                return;

            float pathLength = _path.TotalLength;
            float near = Mathf.Clamp(projectedDistance + _profile.streamNearOffset, 0f, pathLength);
            float head = Mathf.Min(pathLength, projectedDistance + _profile.streamLength);
            Vector3 cameraPosition = _camera != null ? _camera.transform.position : Vector3.zero;

            int alive = 0;
            for (int i = 0; i < _count; i++)
            {
                ref Butterfly butterfly = ref _butterflies[i];
                if (!butterfly.IsReleased)
                {
                    if (elapsed < butterfly.ReleaseTime || !_canRespawn)
                        continue;
                    butterfly.IsReleased = true;
                    Spawn(ref butterfly, near);
                }
                else if (!butterfly.IsAlive)
                {
                    continue;
                }

                butterfly.Age += deltaTime;
                butterfly.PathDistance += butterfly.Speed * deltaTime;
                if (butterfly.PathDistance >= head)
                {
                    if (!_canRespawn)
                    {
                        butterfly.IsAlive = false;
                        continue;
                    }

                    Spawn(ref butterfly, near);
                }

                butterfly.Position = ComputePosition(ref butterfly, elapsed, pathLength);
                float alpha = presence * ComputeEdgeFade(ref butterfly, head);
                WriteParticle(alive, ref butterfly, elapsed, alpha, cameraPosition);
                EmitAfterglow(ref butterfly, deltaTime, alpha);
                alive++;
            }

            AliveCount = alive;
            if (_body != null)
                _body.SetParticles(_particles, alive);
        }

        /// <summary>모든 나비와 잔광을 즉시 지운다. 표시 개체는 다음 재생을 위해 남겨 둔다.</summary>
        public void Clear()
        {
            for (int i = 0; i < _count; i++)
                _butterflies[i].IsAlive = false;
            AliveCount = 0;
            _path = null;
            ClearRenderer(_body);
            ClearRenderer(_afterglow);
        }

        /// <summary>표시 개체까지 모두 해제한다.</summary>
        public void Dispose()
        {
            Clear();
            if (_body != null)
                UnityEngine.Object.Destroy(_body.gameObject);
            if (_afterglow != null)
                UnityEngine.Object.Destroy(_afterglow.gameObject);
            _body = null;
            _afterglow = null;
        }

        #region 비행

        private void Spawn(ref Butterfly butterfly, float near)
        {
            butterfly.IsAlive = true;
            butterfly.Age = 0f;
            butterfly.PathDistance = near + UnityEngine.Random.value * RespawnJitter;
            float variance = _profile.streamSpeedVariance;
            butterfly.Speed = _profile.streamSpeed * (1f + UnityEngine.Random.Range(-variance, variance));
            butterfly.Phase = UnityEngine.Random.value * Mathf.PI * 2f;
            butterfly.FlapFrequency = _profile.streamFlapFrequency * UnityEngine.Random.Range(0.8f, 1.2f);
            butterfly.Lateral = new Vector2(
                UnityEngine.Random.Range(-_profile.streamLateralSpread, _profile.streamLateralSpread),
                UnityEngine.Random.Range(-_profile.streamHeightSpread, _profile.streamHeightSpread));
            butterfly.Color = _profile.streamColors != null
                ? (Color32)_profile.streamColors.Evaluate(UnityEngine.Random.value)
                : new Color32(255, 255, 255, 255);
            butterfly.AfterglowTimer = UnityEngine.Random.value;
        }

        private Vector3 ComputePosition(ref Butterfly butterfly, float elapsed, float pathLength)
        {
            _path.Sample(butterfly.PathDistance, out Vector3 pathPosition, out Vector3 tangent);
            Vector3 right = Vector3.Cross(Vector3.up, tangent);
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;

            float wave = elapsed * _profile.flutterFrequency * Mathf.PI * 2f + butterfly.Phase;
            float side = butterfly.Lateral.x + Mathf.Sin(wave) * _profile.streamFlutterAmplitude;
            float height = _profile.flightHeight + butterfly.Lateral.y
                + Mathf.Sin(wave * VerticalFlutterFrequencyRatio) * _profile.streamFlutterAmplitude * VerticalFlutterRatio;
            Vector3 onPath = pathPosition + right * side + Vector3.up * height;

            // 목표 근처에서는 목표 위를 감싸 돌며 모여들어, 길의 끝이 어디인지 보여 준다.
            float arrive = 1f - Mathf.Clamp01((pathLength - butterfly.PathDistance) / _profile.arriveRadius);
            if (arrive <= 0f)
                return onPath;

            float orbitAngle = elapsed * _profile.arriveOrbitSpeed + butterfly.Phase;
            float orbitRadius = _profile.arriveOrbitRadius + Mathf.Abs(butterfly.Lateral.x);
            Vector3 orbit = _path.EndPoint + Vector3.up * height
                + new Vector3(Mathf.Cos(orbitAngle), 0f, Mathf.Sin(orbitAngle)) * orbitRadius;
            return Vector3.Lerp(onPath, orbit, Mathf.SmoothStep(0f, 1f, arrive));
        }

        // 막 날아오른 나비는 짧게 나타나고, 구간 끝에 가까워질수록 흐려진다.
        private float ComputeEdgeFade(ref Butterfly butterfly, float head)
        {
            float appear = Mathf.Clamp01(butterfly.Age / AppearDuration);
            float edge = _profile.streamEdgeFadeDistance;
            float vanish = edge > 0f ? Mathf.Clamp01((head - butterfly.PathDistance) / edge) : 1f;
            return Mathf.Min(appear, vanish);
        }

        // 화면에서 일정 크기 아래로 작아지지 않게 해, 멀리 날아간 나비도 점이 아니라 나비로 보이게 한다.
        private float ComputeWorldSize(Vector3 position, Vector3 cameraPosition)
        {
            if (_camera == null)
                return _profile.streamSize;

            float distance = Vector3.Distance(cameraPosition, position);
            return Mathf.Max(_profile.streamSize, distance * _screenSizeFactor * _profile.streamMinScreenFraction);
        }

        #endregion

        #region 표시

        // 날갯짓은 셰이더 없이 가로 폭을 접었다 펴는 것으로 흉내 낸다. 빌보드라 어느 각도에서도 나비 모양 전체가 보인다.
        private void WriteParticle(int index, ref Butterfly butterfly, float elapsed, float alpha, Vector3 cameraPosition)
        {
            if (_body == null)
                return;

            float size = ComputeWorldSize(butterfly.Position, cameraPosition);
            float flapPhase = elapsed * butterfly.FlapFrequency * Mathf.PI * 2f + butterfly.Phase;
            float flap = Mathf.Lerp(MinFlapWidth, 1f, Mathf.Abs(Mathf.Sin(flapPhase)));

            Color32 color = butterfly.Color;
            color.a = (byte)(color.a * Mathf.Clamp01(alpha));

            ref ParticleSystem.Particle particle = ref _particles[index];
            particle.position = butterfly.Position;
            particle.velocity = Vector3.zero;
            particle.startSize3D = new Vector3(size * flap, size, size);
            particle.rotation = Mathf.Sin(flapPhase * 0.25f) * SwayDegrees;
            particle.startColor = color;
            particle.startLifetime = ParticleLifetime;
            particle.remainingLifetime = ParticleLifetime;
            particle.randomSeed = (uint)index;
        }

        // 나비가 지나간 자리에 천천히 떠오르며 사라지는 빛가루를 남겨, 흐름이 몇 초 동안 선처럼 남게 한다.
        private void EmitAfterglow(ref Butterfly butterfly, float deltaTime, float alpha)
        {
            if (_afterglow == null || _profile.afterglowRate <= 0f || !_canRespawn)
                return;

            butterfly.AfterglowTimer += deltaTime * _profile.afterglowRate;
            if (butterfly.AfterglowTimer < 1f)
                return;

            butterfly.AfterglowTimer -= 1f;
            Color32 color = butterfly.Color;
            color.a = (byte)(color.a * Mathf.Clamp01(alpha));

            _afterglowEmit.position = butterfly.Position;
            _afterglowEmit.velocity = new Vector3(0f, _profile.afterglowRiseSpeed, 0f);
            _afterglowEmit.startSize = _profile.streamSize * _profile.afterglowSizeRatio;
            _afterglowEmit.startLifetime = _profile.afterglowLifetime;
            _afterglowEmit.startColor = color;
            _afterglow.Emit(_afterglowEmit, 1);
        }

        #endregion

        #region 준비

        private void EnsureCapacity(int count)
        {
            if (_butterflies.Length < count)
            {
                _butterflies = new Butterfly[count];
                _particles = new ParticleSystem.Particle[count];
            }
        }

        private void EnsureRenderers(Transform root)
        {
            if (_body == null)
                _body = CreateSystem(_profile.streamPrefab, root, _count);
            else
                RaiseMaxParticles(_body, _count);

            if (_afterglow == null)
                _afterglow = CreateSystem(_profile.afterglowPrefab, root, 0);

            _afterglowEmit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
        }

        // 수동으로 위치를 쓰는 데 필요한 설정만 강제하고, 모양·재질·곡선은 프리팹이 정한다.
        private static ParticleSystem CreateSystem(GameObject prefab, Transform root, int minParticles)
        {
            if (prefab == null)
                return null;

            GameObject instance = UnityEngine.Object.Instantiate(prefab, root);
            ParticleSystem system = instance.GetComponentInChildren<ParticleSystem>(true);
            if (system == null)
            {
                Debug.LogWarning($"[ButterflyStream] '{prefab.name}' 프리팹에 ParticleSystem이 없어 표시하지 않습니다.");
                UnityEngine.Object.Destroy(instance);
                return null;
            }

            ParticleSystem.MainModule main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.startSize3D = minParticles > 0 || main.startSize3D;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            RaiseMaxParticles(system, minParticles);
            instance.SetActive(false);
            return system;
        }

        private static void RaiseMaxParticles(ParticleSystem system, int minParticles)
        {
            ParticleSystem.MainModule main = system.main;
            if (main.maxParticles < minParticles)
                main.maxParticles = minParticles;
        }

        private static void ClearRenderer(ParticleSystem system)
        {
            if (system == null)
                return;
            system.Clear(true);
            system.gameObject.SetActive(false);
        }

        #endregion
    }
}
