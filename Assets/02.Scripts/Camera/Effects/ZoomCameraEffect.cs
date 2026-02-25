namespace UPlayGround.CameraEffects
{
    public sealed class ZoomCameraEffect : TimedCameraEffectBase
    {
        private readonly float _distanceOffset;

        public ZoomCameraEffect(string effectId, float distanceOffset, float holdDuration,
            float blendInDuration = 0.1f, float blendOutDuration = 0.12f)
            : base(effectId, holdDuration, blendInDuration, blendOutDuration)
        {
            _distanceOffset = distanceOffset;
        }

        protected override void OnEvaluate(CameraEffectContext context, float deltaTime, float weight,
            ref CameraEffectOutput output)
        {
            output.DistanceOffset += _distanceOffset * weight;
        }
    }
}
