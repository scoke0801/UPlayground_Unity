namespace UPlayGround.CameraEffects
{
    public interface ICameraEffect
    {
        string EffectId { get; }
        bool IsFinished { get; }

        void OnStart(CameraEffectContext context);
        void Evaluate(CameraEffectContext context, float deltaTime, ref CameraEffectOutput output);
        void RequestStop();
        void OnStop(CameraEffectContext context);
    }
}
