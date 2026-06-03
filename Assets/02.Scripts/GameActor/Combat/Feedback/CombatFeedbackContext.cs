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
    }
}
