using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.UI;
using UnityEngine;

namespace UPlayGround.Component
{
    /// <summary>플레이어 공격의 숫자, 히트 FX, 카메라와 히트스톱 표현 책임.</summary>
    public sealed class PlayerCombatPresenter
    {
        private readonly PlayerAttackHitFeedbackProfile _profile;

        public PlayerCombatPresenter(PlayerAttackHitFeedbackProfile profile) => _profile = profile;

        public void ShowHit(in CombatResult result)
        {
            if (!result.DamageApplied || result.FinalDamage <= 0f) return;
            var context = new CombatFeedbackContext(
                result.Hit,
                result.Hit.HitPoint,
                result.Hit.AttackDirection,
                result.Hit.HitTarget,
                result.FinalDamage,
                result.FloaterStyle,
                ResolveHitFxKey(result.Hit));
            CombatFeedbackDispatcher.ShowDamageFloater(context);
            CombatFeedbackDispatcher.ShowHitFx(context);
            CombatFeedbackDispatcher.PlayDamageImpact(result.Hit);
        }

        public void ApplyImpact(AttackData attackData, bool protectParryWindow)
        {
            if (attackData == null) return;
            if (protectParryWindow && !attackData.isCounterAttack && !attackData.useCounterHitFeedback)
                return;
            CombatFeedbackDispatcher.ApplyPlayerAttackHitFeedback(attackData, _profile);
        }

        private static string ResolveHitFxKey(in HitContext hit)
            => !string.IsNullOrWhiteSpace(hit.HitParticleName)
                ? hit.HitParticleName
                : FXKeyType.DefaultCombatHit.ToKey();
    }
}
