using UnityEngine;

namespace UPlayGround.Data.World
{
    /// <summary>GPU 장식 생물의 외형과 이동·출현 조건을 공유하며 런타임 개체 상태는 보관하지 않는다.</summary>
    [CreateAssetMenu(menuName = "UPlayGround/World/Ambient Fauna Profile")]
    public sealed class AmbientFaunaProfileSO : ScriptableObject
    {
        public string SpeciesId;
        public ComputeShader Simulation;
        public Mesh Mesh;
        public Material Material;
        public Vector2 SizeRange = new(0.12f, 0.18f);
        public Vector2 SpeedRange = new(0.2f, 0.55f);
        [Min(0.01f)] public float Acceleration = 2;
        [Min(0.01f)] public float FleeSpeed = 1.6f;
        [Min(0.01f)] public float AvoidRadius = 2;
        [Range(0, 1)] public float WindInfluence = 0.15f;
        [Min(0)] public float FlapFrequency = 7;
        [Range(0, 85)] public float FlapAngle = 60;
        public AnimationCurve HourDensity = AnimationCurve.Linear(0, 1, 24, 1);
        [Range(0, 1)] public float RainDensity = 0.1f;
        [Range(0, 1)] public float SnowDensity;
        [Min(0.01f)] public float FadeSeconds = 0.8f;
        [Min(0)] public float ActivationDistance = 35;
        [Min(0)] public float ReleaseDistance = 45;
        public Vector2 NearFade = new(0.3f, 1.2f);
        public uint Revision { get; private set; }
        private void OnValidate() => Revision++;

        /// <summary>GPU 할당 전에 잘못된 수치와 누락된 참조를 거절한다.</summary>
        public bool TryValidate(out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(SpeciesId) || Simulation == null || Mesh == null || Material == null)
                error = "종 ID / Compute Shader / Mesh / Material 연결이 필요합니다.";
            else if (Material.shader == null || !Material.shader.isSupported || Material.shader.name != "UPlayGround/Ambient Fauna")
                error = "Ambient Fauna 전용 머티리얼이 필요합니다.";
            else if (Mesh.subMeshCount != 1 || Mesh.vertexCount == 0)
                error = "GPU 전용 단일 submesh가 필요합니다.";
            else if (!IsRangeValid(SizeRange, 0.001f) || !IsRangeValid(SpeedRange, 0.001f)
                || !IsRangeValid(NearFade, 0) || NearFade.y <= NearFade.x
                || !IsFinite(Acceleration) || Acceleration <= 0 || !IsFinite(FleeSpeed) || FleeSpeed <= 0
                || !IsFinite(AvoidRadius) || AvoidRadius <= 0 || !IsFinite(FadeSeconds) || FadeSeconds <= 0
                || !IsFinite(FlapFrequency) || FlapFrequency < 0 || !IsFinite(FlapAngle) || FlapAngle < 0 || FlapAngle > 85
                || !IsFinite(WindInfluence) || WindInfluence < 0 || WindInfluence > 1
                || !IsFinite(ActivationDistance) || ActivationDistance < 0
                || !IsFinite(ReleaseDistance) || ReleaseDistance < ActivationDistance
                || !IsFraction(RainDensity) || !IsFraction(SnowDensity))
                error = "종 프로필의 범위 또는 수치가 유효하지 않습니다.";
            else if (HourDensity == null || HourDensity.length == 0)
                error = "시간대별 밀도 곡선이 필요합니다.";
            else
            {
                foreach (var key in HourDensity.keys)
                {
                    if (IsFinite(key.time) && IsFraction(key.value)
                        && IsFinite(key.inTangent) && IsFinite(key.outTangent)) continue;
                    error = "밀도 곡선에 NaN/Infinity 또는 0~1 밖의 값이 있습니다.";
                    break;
                }
            }
            return error == null;
        }

        /// <summary>자정 경계와 날씨를 적용한 표시 목표를 계산한다. GPU 할당 용량과는 별개다.</summary>
        public float EvaluateDensity(float hour, float rain, float snow)
        {
            if (!IsFinite(hour) || !IsFinite(rain) || !IsFinite(snow) || HourDensity == null) return 0;
            float density = HourDensity.Evaluate(Mathf.Repeat(hour, 24));
            return IsFinite(density) ? Mathf.Clamp01(density)
                * Mathf.Lerp(1, RainDensity, Mathf.Clamp01(rain))
                * Mathf.Lerp(1, SnowDensity, Mathf.Clamp01(snow)) : 0;
        }

        /// <summary>직렬화된 수치의 NaN과 Infinity를 검사한다.</summary>
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool IsFraction(float value) => IsFinite(value) && value >= 0 && value <= 1;
        private static bool IsRangeValid(Vector2 value, float minimum) => IsFinite(value.x)
            && IsFinite(value.y) && value.x >= minimum && value.y >= value.x;
    }
}
