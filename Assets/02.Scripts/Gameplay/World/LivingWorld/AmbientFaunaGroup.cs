using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.Data.World;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    public enum AmbientFaunaGroupState { Disabled, Dormant, Active, Retiring, Faulted }

    /// <summary>고정 서식지의 GPU 이동·Indirect 출력과 버퍼 수명을 그룹 단위로 소유한다.</summary>
    [DisallowMultipleComponent]
    public sealed partial class AmbientFaunaGroup : MonoBehaviour
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct ParticleState
        {
            public Vector4 PositionSize;
            public Vector4 VelocityPhase;
            public Vector4 HeadingSeed;
        }
        public const int ParticleStride = 48;
        [SerializeField] private AmbientFaunaProfileSO _profile;
        [SerializeField, Range(1, 4096)] private int _capacity = 32;
        [SerializeField] private uint _seed = 1;
        [SerializeField] private Vector4[] _anchors = Array.Empty<Vector4>();
        [SerializeField] private Matrix4x4 _bakedLocalToWorld = Matrix4x4.identity;
        [SerializeField] private WildlifeCreature[] _cpuFallbacks = Array.Empty<WildlifeCreature>();
        private WildlifeCreature[] _preparedFallbacks = Array.Empty<WildlifeCreature>();
        private bool[] _fallbackWasActive;
        private bool _hasFallbackOwnership, _isSuspended, _hasReservation;
        private AmbientFaunaHabitat _habitat;
        private GraphicsBuffer _particles, _anchorBuffer, _arguments;
        private ComputeShader _simulation;
        private MaterialPropertyBlock _properties;
        private int _initializeKernel, _simulateKernel, _lastFrame = -1;
        private float _fade, _density, _simulationTime;
        private Bounds _localBounds;
        private bool _needsPreparation;
        private AmbientFaunaProfileSO _preparedProfile;
        private uint _preparedRevision;
        private int _preparedFrame, _lastRendererFrame = -1, _lastVisibleFrame = -1;
        private readonly Collider[] _obstacleProbe = new Collider[32];
        private readonly Vector4[] _localInfluences = new Vector4[AmbientFaunaHabitat.InfluenceCapacity];
        private readonly Vector4[] _influenceStrengths = new Vector4[AmbientFaunaHabitat.InfluenceCapacity];
        private readonly Vector4[] _influenceEnds = new Vector4[AmbientFaunaHabitat.InfluenceCapacity];
        public AmbientFaunaProfileSO Profile => _profile;
        public int Capacity => _capacity;
        public bool IsBudgetEligible { get; internal set; } = true;
        internal float GetHabitatDensity() => _habitat != null && _habitat.IsRegionActive ? _habitat.EvaluateDensity(_profile) : 0;
        internal bool CanActivateForBudget(UnityEngine.Camera camera) => HasRecentVisibility && (camera == null
            || WorldBounds.SqrDistance(camera.transform.position) <= _profile.ReleaseDistance * _profile.ReleaseDistance);
        private bool HasRecentVisibility => _lastVisibleFrame >= 0 && Time.frameCount - _lastVisibleFrame <= 2;

        public bool IsReady { get; private set; }
        public AmbientFaunaGroupState State { get; private set; }
        public int AllocatedCapacity => _particles != null ? _particles.count : 0;
        public long BufferBytes => _particles == null ? 0 : (long)_particles.count * ParticleStride
            + (long)_anchors.Length * 16 + GraphicsBuffer.IndirectDrawIndexedArgs.size;
        public uint Generation { get; private set; }
        public Bounds WorldBounds => TransformBounds(_localBounds, transform.localToWorldMatrix);
        public float DisplayDensity => _density * _fade;
        internal bool CanRender => IsReady && !_isSuspended && _particles != null && _fade > 0 && isActiveAndEnabled;
        internal GraphicsBuffer ParticleBuffer => _particles;
        internal GraphicsBuffer ArgumentBuffer => _arguments;
        internal Mesh DrawMesh => _profile.Mesh;
        internal Material DrawMaterial => _profile.Material;
        internal MaterialPropertyBlock DrawProperties => _properties;
        private readonly Vector3[] _ambientDirections = { Vector3.up };
        private readonly Color[] _ambientColors = new Color[1];
#if UNITY_EDITOR
        public GraphicsBuffer DebugParticleBuffer => _particles;
        public uint DebugSimulationDispatchCount { get; private set; }
