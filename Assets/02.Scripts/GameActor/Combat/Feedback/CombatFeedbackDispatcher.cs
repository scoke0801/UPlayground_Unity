using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UPlayGround.Manager.Combat;
using UPlayGround.Manager.Handler;
using UPlayGround.UI;

namespace UPlayGround.Combat
{
    public static class CombatFeedbackDispatcher
    {
        public static void ShowDamageFloater(in CombatFeedbackContext context)
        {
            UIManager.Instance.ShowDamageFloater(
                context.HitPoint,
                context.DamageAmount,
                context.FloaterStyle);
        }

        public static void ShowHitFx(in CombatFeedbackContext context)
        {
            if (string.IsNullOrWhiteSpace(context.HitFxKey))
                return;

            GameObjectManager.Instance.ShowFX(context.HitFxKey, context.HitPoint);
        }

        public static void ShowHitFx(string hitFxKey, Vector3 hitPoint)
        {
            if (string.IsNullOrWhiteSpace(hitFxKey))
                return;

            GameObjectManager.Instance.ShowFX(hitFxKey, hitPoint);
        }

        public static void ApplyColorHit(ActorColorChanger colorChanger)
        {
            colorChanger?.OnHit();
        }

        public static void ApplyPlayerDamagedCamera(
            bool isHeavyReaction,
            UPlayGround.Data.Path.CameraShakeIdType lightShakeKey,
            UPlayGround.Data.Path.CameraShakeIdType heavyShakeKey)
        {
            CameraManager.Instance.CombatCamera?.PlayPlayerDamaged(isHeavyReaction, lightShakeKey, heavyShakeKey);
        }

        public static void ApplyPlayerDeathFeedback(UPlayGround.Data.Path.CameraShakeIdType deathShakeKey)
        {
            GameCombatManager.Instance.GameHitStop.Execute(GameHitStopHandler.HitStopIntensity.PlayerDie);
            CameraManager.Instance.CombatCamera?.PlayPlayerDeath(deathShakeKey);
        }

        public static FloatStyle GetPlayerAttackFloaterStyle(AttackKind attackKind)
        {
            return attackKind is AttackKind.HeavyAttack
                                or AttackKind.SkillAttack
                                or AttackKind.FinishAttack
                                or AttackKind.ChargeAttack
                ? FloatStyle.Critical
                : FloatStyle.Normal;
        }

        public static void ApplyPlayerAttackHitFeedback(
            AttackData attackData,
            in PlayerAttackHitFeedbackProfile profile)
        {
            if (attackData == null)
                return;

            GameCombatManager.Instance.GameHitStop.ResetActorTimeScale();

            bool isKillHit = attackData.hitTarget != null
                             && !(attackData.hitTarget.GetComponent<IDamageable>()?.IsAlive() ?? true);

            if (isKillHit)
            {
                CameraManager.Instance.CombatCamera?.TryPlayKill(attackData.hitTarget.transform);
                return;
            }

            AttackKind kind = attackData.attackKind;

            VitalOrbTrigger orbTrigger = kind is AttackKind.HeavyAttack or AttackKind.ChargeAttack
                ? VitalOrbTrigger.HeavyAttackHit
                : VitalOrbTrigger.LightAttackHit;
            GameCombatManager.Instance.GameVitalOrb.TrySpawn(orbTrigger, attackData.hitPoint);

            switch (kind)
            {
                case AttackKind.ChargeAttack:
                case AttackKind.SkillAttack:
                    CameraManager.Instance.CombatCamera?.PlayPlayerAttackHit(attackData, profile);
                    GameCombatManager.Instance.GameHitStop.Execute(GameHitStopHandler.HitStopIntensity.Critical);
                    break;

                case AttackKind.HeavyAttack:
                case AttackKind.DashAttack:
                case AttackKind.JumpAttack:
                    CameraManager.Instance.CombatCamera?.PlayPlayerAttackHit(attackData, profile);
                    GameCombatManager.Instance.GameHitStop.Execute(GameHitStopHandler.HitStopIntensity.Heavy);
                    break;

                default:
                    CameraManager.Instance.CombatCamera?.PlayPlayerAttackHit(attackData, profile);
                    GameCombatManager.Instance.GameHitStop.Execute(GameHitStopHandler.HitStopIntensity.Light);
                    break;
            }
        }
    }
}
