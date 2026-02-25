namespace UPlayGround.CameraEffects
{
    public sealed class FovCameraEffect : TimedCameraEffectBase
    {
        private readonly float _fovOffset;

        public FovCameraEffect(string effectId, float fovOffset, float holdDuration, float blendInDuration = 0.1f,
            float blendOutDuration = 0.15f)
            : base(effectId, holdDuration, blendInDuration, blendOutDuration)
        {
            _fovOffset = fovOffset;
        }

        protected override void OnEvaluate(CameraEffectContext context, float deltaTime, float weight,
            ref CameraEffectOutput output)
        {
            output.FovOffset += _fovOffset * weight;
        }
    }
}
