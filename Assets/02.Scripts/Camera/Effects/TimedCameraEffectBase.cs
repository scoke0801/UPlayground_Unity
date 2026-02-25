using UnityEngine;

namespace UPlayGround.CameraEffects
{
    public abstract class TimedCameraEffectBase : ICameraEffect
    {
        public string EffectId { get; }
        public bool IsFinished { get; private set; }

        private readonly float _blendInDuration;
        private readonly float _holdDuration;
        private readonly float _blendOutDuration;

        private float _elapsed;
        private bool _stopRequested;
        private float _stopElapsed;
        private float _stopStartWeight;

        protected TimedCameraEffectBase(string effectId, float holdDuration, float blendInDuration, float blendOutDuration)
        {
            EffectId = effectId;
            _holdDuration = Mathf.Max(-1f, holdDuration);
            _blendInDuration = Mathf.Max(0f, blendInDuration);
            _blendOutDuration = Mathf.Max(0f, blendOutDuration);
        }

        public virtual void OnStart(CameraEffectContext context)
        {
            _elapsed = 0f;
            _stopElapsed = 0f;
            _stopStartWeight = 1f;
            _stopRequested = false;
            IsFinished = false;
        }

        public void Evaluate(CameraEffectContext context, float deltaTime, ref CameraEffectOutput output)
        {
            if (IsFinished)
            {
                return;
            }

            float weight;
            if (_stopRequested)
            {
                _stopElapsed += deltaTime;
                if (_blendOutDuration <= 0f)
                {
                    IsFinished = true;
                    return;
                }

                float t = Mathf.Clamp01(_stopElapsed / _blendOutDuration);
                weight = Mathf.Lerp(_stopStartWeight, 0f, t);
                if (t >= 1f)
                {
                    IsFinished = true;
                }
            }
            else
            {
                _elapsed += deltaTime;
                weight = EvaluateWeight(_elapsed);
                if (_holdDuration >= 0f)
                {
                    float totalDuration = _blendInDuration + _holdDuration + _blendOutDuration;
                    if (_elapsed >= totalDuration)
                    {
                        IsFinished = true;
                    }
                }
            }

            if (weight > 0f)
            {
                OnEvaluate(context, deltaTime, weight, ref output);
            }
        }

        public void RequestStop()
        {
            if (IsFinished || _stopRequested)
            {
                return;
            }

            _stopRequested = true;
            _stopElapsed = 0f;
            _stopStartWeight = EvaluateWeight(_elapsed);
        }

        public virtual void OnStop(CameraEffectContext context)
        {
        }

        protected abstract void OnEvaluate(CameraEffectContext context, float deltaTime, float weight,
            ref CameraEffectOutput output);

        private float EvaluateWeight(float elapsed)
        {
            if (_blendInDuration > 0f && elapsed < _blendInDuration)
            {
                return Mathf.Clamp01(elapsed / _blendInDuration);
            }

            if (_holdDuration < 0f)
            {
                return 1f;
            }

            float holdEnd = _blendInDuration + _holdDuration;
            if (elapsed < holdEnd)
            {
                return 1f;
            }

            if (_blendOutDuration <= 0f)
            {
                return 0f;
            }

            float outT = Mathf.Clamp01((elapsed - holdEnd) / _blendOutDuration);
            return 1f - outT;
        }
    }
}
