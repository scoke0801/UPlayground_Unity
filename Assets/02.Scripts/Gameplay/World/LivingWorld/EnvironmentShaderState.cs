using UnityEngine;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>씬 하나가 소유한 환경 전역값과 기존 TGV 바람 값을 함께 관리한다.</summary>
    internal sealed class EnvironmentShaderState
    {
        private static EnvironmentShaderState _owner;
        private static readonly int Wind = Shader.PropertyToID("_LW_Wind");
        private static readonly int PreviousWind = Shader.PropertyToID("_LW_PreviousWind");
        private int _windFrame = -1;
        private Vector4 _currentWind, _previousWind;
        private static readonly int Wetness = Shader.PropertyToID("_LW_Wetness");
        private static readonly int Direction = Shader.PropertyToID("SICSGlobalWindDirection");
        private static readonly int Strength = Shader.PropertyToID("SICSGlobalWindStrength");
        private static readonly int Speed = Shader.PropertyToID("SICSGlobalWindSpeed");
        private static readonly int Scale = Shader.PropertyToID("SICSGlobalWindScale");
        private static readonly int Jitter = Shader.PropertyToID("SICSGlobalTreeJitter");
        private static readonly int Noise = Shader.PropertyToID("SICSGlobalNoiseTexture");
        private Vector4 _direction;
        private float _strength, _speed, _scale, _jitter;
        private Texture _noise;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _owner?.Release();
            _owner = null;
            Shader.SetGlobalVector(Wind, Vector4.zero);
            Shader.SetGlobalVector(PreviousWind, Vector4.zero);
            Shader.SetGlobalFloat(Wetness, 0);
        }

        /// <summary>중복 환경 작성자를 거부하고 외부 팩의 기존 값을 보관한다.</summary>
        public bool TryAcquire()
        {
            if (_owner != null) return _owner == this;
            _owner = this;
            _direction = Shader.GetGlobalVector(Direction);
            _strength = Shader.GetGlobalFloat(Strength);
            _speed = Shader.GetGlobalFloat(Speed);
            _scale = Shader.GetGlobalFloat(Scale);
            _jitter = Shader.GetGlobalFloat(Jitter);
            _noise = Shader.GetGlobalTexture(Noise);
            return true;
        }

        /// <summary>기존 식생과 물이 동일한 풍향·세기를 소비하게 한다.</summary>
        public void Apply(EnvironmentSchedule.Sample sample, float strength, float speed, float scale, float jitter, Texture noise)
        {
            if (_owner != this) return;
            Vector2 direction = sample.Wind.normalized;
            float magnitude = sample.Wind.magnitude;
            if (_windFrame != Time.frameCount) { _previousWind = _currentWind; _windFrame = Time.frameCount; }
            _currentWind = new Vector4(sample.Wind.x, 0, sample.Wind.y, magnitude);
            Shader.SetGlobalVector(PreviousWind, _previousWind);
            Shader.SetGlobalVector(Wind, _currentWind);
            Shader.SetGlobalFloat(Wetness, sample.Wetness);
            Shader.SetGlobalVector(Direction, new Vector4(direction.x, 0, direction.y, 0));
            Shader.SetGlobalFloat(Strength, magnitude * strength);
            Shader.SetGlobalFloat(Speed, magnitude * speed);
            Shader.SetGlobalFloat(Scale, scale);
            Shader.SetGlobalFloat(Jitter, magnitude * jitter);
            if (noise != null) Shader.SetGlobalTexture(Noise, noise);
        }

        /// <summary>지연된 해제가 다음 씬의 값을 덮지 않도록 소유자일 때만 복구한다.</summary>
        public void Release()
        {
            if (_owner != this) return;
            Shader.SetGlobalVector(Wind, Vector4.zero);
            Shader.SetGlobalVector(PreviousWind, Vector4.zero);
            Shader.SetGlobalFloat(Wetness, 0);
            Shader.SetGlobalVector(Direction, _direction);
            Shader.SetGlobalFloat(Strength, _strength);
            Shader.SetGlobalFloat(Speed, _speed);
            Shader.SetGlobalFloat(Scale, _scale);
            Shader.SetGlobalFloat(Jitter, _jitter);
            Shader.SetGlobalTexture(Noise, _noise);
            _owner = null;
        }
    }
}
