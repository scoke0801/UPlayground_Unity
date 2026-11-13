using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Gameplay.World.LivingWorld;
using UPlayGround.Manager;
using UPlayGround.Data.Save;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>기존 피격 경계를 환경 상태 작성자에 연결하고 저장된 표현만 적용한다.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(SceneEntityId))]
    public sealed class ReactiveEnvironmentObject : MonoBehaviour, IDamageable, IEnvironmentReactionTarget, IReactiveGatheringTarget, IEnvironmentColliderFilter
    {
        [SerializeField] private ReactiveEnvironmentDefinitionSO _definition;
        [SerializeField] private EnvironmentController _weather;
        [SerializeField] private GameObject _intact;
        [SerializeField] private GameObject _remnant;
        [SerializeField] private Transform[] _rainSamples;
        [SerializeField] private Transform[] _heatSamples;
        [SerializeField] private Renderer[] _surfaces;
        [SerializeField] private GameObject _flame;
        [SerializeField] private EnvironmentSurfaceBinding[] _surfaceBindings;
        [SerializeField] private Renderer[] _excludedSurfaces;
        [SerializeField] private Collider[] _hitColliders;
        [SerializeField] private bool _ownsGathering;
        [SerializeField] private Transform[] _burnAnchors;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _impactAnchor;
        [SerializeField] private Transform _landingAnchor;
        private EnvironmentCombustionPresentation[] _combustionEffects;
        private Quaternion _visualRotation;
        private Vector3 _hitAxis;
        private float _hitRemaining, _hitCooldown;
        private bool _wasFalling;
        private bool _hasImpactPoint;
        private Vector3 _impactPoint;
        public bool IsAvailable => this != null;
        public string PersistenceSignature => null;
        public Transform OcclusionRoot => transform;
        /// <summary>명시한 피격 Collider만 전투 검출기에 허용한다.</summary>
        public bool CanReceiveCollider(Collider collider)
        {
            if (_hitColliders == null) return false;
            foreach (var candidate in _hitColliders) if (candidate == collider) return true;
            return false;
        }
        public bool OwnsGathering => _ownsGathering;
        public bool CanGather => _isBound && !State.isBroken;
        public float GatheringHealth => State.durability;
        public float GatheringMaximumHealth => _definition != null ? _definition.reaction.maxDurability : 0;
        /// <summary>채집도 환경 내구도·원인별 보상 원장 한 경로로 전달한다.</summary>
        public bool TryGather(float damage)
        {
            float before = State.durability;
            bool accepted = CanGather && _world.ApplyEnvironmentCombatHit(this, damage);
            if (accepted && State.durability < before)
                PresentImpact(_impactAnchor != null ? _impactAnchor.position : ReactionPosition, transform.forward, -transform.forward);
            return accepted;
        }
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _surfaceProperties;
        private Color[][] _surfaceColors;
        private WorldStateManager _world;
        private GatheringActor _gathering;
        public System.Collections.Generic.List<UPlayGround.Data.Item.ItemDropList> GatheringDrops => _gathering != null ? _gathering.GetData()?.dropItems : null;
        private SceneEntityId _identity;
        private bool _hasStarted;
        private bool _isBound;
        private ReactiveEnvironmentSurface _reactiveSurface;
        private ReactiveEnvironmentDestruction _destruction;
        private bool _hasAppliedSnapshot;
        private EnvironmentDestructionSaveState _appliedDestruction;
        public ReactiveEnvironmentDefinitionSO Definition => _definition;
        public string StableId => _identity != null ? _identity.Guid : null;
        public EnvironmentReactionState State { get; private set; }
        public Vector3 ReactionPosition
        {
            get
            {
                Transform[] samples = _heatSamples != null && _heatSamples.Length > 0 ? _heatSamples : _rainSamples;
                if (samples != null)
                    foreach (Transform sample in samples)
                        if (sample != null && sample.gameObject.activeInHierarchy) return sample.position;
                return transform.position;
            }
        }
        public Vector2 Wind => _weather != null && _weather.IsReady ? _weather.Current.Wind : Vector2.zero;

        /// <summary>에디터 요청 때만 실제 날씨 연결과 활성 샘플별 차폐를 조회한다.</summary>
        public string DescribeRain()
        {
            var text = new System.Text.StringBuilder();
            text.Append($"weather={(_weather != null ? _weather.name : "없음")} ready={(_weather != null && _weather.IsReady)} rain={(_weather != null ? _weather.Current.Rain : 0):F3}\n");
            if (_rainSamples != null)
                foreach (var sample in _rainSamples)
                {
                    if (sample == null) { text.AppendLine("누락 sample"); continue; }
                    bool active = sample.gameObject.activeInHierarchy;
                    text.AppendLine($"{sample.name} {sample.position} active={active} sheltered={(active && _weather != null && _weather.IsRainSheltered(sample.position, transform))}");
                }
            SampleRain(out float rain, out float exposure);
            text.Append($"exposure={exposure:F3} contact={rain * exposure * (_definition != null ? _definition.reaction.rainAcceptance : 0):F3} heatPoint={ReactionPosition}");
            return text.ToString();
        }

        private void Awake()
        {
            _identity = GetComponent<SceneEntityId>();
            _gathering = GetComponent<GatheringActor>();
            _destruction = GetComponent<ReactiveEnvironmentDestruction>();
            _destruction?.Initialize();
            if (_visualRoot != null) _visualRotation = _visualRoot.localRotation;
            _surfaceProperties = new MaterialPropertyBlock();
            _surfaces ??= System.Array.Empty<Renderer>();
            _surfaceColors = new Color[_surfaces.Length][];
            for (int i = 0; i < _surfaces.Length; i++)
            {
                var materials = _surfaces[i] != null ? _surfaces[i].sharedMaterials : System.Array.Empty<Material>();
                _surfaceColors[i] = new Color[materials.Length];
                for (int slot = 0; slot < materials.Length; slot++)
                    _surfaceColors[i][slot] = materials[slot] != null && materials[slot].HasProperty(BaseColor)
                        ? materials[slot].GetColor(BaseColor) : Color.white;
            }
        }
        private void Start() { _hasStarted = true; Bind(); }
        private void OnEnable() { if (_hasStarted) Bind(); }
        private void OnDisable()
        {
            if (_world != null && _isBound) _world.UnregisterEnvironment(this);
            _isBound = false;
            _reactiveSurface?.Release();
            if (_combustionEffects != null) foreach (var effect in _combustionEffects) effect.Release();
            _hitRemaining = _hitCooldown = 0; _wasFalling = false; _hasImpactPoint = false;
            if (_visualRoot != null) _visualRoot.localRotation = _visualRotation;
            _hasAppliedSnapshot = false;
        }
        private void OnDestroy() => _reactiveSurface?.Release();
        private void Bind()
        {
            if (_weather == null && Services.TryGet<UPlayGround.UI.IUIWeatherService>(out var weather))
                _weather = weather as EnvironmentController;
            _world = WorldStateManager.Instance;
            _isBound = _world != null && _world.RegisterEnvironment(this);
        }

        /// <summary>씬 배치와 명시 프로필의 누락·보상 이중 소유를 거부한다.</summary>
        public bool IsAuthoringValid()
        {
            if (_definition == null || !_definition.IsValid() || _identity == null || !_identity.HasGuid
                || !IsGatheringValid() || _weather == null
                || _flame == gameObject || _rainSamples == null || _rainSamples.Length == 0) return false;
            if (_definition.reaction.canBreak && (_intact == null || _remnant == null || _intact == _remnant
                || _intact == gameObject || _remnant == gameObject || _definition.destruction == null
                || !_intact.transform.IsChildOf(transform) || !_remnant.transform.IsChildOf(transform)
                || _intact.transform.IsChildOf(_remnant.transform) || _remnant.transform.IsChildOf(_intact.transform))) return false;
            if (_hitColliders != null)
                foreach (var collider in _hitColliders)
                    if (collider == null || collider.GetComponentInParent<ReactiveEnvironmentObject>(true) != this) return false;
            if (_flame != null && (_flame.transform.parent != transform || _flame == _intact || _flame == _remnant))
                return false;
            foreach (Transform sample in _rainSamples)
                if (sample == null || !sample.IsChildOf(transform)) return false;
            if (_visualRoot != null && (_visualRoot == transform || !_visualRoot.IsChildOf(transform)
                || _visualRoot.GetComponentsInChildren<Collider>(true).Length > 0)) return false;
            if (_burnAnchors != null)
                foreach (var anchor in _burnAnchors) if (anchor == null || !anchor.IsChildOf(transform)) return false;
            if (_impactAnchor != null && !_impactAnchor.IsChildOf(transform)
                || _landingAnchor != null && !_landingAnchor.IsChildOf(transform)) return false;
            if (_heatSamples != null)
                foreach (Transform sample in _heatSamples)
                    if (sample == null || !sample.IsChildOf(transform)) return false;
            if (_definition.surfaceBackend == EnvironmentSurfaceBackend.ReactiveToon
                && ReactiveEnvironmentSurface.Validate(transform, GetSurfaceBindings(), _excludedSurfaces) != null) return false;
            if (_definition.destruction != null && (_destruction == null || !_destruction.IsValid(_definition.destruction))) return false;
            return true;
        }

        private bool IsGatheringValid()
        {
            var gathering = GetComponent<GatheringActor>();
            _gathering = gathering;
            if (gathering == null) return !_ownsGathering;
            var data = gathering.GetData();
            return _ownsGathering && _definition.reaction.canBreak && data != null
                && data.interactionObjectType != UPlayGround.Data.EnumType.InteractionObjectType.FISHING_ZONE
                && data.regrowGameMinutes <= 0;
        }

        private EnvironmentSurfaceBinding[] GetSurfaceBindings() =>
            _surfaceBindings != null && _surfaceBindings.Length > 0 ? _surfaceBindings : ReactiveEnvironmentSurface.BindAll(_surfaces);

        /// <summary>외부 화염·물 명중은 실행 ID와 연타 번호를 유지해 전달한다.</summary>
        public bool TryApplyImpact(ulong attackId, uint sequence, float damage, float heat, float water)
        {
            float before = State.durability;
            bool accepted = _isBound && _world.ApplyEnvironmentImpact(this, attackId, sequence, damage, heat, water);
            if (accepted && State.durability < before)
                PresentImpact(_impactAnchor != null ? _impactAnchor.position : ReactionPosition, transform.forward, -transform.forward);
            return accepted;
        }

        /// <summary>시각 품질·카메라 실내 여부와 독립적인 CPU 강수 입력.</summary>
        public void SampleRain(out float rain, out float exposure)
        {
            rain = _weather != null && _weather.IsReady ? _weather.Current.Rain : 0;
            exposure = 0;
            if (rain <= 0 || _rainSamples == null) return;
            int count = 0;
            foreach (Transform sample in _rainSamples)
            {
                if (sample == null || !sample.gameObject.activeInHierarchy) continue;
                count++;
                if (!_weather.IsRainSheltered(sample.position, transform)) exposure++;
            }
            exposure = count > 0 ? exposure / count : 0;
        }

        /// <summary>파괴 후에도 같은 상태와 잔여 연료를 유지하며 root는 살아 있게 둔다.</summary>
        public void ApplySnapshot(EnvironmentReactionState state, EnvironmentDestructionSaveState destruction = null)
        {
            if (_hasAppliedSnapshot && State.HasSameValues(state) && ReferenceEquals(_appliedDestruction, destruction))
            { State = state; return; }
            bool changed = (_hasAppliedSnapshot && State.isBroken != state.isBroken) || !_hasAppliedSnapshot && state.isBroken;
            State = state;
            _appliedDestruction = destruction;
            _hasAppliedSnapshot = true;
            if (_definition.presentation != null && _burnAnchors != null)
            {
                if (_combustionEffects == null)
                {
                    _combustionEffects = new EnvironmentCombustionPresentation[_burnAnchors.Length];
                    for (int i = 0; i < _combustionEffects.Length; i++)
                        _combustionEffects[i] = new EnvironmentCombustionPresentation(_definition.presentation);
                }
                foreach (var effect in _combustionEffects) effect.Apply(state);
            }
            if (_definition.reaction.canBreak)
            {
                _intact.SetActive(!state.isBroken);
                _remnant.SetActive(state.isBroken);
            }
            _destruction?.Apply(_definition.destruction, destruction ?? new EnvironmentDestructionSaveState(), state.isBroken);
            if (changed && _world != null)
                _world.NotifyEnvironmentStructureChanged(this, GetReactionBounds());
            if (_flame != null) _flame.SetActive(state.combustion == EnvironmentCombustion.Burning);
            if (_definition.surfaceBackend == EnvironmentSurfaceBackend.ReactiveToon)
            {
                _reactiveSurface ??= new ReactiveEnvironmentSurface(GetSurfaceBindings());
                _reactiveSurface.Apply(state, _definition.reaction.maxDurability, _definition.reaction.initialFuel);
                if (_impactAnchor != null && _definition.presentation != null)
                    _reactiveSurface.ApplyWound(_hasImpactPoint ? _impactPoint : _impactAnchor.position,
                        _definition.presentation.GetWoundRadius(1 - GetHealthPercent()));
                return;
            }
            Color tint = Color.Lerp(Color.white, _definition.wetTint, state.wetness);
            tint = Color.Lerp(tint, _definition.charTint, state.charAmount);
            for (int i = 0; i < _surfaces.Length; i++)
            {
                if (_surfaces[i] == null) continue;
                for (int slot = 0; slot < _surfaceColors[i].Length; slot++)
                {
                    _surfaces[i].GetPropertyBlock(_surfaceProperties, slot);
                    _surfaceProperties.SetColor(BaseColor, _surfaceColors[i][slot] * tint);
                    _surfaces[i].SetPropertyBlock(_surfaceProperties, slot);
                }
            }
        }

        private Bounds GetReactionBounds()
        {
            var bounds = new Bounds(transform.position, Vector3.zero);
            foreach (var binding in GetSurfaceBindings())
                if (binding.renderer != null) bounds.Encapsulate(binding.renderer.bounds);
            return bounds;
        }

        /// <summary>기존 근접·투사체의 기계적 피해만 즉시 전달한다. 원소는 추측하지 않는다.</summary>
        public CombatResult ReceiveHit(in HitRequest request)
        {
            float before = State.durability;
            Vector3 normal = ResolveImpactNormal(request);
            _destruction?.SetImpactDirection(request.AttackDirection);
            bool accepted = CanTakeDamage() && _world.ApplyEnvironmentCombatHit(this, request.Damage, request.EnvironmentHit);
            float damage = accepted ? before - State.durability : 0;
            if (damage > 0) PresentImpact(request.HitPoint, request.AttackDirection, normal);
            return new CombatResult(new HitContext(request, null), new DefenseResult(DefenseOutcome.None, accepted),
                new DamageResult(request.Damage, damage, 1, 0, 1, 1, false, default), default, default);
        }
        public bool IsAlive() => _isBound;
        public bool CanTakeDamage() => IsAlive();
        public Transform GetTransform() => transform;
        public void LockOn() { }
        public void UnLockOn() { }
        public float GetHealthPercent() => _definition != null && _definition.reaction != null
            && _definition.reaction.maxDurability > 0 ? State.durability / _definition.reaction.maxDurability : 0;
        public float GetCurrentHealth() => State.durability;
        public void ApplyHealingEffect(float healAmount) { }

        /// <summary>중앙 물리 갱신에서만 방향성 파괴를 진행한다.</summary>
        public void FixedTick(float deltaTime)
        {
            bool falling = _appliedDestruction != null && _appliedDestruction.isFalling;
            _destruction?.FixedTick(deltaTime);
            if (_wasFalling && !falling && _landingAnchor != null)
            {
                Vector3 point = _landingAnchor.position;
                var profile = _definition.presentation;
                if (profile != null && Physics.Raycast(point + Vector3.up * profile.landingProbeDistance, Vector3.down,
                    out var ground, profile.landingProbeDistance * 2, profile.landingGroundLayers, QueryTriggerInteraction.Ignore))
                {
                    PlayBurst(profile.landing, ground.point, ground.normal, profile.landingParticles, profile.landingSound);
                    PlayBurst(profile.leaves, ground.point, ground.normal, profile.landingParticles, null);
                }
            }
            _wasFalling = falling;
            _hitCooldown = Mathf.Max(0, _hitCooldown - deltaTime);
            if (_visualRoot != null && _hitRemaining > 0)
            {
                _hitRemaining = Mathf.Max(0, _hitRemaining - deltaTime);
                float phase = 1 - _hitRemaining / _definition.presentation.hitSeconds;
                float angle = Mathf.Sin(phase * Mathf.PI * 2) * (1 - phase) * _definition.presentation.hitDegrees;
                _visualRoot.localRotation = Quaternion.AngleAxis(angle, _hitAxis) * _visualRotation;
            }
            if (_combustionEffects == null) return;
            int active = 0;
            foreach (var anchor in _burnAnchors) if (anchor != null && anchor.gameObject.activeInHierarchy) active++;
            for (int i = 0; i < _combustionEffects.Length; i++)
            {
                var anchor = _burnAnchors[i];
                if (anchor == null || !anchor.gameObject.activeInHierarchy) { _combustionEffects[i].Release(); continue; }
                _combustionEffects[i].Tick(deltaTime, anchor.position, Wind, 1f / Mathf.Max(1, active));
            }
        }

        private Vector3 ResolveImpactNormal(in HitRequest request)
        {
            Vector3 direction = request.AttackDirection.normalized;
            var profile = _definition.presentation;
            if (profile != null && _hitColliders != null && direction.sqrMagnitude > 0.5f)
                foreach (var collider in _hitColliders)
                    if (collider != null && collider.gameObject == request.HitTarget
                        && collider.Raycast(new Ray(request.HitPoint - direction * profile.contactProbeDistance, direction),
                            out var hit, profile.contactProbeDistance * 2)) return hit.normal;
            return -direction;
        }

        private void PresentImpact(Vector3 point, Vector3 direction, Vector3 normal)
        {
            var profile = _definition.presentation;
            if (profile == null || _hitCooldown > 0) return;
            _hasImpactPoint = true; _impactPoint = point;
            _hitCooldown = profile.hitCooldown;
            if (!State.isBroken && _visualRoot != null)
            {
                _hitAxis = _visualRoot.parent.InverseTransformDirection(Vector3.Cross(Vector3.up, direction));
                _hitRemaining = profile.hitSeconds;
            }
            _reactiveSurface?.ApplyWound(point, profile.GetWoundRadius(1 - GetHealthPercent()));
            PlayBurst(profile.impact, point, normal, profile.impactParticles, profile.impactSound);
            if (_burnAnchors != null)
                foreach (var anchor in _burnAnchors)
                    if (anchor != null && anchor.gameObject.activeInHierarchy)
                    { PlayBurst(profile.leaves, anchor.position, Vector3.up, profile.impactParticles, null); break; }
        }

        private void PlayBurst(GameObject prefab, Vector3 point, Vector3 normal, int count, AudioClip sound)
        {
            var profile = _definition.presentation;
            if (profile == null) return;
            if (Services.TryGet<IActorQueryService>(out var actors) && actors.PlayerTransform != null
                && (point - actors.PlayerTransform.position).sqrMagnitude > profile.maximumDistance * profile.maximumDistance) return;
            if (Services.TryGet<IParticleEffectService>(out var effects))
            {
                var particles = effects.RentParticles(prefab, profile.instancesPerEffect);
                if (particles != null)
                {
                    particles.transform.SetPositionAndRotation(point, normal.sqrMagnitude > 0.001f
                        ? Quaternion.FromToRotation(Vector3.forward, normal) : Quaternion.identity);
                    particles.Emit(count);
                    effects.ReturnParticles(particles, false);
                }
            }
            if (sound != null && Services.TryGet<ISoundService>(out var audio))
                audio.PlayClip(sound, UPlayGround.Data.Sound.SoundBusType.SFX, point);
        }
    }
}
