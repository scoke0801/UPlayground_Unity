using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Manager;
using UPlayGround.Data.Sound;
using UPlayGround.UI;

namespace UPlayGround.Combat
{
    public static class CombatFeedbackDispatcher
    {
        // 매니저 참조 캐싱 — 반복 레지스트리/Instance 조회 방지.
        // 인터페이스 캐시는 파괴된 매니저(fake-null)를 C# null 체크로 감지하지 못하므로,
        // 도메인 리로드 비활성화 환경에서 세션 간 stale 참조가 남지 않도록
        // ResetStaticCaches()가 매 플레이 진입 시(SubsystemRegistration) 캐시를 비운다.
        private static IActorCombatService _cachedGameCombatManager;
        private static IActorCombatService GameCombatMgr => _cachedGameCombatManager ??= ActorSvc.Combat;
        private static CameraManager _cachedCameraManager;
        private static CameraManager CameraMgr => _cachedCameraManager != null ? _cachedCameraManager : (_cachedCameraManager = CameraManager.Instance);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticCaches()
        {
            _cachedGameCombatManager = null;
            _cachedCameraManager = null;
        }

        private static CombatCameraAttackContext ToCameraContext(AttackData attackData)
        {
            return new CombatCameraAttackContext(
                attackData?.attacker != null ? attackData.attacker.transform : null,
                attackData?.hitTarget != null ? attackData.hitTarget.transform : null,
                attackData?.hitPoint ?? Vector3.zero,
                attackData?.attackDirection ?? Vector3.zero,
                attackData?.attackKind ?? AttackKind.NormalAttack,
                attackData?.reactionType ?? AttackReactionType.Hit);
        }

        // 플레이어 공격 적중 시 피격자(적)는 항상 풀프리즈하고 공격자(플레이어)만 reactionData의 약한 스케일로 멈춘다.
        // 사람 눈은 0%↔10% 차이는 잘 못 느끼지만, 공격자 쪽 루트모션/카메라가 미세하게 진행되어 조작감이 끊기지 않는다.
        private const float VictimFreezeScale = 0f; // ExecuteLocalImpact에서 MinImpactTimeScale로 클램프 → 풀프리즈

        public static void ShowDamageFloater(in CombatFeedbackContext context)
        {
            ActorSvc.UI.ShowDamageFloater(
                context.HitPoint,
                context.DamageAmount,
                context.FloaterStyle);
        }

        public static void ShowHitFx(in CombatFeedbackContext context)
        {
            ShowHitFx(context.HitFxKey, context.HitPoint, context.AttackDirection);
        }

        public static void ShowHitFx(string hitFxKey, Vector3 hitPoint)
        {
            ShowHitFx(hitFxKey, hitPoint, Vector3.zero);
        }

        /// <summary>
        /// 타격 방향으로 정렬한 히트 FX를 표시한다.
        /// attackDirection이 유효하지 않으면 회전을 지정하지 않아 프리팹 자체 회전이 유지된다.
        /// </summary>
        public static void ShowHitFx(string hitFxKey, Vector3 hitPoint, Vector3 attackDirection)
        {
            if (string.IsNullOrWhiteSpace(hitFxKey))
                return;

            ActorSvc.Objects.ShowFX(hitFxKey, hitPoint, ResolveHitFxRotation(attackDirection));
        }

        /// <summary>
        /// GameObjectManager.ShowFX는 default(zero quaternion)를 "프리팹 회전 유지"로 해석하고,
        /// 유효한 회전이 오면 "지정 회전 * 프리팹 회전"으로 합성한다. 그 계약에 맞춰 값을 만든다.
        /// </summary>
        private static Quaternion ResolveHitFxRotation(Vector3 attackDirection)
        {
            if (attackDirection.sqrMagnitude <= 0.000001f)
                return default;

            Vector3 forward = attackDirection.normalized;

            // 거의 수직인 타격(내려찍기 등)에서 up 힌트가 forward와 평행해지면
            // LookRotation이 퇴화한다. 그 경우에만 힌트 축을 바꾼다.
            Vector3 upHint = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.999f
                ? Vector3.forward
                : Vector3.up;

            return Quaternion.LookRotation(forward, upHint);
        }

