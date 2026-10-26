using UnityEngine;

namespace UPlayGround.Data.Combat
{
    /// <summary>실측 속도를 GAS 피해 배율과 적중 히트스톱으로 변환하는 조정 가능한 규칙.</summary>
    [CreateAssetMenu(fileName = "MotionImpactResponse", menuName = "UPlayGround/전투/모션 타격 반응")]
    public sealed class MotionImpactResponseSO : ScriptableObject
    {
        [Tooltip("타격 구간 평균 칼끝 속도(m/s)의 정규화 범위. 루트 병진과 런타임 히트스톱은 제외한다.")]
        public Vector2 speedRange = new(4f, 24f);
        [Tooltip("가로축은 정규화 속도, 세로축은 기존 HitPhase.damage에 곱할 배율이다.")]
        public AnimationCurve damageBySpeed = AnimationCurve.Linear(0f, 1.15f, 1f, 0.85f);
        public Vector2 damageMultiplierLimits = new(0.5f, 1.5f);
        [Tooltip("가로축은 정규화 속도, 세로축은 실제 적중 때의 로컬 히트스톱 초다.")]
        public AnimationCurve hitStopBySpeed = AnimationCurve.Linear(0f, 0.085f, 1f, 0.035f);
        [Min(0f)] public float maximumHitStop = 0.12f;
        [Range(0.001f, 1f)] public float hitStopScale = 0.1f;
        public bool useSwingDirection = true;
        [Min(0f)] public float minimumDirectionSpeed = 0.1f;

        /// <summary>공격속도 버프와 히트스톱으로 기본 피해가 역으로 감소·증가하지 않도록 저작 속도를 사용한다.</summary>
        public void Evaluate(float meanSpeed, out float damageMultiplier, out float hitStopDuration)
        {
            damageMultiplier = 1f;
            hitStopDuration = 0f;
            if (float.IsNaN(meanSpeed) || float.IsInfinity(meanSpeed)) return;
            float speed = Mathf.InverseLerp(speedRange.x, Mathf.Max(speedRange.x + 0.001f, speedRange.y), meanSpeed);
            float damage = damageBySpeed != null ? damageBySpeed.Evaluate(speed) : 1f;
            float stop = hitStopBySpeed != null ? hitStopBySpeed.Evaluate(speed) : 0f;
            if (!float.IsNaN(damage) && !float.IsInfinity(damage))
                damageMultiplier = Mathf.Clamp(damage, Mathf.Max(0f, damageMultiplierLimits.x),
                    Mathf.Max(damageMultiplierLimits.x, damageMultiplierLimits.y));
            if (!float.IsNaN(stop) && !float.IsInfinity(stop))
                hitStopDuration = Mathf.Clamp(stop, 0f, Mathf.Max(0f, maximumHitStop));
        }
    }
}