#endif

        /// <summary>저작 도구에서 안전 영역을 설정하고 고정 좌표를 베이크한다.</summary>
        public void SetBakedAnchors(Vector4[] anchors)
        {
            if (Application.isPlaying) throw new InvalidOperationException("플레이 중 앵커를 베이크할 수 없습니다.");
            _anchors = anchors != null ? (Vector4[])anchors.Clone() : Array.Empty<Vector4>();
            _bakedLocalToWorld = transform.localToWorldMatrix;
            CaptureBakedGeometry();
        }

        /// <summary>개체 수·프로필·CPU 권위·안전 앵커를 GPU 할당 전에 검사한다.</summary>
        public bool TryPrepare(out string error)
        {
            ReleaseGpu();
            RestoreFallbacks();
            IsReady = false;
            State = AmbientFaunaGroupState.Faulted;
            _needsPreparation = false;
            _preparedProfile = _profile;
            _preparedRevision = _profile != null ? _profile.Revision : 0;
            if (GetComponentsInChildren<WildlifeCreature>(true).Length > 0)
                return Fail("GPU 그룹 내부에 CPU WildlifeCreature를 포함할 수 없습니다.", out error);
            if (_profile == null) return Fail("GPU 종 프로필이 연결되지 않았습니다.", out error);
            if (!_profile.TryValidate(out error)) return false;
            if (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsInstancing)
                return Fail("현재 그래픽 환경은 GPU 환경 생물을 지원하지 않습니다. CPU 원본을 유지합니다.", out error);
            if (_capacity < 1 || _capacity > 4096 || Marshal.SizeOf<ParticleState>() != ParticleStride)
                return Fail("GPU 개체 수 또는 48 byte 버퍼 계약이 유효하지 않습니다.", out error);

            if (!HasValidTransform()) return Fail("고정 좌표 베이크가 오래됐거나 단위 스케일이 아닙니다.", out error);
            var placement = GetComponent<AmbientFaunaPlacement>();
            if (placement != null && (placement.BakeVersion != 1 || placement.SettingsHash != placement.ComputeSettingsHash()))
                return Fail("배치 볼륨 또는 제외 영역 설정이 변경됐습니다. 다시 베이크하세요.", out error);
            if (!HasUnchangedGeometry()) return Fail("베이크한 정적 지형이 변경됐습니다. 다시 베이크하세요.", out error);
            if (_anchors == null || _anchors.Length == 0)
                return Fail("GPU 환경 생물의 유효 앵커가 0개입니다.", out error);
            for (int i = 0; i < _anchors.Length; i++)
            {
                Vector4 anchor = _anchors[i];
                if (!AmbientFaunaProfileSO.IsFinite(anchor.x) || !AmbientFaunaProfileSO.IsFinite(anchor.y)
                    || !AmbientFaunaProfileSO.IsFinite(anchor.z) || !AmbientFaunaProfileSO.IsFinite(anchor.w)
                    || anchor.w <= _profile.SizeRange.y)
                    return Fail("안전 앵커에 NaN/Infinity 또는 너무 작은 반경이 있습니다.", out error);
                Vector3 center = transform.TransformPoint(new Vector3(anchor.x, anchor.y, anchor.z));
                if (placement != null && placement.IntersectsExclusion(center, anchor.w))
                    return Fail("안전 앵커가 제외 영역에 겹칩니다.", out error);
                int obstacles = Physics.OverlapSphereNonAlloc(center, anchor.w, _obstacleProbe,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                if (obstacles == _obstacleProbe.Length) return Fail("안전 영역의 충돌체 검사 상한을 초과했습니다.", out error);
                for (int j = 0; j < obstacles; j++)
                {
                    Collider obstacle = _obstacleProbe[j];
                    if (obstacle.attachedRigidbody != null || obstacle.GetComponentInParent<GameActor>() != null
                        || obstacle.GetComponentInParent<WildlifeCreature>() != null) continue;
                    return Fail("베이크한 안전 구에 정적 충돌체가 들어왔습니다. 앵커를 다시 베이크하세요.", out error);
                }
            }
            for (int i = 0; i < _cpuFallbacks.Length; i++)
            {
                var cpu = _cpuFallbacks[i];
                if (cpu != null && (cpu.Profile == null || cpu.Profile.MaxHealth > 0 || cpu.Profile.DropTable != null))
                    return Fail("사냥·보상 권위를 가진 CPU 생물은 GPU 장식으로 대체할 수 없습니다.", out error);
                if (cpu != null && transform.IsChildOf(cpu.transform))
                    return Fail("CPU 원본의 하위에 대체 GPU 그룹을 배치할 수 없습니다.", out error);
                if (cpu == null) continue;
                var registered = AmbientFaunaRendererFeature.Groups;
                for (int j = 0; j < registered.Count; j++)
                {
                    var other = registered[j];
                    if (other == null || other == this || !other.IsReady) continue;
                    for (int k = 0; k < other._preparedFallbacks.Length; k++)
                        if (other._preparedFallbacks[k] == cpu)
                            return Fail("같은 CPU 원본을 두 GPU 그룹에 연결할 수 없습니다.", out error);
                }
            }
            _localBounds = CalculateLocalBounds(_anchors, _profile.SizeRange.y);
            _preparedFallbacks = (WildlifeCreature[])_cpuFallbacks.Clone();
            _fallbackWasActive = new bool[_cpuFallbacks.Length];
            _properties ??= new MaterialPropertyBlock();
            _fade = _density = _simulationTime = 0;
            _lastFrame = -1;
            _preparedFrame = Time.frameCount;
            _lastRendererFrame = -1;
            _preparedProfile = _profile;
            _preparedRevision = _profile.Revision;
            _needsPreparation = false;
            IsReady = true;
            State = AmbientFaunaGroupState.Dormant;
            error = null;
            return true;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            _habitat = GetComponentInParent<AmbientFaunaHabitat>(true);
            if (_habitat == null)
            {
                Debug.LogWarning("AmbientFaunaHabitat가 필요합니다.", this);
                return;
            }
            if (!TryPrepare(out string error))
            {
                Debug.LogWarning(error, this);
                return;
            }
            if (_habitat.TryRegister(this)) return;
            IsReady = false;
            State = AmbientFaunaGroupState.Faulted;
        }

        private void OnDisable()
        {
            ReleaseGpu();
            RestoreFallbacks();
            if (_habitat != null) _habitat.Unregister(this);
            _habitat = null;
            IsReady = false;
            State = AmbientFaunaGroupState.Disabled;
            Generation++;
        }

        private void OnValidate() => _needsPreparation = true;

        private void OnTransformParentChanged()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            OnDisable();
            OnEnable();
        }

        /// <summary>호스트 중지 시 GPU 자원과 CPU 원본의 소유권을 반환한다.</summary>
        public void SetSuspended(bool isSuspended)
        {
            _isSuspended = isSuspended;
            if (isSuspended)
            {
                ReleaseGpu(); RestoreFallbacks();
                _lastRendererFrame = _lastVisibleFrame = -1;
                State = IsReady ? AmbientFaunaGroupState.Dormant : AmbientFaunaGroupState.Disabled;
            }
            else _preparedFrame = Time.frameCount;
        }

        /// <summary>실제로 선택된 카메라의 출력 패스가 확인된 뒤에만 CPU 원본을 인계받는다.</summary>
        internal void MarkRendererAvailable()
        {
            if (!IsReady || _isSuspended) return;
            _lastRendererFrame = Time.frameCount;
            ClaimFallbacks();
        }

        /// <summary>실제로 그릴 수 있는 카메라의 시야 안에 들어온 프레임만 기록한다.</summary>
        internal void MarkVisible() => _lastVisibleFrame = Time.frameCount;

        /// <summary>품질 한도가 줄면 후순위 그룹의 실제 할당을 즉시 반환한다.</summary>
        internal void ReleaseForBudget() { ReleaseGpu(); State = AmbientFaunaGroupState.Dormant; }

        /// <summary>프레임당 한 번만 이동을 적분하고 모든 카메라에 같은 상태를 제출한다.</summary>
        public void ApplyWorldInputs(float deltaTime, Vector3 observer, bool hasObserver, Vector3 wind)
        {
            if (_isSuspended || _lastFrame == Time.frameCount) return;
            if (_needsPreparation || _profile != _preparedProfile
                || (_profile != null && _profile.Revision != _preparedRevision))
            {
                if (!TryPrepare(out string error)) { Debug.LogWarning(error, this); return; }
            }
            if (!IsReady) return;
            _lastFrame = Time.frameCount;
            if (Time.frameCount - Mathf.Max(_preparedFrame, _lastRendererFrame) > 2)
            {
                ReleaseGpu(); RestoreFallbacks(); State = AmbientFaunaGroupState.Dormant;
                return;
            }
            // 렌더러는 정상이어도 화면 밖 군집은 GPU를 반환한다. CPU fallback은 다시 켜지 않는다.
            if (!HasRecentVisibility)
            {
                ReleaseGpu(); State = AmbientFaunaGroupState.Dormant;
                return;
            }
            if (!HasValidTransform())
            {
                ReleaseGpu();
                RestoreFallbacks();
                IsReady = false;
                State = AmbientFaunaGroupState.Faulted;
                Debug.LogWarning("GPU 서식지 좌표가 변경되어 중지했습니다. 앵커를 다시 베이크하세요.", this);
                return;
            }
            float distance = hasObserver ? WorldBounds.SqrDistance(observer) : 0;
            float activation = _particles == null ? _profile.ActivationDistance : _profile.ReleaseDistance;
            float targetDensity = IsBudgetEligible && _habitat.IsRegionActive && distance <= activation * activation
                ? _habitat.EvaluateDensity(_profile) * _habitat.CombatDensity : 0;
            float step = Mathf.Clamp(deltaTime, 0, 0.1f);
            float fadeStep = step / _profile.FadeSeconds;
            _density = Mathf.MoveTowards(_density, targetDensity, fadeStep);
            bool wantsActive = targetDensity > 0.001f;
            if (_particles == null && !wantsActive) { State = AmbientFaunaGroupState.Dormant; return; }
            if (_particles == null && !TryAllocate()) return;
            _fade = Mathf.MoveTowards(_fade, wantsActive ? 1 : 0, fadeStep);
            State = wantsActive ? AmbientFaunaGroupState.Active : AmbientFaunaGroupState.Retiring;
            if (!wantsActive && _fade <= 0) { ReleaseGpu(); State = AmbientFaunaGroupState.Dormant; return; }
            BindSimulationInputs(observer, hasObserver, wind);
            // 큰 프레임 지연을 작은 적분으로 나누되 catch-up 폭주를 제한한다.
            int steps = Mathf.Min(6, Mathf.CeilToInt(step * 60));
            for (int i = 0; i < steps; i++)
            {
                float frameDelta = step / steps;
                _simulationTime += frameDelta;
                _simulation.SetFloat(Ids.FrameDelta, frameDelta);
                _simulation.SetFloat(Ids.Time, _simulationTime);
                _simulation.Dispatch(_simulateKernel, (_capacity + 63) / 64, 1, 1);
#if UNITY_EDITOR
                DebugSimulationDispatchCount++;
#endif
            }
            _properties.SetMatrix(Ids.LocalToWorld, transform.localToWorldMatrix);
            _properties.SetFloat(Ids.Fade, _fade);
            _properties.SetFloat(Ids.Density, _density);
            _properties.SetFloat(Ids.Time, _simulationTime);
            RenderSettings.ambientProbe.Evaluate(_ambientDirections, _ambientColors);
            _properties.SetColor(Ids.Ambient, RenderSettings.ambientMode == AmbientMode.Flat
                ? RenderSettings.ambientLight : _ambientColors[0]);
        }

        private bool TryAllocate()
        {
            if (_habitat == null || !_habitat.TryReserve(this, _capacity)) return false;
            _hasReservation = true;
            try
            {
                _simulation = Instantiate(_profile.Simulation);
                _initializeKernel = _simulation.FindKernel("Initialize");
                _simulateKernel = _simulation.FindKernel("Simulate");
                _particles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _capacity, ParticleStride);
                _anchorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _anchors.Length, 16);
                _arguments = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
                _anchorBuffer.SetData(_anchors);
                _arguments.SetData(new[] { new GraphicsBuffer.IndirectDrawIndexedArgs {
                    indexCountPerInstance = _profile.Mesh.GetIndexCount(0), instanceCount = (uint)_capacity,
                    startIndex = _profile.Mesh.GetIndexStart(0), baseVertexIndex = (uint)_profile.Mesh.GetBaseVertex(0), startInstance = 0 } });
                _simulation.SetInt(Ids.Capacity, _capacity);
                _simulation.SetInt(Ids.AnchorCount, _anchors.Length);
                _simulation.SetInt(Ids.Seed, unchecked((int)_seed));
                _simulation.SetVector(Ids.SizeSpeed, new Vector4(_profile.SizeRange.x, _profile.SizeRange.y, _profile.SpeedRange.x, _profile.SpeedRange.y));
                _simulation.SetVector(Ids.Movement, new Vector4(_profile.Acceleration, _profile.FleeSpeed, _profile.AvoidRadius, _profile.WindInfluence));
                _simulation.SetFloat(Ids.FlapFrequency, _profile.FlapFrequency);
                _simulation.SetFloat(Ids.MeshRadius, _profile.Mesh.bounds.extents.magnitude + _profile.Mesh.bounds.center.magnitude);
                _simulation.SetInt(Ids.Behavior, (int)_profile.Behavior);
                _simulation.SetVector(Ids.Wander, new Vector4(_profile.WanderFrequency, _profile.WanderRadius, _profile.HoverFraction, _profile.RecoverySeconds));
                _simulation.SetFloat(Ids.Lifetime, _profile.ParticleLifetime);
                _simulation.SetBuffer(_initializeKernel, Ids.Particles, _particles);
                _simulation.SetBuffer(_initializeKernel, Ids.Anchors, _anchorBuffer);
                _simulation.SetBuffer(_simulateKernel, Ids.Particles, _particles);
                _simulation.SetBuffer(_simulateKernel, Ids.Anchors, _anchorBuffer);
                _simulation.Dispatch(_initializeKernel, (_capacity + 63) / 64, 1, 1);
                _properties.SetBuffer(Ids.Particles, _particles);
                _properties.SetFloat(Ids.FlapAngle, _profile.FlapAngle * Mathf.Deg2Rad);
                _properties.SetVector(Ids.NearFade, _profile.NearFade);
                _properties.SetFloat(Ids.Lifetime, _profile.ParticleLifetime);
                Generation++;
                return true;
            }
            catch (Exception exception)
            {
                ReleaseGpu();
                RestoreFallbacks();
                IsReady = false;
                State = AmbientFaunaGroupState.Faulted;
                Debug.LogError("GPU 환경 생물 준비 실패: " + exception.Message, this);
                return false;
            }
        }

        private void BindSimulationInputs(Vector3 observer, bool hasObserver, Vector3 wind)
        {
            _simulation.SetInt(Ids.HasObserver, hasObserver ? 1 : 0);
            _simulation.SetVector(Ids.Observer, hasObserver ? transform.InverseTransformPoint(observer) : Vector3.zero);
            _simulation.SetVector(Ids.ObserverEnd, hasObserver ? transform.InverseTransformPoint(observer + Vector3.up * _habitat.ObserverHeight) : Vector3.zero);
            _simulation.SetVector(Ids.ObserverVelocity, transform.InverseTransformDirection(_habitat.ObserverVelocity));
            _simulation.SetVector(Ids.Wind, transform.InverseTransformDirection(wind));
            int count = _habitat.CopyInfluences(transform, _localInfluences, _influenceStrengths);
            _habitat.CopyInfluenceEnds(transform, _influenceEnds);
            _simulation.SetInt(Ids.InfluenceCount, count);
            _simulation.SetVectorArray(Ids.Influences, _localInfluences);
            _simulation.SetVectorArray(Ids.InfluenceStrengths, _influenceStrengths);
            _simulation.SetVectorArray(Ids.InfluenceEnds, _influenceEnds);
        }

        private void ReleaseGpu()
        {
            if (_particles == null && _anchorBuffer == null && _arguments == null && _simulation == null && !_hasReservation) return;
            _particles?.Dispose(); _particles = null;
            _anchorBuffer?.Dispose(); _anchorBuffer = null;
            _arguments?.Dispose(); _arguments = null;
            if (_simulation != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_simulation);
                else UnityEngine.Object.DestroyImmediate(_simulation);
                _simulation = null;
            }
            if (_hasReservation && _habitat != null) _habitat.ReleaseReservation(this);
            _hasReservation = false;
            _fade = _simulationTime = 0;
            Generation++;
        }

        private void ClaimFallbacks()
        {
            if (_hasFallbackOwnership) return;
            for (int i = 0; i < _preparedFallbacks.Length; i++)
            {
                var cpu = _preparedFallbacks[i];
                _fallbackWasActive[i] = cpu != null && cpu.gameObject.activeSelf;
                if (_fallbackWasActive[i]) cpu.gameObject.SetActive(false);
            }
            _hasFallbackOwnership = true;
        }

        private void RestoreFallbacks()
        {
            if (!_hasFallbackOwnership) return;
            for (int i = 0; i < _preparedFallbacks.Length; i++)
                if (_preparedFallbacks[i] != null && _fallbackWasActive[i]) _preparedFallbacks[i].gameObject.SetActive(true);
            _hasFallbackOwnership = false;
        }

        private bool HasValidTransform()
        {
            if ((transform.lossyScale - Vector3.one).sqrMagnitude > 0.000001f) return false;
            Matrix4x4 current = transform.localToWorldMatrix;
            for (int i = 0; i < 16; i++)
                if (!AmbientFaunaProfileSO.IsFinite(current[i]) || !AmbientFaunaProfileSO.IsFinite(_bakedLocalToWorld[i])
                    || Mathf.Abs(current[i] - _bakedLocalToWorld[i]) > 0.001f) return false;
            return true;
        }

        private static bool Fail(string message, out string error) { error = message; return false; }

        /// <summary>날개가 펼쳐진 최대 크기까지 포함하는 로컬 컬링 경계를 계산한다.</summary>
        public static Bounds CalculateLocalBounds(Vector4[] anchors, float size)
        {
            Bounds bounds = new(new Vector3(anchors[0].x, anchors[0].y, anchors[0].z), Vector3.zero);
            for (int i = 0; i < anchors.Length; i++)
            {
                Vector3 center = new(anchors[i].x, anchors[i].y, anchors[i].z);
                Vector3 radius = Vector3.one * (anchors[i].w + size);
                bounds.Encapsulate(center - radius);
                bounds.Encapsulate(center + radius);
            }
            return bounds;
        }

        private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            Vector3 size = bounds.extents;
            Vector3 x = matrix.MultiplyVector(new Vector3(size.x, 0, 0));
            Vector3 y = matrix.MultiplyVector(new Vector3(0, size.y, 0));
            Vector3 z = matrix.MultiplyVector(new Vector3(0, 0, size.z));
            return new Bounds(matrix.MultiplyPoint3x4(bounds.center), 2 * new Vector3(
                Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x), Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
                Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)));
        }

        private static class Ids
        {
            public static readonly int Behavior = Shader.PropertyToID("_AF_Behavior"), Wander = Shader.PropertyToID("_AF_Wander"), Lifetime = Shader.PropertyToID("_AF_Lifetime"), ObserverEnd = Shader.PropertyToID("_AF_ObserverEnd"), InfluenceEnds = Shader.PropertyToID("_AF_InfluenceEnds");
            public static readonly int Particles = Shader.PropertyToID("_AF_Particles"), Anchors = Shader.PropertyToID("_AF_Anchors"),
                Capacity = Shader.PropertyToID("_AF_Capacity"), AnchorCount = Shader.PropertyToID("_AF_AnchorCount"), Seed = Shader.PropertyToID("_AF_Seed"),
                SizeSpeed = Shader.PropertyToID("_AF_SizeSpeed"), Movement = Shader.PropertyToID("_AF_Movement"),
                Observer = Shader.PropertyToID("_AF_ObserverLocal"), ObserverVelocity = Shader.PropertyToID("_AF_ObserverVelocity"),
                HasObserver = Shader.PropertyToID("_AF_HasObserver"), Wind = Shader.PropertyToID("_AF_WindLocal"),
                FrameDelta = Shader.PropertyToID("_AF_FrameDelta"), Time = Shader.PropertyToID("_AF_Time"),
                FlapFrequency = Shader.PropertyToID("_AF_FlapFrequency"), MeshRadius = Shader.PropertyToID("_AF_MeshRadius"),
                LocalToWorld = Shader.PropertyToID("_AF_LocalToWorld"),
                FlapAngle = Shader.PropertyToID("_AF_FlapAngle"), NearFade = Shader.PropertyToID("_AF_NearFade"),
                Fade = Shader.PropertyToID("_AF_Fade"), Density = Shader.PropertyToID("_AF_Density"),
                Ambient = Shader.PropertyToID("_AF_AmbientColor"),
                InfluenceCount = Shader.PropertyToID("_AF_InfluenceCount"), Influences = Shader.PropertyToID("_AF_Influences"),
                InfluenceStrengths = Shader.PropertyToID("_AF_InfluenceStrengths");
        }
    }
}