        public static void ApplyColorHit(ActorColorChanger colorChanger)
        {
            colorChanger?.OnHit();
        }

        /// <summary>
        /// 실제 피해가 적용된 순간에 재생하는 충돌음.
        /// 공격 모션의 휘두르기음과 분리해 헛스윙에는 재생하지 않는다.
        /// </summary>
        public static void PlayDamageImpact(in HitContext hit)
        {
            PlayDamageImpact(hit, false);
        }

        /// <summary>
        /// 피해가 적용된 순간의 충돌음. 소유권은 <b>피격자</b>에 있다
        /// (투사체·잔류 판정·환경 피해까지 한 지점에서 덮이도록 공격자측에서 부르지 않는다).
        /// </summary>
        public static void PlayDamageImpact(in CombatResult result)
        {
            PlayDamageImpact(result.Hit, result.Damage.IsCritical);
        }

        public static void PlayDamageImpact(in HitContext hit, bool isCritical)
        {
            Vector3 position = hit.HitPoint;
            if (position == Vector3.zero && hit.Victim != null)
                position = hit.Victim.transform.position;

            Svc.Sound?.PlaySfx(ResolveImpactSoundKey(hit, isCritical), position);
        }

        /// <summary>
        /// 임팩트 티어를 해석한다. 상위 티어 키가 아직 저작되지 않았으면 Heavy/Light로 폴백해
        /// 사운드 데이터가 없는 동안에도 충돌음이 사라지지 않게 한다.
        /// </summary>
        private static string ResolveImpactSoundKey(in HitContext hit, bool isCritical)
        {
            if (isCritical && HasSound(GameSoundKey.CombatHitCritical))
                return GameSoundKey.CombatHitCritical;

            if (IsBreakImpact(hit) && HasSound(GameSoundKey.CombatHitBreak))
                return GameSoundKey.CombatHitBreak;

            return IsHeavyImpact(hit)
                ? GameSoundKey.CombatHitHeavy
                : GameSoundKey.CombatHitLight;
        }

        private static bool HasSound(string key) => Svc.Sound?.HasSound(key) == true;

        // 행동 불능으로 몰아넣는 리액션은 일반 강타와 다른 큐를 준다.
        private static bool IsBreakImpact(in HitContext hit)
            => hit.ReactionType is AttackReactionType.Stun
                or AttackReactionType.Knockdown
                or AttackReactionType.Grab;

        public static void ApplyPlayerDamagedCamera(
            bool isHeavyReaction,
            UPlayGround.Data.Path.CameraShakeIdType lightShakeKey,
            UPlayGround.Data.Path.CameraShakeIdType heavyShakeKey)
        {
            CameraMgr?.CombatCamera?.PlayPlayerDamaged(isHeavyReaction, lightShakeKey, heavyShakeKey);
        }

        public static void ApplyPlayerDamagedHitStop(AttackData incomingAttack, GameActor victim)
        {
            if (incomingAttack == null || GameCombatMgr == null)
                return;

            ResolveIncomingHitStop(incomingAttack, out float duration, out float localScale, out float globalDuration, out float globalScale);
            if (duration <= 0f)
                return;

            // 다인 전투 누수 차단: 플레이어가 이미 피격 히트스톱 중이면 피해자 freeze만 재시작하지 않는다.
            // 공격자 히트스톱과 global pulse는 타격 피드백이므로 유지한다.
            if (victim != null && GameCombatMgr?.IsActorHitStopping(victim) == true)
            {
                if (incomingAttack.attacker != null)
                    GameCombatMgr?.ExecuteActorHitStop(incomingAttack.attacker, duration, localScale);

                if (globalDuration > 0f)
                    GameCombatMgr?.ExecuteHitStop(globalDuration, globalScale);

                return;
            }

            GameCombatMgr?.ExecuteLocalImpact(
                incomingAttack.attacker,
                victim,
                duration,
                localScale,
                includeAttacker: true);

            if (globalDuration > 0f)
                GameCombatMgr?.ExecuteHitStop(globalDuration, globalScale);
        }

