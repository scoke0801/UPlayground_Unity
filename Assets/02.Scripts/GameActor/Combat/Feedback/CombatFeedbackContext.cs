using UnityEngine;
using UPlayGround.Data;
using UPlayGround.UI;

namespace UPlayGround.Combat
{
    public readonly struct CombatFeedbackContext
    {
        public readonly AttackData AttackData;
        public readonly Vector3 HitPoint;
        public readonly Vector3 AttackDirection;
        public readonly GameObject HitTarget;
        public readonly float DamageAmount;
        public readonly FloatStyle FloaterStyle;
        public readonly string HitFxKey;

        public CombatFeedbackContext(
            AttackData attackData,
            Vector3 hitPoint,
            Vector3 attackDirection,
            GameObject hitTarget,
            float damageAmount,
            FloatStyle floaterStyle,
            string hitFxKey = null)
        {
            AttackData = attackData;
            HitPoint = hitPoint;
            AttackDirection = attackDirection;
            HitTarget = hitTarget;
            DamageAmount = damageAmount;
            FloaterStyle = floaterStyle;
            HitFxKey = hitFxKey;
        }

        public static CombatFeedbackContext FromDamageResult(
            AttackData attackData,
            DamageResult damageResult,
            Vector3 fallbackPosition,
            string hitFxKey = null)
        {
            Vector3 hitPoint = attackData != null && attackData.hitPoint != Vector3.zero
                ? attackData.hitPoint
                : fallbackPosition;

            return new CombatFeedbackContext(
                attackData,
                hitPoint,
                attackData?.attackDirection ?? Vector3.zero,
                attackData?.hitTarget,
                damageResult.FinalDamage,
                damageResult.FloaterStyle,
                hitFxKey);
        }

        /// <summary>
        /// <see cref="CombatResult"/>로부터 피드백 컨텍스트를 만든다 (P1).
        /// <see cref="FromDamageResult"/>와 값이 동일하도록: raw hitPoint(zero면 fallback) 판정과
        /// 원본 AttackData(Source) 전달을 그대로 유지한다(다운스트림이 hitParticleName/attackKind를 읽는다).
        /// </summary>
        public static CombatFeedbackContext FromCombatResult(
            in CombatResult result,
            Vector3 fallbackPosition,
            string hitFxKey = null)
        {
            AttackData attackData = result.Hit.Source;
            Vector3 hitPoint = result.Hit.HitPoint != Vector3.zero
                ? result.Hit.HitPoint
                : fallbackPosition;

            return new CombatFeedbackContext(
                attackData,
                hitPoint,
                result.Hit.AttackDirection,
                result.Hit.HitTarget,
                result.Damage.FinalDamage,
                result.Damage.FloaterStyle,
                hitFxKey);
        }
    }
}
