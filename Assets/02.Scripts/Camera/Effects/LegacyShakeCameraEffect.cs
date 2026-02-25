using UPlayGround.Data;

namespace UPlayGround.CameraEffects
{
    public sealed class LegacyShakeCameraEffect : TimedCameraEffectBase
    {
        private readonly string _shakeKey;
        private readonly CameraShakeData _shakeData;

        public LegacyShakeCameraEffect(string effectId, string shakeKey, float holdDuration, float blendOutDuration = 0.1f)
            : base(effectId, holdDuration, 0f, blendOutDuration)
        {
            _shakeKey = shakeKey;
        }

        public LegacyShakeCameraEffect(string effectId, CameraShakeData shakeData, float holdDuration, float blendOutDuration = 0.1f)
            : base(effectId, holdDuration, 0f, blendOutDuration)
        {
            _shakeData = shakeData;
        }

        public override void OnStart(CameraEffectContext context)
        {
            base.OnStart(context);

            if (_shakeData != null)
            {
                context.Manager.StartShake(_shakeData);
                return;
            }

            if (string.IsNullOrEmpty(_shakeKey) == false)
            {
                context.Manager.StartShake(_shakeKey);
            }
        }

        protected override void OnEvaluate(CameraEffectContext context, float deltaTime, float weight,
            ref CameraEffectOutput output)
        {
        }

        public override void OnStop(CameraEffectContext context)
        {
            context.Manager.StopShake();
        }
    }
}
