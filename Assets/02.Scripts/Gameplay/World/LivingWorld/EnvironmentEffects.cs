using System;
using UnityEngine;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>전용 FX 자식만 제어하며 실내 입자 제거와 환경음 감쇠를 적용한다.</summary>
    public sealed class EnvironmentEffects : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _rain;
        [SerializeField] private ParticleSystem _dust;
        [SerializeField] private AudioSource _rainAudio;
        [SerializeField] private AudioSource _windAudio;
        [SerializeField] private BoxCollider[] _shelters = Array.Empty<BoxCollider>();
        [SerializeField] private LayerMask _roofLayers;
        [SerializeField, Min(0)] private float _rainRate = 120;
        [SerializeField, Min(0)] private float _dustRate = 8;
        [SerializeField, Min(0)] private float _particleWindSpeed = 2;
        [SerializeField, Range(0, 1)] private float _audioVolume = 0.15f;
        [SerializeField, Range(0, 1)] private float _indoorAudio = 0.15f;
        [SerializeField, Range(0, 1)] private float _focusAudio = 0.25f;
        [SerializeField, Range(0, 1)] private float _focusParticles = 0.25f;
        [SerializeField, Min(0.01f)] private float _audioFadeSeconds = 0.4f;
        private ParticleSystem.Particle[] _rainParticles, _dustParticles;
        private bool _initialized;

        /// <summary>I가 실제 지붕 레이어와 보호 볼륨을 연결한다. 물리 Trigger 진입은 요구하지 않는다.</summary>
        public void BindShelters(BoxCollider[] shelters, LayerMask roofLayers)
        {
            _shelters = shelters ?? Array.Empty<BoxCollider>();
            _roofLayers = roofLayers;
            if (_initialized) ConfigureRoofCollision();
        }

        /// <summary>회전·크기가 있는 처마 보호 볼륨을 월드 좌표로 조회한다.</summary>
        public bool IsSheltered(Vector3 position)
        {
            foreach (BoxCollider shelter in _shelters)
            {
                if (shelter == null || !shelter.gameObject.activeInHierarchy) continue;
                Vector3 local = shelter.transform.InverseTransformPoint(position) - shelter.center;
                Vector3 half = shelter.size * 0.5f;
                if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z)
                    return true;
            }
            return false;
        }

        /// <summary>씬 준비 후 전용 입자 버퍼와 지붕 충돌을 한 번 설정한다.</summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _rain = KeepOwned(_rain);
            _dust = KeepOwned(_dust);
            _rainAudio = KeepOwned(_rainAudio);
            _windAudio = KeepOwned(_windAudio);
            _rainParticles = PrepareParticles(_rain);
            _dustParticles = PrepareParticles(_dust);
            ConfigureRoofCollision();
            StopEffects();
        }

        private T KeepOwned<T>(T component) where T : Component
        {
            if (component == null || component.transform.IsChildOf(transform)) return component;
            Debug.LogError("환경 효과는 전용 프리팹 자식만 참조해야 합니다.", this);
            return null;
        }

        private static ParticleSystem.Particle[] PrepareParticles(ParticleSystem particles)
        {
            if (particles == null) return Array.Empty<ParticleSystem.Particle>();
            var main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            return new ParticleSystem.Particle[main.maxParticles];
        }

        private void ConfigureRoofCollision()
        {
            if (_rain == null) return;
            var collision = _rain.collision;
            collision.enabled = _roofLayers.value != 0;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.collidesWith = _roofLayers;
            collision.lifetimeLoss = 1;
            collision.quality = ParticleSystemCollisionQuality.High;
        }

        /// <summary>관측점의 실내 여부와 대화·전투·조사 집중도를 반영한다.</summary>
        public void Apply(EnvironmentSchedule.Sample sample, Vector3 observer, bool hasObserver, bool hasFocus)
        {
            Initialize();
            bool inside = hasObserver && IsSheltered(observer);
            float visibility = hasFocus ? _focusParticles : 1;
            // 지붕 충돌층을 연결하기 전에는 실내 관통 가능성이 있는 비를 방출하지 않는다.
            float rain = hasObserver && !inside && _roofLayers.value != 0 ? sample.Rain : 0;
            float dust = sample.Wind.magnitude * (1 - sample.Rain) * (1 - sample.Wetness);
            SetParticles(_rain, rain * _rainRate * visibility, sample.Wind);
            SetParticles(_dust, dust * _dustRate * visibility, sample.Wind);
            if (inside && _rain != null) _rain.Clear(withChildren: true);
            float volume = _audioVolume * (inside ? _indoorAudio : 1) * (hasFocus ? _focusAudio : 1);
            SetAudio(_rainAudio, hasObserver ? sample.Rain * volume : 0);
            SetAudio(_windAudio, hasObserver ? sample.Wind.magnitude * volume : 0);
        }

        private void SetParticles(ParticleSystem particles, float rate, Vector2 wind)
        {
            if (particles == null || !particles.gameObject.activeInHierarchy) return;
            var emission = particles.emission;
            emission.rateOverTime = rate;
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = wind.x * _particleWindSpeed;
            velocity.z = wind.y * _particleWindSpeed;
            if (rate > 0 && !particles.isPlaying) particles.Play(withChildren: false);
            if (rate <= 0 && particles.isPlaying) particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        private void SetAudio(AudioSource source, float volume)
        {
            if (source == null || !source.isActiveAndEnabled) return;
            source.volume = Mathf.MoveTowards(source.volume, volume, Time.unscaledDeltaTime / _audioFadeSeconds);
            if (source.volume > 0 && source.clip != null && !source.isPlaying) source.Play();
            if (source.volume == 0 && source.isPlaying) source.Stop();
        }

        /// <summary>생성점이 처마 밖이어도 실내로 이동한 입자를 렌더 전에 제거한다.</summary>
        public void CullShelteredParticles()
        {
            if (!_initialized) return;
            Cull(_rain, _rainParticles);
            Cull(_dust, _dustParticles);
        }

        private void Cull(ParticleSystem particles, ParticleSystem.Particle[] buffer)
        {
            if (particles == null || !particles.IsAlive(false)) return;
            int count = particles.GetParticles(buffer);
            for (int i = 0; i < count; i++)
            {
                if (IsSheltered(buffer[i].position)) buffer[i].remainingLifetime = -1;
            }
            particles.SetParticles(buffer, count);
        }

        /// <summary>이 프리팹의 효과와 소리만 정리한다.</summary>
        public void StopEffects()
        {
            if (_rain != null) _rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_dust != null) _dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_rainAudio != null) { _rainAudio.Stop(); _rainAudio.volume = 0; }
            if (_windAudio != null) { _windAudio.Stop(); _windAudio.volume = 0; }
        }

        private void OnDisable() { if (_initialized) StopEffects(); }
    }
}
