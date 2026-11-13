using UnityEngine;

namespace UPlayGround.Data.World
{
    /// <summary>CPU 수면 조회와 GPU 파도·접촉 파문의 시간과 수치를 공유한다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/World/Water Dynamics")]
    public sealed class WaterDynamicsProfileSO : ScriptableObject
    {
        [System.Serializable]
        public struct Wave
        {
            public Vector2 direction;
            [Min(0f)] public float amplitude;
            [Min(0.1f)] public float wavelength;
            public float speed;

            /// <summary>위상 계산용 파수 벡터·진폭·각속도를 반환한다.</summary>
            public Vector4 GetParameters()
            {
                Vector2 axis = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
                float waveNumber = 2f * Mathf.PI / Mathf.Max(0.1f, wavelength);
                return new Vector4(axis.x * waveNumber, axis.y * waveNumber, amplitude, speed * waveNumber);
            }
        }

        public Wave firstWave = new() { direction = new Vector2(1f, 0.3f), amplitude = 0.08f, wavelength = 12f, speed = 0.8f };
        public Wave secondWave = new() { direction = new Vector2(-0.4f, 1f), amplitude = 0.04f, wavelength = 7f, speed = 0.5f };
        [Min(0.01f)] public float rippleDuration = 2.5f;
        [Min(0.01f)] public float rippleSpeed = 1.8f;
        [Min(0.01f)] public float rippleWidth = 0.18f;
        [Range(0f, 1f)] public float rippleStrength = 0.1f;
        [Min(0.01f)] public float contactInterval = 0.25f;
        [Min(0f)] public float entryStrength = 1f;
        [Min(0f)] public float swimStrength = 0.35f;
        [Min(0f)] public float stepStrength = 0.4f;
        [Range(0f, 1f)] public float rainStrength = 0.05f;
        [Min(0.1f)] public float rainCellSize = 0.7f;
        [Min(0.1f)] public float rainFrequency = 1.8f;

        public float MaximumDisplacement => firstWave.amplitude + secondWave.amplitude;
        public float MaximumSlope
        {
            get
            {
                Vector4 first = firstWave.GetParameters(), second = secondWave.GetParameters();
                return Mathf.Abs(first.z) * new Vector2(first.x, first.y).magnitude
                    + Mathf.Abs(second.z) * new Vector2(second.x, second.y).magnitude;
            }
        }

        /// <summary>시각 정점과 같은 월드 XZ·게임 시계로 큰 파도의 높이 오프셋을 구한다.</summary>
        public float EvaluateHeight(Vector2 position, float time)
        {
            return Evaluate(firstWave.GetParameters(), position, time) + Evaluate(secondWave.GetParameters(), position, time);
        }

        private static float Evaluate(Vector4 wave, Vector2 position, float time) =>
            wave.z * Mathf.Sin(position.x * wave.x + position.y * wave.y - time * wave.w);
    }
}
