using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.Manager.Combat;
using UPlayGround.Manager.Handler;
using UPlayGround.UI;

namespace UPlayGround.Combat
{
    public static class CombatFeedbackDispatcher
    {
        // 플레이어 공격 적중 시 피격자(적)는 항상 풀프리즈하고 공격자(플레이어)만 reactionData의 약한 스케일로 멈춘다.
        // 사람 눈은 0%↔10% 차이는 잘 못 느끼지만, 공격자 쪽 루트모션/카메라가 미세하게 진행되어 조작감이 끊기지 않는다.
        private const float VictimFreezeScale = 0f; // ExecuteLocalImpact에서 MinImpactTimeScale로 클램프 → 풀프리즈

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

        public static void ApplyPlayerDamagedHitStop(AttackData incomingAttack, GameActor victim)
        {
            if (incomingAttack == null || GameCombatManager.Instance == null)
                return;

            ResolveIncomingHitStop(incomingAttack, out float duration, out float localScale, out float globalDuration, out float globalScale);
            if (duration <= 0f)
                return;

            // 다인 전투 누수 차단: 플레이어가 이미 피격 히트스톱 중이면 피해자 freeze만 재시작하지 않는다.
            // 공격자 히트스톱과 global pulse는 타격 피드백이므로 유지한다.
            if (victim != null && GameCombatManager.Instance.GameHitStop.IsActorHitStopping(victim))
            {
                if (incomingAttack.attacker != null)
                    GameCombatManager.Instance.GameHitStop.ExecuteActorOnly(incomingAttack.attacker, duration, localScale);

                if (globalDuration > 0f)
                    GameCombatManager.Instance.GameHitStop.Execute(globalDuration, globalScale);

                return;
            }

            GameCombatManager.Instance.GameHitStop.ExecuteLocalImpact(
                incomingAttack.attacker,
                victim,
                duration,
                localScale,
                includeAttacker: true);

            if (globalDuration > 0f)
                GameCombatManager.Instance.GameHitStop.Execute(globalDuration, globalScale);
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

            AttackKind kind = attackData.attackKind;
            bool isKillHit = attackData.hitTarget != null
                             && !(attackData.hitTarget.GetComponent<IDamageable>()?.IsAlive() ?? true);

            if (isKillHit)
            {
                ApplyPlayerKillHitFeedback(attackData, profile);
                return;
            }

            VitalOrbTrigger orbTrigger = kind is AttackKind.HeavyAttack or AttackKind.ChargeAttack
                ? VitalOrbTrigger.HeavyAttackHit
                : VitalOrbTrigger.LightAttackHit;
            GameCombatManager.Instance.GameVitalOrb.TrySpawn(orbTrigger, attackData.hitPoint);

            if (attackData.isCounterAttack || attackData.useCounterHitFeedback)
            {
                CameraManager.Instance.CombatCamera?.PlayPlayerAttackHit(attackData, profile);
                ApplyPlayerAttackLocalHitStop(attackData, 0.14f, 0.01f, 0.045f);
                return;
            }

            switch (kind)
            {
                case AttackKind.ChargeAttack:
                case AttackKind.SkillAttack:
                    CameraManager.Instance.CombatCamera?.PlayPlayerAttackHit(attackData, profile);
                    ApplyPlayerAttackLocalHitStop(attackData, 0.13f, 0.01f, 0.04f);
                    break;

                case AttackKind.HeavyAttack:
                case AttackKind.DashAttack:
                case AttackKind.JumpAttack:
                    CameraManager.Instance.CombatCamera?.PlayPlayerAttackHit(attackData, profile);
                    ApplyPlayerAttackLocalHitStop(attackData, 0.10f, 0.015f, 0.035f);
                    break;

                default:
                    CameraManager.Instance.CombatCamera?.PlayPlayerAttackHit(attackData, profile);
                    ApplyPlayerAttackLocalHitStop(attackData, 0.06f, 0.03f, 0.025f);
                    break;
            }
        }

        private static void ApplyPlayerKillHitFeedback(
            AttackData attackData,
            in PlayerAttackHitFeedbackProfile profile)
        {
            CameraManager.Instance.CombatCamera?.PlayPlayerAttackHit(attackData, profile);

            float duration = attackData.isCounterAttack || attackData.useCounterHitFeedback
                ? 0.15f
                : attackData.attackKind is AttackKind.SkillAttack or AttackKind.ChargeAttack or AttackKind.HeavyAttack
                    ? 0.13f
                    : 0.10f;

            ApplyPlayerAttackLocalHitStop(
                attackData,
                duration,
                0.01f,
                0.05f,
                useReactionData: false);

            CameraManager.Instance.CombatCamera?.TryPlayKill(attackData.hitTarget.transform);
        }

        public static void ApplyPlayerSpecialBreakHitStop(
            GameActor attacker,
            GameActor victim,
            float duration)
        {
            ApplyPlayerSpecialBreakImpactFeedback(
                attacker,
                victim,
                victim != null ? victim.transform.position : Vector3.zero,
                duration,
                0.01f,
                Mathf.Min(duration, 0.05f),
                0.02f,
                CameraShakeIdType.CriticalHit,
                0.26f,
                0.16f);
        }

