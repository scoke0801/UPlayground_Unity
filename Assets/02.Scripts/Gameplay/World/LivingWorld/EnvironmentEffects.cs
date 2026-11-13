using System;
using UnityEngine;
using UPlayGround.Components;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>전용 FX 자식만 제어하며 실내 입자 제거와 환경음 감쇠를 적용한다.</summary>
    public sealed partial class EnvironmentEffects : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _rain;
        [SerializeField] private ParticleSystem _dust;
        [SerializeField] private ParticleSystem _snow;
        [SerializeField] private AudioSource _rainAudio;
        [SerializeField] private AudioSource _windAudio;
        [SerializeField] private BoxCollider[] _shelters = Array.Empty<BoxCollider>();
        [SerializeField] private LayerMask _roofLayers;
        [SerializeField, Min(0)] private float _roofProbeDistance = 30;
        [SerializeField, Min(0)] private float _rainRate = 120;
        [SerializeField, Min(0)] private float _dustRate = 8;
        [SerializeField, Min(0)] private float _snowRate = 80;
        [SerializeField] private Vector3 _snowOffset = new(0, 8, 0);
        [SerializeField, Min(0)] private float _particleWindSpeed = 2;
        [SerializeField, Range(0, 1)] private float _audioVolume = 0.15f;
        [SerializeField, Range(0, 1)] private float _indoorAudio = 0.15f;
        [SerializeField, Range(0, 1)] private float _focusAudio = 0.25f;
        [SerializeField, Range(0, 1)] private float _focusParticles = 0.25f;
        [SerializeField, Min(0.01f)] private float _audioFadeSeconds = 0.4f;
        private ParticleSystem.Particle[] _rainParticles, _dustParticles, _snowParticles;
        private bool _initialized;
        private readonly RaycastHit[] _roofHits = new RaycastHit[32];

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
                if (shelter == null || !shelter.enabled || !shelter.gameObject.activeInHierarchy) continue;
                Vector3 local = shelter.transform.InverseTransformPoint(position) - shelter.center;
                Vector3 half = shelter.size * 0.5f;
                if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z)
                    return true;
            }
            return false;
        }

        /// <summary>보호 볼륨과 실제 지붕을 함께 확인하고 자신의 Collider는 차폐에서 제외한다.</summary>
        public bool IsRainSheltered(Vector3 position, Transform receiverRoot = null)
        {
            if (IsSheltered(position)) return true;
            int count = Physics.RaycastNonAlloc(position, Vector3.up, _roofHits,
                _roofProbeDistance, _roofLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (receiverRoot == null || !_roofHits[i].transform.IsChildOf(receiverRoot)) return true;
            // 포화된 쿼리에서 보지 못한 지붕을 야외로 잘못 판정하지 않는다.
            return count == _roofHits.Length;
        }

        /// <summary>씬 준비 후 전용 입자 버퍼와 지붕 충돌을 한 번 설정한다.</summary>
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _rain = KeepOwned(_rain);
            _dust = KeepOwned(_dust);
            _snow = KeepOwned(_snow);
            _rainAudio = KeepOwned(_rainAudio);
            _windAudio = KeepOwned(_windAudio);
            _rainParticles = PrepareParticles(_rain);
            _dustParticles = PrepareParticles(_dust);
            _snowParticles = PrepareParticles(_snow);
            InitializeStorm();
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
            // 화면 밖에서는 밀린 시뮬레이션을 재생하지 않고 멈춘다.
            main.cullingMode = ParticleSystemCullingMode.Pause;
            return new ParticleSystem.Particle[main.maxParticles];
        }

        private void ConfigureRoofCollision()
        {
            ConfigurePrecipitationCollision(_rain);
            ConfigurePrecipitationCollision(_snow);
        }

        private void ConfigurePrecipitationCollision(ParticleSystem particles)
        {
            if (particles == null) return;
            var collision = particles.collision;
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
            bool submerged = hasObserver && FootstepWaterSurface.TryResolveSurface(observer, out _, out Vector3 water) && observer.y < water.y;
            bool inside = hasObserver && (submerged || IsRainSheltered(observer));
            float visibility = hasFocus ? _focusParticles : 1;
            // 지붕 충돌층을 연결하기 전에는 실내 관통 가능성이 있는 비를 방출하지 않는다.
            float rain = hasObserver && !inside && _roofLayers.value != 0 ? sample.Rain : 0;
            float snow = hasObserver && !inside && _roofLayers.value != 0 ? sample.Snow : 0;
            if (hasObserver && _snow != null) _snow.transform.position = observer + _snowOffset;
            float dust = hasObserver && !inside
                ? sample.Wind.magnitude * (1 - sample.Rain) * (1 - sample.Snow) * (1 - sample.Wetness) : 0;
            SetParticles(_rain, rain * _rainRate * visibility, sample.Wind);
            SetParticles(_dust, dust * _dustRate * visibility, sample.Wind);
            SetParticles(_snow, snow * _snowRate * visibility, sample.Wind);
            if (inside && _rain != null) _rain.Clear(withChildren: true);
            if (inside && _snow != null) _snow.Clear(withChildren: true);
            if (submerged && _dust != null) _dust.Clear(withChildren: true);
            float volume = _audioVolume * (inside ? _indoorAudio : 1) * (hasFocus ? _focusAudio : 1);
            SetAudio(_rainAudio, hasObserver ? sample.Rain * volume : 0);
            SetAudio(_windAudio, hasObserver ? sample.Wind.magnitude * volume : 0);
            ApplyStorm(sample.Storm, observer, hasObserver, hasFocus, volume, inside ? 0 : 1);
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
            Cull(_snow, _snowParticles);
        }

        private void Cull(ParticleSystem particles, ParticleSystem.Particle[] buffer)
        {
            if (_shelters.Length == 0 || particles == null || !particles.IsAlive(false)) return;
            int count = particles.GetParticles(buffer);
            bool hasRemovedParticles = false;
            // 보호 구역의 Transform/Collider 정보를 입자마다 네이티브에서 읽지 않는다.
            // 호출마다 한 번씩 읽으므로 이동·회전·크기 및 활성 상태 변경은 즉시 반영된다.
            foreach (BoxCollider shelter in _shelters)
            {
                if (shelter == null || !shelter.enabled || !shelter.gameObject.activeInHierarchy) continue;
                Matrix4x4 worldToLocal = shelter.transform.worldToLocalMatrix;
                var localBounds = new Bounds(shelter.center, shelter.size);
                for (int i = 0; i < count; i++)
                {
                    if (buffer[i].remainingLifetime <= 0
                        || !localBounds.Contains(worldToLocal.MultiplyPoint3x4(buffer[i].position)))
                        continue;

                    buffer[i].remainingLifetime = -1;
                    hasRemovedParticles = true;
                }
            }
            if (hasRemovedParticles) particles.SetParticles(buffer, count);
        }

        /// <summary>이 프리팹의 효과와 소리만 정리한다.</summary>
        public void StopEffects()
        {
            if (_rain != null) _rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_dust != null) _dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_snow != null) _snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            StopStorm();
            if (_rainAudio != null) { _rainAudio.Stop(); _rainAudio.volume = 0; }
            if (_windAudio != null) { _windAudio.Stop(); _windAudio.volume = 0; }
        }

        private void OnDisable() { if (_initialized) StopEffects(); }
    }
}
