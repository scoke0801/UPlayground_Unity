using UnityEngine;

namespace UPlayGround.CameraEffects
{
    public struct CameraEffectOutput
    {
        public Vector3 WorldPositionOffset;
        public Vector3 LocalPositionOffset;
        public Vector3 LocalEulerOffset;
        public float DistanceOffset;
        public float FovOffset;

        private bool _hasTimeScale;
        private float _timeScale;

        public bool HasTimeScale => _hasTimeScale;
        public float TimeScale => _timeScale;

        public void Reset()
        {
            WorldPositionOffset = Vector3.zero;
            LocalPositionOffset = Vector3.zero;
            LocalEulerOffset = Vector3.zero;
            DistanceOffset = 0f;
            FovOffset = 0f;

            _hasTimeScale = false;
            _timeScale = 1f;
        }

        public void PushTimeScale(float timeScale)
        {
            float clamped = Mathf.Clamp(timeScale, 0.01f, 2f);
            if (_hasTimeScale == false)
            {
                _timeScale = clamped;
                _hasTimeScale = true;
                return;
            }

            _timeScale = Mathf.Min(_timeScale, clamped);
        }
    }
}
