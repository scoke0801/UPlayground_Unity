using UnityEngine;

namespace UPlayGround.CameraEffects
{
    public sealed class ProceduralShakeCameraEffect : TimedCameraEffectBase
    {
        private readonly Vector3 _amplitude;
        private readonly float _frequency;
        private readonly float _seed;
        private float _noiseTime;

        public ProceduralShakeCameraEffect(string effectId, Vector3 amplitude, float frequency, float holdDuration,
            float blendInDuration = 0.02f, float blendOutDuration = 0.15f)
            : base(effectId, holdDuration, blendInDuration, blendOutDuration)
        {
            _amplitude = amplitude;
            _frequency = Mathf.Max(0.1f, frequency);
            _seed = Random.Range(0f, 10000f);
        }

        protected override void OnEvaluate(CameraEffectContext context, float deltaTime, float weight,
            ref CameraEffectOutput output)
        {
            _noiseTime += deltaTime * _frequency;

            float x = (Mathf.PerlinNoise(_seed + 17.11f, _noiseTime) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(_seed + 37.73f, _noiseTime) - 0.5f) * 2f;
            float z = (Mathf.PerlinNoise(_seed + 59.41f, _noiseTime) - 0.5f) * 2f;

            Vector3 localOffset = Vector3.Scale(new Vector3(x, y, z), _amplitude) * weight;
            output.LocalPositionOffset += localOffset;
        }
    }
}