        public static void ApplyPlayerDeathFeedback(UPlayGround.Data.Path.CameraShakeIdType deathShakeKey)
        {
            GameCombatMgr?.ExecutePlayerDeathHitStop();
            CameraMgr?.CombatCamera?.PlayPlayerDeath(deathShakeKey);
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

            GameCombatMgr?.ResetActorHitStop();

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
            bool weightPolicyHandled = attackData.attacker is PlayerActor player
                                       && player.TrySpawnWeightRecovery(attackData.hitPoint, orbTrigger, false);
            if (!weightPolicyHandled)
                GameCombatMgr?.TrySpawnVitalOrb(orbTrigger, attackData.hitPoint);

            if (attackData.isCounterAttack || attackData.useCounterHitFeedback)
            {
                CameraMgr?.CombatCamera?.PlayPlayerAttackHit(ToCameraContext(attackData), profile);
                ApplyPlayerAttackLocalHitStop(attackData, 0.14f, 0.01f, 0.045f);
                return;
            }

            switch (kind)
            {
                case AttackKind.ChargeAttack:
                case AttackKind.SkillAttack:
                    CameraMgr?.CombatCamera?.PlayPlayerAttackHit(ToCameraContext(attackData), profile);
                    ApplyPlayerAttackLocalHitStop(attackData, 0.13f, 0.01f, 0.04f);
                    break;

                case AttackKind.HeavyAttack:
                case AttackKind.DashAttack:
                case AttackKind.JumpAttack:
                    CameraMgr?.CombatCamera?.PlayPlayerAttackHit(ToCameraContext(attackData), profile);
                    ApplyPlayerAttackLocalHitStop(attackData, 0.10f, 0.015f, 0.035f);
                    break;

                default:
                    CameraMgr?.CombatCamera?.PlayPlayerAttackHit(ToCameraContext(attackData), profile);
                    ApplyPlayerAttackLocalHitStop(attackData, 0.06f, 0.03f, 0.025f);
                    break;
            }
        }

        private static void ApplyPlayerKillHitFeedback(
            AttackData attackData,
            in PlayerAttackHitFeedbackProfile profile)
        {
            CameraMgr?.CombatCamera?.PlayPlayerAttackHit(ToCameraContext(attackData), profile);

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

            CameraMgr?.CombatCamera?.TryPlayKill(attackData.hitTarget.transform);
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
            if (GameCombatMgr == null)
                return;

            duration = Mathf.Max(0f, duration);
            localTimeScale = Mathf.Clamp(localTimeScale, 0.001f, 1f);
            if (duration > 0f)
            {
                GameCombatMgr.ExecuteLocalImpact(
                    attacker,
                    victim,
                    duration,
                    localTimeScale,
                    includeAttacker: true);
            }

            globalPulseDuration = Mathf.Max(0f, globalPulseDuration);
            if (globalPulseDuration > 0f)
            {
                GameCombatMgr.ExecuteHitStop(
                    globalPulseDuration,
                    Mathf.Clamp(globalPulseScale, 0.001f, 1f));
            }

            Vector3 hitDirection = ResolveHitDirection(attacker, victim);
            CameraMgr?.CombatCamera?.Play(new CombatCameraIntent(
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
            if (attackData == null || GameCombatMgr == null)
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

            if (attackData.isCounterAttack || attackData.useCounterHitFeedback)
                GameCombatMgr.StopDefenseFeedbackForCounterAttack(attackData.attacker);

            GameCombatMgr.ExecuteLocalImpact(
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
                GameCombatMgr.ExecuteHitStop(pulseDuration, pulseScale);
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

        private static bool IsHeavyImpact(in HitContext hit)
        {
            if (hit.AttackKind is AttackKind.HeavyAttack
                or AttackKind.ChargeAttack
                or AttackKind.SkillAttack
                or AttackKind.FinishAttack
                or AttackKind.DashAttack
                or AttackKind.JumpAttack)
                return true;

            return hit.ReactionType is AttackReactionType.Heavy
                or AttackReactionType.KnockBack
                or AttackReactionType.Airborne
                or AttackReactionType.Knockdown
                or AttackReactionType.Stun
                or AttackReactionType.Grab;
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
