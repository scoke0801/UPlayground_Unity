using UnityEngine;
using UPlayGround.UI;

namespace UPlayGround.Combat
{
    public readonly struct CombatFeedbackContext
    {
        public readonly HitContext Hit;
        public readonly Vector3 HitPoint;
        public readonly Vector3 AttackDirection;
        public readonly GameObject HitTarget;
        public readonly float DamageAmount;
        public readonly FloatStyle FloaterStyle;
        public readonly string HitFxKey;

        public CombatFeedbackContext(
            HitContext hit,
            Vector3 hitPoint,
            Vector3 attackDirection,
            GameObject hitTarget,
            float damageAmount,
            FloatStyle floaterStyle,
            string hitFxKey = null)
        {
            Hit = hit;
            HitPoint = hitPoint;
            AttackDirection = attackDirection;
            HitTarget = hitTarget;
            DamageAmount = damageAmount;
            FloaterStyle = floaterStyle;
            HitFxKey = hitFxKey;
        }

        /// <summary><see cref="CombatResult"/>로부터 피드백 입력을 만든다.</summary>
        public static CombatFeedbackContext FromCombatResult(
            in CombatResult result,
            Vector3 fallbackPosition,
            string hitFxKey = null)
        {
            Vector3 hitPoint = result.Hit.HitPoint != Vector3.zero
                ? result.Hit.HitPoint
                : fallbackPosition;

            return new CombatFeedbackContext(
                result.Hit,
                hitPoint,
                result.Hit.AttackDirection,
                result.Hit.HitTarget,
                result.Damage.FinalDamage,
                result.Damage.FloaterStyle,
                hitFxKey);
        }
    }
}
