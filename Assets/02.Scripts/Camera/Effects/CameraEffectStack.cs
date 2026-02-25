using System.Collections.Generic;

namespace UPlayGround.CameraEffects
{
    public class CameraEffectStack
    {
        private readonly List<ICameraEffect> _activeEffects = new List<ICameraEffect>();

        public int Count => _activeEffects.Count;

        public void Play(ICameraEffect effect, CameraEffectContext context)
        {
            if (effect == null)
            {
                return;
            }

            Stop(effect.EffectId);

            effect.OnStart(context);
            _activeEffects.Add(effect);
        }

        public void Stop(string effectId)
        {
            if (string.IsNullOrEmpty(effectId))
            {
                return;
            }

            for (int i = 0; i < _activeEffects.Count; i++)
            {
                if (_activeEffects[i].EffectId == effectId)
                {
                    _activeEffects[i].RequestStop();
                }
            }
        }

        public void StopAll()
        {
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                _activeEffects[i].RequestStop();
            }
        }

        public void Evaluate(CameraEffectContext context, float deltaTime, ref CameraEffectOutput output)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                ICameraEffect effect = _activeEffects[i];
                effect.Evaluate(context, deltaTime, ref output);

                if (effect.IsFinished)
                {
                    effect.OnStop(context);
                    _activeEffects.RemoveAt(i);
                }
            }
        }
    }
}
