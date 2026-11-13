using UnityEngine;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.World.ReactiveEnvironment
{
    /// <summary>읽기 전용 연소 snapshot을 별도 화염·연기 emitter에 연결한다.</summary>
    public sealed class EnvironmentCombustionPresentation
    {
        private readonly EnvironmentPresentationProfileSO _profile;
        private IParticleEffectService _effects;
        private ParticleSystem _flame, _smoke;
        private EnvironmentReactionState _state;
        private bool _hasState;
        private bool _quenched;
        private float _residualTime;
        private float _flameRate = -1, _smokeRate = -1;
        private Vector2 _flameWind, _smokeWind;
        private float _appliedUpdraft;
        public bool HasFlame => _flame != null;
        public bool HasSmoke => _smoke != null;

        public EnvironmentCombustionPresentation(EnvironmentPresentationProfileSO profile) => _profile = profile;

        /// <summary>반복 snapshot은 소화 타이머나 입자 재생을 초기화하지 않는다.</summary>
        public void Apply(in EnvironmentReactionState state)
        {
            if (_hasState && _state.combustion == EnvironmentCombustion.Burning && state.combustion != EnvironmentCombustion.Burning)
                _quenched = state.wetness > _state.wetness;
            if (!_hasState || _state.combustion == EnvironmentCombustion.Burning && state.combustion != EnvironmentCombustion.Burning)
                _residualTime = 0;
            _state = state; _hasState = true;
        }

        /// <summary>중앙 물리 tick에서만 수명·거리 예산을 갱신한다.</summary>
        public void Tick(float seconds, Vector3 position, Vector2 wind, float weight = 1)
        {
            if (_profile == null || !_hasState) return;
            bool burning = _state.combustion == EnvironmentCombustion.Burning && _state.fuel > 0;
            bool residual = _state.combustion == EnvironmentCombustion.Cooling || _state.combustion == EnvironmentCombustion.Spent;
            if (residual) _residualTime += seconds;
            bool visible = true;
            if (Services.TryGet<IActorQueryService>(out var actors) && actors.PlayerTransform != null)
                visible = (position - actors.PlayerTransform.position).sqrMagnitude <= _profile.maximumDistance * _profile.maximumDistance;
            if (visible && Services.TryGet<ICameraViewService>(out var cameras))
            {
                var camera = cameras.GetMainCamera();
                if (camera != null)
                {
                    Vector3 viewport = camera.WorldToViewportPoint(position);
                    float margin = _profile.viewportMargin;
                    visible = (position - camera.transform.position).sqrMagnitude <= _profile.maximumDistance * _profile.maximumDistance
                        && viewport.z > 0 && viewport.x >= -margin && viewport.x <= 1 + margin
                        && viewport.y >= -margin && viewport.y <= 1 + margin;
                }
            }
            float strength = burning ? Mathf.Lerp(_profile.minimumFlameStrength, 1, _state.heat) * (1 - _profile.wetFlameReduction * _state.wetness) : 0;
            float smoke = burning ? Mathf.Max(_profile.minimumBurnSmoke, strength)
                : residual && _state.heat > _profile.residualHeatThreshold
                    ? Mathf.Clamp01(1 - _residualTime / _profile.residualSeconds) * _state.heat
                : _state.combustion == EnvironmentCombustion.Heating && _state.heat >= _profile.heatingSmokeThreshold
                    ? Mathf.InverseLerp(_profile.heatingSmokeThreshold, 1, _state.heat) * _profile.heatingSmokeStrength : 0;
            if (residual && _quenched)
                smoke = Mathf.Max(smoke, _profile.steamStrength * Mathf.Clamp01(1 - _residualTime / _profile.steamSeconds));
            if (_effects == null) Services.TryGet(out _effects);
            bool updateVelocity = _appliedUpdraft != _profile.updraft;
            ApplyEmitter(ref _flame, _profile.flame, visible ? strength * weight * _profile.flameRate : 0,
                position, Vector2.zero, strength, ref _flameRate, ref _flameWind, updateVelocity);
            ApplyEmitter(ref _smoke, _profile.smoke, visible ? smoke * weight * _profile.smokeRate : 0,
                position + Vector3.up * _profile.smokeHeightOffset, wind, 0, ref _smokeRate, ref _smokeWind, updateVelocity);
            _appliedUpdraft = _profile.updraft;
        }

        private void ApplyEmitter(ref ParticleSystem particles, GameObject prefab, float rate, Vector3 position,
            Vector2 wind, float flameStrength, ref float previousRate, ref Vector2 previousWind, bool updateVelocity)
        {
            if (_effects == null) return;
            if (rate <= 0 || prefab == null)
            {
                if (particles != null) _effects.ReturnParticles(particles, false);
                particles = null; previousRate = -1;
                return;
            }
            bool created = particles == null;
            if (created) particles = _effects.RentParticles(prefab, _profile.instancesPerEffect);
            if (particles == null) return;
            var rotation = prefab.transform.rotation;
            if (created || particles.transform.position != position || particles.transform.rotation != rotation)
                particles.transform.SetPositionAndRotation(position, rotation);
            if (created)
            {
                particles.transform.localScale = Vector3.one * _profile.effectScale;
                var main = particles.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
            }
            if (created || previousRate != rate)
            {
                var emission = particles.emission; emission.rateOverTime = rate;
                if (flameStrength > 0)
                {
                    var main = particles.main;
                    main.startSize = new ParticleSystem.MinMaxCurve(_profile.flameSize * flameStrength * _profile.particleVariation, _profile.flameSize * flameStrength);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(_profile.flameSpeed * flameStrength * _profile.particleVariation, _profile.flameSpeed * flameStrength);
                }
                previousRate = rate;
            }
            if (created || updateVelocity || previousWind != wind)
            {
                var velocity = particles.velocityOverLifetime;
                velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = wind.x; velocity.y = _profile.updraft; velocity.z = wind.y;
                previousWind = wind;
            }
            if (created) particles.Play(true);
        }

        /// <summary>언로드·풀 반환 때 대여한 효과를 모두 비운다.</summary>
        public void Release()
        {
            if (_flame != null) _effects?.ReturnParticles(_flame, true);
            if (_smoke != null) _effects?.ReturnParticles(_smoke, true);
            _flame = _smoke = null; _flameRate = _smokeRate = -1;
            _hasState = false; _quenched = false; _residualTime = 0;
        }
    }
}