        public static void ApplyPlayerSpecialBreakImpactFeedback(
            GameActor attacker,
            GameActor victim,
            Vector3 hitPoint,
            float duration,
            float localTimeScale,
            float globalPulseDuration,
            float globalPulseScale,
            CameraShakeIdType cameraShakeKey,
            float cameraPunchStrength,
            float cameraPunchDuration)
        {
            if (GameCombatManager.Instance == null)
                return;

            duration = Mathf.Max(0f, duration);
            localTimeScale = Mathf.Clamp(localTimeScale, 0.001f, 1f);
            if (duration > 0f)
            {
                GameCombatManager.Instance.GameHitStop.ExecuteLocalImpact(
                    attacker,
                    victim,
                    duration,
                    localTimeScale,
                    includeAttacker: true);
            }

            globalPulseDuration = Mathf.Max(0f, globalPulseDuration);
            if (globalPulseDuration > 0f)
            {
                GameCombatManager.Instance.GameHitStop.Execute(
                    globalPulseDuration,
                    Mathf.Clamp(globalPulseScale, 0.001f, 1f));
            }

            Vector3 hitDirection = ResolveHitDirection(attacker, victim);
            CameraManager.Instance.CombatCamera?.Play(new CombatCameraIntent(
                CombatCameraIntentType.SkillHit,
                attacker != null ? attacker.transform : null,
                victim != null ? victim.transform : null,
                hitPoint,
                hitDirection,
                AttackKind.SkillAttack,
                AttackReactionType.Knockdown,
                cameraShakeKey,
                Mathf.Max(0f, cameraPunchStrength),
                Mathf.Max(0f, cameraPunchDuration)));
        }

        private static void ApplyPlayerAttackLocalHitStop(
            AttackData attackData,
            float fallbackDuration,
            float fallbackTimeScale,
            float globalPulseDuration,
            bool useReactionData = true)
        {
            if (attackData == null || GameCombatManager.Instance == null)
                return;

            float duration = fallbackDuration;
            float timeScale = fallbackTimeScale;

            if (useReactionData && TryGetReactionHitStop(attackData.reactionData, out float reactionDuration, out float reactionScale))
            {
                duration = reactionDuration;
                timeScale = reactionScale;
            }

            GameActor victim = attackData.hitTarget != null
                ? attackData.hitTarget.GetComponentInParent<GameActor>()
                : null;

            GameCombatManager.Instance.GameHitStop.ExecuteLocalImpact(
                attackData.attacker,
                victim,
                duration,
                timeScale,
                includeAttacker: true,
                victimTimeScale: VictimFreezeScale);

            ResolvePlayerAttackGlobalPulse(
                attackData,
                duration,
                timeScale,
                globalPulseDuration,
                out float pulseDuration,
                out float pulseScale);

            if (pulseDuration > 0f)
                GameCombatManager.Instance.GameHitStop.Execute(pulseDuration, pulseScale);
        }

        private static bool TryGetReactionHitStop(
            AttackReactionData reactionData,
            out float duration,
            out float timeScale)
        {
            duration = 0f;
            timeScale = 1f;

            if (reactionData == null || reactionData.hitStopDuration <= 0f)
                return false;

            duration = Mathf.Max(0f, reactionData.hitStopDuration);
            timeScale = Mathf.Clamp(reactionData.hitStopScale, 0.001f, 1f);
            return true;
        }

        private static void ResolvePlayerAttackGlobalPulse(
            AttackData attackData,
            float localDuration,
            float localScale,
            float fallbackGlobalDuration,
            out float pulseDuration,
            out float pulseScale)
        {
            pulseDuration = fallbackGlobalDuration;
            pulseScale = 0.05f;

            if (attackData == null)
                return;

            switch (attackData.reactionType)
            {
                case AttackReactionType.Heavy:
                case AttackReactionType.KnockBack:
                case AttackReactionType.Airborne:
                case AttackReactionType.Knockdown:
                case AttackReactionType.Stun:
                case AttackReactionType.Grab:
                    pulseDuration = Mathf.Max(pulseDuration, Mathf.Min(localDuration * 0.45f, 0.055f));
                    pulseScale = Mathf.Min(0.02f, localScale);
                    break;

                case AttackReactionType.Light:
                case AttackReactionType.Hit:
                    pulseDuration = Mathf.Min(pulseDuration, 0.025f);
                    pulseScale = Mathf.Min(0.08f, Mathf.Max(localScale, 0.03f));
                    break;

                default:
                    pulseDuration = 0f;
                    pulseScale = 1f;
                    break;
            }
        }

        private static void ResolveIncomingHitStop(
            AttackData attackData,
            out float duration,
            out float localScale,
            out float globalDuration,
            out float globalScale)
        {
            if (TryGetReactionHitStop(attackData.reactionData, out duration, out localScale))
            {
                ResolvePlayerAttackGlobalPulse(
                    attackData,
                    duration,
                    localScale,
                    0.02f,
                    out globalDuration,
                    out globalScale);
                return;
            }

            switch (attackData.reactionType)
            {
                case AttackReactionType.Heavy:
                case AttackReactionType.KnockBack:
                case AttackReactionType.Airborne:
                case AttackReactionType.Knockdown:
                case AttackReactionType.Stun:
                case AttackReactionType.Grab:
                    duration = 0.09f;
                    localScale = 0.015f;
                    globalDuration = 0.04f;
                    globalScale = 0.03f;
                    break;

                case AttackReactionType.Light:
                    duration = 0.04f;
                    localScale = 0.08f;
                    globalDuration = 0.015f;
                    globalScale = 0.12f;
                    break;

                case AttackReactionType.Hit:
                    duration = 0.06f;
                    localScale = 0.05f;
                    globalDuration = 0.02f;
                    globalScale = 0.08f;
                    break;

                default:
                    duration = 0f;
                    localScale = 1f;
                    globalDuration = 0f;
                    globalScale = 1f;
                    break;
            }
        }

        private static Vector3 ResolveHitDirection(GameActor attacker, GameActor victim)
        {
            if (attacker != null && victim != null)
            {
                Vector3 direction = victim.transform.position - attacker.transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.0001f)
                    return direction.normalized;
            }

            if (attacker != null)
                return attacker.transform.forward;

            return Vector3.forward;
        }
    }
}
