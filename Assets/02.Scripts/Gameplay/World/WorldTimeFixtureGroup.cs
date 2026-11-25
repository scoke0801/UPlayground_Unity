using System;
using System.Collections;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.World
{
    /// <summary>월드 시각에 맞춰 생활 조명과 발광 표현을 함께 제어한다.</summary>
    [DisallowMultipleComponent]
    public sealed class WorldTimeFixtureGroup : MonoBehaviour
    {
        [SerializeField] private Light[] _lights = Array.Empty<Light>();
        [SerializeField] private Renderer[] _emitters = Array.Empty<Renderer>();
        [SerializeField, ColorUsage(false, true)] private Color _nightEmission = new Color(1.7f, 0.72f, 0.23f);
        [Tooltip("가로축은 시각(0~24시), 세로축은 각 등잔의 최대 세기에 곱할 비율이다.")]
        [SerializeField] private AnimationCurve _intensityByHour = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(5f, 1f), new Keyframe(7f, 0f),
            new Keyframe(17.5f, 0f), new Keyframe(20f, 1f), new Keyframe(24f, 1f));

        private float[] _authoredIntensities;
        private MaterialPropertyBlock _emissionBlock;
        private float _lastIntensity = -1f;
        private IGameTimeService _clock;
        private Coroutine _clockBinding;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _authoredIntensities = new float[_lights.Length];
            _emissionBlock = new MaterialPropertyBlock();
            for (int i = 0; i < _lights.Length; i++)
                if (_lights[i] != null)
                    _authoredIntensities[i] = _lights[i].intensity;
        }

        private void OnEnable()
        {
            ApplyIntensity(0f);
            if (!TryBindClock())
                _clockBinding = StartCoroutine(WaitForClock());
        }

        private void OnDisable()
        {
            if (_clockBinding != null)
            {
                StopCoroutine(_clockBinding);
                _clockBinding = null;
            }

            if (_clock != null)
            {
                _clock.OnGameMinuteChanged -= OnGameMinuteChanged;
                _clock = null;
            }

            ApplyIntensity(0f);
        }

        private IEnumerator WaitForClock()
        {
            while (!TryBindClock())
                yield return null;

            _clockBinding = null;
        }

        private bool TryBindClock()
        {
            if (!Services.TryGet<IGameTimeService>(out _clock))
                return false;

            _clock.OnGameMinuteChanged += OnGameMinuteChanged;
            OnGameMinuteChanged(_clock.CurrentDay, _clock.MinuteOfDay);
            return true;
        }

        private void OnGameMinuteChanged(int day, float minuteOfDay)
        {
            ApplyIntensity(Mathf.Clamp01(_intensityByHour.Evaluate(minuteOfDay / 60f)));
        }

        private void ApplyIntensity(float intensity)
        {
            if (Mathf.Approximately(_lastIntensity, intensity))
                return;

            _lastIntensity = intensity;
            bool lit = intensity > 0f;

            for (int i = 0; i < _lights.Length; i++)
            {
                var light = _lights[i];
                if (light == null) continue;
                light.intensity = _authoredIntensities[i] * intensity;
                light.enabled = lit && _authoredIntensities[i] > 0f;
            }

            foreach (var emitter in _emitters)
            {
                if (emitter == null) continue;
                emitter.GetPropertyBlock(_emissionBlock);
                _emissionBlock.SetColor(EmissionColorId, _nightEmission * intensity);
                emitter.SetPropertyBlock(_emissionBlock);
            }
        }
    }
}
