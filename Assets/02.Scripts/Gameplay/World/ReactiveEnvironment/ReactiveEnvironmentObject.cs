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
    public sealed class ReactiveEnvironmentObject : MonoBehaviour, IDamageable
    {
        [SerializeField] private ReactiveEnvironmentDefinitionSO _definition;
        [SerializeField] private EnvironmentController _weather;
        [SerializeField] private GameObject _intact;
        [SerializeField] private GameObject _remnant;
        [SerializeField] private Transform[] _rainSamples;
        [SerializeField] private Transform[] _heatSamples;
        [SerializeField] private Renderer[] _surfaces;
        [SerializeField] private GameObject _flame;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _surfaceProperties;
        private Color[][] _surfaceColors;
        private WorldStateManager _world;
        private SceneEntityId _identity;
        private bool _hasStarted;
        private bool _isBound;
        private ReactiveEnvironmentSurface _reactiveSurface;
        private ReactiveEnvironmentDestruction _destruction;
        private bool _hasAppliedSnapshot;
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

        private void Awake()
        {
            _identity = GetComponent<SceneEntityId>();
            _destruction = GetComponent<ReactiveEnvironmentDestruction>();
            _destruction?.Initialize();
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
                || GetComponent<GatheringActor>() != null || _weather == null
                || _intact == null || _remnant == null || _intact == _remnant
                || _intact == gameObject || _remnant == gameObject
                || !_intact.transform.IsChildOf(transform) || !_remnant.transform.IsChildOf(transform)
                || _intact.transform.IsChildOf(_remnant.transform) || _remnant.transform.IsChildOf(_intact.transform)
                || _flame == gameObject || _rainSamples == null || _rainSamples.Length == 0) return false;
            if (_flame != null && (_flame.transform.parent != transform || _flame == _intact || _flame == _remnant))
                return false;
            foreach (Transform sample in _rainSamples)
                if (sample == null || !sample.IsChildOf(transform)) return false;
            if (_heatSamples != null)
                foreach (Transform sample in _heatSamples)
                    if (sample == null || !sample.IsChildOf(transform)) return false;
            if (_definition.surfaceBackend == EnvironmentSurfaceBackend.ReactiveToon
                && ReactiveEnvironmentSurface.Validate(transform, _surfaces) != null) return false;
            if (_definition.destruction != null && (_destruction == null || !_destruction.IsValid(_definition.destruction))) return false;
            return true;
        }

        /// <summary>외부 화염·물 명중은 실행 ID와 연타 번호를 유지해 전달한다.</summary>
        public bool TryApplyImpact(ulong attackId, uint sequence, float damage, float heat, float water) =>
            _isBound && _world.ApplyEnvironmentImpact(this, attackId, sequence, damage, heat, water);

        /// <summary>시각 품질·카메라 실내 여부와 독립적인 CPU 강수 입력.</summary>
        public void SampleRain(out float rain, out float exposure)
        {
            rain = _weather != null && _weather.IsReady ? _weather.Current.Rain : 0;
            exposure = 0;
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
            bool changed = (_hasAppliedSnapshot && State.isBroken != state.isBroken) || !_hasAppliedSnapshot && state.isBroken;
            State = state;
            _hasAppliedSnapshot = true;
            _intact.SetActive(!state.isBroken);
            _remnant.SetActive(state.isBroken);
            _destruction?.Apply(_definition.destruction, destruction ?? new EnvironmentDestructionSaveState(), state.isBroken);
            if (changed && _world != null)
                _world.NotifyEnvironmentStructureChanged(this, _surfaces.Length > 0 && _surfaces[0] != null
                    ? _surfaces[0].bounds : new Bounds(transform.position, Vector3.zero));
            if (_flame != null) _flame.SetActive(state.combustion == EnvironmentCombustion.Burning);
            if (_definition.surfaceBackend == EnvironmentSurfaceBackend.ReactiveToon)
            {
                _reactiveSurface ??= new ReactiveEnvironmentSurface(_surfaces);
                _reactiveSurface.Apply(state, _definition.reaction.maxDurability, _definition.reaction.initialFuel);
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

        /// <summary>기존 근접·투사체의 기계적 피해만 즉시 전달한다. 원소는 추측하지 않는다.</summary>
        public CombatResult ReceiveHit(in HitRequest request)
        {
            float before = State.durability;
            _destruction?.SetImpactDirection(request.AttackDirection);
            bool accepted = CanTakeDamage() && _world.ApplyEnvironmentCombatHit(this, request.Damage, request.EnvironmentHit);
            float damage = accepted ? before - State.durability : 0;
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
        public void FixedTick(float deltaTime) => _destruction?.FixedTick(deltaTime);
    }
}
