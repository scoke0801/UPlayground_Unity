using UnityEngine;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    public sealed partial class EnvironmentEffects
    {
        [Header("폭풍 (월드 낙뢰 접점 / 미리보기 원경 번개)")]
        [SerializeField] private LineRenderer _lightningBolt;
        [SerializeField] private LineRenderer _lightningBranch;
        [SerializeField] private Light _lightningLight;
        [SerializeField] private AudioSource _thunderAudio;
        [SerializeField] private AudioClip[] _thunderClips = System.Array.Empty<AudioClip>();
        [SerializeField] private Vector2 _strikeInterval = new(8, 22);
        [SerializeField] private Vector2 _strikeDistance = new(180, 400);
        [SerializeField, Min(1)] private float _strikeHeight = 180;
        [SerializeField, Min(0)] private float _boltJitter = 9;
        [SerializeField, Min(0)] private float _branchLength = 36;
        [SerializeField] private Vector3 _lightOffset = new(0, 25, 0);
        [SerializeField, Min(0.01f)] private float _flashSeconds = 0.55f;
        [SerializeField, Min(0)] private float _flashIntensity = 2;
        [SerializeField, Min(0)] private float _lightIntensity = 4;
        [SerializeField, Min(1)] private float _soundSpeed = 343;
        [SerializeField] private AnimationCurve _flashEnvelope = new(
            new Keyframe(0, 0), new Keyframe(0.04f, 1), new Keyframe(0.18f, 0.1f),
            new Keyframe(0.28f, 0.65f), new Keyframe(1, 0));
        private readonly System.Random _stormRandom = new();
        private readonly Vector3[] _boltPositions = new Vector3[25];
        private readonly Vector3[] _branchPositions = new Vector3[9];
        private float _strikeCountdown, _flashAge = float.PositiveInfinity, _thunderCountdown = -1;
        private float _strikeStrength;
        private bool _usesWorldLightning;
        private Vector3 _worldStrikePoint;
        private bool _hasWorldStrike;

        /// <summary>월드 낙뢰를 사용할 때 원경 장식 타이머의 중복 생성을 멈춘다.</summary>
        public void SetWorldLightning(bool enabled) => _usesWorldLightning = enabled;

        /// <summary>논리가 확정한 같은 접점으로 본선과 천둥 거리를 계산한다.</summary>
        public void PresentLightning(UPlayGround.Data.Save.EnvironmentLightningStrike strike, Vector3 observer, float storm)
        {
            Vector3 bottom = strike.Point;
            _worldStrikePoint = bottom;
            _hasWorldStrike = true;
            Vector3 top = bottom + Vector3.up * _strikeHeight;
            BuildBolt(_lightningBolt, _boltPositions, top, bottom, _boltJitter);
            Vector3 sideways = Vector3.right * _branchLength;
            Vector3 split = _boltPositions[10];
            BuildBolt(_lightningBranch, _branchPositions, split, split + sideways - Vector3.up * _branchLength, _boltJitter * 0.5f);
            LightningDirection = (top - observer).normalized;
            _flashAge = 0;
            _strikeStrength = Mathf.Clamp01(storm);
            _thunderCountdown = Vector3.Distance(observer, bottom) / Mathf.Max(1f, _soundSpeed);
        }

        /// <summary>하늘과 월드 조명이 같은 섬광 포락선을 소비한다.</summary>
        public float LightningIntensity { get; private set; }
        /// <summary>하늘의 구름 내부 섬광이 원경 번개 방향에 모이게 한다.</summary>
        public Vector3 LightningDirection { get; private set; } = Vector3.up;

        /// <summary>접점 불티는 기존 효과 풀의 상한 안에서만 재생하며 열을 다시 제출하지 않는다.</summary>
        public void PresentLightningContact(UPlayGround.Data.Save.EnvironmentLightningStrike strike,
            UPlayGround.Data.ReactiveEnvironment.EnvironmentLightningProfileSO profile)
        {
            if (profile == null || profile.contactEffect == null) return;
            if (UPlayGround.Manager.Services.TryGet<UPlayGround.Manager.IActorQueryService>(out var actors)
                && actors.PlayerTransform != null
                && (strike.Point - actors.PlayerTransform.position).sqrMagnitude > profile.maximumPresentationDistance * profile.maximumPresentationDistance) return;
            if (!UPlayGround.Manager.Services.TryGet<UPlayGround.Manager.IParticleEffectService>(out var effects)) return;
            var particles = effects.RentParticles(profile.contactEffect, profile.maximumContactEffects);
            if (particles == null) return;
            particles.transform.SetPositionAndRotation(strike.Point, Quaternion.FromToRotation(Vector3.forward, strike.Normal));
            particles.Emit(profile.contactParticles);
            effects.ReturnParticles(particles, false);
        }

        private void InitializeStorm()
        {
            _lightningBolt = KeepOwned(_lightningBolt);
            _lightningBranch = KeepOwned(_lightningBranch);
            _lightningLight = KeepOwned(_lightningLight);
            _thunderAudio = KeepOwned(_thunderAudio);
        }

        private float ChooseRange(Vector2 range, float minimum) =>
            Mathf.Lerp(Mathf.Max(minimum, range.x), Mathf.Max(minimum, Mathf.Max(range.x, range.y)), (float)_stormRandom.NextDouble());

        private void ApplyStorm(float storm, Vector3 observer, bool hasObserver, bool hasFocus, float volume, float lightVisibility)
        {
            if (!hasObserver) { StopStorm(); return; }
            float delta = Time.deltaTime;
            if (delta <= 0) return;
            _flashAge += delta;
            if (_thunderCountdown >= 0)
            {
                _thunderCountdown -= delta;
                if (_thunderCountdown < 0 && _thunderAudio != null && _thunderAudio.isActiveAndEnabled && _thunderClips.Length > 0)
                {
                    AudioClip clip = _thunderClips[_stormRandom.Next(_thunderClips.Length)];
                    if (clip != null) _thunderAudio.PlayOneShot(clip, volume * _strikeStrength);
                }
            }
            if (storm > 0 && !_usesWorldLightning)
            {
                _strikeCountdown -= delta * storm;
                if (_strikeCountdown <= 0 && _thunderCountdown < 0 && _flashAge >= _flashSeconds)
                    BeginStrike(observer, storm);
            }
            else _strikeCountdown = Mathf.Max(_strikeCountdown, Mathf.Max(1, _strikeInterval.x));

            float envelope = _flashAge < _flashSeconds && _flashEnvelope != null
                ? Mathf.Max(0, _flashEnvelope.Evaluate(_flashAge / Mathf.Max(0.01f, _flashSeconds))) : 0;
            float strength = envelope * _strikeStrength * (hasFocus ? _focusParticles : 1);
            LightningIntensity = strength * _flashIntensity;
            SetBoltVisibility(_lightningBolt, strength);
            SetBoltVisibility(_lightningBranch, strength);
            if (_lightningLight == null) return;
            _lightningLight.transform.position = (_hasWorldStrike ? _worldStrikePoint : observer) + _lightOffset;
            _lightningLight.intensity = strength * _lightIntensity * lightVisibility;
            _lightningLight.enabled = strength * lightVisibility > 0;
        }

        private void BeginStrike(Vector3 observer, float storm)
        {
            _hasWorldStrike = false;
            float angle = (float)_stormRandom.NextDouble() * Mathf.PI * 2;
            float distance = ChooseRange(_strikeDistance, 1);
            Vector3 horizontal = new(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 top = observer + horizontal * distance + Vector3.up * _strikeHeight;
            Vector3 bottom = top - Vector3.up * (_strikeHeight * 0.8f);
            BuildBolt(_lightningBolt, _boltPositions, top, bottom, _boltJitter);
            Vector3 split = _boltPositions[10];
            Vector3 sideways = new(-horizontal.z, 0, horizontal.x);
            BuildBolt(_lightningBranch, _branchPositions, split,
                split + sideways * _branchLength - Vector3.up * (_strikeHeight * 0.3f), _boltJitter * 0.5f);
            LightningDirection = (top - observer).normalized;
            _flashAge = 0;
            _strikeStrength = Mathf.Clamp01(storm);
            _thunderCountdown = distance / Mathf.Max(1, _soundSpeed);
            _strikeCountdown = ChooseRange(_strikeInterval, 1);
        }

        private void BuildBolt(LineRenderer line, Vector3[] positions, Vector3 top, Vector3 bottom, float jitter)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                float t = i / (float)(positions.Length - 1);
                Vector3 offset = new((float)_stormRandom.NextDouble() - 0.5f, 0, (float)_stormRandom.NextDouble() - 0.5f);
                positions[i] = Vector3.Lerp(top, bottom, t) + offset * (Mathf.Sin(t * Mathf.PI) * jitter);
            }
            positions[0] = top;
            positions[positions.Length - 1] = bottom;
            if (line == null) return;
            line.useWorldSpace = true;
            line.positionCount = positions.Length;
            line.SetPositions(positions);
        }

        private static void SetBoltVisibility(LineRenderer line, float strength)
        {
            if (line == null) return;
            line.enabled = strength > 0.01f;
            Color color = new(0.65f, 0.78f, 1, Mathf.Clamp01(strength));
            line.startColor = color;
            line.endColor = color;
        }

        private void StopStorm()
        {
            _hasWorldStrike = false;
            LightningIntensity = 0;
            _flashAge = float.PositiveInfinity;
            _thunderCountdown = -1;
            _strikeCountdown = ChooseRange(_strikeInterval, 1);
            SetBoltVisibility(_lightningBolt, 0);
            SetBoltVisibility(_lightningBranch, 0);
            if (_lightningLight != null) _lightningLight.enabled = false;
            if (_thunderAudio != null) _thunderAudio.Stop();
        }
    }
}
